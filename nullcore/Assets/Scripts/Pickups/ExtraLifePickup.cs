using UnityEngine;
using Nullcore.Core;
using Nullcore.Player;

namespace Nullcore.Pickups
{
    [RequireComponent(typeof(Collider2D))]
    public class ExtraLifePickup : MonoBehaviour
    {
        private bool collected;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (collected || other.GetComponent<PlayerController>() == null)
            {
                return;
            }

            collected = true;
            GameSession.GainExtraLife();
            Destroy(gameObject);
        }
    }
}
