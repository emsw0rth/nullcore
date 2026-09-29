using UnityEngine;

namespace Nullcore.Core
{
    public static class GameSession
    {
        public static string LevelName { get; private set; }
        public static float ElapsedTime { get; private set; }
        public static string NextLevelSceneName { get; private set; }

        private static float startTime;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ResetOnGameStart()
        {
            LevelName = string.Empty;
            ElapsedTime = 0f;
            NextLevelSceneName = string.Empty;
            startTime = 0f;
        }

        public static void StartLevel(string levelName)
        {
            LevelName = levelName;
            startTime = Time.time;
        }

        public static float StopTimer()
        {
            ElapsedTime = Time.time - startTime;
            return ElapsedTime;
        }

        public static void SetNextLevel(string sceneName)
        {
            NextLevelSceneName = sceneName;
        }
    }
}
