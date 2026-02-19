// contains respawn logic 

using UnityEngine;

public class FlightManager : MonoBehaviour
{
    public GameObject drone;
    public Transform startPoint;
    public DroneCommentator commentator;

    public void ResetDrone(){
        // move drone to start
        drone.transform.position = startPoint.position;
        drone.transform.rotation = startPoint.rotation;

        // kill momentum
        Rigidbody rb = drone.GetComponent<Rigidbody>();
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // reset goal state
        commentator.ResetGoalStatus(); 
        
        Debug.Log("<color=yellow>respawned:</color> drone reset to start");
    }
}