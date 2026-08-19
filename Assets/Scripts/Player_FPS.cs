using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class HumanFirstPersonController : MonoBehaviour
{
    [Header("Referências")]
    public Camera playerCamera;

    [Header("Corpo")]
    public float characterHeight = 1.8f;
    public float cameraHeight = 1.65f;
    public float characterRadius = 0.3f;

    [Header("Movimento")]
    public float walkSpeed = 4.5f;
    public float runSpeed = 7.5f;
    public float acceleration = 18f;
    public float deceleration = 22f;
    public float jumpHeight = 1.3f;
    public float gravity = -22f;

    [Header("Mouse")]
    public float mouseSensitivity = 2.2f;
    public float mouseSmoothTime = 0.03f;
    public float maxLookAngle = 85f;

    [Header("POV Humano")]
    public float idleBreathAmount = 0.015f;
    public float idleBreathFrequency = 1.4f;

    public float walkBobAmount = 0.045f;
    public float walkBobFrequency = 7.5f;

    public float runBobAmount = 0.075f;
    public float runBobFrequency = 10.5f;

    public float sideBobAmount = 0.025f;
    public float cameraSmoothTime = 0.04f;

    [Header("Inclinação")]
    public float strafeTiltAmount = 3.5f;
    public float mouseTiltAmount = 1.5f;
    public float tiltSmooth = 9f;

    [Header("FOV")]
    public float normalFOV = 70f;
    public float runFOV = 76f;
    public float fovSmoothTime = 0.12f;

    [Header("Impacto ao cair")]
    public float landingBumpAmount = 0.08f;
    public float landingRecoverSpeed = 8f;

    private CharacterController controller;

    private Vector2 moveInput;
    private Vector2 currentMouseDelta;
    private Vector2 currentMouseDeltaVelocity;

    private Vector3 horizontalVelocity;
    private Vector3 cameraPositionVelocity;

    private float verticalVelocity;
    private float yaw;
    private float pitch;
    private float bobTimer;
    private float currentRoll;
    private float currentFOVVelocity;
    private float landingOffset;

    private bool wasGrounded;
    private bool isRunning;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>();
        }

        controller.height = characterHeight;
        controller.radius = characterRadius;
        controller.center = new Vector3(0f, characterHeight / 2f, 0f);

        playerCamera.transform.localPosition = new Vector3(0f, cameraHeight, 0f);
        playerCamera.transform.localRotation = Quaternion.identity;
        playerCamera.fieldOfView = normalFOV;

        yaw = transform.eulerAngles.y;
        pitch = 0f;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        HandleCursor();
        HandleLook();
        HandleMovement();
        HandleCameraEffects();
    }

    void HandleCursor()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (Input.GetMouseButtonDown(0))
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void HandleLook()
    {
        Vector2 targetMouseDelta = new Vector2(
            Input.GetAxisRaw("Mouse X"),
            Input.GetAxisRaw("Mouse Y")
        );

        currentMouseDelta = Vector2.SmoothDamp(
            currentMouseDelta,
            targetMouseDelta,
            ref currentMouseDeltaVelocity,
            mouseSmoothTime
        );

        yaw += currentMouseDelta.x * mouseSensitivity;
        pitch -= currentMouseDelta.y * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, -maxLookAngle, maxLookAngle);

        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
    }

    void HandleMovement()
    {
        moveInput = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")
        );

        if (moveInput.magnitude > 1f)
        {
            moveInput.Normalize();
        }

        bool isGrounded = controller.isGrounded;

        isRunning = Input.GetKey(KeyCode.LeftShift) && moveInput.y > 0.1f;

        float targetSpeed = isRunning ? runSpeed : walkSpeed;

        Vector3 moveDirection = transform.right * moveInput.x + transform.forward * moveInput.y;

        if (moveDirection.magnitude > 1f)
        {
            moveDirection.Normalize();
        }

        Vector3 targetHorizontalVelocity = moveDirection * targetSpeed;

        float currentAcceleration = targetHorizontalVelocity.magnitude > horizontalVelocity.magnitude
            ? acceleration
            : deceleration;

        horizontalVelocity = Vector3.MoveTowards(
            horizontalVelocity,
            targetHorizontalVelocity,
            currentAcceleration * Time.deltaTime
        );

        if (isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        verticalVelocity += gravity * Time.deltaTime;

        float previousVerticalVelocity = verticalVelocity;

        Vector3 finalVelocity = horizontalVelocity + Vector3.up * verticalVelocity;

        controller.Move(finalVelocity * Time.deltaTime);

        if (!wasGrounded && controller.isGrounded && previousVerticalVelocity < -5f)
        {
            landingOffset = -landingBumpAmount;
        }

        wasGrounded = controller.isGrounded;
    }

    void HandleCameraEffects()
    {
        bool isMoving = moveInput.magnitude > 0.1f && controller.isGrounded;

        float verticalBob = 0f;
        float horizontalBob = 0f;

        if (isMoving)
        {
            float bobAmount = isRunning ? runBobAmount : walkBobAmount;
            float bobFrequency = isRunning ? runBobFrequency : walkBobFrequency;

            bobTimer += Time.deltaTime * bobFrequency;

            verticalBob = Mathf.Sin(bobTimer * 2f) * bobAmount;
            horizontalBob = Mathf.Cos(bobTimer) * sideBobAmount;
        }
        else
        {
            verticalBob = Mathf.Sin(Time.time * idleBreathFrequency) * idleBreathAmount;
            horizontalBob = 0f;
        }

        landingOffset = Mathf.Lerp(
            landingOffset,
            0f,
            Time.deltaTime * landingRecoverSpeed
        );

        Vector3 targetCameraPosition = new Vector3(
            horizontalBob,
            cameraHeight + verticalBob + landingOffset,
            0f
        );

        playerCamera.transform.localPosition = Vector3.SmoothDamp(
            playerCamera.transform.localPosition,
            targetCameraPosition,
            ref cameraPositionVelocity,
            cameraSmoothTime
        );

        float targetRoll = 0f;

        targetRoll += -moveInput.x * strafeTiltAmount;
        targetRoll += -currentMouseDelta.x * mouseTiltAmount;

        currentRoll = Mathf.Lerp(
            currentRoll,
            targetRoll,
            Time.deltaTime * tiltSmooth
        );

        playerCamera.transform.localRotation = Quaternion.Euler(
            pitch,
            0f,
            currentRoll
        );

        float targetFOV = isRunning && isMoving ? runFOV : normalFOV;

        playerCamera.fieldOfView = Mathf.SmoothDamp(
            playerCamera.fieldOfView,
            targetFOV,
            ref currentFOVVelocity,
            fovSmoothTime
        );
    }
}