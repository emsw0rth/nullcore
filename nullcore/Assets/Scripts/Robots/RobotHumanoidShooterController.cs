using UnityEngine;
using Nullcore.Player;

namespace Nullcore.Robots
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class RobotHumanoidShooterController : MonoBehaviour
    {
        private enum State
        {
            Patrol,
            ArmingWeapon,
            Shooting,
            HolsteringWeapon
        }

        [SerializeField] private float speed = 2f;
        [SerializeField] private bool startFacingRight = true;

        [Header("Sensors")]
        [SerializeField] private Transform groundCheck;
        [SerializeField] private float groundCheckRadius = 0.1f;
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private LayerMask obstacleLayer;
        [SerializeField] private Collider2D bodyCollider;
        [SerializeField] private float bodyHalfWidth = 0.35f;
        [SerializeField] private float wallCheckDistance = 0.05f;
        [SerializeField] private float edgeCheckDropDistance = 0.3f;

        // Sensors start from bodyCollider's real bounds; bodyHalfWidth is only a fallback for
        // instances with no bodyCollider assigned.

        [Header("Player Detection")]
        [SerializeField] private float detectionWidth = 3f;
        [SerializeField] private float detectionHeight = 1f;

        [Header("Visuals")]
        [SerializeField] private SpriteRenderer bodyRenderer;
        [SerializeField] private Sprite[] walkFrames;
        [SerializeField] private float walkFrameDuration = 0.1f;

        [Header("Attack")]
        [SerializeField] private Sprite[] armWeaponFrames;
        [SerializeField] private float armWeaponFrameDuration = 0.08f;
        [SerializeField] private Sprite shootFrame;
        [SerializeField] private float shootHoldDuration = 0.2f;
        [SerializeField] private Sprite[] holsterWeaponFrames;
        [SerializeField] private float holsterWeaponFrameDuration = 0.08f;
        [SerializeField] private GameObject laserShotPrefab;
        [SerializeField] private float laserSpawnForwardOffset = 0.5f;
        [SerializeField] private float laserSpawnHeightOffset = 0.2f;

        private Rigidbody2D rb;
        private bool facingRight;
        private bool isGrounded;
        private int walkFrameIndex;
        private float walkFrameTimer;

        private State state = State.Patrol;
        private int sequenceFrameIndex;
        private float sequenceTimer;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            facingRight = startFacingRight;
            UpdateFacingVisuals();
        }

        private void FixedUpdate()
        {
            isGrounded = groundCheck != null && Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

            if (!isGrounded)
            {
                return;
            }

            switch (state)
            {
                case State.Patrol:
                    UpdatePatrol();
                    break;
                case State.ArmingWeapon:
                    HoldStill();
                    if (AdvanceSequence(armWeaponFrames, armWeaponFrameDuration))
                    {
                        EnterShooting();
                    }
                    break;
                case State.Shooting:
                    HoldStill();
                    sequenceTimer -= Time.fixedDeltaTime;
                    if (sequenceTimer <= 0f)
                    {
                        EnterHolsteringWeapon();
                    }
                    break;
                case State.HolsteringWeapon:
                    HoldStill();
                    if (AdvanceSequence(holsterWeaponFrames, holsterWeaponFrameDuration))
                    {
                        if (PlayerSpotted())
                        {
                            EnterArmingWeapon();
                        }
                        else
                        {
                            EnterPatrol();
                        }
                    }
                    break;
            }
        }

        private void UpdatePatrol()
        {
            if (ShouldTurnAround())
            {
                facingRight = !facingRight;
                UpdateFacingVisuals();
            }

            float direction = facingRight ? 1f : -1f;
            rb.linearVelocity = new Vector2(direction * speed, rb.linearVelocity.y);

            AdvanceWalkAnimation();

            if (PlayerSpotted())
            {
                EnterArmingWeapon();
            }
        }

        private void HoldStill()
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }

        private bool PlayerSpotted()
        {
            float direction = facingRight ? 1f : -1f;
            Vector2 center = rb.position + Vector2.right * direction * (detectionWidth * 0.5f);
            Vector2 size = new Vector2(detectionWidth, detectionHeight);

            var hits = Physics2D.OverlapBoxAll(center, size, 0f);
            foreach (var hit in hits)
            {
                if (hit.GetComponent<PlayerController>() != null)
                {
                    return true;
                }
            }
            return false;
        }

        private void EnterArmingWeapon()
        {
            state = State.ArmingWeapon;
            sequenceFrameIndex = 0;
            sequenceTimer = armWeaponFrameDuration;
            if (bodyRenderer != null && armWeaponFrames != null && armWeaponFrames.Length > 0)
            {
                bodyRenderer.sprite = armWeaponFrames[0];
            }
        }

        private void EnterShooting()
        {
            state = State.Shooting;
            sequenceTimer = shootHoldDuration;
            if (bodyRenderer != null && shootFrame != null)
            {
                bodyRenderer.sprite = shootFrame;
            }

            FireLaser();
        }

        private void FireLaser()
        {
            if (laserShotPrefab == null)
            {
                return;
            }

            float direction = facingRight ? 1f : -1f;
            Vector2 spawnPos = rb.position + new Vector2(direction * laserSpawnForwardOffset, laserSpawnHeightOffset);

            var laser = Instantiate(laserShotPrefab, spawnPos, Quaternion.identity);
            laser.GetComponent<LaserShotController>()?.Launch(new Vector2(direction, 0f), facingRight);
        }

        private void EnterHolsteringWeapon()
        {
            state = State.HolsteringWeapon;
            sequenceFrameIndex = 0;
            sequenceTimer = holsterWeaponFrameDuration;
            if (bodyRenderer != null && holsterWeaponFrames != null && holsterWeaponFrames.Length > 0)
            {
                bodyRenderer.sprite = holsterWeaponFrames[0];
            }
        }

        private void EnterPatrol()
        {
            state = State.Patrol;
        }

        // Advances a non-looping frame sequence (used for the arm/holster weapon animations). Returns
        // true once the sequence has played through its final frame.
        private bool AdvanceSequence(Sprite[] frames, float frameDuration)
        {
            if (frames == null || frames.Length == 0)
            {
                return true;
            }

            sequenceTimer -= Time.fixedDeltaTime;
            if (sequenceTimer > 0f)
            {
                return false;
            }

            sequenceFrameIndex++;
            if (sequenceFrameIndex >= frames.Length)
            {
                return true;
            }

            if (bodyRenderer != null)
            {
                bodyRenderer.sprite = frames[sequenceFrameIndex];
            }
            sequenceTimer = frameDuration;
            return false;
        }

        private void AdvanceWalkAnimation()
        {
            if (walkFrames == null || walkFrames.Length == 0 || bodyRenderer == null)
            {
                return;
            }

            walkFrameTimer -= Time.fixedDeltaTime;
            if (walkFrameTimer > 0f)
            {
                return;
            }

            walkFrameIndex = (walkFrameIndex + 1) % walkFrames.Length;
            bodyRenderer.sprite = walkFrames[walkFrameIndex];
            walkFrameTimer = walkFrameDuration;
        }

        private bool ShouldTurnAround()
        {
            float direction = facingRight ? 1f : -1f;
            float frontX = FrontEdgeX(direction);
            Vector2 wallOrigin = new Vector2(frontX, BodyCenterY);

            // Obstacles (e.g. a closed door) aren't necessarily on the Ground layer, so this checks
            // a separate, broader mask than the ground/edge checks below. Triggers (ability pickups,
            // switch/teleporter detection zones, etc.) must be excluded explicitly - Physics2D.Raycast
            // hits them by default, which would otherwise register as a false "wall ahead".
            if (Cast(wallOrigin, Vector2.right * direction, wallCheckDistance, obstacleLayer))
            {
                return true;
            }

            // Cast from foot height (not body center) so the ray actually reaches the ground plane.
            Vector2 edgeOrigin = new Vector2(frontX, groundCheck.position.y);
            return !Cast(edgeOrigin, Vector2.down, edgeCheckDropDistance, groundLayer);
        }

        // The x of the body's leading edge in the given direction. The collider isn't necessarily
        // centered on the root (the art is asymmetric), so measure from its real bounds rather than
        // from root +/- half-width, otherwise the sensor can start behind the edge and miss walls.
        private float FrontEdgeX(float direction)
        {
            if (bodyCollider != null)
            {
                var bounds = bodyCollider.bounds;
                return direction > 0f ? bounds.max.x : bounds.min.x;
            }
            return rb.position.x + direction * bodyHalfWidth;
        }

        private float BodyCenterY => bodyCollider != null ? bodyCollider.bounds.center.y : rb.position.y;

        private static readonly RaycastHit2D[] RaycastResults = new RaycastHit2D[1];

        private static bool Cast(Vector2 origin, Vector2 direction, float distance, LayerMask mask)
        {
            var filter = new ContactFilter2D();
            filter.SetLayerMask(mask);
            filter.useTriggers = false;
            return Physics2D.Raycast(origin, direction, filter, RaycastResults, distance) > 0;
        }

        private void UpdateFacingVisuals()
        {
            // The source art faces right by default, so it needs to be flipped when facing left.
            if (bodyRenderer != null)
            {
                bodyRenderer.flipX = !facingRight;
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (groundCheck != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
            }

            float direction = facingRight ? 1f : -1f;
            float frontX = bodyCollider != null
                ? (direction > 0f ? bodyCollider.bounds.max.x : bodyCollider.bounds.min.x)
                : transform.position.x + direction * bodyHalfWidth;
            float centerY = bodyCollider != null ? bodyCollider.bounds.center.y : transform.position.y;
            Vector2 wallOrigin = new Vector2(frontX, centerY);
            Gizmos.color = Color.red;
            Gizmos.DrawLine(wallOrigin, wallOrigin + Vector2.right * direction * wallCheckDistance);

            if (groundCheck != null)
            {
                Vector2 edgeOrigin = new Vector2(frontX, groundCheck.position.y);
                Gizmos.DrawLine(edgeOrigin, edgeOrigin + Vector2.down * edgeCheckDropDistance);
            }

            Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
            Vector2 detectionCenter = (Vector2)transform.position + Vector2.right * direction * (detectionWidth * 0.5f);
            Gizmos.DrawWireCube(detectionCenter, new Vector3(detectionWidth, detectionHeight, 0f));
        }
    }
}
