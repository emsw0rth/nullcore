using UnityEngine;
using Nullcore.Player;

namespace Nullcore.Robots
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class RobotMiniDriverController : MonoBehaviour
    {
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

        // The true half-width of the body, read from its actual collider bounds when available so the
        // sensors track however the robot has been scaled rather than a stale hand-tuned constant
        // (bodyHalfWidth is kept as a manual fallback for instances with no bodyCollider assigned).
        private float BodyHalfWidth => bodyCollider != null ? bodyCollider.bounds.extents.x : bodyHalfWidth;

        [Header("Visuals")]
        [SerializeField] private SpriteRenderer bodyRenderer;
        [SerializeField] private SpriteRenderer[] wheelRenderers;
        [SerializeField] private Transform[] wheels;
        [SerializeField] private float wheelDegreesPerSecondAtSpeedOne = 360f;

        private Rigidbody2D rb;
        private bool facingRight;
        private bool isGrounded;

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

            if (ShouldTurnAround())
            {
                facingRight = !facingRight;
                UpdateFacingVisuals();
            }

            float direction = facingRight ? 1f : -1f;
            rb.linearVelocity = new Vector2(direction * speed, rb.linearVelocity.y);

            RotateWheels(direction);
        }

        private bool ShouldTurnAround()
        {
            float direction = facingRight ? 1f : -1f;
            float halfWidth = BodyHalfWidth;
            Vector2 wallOrigin = rb.position + Vector2.right * direction * halfWidth;

            // Obstacles (e.g. a closed door) aren't necessarily on the Ground layer, so this checks
            // a separate, broader mask than the ground/edge checks below. Triggers (ability pickups,
            // switch/teleporter detection zones, etc.) must be excluded explicitly - Physics2D.Raycast
            // hits them by default, which would otherwise register as a false "wall ahead".
            if (Cast(wallOrigin, Vector2.right * direction, wallCheckDistance, obstacleLayer))
            {
                return true;
            }

            // Cast from foot height (not body center) so the ray actually reaches the ground plane.
            Vector2 edgeOrigin = new Vector2(groundCheck.position.x + direction * halfWidth, groundCheck.position.y);
            return !Cast(edgeOrigin, Vector2.down, edgeCheckDropDistance, groundLayer);
        }

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
            if (wheelRenderers != null)
            {
                foreach (var wheel in wheelRenderers)
                {
                    if (wheel != null)
                    {
                        wheel.flipX = !facingRight;
                    }
                }
            }
        }

        private void RotateWheels(float direction)
        {
            if (wheels == null)
            {
                return;
            }

            float degrees = -direction * speed * wheelDegreesPerSecondAtSpeedOne * Time.fixedDeltaTime;
            foreach (var wheel in wheels)
            {
                if (wheel != null)
                {
                    wheel.Rotate(0f, 0f, degrees);
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            other.GetComponent<PlayerController>()?.TakeDamage();
        }

        // Keeps hurting a player who stays in contact once their invulnerability has run out.
        private void OnTriggerStay2D(Collider2D other)
        {
            other.GetComponent<PlayerController>()?.TakeDamage();
        }

        private void OnDrawGizmosSelected()
        {
            if (groundCheck != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
            }

            float direction = facingRight ? 1f : -1f;
            float halfWidth = BodyHalfWidth;
            Vector2 wallOrigin = (Vector2)transform.position + Vector2.right * direction * halfWidth;
            Gizmos.color = Color.red;
            Gizmos.DrawLine(wallOrigin, wallOrigin + Vector2.right * direction * wallCheckDistance);

            if (groundCheck != null)
            {
                Vector2 edgeOrigin = new Vector2(groundCheck.position.x + direction * halfWidth, groundCheck.position.y);
                Gizmos.DrawLine(edgeOrigin, edgeOrigin + Vector2.down * edgeCheckDropDistance);
            }
        }
    }
}
