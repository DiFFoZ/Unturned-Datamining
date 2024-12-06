using System;

namespace SDG.Unturned;

public class ItemCaliberAsset : ItemAsset
{
    private ushort[] _calibers;

    private float _recoil_x;

    private float _recoil_y;

    /// <summary>
    /// Recoil magnitude multiplier while the gun is aiming down sights.
    /// </summary>
    public float aimingRecoilMultiplier;

    /// <summary>
    /// Multiplier for gun's Aim_In_Duration.
    /// </summary>
    public float aimDurationMultiplier;

    private float _spread;

    private float _sway;

    private float _shake;

    private int _firerateOffset;

    protected bool _isPaintable;

    /// <summary>
    /// Movement speed multiplier while the gun is aiming down sights.
    /// </summary>
    public float aimingMovementSpeedMultiplier;

    protected bool _isBipod;

    public ushort[] calibers => _calibers;

    public float recoil_x => _recoil_x;

    public float recoil_y => _recoil_y;

    public float spread => _spread;

    public float sway => _sway;

    public float shake => _shake;

    /// <summary>
    /// For backwards compatibility this is *subtracted* from the gun's firerate, so a positive number decreases
    /// the time between shots and a negative number increases the time between shots.
    /// </summary>
    public int FirerateOffset => _firerateOffset;

    public bool isPaintable => _isPaintable;

    /// <summary>
    /// Multiplier for normal bullet damage.
    /// </summary>
    public float ballisticDamageMultiplier { get; protected set; }

    /// <summary>
    /// Multiplier for bullet acceleration due to gravity.
    /// </summary>
    public float BallisticGravityMultiplier { get; protected set; }

    public bool ShouldOnlyAffectAimWhileProne => _isBipod;

    /// <summary>
    /// If true, gun can damage entities with Invulnerable tag. Defaults to false.
    /// </summary>
    public bool CanDamageInvulernableEntities { get; protected set; }

    public bool shouldDestroyAttachmentColliders { get; protected set; }

    /// <summary>
    /// Name to use when instantiating attachment prefab.
    /// By default the asset guid is used, but it can be overridden because some
    /// modders rely on the name for Unity's legacy animation component. For example
    /// in Toothy Deerryte's case there were a lot of duplicate animations to work
    /// around the guid naming, simplified by overriding name.
    /// </summary>
    public string instantiatedAttachmentName { get; protected set; }

    protected override bool doesItemTypeHaveSkins => true;

    [Obsolete("Changed type to int")]
    public byte firerate => MathfEx.ClampToByte(_firerateOffset);

