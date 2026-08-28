using UnityEngine;

namespace Nullcore.Player
{
    public static class AbilityProgress
    {
        public static bool DoubleJumpLearned { get; private set; }
        public static bool DashLearned { get; private set; }
        public static bool SprintLearned { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ResetOnGameStart()
        {
            ResetAll();
        }

        public static void LearnDoubleJump()
        {
            DoubleJumpLearned = true;
        }

        public static void LearnDash()
        {
            DashLearned = true;
        }

        public static void LearnSprint()
        {
            SprintLearned = true;
        }

        public static void ResetAll()
        {
            DoubleJumpLearned = false;
            DashLearned = false;
            SprintLearned = false;
        }
    }
}
