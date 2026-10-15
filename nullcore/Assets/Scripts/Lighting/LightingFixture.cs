using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Nullcore.Backgrounds;

namespace Nullcore.Lighting
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class LightingFixture : MonoBehaviour
    {
        [SerializeField] private bool on;
        [SerializeField] private bool broken;
        [SerializeField] private Sprite offSprite;
        [SerializeField] private Sprite onSprite;
        [SerializeField] private Sprite brokenSprite;
        [SerializeField] private Light2D[] fixtureLights;

        private SpriteRenderer spriteRenderer;

        public bool On => on;
        public bool Broken => broken;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            ExcludeUnlitBackgroundLayer();
            UpdateSprite();
        }

        private void OnValidate()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }
            ExcludeUnlitBackgroundLayer();
            UpdateSprite();
        }

        // Backgrounds that aren't "Lightable" sit on their own sorting layer; fixture lights (of any
        // type) must never target it, so those backgrounds only receive the global ambient light.
        private void ExcludeUnlitBackgroundLayer()
        {
            if (fixtureLights == null)
            {
                return;
            }

            int unlitId = SortingLayer.NameToID(BackgroundController.UnlitSortingLayerName);
            if (unlitId == 0 || !SortingLayer.IsValid(unlitId))
            {
                return;
            }

            foreach (var light in fixtureLights)
            {
                if (light == null)
                {
                    continue;
                }

                var targets = light.targetSortingLayers;
                if (System.Array.IndexOf(targets, unlitId) < 0)
                {
                    continue;
                }

                var filtered = new List<int>(targets);
                filtered.Remove(unlitId);
                light.targetSortingLayers = filtered.ToArray();
            }
        }

        // A broken fixture ignores on/off requests, so it stays in its broken state.
        public void SetOn(bool value)
        {
            if (broken)
            {
                return;
            }
            on = value;
            UpdateSprite();
        }

        public void SetBroken(bool value)
        {
            broken = value;
            UpdateSprite();
        }

        private void UpdateSprite()
        {
            if (spriteRenderer == null)
            {
                return;
            }
            spriteRenderer.sprite = broken ? brokenSprite : (on ? onSprite : offSprite);

            if (fixtureLights != null)
            {
                bool lit = on && !broken;
                foreach (var light in fixtureLights)
                {
                    if (light != null)
                    {
                        light.enabled = lit;
                    }
                }
            }
        }
    }
}
