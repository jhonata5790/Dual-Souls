using UnityEngine;
using System.Collections;

public class PlayerMovement : MonoBehaviour
{
    [Header("Referências")]
    public CharacterController controller;
    public Transform cameraTransform;
    public Transform feetTransform;

    [Header("Movimento")]
    public float walkSpeed = 6f;
    public float sprintSpeed = 10f;
    public float rotationSpeed = 14f;

    [Header("Suavização do Movimento")]
    public float acceleration = 18f;
    public float deceleration = 22f;
    public float airControl = 0.45f;

    private Vector3 currentHorizontalVelocity;

    [Header("Dash Terrestre")]
    public float dashDistance = 8f;
    public float dashDuration = 0.22f;
    public float dashCooldown = 0.7f;
    public AnimationCurve dashCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private bool isDashing = false;
    private bool canDash = true;

    [Header("Air Dash")]
    public float airDashDistance = 6f;
    public float airDashHeight = 2.2f;
    public float airDashDuration = 0.32f;

    public AnimationCurve airDashMoveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    public AnimationCurve airDashArcCurve = new AnimationCurve(
        new Keyframe(0f, 0f),
        new Keyframe(0.35f, 1f),
        new Keyframe(1f, 0f)
    );

    private bool hasUsedAirDash = false;

    [Header("Pulo")]
    public float gravity = -22f;
    public float jumpHeight = 2.4f;
    public float doubleJumpHeight = 2.2f;

    [Header("Polimento do Pulo")]
    public float coyoteTime = 0.12f;
    public float jumpBufferTime = 0.12f;
    public float fallMultiplier = 1.5f;

    private float coyoteTimer;
    private float jumpBufferTimer;

    [Header("Plataforma de Gelo")]
    public GameObject icePlatformPrefab;
    public float icePlatformLifeTime = 0.2f;

    private bool canDoubleJump = false;
    private bool hasUsedDoubleJump = false;

    [Header("Efeitos")]
    public GameObject dashWindFXPrefab;
    public GameObject iceCreateFXPrefab;
    public GameObject iceBreakFXPrefab;
    public Transform dashEffectPoint;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;

    private Vector3 verticalVelocity;
    private bool isGrounded;

    void Update()
    {
        CheckGround();
        HandleJumpBuffer();

        if (!isDashing)
        {
            HandleMovement();
            HandleJump();
            HandleGravity();
        }

        HandleDash();
    }

    void CheckGround()
    {
        isGrounded = Physics.CheckSphere(
            groundCheck.position,
            groundDistance,
            groundMask
        );

        if (isGrounded)
        {
            coyoteTimer = coyoteTime;

            canDoubleJump = true;
            hasUsedDoubleJump = false;
            hasUsedAirDash = false;

            if (verticalVelocity.y < 0f)
            {
                verticalVelocity.y = -2f;
            }
        }
        else
        {
            coyoteTimer -= Time.deltaTime;
        }
    }

    void HandleJumpBuffer()
    {
        if (Input.GetButtonDown("Jump"))
        {
            jumpBufferTimer = jumpBufferTime;
        }
        else
        {
            jumpBufferTimer -= Time.deltaTime;
        }
    }

    void HandleMovement()
    {
        Vector3 inputDirection = GetCameraRelativeDirection(out bool hasInput);

        float targetSpeed = Input.GetKey(KeyCode.LeftShift)
            ? sprintSpeed
            : walkSpeed;

        Vector3 targetVelocity = hasInput
            ? inputDirection * targetSpeed
            : Vector3.zero;

        float controlMultiplier = isGrounded ? 1f : airControl;

        float smoothRate = hasInput
            ? acceleration * controlMultiplier
            : deceleration * controlMultiplier;

        currentHorizontalVelocity = Vector3.Lerp(
            currentHorizontalVelocity,
            targetVelocity,
            smoothRate * Time.deltaTime
        );

        controller.Move(currentHorizontalVelocity * Time.deltaTime);

        if (hasInput && currentHorizontalVelocity.sqrMagnitude > 0.05f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(inputDirection);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }
    }

    void HandleJump()
    {
        if (jumpBufferTimer <= 0f)
        {
            return;
        }

        if (coyoteTimer > 0f)
        {
            verticalVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
        }
        else if (canDoubleJump && !hasUsedDoubleJump)
        {
            verticalVelocity.y = Mathf.Sqrt(doubleJumpHeight * -2f * gravity);

            hasUsedDoubleJump = true;
            canDoubleJump = false;

            jumpBufferTimer = 0f;

            CreateIcePlatform();
        }
    }

    void HandleGravity()
    {
        if (verticalVelocity.y < 0f)
        {
            verticalVelocity.y += gravity * fallMultiplier * Time.deltaTime;
        }
        else
        {
            verticalVelocity.y += gravity * Time.deltaTime;
        }

        controller.Move(verticalVelocity * Time.deltaTime);
    }

    void HandleDash()
    {
        if (!Input.GetKeyDown(KeyCode.Q) || isDashing)
        {
            return;
        }

        Vector3 dashDirection = GetCameraRelativeDirection(out bool hasDashInput);

        // Futuramente:
        // Se !hasDashInput, aqui entra guarda alta / parry.
        if (!hasDashInput)
        {
            return;
        }

        if (isGrounded && canDash)
        {
            StartCoroutine(GroundDash(dashDirection));
        }
        else if (!isGrounded && !hasUsedAirDash)
        {
            StartCoroutine(AirDash(dashDirection));
        }
    }

