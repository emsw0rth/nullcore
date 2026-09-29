using UnityEngine;

namespace Nullcore.Interactables
{
    [RequireComponent(typeof(Collider2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class DoorController : MonoBehaviour
    {
        [SerializeField] private Sprite closedSprite;
        [SerializeField] private Sprite openSprite;
        [SerializeField] private bool on = false;

        private SpriteRenderer spriteRenderer;
        private Collider2D col;

        public bool On
        {
            get => on;
            set
            {
                on = value;
                UpdateVisual();
            }
        }

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            col = GetComponent<Collider2D>();
            UpdateVisual();
        }

        private void OnValidate()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }
            if (col == null)
            {
                col = GetComponent<Collider2D>();
            }
            UpdateVisual();
        }

        public void SetOn(bool value)
        {
            On = value;
        }

        private void UpdateVisual()
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = on ? openSprite : closedSprite;
            }
            if (col != null)
            {
                // Open doors don't block movement; closed doors do.
                col.enabled = !on;
            }
        }
    }
}
