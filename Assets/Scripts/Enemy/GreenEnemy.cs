using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using PrismZone.Core;

namespace PrismZone.Enemy
{
    /// <summary>
    /// GREEN — Watcher. Patrols waypoints. Eyeballs a 120° cone at range 6;
    /// walls/doors break line-of-sight. On LOS catch, enters Chase using a
    /// Tilemap BFS path. If LOS stays lost for 20s, walks back to nearest waypoint.
    /// Reveals under RED filter.
    /// </summary>
    public class GreenEnemy : EnemyBase
    {
        [Header("GREEN")]
        [SerializeField] private Transform target;
        [SerializeField] private Transform[] waypoints;
        [SerializeField] private float patrolSpeed = 1.5f;
        [SerializeField] private float chaseSpeed = 3.5f;
        [SerializeField] private float visionRange = 6f;
        [SerializeField] private float visionAngleDeg = 120f;
        [Tooltip("Close-range omnidirectional aggro. Player within this radius is spotted regardless of vision cone (still blocked by walls). Simulates hearing / proximity awareness.")]
        [SerializeField] private float proximityAggroRadius = 2.5f;
        [Tooltip("Player within this radius during an active-hunt state is caught → GameOver. Bypasses trigger colliders (which the guard's own solid body can prevent from overlapping). Must be > 1.0 because each sprite collider has ~0.5 half-width so they can't physically overlap any closer.")]
        [SerializeField] private float catchRadius = 1.2f;
        [Tooltip("Chase speed during broadcast (Locating state). Player is stunned — this should feel threatening.")]
        [SerializeField] private float locatingSpeed = 4.5f;
        [SerializeField] private float aggroLossTime = 20f;
        [SerializeField] private float arriveThreshold = 0.1f;
        [SerializeField] private float repathInterval = 0.4f;
        [SerializeField] private LayerMask sightBlockers;
        [SerializeField] private Tilemap walkableTilemap;
        [SerializeField] private Tilemap wallTilemap;

        [Header("Collision")]
        [Tooltip("Size of the enemy's body when checking 'can I step into this spot'. Keep slightly smaller than the visual sprite so the enemy doesn't snag corners.")]
        [SerializeField] private Vector2 bodySize = new Vector2(0.7f, 0.5f);
        [Tooltip("Layers that block movement. Leave empty to block against ALL non-trigger colliders.")]
        [SerializeField] private LayerMask blockerMask = ~0;

        private TilemapPathfinder _pathfinder;
        private List<Vector3Int> _path = new List<Vector3Int>();
        private int _pathIndex;
        private float _nextRepathTime;
        private int _waypointIndex;
        private float _lostSightAt = -1f;
        private Vector2 _facing = Vector2.right;
        private bool _alarmSubscribed;
        private Rigidbody2D _rb;
        private bool _caught;           // one-shot latch so GameOver only fires once per guard
        private float _locatingEnteredAt = -1f; // watchdog — broadcast coroutine death must not strand us in Locating

        protected override void Awake()
        {
            base.Awake();
            if (target == null)
            {
                var p = GameObject.FindGameObjectWithTag(GameLayers.TagPlayer);
                if (p != null) target = p.transform;
            }
            if (wallTilemap != null) _pathfinder = new TilemapPathfinder(wallTilemap);
            _rb = GetComponent<Rigidbody2D>();
            // Collect our own colliders so the OverlapBox check can ignore them.
            _ownColliders = GetComponentsInChildren<Collider2D>(true);
            // Cache PlayerController to avoid per-frame GetComponent calls (Fix 6).
            if (target != null) _playerController = target.GetComponent<PrismZone.Player.PlayerController>();
            // Detach any waypoint that is parented under this guard. Patrol targets are
            // read as world positions, so a waypoint under our own transform drifts with
            // us every frame — the enemy chases a moving target that is always the same
            // relative offset away, effectively walking a constant vector into a wall
            // until stuck. Reparent to scene root with world position preserved.
            if (waypoints != null)
            {
                for (int i = 0; i < waypoints.Length; i++)
                {
                    var wp = waypoints[i];
                    if (wp == null) continue;
                    if (wp.IsChildOf(transform))
                    {
                        Debug.LogWarning($"[GreenEnemy] Waypoint '{wp.name}' was parented under this guard ('{name}'). Detaching to scene root so patrol positions stop drifting. Fix the prefab hierarchy when possible.", this);
                        wp.SetParent(null, worldPositionStays: true);
                    }
                }
            }
        }

        private Collider2D[] _ownColliders;
        private readonly Collider2D[] _overlapBuf = new Collider2D[4];
        private PrismZone.Player.PlayerController _playerController;
        private int _returnWaypointIndex = -1; // cached nearest waypoint index for Return state

