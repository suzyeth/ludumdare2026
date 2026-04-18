using UnityEngine;
using UnityEngine.SceneManagement;

namespace PrismZone.Core
{
    /// <summary>
    /// Attach to any root GameObject that should survive scene loads (Player,
    /// HUD_Canvas, EventSystem, persistent audio, etc). First instance wins and
    /// becomes DontDestroyOnLoad. Later scene-local copies with the same name are
    /// destroyed on Awake so levels can carry placeholder copies without conflict.
    ///
    /// Why name-based dedupe: scene designers drop the prefab into Level_Template.
    /// When the level is entered mid-run, the persistent version already exists —
    /// the scene copy self-destructs cleanly.
    /// </summary>
    [DefaultExecutionOrder(-150)]
    public class PersistentRoot : MonoBehaviour
    {
        private static readonly System.Collections.Generic.Dictionary<string, PersistentRoot> _roots =
            new System.Collections.Generic.Dictionary<string, PersistentRoot>();

        [Tooltip("Unique id. Defaults to GameObject name if left blank.")]
        [SerializeField] private string persistId = "";

        private string _id;

        private void Awake()
        {
            _id = string.IsNullOrEmpty(persistId) ? gameObject.name : persistId;

            if (_roots.TryGetValue(_id, out var existing) && existing != null && existing != this)
            {
                // Duplicate — kill the newcomer, keep the original.
                Destroy(gameObject);
                return;
            }

            _roots[_id] = this;
            // Only works if this is a root-level GameObject.
            if (transform.parent == null) DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (_roots.TryGetValue(_id, out var r) && r == this) _roots.Remove(_id);
        }
    }
}
