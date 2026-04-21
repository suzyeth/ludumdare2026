using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using PrismZone.Core;

namespace PrismZone.UI
{
    /// <summary>
    /// Esc-toggled pause menu. Freezes Time.timeScale = 0 while open.
    /// Priority order when Esc pressed:
    ///   1. If CluePopup open → close it first
    ///   2. If ItemDetailPanel open → close it first
    ///   3. If PasscodePanel open → close it first
    ///   4. Otherwise toggle PauseMenu
    ///
    /// Runs before default-order modals (CluePopup, ItemDetailPanel, PasscodePanel,
    /// SettingsPanel) so the priority chain consumes Esc first; otherwise those
    /// modals' own Esc handlers could close themselves and leave PauseMenu to
    /// fall through to toggle Pause on the same frame.
    /// </summary>
    [DefaultExecutionOrder(-10)]
    public class PauseMenu : MonoBehaviour
    {
        public static PauseMenu Instance { get; private set; }

        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private Button musicToggleButton;
        [SerializeField] private TMP_Text musicToggleLabel;
        [SerializeField] private string mainMenuScene = "Scene_MainMenu";

        // Remember the user's music level before muting so toggling On restores it
        // instead of restoring the default.
        private const string PK_MusicLastNonZero = "pz.music.lastNonZero";

        private CanvasGroup _group;
        private PasscodePanel _cachedPasscode;

        public bool IsOpen => _group != null && _group.alpha > 0.5f;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            _group = GetComponent<CanvasGroup>();
            if (_group == null) _group = gameObject.AddComponent<CanvasGroup>();
            SetVisible(false);

            if (resumeButton       != null) resumeButton.onClick.AddListener(Resume);
            if (settingsButton     != null) settingsButton.onClick.AddListener(OpenSettings);
            if (mainMenuButton     != null) mainMenuButton.onClick.AddListener(GotoMainMenu);
            if (quitButton         != null) quitButton.onClick.AddListener(QuitToMenu);
            if (musicToggleButton  != null) musicToggleButton.onClick.AddListener(ToggleMusic);
            RefreshMusicLabel();
        }

        public void ToggleMusic()
        {
            float cur = GameSettings.MusicVolume;
            if (cur > 0f)
            {
                PlayerPrefs.SetFloat(PK_MusicLastNonZero, cur);
                GameSettings.MusicVolume = 0f;
            }
            else
            {
                float restore = PlayerPrefs.GetFloat(PK_MusicLastNonZero, GameSettings.DefaultMusic);
                if (restore <= 0f) restore = GameSettings.DefaultMusic;
                GameSettings.MusicVolume = restore;
            }
            RefreshMusicLabel();
        }

        private void RefreshMusicLabel()
        {
            if (musicToggleLabel == null) return;
            musicToggleLabel.text = I18nManager.Get(GameSettings.MusicVolume > 0f
                ? "ui.pause.music_on" : "ui.pause.music_off");
        }

        public void OpenSettings()
        {
            if (SettingsPanel.Instance == null) return;
            // PauseMenu is a sibling of SettingsPanel, so hiding its CanvasGroup
            // is safe (does not hide Settings). Time.timeScale stays 0 throughout.
            SetVisible(false);
            SettingsPanel.Instance.OnClosed -= OnSettingsClosed;
            SettingsPanel.Instance.OnClosed += OnSettingsClosed;
            SettingsPanel.Instance.Open();
        }

        private void OnSettingsClosed()
        {
            if (SettingsPanel.Instance != null)
                SettingsPanel.Instance.OnClosed -= OnSettingsClosed;
            // Only reopen the pause backdrop if we're still in a paused state
            // (user didn't click Main Menu / Quit from within Settings).
            if (Time.timeScale == 0f) SetVisible(true);
        }

