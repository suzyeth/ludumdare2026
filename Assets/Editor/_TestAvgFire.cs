using UnityEngine;
using PrismZone.Player;
using PrismZone.Interact;
using PrismZone.UI;

public static class _TestAvgFire
{
    public static void Execute()
    {
        var inv = Inventory.Instance;
        Debug.Log($"[PICK] Inventory.Instance={(inv != null)}");
        if (inv == null) return;
        var player = GameObject.FindGameObjectWithTag("Player");
        var diary = GameObject.Find("_Interactables/Pickup_Diary");
        if (player == null || diary == null) { Debug.LogError("[PICK] missing"); return; }
        player.transform.position = diary.transform.position;
        Physics2D.SyncTransforms();
        var cf = new ContactFilter2D { useTriggers = true, useLayerMask = false };
        var buf = new Collider2D[8];
        int count = Physics2D.OverlapCircle(player.transform.position, 1.5f, cf, buf);
        Debug.Log($"[PICK] overlap={count}");
        for (int i = 0; i < count; i++)
        {
            var it = buf[i].GetComponentInParent<IInteractable>();
            if (it != null) { Debug.Log($"[PICK] Interact '{buf[i].name}'"); it.Interact(player); break; }
        }
        Debug.Log($"[PICK] Slots={inv.Slots.Count}");
        foreach (var id in inv.Slots) Debug.Log($"[PICK]   has: {id}");
    }
}
