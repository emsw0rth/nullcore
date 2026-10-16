using UnityEngine;
using TMPro;
using Nullcore.Core;

namespace Nullcore.UI
{
    public class ExtraLivesHUDController : MonoBehaviour
    {
        [SerializeField] private TMP_Text livesText;

        private void OnEnable()
        {
            GameSession.ExtraLivesChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            GameSession.ExtraLivesChanged -= Refresh;
        }

        private void Refresh()
        {
            if (livesText != null)
            {
                livesText.text = ": " + GameSession.ExtraLives;
            }
        }
    }
}
