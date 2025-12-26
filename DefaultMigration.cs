using BepInEx.Configuration;

public static class DefaultMigration
{
    public static ConfigEntry<bool> enableCanFireWhileSprinting;
    public static ConfigEntry<bool> enableCanFireWhileSliding;
    public static ConfigEntry<bool> enableSprintToFireFix;

    public static void Init(ConfigFile config)
    {
        enableCanFireWhileSprinting = config.Bind("Movement Modifications", "CanFireWhileSprinting", true, "Allows firing weapons while sprinting.");
        enableCanFireWhileSliding = config.Bind("Movement Modifications", "CanFireWhileSliding", true, "Allows firing weapons while sliding.");
        enableSprintToFireFix = config.Bind("Movement Modifications", "SprintToFireFix", true, "Enables the Sprint-to-Fire fix that allows immediate firing while sprinting and proper sprint resume behavior.");
    }

    public static void ApplyFireConstraints(ref GunData gunData)
    {
        if (enableCanFireWhileSprinting.Value)
        {
            gunData.fireConstraints.canFireWhileSprinting = FireConstraints.ActionFireMode.CanPerformDuring;
        }
        if (enableCanFireWhileSliding.Value)
        {
            gunData.fireConstraints.canFireWhileSliding = FireConstraints.ActionFireMode.CanPerformDuring;
        }
    }
}
