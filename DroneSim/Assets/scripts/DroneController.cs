// drone controlling logic 

using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class DroneController : MonoBehaviour
{
    [Header("settings")]
    public float throttlePower = 20f;
    public float cyclicPower = 0.2f;
    public float yawPower = 0.15f;
    
    [Header("inputs")]
    public InputActionReference throttleAction; 
    public InputActionReference cyclicAction;   
    public InputActionReference yawAction;      

    private Rigidbody rb;
    private Vector2 cyclicInput;
    public float throttleInput;
    private float yawInput;

    void Awake(){
        // init physics
        rb = GetComponent<Rigidbody>();
        rb.mass = 1.0f; 
        rb.linearDamping = 1.0f; 
        rb.angularDamping = 2.0f; 
    }

    void Update(){
        // read stick values
        if (cyclicAction != null) cyclicInput = cyclicAction.action.ReadValue<Vector2>();
        if (yawAction != null) yawInput = yawAction.action.ReadValue<float>();
        
        // remap throttle -1:1 to 0:1
        float rawThrottle = (throttleAction != null) ? throttleAction.action.ReadValue<float>() : -1f;
        throttleInput = (rawThrottle + 1f) / 2f; 
    }

    void FixedUpdate(){
        // apply forces
        HandlePropellers();
        // HandleStabilization();
    }

    void HandlePropellers(){
        // apply lift
        Vector3 liftForce = Vector3.up * (throttleInput * throttlePower);
        rb.AddRelativeForce(liftForce);

        // apply rotations
        rb.AddRelativeTorque(Vector3.right * cyclicInput.y * cyclicPower); // pitch
        rb.AddRelativeTorque(Vector3.back * cyclicInput.x * cyclicPower);  // roll
        rb.AddRelativeTorque(Vector3.up * yawInput * yawPower);            // yaw
    }
    
    void HandleStabilization(){
        // auto level if no input
        if (cyclicInput.magnitude < 0.1f){
            Quaternion targetRotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * 2.0f);
        }
    }
}