    public override void BuildDescription(ItemDescriptionBuilder builder, Item itemInstance)
    {
        base.BuildDescription(builder, itemInstance);
        if (!builder.shouldRestrictToLegacyContent)
        {
            if (_recoil_x != 1f)
            {
                builder.Append(PlayerDashboardInventoryUI.localization.format("ItemDescription_RecoilModifier_X", PlayerDashboardInventoryUI.FormatStatModifier(_recoil_x, higherIsPositive: false, higherIsBeneficial: false)), 10000 + DescSort_LowerIsBeneficial(_recoil_x));
            }
            if (_recoil_y != 1f)
            {
                builder.Append(PlayerDashboardInventoryUI.localization.format("ItemDescription_RecoilModifier_Y", PlayerDashboardInventoryUI.FormatStatModifier(_recoil_y, higherIsPositive: false, higherIsBeneficial: false)), 10000 + DescSort_LowerIsBeneficial(_recoil_y));
            }
            if (_spread != 1f)
            {
                builder.Append(PlayerDashboardInventoryUI.localization.format("ItemDescription_SpreadModifier", PlayerDashboardInventoryUI.FormatStatModifier(_spread, higherIsPositive: false, higherIsBeneficial: false)), 10000 + DescSort_LowerIsBeneficial(_spread));
            }
            if (_sway != 1f)
            {
                builder.Append(PlayerDashboardInventoryUI.localization.format("ItemDescription_SwayModifier", PlayerDashboardInventoryUI.FormatStatModifier(_sway, higherIsPositive: true, higherIsBeneficial: false)), 10000 + DescSort_LowerIsBeneficial(_sway));
            }
            if (aimingRecoilMultiplier != 1f)
            {
                builder.Append(PlayerDashboardInventoryUI.localization.format("ItemDescription_RecoilModifier_Aiming", PlayerDashboardInventoryUI.FormatStatModifier(aimingRecoilMultiplier, higherIsPositive: false, higherIsBeneficial: false)), 10000 + DescSort_LowerIsBeneficial(aimingRecoilMultiplier));
            }
            if (aimDurationMultiplier != 1f)
            {
                builder.Append(PlayerDashboardInventoryUI.localization.format("ItemDescription_AimDurationModifier", PlayerDashboardInventoryUI.FormatStatModifier(aimDurationMultiplier, higherIsPositive: false, higherIsBeneficial: false)), 10000 + DescSort_LowerIsBeneficial(aimDurationMultiplier));
            }
            if (aimingMovementSpeedMultiplier != 1f)
            {
                builder.Append(PlayerDashboardInventoryUI.localization.format("ItemDescription_AimingMovementSpeedModifier", PlayerDashboardInventoryUI.FormatStatModifier(aimingMovementSpeedMultiplier, higherIsPositive: true, higherIsBeneficial: true)), 10000 + DescSort_HigherIsBeneficial(aimingMovementSpeedMultiplier));
            }
            if (ballisticDamageMultiplier != 1f)
            {
                builder.Append(PlayerDashboardInventoryUI.localization.format("ItemDescription_BulletDamageModifier", PlayerDashboardInventoryUI.FormatStatModifier(ballisticDamageMultiplier, higherIsPositive: true, higherIsBeneficial: true)), 10000 + DescSort_HigherIsBeneficial(ballisticDamageMultiplier));
            }
            if (BallisticGravityMultiplier != 1f)
            {
                builder.Append(PlayerDashboardInventoryUI.localization.format("ItemDescription_BulletGravityModifier", PlayerDashboardInventoryUI.FormatStatModifier(BallisticGravityMultiplier, higherIsPositive: true, higherIsBeneficial: false)), 10000 + DescSort_LowerIsBeneficial(BallisticGravityMultiplier));
            }
            if (CanDamageInvulernableEntities)
            {
                builder.Append(PlayerDashboardInventoryUI.localization.format("ItemDescription_InvulnerableModifier"), 10000);
            }
        }
    }

    public override void PopulateAsset(Bundle bundle, DatDictionary data, Local localization)
    {
        base.PopulateAsset(bundle, data, localization);
        _calibers = new ushort[data.ParseUInt8("Calibers", 0)];
        for (byte b = 0; b < calibers.Length; b++)
        {
            _calibers[b] = data.ParseUInt16("Caliber_" + b, 0);
        }
        _recoil_x = data.ParseFloat("Recoil_X", 1f);
        _recoil_y = data.ParseFloat("Recoil_Y", 1f);
        aimingRecoilMultiplier = data.ParseFloat("Aiming_Recoil_Multiplier", 1f);
        aimDurationMultiplier = data.ParseFloat("Aim_Duration_Multiplier", 1f);
        _spread = data.ParseFloat("Spread", 1f);
        _sway = data.ParseFloat("Sway", 1f);
        _shake = data.ParseFloat("Shake", 1f);
        _firerateOffset = data.ParseInt32("Firerate");
        float defaultValue = data.ParseFloat("Damage", 1f);
        ballisticDamageMultiplier = data.ParseFloat("Ballistic_Damage_Multiplier", defaultValue);
        BallisticGravityMultiplier = data.ParseFloat("Ballistic_Drop", 1f);
        aimingMovementSpeedMultiplier = data.ParseFloat("Aiming_Movement_Speed_Multiplier", 1f);
        _isPaintable = data.ContainsKey("Paintable");
        _isBipod = data.ContainsKey("Bipod");
        CanDamageInvulernableEntities = data.ParseBool("Invulnerable");
        shouldDestroyAttachmentColliders = data.ParseBool("Destroy_Attachment_Colliders", defaultValue: true);
        instantiatedAttachmentName = data.GetString("Instantiated_Attachment_Name_Override", GUID.ToString("N"));
    }

    protected override AudioReference GetDefaultInventoryAudio()
    {
        return new AudioReference("core.masterbundle", "Sounds/Inventory/SmallGunAttachment.asset");
    }
}
