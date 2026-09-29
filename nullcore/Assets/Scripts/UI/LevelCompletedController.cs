using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using Nullcore.Core;

namespace Nullcore.UI
{
    public class LevelCompletedController : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private TextMeshProUGUI timeText;
        [SerializeField] private Button nextLevelButton;

        private void Start()
        {
            if (messageText != null)
            {
                messageText.text = GameSession.LevelName + " completed!";
            }

            if (timeText != null)
            {
                timeText.text = FormatTime(GameSession.ElapsedTime);
            }

            if (nextLevelButton != null)
            {
                nextLevelButton.onClick.RemoveAllListeners();
                nextLevelButton.onClick.AddListener(OnNextLevelClicked);
            }
        }

        private void OnNextLevelClicked()
        {
            if (!string.IsNullOrEmpty(GameSession.NextLevelSceneName))
            {
                SceneManager.LoadScene(GameSession.NextLevelSceneName);
            }
        }

        private static string FormatTime(float seconds)
        {
            int minutes = Mathf.FloorToInt(seconds / 60f);
            float remainingSeconds = seconds - minutes * 60f;
            return string.Format("{0:00}:{1:00.00}", minutes, remainingSeconds);
        }
    }
}
