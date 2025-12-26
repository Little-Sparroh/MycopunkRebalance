using HarmonyLib;
using UnityEngine;

[HarmonyPatch(typeof(SwarmGun), "OnActiveUpdate")]
public class SwarmGun_OnActiveUpdate_Patch
{
    static bool Prefix(SwarmGun __instance, float ___lastAutoRegenAmmoTime)
    {
        float time = Time.time;
        var swarmData = __instance.SwarmData;

        if (swarmData.autoAmmoRegenInterval > 0f && time - __instance.LastFireTime >= 1f && __instance.HoveringBullets.Count > 0 && time - ___lastAutoRegenAmmoTime > swarmData.autoAmmoRegenInterval)
        {
            __instance.StoredAmmo = Mathf.Min(__instance.StoredAmmo + 1, __instance.GunData.ammoCapacity);
            typeof(SwarmGun).GetField("lastAutoRegenAmmoTime", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(__instance, time);
            return false;
        }
        return true;
    }
}
