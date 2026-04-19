#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using TMPro;
using PrismZone.Core;
using PrismZone.Player;
using PrismZone.Enemy;
using PrismZone.Interact;
using PrismZone.UI;

/// <summary>
/// One-shot scene assembler for Prism Zone MVP P0 smoke test.
/// Idempotent: re-running replaces created objects by name.
/// Invoke via MCP execute_script.
/// </summary>
public static class BuildScene
{
    private const string SpritesPath = "Assets/Art/Sprites";
    private static Sprite SP(string name) => AssetDatabase.LoadAssetAtPath<Sprite>($"{SpritesPath}/{name}.png");

    public static void Execute()
    {
        var scene = SceneManager.GetActiveScene();

        WireBootstrap();
        BuildRoomTilemap();
        BuildPlayer();
        BuildEnemies();
        BuildDoorAndCabinet();
        BuildPickup();
        RebuildCanvas();
        CrossWire();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[BuildScene] Done.");
    }

    // ----------------------------------------------------------
    static void WireBootstrap()
    {
        var boot = GameObject.Find("_Bootstrap");
        if (boot == null) { Debug.LogWarning("_Bootstrap missing"); return; }

        var inv = boot.GetComponent<Inventory>();
        if (inv != null)
        {
            var so = new SerializedObject(inv);
            var prop = so.FindProperty("startingItems");
            prop.arraySize = 1;
            prop.GetArrayElementAtIndex(0).stringValue = "item.glasses";
            so.ApplyModifiedProperties();
        }
    }

