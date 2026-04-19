using TMPro;
using UnityEngine;
using PrismZone.Core;

namespace PrismZone.UI
{
    /// <summary>
    /// Drop on any TMP_Text whose content is an i18n key. The component resolves
    /// the key via <see cref="I18nManager"/> on OnEnable and re-resolves whenever
    /// <see cref="I18nManager.OnLanguageChanged"/> fires.
    ///
    /// No runtime refresh API needed — designer just sets <see cref="i18nKey"/>.
    /// Placeholder text can stay on the TMP field in edit-mode; it is overwritten
    /// the moment this enables.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    [DefaultExecutionOrder(-50)]
    public class LocalizedText : MonoBehaviour
    {
        [Tooltip("TSV id to resolve. Leave blank to skip — TMP keeps its authored text.")]
        [SerializeField] private string i18nKey;
        [Tooltip("Optional formatting args rendered via I18nManager.Format. Use '{0} {1}' tokens in the TSV cell.")]
        [SerializeField] private string[] formatArgs;

        private TMP_Text _tmp;

        public string Key
        {
            get => i18nKey;
            set { i18nKey = value; Apply(); }
        }

        private void Awake()
        {
            _tmp = GetComponent<TMP_Text>();
        }

        private void OnEnable()
        {
            I18nManager.OnLanguageChanged -= Apply;
            I18nManager.OnLanguageChanged += Apply;
            Apply();
        }

        private void OnDisable()
        {
            I18nManager.OnLanguageChanged -= Apply;
        }

        private void Apply()
        {
            if (_tmp == null) _tmp = GetComponent<TMP_Text>();
            if (_tmp == null || string.IsNullOrEmpty(i18nKey)) return;
            if (formatArgs != null && formatArgs.Length > 0)
                _tmp.text = I18nManager.Format(i18nKey, formatArgs);
            else
                _tmp.text = I18nManager.Get(i18nKey);
        }
    }
}
