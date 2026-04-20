namespace PrismZone.Core
{
    /// <summary>
    /// Every gameplay sound event the game triggers. Add new entries here, then
    /// bind an AudioClip in the SoundCatalog ScriptableObject.
    /// Designer-facing: these IDs appear in the catalog's inspector as enum labels.
    /// </summary>
    public enum SoundId
    {
        None = 0,

        // Filter / vision
        FilterSwap = 10,
        FilterOff = 11,

        // Player
        Footstep = 20,
        Run = 21,
        Hide = 22,            // 进柜子布料声 (soft_cloth_rustle)
        HideLeave = 23,
        CabinetOpen = 24,     // 柜门打开吱呀
        CabinetClose = 25,    // 柜门关上咔哒

        // Interact
        Pickup = 30,
        KeyGrab = 31,
        PopupOpen = 32,
        PopupClose = 33,
        UIClick = 34,
        DoorUnlock = 40,
        DoorLocked = 41,
        DoorOpen = 42,        // 教室木门 (old_wooden_door_open)
        Stairs = 43,
        GateOpen = 44,        // 楼梯过场门 (四楼入口 / 一楼出口)

        // Enemy
        GuardSpot = 50,
        GuardAttack = 51,
        GuardAlarm = 52,
        GuardFootstep = 53,   // 保安沉重脚步 (heavy_boss_footsteps)

        // v1.2 broadcast / recorder / flashback
        BroadcastPrelude = 60,
        BroadcastLoop = 61,
        RecorderClick = 62,
        Heartbeat = 63,
        FlashEcho = 64,
        HumanBodyFall = 65,   // 闪回撞击 / 重物落地 (humanBodyFall.mp3)

        // Game state
        GameOver = 90,
        Victory = 91,

        // Music
        BgmMenu = 100,
        BgmAmbient = 101,
        BgmChase = 102,
    }
}
