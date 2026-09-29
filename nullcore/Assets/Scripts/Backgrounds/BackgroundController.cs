using UnityEngine;
using UnityEngine.U2D;

namespace Nullcore.Backgrounds
{
    [ExecuteAlways]
    [RequireComponent(typeof(SpriteShapeRenderer))]
    public class BackgroundController : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] private float shading = 1f;

        private SpriteShapeRenderer spriteShapeRenderer;

        private void Awake()
        {
            spriteShapeRenderer = GetComponent<SpriteShapeRenderer>();
            ApplyShading();
        }

        private void Update()
        {
            ApplyShading();
        }

        private void ApplyShading()
        {
            if (spriteShapeRenderer == null)
            {
                spriteShapeRenderer = GetComponent<SpriteShapeRenderer>();
            }
            if (spriteShapeRenderer == null)
            {
                return;
            }
            spriteShapeRenderer.color = new Color(shading, shading, shading, 1f);
        }
    }
}
