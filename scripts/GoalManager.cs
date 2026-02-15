// gps and mission logic: stores list of specific locations in the map 
// you can choose which location the drone will fly toward 

using UnityEngine;
using System.Collections.Generic;
using System.Linq; 

public class GoalManager : MonoBehaviour
{
    [System.Serializable]
    public struct Landmark {
        public string name;      // e.g. "kitchen"
        public Transform location; // spot in 3d space
    }

    // connections
    public List<Landmark> landmarks; // list of room spots
    public DroneCommentator commentator; // drone ref

    public void SetActiveGoal(string goalName){
        // find spot by name
        Landmark target = landmarks.FirstOrDefault(l => l.name.ToLower() == goalName.ToLower());

        if (target.location != null){
            // update commentator target
            commentator.currentGoal = target.location;
            
            // reset goal flag so ai can trigger again
            commentator.goalReached = false; 
            
            Debug.Log($"goal set to: {target.name}");
        }
        else{
            Debug.LogWarning($"landark not found: {goalName}");
        }
    }

    public void SetRandomGoal(){
        // pick random spot from list
        if (landmarks.Count > 0){
            int rnd = Random.Range(0, landmarks.Count);
            commentator.currentGoal = landmarks[rnd].location;
            commentator.goalReached = false;
        }
    }

    // run this to find all landmarks under a specific parent object
    public void FindAllLandmarks(){
        // clear existing list
        landmarks.Clear();

        // look at all children of this manager
        foreach (Transform child in transform){
            Landmark l;
            l.name = child.name; // uses the gameobject name as the goal name
            l.location = child;
            landmarks.Add(l);
        }
    
        Debug.Log($"found {landmarks.Count} landmarks");
    }

    void Start(){
        // find them automatically when the game starts
        FindAllLandmarks();
    }
}