#if UNITY_EDITOR
using UnityEngine;
using PrismZone.Player;

public static class DiagnoseInteract
{
    public static void Execute()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        Debug.Log($"[DiagInt] Player found: {(player!=null)} pos={player?.transform.position}");

        var pi = player ? player.GetComponent<PlayerInteraction>() : null;
        Debug.Log($"[DiagInt] PlayerInteraction: {(pi!=null)} current={pi?.CurrentTarget?.GetType().Name ?? "null"}");

        foreach (var name in new[] { "Door_Passcode", "Cabinet", "Pickup_CluePaper" })
        {
            var go = GameObject.Find(name);
            if (go == null) { Debug.Log($"[DiagInt] {name}: MISSING"); continue; }
            var cols = go.GetComponents<Collider2D>();
            string colDesc = "";
            foreach (var c in cols)
                colDesc += $" [{c.GetType().Name} trigger={c.isTrigger} enabled={c.enabled} bounds={c.bounds.center}]";
            var ii = go.GetComponentInChildren<IInteractable>();
            Debug.Log($"[DiagInt] {name}: pos={go.transform.position} tag={go.tag} IInteractable={(ii!=null?ii.GetType().Name:"null")} colliders={colDesc}");
        }

        // Keyboard check
        var kb = UnityEngine.InputSystem.Keyboard.current;
        Debug.Log($"[DiagInt] Keyboard.current={(kb!=null)} eKey.isPressed={kb?.eKey.isPressed}");
    }
}
#endif
