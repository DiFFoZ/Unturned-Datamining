using System;
using UnityEngine;

namespace SDG.Unturned;

public class EffectAsset : Asset
{
    protected GameObject _effect;

    protected GameObject[] _splatters;

    private bool _gore;

    private byte _splatter;

    private float _splatterLifetime;

    private float _splatterLifetimeSpread;

    private bool _splatterLiquid;

    private EPlayerTemperature _splatterTemperature;

    private byte _splatterPreload;

    private float _lifetime;

    private float _lifetimeSpread;

    private bool _isStatic;

    private byte _preload;

    public Guid blastmarkEffectGuid;

    private ushort _blast;

    public float cameraShakeRadius;

    public float cameraShakeMagnitudeDegrees;

    public GameObject effect => _effect;

    public GameObject[] splatters => _splatters;

    public bool gore => _gore;

    public byte splatter => _splatter;

    public float splatterLifetime => _splatterLifetime;

    public float splatterLifetimeSpread => _splatterLifetimeSpread;

    public bool splatterLiquid => _splatterLiquid;

    public EPlayerTemperature splatterTemperature => _splatterTemperature;

    public byte splatterPreload => _splatterPreload;

    public float lifetime => _lifetime;

    public float lifetimeSpread => _lifetimeSpread;

    public bool isStatic => _isStatic;

    /// <summary>
    /// If true the music option is respected when this effect is used by ambiance volume.
    /// </summary>
    public bool isMusic { get; private set; }

    public byte preload => _preload;

    public ushort blast
    {
        [Obsolete]
        get
        {
            return _blast;
        }
    }

    /// <summary>
    /// In multiplayer the effect will be spawned for players within this radius.
    /// </summary>
    public float relevantDistance { get; protected set; }

    public bool spawnOnDedicatedServer { get; protected set; }

    public bool randomizeRotation { get; protected set; }

    public override EAssetType assetCategory => EAssetType.EFFECT;

    public EffectAsset FindBlastmarkEffectAsset()
    {
        return Assets.FindEffectAssetByGuidOrLegacyId(blastmarkEffectGuid, blast);
    }

    public override void PopulateAsset(Bundle bundle, DatDictionary data, Local localization)
    {
        base.PopulateAsset(bundle, data, localization);
        if (id < 200 && !base.OriginAllowsVanillaLegacyId && !data.ContainsKey("Bypass_ID_Limit"))
        {
            throw new NotSupportedException("ID < 200");
        }
        _effect = bundle.load<GameObject>("Effect");
        if (effect == null)
        {
            throw new NotSupportedException("Missing effect gameobject");
        }
        _gore = data.ContainsKey("Gore");
        _splatters = new GameObject[data.ParseUInt8("Splatter", 0)];
        for (int i = 0; i < splatters.Length; i++)
        {
            splatters[i] = bundle.load<GameObject>("Splatter_" + i);
            if (splatters[i] == null)
            {
                Assets.ReportError(this, $"missing 'Splatter_{i}' gameobject");
            }
        }
        _splatter = data.ParseUInt8("Splatters", 0);
        _splatterLiquid = data.ContainsKey("Splatter_Liquid");
        if (data.ContainsKey("Splatter_Temperature"))
        {
            _splatterTemperature = (EPlayerTemperature)Enum.Parse(typeof(EPlayerTemperature), data.GetString("Splatter_Temperature"), ignoreCase: true);
        }
        else
        {
            _splatterTemperature = EPlayerTemperature.NONE;
        }
        _splatterLifetime = data.ParseFloat("Splatter_Lifetime");
        if (data.ContainsKey("Splatter_Lifetime_Spread"))
        {
            _splatterLifetimeSpread = data.ParseFloat("Splatter_Lifetime_Spread");
        }
        else
        {
            _splatterLifetimeSpread = 1f;
        }
        _lifetime = data.ParseFloat("Lifetime");
        if (data.ContainsKey("Lifetime_Spread"))
        {
            _lifetimeSpread = data.ParseFloat("Lifetime_Spread");
        }
        else
        {
            _lifetimeSpread = 4f;
        }
        _isStatic = data.ContainsKey("Static");
        isMusic = data.ParseBool("Is_Music");
        if (data.ContainsKey("Preload"))
        {
            _preload = data.ParseUInt8("Preload", 0);
        }
        else
        {
            _preload = 1;
        }
        if (data.ContainsKey("Splatter_Preload"))
        {
            _splatterPreload = data.ParseUInt8("Splatter_Preload", 0);
        }
        else
        {
            _splatterPreload = (byte)(Mathf.CeilToInt((float)(int)splatter / (float)splatters.Length) * preload);
        }
        _blast = data.ParseGuidOrLegacyId("Blast", out blastmarkEffectGuid);
        relevantDistance = data.ParseFloat("Relevant_Distance", -1f);
        spawnOnDedicatedServer = data.ContainsKey("Spawn_On_Dedicated_Server");
        if (data.ContainsKey("Randomize_Rotation"))
        {
            randomizeRotation = data.ParseBool("Randomize_Rotation");
        }
        else
        {
            randomizeRotation = true;
        }
        cameraShakeRadius = data.ParseFloat("CameraShake_Radius");
        cameraShakeMagnitudeDegrees = data.ParseFloat("CameraShake_MagnitudeDegrees");
    }
}
