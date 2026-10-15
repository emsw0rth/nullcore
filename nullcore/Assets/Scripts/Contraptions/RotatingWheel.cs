using UnityEngine;

namespace Nullcore.Contraptions
{
    public class RotatingWheel : MonoBehaviour
    {
        // Constant spin in degrees per second (positive = counter-clockwise). 0 means it only
        // rotates when Rotate() is called, e.g. by a rolling platform.
        [SerializeField] private float spinSpeed;

        public void Rotate(float degrees)
        {
            transform.Rotate(0f, 0f, degrees);
        }

        private void Update()
        {
            if (spinSpeed != 0f)
            {
                Rotate(spinSpeed * Time.deltaTime);
            }
        }
    }
}
