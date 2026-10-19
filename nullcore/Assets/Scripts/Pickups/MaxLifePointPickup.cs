using UnityEngine;
using Nullcore.Core;
using Nullcore.Player;

namespace Nullcore.Pickups
{
    // The heart with a plus: permanently raises the player's maximum life points and fully recharges
    // every life point, old and new.
    [RequireComponent(typeof(Collider2D))]
    public class MaxLifePointPickup : MonoBehaviour
    {
        [Tooltip("How many life points to add to the maximum.")]
        [SerializeField, Min(1)] private int amount = 1;

        private bool collected;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (collected || other.GetComponent<PlayerController>() == null)
            {
                return;
            }

            collected = true;
            GameSession.IncreaseMaxLifePoints(amount);
            GameSession.RestoreAllLifePoints();
            Destroy(gameObject);
        }
    }
}
