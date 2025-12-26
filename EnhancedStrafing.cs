using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

public static class EnhancedStrafing
{
    public static ConfigEntry<bool> enableStrafing;
    public static ConfigEntry<bool> enableWingsuitStrafing;

    public static void Init(ConfigFile config)
    {
        enableStrafing = config.Bind("Movement Modifications", "Strafing", true, "Enables improved strafing speed.");
        enableWingsuitStrafing = config.Bind("Movement Modifications", "WingsuitStrafing", true, "Enables strafing while flying with wingsuit.");
    }

    public static void SetupStrafingPatches(Harmony harmony)
    {
        harmony.PatchAll(typeof(StrafingSpeedPatch));
        harmony.PatchAll(typeof(WingsuitStrafingPatch));
    }
}

[HarmonyPatch]
public static class WingsuitStrafingPatch
{
    [HarmonyPatch(typeof(Wingsuit), "OnJumpPressed")]
    [HarmonyPostfix]
    public static void OnJumpPressedPostfix(Wingsuit __instance)
    {
        if (!EnhancedStrafing.enableWingsuitStrafing.Value) return;
        var dataField = __instance.GetType().GetField("wingsuitData",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (dataField == null) return;

        var data = dataField.GetValue(__instance);
        if (data == null) return;

        var lockField = data.GetType().GetField("lockFlyDirection",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        if (lockField == null || !(bool)lockField.GetValue(data)) return;

        var playerField = __instance.GetType().GetField("player",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (playerField == null) return;

        var player = playerField.GetValue(__instance);
        if (player == null) return;

        Vector2 moveInput = PlayerInput.MoveInput();
        if (moveInput.magnitude < 0.1f) return;

        var isFlyingField = __instance.GetType().GetField("isFlying",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (isFlyingField != null && (bool)isFlyingField.GetValue(__instance))
        {
            Vector3 normalizedInput = new Vector3(moveInput.x, moveInput.y, 0f).normalized;

            var flySpeedField = data.GetType().GetField("flySpeed",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (flySpeedField == null) return;

            float baseFlySpeed = (float)flySpeedField.GetValue(data);
            float strafeForce = Mathf.Abs(normalizedInput.x) * baseFlySpeed * 0.5f;

            if (strafeForce > 0)
            {
                Vector3 strafeVector = __instance.transform.right * (normalizedInput.x > 0 ? 1 : -1) * strafeForce;
                var addForceMethod = player.GetType().GetMethod("AddForce", new System.Type[] { typeof(Vector3) });
                if (addForceMethod != null)
                {
                    addForceMethod.Invoke(player, new object[] { strafeVector });
                }
            }
        }
    }

    [HarmonyPatch(typeof(Wingsuit), "FixedUpdate")]
    [HarmonyPostfix]
    public static void FixedUpdatePostfix(Wingsuit __instance)
    {
        if (!EnhancedStrafing.enableWingsuitStrafing.Value) return;
        var isFlyingField = __instance.GetType().GetField("isFlying",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (isFlyingField == null || !(bool)isFlyingField.GetValue(__instance)) return;

        Vector2 moveInput = PlayerInput.MoveInput();
        if (moveInput.magnitude < 0.01f) return;

        Vector3 normalizedMoveInput = new Vector3(moveInput.x, moveInput.y, 0f);
        float inputMagnitude = normalizedMoveInput.magnitude;
        if (inputMagnitude > 0)
        {
            normalizedMoveInput /= inputMagnitude;
        }

        var dataField = __instance.GetType().GetField("wingsuitData",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (dataField == null) return;

        var data = dataField.GetValue(__instance);
        if (data == null) return;

        var flySpeedField = data.GetType().GetField("flySpeed",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        var flySpeedCurveField = data.GetType().GetField("flySpeedCurve",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        var flySpeedCurveDurationField = data.GetType().GetField("flySpeedCurveDuration",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        if (flySpeedField == null || flySpeedCurveField == null) return;

        float baseFlySpeed = (float)flySpeedField.GetValue(data);
        var flySpeedCurve = flySpeedCurveField.GetValue(data) as AnimationCurve;
        float flySpeedCurveDuration = (flySpeedCurveDurationField != null)
            ? (float)flySpeedCurveDurationField.GetValue(data)
            : 1f;

        var flyStartTimeField = __instance.GetType().GetField("flyStartTime",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (flyStartTimeField == null) return;

        float flyStartTime = (float)flyStartTimeField.GetValue(__instance);
        float timeRatio = Mathf.Min((Time.time - flyStartTime) / flySpeedCurveDuration, 1f);
        float curveMultiplier = flySpeedCurve.Evaluate(timeRatio);

        var playerField = __instance.GetType().GetField("player",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (playerField == null) return;

        var player = playerField.GetValue(__instance);
        if (player == null) return;

        float strafeComponent = normalizedMoveInput.x;
        if (Mathf.Abs(strafeComponent) > 0.1f)
        {
            float strafeMagnitude = baseFlySpeed * curveMultiplier * 2f;
            Vector3 strafeDirection = __instance.transform.right * strafeComponent;
            Vector3 strafeForce = strafeDirection * strafeMagnitude;

            var addForceMethod = player.GetType().GetMethod("AddForce", new System.Type[] { typeof(Vector3) });
            if (addForceMethod != null)
            {
                addForceMethod.Invoke(player, new object[] { strafeForce });
            }
        }
    }
}

[HarmonyPatch]
public static class StrafingSpeedPatch
{
    [HarmonyTargetMethod]
    public static System.Reflection.MethodBase TargetMethod()
    {
        var playerType = System.Type.GetType("Pigeon.Movement.Player, Assembly-CSharp");
        if (playerType == null)
        {
            return null;
        }

        var awakeMethod = playerType.GetMethod("Awake",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (awakeMethod != null)
        {
            return awakeMethod;
        }

        var startMethod = playerType.GetMethod("Start",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (startMethod != null)
        {
            return startMethod;
        }

        return null;
    }

    [HarmonyPostfix]
    public static void Postfix(object __instance)
    {
        if (!EnhancedStrafing.enableStrafing.Value) return;
        var playerType = __instance.GetType();

        var strafeSpeedMultiplierField = playerType.GetField("strafeSpeedMultiplier",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var strafeSpeedMultiplierWhileMovingField = playerType.GetField("strafeSpeedMultiplierWhileMoving",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (strafeSpeedMultiplierField != null)
        {
            strafeSpeedMultiplierField.SetValue(__instance, 1.0f);
        }

        if (strafeSpeedMultiplierWhileMovingField != null)
        {
            strafeSpeedMultiplierWhileMovingField.SetValue(__instance, 1.0f);
        }
    }
}
