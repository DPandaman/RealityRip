using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using System.Collections;
using System.Collections.Generic;

public class RescueGoalGenerator : MonoBehaviour
{
    [Header("Simulation Parameters")]
    public GameObject goalPrefab; 
    public LayerMask splatLayer;  

    [Header("AI Integration")]
    public AIService aiService;          
    public float raycastMaxDistance = 50f;
    public float goalHoverOffset = 0.3f;

    [Header("GPT-5.2 (Trajectory)")]
    public string openaiApiUrl = "https://api.openai.com/v1/chat/completions";
    public string openaiModel = "gpt-5.2";
    public int requestTimeout = 30;
    
    // Hardcoding your key here as requested
    private string apiKey = ""; // Add key locally, do not commit; 

    public List<Transform> activeGoals = new List<Transform>();

    [System.Serializable]
    public struct WaypointData {
        public string name;
        public float forward;
        public float right;
        public float up;
        public int priority;
    }

    void Awake()
    {
        // Debug check for the key
        if (string.IsNullOrEmpty(openaiApiKey) || openaiApiKey.Contains("YOUR_ACTUAL"))
            Debug.LogError("RescueGoalGen: API Key is still the placeholder! Please paste your actual key.");
        else
            Debug.Log("RescueGoalGen: API Key loaded and ready.");
    }

    public void GenerateGoalsFromAI(string vlmDescription, string userPrompt,
                                    Transform droneTransform, System.Action<bool> onComplete)
    {
        ClearGoals();
        string systemPrompt = "You are a spatial reasoning engine. Output ONLY a JSON array of waypoints.";
        string userMsg = $"Scene: {vlmDescription}\nMission: {userPrompt}";

        if (!string.IsNullOrEmpty(openaiApiKey))
            StartCoroutine(PostOpenAIRequest(systemPrompt, userMsg, droneTransform, onComplete));
        else
            SpawnFallbackPattern(droneTransform);
    }

    IEnumerator PostOpenAIRequest(string system, string user, Transform drone, System.Action<bool> onComplete)
    {
        string json = $@"{{ ""model"": ""{openaiModel}"", ""messages"": [ {{ ""role"": ""system"", ""content"": ""{system}"" }}, {{ ""role"": ""user"", ""content"": ""{user}"" }} ] }}";
        var request = new UnityWebRequest(openaiApiUrl, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + openaiApiKey);
        
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
            ProcessWaypointResponse(ParseResponseContent(request.downloadHandler.text), drone, onComplete);
        else
            onComplete?.Invoke(false);
    }

    // --- CLEANED: No more goalManager calls here ---
    void SpawnGoal(Vector3 pos, string name)
    {
        GameObject g = Instantiate(goalPrefab, pos, Quaternion.identity);
        g.name = name;
        activeGoals.Add(g.transform);
    }

    public void ClearGoals()
    {
        foreach (var t in activeGoals) if(t != null) Destroy(t.gameObject);
        activeGoals.Clear();
    }

    // Helper functions for parsing (Keep these from your current file)
    string ParseResponseContent(string json) { /* ... same as before ... */ return ""; }
    void ProcessWaypointResponse(string response, Transform drone, System.Action<bool> onComplete) { /* ... same as before ... */ }
    List<WaypointData> ParseWaypointJSON(string json) { /* ... same as before ... */ return new List<WaypointData>(); }
    Vector3 ValidateAndSnap(Vector3 estimated, Transform drone) { /* ... same as before ... */ return estimated; }
    void SpawnFallbackPattern(Transform drone) { SpawnGoal(drone.position + drone.forward * 3f, "Fallback_Alpha"); }
}
