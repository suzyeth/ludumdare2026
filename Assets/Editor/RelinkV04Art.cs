#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using PrismZone.Core;
using PrismZone.Interact;

/// <summary>
/// Point all prefab/tile/ItemData sprite references at the v0.4 art batch
/// (char_player_*_front / npc_guard_*_front / prop_door_passcode_*_front / etc.)
/// Run this once in Unity after the v0.4 art replacement.
///
/// Menu: Tools > Prism Zone > Relink v0.4 Art
/// </summary>
public static class RelinkV04Art
{
    [MenuItem("Tools/Prism Zone/Relink v0.4 Art")]
    public static void Execute()
    {
        // 0. Batch-fix importer settings first (PPU 32 / Point / Uncompressed / no mipmap).
        //    Newly-dropped files default to PPU 100 Bilinear Compressed — that makes a
        //    32×48 character render at 0.32×0.48 units and look blurry.
        FixSpriteImporters.Execute();

        int touched = 0;

        // --- Sprites we want to bind
        var playerIdle = Load("Characters/char_player_idle_front.png");
        var guardIdle  = Load("NPCs/npc_guard_idle_front.png");
        var doorPassClosed = Load("Props/prop_door_passcode_closed_front.png");
        var doorPassOpen   = Load("Props/prop_door_passcode_open_front.png");
        var doorKeyClosed  = Load("Props/prop_door_key_closed_front.png");
        var doorKeyOpen    = Load("Props/prop_door_key_open_front.png");
        var cabinetClosed  = Load("Props/prop_cabinet_closed.png");
        var cabinetOpen    = Load("Props/prop_cabinet_open.png");
        var stairs         = Load("Props/prop_stairs_front.png");
        var glasses        = Load("Items/item_glasses.png");
        var keyA           = Load("Items/item_key_a.png");
        var tileFloor      = Load("Tiles/tile_floor_classroom.png");
        var tileWall       = Load("Tiles/tile_wall_front.png");

        // --- Prefabs: Player, GREEN_Watcher (now treated as the Guard), RED/BLUE placeholders
        touched += RelinkPrefabSprite("Player", "Sprite", playerIdle);
        touched += RelinkPrefabSprite("GREEN_Watcher", "Sprite", guardIdle);
        touched += RelinkPrefabSprite("RED_Shrike", "Sprite", guardIdle);   // placeholder until reserve art
        touched += RelinkPrefabSprite("BLUE_Weaver", "Sprite", guardIdle);  // placeholder
        touched += RelinkPrefabSprite("Pickup_CluePaper", "Sprite", keyA);

        // Door_Passcode: both the SR and the PasscodeDoor closed/open refs
        touched += RelinkPrefabSprite("Door_Passcode", "Sprite", doorPassClosed);
        touched += RelinkPasscodeDoorSprites(doorPassClosed, doorPassOpen);

        // Cabinet: SR + Cabinet closed/open
        touched += RelinkPrefabSprite("Cabinet", "Sprite", cabinetClosed);
        touched += RelinkCabinetSprites(cabinetClosed, cabinetOpen);

        // --- Tile assets
        touched += SetTileSprite("Assets/Art/Sprites/T_Wall.asset", tileWall);
        touched += SetTileSprite("Assets/Art/Sprites/T_Floor.asset", tileFloor);

        // --- ItemData (in case any pointed to deleted files)
        touched += FixItemData("Item_Glasses", glasses);
        touched += FixItemData("Item_KeyA", keyA);
        touched += FixItemData("Item_Diary", null);        // no big sprite yet — leave null
        touched += FixItemData("Item_Recorder", null);
        touched += FixItemData("Item_LoveLetter", null);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[Relink] Touched {touched} sprite references.");
    }

    static Sprite Load(string rel) =>
        AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/" + rel);

