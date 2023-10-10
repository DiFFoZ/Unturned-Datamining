using UnityEngine;

namespace SDG.Unturned;

public class ItemBarrelAsset : ItemCaliberAsset
{
    protected AudioClip _shoot;

    protected GameObject _barrel;

    private bool _isBraked;

    private bool _isSilenced;

    private float _volume;

    private byte _durability;

    private float _ballisticDrop;

    public AudioClip shoot => _shoot;

    public GameObject barrel => _barrel;

    public bool isBraked => _isBraked;

    public bool isSilenced => _isSilenced;

    public float volume => _volume;

    public byte durability => _durability;

    public override bool showQuality => durability > 0;

    public float ballisticDrop => _ballisticDrop;

    public float gunshotRolloffDistanceMultiplier { get; protected set; }

    public override void BuildDescription(ItemDescriptionBuilder builder, Item itemInstance)
    {
        base.BuildDescription(builder, itemInstance);
        if (!builder.shouldRestrictToLegacyContent && _ballisticDrop != 1f)
        {
            builder.Append(PlayerDashboardInventoryUI.localization.format("ItemDescription_BulletGravityModifier", PlayerDashboardInventoryUI.FormatStatModifier(_ballisticDrop, higherIsPositive: true, higherIsBeneficial: false)), 10000);
        }
    }

    public override void PopulateAsset(Bundle bundle, DatDictionary data, Local localization)
    {
        base.PopulateAsset(bundle, data, localization);
        _shoot = bundle.load<AudioClip>("Shoot");
        _barrel = loadRequiredAsset<GameObject>(bundle, "Barrel");
        _isBraked = data.ContainsKey("Braked");
        _isSilenced = data.ContainsKey("Silenced");
        _volume = data.ParseFloat("Volume", 1f);
        _durability = data.ParseUInt8("Durability", 0);
        if (data.ContainsKey("Ballistic_Drop"))
        {
            _ballisticDrop = data.ParseFloat("Ballistic_Drop");
        }
        else
        {
            _ballisticDrop = 1f;
        }
        float defaultValue = (isSilenced ? 0.5f : 1f);
        gunshotRolloffDistanceMultiplier = data.ParseFloat("Gunshot_Rolloff_Distance_Multiplier", defaultValue);
    }
}
