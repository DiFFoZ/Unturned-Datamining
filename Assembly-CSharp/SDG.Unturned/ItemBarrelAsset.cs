using System;
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

    public AudioClip shoot => _shoot;

    public GameObject barrel => _barrel;

    public bool isBraked => _isBraked;

    public bool isSilenced => _isSilenced;

    public float volume => _volume;

    public byte durability => _durability;

    public override bool showQuality => durability > 0;

    [Obsolete("Moved to ItemCaliberAsset.BallisticGravityMultiplier")]
    public float ballisticDrop => base.BallisticGravityMultiplier;

    /// <summary>
    /// Multiplier for the maximum distance the gunshot can be heard.
    /// </summary>
    public float gunshotRolloffDistanceMultiplier { get; protected set; }

    public override void PopulateAsset(Bundle bundle, DatDictionary data, Local localization)
    {
        base.PopulateAsset(bundle, data, localization);
        _shoot = bundle.load<AudioClip>("Shoot");
        _barrel = loadRequiredAsset<GameObject>(bundle, "Barrel");
        _isBraked = data.ContainsKey("Braked");
        _isSilenced = data.ContainsKey("Silenced");
        _volume = data.ParseFloat("Volume", 1f);
        _durability = data.ParseUInt8("Durability", 0);
        float defaultValue = (isSilenced ? 0.5f : 1f);
        gunshotRolloffDistanceMultiplier = data.ParseFloat("Gunshot_Rolloff_Distance_Multiplier", defaultValue);
    }
}
