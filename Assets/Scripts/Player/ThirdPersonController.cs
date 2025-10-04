using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerController : MonoBehaviour
{
    [Header("References")]
    public Transform cameraTransform;                    //If left empty, auto-grabs Camera.main.transform
    public InputActionReference move;                    //Value/Vector2
    public InputActionReference jump;                    //Button
    public InputActionReference sprint;                  //Button

    [Header("Speeds")]
    public float walkSpeed = 2.6f;                       //Meters per second
    public float sprintSpeed = 5.2f;                     //Meters per second
    public float acceleration = 12f;                     //How quickly to reach target speed
    public float deceleration = 14f;                     //How quickly to slow down when releasing input

    [Header("Rotation")]
    public float rotationSharpness = 12f;                //Higher = snappier turning

    [Header("Jump & Gravity")]
    public float jumpHeight = 1.2f;                      //Meters
    public float gravity = -30f;                         //Meters per second squared
    public float groundedStickForce = -2f;               //Keeps CharacterController grounded

    CharacterController cc;
    float verticalVel;
    float currentSpeed;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    void OnEnable()
    {
        move?.action.Enable();
        jump?.action.Enable();
        sprint?.action.Enable();
    }

    void OnDisable()
    {
        move?.action.Disable();
        jump?.action.Disable();
        sprint?.action.Disable();
    }

    void Update()
    {
        float dt = Time.deltaTime;

        //Read inputs
        Vector2 m = move != null ? move.action.ReadValue<Vector2>() : Vector2.zero;
        bool sprintHeld = sprint != null && sprint.action.IsPressed();

        //Camera-relative directions
        Vector3 camF = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
        Vector3 camR = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;
        Vector3 desiredDir = (camF * m.y + camR * m.x);
        float inputMag = Mathf.Clamp01(desiredDir.magnitude);
        desiredDir = desiredDir.normalized;

        //Target speed from input magnitude (supports analog sticks)
        float targetSpeed = (sprintHeld ? sprintSpeed : walkSpeed) * inputMag;

        //Speed smoothing
        float accel = targetSpeed > currentSpeed ? acceleration : deceleration;
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, accel * dt);

        //Rotate toward movement direction
        if (desiredDir.sqrMagnitude > 0.0001f)
        {
            Quaternion look = Quaternion.LookRotation(desiredDir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, rotationSharpness * dt);
        }

        //Jump
        bool grounded = cc.isGrounded;
        if (grounded && verticalVel < 0f) verticalVel = groundedStickForce;
        if (grounded && jump != null && jump.action.WasPressedThisFrame())
            verticalVel = Mathf.Sqrt(jumpHeight * -2f * gravity);

        //Gravity
        verticalVel += gravity * dt;

        //Move
        Vector3 horizontal = desiredDir * currentSpeed;
        Vector3 velocity = new Vector3(horizontal.x, verticalVel, horizontal.z);
        cc.Move(velocity * dt);
    }
}
