using UnityEngine;
using Nullcore.Core;
using Nullcore.Player;

namespace Nullcore.Pickups
{
    // The electrical heart: recharges one exhausted life point when the player touches it.
    [RequireComponent(typeof(Collider2D))]
    public class LifePointPickup : MonoBehaviour
    {
        [Tooltip("If the player's life points are all charged already: off = leave the heart for later, " +
                 "on = pick it up anyway (and waste it).")]
        [SerializeField] private bool collectWhenFull = false;

        private bool collected;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (collected || other.GetComponent<PlayerController>() == null)
            {
                return;
            }

            if (!GameSession.RestoreLifePoint() && !collectWhenFull)
            {
                return;
            }

            collected = true;
            Destroy(gameObject);
        }
    }
}
