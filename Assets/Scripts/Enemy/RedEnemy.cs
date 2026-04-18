using UnityEngine;
using PrismZone.Core;
using PrismZone.Player;

namespace PrismZone.Enemy
{
    /// <summary>
    /// RED — Shrike. Blind, hears noise. Default invisible; on sound pulse inside
    /// its detection radius, straight-line chases through walls at 5 u/s. If player
    /// stays still for stillnessRecallTime, returns home at 2 u/s and hides again.
    /// Reveals under BLUE filter.
    /// </summary>
    public class RedEnemy : EnemyBase
    {
        [Header("RED")]
        [SerializeField] private Transform target;
        [SerializeField] private float chaseSpeed = 5f;
        [SerializeField] private float returnSpeed = 2f;
        [SerializeField] private float hearingRadius = 8f;
        [SerializeField] private float stillnessRecallTime = 10f;
        [SerializeField] private float reachHomeThreshold = 0.05f;

        private Vector2 _chaseTarget;
        private float _lastPlayerMotionTime;
        private Vector2 _prevPlayerPos;
        private bool _alarmSubscribed;

        protected override void Awake()
        {
            base.Awake();
            if (target == null)
            {
                var p = GameObject.FindGameObjectWithTag(GameLayers.TagPlayer);
                if (p != null) target = p.transform;
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            PlayerNoise.OnPulse += HandleNoise;
            if (AlarmBroadcaster.Instance != null)
            {
                AlarmBroadcaster.Instance.OnAlarm += HandleAlarm;
                _alarmSubscribed = true;
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            PlayerNoise.OnPulse -= HandleNoise;
            if (_alarmSubscribed && AlarmBroadcaster.Instance != null)
                AlarmBroadcaster.Instance.OnAlarm -= HandleAlarm;
            _alarmSubscribed = false;
        }

        private void HandleNoise(PlayerNoise.Pulse p)
        {
            float d = Vector2.Distance(p.Position, transform.position);
            if (d <= Mathf.Max(hearingRadius, p.Radius))
            {
                _chaseTarget = p.Position;
                SetState(State.Chase);
            }
        }

        private void HandleAlarm(AlarmBroadcaster.Alarm a)
        {
            _chaseTarget = a.Position;
            SetState(State.Chase);
        }

        protected override void Tick()
        {
            switch (Current)
            {
                case State.Idle:    TickIdle();   break;
                case State.Chase:   TickChase();  break;
                case State.Return:  TickReturn(); break;
            }
        }

        private void TickIdle()
        {
            TrackPlayerMotion();
        }

        private void TickChase()
        {
            if (target != null) _chaseTarget = target.position;
            transform.position = Vector2.MoveTowards(transform.position, _chaseTarget, chaseSpeed * Time.deltaTime);
            TrackPlayerMotion();

            if (Time.time - _lastPlayerMotionTime >= stillnessRecallTime)
            {
                SetState(State.Return);
            }
        }

        private void TickReturn()
        {
            transform.position = Vector2.MoveTowards(transform.position, _homePosition, returnSpeed * Time.deltaTime);
            if (((Vector2)transform.position - _homePosition).sqrMagnitude <= reachHomeThreshold * reachHomeThreshold)
            {
                SetState(State.Idle);
            }
        }

        private void TrackPlayerMotion()
        {
            if (target == null) return;
            Vector2 p = target.position;
            if (((p - _prevPlayerPos).sqrMagnitude) > 0.0004f) _lastPlayerMotionTime = Time.time;
            _prevPlayerPos = p;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.9f, 0.2f, 0.2f, 0.25f);
            Gizmos.DrawWireSphere(transform.position, hearingRadius);
        }
    }
}
