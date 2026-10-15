using UnityEngine;

namespace Nullcore.Decorations
{
    public class Decoration : MonoBehaviour
    {
        [SerializeField] private bool collidable;

        [Tooltip("Only applies when Collidable. On = an immovable solid, like a platform: no gravity, " +
                 "can't be pushed, no friction. Off = a physics object that falls and can be pushed.")]
        [SerializeField] private bool isStatic;

        [Tooltip("Ground drag while collidable, as a friction coefficient: 0 = slides forever, " +
                 "higher = stops sooner. (The ground and player use a zero-friction material, so a " +
                 "collider material can't provide this.)")]
        [SerializeField, Min(0f)] private float friction = 0.5f;

        [Tooltip("Darkens the sprite(s): 1 = full brightness, 0 = black. Same as the background shading slider.")]
        [SerializeField, Range(0f, 1f)] private float shading = 1f;

        private readonly ContactPoint2D[] contacts = new ContactPoint2D[16];
        private Rigidbody2D body;

        public bool Collidable
        {
            get => collidable;
            set
            {
                collidable = value;
                Apply();
            }
        }

        public bool IsStatic
        {
            get => isStatic;
            set
            {
                isStatic = value;
                Apply();
            }
        }

        public float Friction
        {
            get => friction;
            set => friction = Mathf.Max(0f, value);
        }

        public float Shading
        {
            get => shading;
            set
            {
                shading = Mathf.Clamp01(value);
                ApplyShading();
            }
        }

        private void Awake()
        {
            Apply();
            ApplyShading();
        }

        private void OnValidate()
        {
            Apply();
            ApplyShading();
        }

        // Tints every sprite on the decoration (including child sprites) towards black, keeping alpha.
        private void ApplyShading()
        {
            foreach (var sr in GetComponentsInChildren<SpriteRenderer>(true))
            {
                var color = sr.color;
                if (!Mathf.Approximately(color.r, shading) || !Mathf.Approximately(color.g, shading) || !Mathf.Approximately(color.b, shading))
                {
                    sr.color = new Color(shading, shading, shading, color.a);
                }
            }
        }

        // Non-collidable decorations are pure background: no collider, no physics body, default layer.
        // Collidable ones become real physics objects (gravity, can be pushed) on the Ground layer, so
        // the player can stand on them and NPCs treat them like any other solid object.
        private void Apply()
        {
            foreach (var col in GetComponents<Collider2D>())
            {
                col.enabled = collidable;
            }

            body = GetComponent<Rigidbody2D>();
            if (body != null)
            {
                if (body.simulated != collidable)
                {
                    body.simulated = collidable;
                }

                var bodyType = collidable && isStatic ? RigidbodyType2D.Static : RigidbodyType2D.Dynamic;
                if (body.bodyType != bodyType)
                {
                    body.bodyType = bodyType;
                }
            }

            int groundLayer = LayerMask.NameToLayer("Ground");
            int layer = collidable && groundLayer >= 0 ? groundLayer : 0;
            if (gameObject.layer != layer)
            {
                gameObject.layer = layer;
            }
        }

        // Coulomb-style friction: while resting on something, slow horizontal motion (relative to
        // whatever it's resting on) by friction * gravity each second.
        private void FixedUpdate()
        {
            if (!collidable || friction <= 0f || body == null || !body.simulated || body.bodyType != RigidbodyType2D.Dynamic)
            {
                return;
            }

            float bottom = float.MaxValue;
            foreach (var col in GetComponents<Collider2D>())
            {
                if (col.enabled)
                {
                    bottom = Mathf.Min(bottom, col.bounds.min.y);
                }
            }

            int count = body.GetContacts(contacts);
            for (int i = 0; i < count; i++)
            {
                // Only contacts along the underside count as support (not walls, not things on top).
                if (contacts[i].point.y > bottom + 0.05f)
                {
                    continue;
                }

                var other = contacts[i].rigidbody == body ? contacts[i].otherRigidbody : contacts[i].rigidbody;
                float supportVelocityX = other != null && other.bodyType == RigidbodyType2D.Dynamic ? other.linearVelocity.x : 0f;

                float decel = friction * Mathf.Abs(Physics2D.gravity.y) * body.gravityScale * Time.fixedDeltaTime;
                float vx = Mathf.MoveTowards(body.linearVelocity.x, supportVelocityX, decel);
                body.linearVelocity = new Vector2(vx, body.linearVelocity.y);
                return;
            }
        }
    }
}
