using UnityEngine;

namespace DualSouls.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController3D : MonoBehaviour
    {
        [Header("Movement")]
        public float walkSpeed = 4.5f;
        public float runSpeed = 7.5f;

        [Header("Jump & Gravity")]
        public float jumpHeight = 1.6f;
        public float gravity = -25f;
        public float groundedGravity = -2f;

        [Header("Ground Check")]
        public Transform groundCheck;
        public float groundCheckRadius = 0.25f;
        public LayerMask groundLayer;
        public float coyoteTime = 0.12f;
        public float jumpBufferTime = 0.12f;

        [Header("Dash")]
        public bool canDash = true;
        public float dashSpeed = 16f;
        public float dashDuration = 0.16f;
        public float dashCooldown = 0.6f;

        [Header("Animation")]
        public Animator animator;

        private CharacterController controller;

        private Vector3 velocity;
        private Vector3 moveDirection;

        private bool isGrounded;
        private bool isDashing;

        private float dashTimer;
        private float dashCooldownTimer;

        private float coyoteTimer;
        private float jumpBufferTimer;

        public Vector3 MoveDirection => moveDirection;
        public bool IsGrounded => isGrounded;
        public bool IsDashing => isDashing;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();

            if (animator == null)
                animator = GetComponentInChildren<Animator>();
        }

        private void Update()
        {
            UpdateGroundCheck();
            UpdateTimers();
            ReadJumpInput();

            if (!isDashing)
            {
                HandleMovement();
                HandleJumpAndGravity();
            }

            HandleDash();
            UpdateAnimator();
        }

        private void UpdateGroundCheck()
        {
            if (groundCheck == null)
            {
                isGrounded = controller.isGrounded;
                return;
            }

            isGrounded = Physics.CheckSphere(
                groundCheck.position,
                groundCheckRadius,
                groundLayer,
                QueryTriggerInteraction.Ignore
            );

            if (isGrounded)
                coyoteTimer = coyoteTime;
        }

        private void UpdateTimers()
        {
            if (dashCooldownTimer > 0f)
                dashCooldownTimer -= Time.deltaTime;

            if (coyoteTimer > 0f)
                coyoteTimer -= Time.deltaTime;

            if (jumpBufferTimer > 0f)
                jumpBufferTimer -= Time.deltaTime;
        }

        private void ReadJumpInput()
        {
            if (Input.GetButtonDown("Jump"))
                jumpBufferTimer = jumpBufferTime;
        }

        private void HandleMovement()
        {
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");

            Vector3 inputDirection = new Vector3(horizontal, 0f, vertical).normalized;

            Vector3 forward = transform.forward;
            Vector3 right = transform.right;

            forward.y = 0f;
            right.y = 0f;

            forward.Normalize();
            right.Normalize();

            moveDirection = forward * inputDirection.z + right * inputDirection.x;
            moveDirection.Normalize();

            bool running = Input.GetKey(KeyCode.LeftShift);
            float speed = running ? runSpeed : walkSpeed;

            controller.Move(moveDirection * speed * Time.deltaTime);
        }

        private void HandleJumpAndGravity()
        {
            if (isGrounded && velocity.y < 0f)
                velocity.y = groundedGravity;

            bool canJump = coyoteTimer > 0f;
            bool wantsToJump = jumpBufferTimer > 0f;

            if (wantsToJump && canJump)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                jumpBufferTimer = 0f;
                coyoteTimer = 0f;
            }

            velocity.y += gravity * Time.deltaTime;
            controller.Move(velocity * Time.deltaTime);
        }

        private void HandleDash()
        {
            if (!canDash)
                return;

            if (Input.GetKeyDown(KeyCode.Q) && dashCooldownTimer <= 0f && !isDashing)
            {
                isDashing = true;
                dashTimer = dashDuration;
                dashCooldownTimer = dashCooldown;
            }

            if (isDashing)
            {
                dashTimer -= Time.deltaTime;

                Vector3 dashDirection = moveDirection.sqrMagnitude > 0.01f
                    ? moveDirection
                    : transform.forward;

                dashDirection.y = 0f;
                dashDirection.Normalize();

                controller.Move(dashDirection * dashSpeed * Time.deltaTime);

                if (dashTimer <= 0f)
                    isDashing = false;
            }
        }

        private void UpdateAnimator()
        {
            if (animator == null)
                return;

            float speedPercent = moveDirection.magnitude;

            if (Input.GetKey(KeyCode.LeftShift))
                speedPercent *= 2f;

            animator.SetFloat("Speed", speedPercent, 0.12f, Time.deltaTime);
            animator.SetBool("IsGrounded", isGrounded);
            animator.SetBool("IsDashing", isDashing);
            animator.SetFloat("VerticalVelocity", velocity.y);
        }

        private void OnDrawGizmosSelected()
        {
            if (groundCheck == null)
                return;

            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}