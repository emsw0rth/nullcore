using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using Nullcore.Core;

namespace Nullcore.UI
{
    public class GameOverController : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private TextMeshProUGUI timeText;
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        private void Start()
        {
            if (levelText != null)
            {
                levelText.text = string.IsNullOrEmpty(GameSession.FinalLevelName)
                    ? "You didn't make it far"
                    : "You made it to " + GameSession.FinalLevelName;
            }

            if (timeText != null)
            {
                timeText.text = "Time played: " + FormatTime(GameSession.FinalPlayTime);
            }

            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.RemoveAllListeners();
                mainMenuButton.onClick.AddListener(OnMainMenuClicked);
            }
        }

        private void OnMainMenuClicked()
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }

        private static string FormatTime(float seconds)
        {
            int total = Mathf.FloorToInt(seconds);
            int hours = total / 3600;
            int minutes = (total % 3600) / 60;
            int secs = total % 60;
            return hours > 0
                ? string.Format("{0}:{1:00}:{2:00}", hours, minutes, secs)
                : string.Format("{0:00}:{1:00}", minutes, secs);
        }
    }
}
