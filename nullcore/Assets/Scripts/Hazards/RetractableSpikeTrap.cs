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

        [Header("Extended spikes")]
        [Tooltip("Solid body that grows with the spikes, so anything standing on the trap is pushed up " +
                 "with them instead of sinking into the floor.")]
        [SerializeField] private BoxCollider2D spikeBody;
        [Tooltip("Kills the player while the spikes are out.")]
        [SerializeField] private BoxCollider2D killZone;
        [Tooltip("Local Y of the top of the flat, retracted trap (the solid floor the trap always keeps).")]
        [SerializeField] private float retractedTopY = -0.227f;
        [Tooltip("Local Y of the spike tips for each pop-out frame (same order as Pop Out Frames).")]
        [SerializeField] private float[] popOutTipY;
        [SerializeField] private float spikeWidth = 1f;
        [SerializeField] private float killZoneWidth = 0.9f;
        [Tooltip("How far the spike body / kill zone reach down into the floor, so there is no seam.")]
        [SerializeField] private float overlapIntoFloor = 0.05f;

        private SpriteRenderer spriteRenderer;
        private bool initialized;
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
            Initialize();
        }

        private void Initialize()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            initialized = true;
            EnterIdle();
        }

        public void SetActive(bool value)
        {
            Active = value;
        }

        private void Update()
        {
            Advance(Time.deltaTime);
        }

        // Steps the trap's state machine forward by the given time.
        public void Advance(float deltaTime)
        {
            if (!initialized)
            {
                Initialize();
            }

            if (!active)
            {
                if (state != State.Idle)
                {
                    EnterIdle();
                }
                return;
            }

            timer -= deltaTime;
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
            frameIndex = -1;
            spriteRenderer.sprite = retractedSprite;
            UpdateSpikeColliders();
            timer = idleTime;
        }

        private void EnterPoppingOut()
        {
            state = State.PoppingOut;
            frameIndex = 0;
            spriteRenderer.sprite = popOutFrames[frameIndex];
            UpdateSpikeColliders();
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
            UpdateSpikeColliders();
            timer = frameDuration;
        }

        private void EnterArmed()
        {
            state = State.Armed;
            frameIndex = popOutFrames.Length - 1;
            spriteRenderer.sprite = popOutFrames[frameIndex];
            UpdateSpikeColliders();
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
            UpdateSpikeColliders();
            timer = frameDuration;
        }

        // The trap's own collider is the solid floor and stays solid in every state, so nothing standing
        // on it can fall through. While the spikes are out, spikeBody grows up with the spike tips (which
        // lifts anything on top of the trap) and killZone covers the spikes.
        private void UpdateSpikeColliders()
        {
            bool extended = frameIndex >= 0 && popOutTipY != null && frameIndex < popOutTipY.Length;

            if (spikeBody != null)
            {
                spikeBody.enabled = extended;
                if (extended)
                {
                    SetBox(spikeBody, spikeWidth, popOutTipY[frameIndex]);
                }
            }

            if (killZone != null)
            {
                killZone.enabled = extended;
                if (extended)
                {
                    SetBox(killZone, killZoneWidth, popOutTipY[frameIndex]);
                }
            }
        }

        private void SetBox(BoxCollider2D box, float width, float tipY)
        {
            float bottom = retractedTopY - overlapIntoFloor;
            float height = Mathf.Max(0.01f, tipY - bottom);
            box.size = new Vector2(width, height);
            box.offset = new Vector2(0f, bottom + height * 0.5f);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            other.GetComponent<PlayerController>()?.TakeDamage();
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            other.GetComponent<PlayerController>()?.TakeDamage();
        }
    }
}
