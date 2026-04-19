using UnityEngine;
using PrismZone.Player;

namespace PrismZone.Interact
{
    /// <summary>
    /// Step-in hiding spot. While PlayerController.IsHidden is true, the player's
    /// collider stays intact but velocity is zeroed and enemies treat the player
    /// as undetectable (they should check IsHidden before chasing — wired in AI
    /// via shared PlayerController reference).
    /// </summary>
    public class Cabinet : MonoBehaviour, IInteractable
    {
        [SerializeField] private Transform hideAnchor;
        [SerializeField] private SpriteRenderer doorRenderer;
        [SerializeField] private Sprite closedSprite;
        [SerializeField] private Sprite openSprite;

        private PlayerController _occupant;
        private Vector3 _savedPosition;

        public string PromptKey => _occupant == null ? "ui.cabinet.prompt" : "ui.cabinet.exit";

        public bool CanInteract(GameObject who)
        {
            var ctl = who.GetComponent<PlayerController>();
            return ctl != null && (_occupant == null || _occupant == ctl);
        }

        public void Interact(GameObject who)
        {
            var ctl = who.GetComponent<PlayerController>();
            if (ctl == null) return;

            if (_occupant == null)
            {
                _occupant = ctl;
                _savedPosition = ctl.transform.position;
                if (hideAnchor != null) ctl.transform.position = hideAnchor.position;
                ctl.IsHidden = true;
                SwapSprite(true);
            }
            else if (_occupant == ctl)
            {
                ctl.IsHidden = false;
                ctl.transform.position = _savedPosition;
                _occupant = null;
                SwapSprite(false);
            }
        }

        private void SwapSprite(bool open)
        {
            if (doorRenderer == null) return;
            var s = open ? openSprite : closedSprite;
            if (s != null) doorRenderer.sprite = s;
        }
    }
}
