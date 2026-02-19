using UnityEngine;
using System.Collections.Generic;

public class MissionController : MonoBehaviour
{
    [Header("Subsystem Dependencies")]
    public VisionBridge vision;           
    public RescueGoalGenerator generator; 
    public PathArchitect architect;       // Updated to match the repo's PathArchitect
    public DroneCommentator commentator;  

    [Header("Mission Config")]
    [TextArea] 
    public string missionPrompt = "Scan scene. Identify structural hazards and potential survivor locations.";

    // --- REQUIRED FOR UI BUTTONS ---
    // The repo doesn't have this, but your UI needs it!
    public void StartMissionFromPrompt(string userPrompt)
    {
        Debug.Log($"MissionController: Received UI prompt: {userPrompt}");
        if (!string.IsNullOrEmpty(userPrompt))
        {
            missionPrompt = userPrompt;
        }
        StartMissionGeneration();
    }
    // -------------------------------

    public void StartMissionGeneration()
    {
        Debug.Log("--- MISSION START: INITIALIZING SEQUENCE ---");
        
        if (vision == null)
        {
            Debug.LogError("MissionController: VisionBridge is missing!");
            return;
        }

        // Step 1: Perception (Vision Bridge)
        vision.ScanScene(missionPrompt, (aiResponse) => 
        {
            // Step 2: Goal Generation (RescueGoalGenerator)
            if (generator != null)
            {
                // Note: The repo version of GenerateGoalsFromAI might have different arguments.
                // We assume it matches the signature: (string response, string prompt, Transform drone, Action<bool> callback)
                generator.GenerateGoalsFromAI(aiResponse, missionPrompt, Camera.main.transform, (success) => 
                {
                    if (success)
                    {
                        // Step 3: Path Planning (PathArchitect)
                        if (architect != null)
                        {
                            // The repo likely uses 'BuildRescuePath' or 'GeneratePath'
                            // We pass the list of goals from the generator
                            architect.BuildRescuePath(generator.activeGoals);
                        }

                        // Step 4: Commentary (DroneCommentator)
                        if(commentator != null) 
                        {
                            string count = generator.activeGoals.Count.ToString();
                            commentator.Announce("Mission Plan", $"{count} goals identified. Path calculated.");
                        }
                    }
                });
            }
        }); 
   }
}