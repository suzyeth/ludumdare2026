using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using PrismZone.Core;
using PrismZone.Player;

namespace PrismZone.UI
{
    /// <summary>
    /// Floating prompt bubble. Reads <see cref="PlayerInteraction.CurrentTarget"/>,
    /// displays its PromptKey (via i18n), and follows the target's world position
    /// (offset upward) — no longer a fixed bottom-of-screen bar.
    ///
    /// Width auto-fits to the TMP_Text content via ContentSizeFitter on this GO.
    /// A little ▽ tail child points at the target below the bubble.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class InteractPrompt : MonoBehaviour
    {
        [Header("Visuals")]
        [SerializeField] private TMP_Text label;
        [SerializeField] private Image background;
        [Tooltip("Extra world-space offset above the target's collider top. Negative Y pushes bubble down INTO the object. Tune in Inspector at runtime — changes apply immediately.")]
        [SerializeField] private Vector2 worldOffset = new Vector2(0f, 0.3f);

        private PlayerInteraction _source;
        private CanvasGroup _group;
        private Canvas _canvas;
        private RectTransform _rt;
        private Camera _cam;

        private void Awake()
        {
            _group = GetComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
            _group.interactable = false;
            _canvas = GetComponentInParent<Canvas>();
            _rt = (RectTransform)transform;
            _cam = Camera.main;

            BindPlayer();
        }

        private void OnEnable()
        {
            // Persistent HUD survives scene loads — rebind to the newly-spawned
            // Player in each scene instead of polling FindGameObjectWithTag every frame.
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            BindPlayer();
            // Camera may have changed between scenes — re-cache on scene load.
            _cam = Camera.main;
        }

        private void BindPlayer()
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            _source = p != null ? p.GetComponent<PlayerInteraction>() : null;
        }

        private void LateUpdate()
        {
            // No per-frame Find here — scene-load rebinding handles respawns.
            // If still null (pre-first-scene or Player missing), hide and bail.
            if (_source == null)
            {
                if (_group != null) _group.alpha = 0f;
                return;
            }

            var target = _source.CurrentTarget;
            bool show = target != null;
            _group.alpha = show ? 1f : 0f;
            if (!show) return;

            // Text first so ContentSizeFitter recomputes the bubble before we reposition.
            if (label != null)
            {
                string key = string.IsNullOrEmpty(target.PromptKey) ? "ui.interact.prompt" : target.PromptKey;
                label.text = I18nManager.Get(key);
            }

            // World → screen → canvas space. Works for both ScreenSpaceOverlay and Camera canvases.
            // Use the cached camera — only re-find if it was destroyed (e.g. cut-scene swap).
            if (_cam == null) _cam = Camera.main;
            var targetMb = target as MonoBehaviour;
            if (_cam == null || targetMb == null || _canvas == null) return;

            // Anchor to the collider's top, not the transform origin — cabinets / tall
            // props have their pivot at the base, so transform.position sits inside them.
            var col = targetMb.GetComponent<Collider2D>();
            float topY = col != null ? col.bounds.max.y : targetMb.transform.position.y;
            Vector3 worldPos = new Vector3(targetMb.transform.position.x, topY, 0f) + (Vector3)worldOffset;
            Vector2 screen = _cam.WorldToScreenPoint(worldPos);

            if (_canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                _rt.position = new Vector3(screen.x, screen.y, 0f);
            }
            else
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    (RectTransform)_canvas.transform, screen, _canvas.worldCamera, out var local);
                _rt.localPosition = local;
            }
        }
    }
}
