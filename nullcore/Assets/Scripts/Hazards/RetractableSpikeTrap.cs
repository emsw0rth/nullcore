using UnityEngine;
using Nullcore.Player;

namespace Nullcore.Hazards
{
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(Collider2D))]
    public class RetractableSpikeTrap : MonoBehaviour
    {
        private enum State
        {
            Idle,
            PoppingOut,
            Armed,
            Retracting
        }

        [SerializeField] private bool active = true;
        [SerializeField] private float idleTime = 2f;
        [SerializeField] private float armedTime = 2f;
        [SerializeField] private float frameDuration = 0.08f;
        [SerializeField] private Sprite retractedSprite;
        [SerializeField] private Sprite[] popOutFrames;

        private SpriteRenderer spriteRenderer;
        private Collider2D col;
        private State state;
        private float timer;
        private int frameIndex;

        public bool Active
        {
            get => active;
            set => active = value;
        }

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            col = GetComponent<Collider2D>();
            EnterIdle();
        }

        public void SetActive(bool value)
        {
            Active = value;
        }

        private void Update()
        {
            if (!active)
            {
                if (state != State.Idle)
                {
                    EnterIdle();
                }
                return;
            }

            timer -= Time.deltaTime;
            if (timer > 0f)
            {
                return;
            }

            switch (state)
            {
                case State.Idle:
                    EnterPoppingOut();
                    break;
                case State.PoppingOut:
                    AdvancePopFrame();
                    break;
                case State.Armed:
                    EnterRetracting();
                    break;
                case State.Retracting:
                    RetreatFrame();
                    break;
            }
        }

        private void EnterIdle()
        {
            state = State.Idle;
            col.isTrigger = false; // retracted and flat - safe to walk on
            frameIndex = -1;
            spriteRenderer.sprite = retractedSprite;
            timer = idleTime;
        }

        private void EnterPoppingOut()
        {
            state = State.PoppingOut;
            col.isTrigger = true; // spikes emerging - no longer solid ground
            frameIndex = 0;
            spriteRenderer.sprite = popOutFrames[frameIndex];
            timer = frameDuration;
        }

        private void AdvancePopFrame()
        {
            frameIndex++;
            if (frameIndex >= popOutFrames.Length)
            {
                EnterArmed();
                return;
            }
            spriteRenderer.sprite = popOutFrames[frameIndex];
            timer = frameDuration;
        }

        private void EnterArmed()
        {
            state = State.Armed;
            spriteRenderer.sprite = popOutFrames[popOutFrames.Length - 1];
            timer = armedTime;
        }

        private void EnterRetracting()
        {
            state = State.Retracting;
            frameIndex = popOutFrames.Length - 1;
            timer = frameDuration;
        }

        private void RetreatFrame()
        {
            frameIndex--;
            if (frameIndex < 0)
            {
                EnterIdle();
                return;
            }
            spriteRenderer.sprite = popOutFrames[frameIndex];
            timer = frameDuration;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            other.GetComponent<PlayerController>()?.Respawn();
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            other.GetComponent<PlayerController>()?.Respawn();
        }
    }
}
