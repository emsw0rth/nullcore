using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using Nullcore.InputSystem;
using Nullcore.Player;

namespace Nullcore.Interactables
{
    [RequireComponent(typeof(Collider2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(AudioSource))]
    public class SwitchController : MonoBehaviour
    {
        [System.Serializable]
        public class BoolEvent : UnityEvent<bool> { }

        [SerializeField] private Sprite offSprite;
        [SerializeField] private Sprite onSprite;
        [SerializeField] private bool on = false;
        [SerializeField] private BoolEvent onToggled;
        [SerializeField] private AudioClip onSound;
        [SerializeField] private AudioClip offSound;

        private SpriteRenderer spriteRenderer;
        private AudioSource audioSource;
        private bool playerInRange;

        public BoolEvent OnToggled => onToggled;

        public bool On
        {
            get => on;
            set
            {
                if (on == value)
                {
                    return;
                }
                on = value;
                UpdateSprite();
                PlayToggleSound();
                onToggled?.Invoke(on);
            }
        }

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            audioSource = GetComponent<AudioSource>();
            UpdateSprite();
        }

        private void OnValidate()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }
            UpdateSprite();
        }

        private void Update()
        {
            if (!playerInRange)
            {
                return;
            }

            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (KeybindManager.WasPressedThisFrame(keyboard, KeybindManager.Use))
            {
                On = !On;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponent<PlayerController>() != null)
            {
                playerInRange = true;
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.GetComponent<PlayerController>() != null)
            {
                playerInRange = false;
            }
        }

        private void UpdateSprite()
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = on ? onSprite : offSprite;
            }
        }

        private void PlayToggleSound()
        {
            if (audioSource == null)
            {
                return;
            }
            AudioClip clip = on ? onSound : offSound;
            if (clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }
    }
}
