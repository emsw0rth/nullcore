using UnityEngine;
using UnityEngine.U2D;

namespace Nullcore.Backgrounds
{
    [ExecuteAlways]
    [RequireComponent(typeof(SpriteShapeRenderer))]
    public class BackgroundController : MonoBehaviour
    {
        // Fixture lights only target the Default sorting layer, so a background moved to this layer
        // still gets the global ambient light but is not lit up by individual lamps.
        public const string UnlitSortingLayerName = "BackgroundNoFixtureLight";
        private const string LitSortingLayerName = "Default";

        [SerializeField, Range(0f, 1f)] private float shading = 1f;
        [SerializeField] private bool lightable = true;

        private SpriteShapeRenderer spriteShapeRenderer;

        private void Awake()
        {
            spriteShapeRenderer = GetComponent<SpriteShapeRenderer>();
            ApplyShading();
        }

        private void OnValidate()
        {
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

            int layerId = SortingLayer.NameToID(lightable ? LitSortingLayerName : UnlitSortingLayerName);
            if (spriteShapeRenderer.sortingLayerID != layerId)
            {
                spriteShapeRenderer.sortingLayerID = layerId;
            }
        }
    }
}
