using UnityEngine;
using UnityEngine.UI;
using PrismZone.Core;

namespace PrismZone.UI
{
    /// <summary>
    /// Attach to any Button to make it play SoundId.UIClick on pointer click.
    /// Self-wiring — no inspector setup beyond adding the component.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class UIClickSfx : MonoBehaviour
    {
        [SerializeField] private SoundId soundId = SoundId.UIClick;

        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(() =>
                AudioManager.Instance?.Play(soundId));
        }
    }
}
