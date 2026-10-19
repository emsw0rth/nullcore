using UnityEngine;
using UnityEngine.InputSystem;
using Nullcore.Core;
using Nullcore.InputSystem;
using Nullcore.Platforms;

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
        [SerializeField] private float jumpAnimDuration = 0.25f;
        [SerializeField] private AudioClip deathSound;

        [Header("Damage")]
        [Tooltip("Seconds the player can't be hurt again after taking damage or respawning.")]
        [SerializeField] private float invulnerabilityDuration = 1.5f;
        [Tooltip("Upward speed the player is thrown at when hurt without dying.")]
        [SerializeField] private float hurtBounceSpeed = 8f;
        [SerializeField] private float blinkInterval = 0.1f;

        [Header("State (read-only)")]
        [SerializeField] private bool isGrounded;
        [SerializeField] private bool hasDoubleJumped;
        [SerializeField] private bool runToggled;
        [SerializeField] private bool facingRight = true;
        [SerializeField] private bool isDashing;
        [SerializeField] private bool canDash = true;

        private static readonly int MotionStateHash = Animator.StringToHash("MotionState");
        private static readonly int JumpStateHash = Animator.StringToHash("Jump");

        private Rigidbody2D rb;
        private PlayerAbilities abilities;
        private SpriteRenderer spriteRenderer;
        private Animator animator;
        private AudioSource audioSource;
        private bool jumpQueued;
        private bool dashQueued;
        private float dashTimeRemaining;
        private float dashCooldownRemaining;
        private float jumpAnimTimeRemaining;
        private bool isMoving;
        private float lastDeathTime = -999f;
        private float invulnerableUntil;

        private const float DeathCooldown = 0.25f;

        public bool IsRunning { get; private set; }
        public bool IsInvulnerable => Time.time < invulnerableUntil;
        public bool HasDoubleJumped => hasDoubleJumped;
        public bool CanDash => canDash;
        public bool IsGrounded => isGrounded;
        public bool IsMoving => isMoving;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            abilities = GetComponent<PlayerAbilities>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            animator = GetComponent<Animator>();
            audioSource = GetComponent<AudioSource>();
        }

        // A hit from a hazard: costs one life point. The player is thrown up and briefly invulnerable.
        // Losing the last life point is a death (see Respawn).
        public void TakeDamage()
        {
            if (IsInvulnerable)
            {
                return;
            }

            if (GameSession.LoseLifePoint())
            {
                Respawn();
                return;
            }

            invulnerableUntil = Time.time + invulnerabilityDuration;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, hurtBounceSpeed);
            isGrounded = false;
        }

        // Instant death: costs an extra life, refills every life point and restarts the whole level, so
        // the player, pickups, enemies, doors, switches and platforms all start over. With no extra lives
        // left it is game over instead.
        public void Respawn()
        {
            // Hazards can report the same death more than once (e.g. enter + stay in one frame, or
            // two hazards at once), so only the first call in a short window counts as a death.
            if (Time.time - lastDeathTime <= DeathCooldown)
            {
                return;
            }
            lastDeathTime = Time.time;

            // The level is about to be unloaded, which would cut off a sound played from this object.
            GameAudioSettings.PlaySfxPersistent(deathSound);

            // Dying with no extra lives left (and so no life points either) ends the run.
            if (GameSession.ExtraLives <= 0)
            {
                GameSession.TriggerGameOver();
                return;
            }

            GameSession.LoseExtraLife();
            GameSession.RestoreAllLifePoints();
            GameSession.RestartLevel();
        }

        private void Update()
        {
            UpdateBlink();

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

            isMoving = moveInput != 0f;

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

            Collider2D groundCollider = groundCheck != null
                ? Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer)
                : null;
            isGrounded = groundCollider != null;

            if (isGrounded)
            {
                hasDoubleJumped = false;

                // Carry the player along with whatever moving platform they're standing on.
                // Applying this here (before the physics step runs, rather than from a
                // post-step collision callback on the platform) is what keeps it smooth
                // under Rigidbody2D interpolation - it becomes part of this tick's normal
                // position/velocity integration instead of a hard teleport after the fact.
                // Carry is expressed as velocity, not a position write - repeatedly forcing
                // a dynamic body's position each tick fights the physics solver (causing
                // jitter and/or suppressed movement/jump response). Adding the platform's
                // velocity on top of the player's own input velocity is consistent with how
                // the rest of this controller already drives movement, and composes cleanly
                // with jumping since Jump() only overwrites the y component.
                MovingPlatformController platform = groundCollider.GetComponentInParent<MovingPlatformController>();
                if (platform != null)
                {
                    rb.linearVelocity += new Vector2(platform.FrameDelta.x / Time.fixedDeltaTime, 0f);
                }
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

            if (animator != null)
            {
                int motionState;
                if (jumpAnimTimeRemaining > 0f)
                {
                    jumpAnimTimeRemaining -= Time.fixedDeltaTime;
                    motionState = 3;
                }
                else
                {
                    motionState = moveInput == 0f ? 0 : (IsRunning ? 2 : 1);
                }
                animator.SetInteger(MotionStateHash, motionState);
            }
        }

        // Flickers the sprite while the player is invulnerable.
        private void UpdateBlink()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            bool faded = IsInvulnerable && blinkInterval > 0f && Mathf.FloorToInt(Time.time / blinkInterval) % 2 == 0;
            var color = spriteRenderer.color;
            float alpha = faded ? 0.3f : 1f;
            if (!Mathf.Approximately(color.a, alpha))
            {
                color.a = alpha;
                spriteRenderer.color = color;
            }
        }

        private void Jump()
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            isGrounded = false;
            jumpAnimTimeRemaining = jumpAnimDuration;

            // The jump animation is a full take-off to landing sequence, so a double jump has to
            // restart it rather than carry on from wherever the first jump's animation had got to.
            if (animator != null)
            {
                animator.Play(JumpStateHash, 0, 0f);
            }
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
