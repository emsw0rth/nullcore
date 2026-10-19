using UnityEngine;

namespace Nullcore.Robots
{
    // The spider boss's bomb. It leaves the cannon at a set speed and angle, then gravity bends its path
    // into an arc. The nose follows the direction of travel, so a bomb launched tip-up at 45 degrees
    // pitches over and ends up diving at the ground, like a nose-heavy shell. It explodes on anything solid.
    [RequireComponent(typeof(Rigidbody2D))]
    public class SpiderProjectileController : MonoBehaviour
    {
        [Tooltip("Speed it leaves the cannon at (units per second). The spider can override this per shot.")]
        [SerializeField] private float launchSpeed = 10f;

        [Tooltip("How strongly gravity pulls it. Higher = a steeper, shorter arc.")]
        [SerializeField] private float gravityScale = 1f;

        [Tooltip("How fast the nose swings round to follow the flight path (degrees per second). " +
                 "Lower = the nose lags behind and the bomb looks heavier.")]
        [SerializeField] private float noseTurnSpeed = 270f;

        [SerializeField] private GameObject explosionPrefab;
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Tooltip("Safety net: the bomb removes itself after this long if it never hits anything.")]
        [SerializeField] private float maxLifetime = 10f;

        private Rigidbody2D rb;
        private float angle;
        private float age;
        private bool exploded;

        public float LaunchSpeed => launchSpeed;

        private void Awake()
        {
            EnsureInitialized();
        }

        private void EnsureInitialized()
        {
            if (rb != null)
            {
                return;
            }
            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = gravityScale;
            rb.freezeRotation = true;
        }

        public void Launch(Vector2 direction)
        {
            Launch(direction, launchSpeed);
        }

        public void Launch(Vector2 direction, float speed)
        {
            EnsureInitialized();
            direction.Normalize();
            rb.linearVelocity = direction * speed;
            angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            ApplyPose(direction.x);
        }

        private void FixedUpdate()
        {
            Vector2 velocity = rb.linearVelocity;
            if (velocity.sqrMagnitude > 0.01f)
            {
                float travelAngle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
                angle = Mathf.MoveTowardsAngle(angle, travelAngle, noseTurnSpeed * Time.fixedDeltaTime);
                ApplyPose(velocity.x);
            }

            age += Time.fixedDeltaTime;
            if (age >= maxLifetime)
            {
                Destroy(gameObject);
            }
        }

        // The art points right. When the bomb is heading left it is flipped over so it doesn't fly upside down.
        private void ApplyPose(float horizontalVelocity)
        {
            rb.rotation = angle;
            if (spriteRenderer != null)
            {
                spriteRenderer.flipY = horizontalVelocity < 0f;
            }
        }

        // Triggers (pickups, detection zones) don't raise this, so only solid things detonate the bomb.
        private void OnCollisionEnter2D(Collision2D collision)
        {
            Vector2 point = collision.contactCount > 0 ? collision.GetContact(0).point : (Vector2)transform.position;
            Explode(point);
        }

        // Spawns the explosion and removes the bomb.
        public void Explode(Vector2 point)
        {
            if (exploded)
            {
                return;
            }
            exploded = true;

            if (explosionPrefab != null)
            {
                Instantiate(explosionPrefab, point, Quaternion.identity);
            }
            Destroy(gameObject);
        }
    }
}
