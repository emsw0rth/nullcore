using UnityEngine;

namespace Nullcore.Core
{
    public static class GameSession
    {
        public static string LevelName { get; private set; }
        public static float ElapsedTime { get; private set; }
        public static string NextLevelSceneName { get; private set; }

        public const int StartingLives = 3;
        public static int Lives { get; private set; } = StartingLives;
        public static event System.Action LivesChanged;

        private static float startTime;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ResetOnGameStart()
        {
            LevelName = string.Empty;
            ElapsedTime = 0f;
            NextLevelSceneName = string.Empty;
            startTime = 0f;
            Lives = StartingLives;
        }

        public static void ResetLives()
        {
            Lives = StartingLives;
            LivesChanged?.Invoke();
        }

        public static void GainLife()
        {
            Lives++;
            LivesChanged?.Invoke();
        }

        public static void LoseLife()
        {
            if (Lives <= 0)
            {
                return;
            }
            Lives--;
            LivesChanged?.Invoke();
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
