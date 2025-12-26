using HarmonyLib;
using UnityEngine;

[HarmonyPatch(typeof(AutocannonArmTip), "OnUpdate")]
public class AutocannonArmTipPatch
{
    static float startTime = -1f;

    static void Prefix(AutocannonArmTip __instance, ref Vector3 position, ref Quaternion rotation)
    {
        var traverse = Traverse.Create(__instance);
        bool isFiring = traverse.Field("isFiring").GetValue<bool>();
        if (isFiring && startTime < 0f) startTime = Time.time;
        if (!isFiring) startTime = -1f;
        if (startTime >= 0f)
        {
            float elapsed = Time.time - startTime;
            float multiplier = Mathf.Max(1f, 6f - elapsed);
            Vector2 currentSpread = traverse.Field("bulletSpread").GetValue<Vector2>();
            traverse.Field("bulletSpread").SetValue(currentSpread * multiplier);
        }
    }

    static void Postfix(AutocannonArmTip __instance, ref Vector3 position, ref Quaternion rotation)
    {
        if (startTime < 0f) return;
        var traverse = Traverse.Create(__instance);
        bool isFiring = traverse.Field("isFiring").GetValue<bool>();
        if (isFiring)
        {
            float elapsed = Time.time - startTime;
            float multiplier = Mathf.Max(1f, 6f - elapsed);
            Vector2 currentSpread = traverse.Field("bulletSpread").GetValue<Vector2>();
            traverse.Field("bulletSpread").SetValue(currentSpread / multiplier);
        }
    }
}
