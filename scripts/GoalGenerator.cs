// turns text from VLM into physical 3d object 

using UnityEngine;
using System.Collections.Generic;

public class RescueGoalGenerator : MonoBehaviour
{
    [Header("Simulation Parameters")]
    public GameObject goalPrefab; // visualization marker (torus)
    public LayerMask splatLayer;  // gaussian splat mesh collider layer
    
    // maintaining a list of active waypoints to manage scene clutter
    public List<Transform> activeGoals = new List<Transform>();

    public void GenerateGoalsFromAI(string aiResponse, Transform droneTransform)
    {
        // reset state for the new measurement update
        ClearGoals();

        Debug.Log($"RescueGoalGen: processing measurement: '{aiResponse}'");

        // 1. Semantic Parsing (The "Measurement Model")
        // currently using a naive heuristic (keyword matching) instead of strict json parsing
        // TODO: implement robust json deserialization for production
        
        bool targetIdentified = false;

        // hypothesis: if 'table' or 'gap' is detected, high probability of survivor/hazard
        if (aiResponse.ToLower().Contains("table") || aiResponse.ToLower().Contains("gap"))
        {
             // estimating position: projecting 2.0m forward vector + slight negative Z bias
             // this assumes the POI is directly in the camera's FOV center
             Vector3 predictedState = droneTransform.position + droneTransform.forward * 2.0f + Vector3.down * 0.5f;
             SpawnGoal(predictedState, "Hazard_Gap");
             targetIdentified = true;
        }
        
        // hypothesis: 'corner' or 'wall' implies structural bounds
        if (aiResponse.ToLower().Contains("corner") || aiResponse.ToLower().Contains("wall"))
        {
             // heuristic: offset to the right to simulate peripheral detection
             SpawnGoal(droneTransform.position + droneTransform.right * 1.5f, "Structure_Ref");
             targetIdentified = true;
        }

        // 2. State Correction / Fallback
        // if the measurement update failed (VLM hallucinated or saw nothing), 
        // initialize a default search pattern (covariance is high, so we search wide)
        if (!targetIdentified || activeGoals.Count == 0)
        {
            Debug.LogWarning("RescueGoalGen: measurement invalid. reverting to prior belief (default pattern).");
            SpawnGoal(droneTransform.position + Vector3.forward * 3f, "Search_Area_Alpha");
            SpawnGoal(droneTransform.position + Vector3.forward * 5f + Vector3.right * 2f, "Search_Area_Beta");
        }
    }

    void SpawnGoal(Vector3 pos, string name)
    {
        // instantiating the visual marker at the estimated coordinates
        GameObject g = Instantiate(goalPrefab, pos, Quaternion.identity);
        g.name = name;
        activeGoals.Add(g.transform);
    }

    public void ClearGoals()
    {
        // cleaning up the scene graph
        foreach (var t in activeGoals) Destroy(t.gameObject);
        activeGoals.Clear();
    }
}