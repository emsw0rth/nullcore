using UnityEngine;

namespace Nullcore.Teleporters
{
    public class TeleporterController : MonoBehaviour
    {
        [SerializeField] private GameObject animationObject;
        [SerializeField] private bool on = false;
        [SerializeField] private string nextLevelSceneName;

        public string NextLevelSceneName => nextLevelSceneName;

        public bool On
        {
            get => on;
            set
            {
                on = value;
                UpdateVisibility();
            }
        }

        private void Awake()
        {
            UpdateVisibility();
        }

        public void SetOn(bool value)
        {
            On = value;
        }

        private void OnValidate()
        {
            UpdateVisibility();
        }

        private void UpdateVisibility()
        {
            if (animationObject != null)
            {
                animationObject.SetActive(on);
            }
        }
    }
}
