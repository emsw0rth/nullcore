using UnityEngine;
using Nullcore.Player;

namespace Nullcore.Robots
{
    // Hurts the player once, the moment this object appears, if they are within `radius` of it.
    public class ExplosionDamage : MonoBehaviour
    {
        [SerializeField] private float radius = 1.5f;

        private void Start()
        {
            foreach (var hit in Physics2D.OverlapCircleAll(transform.position, radius))
            {
                if (hit.isTrigger)
                {
                    continue;
                }

                var player = hit.GetComponentInParent<PlayerController>();
                if (player != null)
                {
                    player.TakeDamage();
                    return;
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.4f, 0f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
