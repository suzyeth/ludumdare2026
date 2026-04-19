using System.Collections;
using TMPro;
using UnityEngine;

namespace PrismZone.UI
{
    /// <summary>
    /// Rolls a TMP label in character-by-character (v1.2 spec: 文字逐字滚动播出).
    /// A click / key press while rolling jumps straight to the end — the AVG popup
    /// drives that by calling <see cref="CompleteImmediately"/>.
    ///
    /// Uses unscaled time so dialogue advances even when <c>Time.timeScale = 0</c>
    /// (happens during Pause, and we may freeze game time on FLASH sequences).
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class TypewriterText : MonoBehaviour
    {
        [SerializeField] private float charactersPerSecond = 40f;

        private TMP_Text _label;
        private Coroutine _routine;

        public bool IsRolling { get; private set; }

        private void Awake() { _label = GetComponent<TMP_Text>(); }

        public void Play(string fullText)
        {
            if (_label == null) _label = GetComponent<TMP_Text>();
            if (_routine != null) StopCoroutine(_routine);
            _label.text = fullText ?? string.Empty;
            _label.ForceMeshUpdate();
            _label.maxVisibleCharacters = 0;
            IsRolling = true;
            _routine = StartCoroutine(Roll());
        }

        public void CompleteImmediately()
        {
            if (!IsRolling) return;
            if (_routine != null) StopCoroutine(_routine);
            if (_label != null) _label.maxVisibleCharacters = int.MaxValue;
            IsRolling = false;
            _routine = null;
        }

        private IEnumerator Roll()
        {
            float spc = 1f / Mathf.Max(1f, charactersPerSecond);
            int total = _label.textInfo.characterCount;
            int i = 0;
            while (i < total)
            {
                i++;
                _label.maxVisibleCharacters = i;
                yield return new WaitForSecondsRealtime(spc);
            }
            IsRolling = false;
            _routine = null;
        }
    }
}
