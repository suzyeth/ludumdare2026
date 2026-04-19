using UnityEngine;
using PrismZone.Core;

namespace PrismZone.Enemy
{
    /// <summary>
    /// BLUE — Weaver. Static trap. On trigger-enter by Player, fires the global
    /// AlarmBroadcaster for alarmDuration seconds. Cooldown prevents spam.
    /// Reveals under GREEN filter. Runs the base reveal logic via EnemyBase, but
    /// Current stays Idle — trap never chases itself.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class BlueTrap : EnemyBase
    {
        [Header("BLUE")]
        [SerializeField] private float alarmDuration = 10f;
        [SerializeField] private float retriggerCooldown = 3f;

        private float _nextTriggerTime;

        protected override void Awake()
        {
            base.Awake();
            var col = GetComponent<Collider2D>();
            col.isTrigger = true;
            if (hiddenAlpha <= 0f) hiddenAlpha = 0f; // static, stays invisible unless revealed
        }

        protected override void Tick() { /* static */ }

        protected override bool ForceVisibleDuringChase => false;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (Time.time < _nextTriggerTime) return;
            if (!other.CompareTag(GameLayers.TagPlayer)) return;
            if (AlarmBroadcaster.Instance == null) return;

            _nextTriggerTime = Time.time + retriggerCooldown;
            AlarmBroadcaster.Instance.Fire(transform.position, alarmDuration);
        }
    }
}
