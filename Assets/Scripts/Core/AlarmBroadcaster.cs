using System;
using UnityEngine;

namespace PrismZone.Core
{
    /// <summary>
    /// BLUE trap triggers fire an alarm here. RED/GREEN enemies subscribe and extend
    /// aggression toward the alarm location for a fixed window (10s per spec).
    /// </summary>
    [DefaultExecutionOrder(-90)]
    public class AlarmBroadcaster : MonoBehaviour
    {
        public static AlarmBroadcaster Instance { get; private set; }

        public const float DefaultDuration = 10f;

        public struct Alarm
        {
            public Vector2 Position;
            public float ExpireTime;
            public bool IsActive(float now) => now <= ExpireTime;
        }

        public event Action<Alarm> OnAlarm;
        public Alarm LastAlarm { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Fire(Vector2 position, float duration = DefaultDuration)
        {
            var alarm = new Alarm
            {
                Position = position,
                ExpireTime = Time.time + duration
            };
            LastAlarm = alarm;
            OnAlarm?.Invoke(alarm);
        }
    }
}
