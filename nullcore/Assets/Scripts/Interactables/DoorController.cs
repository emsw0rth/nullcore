using UnityEngine;

namespace Nullcore.Interactables
{
    [RequireComponent(typeof(Collider2D))]
    public class DoorController : MonoBehaviour
    {
        [Tooltip("The laser barrier. Shown while the door is closed, hidden while it is open.")]
        [SerializeField] private GameObject laserObject;
        [SerializeField] private bool on = false;

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
            col = GetComponent<Collider2D>();
            UpdateVisual();
        }

        private void OnValidate()
        {
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
            if (laserObject != null)
            {
                laserObject.SetActive(!on);
            }
            if (col != null)
            {
                // Open doors don't block movement; closed doors do.
                col.enabled = !on;
            }
        }
    }
}