    Vector3 GetCameraRelativeDirection(out bool hasInput)
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 inputDirection = new Vector3(horizontal, 0f, vertical);

        hasInput = inputDirection.sqrMagnitude > 0.01f;

        if (!hasInput)
        {
            return Vector3.zero;
        }

        Vector3 cameraForward = cameraTransform.forward;
        Vector3 cameraRight = cameraTransform.right;

        cameraForward.y = 0f;
        cameraRight.y = 0f;

        cameraForward.Normalize();
        cameraRight.Normalize();

        Vector3 moveDirection =
            cameraForward * vertical +
            cameraRight * horizontal;

        return moveDirection.normalized;
    }

    IEnumerator GroundDash(Vector3 dashDirection)
    {
        isDashing = true;
        canDash = false;

        currentHorizontalVelocity = Vector3.zero;

        Quaternion targetRotation = Quaternion.LookRotation(dashDirection);
        transform.rotation = targetRotation;

        PlayDashWindFX(dashDirection);

        float elapsed = 0f;
        float previousCurveValue = 0f;

        while (elapsed < dashDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / dashDuration);
            float curveValue = dashCurve.Evaluate(t);

            float curveDelta = curveValue - previousCurveValue;
            previousCurveValue = curveValue;

            Vector3 dashMovement = dashDirection * dashDistance * curveDelta;

            controller.Move(dashMovement);

            yield return null;
        }

        isDashing = false;

        yield return new WaitForSeconds(dashCooldown);

        canDash = true;
    }

    IEnumerator AirDash(Vector3 dashDirection)
    {
        isDashing = true;
        hasUsedAirDash = true;

        currentHorizontalVelocity = Vector3.zero;
        verticalVelocity = Vector3.zero;

        Quaternion targetRotation = Quaternion.LookRotation(dashDirection);
        transform.rotation = targetRotation;

        PlayDashWindFX(dashDirection);
        CreateDiagonalIcePlatform(dashDirection);

        float elapsed = 0f;

        float previousMoveCurve = 0f;
        float previousArcValue = 0f;

        while (elapsed < airDashDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / airDashDuration);

            float moveCurve = airDashMoveCurve.Evaluate(t);
            float moveDelta = moveCurve - previousMoveCurve;
            previousMoveCurve = moveCurve;

            float arcValue = airDashArcCurve.Evaluate(t);
            float arcDelta = arcValue - previousArcValue;
            previousArcValue = arcValue;

            Vector3 horizontalMove =
                dashDirection *
                airDashDistance *
                moveDelta;

            Vector3 verticalMove =
                Vector3.up *
                airDashHeight *
                arcDelta;

            controller.Move(horizontalMove + verticalMove);

            yield return null;
        }

        verticalVelocity.y = -1f;

        isDashing = false;
    }

    void CreateIcePlatform()
    {
        Vector3 spawnPosition = feetTransform.position;

        GameObject platform = Instantiate(
            icePlatformPrefab,
            spawnPosition,
            Quaternion.identity
        );

        PlayIceCreateFX(spawnPosition);

        StartCoroutine(DestroyIcePlatform(platform, icePlatformLifeTime));
    }

    void CreateDiagonalIcePlatform(Vector3 dashDirection)
    {
        Vector3 spawnPosition =
            feetTransform.position
            - dashDirection * 0.8f
            - Vector3.up * 0.3f;

        Quaternion rotation =
            Quaternion.LookRotation(dashDirection) *
            Quaternion.Euler(45f, 0f, 0f);

        GameObject platform = Instantiate(
            icePlatformPrefab,
            spawnPosition,
            rotation
        );

        PlayIceCreateFX(spawnPosition);

        StartCoroutine(DestroyIcePlatform(platform, icePlatformLifeTime));
    }

    IEnumerator DestroyIcePlatform(GameObject platform, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (platform != null)
        {
            PlayIceBreakFX(platform.transform.position);
            Destroy(platform);
        }
    }

    void PlayDashWindFX(Vector3 direction)
    {
        if (dashWindFXPrefab == null || dashEffectPoint == null)
        {
            return;
        }

        Quaternion rotation = Quaternion.LookRotation(direction);

        GameObject fx = Instantiate(
            dashWindFXPrefab,
            dashEffectPoint.position,
            rotation
        );

        Destroy(fx, 1f);
    }

    void PlayIceCreateFX(Vector3 position)
    {
        if (iceCreateFXPrefab == null)
        {
            return;
        }

        GameObject fx = Instantiate(
            iceCreateFXPrefab,
            position,
            Quaternion.identity
        );

        Destroy(fx, 1f);
    }

    void PlayIceBreakFX(Vector3 position)
    {
        if (iceBreakFXPrefab == null)
        {
            return;
        }

        GameObject fx = Instantiate(
            iceBreakFXPrefab,
            position,
            Quaternion.identity
        );

        Destroy(fx, 1f);
    }
}
