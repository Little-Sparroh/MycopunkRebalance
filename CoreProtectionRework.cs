using HarmonyLib;
using UnityEngine;
using System;
using System.Collections;

public static class CoreProtectionRework
{
    public static System.Collections.Generic.Dictionary<EnemyShell, int> ShellID = new();

    public static System.Collections.Generic.Dictionary<int, int> LayerIndex = new();

    public static void SetShellIDs(EnemyPart part)
    {
        try
        {
            if (part?.ChildComponents == null) return;
            foreach (var com in part.ChildComponents)
            {
                if (com is EnemyShell shell && shell?.gameObject != null)
                {
                    string name = shell.gameObject.name;
                    int layer = 0;
                    if (name.Contains("Large")) layer = 1;
                    else if (name.Contains("Inner")) layer = 2;
                    else if (name.Contains("L1")) layer = 1;
                    else if (name.Contains("L2")) layer = 2;
                    else if (name.Contains("L3")) layer = 3;
                    int index = CoreProtectionRework.LayerIndex.TryGetValue(layer, out int i) ? i : 0;
                    int id = layer * 100 + index;
                    CoreProtectionRework.ShellID[shell] = id;
                    CoreProtectionRework.LayerIndex[layer] = index + 1;
                }
                if (com is EnemyPart subPart) SetShellIDs(subPart);
            }
        }
        catch (Exception ex)
        {
            SparrohPlugin.Logger.LogError($"Error in SetShellIDs: {ex.Message}");
        }
    }

    public static bool HasShellChildren(EnemyPart part)
    {
        try
        {
            if (part?.ChildComponents == null) return false;
            foreach (var com in part.ChildComponents)
            {
                if (com?.IsAlive == true && com is EnemyShell) return true;
                if (com is EnemyPart subPart && HasShellChildren(subPart)) return true;
            }
        }
        catch (Exception ex)
        {
            SparrohPlugin.Logger.LogError($"Error in HasShellChildren: {ex.Message}");
        }
        return false;
    }

    public static int CountAliveMetalShells(EnemyPart part)
    {
        try
        {
            int count = 0;
            if (part?.ChildComponents == null) return count;
            foreach (var com in part.ChildComponents)
            {
                if (com?.IsAlive == true && com is EnemyShell shell && shell?.gameObject != null)
                {
                    var name = shell.gameObject.name;
                    if (name.Contains("Large") || name.Contains("Inner") || name.Contains("L1") || name.Contains("L2") || name.Contains("L3") || name.Contains("Shell"))
                    {
                        count++;
                    }
                }
                if (com is EnemyPart subPart)
                {
                    count += CountAliveMetalShells(subPart);
                }
            }
            return count;
        }
        catch (Exception ex)
        {
            SparrohPlugin.Logger.LogError($"Error in CountAliveMetalShells: {ex.Message}");
            return 0;
        }
    }

    public static int CountRemainingMetal(EnemyPart part, string layerString)
    {
        try
        {
            if (string.IsNullOrEmpty(layerString) || part?.ChildComponents == null) return 0;
            int count = 0;
            foreach (var com in part.ChildComponents)
            {
                if (com is EnemyShell shell && shell?.gameObject != null && shell.gameObject.name.Contains(layerString))
                {
                    count++;
                }
                if (com is EnemyPart subPart)
                {
                    count += CountRemainingMetal(subPart, layerString);
                }
            }
            return count;
        }
        catch (Exception ex)
        {
            SparrohPlugin.Logger.LogError($"Error in CountRemainingMetal: {ex.Message}");
            return 0;
        }
    }

    public static bool HasLayer(EnemyPart part, string check)
    {
        try
        {
            if (part?.ChildComponents == null || string.IsNullOrEmpty(check)) return false;
            foreach (var com in part.ChildComponents)
            {
                if (com is EnemyShell shell && shell?.gameObject != null && shell.gameObject.name.Contains(check))
                    return true;
                if (com is EnemyPart subPart && HasLayer(subPart, check))
                    return true;
            }
        }
        catch (Exception ex)
        {
            SparrohPlugin.Logger.LogError($"Error in HasLayer: {ex.Message}");
        }
        return false;
    }
}

public static class CoreProtectionPatches
{
    [HarmonyPatch(typeof(EnemyManager), methodName: "AddChildren", argumentTypes: new Type[] { typeof(EnemyClass), typeof(EnemyPart), typeof(float) })]
    [HarmonyPostfix]
    static void PostfixAddChildren(EnemyClass enemyClass, EnemyPart part, float healthMultiplier)
    {
        try
        {
            if (part is EnemyCore)
            {
                CoreProtectionRework.LayerIndex.Clear();
                CoreProtectionRework.SetShellIDs(part);
            }
        }
        catch (Exception ex)
        {
            SparrohPlugin.Logger.LogError($"Error in PostfixAddChildren: {ex.Message}");
        }
    }

