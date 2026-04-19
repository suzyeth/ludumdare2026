#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.Tilemaps;
using PrismZone.Enemy;

public static class DiagPath
{
    public static void Execute()
    {
        var greenGo = GameObject.Find("GREEN_Watcher");
        if (greenGo == null) { Debug.LogError("No GREEN"); return; }
        var green = greenGo.GetComponent<GreenEnemy>();

        // Reflect on private wallTilemap + target
        var walls = typeof(GreenEnemy).GetField("wallTilemap", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(green) as Tilemap;
        var target = typeof(GreenEnemy).GetField("target", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(green) as Transform;
        var sightBlockers = (LayerMask)typeof(GreenEnemy).GetField("sightBlockers", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(green);
        var path = typeof(GreenEnemy).GetField("_path", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(green);

        Debug.Log($"[DiagPath] wallTilemap={(walls!=null?walls.name:"NULL")} target={(target!=null?target.name:"NULL")} sightBlockers={sightBlockers.value:X}");
        Debug.Log($"[DiagPath] state={green.Current} greenPos={greenGo.transform.position}");

        if (walls != null && target != null)
        {
            var start = walls.WorldToCell(greenGo.transform.position);
            var goal  = walls.WorldToCell(target.position);
            bool startBlocked = walls.HasTile(start);
            bool goalBlocked  = walls.HasTile(goal);
            Debug.Log($"[DiagPath] startCell={start} (blocked={startBlocked}) goalCell={goal} (blocked={goalBlocked})");

            // Test pathfinder manually
            var pf = new TilemapPathfinder(walls);
            var result = pf.FindPath(start, goal);
            Debug.Log($"[DiagPath] BFS path length={result.Count}");
            if (result.Count > 0) Debug.Log($"[DiagPath] first step={result[0]} last step={result[result.Count-1]}");
        }

        // GREEN collider
        var col = greenGo.GetComponent<Collider2D>();
        Debug.Log($"[DiagPath] GREEN collider isTrigger={col?.isTrigger}");

        // Wall layer
        int wallLayer = LayerMask.NameToLayer("Wall");
        Debug.Log($"[DiagPath] Wall layer index={wallLayer}");
    }
}
#endif
