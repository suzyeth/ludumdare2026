#if UNITY_EDITOR
using UnityEngine;
using PrismZone.Core;

public static class DiagI18n
{
    public static void Execute()
    {
        Debug.Log($"[I18n] lang={I18nManager.CurrentLang}");
        foreach (var key in new[] { "ui.pickup.prompt", "ui.cabinet.prompt", "ui.interact.prompt", "item.glasses" })
        {
            Debug.Log($"[I18n] '{key}' -> '{I18nManager.Get(key)}'");
        }

        // Try force-init
        I18nManager.Init();
        Debug.Log("[I18n] After Init()");
        foreach (var key in new[] { "ui.pickup.prompt", "ui.cabinet.prompt" })
        {
            Debug.Log($"[I18n] '{key}' -> '{I18nManager.Get(key)}'");
        }
    }
}
#endif
