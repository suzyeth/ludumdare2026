using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using PrismZone.Core;
using PrismZone.Enemy;

namespace PrismZone.UI
{
    /// <summary>
    /// Central AVG dialogue dispatcher (v1.2 spec §4.1). Designer wires one
    /// <see cref="AvgPopup"/> per <see cref="DialogueType"/> into <see cref="popups"/>;
    /// gameplay code calls <see cref="Show(DialogueType, string, Action)"/> / the
    /// key-based overloads to trigger a node.
    ///
    /// Features:
    ///   - queue: overlapping requests are processed in order
    ///   - freeze: <see cref="SetFrozen"/>(true) pauses queue (broadcast period)
    ///   - skip / advance: primary = Space / Enter / LMB / E; cancel = Esc
    ///   - FLASH style is marked non-skippable by its AvgPopup flag
    ///
    /// Popup <c>Tick</c> runs in unscaled time so dialogue works while Time.timeScale=0.
    /// </summary>
    [DefaultExecutionOrder(-60)]
    public class DialogueManager : MonoBehaviour
    {
        public static DialogueManager Instance { get; private set; }

        [Serializable]
        public struct PopupSlot
        {
            public DialogueType type;
            public AvgPopup popup;
        }

        [SerializeField] private PopupSlot[] popups;

        public event Action<DialogueType, string> OnDialogueFinished;
        public bool IsShowing => _active != null;
        public bool IsFrozen { get; private set; }

        private readonly Dictionary<DialogueType, AvgPopup> _byType = new();
        private readonly Queue<Request> _queue = new();
        private AvgPopup _active;
        private Request _activeRequest;

        private struct Request
        {
            public DialogueType type;
            public string[] textKeys;
            public Action onFinished;
            public Vector3? worldPos;
            public string tag; // optional identifier (e.g. "T-01") — surfaced via OnDialogueFinished
            public string titleKey; // READ only — i18n key for the popup title bar
            public Sprite headerSprite; // READ only — left-side big icon
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            // Inspector-wired entries take precedence...
            if (popups != null)
            {
                foreach (var s in popups) if (s.popup != null) _byType[s.type] = s.popup;
            }
            // ...then auto-discover any AvgPopup in the scene that isn't already mapped.
            // Lets MCP / designer skip filling the popups array — just drop the popup
            // prefabs into the HUD canvas and the manager finds them by their `type` field.
            foreach (var pop in FindObjectsByType<AvgPopup>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (pop == null) continue;
                if (!_byType.ContainsKey(pop.Type)) _byType[pop.Type] = pop;
            }
            if (transform.parent == null) DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            EnemyBase.OnAnyStateChanged -= HandleEnemyStateChanged;
            EnemyBase.OnAnyStateChanged += HandleEnemyStateChanged;
        }

        private void OnDisable()
        {
            EnemyBase.OnAnyStateChanged -= HandleEnemyStateChanged;
        }

        private void OnDestroy() { if (Instance == this) Instance = null; }

        // Spec §4.3: entering Chase force-closes any open AVG popup. We also drain
        // the queue — story beats queued during patrol shouldn't surface mid-chase.
        private void HandleEnemyStateChanged(EnemyBase who, EnemyBase.State prev, EnemyBase.State next)
        {
            if (next == EnemyBase.State.Chase) ClearAll();
        }

        private void Update()
        {
            if (_active == null)
            {
                if (!IsFrozen && _queue.Count > 0) StartRequest(_queue.Dequeue());
                return;
            }

            var kb = Keyboard.current;
            var mouse = Mouse.current;
            bool primary = kb != null && (kb.spaceKey.wasPressedThisFrame
                                         || kb.enterKey.wasPressedThisFrame
                                         || kb.eKey.wasPressedThisFrame);
            if (!primary && mouse != null) primary = mouse.leftButton.wasPressedThisFrame;
            bool cancel = kb != null && kb.escapeKey.wasPressedThisFrame;

            bool finished = _active.Tick(primary, cancel);
            if (finished) ClearActive();
        }

