using TMPro;
using UnityEngine;

namespace Nullcore.UI
{
    public class KeybindCell : MonoBehaviour
    {
        [SerializeField] private string action;
        [SerializeField] private int slot;
        [SerializeField] private TextMeshProUGUI label;

        public string Action => action;
        public int Slot => slot;
        public TextMeshProUGUI Label => label;

        public void Setup(string actionName, int slotIndex, TextMeshProUGUI labelRef)
        {
            action = actionName;
            slot = slotIndex;
            label = labelRef;
        }
    }
}
