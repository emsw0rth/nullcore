using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Nullcore.Robots
{
    // Plays a sprite sequence once and then removes itself. Used for one-off effects such as a cannon's
    // muzzle flash and an explosion. An optional light fades out over the same time.
    public class OneShotSpriteAnimation : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Sprite[] frames;
        [SerializeField] private float framesPerSecond = 24f;

        [Tooltip("Optional light that is bright at the start and fades to nothing as the animation ends.")]
        [SerializeField] private Light2D fadeLight;

        private float elapsed;
        private float lightStartIntensity;

        private void Awake()
        {
            if (fadeLight != null)
            {
                lightStartIntensity = fadeLight.intensity;
            }
        }

        private void Start()
        {
            ShowFrame(0);
        }

        private void Update()
        {
            Advance(Time.deltaTime);
        }

        public void Advance(float deltaTime)
        {
            if (frames == null || frames.Length == 0)
            {
                Destroy(gameObject);
                return;
            }

            elapsed += deltaTime;
            float duration = frames.Length / framesPerSecond;
            if (elapsed >= duration)
            {
                Destroy(gameObject);
                return;
            }

            ShowFrame(Mathf.Min(frames.Length - 1, Mathf.FloorToInt(elapsed * framesPerSecond)));

            if (fadeLight != null)
            {
                fadeLight.intensity = lightStartIntensity * (1f - elapsed / duration);
            }
        }

        private void ShowFrame(int index)
        {
            if (spriteRenderer != null && frames != null && index < frames.Length)
            {
                spriteRenderer.sprite = frames[index];
            }
        }
    }
}
