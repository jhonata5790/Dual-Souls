using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovementHuman : MonoBehaviour
{
    [Header("Referências")]
    public Transform cameraTransform;

    [Header("Movimento")]
    public float walkSpeed = 3.2f;
    public float runSpeed = 5.2f;
    public float acceleration = 9f;
    public float deceleration = 12f;

    [Header("Gravidade")]
    public float gravity = -18f;
    public float groundedForce = -2f;

    [Header("Pulo Opcional")]
    public bool canJump = false;
    public float jumpForce = 5f;

    [Header("Sensação Humana")]
    public float bodySwayAmount = 0.035f;
    public float bodySwaySpeed = 7f;

    private CharacterController controller;
    private Vector3 currentVelocity;
    private Vector3 verticalVelocity;
    private Vector3 bodySwayVelocity;

    private bool movementLocked;

    public bool IsMoving { get; private set; }
    public bool IsRunning { get; private set; }
    public float CurrentSpeed01 { get; private set; }

    void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    void Update()
    {
        if (movementLocked)
        {
            StopMovementSmoothly();
            ApplyGravity();
            return;
        }

        HandleMovement();
        ApplyGravity();
    }

    void HandleMovement()
    {
        float inputX = Input.GetAxisRaw("Horizontal");
        float inputZ = Input.GetAxisRaw("Vertical");

        Vector3 inputDirection = new Vector3(inputX, 0f, inputZ).normalized;

        IsRunning = Input.GetKey(KeyCode.LeftShift) && inputDirection.magnitude > 0.1f;
        float targetSpeed = IsRunning ? runSpeed : walkSpeed;

        Vector3 moveDirection = Vector3.zero;

        if (cameraTransform != null)
        {
            Vector3 forward = cameraTransform.forward;
            Vector3 right = cameraTransform.right;

            forward.y = 0f;
            right.y = 0f;

            forward.Normalize();
            right.Normalize();

            moveDirection = forward * inputDirection.z + right * inputDirection.x;
        }
        else
        {
            moveDirection = transform.forward * inputDirection.z + transform.right * inputDirection.x;
        }

        Vector3 targetVelocity = moveDirection.normalized * targetSpeed;

        float lerpSpeed = inputDirection.magnitude > 0.1f ? acceleration : deceleration;

        currentVelocity = Vector3.Lerp(
            currentVelocity,
            targetVelocity,
            lerpSpeed * Time.deltaTime
        );

        controller.Move(currentVelocity * Time.deltaTime);

        IsMoving = currentVelocity.magnitude > 0.15f;
        CurrentSpeed01 = Mathf.InverseLerp(0f, runSpeed, currentVelocity.magnitude);

        ApplyBodySway(moveDirection);
    }

    void ApplyBodySway(Vector3 moveDirection)
    {
        if (moveDirection.magnitude < 0.1f)
            return;

        Vector3 sway = transform.right * Mathf.Sin(Time.time * bodySwaySpeed) * bodySwayAmount;
        bodySwayVelocity = Vector3.Lerp(bodySwayVelocity, sway, Time.deltaTime * 5f);
    }

    void ApplyGravity()
    {
        if (controller.isGrounded)
        {
            if (verticalVelocity.y < 0f)
                verticalVelocity.y = groundedForce;

            if (canJump && Input.GetKeyDown(KeyCode.Space) && !movementLocked)
            {
                verticalVelocity.y = jumpForce;
            }
        }
        else
        {
            verticalVelocity.y += gravity * Time.deltaTime;
        }

        controller.Move(verticalVelocity * Time.deltaTime);
    }

    void StopMovementSmoothly()
    {
        currentVelocity = Vector3.Lerp(
            currentVelocity,
            Vector3.zero,
            deceleration * Time.deltaTime
        );

        controller.Move(currentVelocity * Time.deltaTime);

        IsMoving = false;
        IsRunning = false;
        CurrentSpeed01 = Mathf.Lerp(CurrentSpeed01, 0f, Time.deltaTime * 8f);
    }

    public void SetMovementLocked(bool locked)
    {
        movementLocked = locked;

        if (locked)
        {
            currentVelocity = Vector3.zero;
            IsMoving = false;
            IsRunning = false;
            CurrentSpeed01 = 0f;
        }
    }

    public bool IsMovementLocked()
    {
        return movementLocked;
    }
}