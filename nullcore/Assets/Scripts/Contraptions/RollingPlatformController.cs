using UnityEngine;

namespace Nullcore.Contraptions
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class RollingPlatformController : MonoBehaviour
    {
        [SerializeField] private RotatingWheel[] wheels;
        [SerializeField] private float wheelRadius = 0.125f;

        private float lastX;

        private void Start()
        {
            lastX = transform.position.x;
        }

        // The platform itself is pushed around by ordinary physics; this only keeps the wheels
        // visually in step with however far it actually moved (rolling without slipping).
        private void LateUpdate()
        {
            float x = transform.position.x;
            float dx = x - lastX;
            lastX = x;

            if (dx == 0f || wheels == null || wheelRadius <= 0f)
            {
                return;
            }

            float degrees = -dx / wheelRadius * Mathf.Rad2Deg;
            foreach (var wheel in wheels)
            {
                if (wheel != null)
                {
                    wheel.Rotate(degrees);
                }
            }
        }
    }
}
