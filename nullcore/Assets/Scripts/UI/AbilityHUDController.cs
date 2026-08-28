using UnityEngine;
using UnityEngine.UI;
using Nullcore.Player;

namespace Nullcore.UI
{
    public class AbilityHUDController : MonoBehaviour
    {
        [SerializeField] private PlayerController player;
        [SerializeField] private PlayerAbilities abilities;

        [Header("Sprint")]
        [SerializeField] private Image sprintIcon;
        [SerializeField] private Sprite sprintOffSprite;
        [SerializeField] private Sprite sprintOnSprite;

        [Header("Dash")]
        [SerializeField] private Image dashIcon;
        [SerializeField] private Sprite dashOffSprite;
        [SerializeField] private Sprite dashOnSprite;

        [Header("Double Jump")]
        [SerializeField] private Image doubleJumpIcon;
        [SerializeField] private Sprite doubleJumpOffSprite;
        [SerializeField] private Sprite doubleJumpOnSprite;

        private void Update()
        {
            if (player == null)
            {
                return;
            }

            bool sprintLearned = abilities != null && abilities.SprintLearned;
            SetIcon(sprintIcon, sprintLearned, player.IsRunning, sprintOffSprite, sprintOnSprite);

            bool dashLearned = abilities != null && abilities.DashLearned;
            bool dashUsable = abilities != null && abilities.DashEnabled && dashLearned && player.CanDash;
            SetIcon(dashIcon, dashLearned, dashUsable, dashOffSprite, dashOnSprite);

            bool doubleJumpLearned = abilities != null && abilities.DoubleJumpLearned;
            bool doubleJumpUsable = abilities != null && abilities.DoubleJumpEnabled && doubleJumpLearned && !player.HasDoubleJumped;
            SetIcon(doubleJumpIcon, doubleJumpLearned, doubleJumpUsable, doubleJumpOffSprite, doubleJumpOnSprite);
        }

        private void SetIcon(Image icon, bool learned, bool on, Sprite offSprite, Sprite onSprite)
        {
            if (icon == null)
            {
                return;
            }

            if (icon.gameObject.activeSelf != learned)
            {
                icon.gameObject.SetActive(learned);
            }

            if (learned)
            {
                icon.sprite = on ? onSprite : offSprite;
            }
        }
    }
}