        // --- Public API --------------------------------------------------------

        public void Show(DialogueType type, string i18nKey, Action onFinished = null, string tag = null)
        {
            ShowKeys(type, new[] { i18nKey }, onFinished, null, tag);
        }

        /// <summary>
        /// Framework primary API (FRAMEWORK.md §4.2). Looks up the row in <see cref="TextTable"/>
        /// — type, multi-page text, optional follow-up — and dispatches everything from the
        /// table. Sets <c>FlagKeys.Dialogue.&lt;id&gt;</c> after the popup closes so other
        /// triggers in OnState mode can react.
        /// </summary>
        public void ShowById(string nodeId, Action onFinished = null, Vector3? worldPos = null,
                             string titleKey = null, Sprite headerSprite = null)
        {
            if (string.IsNullOrEmpty(nodeId)) return;
            var entry = TextTable.Get(nodeId);
            if (entry == null)
            {
                Debug.LogWarning($"[DialogueManager] No TextTable row for nodeId={nodeId}");
                return;
            }
            DialogueType type = MapType(entry.avgType);
            int pageCount = TextTable.PageCount(nodeId, I18nManager.CurrentLang,
                FilterManager.Instance != null ? FilterManager.Instance.Current : FilterColor.None);
            // Build a synthetic key array — one entry per page. Not real i18n keys; the
            // resolver below will hand them straight to TextTable.T(nodeId, page).
            var pseudoKeys = new string[Mathf.Max(1, pageCount)];
            for (int i = 0; i < pseudoKeys.Length; i++)
                pseudoKeys[i] = $"@{nodeId}#{i}";

            void Wrapped()
            {
                // Mark triggered (idempotent — second fire is a no-op via GameFlags dedupe).
                GameFlags.Set($"dialogue.{nodeId}.triggered", true);
                onFinished?.Invoke();
                // Auto-chain to follow_up_id if the row points to one and it's not yet triggered.
                // Propagate titleKey/headerSprite so chained READ pages (diary, letter, notes)
                // keep the same header art — otherwise the NAR→READ chain drops the big icon.
                if (!string.IsNullOrEmpty(entry.followUpId)
                    && !GameFlags.Get($"dialogue.{entry.followUpId}.triggered"))
                {
                    ShowById(entry.followUpId, null, null, titleKey, headerSprite);
                }
            }
            ShowKeys(type, pseudoKeys, Wrapped, worldPos, nodeId, titleKey, headerSprite);
        }

        private static DialogueType MapType(TextEntry.AvgType t)
        {
            return t switch
            {
                TextEntry.AvgType.NAR   => DialogueType.NAR,
                TextEntry.AvgType.READ  => DialogueType.READ,
                TextEntry.AvgType.TIP   => DialogueType.NAR, // C1 scope cut: TIP routes to NAR
                TextEntry.AvgType.FLASH => DialogueType.FLASH,
                TextEntry.AvgType.ENV   => DialogueType.ENV,
                _                       => DialogueType.NAR,
            };
        }

        public void ShowKeys(DialogueType type, string[] i18nKeys, Action onFinished = null,
                             Vector3? worldPos = null, string tag = null,
                             string titleKey = null, Sprite headerSprite = null)
        {
            if (i18nKeys == null || i18nKeys.Length == 0) return;
            _queue.Enqueue(new Request
            {
                type = type, textKeys = i18nKeys, onFinished = onFinished,
                worldPos = worldPos, tag = tag,
                titleKey = titleKey, headerSprite = headerSprite
            });
        }

        public void ShowEnv(string i18nKey, Vector3 worldPos, Action onFinished = null, string tag = null)
        {
            ShowKeys(DialogueType.ENV, new[] { i18nKey }, onFinished, worldPos, tag);
        }

        /// <summary>Pause/resume dispatching. Used by BroadcastController during 广播.</summary>
        public void SetFrozen(bool frozen) { IsFrozen = frozen; }

