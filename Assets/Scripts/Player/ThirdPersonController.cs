using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInput))]
public class ThirdPersonController : MonoBehaviour
{
    [Header("References")]
    public Transform cameraTransform;                    //Assign Cinemachine camera; auto-grabs Camera.main if empty
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

    [Header("Backpedal Settings")]
    public bool backpedalKeepsFacing = true;             //Keep facing forward while moving backward
    public float backpedalMultiplier = 0.7f;             //Speed scale while backpedaling
    public float inputDeadzone = 0.08f;                  //Ignore tiny vertical input
    public float lateralBackpedalDeadzone = 0.25f;       //Treat as straight backpedal if |x| below this

    [Header("Jump Assist")]
    public float coyoteTime = 0.12f;                     //Grace after leaving ground
    public float jumpBuffer = 0.12f;                     //Grace before landing
    float coyoteTimer;
    float bufferTimer;

    [Header("Slope & Air Control")]
    public float maxAirControl = 0.6f;                   //Horizontal control scale while airborne
    public float terminalFallSpeed = -50f;               //Max downward speed
    public float slopeSlideGravity = 20f;                //Slide force on too-steep slopes
    public float groundProbeRadius = 0.25f;              //SphereCast radius for ground normal
    public float groundProbeDistance = 0.6f;             //Probe distance below center
    public LayerMask groundMask = ~0;                    //What counts as ground for normal probing

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

        //Ground probe for slope normal
        Vector3 groundNormal = Vector3.up;
        bool hasGround = false;
        RaycastHit hit;
        Vector3 probeOrigin = transform.position + Vector3.up * 0.1f;
        if (Physics.SphereCast(probeOrigin, groundProbeRadius, Vector3.down, out hit, groundProbeDistance, groundMask, QueryTriggerInteraction.Ignore))
        {
            groundNormal = hit.normal;
            hasGround = true;
        }

        //Read inputs
        Vector2 m = move != null ? move.action.ReadValue<Vector2>() : Vector2.zero;
        bool sprintHeld = sprint != null && sprint.action.IsPressed();

        //Camera-relative directions with null-guard fallback
        Vector3 camF = Vector3.forward;
        Vector3 camR = Vector3.right;
        if (cameraTransform != null)
        {
            camF = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
            camR = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;
        }

        //Default desired movement is camera-relative
        Vector3 desiredCam = camF * m.y + camR * m.x;

        //Detect straight backpedal input (pressing S with minimal A/D)
        bool isBackward = m.y < -inputDeadzone;
        bool smallLateral = Mathf.Abs(m.x) < lateralBackpedalDeadzone;
        bool backwardOnly = isBackward && smallLateral;

        //Character-relative backpedal to decouple from camera yaw
        Vector3 moveDir = desiredCam;
        if (backpedalKeepsFacing && backwardOnly)
        {
            float backAmt = -m.y; //positive magnitude of backward input
            moveDir = (-transform.forward * backAmt) + (transform.right * m.x);
        }

        //Cache grounded early (will be toggled false when we jump)
        bool grounded = cc.isGrounded;

        //Project movement onto ground plane first, then derive magnitude/normal
        if (grounded && hasGround && moveDir.sqrMagnitude > 0.0001f)
            moveDir = Vector3.ProjectOnPlane(moveDir, groundNormal);

        float inputMag = Mathf.Clamp01(moveDir.magnitude);
        Vector3 moveDirNorm = inputMag > 0.0001f ? moveDir.normalized : Vector3.zero;

        //Target speed from input magnitude
        float baseSpeed = sprintHeld ? sprintSpeed : walkSpeed;
        if (backpedalKeepsFacing && backwardOnly) baseSpeed *= backpedalMultiplier;
        float targetSpeed = baseSpeed * inputMag;

        //Speed smoothing
        float accel = targetSpeed > currentSpeed ? acceleration : deceleration;
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, accel * dt);

        //Choose facing direction
        Vector3 faceDir = moveDirNorm;
        if (backpedalKeepsFacing && backwardOnly)
            faceDir = transform.forward; //keep facing forward while backpedaling

        //Rotate toward facing direction
        if (faceDir.sqrMagnitude > 0.0001f)
        {
            Quaternion look = Quaternion.LookRotation(faceDir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, rotationSharpness * dt);
        }

        //Jump assist: coyote + buffer
        if (grounded) coyoteTimer = coyoteTime; else coyoteTimer -= dt;
        if (jump != null && jump.action.WasPressedThisFrame()) bufferTimer = jumpBuffer; else bufferTimer -= dt;

        //Apply grounded stick
        if (grounded && verticalVel < 0f) verticalVel = groundedStickForce;

        //Consume buffered jump if within coyote window
        if (coyoteTimer > 0f && bufferTimer > 0f)
        {
            verticalVel = Mathf.Sqrt(jumpHeight * -2f * gravity);
            coyoteTimer = 0f;
            bufferTimer = 0f;
            grounded = false;
        }

        //Gravity with terminal velocity
        verticalVel += gravity * dt;
        if (verticalVel < terminalFallSpeed) verticalVel = terminalFallSpeed;

        //Horizontal with reduced air control
        float airControlScale = grounded ? 1f : maxAirControl;
        Vector3 horizontal = moveDirNorm * (currentSpeed * airControlScale);

        //Steep slope slide
        if (grounded && hasGround)
        {
            float slopeAngle = Vector3.Angle(groundNormal, Vector3.up);
            if (slopeAngle > cc.slopeLimit)
            {
                Vector3 slideDir = Vector3.ProjectOnPlane(Vector3.down, groundNormal).normalized;
                horizontal += slideDir * slopeSlideGravity * dt;
            }
        }

        //Compose final velocity and move
        Vector3 velocity = new Vector3(horizontal.x, verticalVel, horizontal.z);
        cc.Move(velocity * dt);
    }
}