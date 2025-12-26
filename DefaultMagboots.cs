using BepInEx.Configuration;
using HarmonyLib;
using Pigeon.Movement;
using System.Reflection;

public static class DefaultMagboots
{
    public static ConfigEntry<bool> enableWallrunning;

    public static void Init(ConfigFile config)
    {
        enableWallrunning = config.Bind("Movement Modifications", "Wallrunning", true, "Enables wallrunning ability.");
    }

    public static void SetupWallrunningPatch(Harmony harmony)
    {
        MethodInfo getterMethod = AccessTools.PropertyGetter(typeof(Player), "EnableWallrun");
        if (getterMethod == null)
        {
            return;
        }
        HarmonyMethod wallrunPrefix = new HarmonyMethod(typeof(DefaultMagboots), nameof(EnableWallrunGetPrefix));
        harmony.Patch(getterMethod, prefix: wallrunPrefix);
    }

    public static bool EnableWallrunGetPrefix(Player __instance, ref bool __result)
    {
        if (!__instance.IsLocalPlayer) return true;

        if (!enableWallrunning.Value) return true;

        __result = true;
        return false;
    }
}