    [HarmonyPatch(typeof(EnemyShell), "OnKill_Server", new System.Type[] { typeof(IDamageSource), typeof(DamageFlags) })]
    [HarmonyPrefix]
    static void PrefixOnKill_Server(EnemyShell __instance, IDamageSource source, DamageFlags flags)
    {
        try
        {
            if (__instance?.Parent is EnemyPart parentPart && parentPart is EnemyCore parentCore)
            {
                int remainingMetalShells = CoreProtectionRework.CountAliveMetalShells(parentCore) - 1;

                if (remainingMetalShells >= 3)
                {
                    GameManager.Instance.StartCoroutine(DelayedCheckCoreDestruction(parentPart, source));
                }
                else
                {
                    if (!CoreProtectionRework.HasShellChildren(parentCore))
                    {
                        parentCore.Kill(DamageFlags.None);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            SparrohPlugin.Logger.LogError($"Error in PrefixOnKill_Server: {ex.Message}");
        }
    }

    private static IEnumerator DelayedCheckCoreDestruction(EnemyPart core, IDamageSource source)
    {
        yield return null;
        try
        {
            if (core is EnemyCore enemyCore && !CoreProtectionRework.HasShellChildren(core))
            {
                enemyCore.Kill(DamageFlags.None);
            }
        }
        catch (Exception ex)
        {
            SparrohPlugin.Logger.LogError($"Error in DelayedCheckCoreDestruction: {ex.Message}");
        }
    }

    [HarmonyPatch(typeof(EnemyPart), "Damage", new System.Type[] { typeof(DamageData), typeof(IDamageSource), typeof(Vector3) })]
    [HarmonyPrefix]
    static bool PrefixDamage(EnemyPart __instance, ref DamageData data, ref bool __result)
    {
        try
        {
            bool hasL3 = CoreProtectionRework.HasLayer(__instance, "L3");
            bool hasL2OrInner = CoreProtectionRework.HasLayer(__instance, "L2") || CoreProtectionRework.HasLayer(__instance, "Inner");
            int metalLayer = hasL3 ? 3 : (hasL2OrInner ? 2 : 1);
            string layerString = metalLayer == 3 ? "L3" : metalLayer == 2 ? "Inner" : metalLayer == 1 ? "Large" : "";
            bool deny = false;

            if (__instance is EnemyCore)
            {
                int remainingMetal = CoreProtectionRework.CountRemainingMetal(__instance, layerString);
                deny = (metalLayer == 1) ? false : (remainingMetal > 0);
            }
            else if (__instance is EnemyShell shell && CoreProtectionRework.ShellID.TryGetValue(shell, out int id))
            {
                int l = id / 100;
                string protectString = l == 3 ? "Inner" : l == 2 ? "Large" : "";
                deny = CoreProtectionRework.CountRemainingMetal(__instance, protectString) > 0;
            }

            if (deny && data.effect != EffectType.IgnoreImmunity)
            {
                __result = false;
                return false;
            }

            if (!deny && data.effect == EffectType.IgnoreImmunity)
            {
                int countL1 = CoreProtectionRework.CountRemainingMetal(__instance, "Large");
                int countL2 = CoreProtectionRework.CountRemainingMetal(__instance, "Inner");
                int countL3 = CoreProtectionRework.CountRemainingMetal(__instance, "L3");
                float mult = 1f;

                if (__instance is EnemyCore)
                {
                    mult = countL3 == 3 ? 0.25f : (countL3 == 2 && countL2 == 3 ? 0.5f : (countL3 == 2 && countL2 == 2 && countL1 == 3 ? 0.75f : 1f));
                }
                else if (__instance is EnemyShell shell && CoreProtectionRework.ShellID.TryGetValue(shell, out int id))
                {
                    int l = id / 100;
                    if (l == 3) mult = 1f;
                    else if (l == 2) mult = countL3 == 3 ? 0.75f : 1f;
                    else if (l == 1) mult = countL3 == 3 ? 0.5f : (countL2 == 3 ? 0.75f : 1f);
                }
                data.damage *= mult;
            }
        }
        catch (Exception ex)
        {
            SparrohPlugin.Logger.LogError($"Error in PrefixDamage: {ex.Message}");
        }

        return true;
    }
}
