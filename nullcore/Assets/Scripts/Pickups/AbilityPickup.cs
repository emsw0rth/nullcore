using UnityEngine;
using Nullcore.Player;

namespace Nullcore.Pickups
{
    [RequireComponent(typeof(Collider2D))]
    public class AbilityPickup : MonoBehaviour
    {
        private enum AbilityType
        {
            DoubleJump,
            Dash,
            Sprint
        }

        [SerializeField] private AbilityType abilityType;

        private void OnTriggerEnter2D(Collider2D other)
        {
            var abilities = other.GetComponent<PlayerAbilities>();
            if (abilities == null)
            {
                return;
            }

            switch (abilityType)
            {
                case AbilityType.DoubleJump:
                    abilities.LearnDoubleJump();
                    break;
                case AbilityType.Dash:
                    abilities.LearnDash();
                    break;
                case AbilityType.Sprint:
                    abilities.LearnSprint();
                    break;
            }

            Destroy(gameObject);
        }
    }
}
