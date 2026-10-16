using UnityEngine;
using Nullcore.Player;

namespace Nullcore.Hazards
{
    [RequireComponent(typeof(Collider2D))]
    public class SpikeHazard : MonoBehaviour
    {
        private void OnTriggerEnter2D(Collider2D other)
        {
            Hurt(other);
        }

        // Keeps hurting a player who stays on the spikes once their invulnerability has run out.
        private void OnTriggerStay2D(Collider2D other)
        {
            Hurt(other);
        }

        private static void Hurt(Collider2D other)
        {
            var player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                player.TakeDamage();
            }
        }
    }
}
