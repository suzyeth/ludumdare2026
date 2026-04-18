using UnityEngine;
using UnityEngine.InputSystem;

namespace PrismZone.Core
{
    /// <summary>
    /// One-shot scene bootstrap. Attach to the _Bootstrap GameObject in the scene.
    /// - Initialises I18nManager (loads zh/en JSON)
    /// - Enables the project-wide Input Actions asset so PlayerController/PlayerInteraction
    ///   can read actions without a PlayerInput component.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private bool enableInputAsset = true;

        /// <summary>
        /// Keep the whole _Bootstrap branch alive across scene loads so Inventory /
        /// FilterManager / AlarmBroadcaster / audio singletons survive transitions.
        /// Only one Bootstrap may exist — the first wins, later duplicates self-destruct.
        /// </summary>
        private static GameBootstrap _instance;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            I18nManager.Init();

            if (enableInputAsset && InputSystem.actions != null)
            {
                InputSystem.actions.Enable();
            }
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }
    }
}