    // ----------------------------------------------------------
    static void BuildRoomTilemap()
    {
        // Grid + Tilemap_Walls
        var grid = GameObject.Find("Grid");
        if (grid == null)
        {
            grid = new GameObject("Grid");
            grid.AddComponent<Grid>();
        }

        var wallsGo = GameObject.Find("Tilemap_Walls");
        if (wallsGo == null)
        {
            wallsGo = new GameObject("Tilemap_Walls");
            wallsGo.transform.SetParent(grid.transform, false);
            var tm = wallsGo.AddComponent<Tilemap>();
            wallsGo.AddComponent<TilemapRenderer>();
            // Collider stack for proper wall blocking.
            var tc = wallsGo.AddComponent<TilemapCollider2D>();
            tc.compositeOperation = Collider2D.CompositeOperation.Merge;
            var rb = wallsGo.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Static;
            wallsGo.AddComponent<CompositeCollider2D>();
            var tr = wallsGo.GetComponent<TilemapRenderer>();
            tr.sortingLayerName = SortingLayerOrDefault("Mid");
            tr.sortingOrder = 0;
        }
        int wallLayer = LayerMask.NameToLayer("Wall");
        if (wallLayer >= 0) wallsGo.layer = wallLayer;

        // Ground (visual only)
        var groundGo = GameObject.Find("Tilemap_Ground");
        if (groundGo == null)
        {
            groundGo = new GameObject("Tilemap_Ground");
            groundGo.transform.SetParent(grid.transform, false);
            groundGo.AddComponent<Tilemap>();
            var gr = groundGo.AddComponent<TilemapRenderer>();
            gr.sortingLayerName = SortingLayerOrDefault("BG");
            gr.sortingOrder = -10;
        }

        // Create tile assets on disk for S_Wall and S_Floor if missing
        var wallTile = CreateTileAsset("T_Wall", SP("S_Wall"));
        var floorTile = CreateTileAsset("T_Floor", SP("S_Floor"));

        var walls = wallsGo.GetComponent<Tilemap>();
        var ground = groundGo.GetComponent<Tilemap>();

        // Draw a 24×12 room. Floor fills interior, walls ring the edge.
        walls.ClearAllTiles();
        ground.ClearAllTiles();
        int w = 24, h = 12;
        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                var cell = new Vector3Int(x - w / 2, y - h / 2, 0);
                bool edge = x == 0 || x == w - 1 || y == 0 || y == h - 1;
                if (edge) walls.SetTile(cell, wallTile);
                else      ground.SetTile(cell, floorTile);
            }
        }
        walls.RefreshAllTiles();
        ground.RefreshAllTiles();
    }

    static TileBase CreateTileAsset(string name, Sprite sprite)
    {
        string path = $"{SpritesPath}/{name}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<Tile>(path);
        if (existing != null) { existing.sprite = sprite; EditorUtility.SetDirty(existing); return existing; }
        var tile = ScriptableObject.CreateInstance<Tile>();
        tile.sprite = sprite;
        AssetDatabase.CreateAsset(tile, path);
        return tile;
    }

    // ----------------------------------------------------------
    static void BuildPlayer()
    {
        var go = EnsureGO("Player", new Vector3(-2, 0, 0), tag: "Player");

        var rb = EnsureComponent<Rigidbody2D>(go);
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.linearDamping = 6f;

        var col = EnsureComponent<BoxCollider2D>(go);
        col.size = new Vector2(0.75f, 1.5f);  // vertical player (24x48 @ PPU 32)

        var sr = go.transform.Find("Sprite")?.GetComponent<SpriteRenderer>();
        if (sr == null) sr = CreateSpriteChild(go, "Sprite", SP("S_Player"), "PlayerNPC", 0,
                                               scale: Vector3.one);

        var ctl = EnsureComponent<PlayerController>(go);
        SerializedSet(ctl, "spriteRenderer", sr);
        SerializedSet(ctl, "walkSpeed", 3f);
        SerializedSet(ctl, "runSpeed", 5f);
        SerializedSet(ctl, "climbSpeed", 2.5f);
        SerializedSet(ctl, "pixelsPerUnit", 32f);
        SerializedSet(ctl, "runNoiseInterval", 0.25f);
        SerializedSet(ctl, "runNoiseRadius", 8f);

        var pi = EnsureComponent<PlayerInteraction>(go);
        SerializedSet(pi, "interactRadius", 1.5f);
    }

    // ----------------------------------------------------------
    static void BuildEnemies()
    {
        // MVP uses a single GREEN tracker. RED / BLUE scripts live in
        // Assets/Scripts/Enemy/ — drop them in by hand if you want to re-enable them.

        // GREEN
        var green = EnsureGO("GREEN_Watcher", new Vector3(8, -3, 0), tag: "Enemy");
        if (green.transform.Find("Sprite") == null)
            CreateSpriteChild(green, "Sprite", SP("S_Green"), "PlayerNPC", 1, Vector3.one);
        var grb = EnsureComponent<Rigidbody2D>(green);
        grb.gravityScale = 0f;
        grb.bodyType = RigidbodyType2D.Kinematic;
        var gcol = EnsureComponent<BoxCollider2D>(green);
        gcol.size = new Vector2(0.75f, 1.5f);  // vertical GREEN (24x48)
        gcol.isTrigger = true;  // No physical push on Player; catch logic will use OnTriggerEnter

        // Waypoints (optional — if user deleted them, GREEN stays Idle until chase)
        var wp1 = GameObject.Find("WP_1");
        var wp2 = GameObject.Find("WP_2");

        var greenAi = EnsureComponent<GreenEnemy>(green);
        SerializedSet(greenAi, "revealFilter", (int)FilterColor.Red);
        SerializedSet(greenAi, "patrolSpeed", 1.5f);
        SerializedSet(greenAi, "chaseSpeed", 3f);
        // MVP single-tracker tuning: omni aggro radius, long pursuit.
        SerializedSet(greenAi, "visionRange", 10f);
        SerializedSet(greenAi, "visionAngleDeg", 360f);
        SerializedSet(greenAi, "aggroLossTime", 30f);
        SerializedSet(greenAi, "hiddenAlpha", 0.35f);
        SerializedSet(greenAi, "repathInterval", 0.4f);
        SerializedSet(greenAi, "sightBlockers", (LayerMask)(1 << LayerMask.NameToLayer("Wall")));
        var wallsTm = GameObject.Find("Tilemap_Walls").GetComponent<Tilemap>();
        SerializedSet(greenAi, "wallTilemap", wallsTm);
        SerializedSet(greenAi, "spriteRenderer", green.transform.GetChild(0).GetComponent<SpriteRenderer>());
        // Waypoints array — only keep non-null (user may have deleted them)
        var so = new SerializedObject(greenAi);
        var wpProp = so.FindProperty("waypoints");
        int wpCount = 0;
        if (wp1 != null) wpCount++;
        if (wp2 != null) wpCount++;
        wpProp.arraySize = wpCount;
        int idx = 0;
        if (wp1 != null) wpProp.GetArrayElementAtIndex(idx++).objectReferenceValue = wp1.transform;
        if (wp2 != null) wpProp.GetArrayElementAtIndex(idx++).objectReferenceValue = wp2.transform;
        so.ApplyModifiedProperties();

        /* BLUE disabled for MVP — see note at the top of BuildEnemies().
        var blue = GameObject.Find("BLUE_Weaver");
        if (blue != null) Object.DestroyImmediate(blue);
        blue = new GameObject("BLUE_Weaver");
        blue.tag = "Trap";
        blue.transform.position = new Vector3(-4, -3, 0);
        CreateSpriteChild(blue, "Sprite", SP("S_Blue"), "Traps", 0, new Vector3(0.75f, 0.75f, 1f));
        var ccol = blue.AddComponent<CircleCollider2D>();
        ccol.isTrigger = true;
        ccol.radius = 0.5f;
        var blueAi = blue.AddComponent<BlueTrap>();
        SerializedSet(blueAi, "revealFilter", (int)FilterColor.Green);
        SerializedSet(blueAi, "alarmDuration", 10f);
        SerializedSet(blueAi, "retriggerCooldown", 3f);
        SerializedSet(blueAi, "spriteRenderer", blue.transform.GetChild(0).GetComponent<SpriteRenderer>());
        */
    }

    // ----------------------------------------------------------
    static void BuildDoorAndCabinet()
    {
        // Door — idempotent: keeps user position if GO already exists
        var door = EnsureGO("Door_Passcode", new Vector3(8, -3, 0), tag: "Door");
        var doorSrT = door.transform.Find("Sprite");
        SpriteRenderer doorSr = doorSrT ? doorSrT.GetComponent<SpriteRenderer>() : null;
        if (doorSr == null) doorSr = CreateSpriteChild(door, "Sprite", SP("S_Door"), "Mid", 2, new Vector3(1, 2, 1));

        // Ensure exactly two BoxCollider2D (one blocking, one trigger)
        var cols = door.GetComponents<BoxCollider2D>();
        BoxCollider2D blocking = cols.Length > 0 ? cols[0] : door.AddComponent<BoxCollider2D>();
        blocking.size = new Vector2(1, 2);
        blocking.isTrigger = false;
        BoxCollider2D triggerCol = cols.Length > 1 ? cols[1] : door.AddComponent<BoxCollider2D>();
        triggerCol.size = new Vector2(1.5f, 2.5f);
        triggerCol.isTrigger = true;

        var pd = EnsureComponent<PasscodeDoor>(door);
        SerializedSet(pd, "passcode", "3071");
        SerializedSet(pd, "blockingCollider", blocking);
        SerializedSet(pd, "doorRenderer", doorSr);
        SerializedSet(pd, "closedSprite", SP("S_Door"));
        SerializedSet(pd, "openSprite", SP("S_Floor"));

        // Cabinet — idempotent
        var cab = EnsureGO("Cabinet", new Vector3(-6, -3, 0), tag: "Cabinet");
        var cabSrT = cab.transform.Find("Sprite");
        SpriteRenderer cabSr = cabSrT ? cabSrT.GetComponent<SpriteRenderer>() : null;
        if (cabSr == null) cabSr = CreateSpriteChild(cab, "Sprite", SP("S_Cabinet"), "Mid", 2, new Vector3(1, 2, 1));

        var cCol = EnsureComponent<BoxCollider2D>(cab);
        cCol.size = new Vector2(1.2f, 2.2f);
        cCol.isTrigger = true;

        var anchorT = cab.transform.Find("HideAnchor");
        if (anchorT == null)
        {
            var anchor = new GameObject("HideAnchor");
            anchor.transform.SetParent(cab.transform, false);
            anchor.transform.localPosition = new Vector3(0, -0.3f, 0);
            anchorT = anchor.transform;
        }

        var cc = EnsureComponent<Cabinet>(cab);
        SerializedSet(cc, "hideAnchor", anchorT);
        SerializedSet(cc, "doorRenderer", cabSr);
        SerializedSet(cc, "closedSprite", SP("S_Cabinet"));
        SerializedSet(cc, "openSprite", SP("S_Floor"));
    }

    // ----------------------------------------------------------
    static void BuildPickup()
    {
        // Idempotent — keep user position if exists
        var pk = EnsureGO("Pickup_CluePaper", new Vector3(-3, 2, 0), tag: "Interactable");
        var srT = pk.transform.Find("Sprite");
        // Bright cream sprite so it's visible on the dark floor (was S_Floor = invisible).
        if (srT == null)
            CreateSpriteChild(pk, "Sprite", SP("S_Player"), "Mid", 3, new Vector3(0.6f, 0.6f, 1f));
        else
        {
            var sr = srT.GetComponent<SpriteRenderer>();
            if (sr != null) sr.sprite = SP("S_Player");
        }
        var col = EnsureComponent<CircleCollider2D>(pk);
        col.isTrigger = true;
        col.radius = 0.5f;
        var pu = EnsureComponent<Pickup>(pk);
        SerializedSet(pu, "itemId", "");
        SerializedSet(pu, "clueTextKey", "clue.room1.code");
        SerializedSet(pu, "promptKey", "ui.pickup.prompt");
        SerializedSet(pu, "destroyOnPickup", true);
        SerializedSet(pu, "oneShotClue", true);
    }

    // ----------------------------------------------------------
    static void RebuildCanvas()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            canvas = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        }
        // Force Overlay every run — ScreenSpaceCamera breaks when combined with the
        // FullScreen filter pass (UI gets filtered too or rendered behind the scene).
        canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(640, 360);
        scaler.matchWidthOrHeight = 0.5f;

        // Nuke old template buttons
        for (int i = canvas.transform.childCount - 1; i >= 0; i--)
        {
            var child = canvas.transform.GetChild(i).gameObject;
            if (child.name.Contains("Button") || child.name == "FilterHUD" ||
                child.name == "InventoryHUD" || child.name == "PasscodePanel" ||
                child.name == "CluePopup")
            {
                Object.DestroyImmediate(child);
            }
        }

        BuildFilterHUD(canvas);
        BuildInventoryHUD(canvas);
        BuildPasscodePanel(canvas);
        BuildCluePopup(canvas);
        BuildInteractPrompt(canvas);
    }

    static void BuildInteractPrompt(GameObject canvas)
    {
        var go = new GameObject("InteractPrompt", typeof(RectTransform));
        go.transform.SetParent(canvas.transform, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0, 56);
        rt.sizeDelta = new Vector2(200, 28);

        var bg = go.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.7f);

        var label = MakeText(go, "Label", Vector2.zero, new Vector2(200, 28), "[E]", 14);
        label.alignment = TextAlignmentOptions.Center;

        var prompt = go.AddComponent<InteractPrompt>();
        SerializedSet(prompt, "label", label);
        SerializedSet(prompt, "background", bg);
        // Do NOT SetActive(false) — Update() would never run. Visibility is driven by
        // CanvasGroup.alpha inside InteractPrompt.Update.
    }

    static void BuildFilterHUD(GameObject canvas)
    {
        var go = new GameObject("FilterHUD", typeof(RectTransform));
        go.transform.SetParent(canvas.transform, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0, 0);
        rt.pivot = new Vector2(0, 0);
        rt.anchoredPosition = new Vector2(16, 16);
        rt.sizeDelta = new Vector2(140, 30);

        var noneDot  = MakeDot(go, "NoneDot",  new Vector2(0,   0), new Color(0.4f, 0.4f, 0.4f));
        var redDot   = MakeDot(go, "RedDot",   new Vector2(24,  0), Hex("#E53935"));
        var greenDot = MakeDot(go, "GreenDot", new Vector2(48,  0), Hex("#43A047"));
        var blueDot  = MakeDot(go, "BlueDot",  new Vector2(72,  0), Hex("#1E88E5"));

        var labelGo = new GameObject("Label", typeof(RectTransform));
        labelGo.transform.SetParent(go.transform, false);
        var lrt = (RectTransform)labelGo.transform;
        lrt.anchorMin = lrt.anchorMax = new Vector2(0, 0);
        lrt.pivot = new Vector2(0, 0);
        lrt.anchoredPosition = new Vector2(96, 2);
        lrt.sizeDelta = new Vector2(60, 20);
        var txt = labelGo.AddComponent<TextMeshProUGUI>();
        txt.fontSize = 14;
        txt.text = "";
        txt.color = Color.white;

        var hud = go.AddComponent<FilterHUD>();
        SerializedSet(hud, "noneDot", noneDot);
        SerializedSet(hud, "redDot", redDot);
        SerializedSet(hud, "greenDot", greenDot);
        SerializedSet(hud, "blueDot", blueDot);
        SerializedSet(hud, "label", txt);
        SerializedSet(hud, "mutedAlpha", 0.25f);
        SerializedSet(hud, "activeAlpha", 1.0f);
    }

    static Image MakeDot(GameObject parent, string name, Vector2 pos, Color c)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent.transform, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0, 0);
        rt.pivot = new Vector2(0, 0);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(20, 20);
        var img = go.AddComponent<Image>();
        img.color = c;
        return img;
    }

    static void BuildInventoryHUD(GameObject canvas)
    {
        var go = new GameObject("InventoryHUD", typeof(RectTransform));
        go.transform.SetParent(canvas.transform, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(1, 0);
        rt.pivot = new Vector2(1, 0);
        rt.anchoredPosition = new Vector2(-16, 16);
        rt.sizeDelta = new Vector2(160, 24);

        var slotViews = new List<(Image icon, TMP_Text label)>();
        for (int i = 0; i < Inventory.MaxSlots; i++)
        {
            var slot = new GameObject($"Slot{i}", typeof(RectTransform));
            slot.transform.SetParent(go.transform, false);
            var srt = (RectTransform)slot.transform;
            srt.anchorMin = srt.anchorMax = new Vector2(0, 0);
            srt.pivot = new Vector2(0, 0);
            srt.anchoredPosition = new Vector2(i * 40, 0);
            srt.sizeDelta = new Vector2(36, 24);
            var img = slot.AddComponent<Image>();
            img.color = new Color(0.15f, 0.15f, 0.15f, 0.7f);

            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(slot.transform, false);
            var lrt = (RectTransform)labelGo.transform;
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
            var t = labelGo.AddComponent<TextMeshProUGUI>();
            t.fontSize = 10;
            t.alignment = TextAlignmentOptions.Center;
            t.text = "";
            slotViews.Add((img, t));
        }

        var hud = go.AddComponent<InventoryHUD>();
        var so = new SerializedObject(hud);
        var slotsProp = so.FindProperty("slots");
        slotsProp.arraySize = slotViews.Count;
        for (int i = 0; i < slotViews.Count; i++)
        {
            var elem = slotsProp.GetArrayElementAtIndex(i);
            elem.FindPropertyRelative("icon").objectReferenceValue = slotViews[i].icon;
            elem.FindPropertyRelative("label").objectReferenceValue = slotViews[i].label;
        }
        so.ApplyModifiedProperties();
    }

    static void BuildPasscodePanel(GameObject canvas)
    {
        var go = new GameObject("PasscodePanel", typeof(RectTransform));
        go.transform.SetParent(canvas.transform, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(240, 260);
        var bg = go.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.8f);

        // Title
        var title = MakeText(go, "Title", new Vector2(0, 110), new Vector2(220, 24), "Enter code", 14);
        title.alignment = TextAlignmentOptions.Center;

        // Display
        var display = MakeText(go, "Display", new Vector2(0, 80), new Vector2(220, 30), "_ _ _ _", 20);
        display.alignment = TextAlignmentOptions.Center;

        // Digit buttons 3×4 grid (1-9 then 0)
        var digitBtns = new Button[10];
        for (int i = 0; i < 10; i++)
        {
            int digit = (i == 9) ? 0 : i + 1;
            int col = i % 3;
            int row = (i == 9) ? 3 : i / 3;
            var pos = new Vector2(-50 + col * 50, 40 - row * 40);
            digitBtns[digit] = MakeButton(go, $"Btn_{digit}", pos, new Vector2(40, 32), digit.ToString());
        }
        var clearBtn = MakeButton(go, "Btn_Clear", new Vector2(0, -100), new Vector2(70, 32), "Clear");
        var submitBtn = MakeButton(go, "Btn_Submit", new Vector2(75, -100), new Vector2(70, 32), "OK");
        var closeBtn = MakeButton(go, "Btn_Close", new Vector2(100, 115), new Vector2(28, 28), "X");

        var errLabel = MakeText(go, "Error", new Vector2(0, -140), new Vector2(220, 24), "", 12);
        errLabel.color = new Color(1f, 0.4f, 0.4f);
        errLabel.alignment = TextAlignmentOptions.Center;

        var panel = go.AddComponent<PasscodePanel>();
        SerializedSet(panel, "titleLabel", title);
        SerializedSet(panel, "displayLabel", display);
        SerializedSet(panel, "errorLabel", errLabel);
        var so = new SerializedObject(panel);
        var btnProp = so.FindProperty("digitButtons");
        btnProp.arraySize = 10;
        for (int i = 0; i < 10; i++)
            btnProp.GetArrayElementAtIndex(i).objectReferenceValue = digitBtns[i];
        so.ApplyModifiedProperties();
        SerializedSet(panel, "clearButton", clearBtn);
        SerializedSet(panel, "submitButton", submitBtn);
        SerializedSet(panel, "closeButton", closeBtn);
        // Do NOT SetActive(false) — OnEnable wiring would disconnect. Visibility
        // is driven by CanvasGroup.alpha inside PasscodePanel.
    }

    static void BuildCluePopup(GameObject canvas)
    {
        var go = new GameObject("CluePopup", typeof(RectTransform));
        go.transform.SetParent(canvas.transform, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0, 40);
        rt.sizeDelta = new Vector2(300, 100);
        var bg = go.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.85f);

        var body = MakeText(go, "Body", new Vector2(0, 10), new Vector2(280, 60), "", 14);
        body.alignment = TextAlignmentOptions.Center;

        var hint = MakeText(go, "Hint", new Vector2(0, -32), new Vector2(280, 20), "[E]", 10);
        hint.alignment = TextAlignmentOptions.Center;
        hint.color = new Color(0.7f, 0.7f, 0.7f);

        var popup = go.AddComponent<CluePopup>();
        SerializedSet(popup, "body", body);
        // Stay active; CanvasGroup drives visibility.
    }

    static TMP_Text MakeText(GameObject parent, string name, Vector2 pos, Vector2 size, string text, float fontSize)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent.transform, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = fontSize;
        t.color = Color.white;
        return t;
    }

    static Button MakeButton(GameObject parent, string name, Vector2 pos, Vector2 size, string label)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent.transform, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        var img = go.AddComponent<Image>();
        img.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);
        var btn = go.AddComponent<Button>();
        var target = new ColorBlock { normalColor = Color.white, highlightedColor = new Color(1,1,0.8f), pressedColor = new Color(0.8f,0.8f,0.8f), selectedColor = Color.white, disabledColor = Color.gray, colorMultiplier = 1, fadeDuration = 0.1f };
        btn.colors = target;
        btn.targetGraphic = img;

        var labelGo = new GameObject("Label", typeof(RectTransform));
        labelGo.transform.SetParent(go.transform, false);
        var lrt = (RectTransform)labelGo.transform;
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
        var t = labelGo.AddComponent<TextMeshProUGUI>();
        t.alignment = TextAlignmentOptions.Center;
        t.fontSize = 14;
        t.text = label;
        t.color = Color.white;
        return btn;
    }

    // ----------------------------------------------------------
    static void CrossWire()
    {
        // Player is now in scene; enemy target auto-find handles it at Awake, but
        // explicit wiring cuts warning/missed edge cases.
        var player = GameObject.Find("Player");
        if (player == null) return;

        var green = GameObject.Find("GREEN_Watcher")?.GetComponent<GreenEnemy>();
        if (green != null) SerializedSet(green, "target", player.transform);
    }

    // ----------------------------------------------------------
    /// <summary>Find-or-create. If the GO exists, keeps its transform. Otherwise creates at defaultPos.</summary>
    static GameObject EnsureGO(string name, Vector3 defaultPos, string tag = null)
    {
        var go = GameObject.Find(name);
        if (go == null)
        {
            go = new GameObject(name);
            go.transform.position = defaultPos;
        }
        if (!string.IsNullOrEmpty(tag) && go.tag != tag) go.tag = tag;
        return go;
    }

    /// <summary>Ensure this GO has a component of T; add one if missing, return it either way.</summary>
    static T EnsureComponent<T>(GameObject go) where T : Component
    {
        var c = go.GetComponent<T>();
        if (c == null) c = go.AddComponent<T>();
        return c;
    }

    static SpriteRenderer CreateSpriteChild(GameObject parent, string name, Sprite sprite, string sortingLayer, int order, Vector3 scale)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localScale = scale;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerName = SortingLayerOrDefault(sortingLayer);
        sr.sortingOrder = order;
        return sr;
    }

    static string SortingLayerOrDefault(string want)
    {
        foreach (var l in SortingLayer.layers)
            if (l.name == want) return want;
        return "Default";
    }

    static void SerializedSet(Object obj, string field, object value)
    {
        var so = new SerializedObject(obj);
        var prop = so.FindProperty(field);
        if (prop == null) { Debug.LogWarning($"Missing field '{field}' on {obj.GetType().Name}"); return; }
        switch (value)
        {
            case bool b: prop.boolValue = b; break;
            case float f: prop.floatValue = f; break;
            case int i: prop.intValue = i; break;
            case string s: prop.stringValue = s; break;
            case LayerMask lm: prop.intValue = lm.value; break;
            case Object o: prop.objectReferenceValue = o; break;
            case Color c: prop.colorValue = c; break;
            case Vector2 v2: prop.vector2Value = v2; break;
            case Vector3 v3: prop.vector3Value = v3; break;
            default:
                if (value is System.Enum e) prop.intValue = System.Convert.ToInt32(e);
                else Debug.LogWarning($"Unhandled type for {field}: {value?.GetType()}");
                break;
        }
        so.ApplyModifiedProperties();
    }

    static Color Hex(string s) { ColorUtility.TryParseHtmlString(s, out var c); return c; }
}
#endif
