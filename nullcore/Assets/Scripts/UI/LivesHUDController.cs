using UnityEngine;
using TMPro;
using Nullcore.Core;

namespace Nullcore.UI
{
    public class LivesHUDController : MonoBehaviour
    {
        [SerializeField] private TMP_Text livesText;

        private void OnEnable()
        {
            GameSession.LivesChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            GameSession.LivesChanged -= Refresh;
        }

        private void Refresh()
        {
            if (livesText != null)
            {
                livesText.text = ": " + GameSession.Lives;
            }
        }
    }
}
