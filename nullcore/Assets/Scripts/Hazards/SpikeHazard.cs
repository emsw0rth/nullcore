using UnityEngine;
using Nullcore.Player;

namespace Nullcore.Hazards
{
    [RequireComponent(typeof(Collider2D))]
    public class SpikeHazard : MonoBehaviour
    {
        private void OnTriggerEnter2D(Collider2D other)
        {
            var player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                player.Respawn();
            }
        }
    }
}
