using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using System.Reflection;
using Pigeon.Movement;

public static class SprintToFireFixPatches
{
    private static readonly FieldInfo playerField = AccessTools.Field(typeof(Gun), "player");
    private static readonly FieldInfo gunDataField = AccessTools.Field(typeof(Gun), "gunData");
    private static readonly FieldInfo isFireInputHeldField = AccessTools.Field(typeof(Gun), "isFireInputHeld");
    private static readonly MethodInfo tryFireMethod = AccessTools.Method(typeof(Gun), "TryFire");
    private static readonly PropertyInfo canFireWithoutAmmoProperty = AccessTools.Property(typeof(Gun), "CanFireWithoutAmmo");

    [HarmonyPatch(typeof(Gun), "CanFireDuringAnimationState")]
    [HarmonyPrefix]
    private static bool CanFireDuringAnimationStatePrefix(Gun __instance, ref bool __result)
    {
        try
        {
            var isFireInputHeld = (bool)isFireInputHeldField.GetValue(__instance);
            if (isFireInputHeld)
            {
                __result = true;
                return false;
            }
        }
        catch (System.Exception ex)
        {
            SparrohPlugin.Logger.LogError($"Error in CanFireDuringAnimationState patch: {ex.Message}");
        }
        return true;
    }

    [HarmonyPatch(typeof(Gun), "MinWalkingWeightToFire")]
    [HarmonyPrefix]
    private static bool MinWalkingWeightToFirePrefix(Gun __instance, ref float __result)
    {
        try
        {
            var isFireInputHeld = (bool)isFireInputHeldField.GetValue(__instance);
            if (isFireInputHeld)
            {
                __result = 0f;
                return false;
            }
        }
        catch (System.Exception ex)
        {
            SparrohPlugin.Logger.LogError($"Error in MinWalkingWeightToFire patch: {ex.Message}");
        }
        return true;
    }

    [HarmonyPatch(typeof(Gun), "Update")]
    [HarmonyPostfix]
    private static void UpdatePostfix(Gun __instance)
    {
        try
        {
            var isFireInputHeld = (bool)isFireInputHeldField.GetValue(__instance);

            var modGunData = __instance.gameObject.GetComponent<ModGunData>();
            if (modGunData == null)
            {
                modGunData = __instance.gameObject.AddComponent<ModGunData>();
            }

            if (modGunData.PreviousFireInputHeld && !isFireInputHeld)
            {
                if (modGunData.SprintingLockedBySprintToFire)
                {
                    var player = playerField.GetValue(__instance) as Player;
                    if (player != null)
                    {
                        player.SprintLocks = 0;
                        modGunData.SprintingLockedBySprintToFire = false;

                        if (player.AutoSprint)
                        {
                            var wantsToSprintField = typeof(Player).GetField("wantsToSprint", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                            if (wantsToSprintField != null)
                            {
                                wantsToSprintField.SetValue(player, true);
                            }
                        }
                    }
                }
            }

            modGunData.PreviousFireInputHeld = isFireInputHeld;

            if (isFireInputHeld)
            {
                var wantsToFireProperty = AccessTools.Property(typeof(Gun), "WantsToFire");
                wantsToFireProperty.SetValue(__instance, true);
            }
        }
        catch (System.Exception ex)
        {
            SparrohPlugin.Logger.LogError($"Error in Update postfix: {ex.Message}");
        }
    }

    [HarmonyPatch(typeof(Gun), "HandleFiring")]
    [HarmonyPrefix]
    private static bool HandleFiringPrefix(Gun __instance)
    {
        try
        {
            var player = playerField.GetValue(__instance) as Player;
            if (player == null)
            {
                return true;
            }

            var gunData = gunDataField.GetValue(__instance);
            if (gunData == null)
            {
                return true;
            }

            var isFireInputHeld = (bool)isFireInputHeldField.GetValue(__instance);
            var wantsToFire = (bool)AccessTools.Property(typeof(Gun), "WantsToFire").GetValue(__instance);
            
            if (player.IsSprinting && isFireInputHeld)
            {
                var chargeData = gunData.GetType().GetField("chargeData").GetValue(gunData);
                var canChargeFire = (bool)chargeData.GetType().GetProperty("CanFire").GetValue(chargeData);

                var canFireWithoutAmmo = (bool)canFireWithoutAmmoProperty.GetValue(__instance);

                if (canChargeFire && ((double)__instance.RemainingAmmo >= 1.0 || canFireWithoutAmmo))
                {
                    var fireConstraints = gunData.GetType().GetField("fireConstraints").GetValue(gunData);
                    var canFireWhileSprinting = (int)fireConstraints.GetType().GetField("canFireWhileSprinting").GetValue(fireConstraints);

                    if (canFireWhileSprinting != 1)
                    {
                        player.SprintLocks = 1;

                        var modGunData = __instance.gameObject.GetComponent<ModGunData>();
                        if (modGunData != null)
                        {
                            modGunData.SprintingLockedBySprintToFire = true;
                        }
                    }

                    tryFireMethod.Invoke(__instance, null);
                    return false;
                }
                else
                {
                }
            }
            else
            {
            }
        }
        catch (System.Exception ex)
        {
            SparrohPlugin.Logger.LogError($"Error in SprintToFireFix patch: {ex.Message}");
            SparrohPlugin.Logger.LogError($"Stack trace: {ex.StackTrace}");
        }
        return true;
    }
}
