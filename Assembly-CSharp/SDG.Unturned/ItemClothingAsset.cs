using UnityEngine;

namespace SDG.Unturned;

public class ItemClothingAsset : ItemAsset
{
    protected float _armor;

    protected float _explosionArmor;

    private bool _proofWater;

    private bool _proofFire;

    private bool _proofRadiation;

    /// <summary>
    /// Left-handed character skeleton is mirrored, so most item models are mirrored again to preserve appearance.
    /// Unfortunately this does not work well for some items e.g. the particle system on Elver/Dango glasses.
    /// </summary>
    internal bool shouldMirrorLeftHandedModel;

    public float movementSpeedMultiplier = 1f;

    /// <summary>
    /// Sound to play when equipped.
    /// </summary>
    public AudioReference wearAudio;

    /// <summary>
    /// Overrides value of TakesPriorityOverCosmetic if <see cref="F:SDG.Unturned.ItemClothingAsset.hasPriorityOverCosmeticOverride" /> is true.
    /// </summary>
    protected bool priorityOverCosmeticOverride;

    /// <summary>
    /// If true, the value of <see cref="F:SDG.Unturned.ItemClothingAsset.priorityOverCosmeticOverride" /> is used rather than <see cref="M:SDG.Unturned.ItemClothingAsset.GetDefaultTakesPriorityOverCosmetic" />.
    /// Defaults to false. True if <see cref="F:SDG.Unturned.ItemClothingAsset.priorityOverCosmeticOverride" /> is set.
    /// </summary>
    protected bool hasPriorityOverCosmeticOverride;

    /// <summary>
    /// Multiplier to incoming damage. Defaults to 1.0.
    /// </summary>
    public float armor => _armor;

    /// <summary>
    /// Multiplier to explosive damage. Defaults to <see cref="P:SDG.Unturned.ItemClothingAsset.armor" /> value if Armor_Explosion isn't specified.
    /// </summary>
    public float explosionArmor => _explosionArmor;

    /// <summary>
    /// Armor against falling damage. Defaults to 1.0, i.e., take the normal amount of damage.
    /// </summary>
    public float fallingDamageMultiplier { get; protected set; }

    public override bool showQuality => true;

    public bool proofWater => _proofWater;

    public bool proofFire => _proofFire;

    public bool proofRadiation => _proofRadiation;

    /// <summary>
    /// If true on any worn clothing item, bones never break when falling.
    /// Defaults to false.
    /// </summary>
    public bool preventsFallingBrokenBones { get; protected set; }

    public bool visibleOnRagdoll { get; protected set; }

    public bool hairVisible { get; protected set; }

    public bool beardVisible { get; protected set; }

    public bool shouldDestroyClothingColliders { get; protected set; }

    /// <summary>
    /// If set, find a child meshrenderer with this name and change its material to the character skin material.
    /// </summary>
    public string skinOverride { get; protected set; }

    /// <summary>
    /// The player can be wearing both a "real" in-game item and a cosmetic item in the same clothing slot.
    /// If true, the real item is shown rather than the cosmetic item. For example, night vision goggles
    /// are shown over any glasses cosmetic because of their gameplay-related green glow.
    /// </summary>
    public bool TakesPriorityOverCosmetic
    {
        get
        {
            if (!hasPriorityOverCosmeticOverride)
            {
                return GetDefaultTakesPriorityOverCosmetic();
            }
            return priorityOverCosmeticOverride;
        }
    }

    /// <summary>
    /// For 3D clothes. Ideally, this wouldn't be type specific, but we have a separate prefab property for each
    /// type of clothing at the moment.
    /// </summary>
    internal virtual GameObject ClothingPrefab => null;

    public bool shouldBeVisible(bool isRagdoll)
    {
        if (isRagdoll)
        {
            return visibleOnRagdoll;
        }
        return true;
    }

