using UnityEngine;

namespace Nullcore.Player
{
    public class PlayerAbilities : MonoBehaviour
    {
        [SerializeField] private bool doubleJumpEnabled = true;
        [SerializeField] private bool dashEnabled = true;

        public bool DoubleJumpEnabled => doubleJumpEnabled;
        public bool DashEnabled => dashEnabled;

        public bool DoubleJumpLearned => AbilityProgress.DoubleJumpLearned;
        public bool DashLearned => AbilityProgress.DashLearned;
        public bool SprintLearned => AbilityProgress.SprintLearned;

        public void LearnDoubleJump()
        {
            AbilityProgress.LearnDoubleJump();
        }

        public void LearnDash()
        {
            AbilityProgress.LearnDash();
        }

        public void LearnSprint()
        {
            AbilityProgress.LearnSprint();
        }
    }
}
