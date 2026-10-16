using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Nullcore.Core;

namespace Nullcore.UI
{
    // Draws the player's life points as a battery: a left cap, one cell per maximum life point
    // (charged = available, exhausted = spent) and a right cap. The cells are rebuilt whenever the
    // maximum changes, so the battery grows as the player gains more life points.
    [RequireComponent(typeof(RectTransform))]
    public class LifePointsHUDController : MonoBehaviour
    {
        [SerializeField] private Sprite leftSprite;
        [SerializeField] private Sprite chargedSprite;
        [SerializeField] private Sprite exhaustedSprite;
        [SerializeField] private Sprite rightSprite;
        [Tooltip("On-screen size of one source pixel, in canvas units.")]
        [SerializeField] private float pixelScale = 1.5f;

        private readonly List<Image> cells = new List<Image>();
        private int builtMax = -1;

        private void OnEnable()
        {
            GameSession.LifePointsChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            GameSession.LifePointsChanged -= Refresh;
        }

        // Also used by the editor to bake a preview of the battery into the prefab.
        public void Rebuild(int maxPoints, int points)
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                DestroyChild(transform.GetChild(i).gameObject);
            }
            cells.Clear();

            CreatePiece("Left", leftSprite);
            for (int i = 0; i < maxPoints; i++)
            {
                cells.Add(CreatePiece("Cell " + (i + 1), chargedSprite));
            }
            CreatePiece("Right", rightSprite);

            builtMax = maxPoints;
            ApplyCharge(points);
        }

        private void Refresh()
        {
            if (builtMax != GameSession.MaxLifePoints)
            {
                Rebuild(GameSession.MaxLifePoints, GameSession.LifePoints);
                return;
            }
            ApplyCharge(GameSession.LifePoints);
        }

        private void ApplyCharge(int points)
        {
            for (int i = 0; i < cells.Count; i++)
            {
                cells[i].sprite = i < points ? chargedSprite : exhaustedSprite;
            }
        }

        private Image CreatePiece(string pieceName, Sprite sprite)
        {
            var go = new GameObject(pieceName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement));
            go.transform.SetParent(transform, false);

            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;

            var layout = go.GetComponent<LayoutElement>();
            if (sprite != null)
            {
                layout.preferredWidth = sprite.rect.width * pixelScale;
                layout.preferredHeight = sprite.rect.height * pixelScale;
            }
            return image;
        }

        private static void DestroyChild(GameObject child)
        {
            if (Application.isPlaying)
            {
                Destroy(child);
            }
            else
            {
                DestroyImmediate(child);
            }
        }
    }
}
