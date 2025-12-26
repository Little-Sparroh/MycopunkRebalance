using HarmonyLib;
using UnityEngine;

[HarmonyPatch(typeof(FlamethrowerArmTip), "UpdateMaxLength")]
public class FlamethrowerArmTipPatch
{
    static void Postfix(FlamethrowerArmTip __instance, ref Vector3 position, ref Quaternion rotation, ref float __result)
    {
        if (__instance.Brain != null && __instance.Brain.EnemyClass.customSpawner is AmalgamationSpawner)
        {
            __result = Mathf.Min(__result, 12f);
        }
    }
}