        // Stuck detection — if chase/locating hasn't moved us for this long, force a repath.
        private Vector2 _stuckProbePos;
        private float _stuckProbeAt = -1f;
        private const float StuckProbeWindow = 0.5f;
        private const float StuckMoveThresholdSqr = 0.0025f; // 0.05 world unit squared

        protected override void OnEnable()
        {
            base.OnEnable();
            if (AlarmBroadcaster.Instance != null)
            {
                AlarmBroadcaster.Instance.OnAlarm += HandleAlarm;
                _alarmSubscribed = true;
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (_alarmSubscribed && AlarmBroadcaster.Instance != null)
                AlarmBroadcaster.Instance.OnAlarm -= HandleAlarm;
            _alarmSubscribed = false;
        }

        private void HandleAlarm(AlarmBroadcaster.Alarm a)
        {
            // Alarm pulls GREEN toward the trap point; behaves like a temporary chase goal.
            SetState(State.Chase);
            RecomputePath(a.Position);
        }

        protected override void OnStateChanged(State prev, State next)
        {
            // Reset the Locating watchdog when we leave Locating so next entry
            // starts a fresh 20s window.
            if (prev == State.Locating && next != State.Locating)
                _locatingEnteredAt = -1f;

            // Cache nearest waypoint index once on Return entry so TickReturn
            // doesn't scan the full array every frame (Fix 7).
            if (next == State.Return)
            {
                _returnWaypointIndex = 0;
                if (waypoints != null)
                {
                    float best = float.PositiveInfinity;
                    for (int i = 0; i < waypoints.Length; i++)
                    {
                        if (waypoints[i] == null) continue;
                        float d = ((Vector2)waypoints[i].position - (Vector2)transform.position).sqrMagnitude;
                        if (d < best) { best = d; _returnWaypointIndex = i; }
                    }
                }
            }
        }

        protected override void Tick()
        {
            // Distance-based catch check — runs every frame regardless of
            // colliders. The guard's solid collider shell can prevent the
            // player's own solid collider from overlapping the guard's trigger
            // zone, making OnTriggerEnter unreliable. A raw distance check
            // guarantees catch during active-hunt states.
            TryCatchByDistance();

            // Cache once per frame — TickChase reuses this result (Fix 5).
            bool canSee = CanSeePlayer();

            // Locating is driven by BroadcastController — don't let LOS
            // re-transition out of it while the broadcast is running.
            if (Current != State.Locating && canSee)
            {
                _lostSightAt = -1f;
                SetState(State.Chase);
            }

            switch (Current)
            {
                case State.Idle:
                case State.Patrol:   TickPatrol();        break;
                case State.Chase:    TickChase(canSee);   break;
                case State.Locating: TickLocating();      break;
                case State.Return:   TickReturn();        break;
            }
        }

        private bool CanSeePlayer()
        {
            if (target == null) return false;
            // Hiding in a cabinet = invisible to sight-based enemies (guards, GREEN).
            // Sound-based RED still hears running noise via PlayerNoise pulse, which is
            // suppressed elsewhere while hiding (Rigidbody velocity = 0).
            // Use the cached reference set in Awake to avoid per-frame GetComponent (Fix 6).
            if (_playerController != null && _playerController.IsHidden) return false;

            Vector2 to = (Vector2)target.position - (Vector2)transform.position;
            float dist = to.magnitude;
            if (dist > visionRange) return false;

            Vector2 dir = dist > 0.0001f ? to / dist : _facing;

            // Close-range omnidirectional detection — player standing right
            // next to the guard is spotted even if behind. Still respects walls
            // so adjacent rooms don't leak aggro through solid geometry.
            if (dist <= proximityAggroRadius)
            {
                var closeHit = Physics2D.Raycast(transform.position, dir, dist, sightBlockers);
                return closeHit.collider == null;
            }

            // Far-range: require player inside the vision cone AND no wall between.
            float dot = Vector2.Dot(_facing.normalized, dir);
            float cosHalf = Mathf.Cos(visionAngleDeg * 0.5f * Mathf.Deg2Rad);
            if (dot < cosHalf) return false;

            var hit = Physics2D.Raycast(transform.position, dir, dist, sightBlockers);
            return hit.collider == null;
        }

        private void TickPatrol()
        {
            if (waypoints == null || waypoints.Length == 0) { SetState(State.Idle); return; }
            var wp = waypoints[_waypointIndex];
            if (wp == null) return;
            StepTowards(wp.position, patrolSpeed);
            if (((Vector2)wp.position - (Vector2)transform.position).sqrMagnitude <= arriveThreshold * arriveThreshold)
            {
                _waypointIndex = (_waypointIndex + 1) % waypoints.Length;
            }
        }

        private void TickChase(bool canSeePlayer)
        {
            if (target == null) { SetState(State.Return); return; }

            // Reuse the frame-cached result from Tick() — no second CanSeePlayer() call (Fix 5).
            if (canSeePlayer) _lostSightAt = -1f;
            else if (_lostSightAt < 0f) _lostSightAt = Time.time;

            if (_lostSightAt > 0f && Time.time - _lostSightAt >= aggroLossTime)
            {
                SetState(State.Return);
                return;
            }

            if (Time.time >= _nextRepathTime)
            {
                _nextRepathTime = Time.time + repathInterval;
                RecomputePath(target.position);
            }

            FollowPath(chaseSpeed);
        }

        private void TickLocating()
        {
            // Broadcast mode (spec §4.3): AVG frozen, player stunned, guard
            // gets a "room-locate" sweep straight toward the player's known
            // position. No LOS check — the broadcast IS the tell.
            if (target == null) return;

            // Watchdog: if we entered Locating and BroadcastController never
            // flips us back (coroutine death / scene teardown), bail to Patrol
            // after a safe upper bound. 20s generously covers any reasonable
            // broadcast duration (spec default 10s + slack).
            if (_locatingEnteredAt < 0f) _locatingEnteredAt = Time.time;
            if (Time.time - _locatingEnteredAt > 20f)
            {
                _locatingEnteredAt = -1f;
                SetState(State.Patrol);
                return;
            }

            if (Time.time >= _nextRepathTime)
            {
                _nextRepathTime = Time.time + repathInterval;
                RecomputePath(target.position);
            }
            FollowPath(locatingSpeed);
        }

        private void TickReturn()
        {
            if (waypoints == null || waypoints.Length == 0) { SetState(State.Idle); return; }
            // Use index cached in OnStateChanged — no per-frame array scan (Fix 7).
            int idx = (_returnWaypointIndex >= 0 && _returnWaypointIndex < waypoints.Length)
                ? _returnWaypointIndex : 0;
            Transform closest = waypoints[idx];
            if (closest == null) { SetState(State.Idle); return; }
            StepTowards(closest.position, patrolSpeed);
            if (((Vector2)closest.position - (Vector2)transform.position).sqrMagnitude <= arriveThreshold * arriveThreshold)
            {
                SetState(State.Patrol);
            }
        }

        private void RecomputePath(Vector3 worldGoal)
        {
            if (_pathfinder == null || wallTilemap == null) { _path.Clear(); return; }
            var start = wallTilemap.WorldToCell(transform.position);
            var goal = wallTilemap.WorldToCell(worldGoal);
            // Copy the result — FindPath returns a shared internal list that is
            // cleared on the next call, so each enemy must own its path (Fix 1).
            _path = new List<Vector3Int>(_pathfinder.FindPath(start, goal));
            _pathIndex = 0;
            SmoothPath();
            // Reset the stuck probe so a fresh path gets a clean move-or-repath window.
            _stuckProbePos = transform.position;
            _stuckProbeAt = Time.time;
        }

        /// <summary>
        /// String-pull smoothing: collapse cell-by-cell BFS path into sparse
        /// anchor points at actual level corners. Walking straight lines between
        /// anchors prevents per-tile turn stutter AND keeps <see cref="StepTowards"/>'s
        /// wall-slide useful — BFS produces axis-only steps that fail both slide
        /// branches, so without smoothing the enemy freezes whenever a tile-center
        /// waypoint grazes a wall.
        /// </summary>
        private void SmoothPath()
        {
            if (_path == null || _path.Count < 2) return;
            var smoothed = new List<Vector3Int>(_path.Count);
            Vector3 anchor = transform.position;
            int i = 0;
            while (i < _path.Count)
            {
                int farthest = i;
                for (int j = i + 1; j < _path.Count; j++)
                {
                    Vector3 candidate = wallTilemap.GetCellCenterWorld(_path[j]);
                    if (HasClearLine(anchor, candidate))
                        farthest = j;
                    else
                        break;
                }
                smoothed.Add(_path[farthest]);
                anchor = wallTilemap.GetCellCenterWorld(_path[farthest]);
                i = farthest + 1;
            }
            _path = smoothed;
        }

        /// <summary>
        /// Box-sampled line-of-sight. Reuses <see cref="CanMoveTo"/> at intervals
        /// along the segment so triggers and our own colliders are filtered the
        /// same way as movement. Cheap enough for per-repath use on WebGL.
        /// </summary>
        private bool HasClearLine(Vector3 from, Vector3 to)
        {
            Vector2 diff = (Vector2)(to - from);
            float dist = diff.magnitude;
            if (dist < 0.01f) return true;
            int steps = Mathf.Max(1, Mathf.CeilToInt(dist / 0.25f));
            for (int s = 1; s <= steps; s++)
            {
                float t = (float)s / steps;
                Vector2 sample = (Vector2)from + diff * t;
                if (!CanMoveTo(sample)) return false;
            }
            return true;
        }

        private void FollowPath(float speed)
        {
            if (_path == null || _path.Count == 0 || wallTilemap == null)
            {
                if (target != null) StepTowards(target.position, speed);
                return;
            }
            if (_pathIndex >= _path.Count) return;

            // Opportunistic skip: if the next anchor is already LOS-clear from
            // the current position, drop the intermediate one. Lets the enemy
            // cut corners across open rooms instead of faithfully visiting a
            // waypoint that smoothing couldn't eliminate (enemy may have moved
            // into open space since the last repath).
            if (_pathIndex + 1 < _path.Count)
            {
                Vector3 next = wallTilemap.GetCellCenterWorld(_path[_pathIndex + 1]);
                if (HasClearLine(transform.position, next))
                    _pathIndex++;
            }

            Vector3 waypoint = wallTilemap.GetCellCenterWorld(_path[_pathIndex]);
            StepTowards(waypoint, speed);
            if (((Vector2)waypoint - (Vector2)transform.position).sqrMagnitude <= arriveThreshold * arriveThreshold)
                _pathIndex++;

            // Stuck detection — wall-slide failed and path skip found nothing
            // reachable. Force a repath next frame so a fresh BFS can route
            // around whatever changed (e.g. player moved, door opened).
            if (_stuckProbeAt > 0f && Time.time - _stuckProbeAt >= StuckProbeWindow)
            {
                Vector2 here = transform.position;
                if ((here - _stuckProbePos).sqrMagnitude < StuckMoveThresholdSqr)
                    _nextRepathTime = 0f;
                _stuckProbePos = here;
                _stuckProbeAt = Time.time;
            }
        }

        private void StepTowards(Vector3 goal, float speed)
        {
            // Lightweight obstacle-aware move. Avoids the overhead of Rigidbody2D.MovePosition
            // + useFullKinematicContacts (which stutters visibly) by doing a single
            // Physics2D.OverlapBox probe at the destination. If the probe hits a non-trigger
            // collider that is not our own, we try one-axis slides so the enemy grazes walls
            // instead of freezing solid.
            Vector2 before = transform.position;
            Vector2 step = Vector2.MoveTowards(before, goal, speed * Time.deltaTime) - before;
            if (step.sqrMagnitude < 0.000001f) return;

            Vector2 applied;
            if (CanMoveTo(before + step))             applied = step;
            else if (CanMoveTo(before + new Vector2(step.x, 0f))) applied = new Vector2(step.x, 0f); // wall-slide X
            else if (CanMoveTo(before + new Vector2(0f, step.y))) applied = new Vector2(0f, step.y); // wall-slide Y
            else return; // fully blocked — stay put

            transform.position = before + applied;
            if (applied.sqrMagnitude > 0.0001f) _facing = applied.normalized;
        }

        private bool CanMoveTo(Vector2 pos)
        {
            int hits = Physics2D.OverlapBoxNonAlloc(pos, bodySize, 0f, _overlapBuf, blockerMask);
            for (int i = 0; i < hits; i++)
            {
                var c = _overlapBuf[i];
                if (c == null) continue;
                if (c.isTrigger) continue;                     // triggers never block
                if (IsOwnCollider(c)) continue;                 // don't block against ourselves
                return false;
            }
            return true;
        }

        private bool IsOwnCollider(Collider2D c)
        {
            if (_ownColliders == null) return false;
            for (int i = 0; i < _ownColliders.Length; i++)
                if (_ownColliders[i] == c) return true;
            return false;
        }

        private void TryCatchByDistance()
        {
            if (_caught) return;
            if (target == null) return;
            var st = Current;
            // Only active-hunt states can catch. Patrol / Idle / Return brush-past = safe.
            if (st != State.Chase && st != State.Locating && st != State.Alert) return;

            if (_playerController != null && _playerController.IsHidden) return; // hidden in cabinet

            float sqr = ((Vector2)target.position - (Vector2)transform.position).sqrMagnitude;
            if (sqr > catchRadius * catchRadius) return;

            _caught = true;
            PrismZone.Core.GameOverController.TriggerGameOver("caught by GREEN");
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.8f, 0.2f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, visionRange);
            Vector3 f = _facing == Vector2.zero ? Vector3.right : (Vector3)_facing;
            float half = visionAngleDeg * 0.5f;
            Quaternion l = Quaternion.AngleAxis( half, Vector3.forward);
            Quaternion r = Quaternion.AngleAxis(-half, Vector3.forward);
            Gizmos.DrawLine(transform.position, transform.position + l * f * visionRange);
            Gizmos.DrawLine(transform.position, transform.position + r * f * visionRange);
        }
    }
}