        /// <summary>Force-close current popup (e.g. player entered Chase — spec §4.3).</summary>
        public void CancelActive()
        {
            if (_active == null) return;
            _active.Close();
            ClearActive();
        }

        /// <summary>Drop every queued + active popup (e.g. scene reload).</summary>
        public void ClearAll()
        {
            // Close active first — its onFinished callback may re-enqueue a follow-up.
            // Clearing the queue afterward also removes any such follow-up.
            if (_active != null) _active.Close();
            _queue.Clear();
            ClearActive();
        }

        // --- Internals ---------------------------------------------------------

        private void StartRequest(Request r)
        {
            if (!_byType.TryGetValue(r.type, out var popup) || popup == null)
            {
                Debug.LogWarning($"[DialogueManager] No popup wired for type {r.type}, dropping tag={r.tag}");
                r.onFinished?.Invoke();
                OnDialogueFinished?.Invoke(r.type, r.tag);
                return;
            }

            // Resolve text per element. Two formats are accepted:
            //   - "@nodeId#page" (pseudo-key from ShowById): pulls from TextTable directly
            //     so filter-conditional cells (zh_red/zh_green/...) and multi-page (|) work.
            //   - anything else (legacy): goes through I18nManager → TextTable for back-compat.
            // If the row is missing entirely the key string passes through visibly so designers
            // see the gap on screen instead of an empty bubble.
            var lines = new string[r.textKeys.Length];
            for (int i = 0; i < r.textKeys.Length; i++)
            {
                var key = r.textKeys[i];
                if (!string.IsNullOrEmpty(key) && key.Length > 1 && key[0] == '@')
                {
                    int hash = key.IndexOf('#');
                    string nodeId = hash > 0 ? key.Substring(1, hash - 1) : key.Substring(1);
                    int page = 0;
                    if (hash > 0) int.TryParse(key.Substring(hash + 1), out page);
                    var filter = FilterManager.Instance != null ? FilterManager.Instance.Current : FilterColor.None;
                    lines[i] = TextTable.T(nodeId, page, I18nManager.CurrentLang, filter);
                }
                else
                {
                    lines[i] = I18nManager.Get(key);
                }
            }

            _active = popup;
            _activeRequest = r;

            if (r.type == DialogueType.ENV && r.worldPos.HasValue && Camera.main != null)
                popup.AnchorToWorld(r.worldPos.Value, Camera.main);

            // READ header is per-node (different sprite + title for diary/letter/notes/etc.).
            // Manager fills it via SetHeader before Show so the popup renders correctly first frame.
            if (r.type == DialogueType.READ)
            {
                string titleText = string.IsNullOrEmpty(r.titleKey) ? null : I18nManager.Get(r.titleKey);
                popup.SetHeader(titleText, r.headerSprite);
            }

            popup.Show(lines, OnPopupFinished);

            // If the tag looks like "foo.page.N" (e.g. item.diary.page.2), show
            // "第 N 页" in the page counter instead of the internal sub-page
            // index. Matches the diary's chained READ pages rather than
            // sub-pages within one popup cell.
            int pageNum = ExtractPageNumber(r.tag);
            if (pageNum > 0)
                popup.SetPageCounterOverride(I18nManager.Format("ui.popup.page_number", pageNum));
        }

        private static int ExtractPageNumber(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return -1;
            const string marker = ".page.";
            int idx = tag.LastIndexOf(marker, System.StringComparison.Ordinal);
            if (idx < 0) return -1;
            string tail = tag.Substring(idx + marker.Length);
            return int.TryParse(tail, out int n) ? n : -1;
        }

        private void OnPopupFinished()
        {
            var req = _activeRequest;
            req.onFinished?.Invoke();
            OnDialogueFinished?.Invoke(req.type, req.tag);
        }

        private void ClearActive()
        {
            _active = null;
            _activeRequest = default;
        }
    }
}
