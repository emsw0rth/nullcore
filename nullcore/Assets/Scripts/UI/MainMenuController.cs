using UnityEngine;
using UnityEngine.SceneManagement;

namespace Nullcore.UI
{
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject optionsPanel;
        [SerializeField] private string firstLevelSceneName = "Level_1";

        public void OnStartClicked()
        {
            SceneManager.LoadScene(firstLevelSceneName);
        }

        public void OnOptionsClicked()
        {
            mainMenuPanel.SetActive(false);
            optionsPanel.SetActive(true);
        }

        public void OnBackClicked()
        {
            optionsPanel.SetActive(false);
            mainMenuPanel.SetActive(true);
        }
    }
}
