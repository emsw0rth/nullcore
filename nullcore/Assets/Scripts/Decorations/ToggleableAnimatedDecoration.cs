using UnityEngine;

namespace Nullcore.Decorations
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class ToggleableAnimatedDecoration : MonoBehaviour
    {
        [SerializeField] private bool on = true;
        [SerializeField] private float frameDuration = 0.08f;
        [SerializeField] private Sprite[] animationFrames;

        private SpriteRenderer spriteRenderer;
        private int frameIndex;
        private float frameTimer;

        public bool On
        {
            get => on;
            set
            {
                on = value;
                if (!on)
                {
                    ShowIdleFrame();
                }
            }
        }

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (!on)
            {
                ShowIdleFrame();
            }
        }

        public void SetOn(bool value)
        {
            On = value;
        }

        private void Update()
        {
            if (!on || animationFrames == null || animationFrames.Length == 0)
            {
                return;
            }

            frameTimer -= Time.deltaTime;
            if (frameTimer > 0f)
            {
                return;
            }

            frameIndex = (frameIndex + 1) % animationFrames.Length;
            spriteRenderer.sprite = animationFrames[frameIndex];
            frameTimer = frameDuration;
        }

        private void ShowIdleFrame()
        {
            frameIndex = 0;
            frameTimer = 0f;
            if (animationFrames != null && animationFrames.Length > 0)
            {
                spriteRenderer.sprite = animationFrames[0];
            }
        }
    }
}
