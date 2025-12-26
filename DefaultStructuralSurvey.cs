using BepInEx.Configuration;
using HarmonyLib;

public static class DefaultStructuralSurvey
{
    public static ConfigEntry<bool> enableStructuralSurvey;
}

[HarmonyPatch]
public static class HighlightLowHealthPatch
{
    [HarmonyPatch(typeof(EnemyPart), "get_AutoHighlight")]
    [HarmonyPrefix]
    public static bool AutoHighlightPrefix(EnemyPart __instance, ref bool __result)
    {
        if (DefaultStructuralSurvey.enableStructuralSurvey.Value && (double) __instance.Health < (double) __instance.MaxHealth * 0.37999999523162842)
        {
            __result = true;
            return false;
        }
        return true;
    }
}
