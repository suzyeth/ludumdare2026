using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PrismZone.Core;
using PrismZone.Interact;

namespace PrismZone.UI
{
    /// <summary>
    /// Center-screen 4-digit passcode modal. Listens to PasscodeDoor.OnRequestPasscode,
    /// pauses input routing by time.timeScale is untouched (so enemies keep ticking)
    /// but player input is ignored while panel is open via the open flag.
    /// </summary>
    public class PasscodePanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text displayLabel;
        [SerializeField] private TMP_Text errorLabel;
        [SerializeField] private Button[] digitButtons = new Button[10];
        [SerializeField] private Button clearButton;
        [SerializeField] private Button submitButton;
        [SerializeField] private Button closeButton;

        private PasscodeDoor _target;
        private string _entered = string.Empty;
        private CanvasGroup _group;

        public bool IsOpen => _group != null && _group.alpha > 0.5f;

        private void Awake()
        {
            _group = GetComponent<CanvasGroup>();
            if (_group == null) _group = gameObject.AddComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.interactable = false;
            _group.blocksRaycasts = false;

            for (int i = 0; i < digitButtons.Length; i++)
            {
                int digit = i;
                if (digitButtons[i] != null)
                    digitButtons[i].onClick.AddListener(() => PressDigit(digit));
            }
            if (clearButton != null) clearButton.onClick.AddListener(Clear);
            if (submitButton != null) submitButton.onClick.AddListener(Submit);
            if (closeButton != null) closeButton.onClick.AddListener(Close);
        }

        private void Update()
        {
            if (!IsOpen) return;
            // Esc also closes
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame) Close();
        }

        private void OnEnable() { PasscodeDoor.OnRequestPasscode += Open; }
        private void OnDisable() { PasscodeDoor.OnRequestPasscode -= Open; }

        private void Open(PasscodeDoor door)
        {
            _target = door;
            _entered = string.Empty;
            if (titleLabel != null) titleLabel.text = I18nManager.Get("ui.passcode.title");
            if (errorLabel != null) errorLabel.text = string.Empty;
            RefreshDisplay();
            SetVisible(true);
        }

        public void Close()
        {
            SetVisible(false);
            _target = null;
            _entered = string.Empty;
        }

        private void SetVisible(bool on)
        {
            if (_group == null) return;
            _group.alpha = on ? 1f : 0f;
            _group.interactable = on;
            _group.blocksRaycasts = on;
        }

        private void PressDigit(int d)
        {
            if (_entered.Length >= 4) return;
            _entered += d.ToString();
            RefreshDisplay();
        }

        private void Clear()
        {
            _entered = string.Empty;
            if (errorLabel != null) errorLabel.text = string.Empty;
            RefreshDisplay();
        }

        private void Submit()
        {
            if (_target == null) return;
            if (_entered.Length != 4) return;
            if (_target.TrySubmit(_entered)) Close();
            else
            {
                if (errorLabel != null) errorLabel.text = I18nManager.Get("ui.passcode.wrong");
                _entered = string.Empty;
                RefreshDisplay();
            }
        }

        private void RefreshDisplay()
        {
            if (displayLabel == null) return;
            char[] buf = { '_', '_', '_', '_' };
            for (int i = 0; i < _entered.Length && i < 4; i++) buf[i] = _entered[i];
            displayLabel.text = new string(buf);
        }
    }
}
