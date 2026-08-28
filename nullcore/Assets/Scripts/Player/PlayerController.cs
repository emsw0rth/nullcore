using UnityEngine;
using UnityEngine.InputSystem;
using Nullcore.InputSystem;

namespace Nullcore.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private float walkSpeed = 5f;
        [SerializeField] private float runSpeed = 9f;
        [SerializeField] private float jumpForce = 12f;
        [SerializeField] private Transform groundCheck;
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private float groundCheckRadius = 0.15f;
        [SerializeField] private float dashSpeed = 18f;
        [SerializeField] private float dashDuration = 0.2f;
        [SerializeField] private float dashCooldown = 1.5f;

        [Header("State (read-only)")]
        [SerializeField] private bool isGrounded;
        [SerializeField] private bool hasDoubleJumped;
        [SerializeField] private bool runToggled;
        [SerializeField] private bool facingRight = true;
        [SerializeField] private bool isDashing;
        [SerializeField] private bool canDash = true;

        private Rigidbody2D rb;
        private PlayerAbilities abilities;
        private SpriteRenderer spriteRenderer;
        private bool jumpQueued;
        private bool dashQueued;
        private float dashTimeRemaining;
        private float dashCooldownRemaining;
        private Vector3 spawnPosition;

        public bool IsRunning { get; private set; }
        public bool HasDoubleJumped => hasDoubleJumped;
        public bool CanDash => canDash;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            abilities = GetComponent<PlayerAbilities>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            spawnPosition = transform.position;
        }

        public void Respawn()
        {
            transform.position = spawnPosition;
            rb.linearVelocity = Vector2.zero;
            isDashing = false;
            isGrounded = false;
            hasDoubleJumped = false;
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (KeybindManager.WasPressedThisFrame(keyboard, KeybindManager.Jump))
            {
                jumpQueued = true;
            }

            if (KeybindManager.WasPressedThisFrame(keyboard, KeybindManager.ToggleRunWalk))
            {
                runToggled = !runToggled;
            }

            if (KeybindManager.WasPressedThisFrame(keyboard, KeybindManager.Dash))
            {
                dashQueued = true;
            }
        }

        private void FixedUpdate()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            float moveInput = 0f;
            if (KeybindManager.IsPressed(keyboard, KeybindManager.Left))
            {
                moveInput -= 1f;
            }
            if (KeybindManager.IsPressed(keyboard, KeybindManager.Right))
            {
                moveInput += 1f;
            }

            if (moveInput > 0f)
            {
                facingRight = true;
            }
            else if (moveInput < 0f)
            {
                facingRight = false;
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.flipX = !facingRight;
            }

            if (dashQueued && !isDashing && canDash && abilities != null && abilities.DashEnabled && abilities.DashLearned)
            {
                canDash = false;
                dashCooldownRemaining = dashCooldown;
                isDashing = true;
                dashTimeRemaining = dashDuration;
            }
            dashQueued = false;

            if (!canDash)
            {
                dashCooldownRemaining -= Time.fixedDeltaTime;
                if (dashCooldownRemaining <= 0f)
                {
                    canDash = true;
                }
            }

            bool shiftHeld = KeybindManager.IsPressed(keyboard, KeybindManager.Sprint);
            bool wantsToRun = runToggled ? !shiftHeld : shiftHeld;
            IsRunning = wantsToRun && abilities != null && abilities.SprintLearned;

            if (isDashing)
            {
                rb.linearVelocity = new Vector2(facingRight ? dashSpeed : -dashSpeed, rb.linearVelocity.y);
                dashTimeRemaining -= Time.fixedDeltaTime;
                if (dashTimeRemaining <= 0f)
                {
                    isDashing = false;
                }
            }
            else
            {
                float speed = IsRunning ? runSpeed : walkSpeed;
                rb.linearVelocity = new Vector2(moveInput * speed, rb.linearVelocity.y);
            }

            isGrounded = groundCheck != null && Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

            if (isGrounded)
            {
                hasDoubleJumped = false;
            }

            if (jumpQueued)
            {
                if (isGrounded)
                {
                    Jump();
                }
                else if (abilities != null && abilities.DoubleJumpEnabled && abilities.DoubleJumpLearned && !hasDoubleJumped)
                {
                    hasDoubleJumped = true;
                    Jump();
                }
            }
            jumpQueued = false;
        }

        private void Jump()
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            isGrounded = false;
        }

        private void OnDrawGizmosSelected()
        {
            if (groundCheck == null)
            {
                return;
            }
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}
