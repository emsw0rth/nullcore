using UnityEngine;
using Nullcore.Player;

namespace Nullcore.Hazards
{
    [RequireComponent(typeof(Collider2D))]
    public class SawbladeHazard : MonoBehaviour
    {
        [SerializeField] private float rotationSpeed = 360f;

        private void Update()
        {
            transform.Rotate(0f, 0f, -rotationSpeed * Time.deltaTime);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            other.GetComponent<PlayerController>()?.Respawn();
        }
    }
}
