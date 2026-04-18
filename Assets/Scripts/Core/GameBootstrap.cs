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

        private void Awake()
        {
            I18nManager.Init();

            if (enableInputAsset && InputSystem.actions != null)
            {
                InputSystem.actions.Enable();
            }
        }
    }
}
