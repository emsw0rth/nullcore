using UnityEngine;
using Nullcore.Player;

namespace Nullcore.Robots
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class LaserShotController : MonoBehaviour
    {
        [SerializeField] private float speed = 12f;
        [SerializeField] private LayerMask wallLayer;

        [Header("Visuals")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Sprite[] burstFrames;
        [SerializeField] private float burstFrameDuration = 0.05f;
        [SerializeField] private Sprite[] loopFrames;
        [SerializeField] private float loopFrameDuration = 0.08f;
        [SerializeField] private Transform lightsRoot;

        private Rigidbody2D rb;
        private Camera mainCamera;
        private int frameIndex;
        private float frameTimer;
        private bool burstComplete;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            mainCamera = Camera.main;

            if (spriteRenderer != null && burstFrames != null && burstFrames.Length > 0)
            {
                spriteRenderer.sprite = burstFrames[0];
            }
            frameTimer = burstFrameDuration;
        }

        public void Launch(Vector2 direction, bool facingRight)
        {
            rb.linearVelocity = direction.normalized * speed;
            if (spriteRenderer != null)
            {
                spriteRenderer.flipX = !facingRight;
            }
            if (lightsRoot != null)
            {
                // Lights are authored facing right; turn them around when fired left.
                lightsRoot.localRotation = Quaternion.Euler(0f, 0f, facingRight ? 0f : 180f);
            }
        }

        private void Update()
        {
            AdvanceAnimation();

            if (mainCamera != null && !IsVisibleFromCamera())
            {
                Destroy(gameObject);
            }
        }

        private void AdvanceAnimation()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            frameTimer -= Time.deltaTime;
            if (frameTimer > 0f)
            {
                return;
            }

            if (!burstComplete)
            {
                frameIndex++;
                if (burstFrames == null || frameIndex >= burstFrames.Length)
                {
                    burstComplete = true;
                    frameIndex = 0;
                    frameTimer = loopFrameDuration;
                    if (loopFrames != null && loopFrames.Length > 0)
                    {
                        spriteRenderer.sprite = loopFrames[0];
                    }
                }
                else
                {
                    spriteRenderer.sprite = burstFrames[frameIndex];
                    frameTimer = burstFrameDuration;
                }
            }
            else if (loopFrames != null && loopFrames.Length > 0)
            {
                frameIndex = (frameIndex + 1) % loopFrames.Length;
                spriteRenderer.sprite = loopFrames[frameIndex];
                frameTimer = loopFrameDuration;
            }
        }

        private bool IsVisibleFromCamera()
        {
            Vector3 viewportPos = mainCamera.WorldToViewportPoint(transform.position);
            return viewportPos.z > 0f && viewportPos.x >= -0.05f && viewportPos.x <= 1.05f && viewportPos.y >= -0.05f && viewportPos.y <= 1.05f;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                player.TakeDamage();
                Destroy(gameObject);
                return;
            }

            if (((1 << other.gameObject.layer) & wallLayer) != 0)
            {
                Destroy(gameObject);
            }
        }
    }
}
