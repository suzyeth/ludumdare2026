#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using PrismZone.Interact;
using PrismZone.UI;

/// <summary>
/// Swaps placeholder sprites for the real pixel art in Assets/Art/Sprites/.
/// Sprites import at PPU 32 so native sprite units = pixelSize/32.
/// Scene objects use local scale (1,1,1) now; collider sizes match the sprite.
/// </summary>
public static class WireRealSprites
{
    static Sprite Load(string relPath) =>
        AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/" + relPath);

    public static void Execute()
    {
        var player = Load("Characters/char_player_idle.png");
        var green  = Load("NPCs/npc_green_idle.png");
        var red    = Load("NPCs/npc_red_idle.png");      // 32x32, for future use
        var blue   = Load("NPCs/npc_blue.png");          // 24x24, for future use
        var doorC  = Load("Props/prop_door_closed.png"); // 32x64
        var doorO  = Load("Props/prop_door_open.png");
        var cabC   = Load("Props/prop_cabinet_closed.png");
        var cabO   = Load("Props/prop_cabinet_open.png");
        var glass  = Load("Items/item_glasses.png");
        var noteR  = Load("Items/item_note_red.png");
        var tileFloor = Load("Tiles/tile_floor_01.png");
        var tileWall  = Load("Tiles/tile_wall_01.png");

        // --- Player: vertical 24x48 = 0.75 x 1.5 units at PPU 32.
        var playerGo = GameObject.Find("Player");
        if (playerGo != null)
        {
            var sprT = playerGo.transform.Find("Sprite");
            if (sprT != null)
            {
                sprT.localScale = Vector3.one;
                sprT.localPosition = Vector3.zero;
                var sr = sprT.GetComponent<SpriteRenderer>();
                if (sr != null && player != null) sr.sprite = player;
            }
            var col = playerGo.GetComponent<BoxCollider2D>();
            if (col != null) col.size = new Vector2(0.75f, 1.5f);
        }

        // --- GREEN: same vertical dims
        var greenGo = GameObject.Find("GREEN_Watcher");
        if (greenGo != null)
        {
            var sprT = greenGo.transform.Find("Sprite");
            if (sprT != null)
            {
                sprT.localScale = Vector3.one;
                sprT.localPosition = Vector3.zero;
                var sr = sprT.GetComponent<SpriteRenderer>();
                if (sr != null && green != null) sr.sprite = green;
            }
            var col = greenGo.GetComponent<BoxCollider2D>();
            if (col != null) col.size = new Vector2(0.75f, 1.5f);
        }

        // --- Door: 32x64 native = 1 x 2, scale 1
        var doorGo = GameObject.Find("Door_Passcode");
        if (doorGo != null)
        {
            var sprT = doorGo.transform.Find("Sprite");
            if (sprT != null)
            {
                sprT.localScale = Vector3.one;
                var sr = sprT.GetComponent<SpriteRenderer>();
                if (sr != null && doorC != null) sr.sprite = doorC;
            }
            var pd = doorGo.GetComponent<PasscodeDoor>();
            if (pd != null)
            {
                var so = new SerializedObject(pd);
                so.FindProperty("closedSprite").objectReferenceValue = doorC;
                so.FindProperty("openSprite").objectReferenceValue = doorO;
                so.ApplyModifiedProperties();
            }
        }

        // --- Cabinet
        var cabGo = GameObject.Find("Cabinet");
        if (cabGo != null)
        {
            var sprT = cabGo.transform.Find("Sprite");
            if (sprT != null)
            {
                sprT.localScale = Vector3.one;
                var sr = sprT.GetComponent<SpriteRenderer>();
                if (sr != null && cabC != null) sr.sprite = cabC;
            }
            var cc = cabGo.GetComponent<Cabinet>();
            if (cc != null)
            {
                var so = new SerializedObject(cc);
                so.FindProperty("closedSprite").objectReferenceValue = cabC;
                so.FindProperty("openSprite").objectReferenceValue = cabO;
                so.ApplyModifiedProperties();
            }
        }

        // --- Pickup clue paper: swap in a note sprite (24x24 = 0.75 units native)
        var pkGo = GameObject.Find("Pickup_CluePaper");
        if (pkGo != null)
        {
            var sprT = pkGo.transform.Find("Sprite");
            if (sprT != null)
            {
                sprT.localScale = Vector3.one;
                var sr = sprT.GetComponent<SpriteRenderer>();
                if (sr != null && noteR != null) sr.sprite = noteR;
            }
            var col = pkGo.GetComponent<CircleCollider2D>();
            if (col != null) col.radius = 0.4f; // slightly tighter since sprite is 0.75
        }

        // --- Tilemap tiles: swap assets if the Tile assets exist
        var tWall  = AssetDatabase.LoadAssetAtPath<UnityEngine.Tilemaps.Tile>("Assets/Art/Sprites/T_Wall.asset");
        var tFloor = AssetDatabase.LoadAssetAtPath<UnityEngine.Tilemaps.Tile>("Assets/Art/Sprites/T_Floor.asset");
        if (tWall != null && tileWall != null) { tWall.sprite = tileWall; EditorUtility.SetDirty(tWall); }
        if (tFloor != null && tileFloor != null) { tFloor.sprite = tileFloor; EditorUtility.SetDirty(tFloor); }

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), "Assets/Scenes/SampleScene.unity");
        Debug.Log("[WireRealSprites] Done.");
    }
}
#endif
