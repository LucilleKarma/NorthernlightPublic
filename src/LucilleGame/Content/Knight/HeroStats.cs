namespace Northernlight.Content.Knight;

/// <summary>
///     A single collection of stats for all heroes.
/// </summary>
public readonly record struct HeroStats
{
    /// <summary>
    ///     The base amount of damage Kris does
    ///     when attacking.
    /// </summary>
    public required int KrisBaseDamage
    {
        get;
        init;
    }

    /// <summary>
    ///     The base amount of damage Susie does
    ///     when attacking.
    /// </summary>
    public required int SusieBaseDamage
    {
        get;
        init;
    }

    /// <summary>
    ///     The base amount of damage Noelle does
    ///     when attacking.
    /// </summary>
    public required int NoelleBaseDamage
    {
        get;
        init;
    }

    /// <summary>
    ///     The general damage factor to apply when
    ///     heroes attack the Knight.
    /// </summary>
    public required float GeneralDamageFactor
    {
        get;
        init;
    }

    /// <summary>
    ///     The amount that Susie's good heal heals.
    /// </summary>
    public required int GoodHealHealQuantity
    {
        get;
        init;
    }

    /// <summary>
    ///     The tension point cost of Kris' cross
    ///     slash.
    /// </summary>
    public required float CrossSlashTensionPointCost
    {
        get;
        init;
    }

    /// <summary>
    ///     The tension point cost of Susie's rude
    ///     buster.
    /// </summary>
    public required float RudeBusterTensionPointCost
    {
        get;
        init;
    }

    /// <summary>
    ///     The tension point cost of Susie's good
    ///     heal.
    /// </summary>
    public required float GoodHealTensionPointCost
    {
        get;
        init;
    }

    /// <summary>
    ///     The tension point cost of Noelle's
    ///     angel's blessing heal.
    /// </summary>
    public required float AngelsBlessingTensionPointCost
    {
        get;
        init;
    }

    /// <summary>
    ///     The amount of HP that Noelle's angel's blessing
    ///     heals when used on a non-downed hero.
    /// </summary>
    public required int AngelsBlessingHealAmount_NotRevive
    {
        get;
        init;
    }

    /// <summary>
    ///     The amount of HP that Noelle's angel's blessing
    ///     heals when used on a downed hero.
    /// </summary>
    public required int AngelsBlessingHealAmount_Revive
    {
        get;
        init;
    }

    /// <summary>
    ///     The amount of HP that Noelle's angel's blessing
    ///     heals, regardless of if the targeted hero is
    ///     downed, on Side B.
    /// </summary>
    public required int AngelsBlessingHealAmount_SideB
    {
        get;
        init;
    }

    /// <summary>
    ///     The tension point cost of Noelle's ice
    ///     shock.
    /// </summary>
    public required float IceShockTensionPointCost
    {
        get;
        init;
    }

    /// <summary>
    ///     The damage factor of Kris' cross slashes.
    /// </summary>
    /// <remarks>
    ///     This value already accounts for <see cref="GeneralDamageFactor"/>
    ///     when being calculated.
    /// </remarks>
    public required float CrossSlashDamageFactor
    {
        get;
        init;
    }

    /// <summary>
    ///     The damage factor of Susie's rude buster.
    /// </summary>
    /// <remarks>
    ///     This value already accounts for <see cref="GeneralDamageFactor"/>
    ///     when being calculated.
    /// </remarks>
    public required float RudeBusterDamageFactor
    {
        get;
        init;
    }

    /// <summary>
    ///     The damage factor of Noelle's ice shock.
    /// </summary>
    /// <remarks>
    ///     This value already accounts for <see cref="GeneralDamageFactor"/>
    ///     when being calculated.
    /// </remarks>
    public required float IceShockDamageFactor
    {
        get;
        init;
    }
}
