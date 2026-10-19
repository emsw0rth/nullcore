using UnityEngine;
using UnityEngine.SceneManagement;
using Nullcore.Player;

namespace Nullcore.Core
{
    public static class GameSession
    {
        public static string LevelName { get; private set; }
        public static float ElapsedTime { get; private set; }
        public static string NextLevelSceneName { get; private set; }

        // Extra lives: spent when the player dies (all life points exhausted).
        public const int StartingExtraLives = 3;
        public static int ExtraLives { get; private set; } = StartingExtraLives;
        public static event System.Action ExtraLivesChanged;

        // Life points: the player's health. Each hit costs one; when none are left the player dies.
        // The maximum can grow during a run, and exhausted points can be recharged.
        public const int StartingMaxLifePoints = 3;
        public static int MaxLifePoints { get; private set; } = StartingMaxLifePoints;
        public static int LifePoints { get; private set; } = StartingMaxLifePoints;
        public static event System.Action LifePointsChanged;

        // Shown on the game over screen: total time spent playing this run, and the level the player was on.
        public static float FinalPlayTime { get; private set; }
        public static string FinalLevelName { get; private set; } = string.Empty;
        public const string GameOverSceneName = "GameOver";

        private static float startTime;
        private static bool keepTimerOnNextStart;
        private static float completedLevelsTime;
        private static bool levelTimerRunning;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ResetOnGameStart()
        {
            LevelName = string.Empty;
            ElapsedTime = 0f;
            NextLevelSceneName = string.Empty;
            startTime = 0f;
            keepTimerOnNextStart = false;
            completedLevelsTime = 0f;
            levelTimerRunning = false;
            FinalPlayTime = 0f;
            FinalLevelName = string.Empty;
            ExtraLives = StartingExtraLives;
            MaxLifePoints = StartingMaxLifePoints;
            LifePoints = StartingMaxLifePoints;
        }

        public static void ResetExtraLives()
        {
            ExtraLives = StartingExtraLives;
            ExtraLivesChanged?.Invoke();
        }

        public static void GainExtraLife()
        {
            ExtraLives++;
            ExtraLivesChanged?.Invoke();
        }

        public static void LoseExtraLife()
        {
            if (ExtraLives <= 0)
            {
                return;
            }
            ExtraLives--;
            ExtraLivesChanged?.Invoke();
        }

        // Back to the starting maximum, fully charged (a new run).
        public static void ResetLifePoints()
        {
            MaxLifePoints = StartingMaxLifePoints;
            LifePoints = MaxLifePoints;
            LifePointsChanged?.Invoke();
        }

        // Spends one life point. Returns true when that left the player with none (i.e. they died).
        public static bool LoseLifePoint()
        {
            if (LifePoints > 0)
            {
                LifePoints--;
                LifePointsChanged?.Invoke();
            }
            return LifePoints <= 0;
        }

        // Recharges one exhausted life point. Returns false (and does nothing) if they are all charged.
        public static bool RestoreLifePoint()
        {
            if (LifePoints >= MaxLifePoints)
            {
                return false;
            }
            LifePoints++;
            LifePointsChanged?.Invoke();
            return true;
        }

        public static void RestoreAllLifePoints()
        {
            if (LifePoints == MaxLifePoints)
            {
                return;
            }
            LifePoints = MaxLifePoints;
            LifePointsChanged?.Invoke();
        }

        // Raises the maximum; the new life points start out charged.
        public static void IncreaseMaxLifePoints(int amount = 1)
        {
            if (amount <= 0)
            {
                return;
            }
            MaxLifePoints += amount;
            LifePoints += amount;
            LifePointsChanged?.Invoke();
        }

        public static void StartLevel(string levelName)
        {
            // Restarting the level after a death keeps the clock running, so the final time includes the retries.
            bool restartOfSameLevel = keepTimerOnNextStart && levelName == LevelName;
            keepTimerOnNextStart = false;

            LevelName = levelName;
            levelTimerRunning = true;
            if (!restartOfSameLevel)
            {
                startTime = Time.time;
            }
        }

        // Reloads the current level from scratch (used when the player dies). Extra lives, life points,
        // learned abilities and the level timer live in static state, so they carry over the reload.
        public static void RestartLevel()
        {
            keepTimerOnNextStart = true;
            if (!Application.isPlaying)
            {
                return;
            }

            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public static float StopTimer()
        {
            ElapsedTime = Time.time - startTime;

            // Each level's time is added to the run total once, when that level's clock stops.
            if (levelTimerRunning)
            {
                levelTimerRunning = false;
                completedLevelsTime += ElapsedTime;
            }
            return ElapsedTime;
        }

        // Begins a new run from the main menu: full extra lives and life points, no abilities, clocks at zero.
        public static void StartRun()
        {
            ResetExtraLives();
            ResetLifePoints();
            AbilityProgress.ResetAll();
            completedLevelsTime = 0f;
            levelTimerRunning = false;
            FinalPlayTime = 0f;
            FinalLevelName = string.Empty;
        }

        // The run is over: records how long the player played and how far they got, then shows the game over scene.
        public static void TriggerGameOver()
        {
            StopTimer();
            FinalPlayTime = completedLevelsTime;
            FinalLevelName = LevelName;

            if (Application.isPlaying)
            {
                SceneManager.LoadScene(GameOverSceneName);
            }
        }

        public static void SetNextLevel(string sceneName)
        {
            NextLevelSceneName = sceneName;
        }
    }
}
