using UnityEngine;
using Nullcore.Player;

namespace Nullcore.Robots
{
    // The spider boss. Stands idle until the player comes close, then repeats: walk around for a while ->
    // hunker down (arming) -> lob bombs from the left and right cannons in turn -> stand back up -> walk.
    [RequireComponent(typeof(Rigidbody2D))]
    public class SpiderBossController : MonoBehaviour
    {
        private enum State
        {
            Dormant,
            Walking,
            Arming,
            Shooting,
            Disarming
        }

        [Header("Movement")]
        [SerializeField] private float speed = 1.5f;
        [SerializeField] private bool startFacingRight = true;
        [Tooltip("The boss stays idle until the player is this close (horizontally). 0 = awake from the start.")]
        [SerializeField] private float wakeDistance = 16f;

        [Header("Attack cycle")]
        [SerializeField] private float minWalkTime = 3f;
        [SerializeField] private float maxWalkTime = 5f;
        [Tooltip("Shots from each cannon before it stands back up. Cannons alternate left, right, left, right...")]
        [SerializeField] private int shotsPerSide = 3;
        [SerializeField] private float gapBetweenShots = 0.2f;
        [Tooltip("Which frame of the shooting animation the bomb leaves the cannon on (0 = first frame).")]
        [SerializeField] private int fireFrame = 1;

        [Header("Projectile")]
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private GameObject firingEffectPrefab;
        [Tooltip("Speed the bomb leaves the cannon at (units per second).")]
        [SerializeField] private float projectileSpeed = 10f;
        [Tooltip("Launch angle above the horizontal. The left cannon fires up-left, the right cannon up-right.")]
        [SerializeField] private float launchAngle = 45f;
        [SerializeField] private Transform muzzleLeft;
        [SerializeField] private Transform muzzleRight;

        [Header("Sensors")]
        [SerializeField] private Transform groundCheck;
        [SerializeField] private float groundCheckRadius = 0.2f;
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private LayerMask obstacleLayer;
        [SerializeField] private Collider2D bodyCollider;
        [SerializeField] private float wallCheckDistance = 0.1f;
        [SerializeField] private float edgeCheckDropDistance = 0.5f;
        [Tooltip("How far in from the front edge of the body the ledge check looks, so the boss turns before its front feet leave the platform.")]
        [SerializeField] private float edgeInset = 0.3f;

        [Header("Visuals")]
        [SerializeField] private SpriteRenderer bodyRenderer;
        [SerializeField] private Sprite idleSprite;
        [Tooltip("The walk animation, facing right. Flipped when walking left.")]
        [SerializeField] private Sprite[] walkFrames;
        [Tooltip("Hunkering down. Played once and held on its last frame, and played backwards to stand up.")]
        [SerializeField] private Sprite[] armFrames;
        [SerializeField] private Sprite[] shootLeftFrames;
        [SerializeField] private Sprite[] shootRightFrames;
        [SerializeField] private float walkFps = 24f;
        [SerializeField] private float armFps = 24f;
        [SerializeField] private float shootFps = 18f;

        private Rigidbody2D rb;
        private Transform player;
        private State state = State.Dormant;
        private bool facingRight;
        private bool isGrounded;

        private float stateTime;
        private float walkTimer;
        private int shotIndex;
        private float shotTime;
        private float gapTimer;
        private bool shotFired;
        private float dt;

        public bool IsGrounded => isGrounded;
        public string StateName => state.ToString();

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            facingRight = startFacingRight;
            ShowIdle();
        }

        private void Update()
        {
            Advance(Time.deltaTime);
        }

        // Steps the boss's behaviour forward by the given time (called every frame by Update).
        public void Advance(float deltaTime)
        {
            dt = deltaTime;
            switch (state)
            {
                case State.Dormant:
                    if (PlayerInWakeRange())
                    {
                        EnterWalking();
                    }
                    break;
                case State.Walking:
                    UpdateWalking();
                    break;
                case State.Arming:
                    UpdateArming();
                    break;
                case State.Shooting:
                    UpdateShooting();
                    break;
                case State.Disarming:
                    UpdateDisarming();
                    break;
            }
        }

        private void FixedUpdate()
        {
            isGrounded = groundCheck != null && gameObject.scene.GetPhysicsScene2D().OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

            float vx = 0f;
            if (state == State.Walking && isGrounded)
            {
                if (ShouldTurnAround())
                {
                    facingRight = !facingRight;
                    UpdateFacingVisuals();
                }
                vx = (facingRight ? 1f : -1f) * speed;
            }
            rb.linearVelocity = new Vector2(vx, rb.linearVelocity.y);
        }

        // ---- states ----

        private void EnterWalking()
        {
            state = State.Walking;
            stateTime = 0f;
            walkTimer = Random.Range(minWalkTime, Mathf.Max(minWalkTime, maxWalkTime));
            UpdateFacingVisuals();
        }

        private void UpdateWalking()
        {
            stateTime += dt;
            SetFrame(walkFrames, Mathf.FloorToInt(stateTime * walkFps) % Mathf.Max(1, walkFrames.Length));

            walkTimer -= dt;
            if (walkTimer <= 0f && isGrounded)
            {
                EnterArming();
            }
        }

        private void EnterArming()
        {
            state = State.Arming;
            stateTime = 0f;
            SetFlip(false);
        }

        private void UpdateArming()
        {
            stateTime += dt;
            int frame = Mathf.FloorToInt(stateTime * armFps);
            SetFrame(armFrames, frame); // holds the last frame once the animation is over
            if (frame >= armFrames.Length)
            {
                EnterShooting();
            }
        }

