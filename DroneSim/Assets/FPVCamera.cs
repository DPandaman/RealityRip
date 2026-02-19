using UnityEngine;

public class FPVCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform droneTarget; // Drag your Drone here

    [Header("Settings")]
    [Range(0, 90)] public float cameraTilt = 20f; // Set your tilt here
    public float smoothSpeed = 0.125f; // Lower = smoother, Higher = tighter

    void LateUpdate()
    {
        if (droneTarget == null) return;

        // 1. SNAP TO POSITION
        // We snap directly to position because laggy position feels bad in FPV.
        // If you still see position jitter, change this to Vector3.Lerp.
        transform.position = droneTarget.position;

        // 2. ROTATION WITH TILT
        // We take the drone's rotation and add our custom tilt on top.
        // The rotation is applied in LateUpdate to ensure the physics frame is finished.
        Quaternion targetRotation = droneTarget.rotation * Quaternion.Euler(cameraTilt, 0, 0);
        
        transform.rotation = targetRotation;
    }
}