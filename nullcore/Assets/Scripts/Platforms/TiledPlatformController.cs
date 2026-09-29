using UnityEngine;

namespace Nullcore.Platforms
{
    [ExecuteAlways]
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(BoxCollider2D))]
    public class TiledPlatformController : MonoBehaviour
    {
        private SpriteRenderer spriteRenderer;
        private BoxCollider2D boxCollider;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            boxCollider = GetComponent<BoxCollider2D>();
            SyncCollider();
        }

        private void Update()
        {
            // The Tiled-mode size handles on SpriteRenderer are dragged directly in the
            // Scene view; that only changes SpriteRenderer's own serialized data, so
            // OnValidate here would never fire for it. Poll instead - cheap, and works
            // identically in Edit mode (thanks to [ExecuteAlways]) and Play mode.
            SyncCollider();
        }

        private void SyncCollider()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }
            if (boxCollider == null)
            {
                boxCollider = GetComponent<BoxCollider2D>();
            }
            if (spriteRenderer == null || boxCollider == null)
            {
                return;
            }
            boxCollider.size = spriteRenderer.size;
        }
    }
}
