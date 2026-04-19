using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PrismZone.Core;
using PrismZone.Player;

namespace PrismZone.UI
{
    /// <summary>
    /// Reads the Player's PlayerInteraction.CurrentTarget each frame and shows its
    /// PromptKey (via I18nManager) in a TMP label. Toggles visibility via CanvasGroup
    /// alpha so this component's own GameObject stays active (otherwise Update stops).
    /// </summary>
    public class InteractPrompt : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private Image background;

        private PlayerInteraction _source;
        private CanvasGroup _group;

        private void Awake()
        {
            _group = GetComponent<CanvasGroup>();
            if (_group == null) _group = gameObject.AddComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
            _group.interactable = false;

            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) _source = p.GetComponent<PlayerInteraction>();
        }

        private void Update()
        {
            if (_source == null)
            {
                var p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) _source = p.GetComponent<PlayerInteraction>();
                if (_source == null) return;
            }

            var target = _source.CurrentTarget;
            bool show = target != null;
            _group.alpha = show ? 1f : 0f;

            if (show && label != null)
            {
                string key = string.IsNullOrEmpty(target.PromptKey) ? "ui.interact.prompt" : target.PromptKey;
                label.text = I18nManager.Get(key);
            }
        }
    }
}