        private void OnDestroy() { if (Instance == this) Instance = null; }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            // Esc toggles pause as before.
            if (kb.escapeKey.wasPressedThisFrame) { HandleEscape(); return; }
            // Keyboard fallbacks — every pause action gets a dedicated key so
            // players can always control the menu even if input dispatch fails.
            if (IsOpen)
            {
                if (kb.spaceKey.wasPressedThisFrame
                    || kb.enterKey.wasPressedThisFrame
                    || kb.numpadEnterKey.wasPressedThisFrame
                    || kb.rKey.wasPressedThisFrame)                // R = Resume
                {
                    Resume();
                    return;
                }
                if (kb.mKey.wasPressedThisFrame) { ToggleMusic(); return; }   // M = Music toggle
                if (kb.sKey.wasPressedThisFrame) { OpenSettings(); return; }  // S = Settings
                if (kb.hKey.wasPressedThisFrame) { GotoMainMenu(); return; }  // H = Home
            }

            // InputSystemUIInputModule drops pointer dispatch at Time.timeScale == 0,
            // so while paused we run a minimal UGUI pointer pipeline ourselves —
            // hover/press/drag/release/click — and suspend the default module to
            // avoid double-dispatch on edge-case frames. Covers every panel that
            // opens under timeScale == 0 (PauseMenu, SettingsPanel, GameOverPanel,
            // VictoryPanel) including their buttons, sliders, and toggles.
            bool paused = Time.timeScale == 0f && EventSystem.current != null && Mouse.current != null;
            if (paused)
            {
                SuppressDefaultModule(true);
                DispatchManualPointer();
            }
            else if (_mDisabledModule != null || _mPointer != null || _mHoverTarget != null)
            {
                // Exited pause (or lost EventSystem / Mouse). Synthesise cancel so
                // selectables don't stay stuck in pressed/highlighted state, then
                // hand control back to the default input module.
                CancelManualPointer();
                SuppressDefaultModule(false);
            }
        }

        private static readonly List<RaycastResult> s_raycastResults = new List<RaycastResult>();
        // Pointer state across frames (drag and hover require continuity).
        private PointerEventData _mPointer;
        private PointerEventData _mHoverPointer;
        private GameObject _mPressHandler;
        private GameObject _mClickHandler;
        private GameObject _mDragHandler;
        private GameObject _mHoverTarget;
        private bool _mDragActive;                  // True only once motion exceeds pixelDragThreshold.
        private BaseInputModule _mDisabledModule;   // Module we temporarily disabled during pause.

        private void OnDisable()
        {
            // Clean up if PauseMenu is disabled mid-interaction (scene unload, Destroy).
            CancelManualPointer();
            SuppressDefaultModule(false);
        }

        private void SuppressDefaultModule(bool suppress)
        {
            var es = EventSystem.current;
            if (es == null) return;
            if (suppress)
            {
                var mod = es.currentInputModule;
                if (mod != null && mod.enabled && _mDisabledModule == null)
                {
                    _mDisabledModule = mod;
                    mod.enabled = false;
                }
            }
            else if (_mDisabledModule != null)
            {
                _mDisabledModule.enabled = true;
                _mDisabledModule = null;
            }
        }

        private void CancelManualPointer()
        {
            // Fire pointerUp + endDrag so selectables leave pressed state cleanly.
            if (_mPointer != null)
            {
                if (_mPressHandler != null)
                    ExecuteEvents.Execute(_mPressHandler, _mPointer, ExecuteEvents.pointerUpHandler);
                if (_mDragActive && _mDragHandler != null)
                    ExecuteEvents.Execute(_mDragHandler, _mPointer, ExecuteEvents.endDragHandler);
                _mPointer = null;
                _mPressHandler = null;
                _mClickHandler = null;
                _mDragHandler = null;
                _mDragActive = false;
            }
            // Fire exit so Highlighted visual clears.
            if (_mHoverTarget != null && _mHoverPointer != null)
                ExecuteEvents.ExecuteHierarchy(_mHoverTarget, _mHoverPointer, ExecuteEvents.pointerExitHandler);
            _mHoverTarget = null;
        }

        private void DispatchManualPointer()
        {
            var es = EventSystem.current;
            var mouse = Mouse.current;
            Vector2 pos = mouse.position.ReadValue();

            // Stale-press recovery: pending _mPointer with no physical press and
            // no release observed this frame → something decoupled us (scene
            // change, timeScale re-entering 0 after a one-frame resume). Cancel
            // cleanly so no selectable is left in pressed state.
            if (_mPointer != null && !mouse.leftButton.isPressed && !mouse.leftButton.wasReleasedThisFrame)
            {
                if (_mPressHandler != null)
                    ExecuteEvents.Execute(_mPressHandler, _mPointer, ExecuteEvents.pointerUpHandler);
                if (_mDragActive && _mDragHandler != null)
                    ExecuteEvents.Execute(_mDragHandler, _mPointer, ExecuteEvents.endDragHandler);
                _mPointer = null;
                _mPressHandler = null;
                _mClickHandler = null;
                _mDragHandler = null;
                _mDragActive = false;
            }

            // --- Hover: dispatch pointerEnter/pointerExit every frame so Selectable
            // Highlighted transitions fire (Sprite Swap Highlighted, hover tints, etc.)
            // while paused. Without this, buttons only visually respond on press.
            if (_mHoverPointer == null) _mHoverPointer = new PointerEventData(es);
            _mHoverPointer.position = pos;
            s_raycastResults.Clear();
            es.RaycastAll(_mHoverPointer, s_raycastResults);
            GameObject hoverHit = s_raycastResults.Count > 0 ? s_raycastResults[0].gameObject : null;
            _mHoverPointer.pointerCurrentRaycast = s_raycastResults.Count > 0 ? s_raycastResults[0] : default;
            if (hoverHit != _mHoverTarget)
            {
                if (_mHoverTarget != null)
                    ExecuteEvents.ExecuteHierarchy(_mHoverTarget, _mHoverPointer, ExecuteEvents.pointerExitHandler);
                if (hoverHit != null)
                    ExecuteEvents.ExecuteHierarchy(hoverHit, _mHoverPointer, ExecuteEvents.pointerEnterHandler);
                _mHoverTarget = hoverHit;
                _mHoverPointer.pointerEnter = hoverHit;
            }

            // --- Press / drag / release / click ---
            if (mouse.leftButton.wasPressedThisFrame)
            {
                _mPointer = new PointerEventData(es)
                {
                    position = pos,
                    pressPosition = pos,
                    button = PointerEventData.InputButton.Left,
                    pointerEnter = hoverHit,
                };
                s_raycastResults.Clear();
                es.RaycastAll(_mPointer, s_raycastResults);
                var rc = s_raycastResults.Count > 0 ? s_raycastResults[0] : default;
                _mPointer.pointerCurrentRaycast = rc;
                _mPointer.pointerPressRaycast = rc;

                GameObject hit = rc.gameObject;
                _mPressHandler = ExecuteEvents.ExecuteHierarchy(hit, _mPointer, ExecuteEvents.pointerDownHandler)
                                 ?? ExecuteEvents.GetEventHandler<IPointerClickHandler>(hit);
                _mPointer.pointerPress = _mPressHandler;
                _mClickHandler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(hit);
                _mDragHandler = ExecuteEvents.GetEventHandler<IDragHandler>(hit);
                _mDragActive = false;

                // Prime drag target; BeginDrag waits until motion crosses threshold
                // so short presses still register as clicks.
                if (_mDragHandler != null)
                    ExecuteEvents.Execute(_mDragHandler, _mPointer, ExecuteEvents.initializePotentialDrag);
            }
            else if (mouse.leftButton.isPressed && _mPointer != null)
            {
                _mPointer.delta = pos - _mPointer.position;
                _mPointer.position = pos;
                s_raycastResults.Clear();
                es.RaycastAll(_mPointer, s_raycastResults);
                _mPointer.pointerCurrentRaycast = s_raycastResults.Count > 0 ? s_raycastResults[0] : default;

                if (!_mDragActive && _mDragHandler != null)
                {
                    float threshold = es.pixelDragThreshold;
                    Vector2 accum = pos - _mPointer.pressPosition;
                    if (accum.sqrMagnitude >= threshold * threshold)
                    {
                        _mDragActive = true;
                        _mPointer.pointerDrag = _mDragHandler;
                        _mPointer.dragging = true;
                        ExecuteEvents.Execute(_mDragHandler, _mPointer, ExecuteEvents.beginDragHandler);
                    }
                }
                if (_mDragActive && _mDragHandler != null)
                    ExecuteEvents.Execute(_mDragHandler, _mPointer, ExecuteEvents.dragHandler);
            }
            else if (mouse.leftButton.wasReleasedThisFrame && _mPointer != null)
            {
                _mPointer.position = pos;

                if (_mPressHandler != null)
                    ExecuteEvents.Execute(_mPressHandler, _mPointer, ExecuteEvents.pointerUpHandler);

                if (_mDragActive && _mDragHandler != null)
                {
                    ExecuteEvents.Execute(_mDragHandler, _mPointer, ExecuteEvents.endDragHandler);
                    _mPointer.dragging = false;
                }

                s_raycastResults.Clear();
                es.RaycastAll(_mPointer, s_raycastResults);
                GameObject releaseHit = s_raycastResults.Count > 0 ? s_raycastResults[0].gameObject : null;
                GameObject releaseClick = releaseHit != null
                    ? ExecuteEvents.GetEventHandler<IPointerClickHandler>(releaseHit) : null;
                if (!_mDragActive && _mClickHandler != null && releaseClick == _mClickHandler)
                    ExecuteEvents.Execute(_mClickHandler, _mPointer, ExecuteEvents.pointerClickHandler);

                _mPointer = null;
                _mPressHandler = null;
                _mClickHandler = null;
                _mDragHandler = null;
                _mDragActive = false;
            }
        }

        private void HandleEscape()
        {
            // Never open pause over GameOver
            if (GameOverController.IsGameOver) return;

            // 1) close transient modals before opening pause
            // Settings panel takes priority: it can be opened from Pause or MainMenu,
            // so its Esc must close it first without toggling Pause below it.
            if (SettingsPanel.Instance != null && SettingsPanel.Instance.IsOpen)
            { SettingsPanel.Instance.Close(); return; }
            if (CluePopup.Instance != null && CluePopup.Instance.IsOpen)
            { CluePopup.Instance.Close(); return; }
            if (ItemDetailPanel.Instance != null && ItemDetailPanel.Instance.IsOpen)
            { ItemDetailPanel.Instance.Close(); return; }
            if (_cachedPasscode == null)
                _cachedPasscode = FindFirstObjectByType<PasscodePanel>();
            if (_cachedPasscode != null && _cachedPasscode.IsOpen)
            { _cachedPasscode.Close(); return; }

            // 2) toggle pause
            if (IsOpen) Resume();
            else Pause();
        }

        public void Pause()
        {
            if (titleLabel != null) titleLabel.text = I18nManager.Get("ui.pause.title");
            SetVisible(true);
            Time.timeScale = 0f;
        }

        public void Resume()
        {
            SetVisible(false);
            Time.timeScale = 1f;
        }

        public void GotoMainMenu()
        {
            Time.timeScale = 1f;
            SetVisible(false);
            SceneManager.LoadSceneAsync(mainMenuScene);
        }

        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#elif UNITY_WEBGL
            // WebGL has no real Application.Quit — fall back to dropping the player
            // back at the main menu so the button still does something visible.
            GotoMainMenu();
#else
            Application.Quit();
#endif
        }

        /// <summary>Alias used by inspector wiring on Quit-to-Menu button (drops timescale + loads menu).</summary>
        public void QuitToMenu() => GotoMainMenu();

        private void SetVisible(bool on)
        {
            if (_group == null) return;
            _group.alpha = on ? 1f : 0f;
            _group.interactable = on;
            _group.blocksRaycasts = on;
        }
    }
}
