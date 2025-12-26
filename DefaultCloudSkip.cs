using BepInEx.Configuration;
using HarmonyLib;
using Pigeon.Movement;
using System.Reflection;

public static class DefaultCloudSkip
{
    public static ConfigEntry<bool> enableCloudSkip;
}

static class DefaultCloudSkipPatches
{
    private static readonly FieldInfo airJumpsField = AccessTools.Field(typeof(Player), "airJumps");
    private static readonly FieldInfo airJumpUpSpeedField = AccessTools.Field(typeof(Player), "airJumpUpSpeed");

    [HarmonyPatch(typeof(Player), "Movement")]
    [HarmonyPrefix]
    public static bool MovementPrefix(Player __instance)
    {
        if (!__instance.IsLocalPlayer) return true;

        if (!DefaultCloudSkip.enableCloudSkip.Value) return true;

        airJumpsField.SetValue(__instance, 1);
        airJumpUpSpeedField.SetValue(__instance, 18.3f);
        return true;
    }
}
