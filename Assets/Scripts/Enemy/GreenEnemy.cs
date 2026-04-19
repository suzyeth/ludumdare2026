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
        }

        private Collider2D[] _ownColliders;
        private static readonly Collider2D[] _overlapBuf = new Collider2D[4];

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

        protected override void Tick()
        {
            if (CanSeePlayer())
            {
                _lostSightAt = -1f;
                SetState(State.Chase);
            }

            switch (Current)
            {
                case State.Idle:
                case State.Patrol: TickPatrol(); break;
                case State.Chase:  TickChase();  break;
                case State.Return: TickReturn(); break;
            }
        }

        private bool CanSeePlayer()
        {
            if (target == null) return false;
            // Hiding in a cabinet = invisible to sight-based enemies (guards, GREEN).
            // Sound-based RED still hears running noise via PlayerNoise pulse, which is
            // suppressed elsewhere while hiding (Rigidbody velocity = 0).
            var pc = target.GetComponent<PrismZone.Player.PlayerController>();
            if (pc != null && pc.IsHidden) return false;

            Vector2 to = (Vector2)target.position - (Vector2)transform.position;
            float dist = to.magnitude;
            if (dist > visionRange) return false;

            Vector2 dir = dist > 0.0001f ? to / dist : _facing;
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

        private void TickChase()
        {
            if (target == null) { SetState(State.Return); return; }

            if (CanSeePlayer()) _lostSightAt = -1f;
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

        private void TickReturn()
        {
            if (waypoints == null || waypoints.Length == 0) { SetState(State.Idle); return; }
            Transform closest = waypoints[0];
            float best = float.PositiveInfinity;
            for (int i = 0; i < waypoints.Length; i++)
            {
                if (waypoints[i] == null) continue;
                float d = ((Vector2)waypoints[i].position - (Vector2)transform.position).sqrMagnitude;
                if (d < best) { best = d; closest = waypoints[i]; }
            }
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
            _path = _pathfinder.FindPath(start, goal);
            _pathIndex = 0;
        }

        private void FollowPath(float speed)
        {
            if (_path == null || _path.Count == 0 || wallTilemap == null)
            {
                if (target != null) StepTowards(target.position, speed);
                return;
            }
            if (_pathIndex >= _path.Count) return;
            Vector3 waypoint = wallTilemap.GetCellCenterWorld(_path[_pathIndex]);
            StepTowards(waypoint, speed);
            if (((Vector2)waypoint - (Vector2)transform.position).sqrMagnitude <= arriveThreshold * arriveThreshold)
                _pathIndex++;
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

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (Current != State.Chase) return; // only kill during active pursuit
            if (!other.CompareTag("Player")) return;
            var pc = other.GetComponent<PrismZone.Player.PlayerController>();
            if (pc != null && pc.IsHidden) return;
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
