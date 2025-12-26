using System;
using BepInEx.Configuration;
using HarmonyLib;
using Pigeon.Movement;
using System.Reflection;
using UnityEngine;

public static class EnhancedAiming
{
    public static ConfigEntry<bool> enableCanAimWhileSliding;
    public static ConfigEntry<bool> enableCanAimWhileReloading;
    public static ConfigEntry<bool> enableCanAimWhileSprinting;

    private static readonly FieldInfo lockSprintingField = AccessTools.Field(typeof(Gun), "lockSprinting");

    public static void Init(ConfigFile config)
    {
        enableCanAimWhileSliding = config.Bind("Movement Modifications", "CanAimWhileSliding", true, "Allows aiming weapons while sliding.");
        enableCanAimWhileReloading = config.Bind("Movement Modifications", "CanAimWhileReloading", true, "Allows aiming weapons while reloading.");
        enableCanAimWhileSprinting = config.Bind("Movement Modifications", "CanAimWhileSprinting", true, "Allows aiming weapons while sprinting.");
    }

    public static void ApplyAimingConstraints(ref GunData gunData, Gun gunPrefab, ModGunData modGunData)
    {
        gunData.fireConstraints.canAimWhileSliding = (FireConstraints.ActionFireMode)(enableCanAimWhileSliding.Value ? 1 : 0);
        gunData.fireConstraints.canAimWhileReloading = enableCanAimWhileReloading.Value;
        modGunData.CanAimWhileSprinting = enableCanAimWhileSprinting.Value;

        bool lockSprintingValue = !enableCanAimWhileSprinting.Value;
        lockSprintingField.SetValue(gunPrefab, lockSprintingValue);
    }

    public static void SetupAimingPatches(Harmony harmony)
    {
        try
        {
            MethodInfo onStartAimMethod = AccessTools.Method(typeof(Gun), "OnStartAim");
            if (onStartAimMethod == null)
            {
                SparrohPlugin.Logger.LogError("Could not find Gun.OnStartAim method!");
                return;
            }
            HarmonyMethod onStartAimPrefix = new HarmonyMethod(typeof(EnhancedAiming), nameof(OnStartAimPrefix));
            HarmonyMethod onStartAimPostfix = new HarmonyMethod(typeof(EnhancedAiming), nameof(OnStartAimPostfix));
            harmony.Patch(onStartAimMethod, prefix: onStartAimPrefix, postfix: onStartAimPostfix);

            MethodInfo canAimMethod = AccessTools.Method(typeof(Gun), "CanAim");
            if (canAimMethod == null)
            {
                SparrohPlugin.Logger.LogError("Could not find Gun.CanAim method!");
                return;
            }
            HarmonyMethod canAimPrefix = new HarmonyMethod(typeof(EnhancedAiming), nameof(CanAimPrefix));
            harmony.Patch(canAimMethod, prefix: canAimPrefix);
        }
        catch (Exception ex)
        {
            SparrohPlugin.Logger.LogError($"Error in SetupAimingPatches: {ex.Message}");
        }
    }

    public static bool OnStartAimPrefix(Gun __instance)
    {
        try
        {
            var modGunData = __instance.gameObject.GetComponent<ModGunData>();
            if (modGunData != null && modGunData.CanAimWhileSprinting)
            {
                FieldInfo playerField = AccessTools.Field(typeof(Gun), "player");
                if (playerField != null)
                {
                    Player player = (Player)playerField.GetValue(__instance);
                    if (player != null)
                    {
                        PropertyInfo isSprintingProp = AccessTools.Property(typeof(Player), "IsSprinting");
                        if (isSprintingProp != null)
                        {
                            modGunData.WasSprinting = (bool)isSprintingProp.GetValue(player);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            SparrohPlugin.Logger.LogError($"Error in OnStartAimPrefix: {ex.Message}");
        }
        return true;
    }

    public static void OnStartAimPostfix(Gun __instance)
    {
        try
        {
            var modGunData = __instance.gameObject.GetComponent<ModGunData>();
            if (modGunData != null && modGunData.CanAimWhileSprinting && modGunData.WasSprinting)
            {
                FieldInfo playerField = AccessTools.Field(typeof(Gun), "player");
                if (playerField != null)
                {
                    Player player = (Player)playerField.GetValue(__instance);
                    if (player != null)
                    {
                        MethodInfo resumeSprintMethod = AccessTools.Method(typeof(Player), "ResumeSprint");
                        if (resumeSprintMethod != null)
                        {
                            resumeSprintMethod.Invoke(player, null);
                        }
                    }
                }
                modGunData.WasSprinting = false;
            }
        }
        catch (Exception ex)
        {
            SparrohPlugin.Logger.LogError($"Error in OnStartAimPostfix: {ex.Message}");
        }
    }

    public static bool CanAimPrefix(Gun __instance, ref bool __result)
    {
        try
        {
            var modGunData = __instance.gameObject.GetComponent<ModGunData>();
            if (modGunData != null && modGunData.CanAimWhileSprinting)
            {
                FieldInfo playerField = AccessTools.Field(typeof(Gun), "player");
                if (playerField != null)
                {
                    Player player = (Player)playerField.GetValue(__instance);
                    if (player != null)
                    {
                        PropertyInfo isSprintingProp = AccessTools.Property(typeof(Player), "IsSprinting");
                        if (isSprintingProp != null && (bool)isSprintingProp.GetValue(player))
                        {
                            FieldInfo isAimInputHeldField = AccessTools.Field(typeof(Gun), "isAimInputHeld");
                            if (isAimInputHeldField != null)
                            {
                                bool isAimInputHeld = (bool)isAimInputHeldField.GetValue(__instance);
                                if (isAimInputHeld)
                                {
                                    __result = true;
                                    return false;
                                }
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            SparrohPlugin.Logger.LogError($"Error in CanAimPrefix: {ex.Message}");
        }
        return true;
    }
}

public class ModGunData : MonoBehaviour
{
    public bool CanAimWhileSprinting { get; set; }
    public bool WasSprinting { get; set; }
    public bool SprintingLockedBySprintToFire { get; set; }
    public bool PreviousFireInputHeld { get; set; }
}
