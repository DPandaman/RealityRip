// contains the logic for generative commentary 
// monitors physics and gives live feedback

using UnityEngine;
using TMPro;
using System.Collections.Generic; 
using System; 

public class DroneCommentator : MonoBehaviour
{
    // connections
    public AIService aiService;
    public TextMeshProUGUI uiText;
    public DroneController droneCtrl; // need this for throttle check
    public Transform currentGoal;     // need this for goal check
    public VoiceService voiceService; // for speaking comments out loud 

    // settings
    [TextArea(3, 10)]
    public string personaPrompt = "You are a Gen Z flight commentator. Use slang like 'cooked', 'bet', 'no cap', and 'skill issue'. Keep it short.";
    
    // state variables 
    private bool isTalking = false;
    private float idleTimer = 0f;
    private Rigidbody rb;
    private Vector3 lastAngularVelocity; 
    public bool goalReached = false; // prevents goal spam
    public float jerkThreshold = 15f;   
    private float smoothTurnTimer = 0f;
    public float smoothTurnMinDuration = 1.5f; // how long to hold the turn 
    public List<string> flightLog = new List<string>(); // stores the full history of comments
    public void ResetGoalStatus() => goalReached = false;

    void Start()
    {
        // get physics ref
        rb = droneCtrl.GetComponent<Rigidbody>(); 
    }

    void Update()
    {
        float speed = rb.linearVelocity.magnitude; 

        // check speeding
        if (speed > 15f && !isTalking){
            TriggerCommentary("speeding", "nothing");
        }

        // check if flying unstable 
        if (rb.angularVelocity.magnitude > 5f && !isTalking){
             TriggerCommentary("losing control", "gravity");
        }

        // check if jerking turns 
        Vector3 angularAcceleration = (rb.angularVelocity - lastAngularVelocity) / Time.deltaTime;
        if (angularAcceleration.magnitude > jerkThreshold && !isTalking){
            TriggerCommentary("jerking turns", "joystick");
        }

        // check idle
        if (speed < 0.1f) idleTimer += Time.deltaTime;
        else idleTimer = 0f;

        if (idleTimer > 10f && !isTalking){
            TriggerCommentary("idle", "nothing");
            idleTimer = 0f;
        }

        // check near miss
        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, 1.0f)){
            if (speed > 5f && !isTalking){
                TriggerCommentary("near miss", hit.collider.name);
            }
        }

        // check if stuck 
        if (droneCtrl != null){
            //hitting throttle but not moving 
            if (droneCtrl.throttleInput > 0.8f && speed < 0.1f && !isTalking){
                if (Physics.Raycast(transform.position, transform.forward, 1.0f)){
                    TriggerCommentary("stuck", "wall");
                }
            }
        }

        // check if upside down 
        if (Vector3.Dot(transform.up, Vector3.down) > 0.5f && !isTalking){
            TriggerCommentary("upside down", "gravity");
        }

        // check if we passed goal
        if (currentGoal != null){
            float distToGoal = Vector3.Distance(transform.position, currentGoal.position);
            if (distToGoal < 2.0f && !goalReached && !isTalking){
                goalReached = true; 
                TriggerCommentary("goal reached", currentGoal.name);
            }
        }

        // check for smooth turn 
        // if we are turning (angular velocity > 1) and not jerking (accel < threshold)
        if (rb.angularVelocity.magnitude > 1.0f && angularAcceleration.magnitude < (jerkThreshold * 0.5f)){
            smoothTurnTimer += Time.deltaTime;
        }
        else{
            smoothTurnTimer = 0f; // reset if we stop or jerk
        }
        // trigger if held long enough
        if (smoothTurnTimer > smoothTurnMinDuration && !isTalking){
            TriggerCommentary("smooth turn", "the air");
            smoothTurnTimer = 0f; // reset so it doesn't spam
        }

        // check for successful navigation thru difficult obstacle 
        // check for narrow gap navigation
        bool leftHit = Physics.Raycast(transform.position, -transform.right, 1.5f);
        bool rightHit = Physics.Raycast(transform.position, transform.right, 1.5f);
        if (leftHit && rightHit && speed > 5f && !isTalking){
            TriggerCommentary("tight gap navigation", "obstacles");
        }

        lastAngularVelocity = rb.angularVelocity; // update spin for next frame
    }

    void OnCollisionEnter(Collision collision){
        if (collision.relativeVelocity.magnitude > 2f && !isTalking){
            TriggerCommentary("crash", collision.gameObject.name); 
        }
    }

   public void TriggerCommentary(string eventType, string objectHit){
        isTalking = true;
        uiText.text = "AI Thinking...";

        float speedMph = rb.linearVelocity.magnitude * 2.237f;

        // custom instruction for high-skill moments
        string skillBonus = "";
        if (eventType == "smooth turn" || eventType == "tight gap navigation" || eventType == "goal reached"){        
            skillBonus = " IMPORTANT: Start your response with 'chat is that rizz'.";
        }

        // build prompts
        string systemPrompt = personaPrompt + skillBonus;
        string userPrompt = $"I just caused a {eventType} event involving {objectHit} at {speedMph:F1} MPH. React.";

        aiService.SendPrompt(systemPrompt, userPrompt, (response) => 
        {
            uiText.text = response;
            LogCommentary(eventType, response);  // log the comment with a timestamp
            if (voiceService != null) voiceService.Speak(response);
            Invoke("ResetTalking", 5f);
        });
    }

    public void LogCommentary(string eventType, string response){
    string timestamp = DateTime.Now.ToString("HH:mm:ss");
    string logEntry = $"[{timestamp}] {eventType.ToUpper()}: {response}"; // format line
    
    flightLog.Add(logEntry); // add to list
    
    // print to console so you can see it live
    Debug.Log("<color=cyan>Flight Logged:</color> " + logEntry);
    }

    void ResetTalking() => isTalking = false;

    public void ExportFlightLog(){
        Debug.Log("--- FLIGHT SUMMARY ---");
        foreach (string entry in flightLog){
            Debug.Log(entry);
        }
        Debug.Log("----------------------");
    }

    public void Announce(string eventType, string context){
        if (isTalking) return; // don't interrupt yourself
        isTalking = true;

        if(uiText != null) uiText.text = "Analysis...";

        string systemPrompt = personaPrompt + " Context: We are in a search-and-rescue simulation.";
        string userPrompt = $"Event: {eventType}. Details: {context}. React to this update.";

        if(aiService != null) {
            aiService.SendPrompt(systemPrompt, userPrompt, (response) => 
            {
                if(uiText != null) uiText.text = response;
                LogCommentary(eventType, response);
                if (voiceService != null) voiceService.Speak(response);
                Invoke("ResetTalking", 4f);
            });
        }
    }
    public string[] GetMapNames()
    {
        // Now returns a string Array to satisfy UIManager
        return new string[] { "Default", "Disaster_Zone", "Stanford_Quad" };
    }

    public void SetMapProfile(string mapName)
    {
        Debug.Log($"Commentator: Switching map profile to {mapName}");
        // You can add logic here later to change the commentator's personality based on the map
    }

    public void SetVibe(string vibe)
    {
        Debug.Log($"Commentator: Setting vibe to {vibe}");
        personaPrompt = $"You are a {vibe} drone pilot. Keep it short and helpful.";
    }
    // ---------------------------------------------
}