    public override void BuildDescription(ItemDescriptionBuilder builder, Item itemInstance)
    {
        base.BuildDescription(builder, itemInstance);
        if (builder.shouldRestrictToLegacyContent)
        {
            return;
        }
        if (type == EItemType.HAT || type == EItemType.SHIRT || type == EItemType.PANTS || type == EItemType.VEST)
        {
            if (_armor != 1f)
            {
                builder.Append(PlayerDashboardInventoryUI.localization.format("ItemDescription_Clothing_Armor", PlayerDashboardInventoryUI.FormatStatModifier(_armor, higherIsPositive: false, higherIsBeneficial: false)), 10000 + DescSort_LowerIsBeneficial(_armor));
            }
            if (_explosionArmor != 1f)
            {
                builder.Append(PlayerDashboardInventoryUI.localization.format("ItemDescription_Clothing_ExplosionArmor", PlayerDashboardInventoryUI.FormatStatModifier(_explosionArmor, higherIsPositive: false, higherIsBeneficial: false)), 10000 + DescSort_LowerIsBeneficial(_explosionArmor));
            }
        }
        if (movementSpeedMultiplier != 1f)
        {
            builder.Append(PlayerDashboardInventoryUI.localization.format("ItemDescription_ClothingMovementSpeedModifier", PlayerDashboardInventoryUI.FormatStatModifier(movementSpeedMultiplier, higherIsPositive: true, higherIsBeneficial: true)), 10000 + DescSort_HigherIsBeneficial(movementSpeedMultiplier));
        }
        if (fallingDamageMultiplier != 1f)
        {
            builder.Append(PlayerDashboardInventoryUI.localization.format("ItemDescription_FallingDamageModifier", PlayerDashboardInventoryUI.FormatStatModifier(fallingDamageMultiplier, higherIsPositive: true, higherIsBeneficial: false)), 10000 + DescSort_LowerIsBeneficial(fallingDamageMultiplier));
        }
        if (_proofFire)
        {
            builder.Append(PlayerDashboardInventoryUI.FormatStatColor(PlayerDashboardInventoryUI.localization.format("ItemDescription_Clothing_FireProof"), isBeneficial: true), 9999);
        }
        if (_proofRadiation)
        {
            builder.Append(PlayerDashboardInventoryUI.FormatStatColor(PlayerDashboardInventoryUI.localization.format("ItemDescription_Clothing_RadiationProof"), isBeneficial: true), 9999);
        }
        if (preventsFallingBrokenBones)
        {
            builder.Append(PlayerDashboardInventoryUI.FormatStatColor(PlayerDashboardInventoryUI.localization.format("ItemDescription_Clothing_FallingBoneBreakingProof"), isBeneficial: true), 9999);
        }
    }

    public override void PopulateAsset(Bundle bundle, DatDictionary data, Local localization)
    {
        base.PopulateAsset(bundle, data, localization);
        if (isPro)
        {
            _armor = 1f;
            _explosionArmor = 1f;
            fallingDamageMultiplier = 1f;
        }
        else
        {
            _armor = data.ParseFloat("Armor");
            if (data.ContainsKey("Armor"))
            {
                _armor = data.ParseFloat("Armor");
            }
            else
            {
                _armor = 1f;
            }
            if (data.ContainsKey("Armor_Explosion"))
            {
                _explosionArmor = data.ParseFloat("Armor_Explosion");
            }
            else
            {
                _explosionArmor = armor;
            }
            fallingDamageMultiplier = data.ParseFloat("Falling_Damage_Multiplier", 1f);
            _proofWater = data.ContainsKey("Proof_Water");
            _proofFire = data.ContainsKey("Proof_Fire");
            _proofRadiation = data.ContainsKey("Proof_Radiation");
            preventsFallingBrokenBones = data.ParseBool("Prevents_Falling_Broken_Bones");
            movementSpeedMultiplier = data.ParseFloat("Movement_Speed_Multiplier", 1f);
        }
        visibleOnRagdoll = data.ParseBool("Visible_On_Ragdoll", defaultValue: true);
        hairVisible = data.ParseBool("Hair_Visible", defaultValue: true);
        beardVisible = data.ParseBool("Beard_Visible", defaultValue: true);
        shouldMirrorLeftHandedModel = data.ParseBool("Mirror_Left_Handed_Model", defaultValue: true);
        if (data.ContainsKey("WearAudio"))
        {
            wearAudio = data.ReadAudioReference("WearAudio", bundle);
        }
        else if (type == EItemType.BACKPACK || type == EItemType.VEST)
        {
            wearAudio = new AudioReference("core.masterbundle", "Sounds/Zipper.mp3");
        }
        else
        {
            wearAudio = new AudioReference("core.masterbundle", "Sounds/Sleeve.mp3");
        }
        shouldDestroyClothingColliders = data.ParseBool("Destroy_Clothing_Colliders", defaultValue: true);
        hasPriorityOverCosmeticOverride = data.TryParseBool("Priority_Over_Cosmetic", out priorityOverCosmeticOverride);
        skinOverride = data.GetString("Skin_Override");
    }

    protected override AudioReference GetDefaultInventoryAudio()
    {
        if (size_x <= 1 || size_y <= 1)
        {
            return new AudioReference("core.masterbundle", "Sounds/Inventory/LightCloth.asset");
        }
        if (rarity == EItemRarity.COMMON || rarity == EItemRarity.UNCOMMON)
        {
            return new AudioReference("core.masterbundle", "Sounds/Inventory/LightClothEquipment.asset");
        }
        return new AudioReference("core.masterbundle", "Sounds/Inventory/MediumClothEquipment.asset");
    }

    /// <summary>
    /// The player can be wearing both a "real" in-game item and a cosmetic item in the same clothing slot.
    /// This is called on the real item if <see cref="F:SDG.Unturned.ItemClothingAsset.priorityOverCosmeticOverride" /> has not been set.
    /// If true, the real item is shown rather than the cosmetic item. If false, the cosmetic item can be seen.
    /// </summary>
    protected virtual bool GetDefaultTakesPriorityOverCosmetic()
    {
        return false;
    }
}
