using UnityEngine;
using PrismZone.Player;

namespace PrismZone.Interact
{
    /// <summary>
    /// Three-state hiding spot.
    ///
    /// Open  → press E → Hide  → press E → Close → press E → Open
    ///
    ///   - Open:  no occupant, cabinet door ajar. Art: Tile_locker_01_open
    ///   - Hide:  player stepped inside, door still open. NPC can still see/catch
    ///            the player. Art: Tile_locker_01_hide, prompt "E 关门".
    ///   - Close: door closed. Player is safe from NPC sight. Art:
    ///            Tile_locker_01_close, prompt "E 离开".
    ///
    /// Player state during Hide: <c>IsInCabinet=true</c> (anchored + sprite hidden)
    /// but <c>IsHidden=false</c> (NPCs still detect). Closing flips IsHidden=true.
    /// </summary>
    public class Cabinet : MonoBehaviour, IInteractable
    {
        private enum CabinetState { Open, Hide, Close }

        [SerializeField] private Transform hideAnchor;
        [SerializeField] private SpriteRenderer doorRenderer;

        [Header("Sprites (Tile_locker_01_* art)")]
        [Tooltip("Default state — cabinet empty, door open. Art: Tile_locker_01_open.")]
        [SerializeField] private Sprite spriteOpen;
        [Tooltip("Player inside, door still open (visible to NPCs). Art: Tile_locker_01_hide.")]
        [SerializeField] private Sprite spriteHide;
        [Tooltip("Player inside, door closed (safe from NPCs). Art: Tile_locker_01_close.")]
        [SerializeField] private Sprite spriteClose;

        private CabinetState _state = CabinetState.Open;
        private PlayerController _occupant;
        private Vector3 _savedPosition;

        public string PromptKey => _state switch
        {
            CabinetState.Open  => "ui.cabinet.prompt",
            CabinetState.Hide  => "ui.cabinet.close_door",
            CabinetState.Close => "ui.cabinet.exit",
            _                  => "ui.cabinet.prompt",
        };

        private void Start() { ApplySprite(); }

        public bool CanInteract(GameObject who)
        {
            var ctl = who.GetComponent<PlayerController>();
            return ctl != null && (_occupant == null || _occupant == ctl);
        }

        public void Interact(GameObject who)
        {
            var ctl = who.GetComponent<PlayerController>();
            if (ctl == null) return;
            if (_occupant != null && _occupant != ctl) return;

            switch (_state)
            {
                case CabinetState.Open:
                    _occupant = ctl;
                    _savedPosition = ctl.transform.position;
                    if (hideAnchor != null) ctl.transform.position = hideAnchor.position;
                    ctl.IsInCabinet = true;
                    _state = CabinetState.Hide;
                    ApplySprite();
                    PrismZone.Core.AudioManager.Instance?.Play(PrismZone.Core.SoundId.Hide);
                    break;

                case CabinetState.Hide:
                    ctl.IsHidden = true;
                    _state = CabinetState.Close;
                    ApplySprite();
                    PrismZone.Core.AudioManager.Instance?.Play(PrismZone.Core.SoundId.CabinetClose);
                    break;

                case CabinetState.Close:
                    ctl.IsHidden = false;
                    ctl.IsInCabinet = false;
                    ctl.transform.position = _savedPosition;
                    _occupant = null;
                    _state = CabinetState.Open;
                    ApplySprite();
                    PrismZone.Core.AudioManager.Instance?.Play(PrismZone.Core.SoundId.CabinetOpen);
                    break;
            }
        }

        private void ApplySprite()
        {
            if (doorRenderer == null) return;
            Sprite s = _state switch
            {
                CabinetState.Open  => spriteOpen,
                CabinetState.Hide  => spriteHide,
                CabinetState.Close => spriteClose,
                _                  => spriteOpen,
            };
            if (s != null) doorRenderer.sprite = s;
        }
    }
}
