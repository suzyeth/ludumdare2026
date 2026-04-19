using System.Collections.Generic;
using UnityEngine;

namespace PrismZone.Core
{
    /// <summary>
    /// Designer-authored mapping from <see cref="SoundId"/> to AudioClip + per-clip
    /// volume / pitch / is-loop. Drop into Assets/Resources/Audio/SoundCatalog.asset
    /// and audio designer edits the list in the Inspector.
    /// </summary>
    [CreateAssetMenu(menuName = "PrismZone/Sound Catalog", fileName = "SoundCatalog")]
    public class SoundCatalog : ScriptableObject
    {
        [System.Serializable]
        public class Entry
        {
            public SoundId id = SoundId.None;
            public AudioClip clip;
            [Range(0f, 1f)] public float volume = 1f;
            [Range(0.5f, 2f)] public float pitch = 1f;
            public bool loop = false;
            public bool isMusic = false;
        }

        [SerializeField] private Entry[] entries = System.Array.Empty<Entry>();

        private Dictionary<SoundId, Entry> _map;

        public Entry Get(SoundId id)
        {
            if (_map == null) Build();
            _map.TryGetValue(id, out var e);
            return e;
        }

        private void Build()
        {
            _map = new Dictionary<SoundId, Entry>(entries.Length);
            foreach (var e in entries)
            {
                if (e == null || e.id == SoundId.None || e.clip == null) continue;
                if (!_map.ContainsKey(e.id)) _map[e.id] = e;
            }
        }
    }
}
