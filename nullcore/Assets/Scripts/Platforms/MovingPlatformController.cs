using UnityEngine;

namespace Nullcore.Platforms
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class MovingPlatformController : MonoBehaviour
    {
        [SerializeField] private Transform pointA;
        [SerializeField] private Transform pointB;
        [SerializeField] private float speed = 2f;
        [SerializeField] private bool active = true;

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
        }

        private void FixedUpdate()
        {
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