        private void EnterShooting()
        {
            state = State.Shooting;
            shotIndex = 0;
            BeginShot();
        }

        private void BeginShot()
        {
            shotTime = 0f;
            gapTimer = 0f;
            shotFired = false;
        }

        private void UpdateShooting()
        {
            bool left = shotIndex % 2 == 0;
            Sprite[] frames = left ? shootLeftFrames : shootRightFrames;

            if (gapTimer > 0f)
            {
                gapTimer -= dt;
                if (gapTimer <= 0f)
                {
                    BeginShot();
                }
                return;
            }

            shotTime += dt;
            int frame = Mathf.FloorToInt(shotTime * shootFps);
            SetFrame(frames, frame);

            if (!shotFired && frame >= fireFrame)
            {
                shotFired = true;
                Fire(left);
            }

            if (frame >= frames.Length)
            {
                shotIndex++;
                if (shotIndex >= shotsPerSide * 2)
                {
                    EnterDisarming();
                }
                else
                {
                    // Rest in the armed pose between shots.
                    SetFrame(shotIndex % 2 == 0 ? shootLeftFrames : shootRightFrames, 0);
                    gapTimer = Mathf.Max(0.01f, gapBetweenShots);
                }
            }
        }

        private void EnterDisarming()
        {
            state = State.Disarming;
            stateTime = 0f;
        }

        private void UpdateDisarming()
        {
            stateTime += dt;
            int frame = Mathf.FloorToInt(stateTime * armFps);
            SetFrame(armFrames, armFrames.Length - 1 - frame); // the arming animation backwards
            if (frame >= armFrames.Length)
            {
                EnterWalking();
            }
        }

        // ---- shooting ----

        private void Fire(bool left)
        {
            Transform muzzle = left ? muzzleLeft : muzzleRight;
            if (muzzle == null)
            {
                return;
            }

            float radians = launchAngle * Mathf.Deg2Rad;
            Vector2 direction = new Vector2((left ? -1f : 1f) * Mathf.Cos(radians), Mathf.Sin(radians));

            if (projectilePrefab != null)
            {
                var bomb = Instantiate(projectilePrefab, muzzle.position, Quaternion.identity);
                var projectile = bomb.GetComponent<SpiderProjectileController>();
                if (projectile != null)
                {
                    projectile.Launch(direction, projectileSpeed);
                }
            }

            if (firingEffectPrefab != null)
            {
                // The flash art points up-right (45 degrees), so turn it to match the launch angle and mirror it for the left cannon.
                float turn = (launchAngle - 45f) * (left ? -1f : 1f);
                var effect = Instantiate(firingEffectPrefab, muzzle.position, Quaternion.Euler(0f, 0f, turn));
                if (left)
                {
                    foreach (var sr in effect.GetComponentsInChildren<SpriteRenderer>())
                    {
                        sr.flipX = true;
                    }
                }
            }
        }

        // ---- movement sensors ----

        private bool ShouldTurnAround()
        {
            float direction = facingRight ? 1f : -1f;

            if (bodyCollider != null && RobotSensors.BodyBlocked(bodyCollider, direction, wallCheckDistance, obstacleLayer))
            {
                return true;
            }

            float frontX = bodyCollider != null
                ? (direction > 0f ? bodyCollider.bounds.max.x : bodyCollider.bounds.min.x)
                : rb.position.x + direction * 1.5f;
            frontX -= direction * edgeInset;

            Vector2 edgeOrigin = new Vector2(frontX, groundCheck.position.y + 0.05f);
            return !RobotSensors.RayHitsSolid(gameObject.scene.GetPhysicsScene2D(), edgeOrigin, Vector2.down, edgeCheckDropDistance, groundLayer);
        }

        private bool PlayerInWakeRange()
        {
            if (wakeDistance <= 0f)
            {
                return true;
            }

            if (player == null)
            {
                var found = FindAnyObjectByType<PlayerController>();
                if (found != null)
                {
                    player = found.transform;
                }
            }
            return player != null && Mathf.Abs(player.position.x - transform.position.x) <= wakeDistance;
        }

        // ---- visuals ----

        private void ShowIdle()
        {
            if (bodyRenderer != null && idleSprite != null)
            {
                bodyRenderer.sprite = idleSprite;
            }
            SetFlip(false);
        }

        private void UpdateFacingVisuals()
        {
            // The walk animation faces right, so it is mirrored to walk left.
            SetFlip(!facingRight);
        }

        private void SetFlip(bool flipped)
        {
            if (bodyRenderer != null)
            {
                bodyRenderer.flipX = flipped;
            }
        }

        private void SetFrame(Sprite[] frames, int index)
        {
            if (bodyRenderer == null || frames == null || frames.Length == 0)
            {
                return;
            }
            bodyRenderer.sprite = frames[Mathf.Clamp(index, 0, frames.Length - 1)];
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
            if (muzzleLeft != null || muzzleRight != null)
            {
                Gizmos.color = Color.yellow;
                float radians = launchAngle * Mathf.Deg2Rad;
                if (muzzleLeft != null)
                {
                    Gizmos.DrawRay(muzzleLeft.position, new Vector2(-Mathf.Cos(radians), Mathf.Sin(radians)) * 2f);
                }
                if (muzzleRight != null)
                {
                    Gizmos.DrawRay(muzzleRight.position, new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * 2f);
                }
            }

            if (wakeDistance > 0f)
            {
                Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.4f);
                Gizmos.DrawLine(transform.position + Vector3.left * wakeDistance, transform.position + Vector3.right * wakeDistance);
            }
        }
    }
}
