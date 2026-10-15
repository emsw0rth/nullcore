using UnityEngine;
using Nullcore.Player;

namespace Nullcore.Contraptions
{
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(BoxCollider2D))]
    public class SpringPlatformController : MonoBehaviour
    {
        private enum State
        {
            Armed,
            Springing,
            Retracting
        }

        [SerializeField] private bool on = true;
        [SerializeField] private float launchSpeed = 18f;
        [SerializeField] private float springFrameDuration = 0.02f;
        [SerializeField] private float retractSlowdown = 5f;
        [SerializeField] private int launchFrame = 5;

        [Header("Frames (0 = idle/armed, 1+ = spring animation)")]
        [SerializeField] private Sprite[] frames;
        [SerializeField] private float[] colliderTops;

        private SpriteRenderer spriteRenderer;
        private BoxCollider2D box;
        private float colliderBottom;
        private float colliderWidth;
        private float colliderOffsetX;

        private State state = State.Armed;
        private int frameIndex;
        private float frameTimer;
        private Rigidbody2D playerBody;
        private Collider2D playerCollider;
        private bool launched;

        public bool On => on;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            box = GetComponent<BoxCollider2D>();
            colliderBottom = box.offset.y - box.size.y * 0.5f;
            colliderWidth = box.size.x;
            colliderOffsetX = box.offset.x;
            ApplyFrame(0);
        }

        private void OnValidate()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }
            if (spriteRenderer != null && frames != null && frames.Length > 0)
            {
                spriteRenderer.sprite = frames[0];
            }
        }

        public void SetOn(bool value)
        {
            on = value;
            if (!on)
            {
                state = State.Armed;
                playerBody = null;
                playerCollider = null;
                ApplyFrame(0);
            }
        }

        private void Update()
        {
            if (frames == null || frames.Length < 2)
            {
                return;
            }

            switch (state)
            {
                case State.Springing:
                    frameTimer -= Time.deltaTime;
                    if (frameTimer > 0f)
                    {
                        return;
                    }
                    ApplyFrame(frameIndex + 1);
                    if (!launched && frameIndex >= launchFrame)
                    {
                        launched = true;
                        Launch();
                    }
                    if (frameIndex >= frames.Length - 1)
                    {
                        state = State.Retracting;
                        frameTimer = springFrameDuration * retractSlowdown;
                    }
                    else
                    {
                        frameTimer = springFrameDuration;
                    }
                    break;

                case State.Retracting:
                    frameTimer -= Time.deltaTime;
                    if (frameTimer > 0f)
                    {
                        return;
                    }
                    ApplyFrame(frameIndex - 1);
                    if (frameIndex <= 0)
                    {
                        state = State.Armed;
                    }
                    else
                    {
                        frameTimer = springFrameDuration * retractSlowdown;
                    }
                    break;
            }
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            if (!on || state != State.Armed)
            {
                return;
            }
            if (collision.collider.GetComponent<PlayerController>() == null)
            {
                return;
            }

            // Only trigger when the player is on top of the platform, not touching its side.
            float topY = box.bounds.max.y - 0.1f;
            foreach (var contact in collision.contacts)
            {
                if (contact.point.y >= topY)
                {
                    playerBody = collision.rigidbody;
                    playerCollider = collision.collider;
                    state = State.Springing;
                    launched = false;
                    frameTimer = springFrameDuration;
                    return;
                }
            }
        }

        private void Launch()
        {
            if (playerBody != null && playerCollider != null && box.IsTouching(playerCollider))
            {
                playerBody.linearVelocity = new Vector2(playerBody.linearVelocity.x, launchSpeed);
            }
            playerBody = null;
            playerCollider = null;
        }

        private void ApplyFrame(int index)
        {
            frameIndex = Mathf.Clamp(index, 0, frames != null ? frames.Length - 1 : 0);
            if (frames == null || frames.Length == 0)
            {
                return;
            }

            spriteRenderer.sprite = frames[frameIndex];

            if (colliderTops != null && frameIndex < colliderTops.Length)
            {
                float height = Mathf.Max(0.05f, colliderTops[frameIndex] - colliderBottom);
                box.size = new Vector2(colliderWidth, height);
                box.offset = new Vector2(colliderOffsetX, colliderBottom + height * 0.5f);
            }
        }
    }
}
