using UnityEngine;

namespace Nullcore.Platforms
{
    // A container whose contents vanish while it is On and come back when it is Off. Put the platform
    // pieces (or anything else) inside it as children. Wire a switch's "On Toggled" event to Toggle.
    public class DisappearingPlatform : MonoBehaviour
    {
        [Tooltip("On = everything inside is gone. Off = everything inside is there.")]
        [SerializeField] private bool on;

        public bool On
        {
            get => on;
            set
            {
                on = value;
                Apply();
            }
        }

        private void Awake()
        {
            Apply();
        }

        // Flips the platform: gone becomes back, back becomes gone. Wire this to a switch's "On Toggled"
        // event and every flick of the switch flips the platform, whichever state it started in.
        public void Toggle()
        {
            On = !On;
        }

        // Keeps the editor preview in step with the checkbox. Deferred, because activating objects is
        // not allowed from inside OnValidate.
        private void OnValidate()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.delayCall += () =>
            {
                // Skip prefab assets in the Project window (no valid scene); only live objects in a scene
                // or in Prefab Mode should preview the state.
                if (this != null && !Application.isPlaying && gameObject.scene.IsValid())
                {
                    Apply();
                }
            };
#endif
        }

        // Switching a child off removes its sprites, colliders, lights and scripts in one go, so nothing
        // inside can be seen, stood on, or bumped into while the platform is "on".
        private void Apply()
        {
            foreach (Transform child in transform)
            {
                if (child.gameObject.activeSelf == on)
                {
                    child.gameObject.SetActive(!on);
                }
            }
        }
    }
}
