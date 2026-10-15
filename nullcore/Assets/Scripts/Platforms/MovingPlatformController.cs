using System.Collections.Generic;
using UnityEngine;
using Nullcore.Player;

namespace Nullcore.Platforms
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class MovingPlatformController : MonoBehaviour
    {
        [SerializeField] private Transform pointA;
        [SerializeField] private Transform pointB;
        [SerializeField] private float speed = 2f;
        [SerializeField] private bool active = true;

        [Tooltip("When on, this platform and all its child objects ignore everything except the player " +
                 "(boxes, NPCs, ground, etc. pass through it instead of being pushed or blocking it).")]
        [SerializeField] private bool collideOnlyWithPlayer;

        private readonly HashSet<Collider2D> ignored = new HashSet<Collider2D>();
        private readonly Collider2D[] overlapResults = new Collider2D[128];
        private Collider2D[] ownColliders;
        private int ticksSinceCleanup;

        private Rigidbody2D rb;
        private Vector2 worldPointA;
        private Vector2 worldPointB;
        private Vector2 currentTargetPos;
        private bool movingToB;
        private Vector2 frameDelta;

        public Vector2 FrameDelta => frameDelta;

        public void SetActive(bool value)
        {
            active = value;
        }

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.sleepMode = RigidbodySleepMode2D.NeverSleep;

            // Cache each point's world position once, before the platform starts moving.
            // Points are children of this (moving) transform, so continuing to read their
            // live .position at runtime would make the "target" chase the platform forever.
            if (pointA != null) worldPointA = pointA.position;
            if (pointB != null) worldPointB = pointB.position;
            movingToB = true;
            currentTargetPos = worldPointB;

            ownColliders = GetComponentsInChildren<Collider2D>(true);
        }

        private void FixedUpdate()
        {
            if (collideOnlyWithPlayer)
            {
                IgnoreNonPlayerColliders();
            }

            if (!active || pointA == null || pointB == null)
            {
                frameDelta = Vector2.zero;
                return;
            }

            Vector2 oldPos = rb.position;
            Vector2 newPos = Vector2.MoveTowards(oldPos, currentTargetPos, speed * Time.fixedDeltaTime);
            rb.MovePosition(newPos);
            frameDelta = newPos - oldPos;

            if (Vector2.Distance(newPos, currentTargetPos) < 0.01f)
            {
                movingToB = !movingToB;
                currentTargetPos = movingToB ? worldPointB : worldPointA;
            }
        }

        // Before the platform moves, find anything about to be in its way and tell the physics engine
        // to ignore collisions (and trigger events) between it and every collider of this platform.
        // Done a little ahead of time so nothing gets nudged on first contact.
        private void IgnoreNonPlayerColliders()
        {
            if (ownColliders == null)
            {
                return;
            }

            bool any = false;
            Bounds bounds = default;
            foreach (var own in ownColliders)
            {
                if (own == null || !own.enabled)
                {
                    continue;
                }
                if (!any) { bounds = own.bounds; any = true; }
                else bounds.Encapsulate(own.bounds);
            }
            if (!any)
            {
                return;
            }

            float margin = speed * Time.fixedDeltaTime * 2f + 0.5f;
            int count = gameObject.scene.GetPhysicsScene2D().OverlapBox(bounds.center, bounds.size + Vector3.one * (margin * 2f), 0f,
                new ContactFilter2D().NoFilter(), overlapResults);

            for (int i = 0; i < count; i++)
            {
                var other = overlapResults[i];
                if (other == null || ignored.Contains(other) || other.transform.IsChildOf(transform))
                {
                    continue;
                }
                if (other.GetComponentInParent<PlayerController>() != null)
                {
                    continue;
                }

                foreach (var own in ownColliders)
                {
                    if (own != null && own.enabled)
                    {
                        Physics2D.IgnoreCollision(own, other, true);
                    }
                }
                ignored.Add(other);
            }

            if (++ticksSinceCleanup > 250)
            {
                ticksSinceCleanup = 0;
                ignored.RemoveWhere(c => c == null);
            }
        }

        private void OnDrawGizmos()
        {
            if (pointA == null || pointB == null)
            {
                return;
            }

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(pointA.position, pointB.position);
            Gizmos.DrawWireSphere(pointA.position, 0.15f);
            Gizmos.DrawWireSphere(pointB.position, 0.15f);
        }
    }
}
