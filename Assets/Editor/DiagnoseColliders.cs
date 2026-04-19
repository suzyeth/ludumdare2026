#if UNITY_EDITOR
using UnityEngine;

public static class DiagnoseColliders
{
    public static void Execute()
    {
        var player = GameObject.Find("Player");
        var walls = GameObject.Find("Tilemap_Walls");
        if (player == null || walls == null) { Debug.LogWarning("Missing Player or Tilemap_Walls"); return; }

        var pCol = player.GetComponent<Collider2D>();
        var cCol = walls.GetComponent<CompositeCollider2D>();
        var tCol = walls.GetComponent<UnityEngine.Tilemaps.TilemapCollider2D>();

        Debug.Log($"[Diag] Player pos={player.transform.position} collider bounds={pCol.bounds}");
        Debug.Log($"[Diag] Walls layer={walls.layer} ({LayerMask.LayerToName(walls.layer)})");
        Debug.Log($"[Diag] TilemapCol compositeOperation={tCol.compositeOperation}");
        Debug.Log($"[Diag] CompositeCol bounds={cCol.bounds} path count={cCol.pathCount}");
        Debug.Log($"[Diag] Is Player overlapping walls? {cCol.OverlapPoint(player.transform.position)}");
    }
}
#endif