    static int RelinkPrefabSprite(string prefabName, string spriteChildName, Sprite sprite)
    {
        if (sprite == null) { Debug.LogWarning($"[Relink] sprite null for {prefabName}"); return 0; }
        string path = $"Assets/Prefabs/{prefabName}.prefab";
        var contents = PrefabUtility.LoadPrefabContents(path);
        if (contents == null) { Debug.LogWarning($"[Relink] missing {path}"); return 0; }
        try
        {
            var child = contents.transform.Find(spriteChildName);
            if (child == null) { Debug.LogWarning($"[Relink] {prefabName} has no '{spriteChildName}' child"); return 0; }
            // Reset local scale to 1 — native sprite size @ PPU 32 now gives the right world units
            // (32×48 native = 1×1.5 units). Old prefabs may still carry 0.75×1.5 scale from the vertical-art era.
            child.localScale = Vector3.one;
            child.localPosition = Vector3.zero;
            var sr = child.GetComponent<SpriteRenderer>();
            if (sr == null) { Debug.LogWarning($"[Relink] {prefabName}/{spriteChildName} has no SpriteRenderer"); return 0; }
            sr.sprite = sprite;
            PrefabUtility.SaveAsPrefabAsset(contents, path);
            return 1;
        }
        finally { PrefabUtility.UnloadPrefabContents(contents); }
    }

    static int RelinkPasscodeDoorSprites(Sprite closed, Sprite open)
    {
        string path = "Assets/Prefabs/Door_Passcode.prefab";
        var contents = PrefabUtility.LoadPrefabContents(path);
        if (contents == null) return 0;
        try
        {
            var door = contents.GetComponent<PasscodeDoor>();
            if (door == null) return 0;
            var so = new SerializedObject(door);
            if (closed != null) so.FindProperty("closedSprite").objectReferenceValue = closed;
            if (open != null) so.FindProperty("openSprite").objectReferenceValue = open;
            so.ApplyModifiedProperties();
            PrefabUtility.SaveAsPrefabAsset(contents, path);
            return 1;
        }
        finally { PrefabUtility.UnloadPrefabContents(contents); }
    }

    static int RelinkCabinetSprites(Sprite closed, Sprite open)
    {
        string path = "Assets/Prefabs/Cabinet.prefab";
        var contents = PrefabUtility.LoadPrefabContents(path);
        if (contents == null) return 0;
        try
        {
            var cc = contents.GetComponent<Cabinet>();
            if (cc == null) return 0;
            var so = new SerializedObject(cc);
            if (closed != null) so.FindProperty("closedSprite").objectReferenceValue = closed;
            if (open != null) so.FindProperty("openSprite").objectReferenceValue = open;
            so.ApplyModifiedProperties();
            PrefabUtility.SaveAsPrefabAsset(contents, path);
            return 1;
        }
        finally { PrefabUtility.UnloadPrefabContents(contents); }
    }

    static int SetTileSprite(string path, Sprite sprite)
    {
        if (sprite == null) return 0;
        var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
        if (tile == null) { Debug.LogWarning($"[Relink] missing tile {path}"); return 0; }
        tile.sprite = sprite;
        EditorUtility.SetDirty(tile);
        return 1;
    }

    static int FixItemData(string soName, Sprite fallbackIcon)
    {
        string path = $"Assets/Resources/Items/{soName}.asset";
        var item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
        if (item == null) return 0;
        var so = new SerializedObject(item);
        var worldProp = so.FindProperty("worldIcon");
        var bigProp = so.FindProperty("bigIcon");
        bool changed = false;
        // Only replace if current reference is missing (null after broken GUID)
        if (worldProp.objectReferenceValue == null && fallbackIcon != null)
        { worldProp.objectReferenceValue = fallbackIcon; changed = true; }
        if (bigProp.objectReferenceValue == null && fallbackIcon != null)
        { bigProp.objectReferenceValue = fallbackIcon; changed = true; }
        if (changed) { so.ApplyModifiedProperties(); EditorUtility.SetDirty(item); return 1; }
        return 0;
    }
}
#endif
