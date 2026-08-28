using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Nullcore.InputSystem;

namespace Nullcore.UI
{
    public class KeybindingsUIController : MonoBehaviour
    {
        [SerializeField] private MainMenuController mainMenuController;
        [SerializeField] private Button backButton;
        [SerializeField] private Button saveButton;
        [SerializeField] private GameObject confirmDiscardPopup;
        [SerializeField] private Button popupDiscardButton;
        [SerializeField] private Button popupCancelButton;

        private KeybindCell listeningCell;
        private string listeningCellPreviousText;
        private bool isDirty;

        private void Start()
        {
            foreach (var cell in GetComponentsInChildren<KeybindCell>(true))
            {
                RefreshLabel(cell);
                var button = cell.GetComponent<Button>();
                if (button != null)
                {
                    var capturedCell = cell;
                    button.onClick.AddListener(() => BeginRebind(capturedCell));
                }
            }

            if (saveButton != null)
            {
                saveButton.onClick.RemoveAllListeners();
                saveButton.onClick.AddListener(OnSaveClicked);
            }

            if (backButton != null)
            {
                backButton.onClick.RemoveAllListeners();
                backButton.onClick.AddListener(TryGoBack);
            }

            if (popupDiscardButton != null)
            {
                popupDiscardButton.onClick.RemoveAllListeners();
                popupDiscardButton.onClick.AddListener(OnConfirmDiscard);
            }

            if (popupCancelButton != null)
            {
                popupCancelButton.onClick.RemoveAllListeners();
                popupCancelButton.onClick.AddListener(OnCancelDiscard);
            }

            if (confirmDiscardPopup != null)
            {
                confirmDiscardPopup.SetActive(false);
            }
        }

        private void Update()
        {
            if (listeningCell == null)
            {
                return;
            }

            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                CancelRebind();
                return;
            }

            foreach (var control in keyboard.allKeys)
            {
                if (control.wasPressedThisFrame)
                {
                    AssignKey(listeningCell, control.keyCode);
                    listeningCell = null;
                    return;
                }
            }
        }

        private void BeginRebind(KeybindCell cell)
        {
            if (listeningCell != null)
            {
                CancelRebind();
            }

            listeningCell = cell;
            listeningCellPreviousText = cell.Label.text;
            cell.Label.text = "Press a key...";
        }

        private void CancelRebind()
        {
            if (listeningCell != null)
            {
                listeningCell.Label.text = listeningCellPreviousText;
            }
            listeningCell = null;
        }

        private void AssignKey(KeybindCell cell, Key key)
        {
            KeybindManager.SetKey(cell.Action, cell.Slot, key);
            isDirty = true;
            RefreshLabel(cell);
        }

        private void RefreshLabel(KeybindCell cell)
        {
            cell.Label.text = KeybindManager.GetDisplayName(cell.Action, cell.Slot);
        }

        private void RefreshAllLabels()
        {
            foreach (var cell in GetComponentsInChildren<KeybindCell>(true))
            {
                RefreshLabel(cell);
            }
        }

        private void OnSaveClicked()
        {
            KeybindManager.Save();
            isDirty = false;
        }

        private void TryGoBack()
        {
            if (isDirty)
            {
                if (confirmDiscardPopup != null)
                {
                    confirmDiscardPopup.SetActive(true);
                }
            }
            else if (mainMenuController != null)
            {
                mainMenuController.OnBackClicked();
            }
        }

        private void OnConfirmDiscard()
        {
            KeybindManager.Revert();
            RefreshAllLabels();
            isDirty = false;

            if (confirmDiscardPopup != null)
            {
                confirmDiscardPopup.SetActive(false);
            }

            if (mainMenuController != null)
            {
                mainMenuController.OnBackClicked();
            }
        }

        private void OnCancelDiscard()
        {
            if (confirmDiscardPopup != null)
            {
                confirmDiscardPopup.SetActive(false);
            }
        }
    }
}
