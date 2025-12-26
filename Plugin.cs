using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Pigeon.Movement;
using System;
using System.IO;
using System.Reflection;

[BepInPlugin(PluginGUID, PluginName, PluginVersion)]
public class SparrohPlugin : BaseUnityPlugin
{
    public const string PluginGUID = "sparroh.mycopunkrebalance";
    public const string PluginName = "MycopunkRebalance";
    public const string PluginVersion = "1.1.0";

    internal static new ManualLogSource Logger;

    private Harmony harmony;

    private void Awake()
    {
        Logger = base.Logger;

        try
        {
            DefaultMigration.Init(Config);
            EnhancedAiming.Init(Config);
            DefaultMagboots.Init(Config);
            EnhancedStrafing.Init(Config);
            DefaultStructuralSurvey.enableStructuralSurvey = Config.Bind("Structural Survey", "AlwaysActive", true, "Makes the Structural Survey upgrade always active, highlighting low-health enemy parts.");
            DefaultCloudSkip.enableCloudSkip = Config.Bind("Movement Modifications", "CloudSkip", true, "Enables Cloud Skip (Double Jump) ability at all times.");

            var configFile = DefaultMigration.enableCanFireWhileSprinting.ConfigFile;
            var watcher = new FileSystemWatcher(Paths.ConfigPath, $"{PluginGUID}.cfg");
            watcher.Changed += (s, e) => { Logger.LogInfo("Config file changed, reloading"); configFile.Reload(); };
            watcher.EnableRaisingEvents = true;
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error during config initialization: {ex.Message}");
        }

        harmony = new Harmony(PluginGUID);

        try
        {
            MethodInfo setupMethod = AccessTools.Method(typeof(Gun), "Setup", new Type[] { typeof(Player), typeof(PlayerAnimation), typeof(IGear) });
            if (setupMethod == null)
            {
                Logger.LogError("Could not find Gun.Setup method for patching.");
                return;
            }
            HarmonyMethod prefix = new HarmonyMethod(typeof(SparrohPlugin), nameof(ModifyWeaponPrefix));
            harmony.Patch(setupMethod, prefix: prefix);
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error patching Gun.Setup: {ex.Message}");
        }

        try
        {
            EnhancedAiming.SetupAimingPatches(harmony);
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error setting up aiming patches: {ex.Message}");
        }

        try
        {
            DefaultMagboots.SetupWallrunningPatch(harmony);
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error setting up wallrunning patches: {ex.Message}");
        }

        try
        {
            EnhancedStrafing.SetupStrafingPatches(harmony);
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error setting up strafing patches: {ex.Message}");
        }

        try
        {
            harmony.PatchAll(typeof(HighlightLowHealthPatch));
            harmony.PatchAll(typeof(SwarmGun_OnActiveUpdate_Patch));
            harmony.PatchAll(typeof(FlamethrowerArmTipPatch));
            harmony.PatchAll(typeof(AutocannonArmTipPatch));
            harmony.PatchAll(typeof(CoreProtectionPatches));
            harmony.PatchAll(typeof(DefaultCloudSkipPatches));

            if (DefaultMigration.enableSprintToFireFix.Value)
            {
                harmony.PatchAll(typeof(SprintToFireFixPatches));
                Logger.LogInfo("SprintToFireFix enabled");
            }
            else
            {
                Logger.LogInfo("SprintToFireFix disabled");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error applying Harmony patches: {ex.Message}");
        }

        Logger.LogInfo($"{PluginName} loaded successfully.");
    }

    public static void ModifyWeaponPrefix(Gun __instance, IGear prefab)
    {
        var modGunData = __instance.gameObject.AddComponent<ModGunData>();
        ApplyGunConstraints(prefab, modGunData);
    }

    private static void ApplyGunConstraints(IGear prefab, ModGunData modGunData)
    {
        if (prefab == null || prefab is not Gun gunPrefab) return;

        ref var gunData = ref gunPrefab.GunData;

        DefaultMigration.ApplyFireConstraints(ref gunData);
        EnhancedAiming.ApplyAimingConstraints(ref gunData, gunPrefab, modGunData);
    }
}
