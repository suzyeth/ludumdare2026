using System;
using UnityEngine;

namespace PrismZone.Player
{
    /// <summary>
    /// Static pulse hub for player-generated noise. RED enemies subscribe;
    /// running emits at ~4 Hz with a radius defined by the caller.
    /// </summary>
    public static class PlayerNoise
    {
        public struct Pulse
        {
            public Vector2 Position;
            public float Radius;
            public float Time;
        }

        public static event Action<Pulse> OnPulse;
        public static Pulse Last { get; private set; }

        public static void Emit(Vector2 position, float radius)
        {
            var p = new Pulse { Position = position, Radius = radius, Time = UnityEngine.Time.time };
            Last = p;
            OnPulse?.Invoke(p);
        }
    }
}
