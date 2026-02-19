// main script 
// tells VisionBridge to scan room 
// passes VLM results to goal generator
// gets path from path generator 
// sends final mission to commentator 

using UnityEngine;

public class MissionController : MonoBehaviour
{
    [Header("Subsystem Dependencies")]
    public VisionBridge vision;        // visual perception
    public RescueGoalGenerator generator; // state estimation
    public PathGenerator architect;    // trajectory planning
    public DroneCommentator commentator; // human-machine interface (HMI)

    [Header("Mission Config")]
    [TextArea] 
    public string missionPrompt = "Scan scene. Identify structural hazards and potential survivor locations (tables, corners). Prioritize tight gaps.";

    // Trigger via UI Event
    public void StartMissionGeneration()
    {
        Debug.Log("--- MISSION START: INITIALIZING SEQUENCE ---");
        
        // Step 1: Perception Update
        // asynchronous call to VLM for scene analysis
        vision.ScanScene(missionPrompt, (aiResponse) => 
        {
            // callback received: measurement data acquired
            
            // Step 2: State Estimation
            // generate goalposts based on semantic parsing of VLM output
            generator.GenerateGoalsFromAI(aiResponse, Camera.main.transform); 

            // Step 3: Path Planning
            // compute optimal trajectory through estimated waypoints
            architect.BuildRescuePath(generator.activeGoals);

            // Step 4: User Feedback
            // construct status report for the HMI
            string stats = $"{generator.activeGoals.Count} points of interest.";
            string status = generator.activeGoals.Count > 0 ? "optimal path found." : "convergence failed (fallback used).";
            
            if(commentator != null) 
            {
                // synthesizing voice response
                commentator.Announce("Mission Plan Generated", $"{stats} {status}");
            }
            
            Debug.Log($"<color=green>MISSION READY:</color> {stats}. pipeline execution complete.");
        });
    }
}
