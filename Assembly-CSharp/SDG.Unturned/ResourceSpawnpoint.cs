using System;
using System.Collections.Generic;
using UnityEngine;

namespace SDG.Unturned;

public class ResourceSpawnpoint
{
    private static List<Collider> colliders = new List<Collider>();

    [Obsolete("Unused index into LevelGround.resources for early versions of the level editor.")]
    public byte type;

    [Obsolete("Trees are now saved by asset GUID. Please use the asset property rather than finding asset by legacy ID.")]
    public ushort id;

    private float _lastDead;

    private bool areConditionsMet;

    private bool isAlive;

    private Vector3 _point;

    private bool _isGenerated;

    private Quaternion _angle;

    private Vector3 _scale;

    private ResourceAsset _asset;

    private Transform _model;

    private Transform _stump;

    private Transform _skybox;

    public ushort health;

    public Guid guid { get; protected set; }

    public float lastDead => _lastDead;

    public bool isDead => health == 0;

    public Vector3 point => _point;

    public bool isGenerated => _isGenerated;

    public Quaternion angle => _angle;

    public Vector3 scale => _scale;

    public ResourceAsset asset => _asset;

    public bool isEnabled { get; private set; }

    public bool isSkyboxEnabled { get; private set; }

    public Transform model => _model;

    public Transform stump => _stump;

    public Transform skybox => _skybox;

    /// <summary>
    /// Can this tree be damaged?
    /// Allows holiday restrictions to be taken into account. (Otherwise holiday trees could be destroyed out of season.)
    /// </summary>
    public bool canBeDamaged => areConditionsMet;

    public bool checkCanReset(float multiplier)
    {
        if (isDead && asset != null && asset.reset > 1f)
        {
            return Time.realtimeSinceStartup - lastDead > asset.reset * multiplier;
        }
        return false;
    }

    public void askDamage(ushort amount)
    {
        if (amount != 0 && !isDead)
        {
            if (amount >= health)
            {
                health = 0;
            }
            else
            {
                health -= amount;
            }
        }
    }

    public void wipe()
    {
        if (!isAlive)
        {
            return;
        }
        isAlive = false;
        if (asset != null)
        {
            health = 0;
            if (asset.isForage)
            {
                model.Find("Forage")?.gameObject.SetActive(value: false);
            }
            else
            {
                if (model != null)
                {
                    model.gameObject.SetActive(value: false);
                }
                if (stump != null)
                {
                    stump.gameObject.SetActive(isEnabled);
                }
            }
        }
        if ((bool)skybox)
        {
            skybox.gameObject.SetActive(value: false);
        }
    }

    public void revive()
    {
        if (isAlive)
        {
            return;
        }
        isAlive = true;
        if (asset != null)
        {
            if (asset.isForage)
            {
                model.Find("Forage")?.gameObject.SetActive(value: true);
                health = asset.health;
            }
            else
            {
                if (model != null && areConditionsMet)
                {
                    model.gameObject.SetActive(isEnabled);
                }
                health = asset.health;
                if (stump != null && areConditionsMet)
                {
                    stump.gameObject.SetActive(isEnabled);
                }
            }
        }
        if ((bool)skybox && areConditionsMet)
        {
            skybox.gameObject.SetActive(isSkyboxEnabled);
        }
    }

    public void kill(Vector3 ragdoll)
    {
        if (!isAlive)
        {
            return;
        }
        isAlive = false;
        _lastDead = Time.realtimeSinceStartup;
        if (asset != null)
        {
            health = 0;
            if (asset.isForage)
            {
                model.Find("Forage")?.gameObject.SetActive(value: false);
            }
            else
            {
                if (model != null)
                {
                    model.gameObject.SetActive(value: false);
                }
                if (stump != null)
                {
                    stump.gameObject.SetActive(isEnabled);
                }
                if (!Dedicator.IsDedicatedServer && asset.hasDebris && GraphicsSettings.debris)
                {
                    ragdoll.y += 8f;
                    ragdoll.x += UnityEngine.Random.Range(-16f, 16f);
                    ragdoll.z += UnityEngine.Random.Range(-16f, 16f);
                    ragdoll *= (float)((Player.player != null && Player.player.skills.boost == EPlayerBoost.FLIGHT) ? 4 : 2);
                    if (model != null && asset.modelGameObject != null)
                    {
                        Vector3 position = model.position + model.up * asset.DebrisVerticalOffset;
                        GameObject original = ((!(asset.debrisGameObject == null)) ? asset.debrisGameObject : asset.modelGameObject);
                        Transform transform = UnityEngine.Object.Instantiate(original, position, model.rotation).transform;
                        transform.name = asset.name + "_Debris";
                        transform.localScale = model.localScale;
                        transform.tag = "Debris";
                        transform.gameObject.layer = 12;
                        transform.gameObject.AddComponent<Rigidbody>();
                        transform.GetComponent<Rigidbody>().interpolation = RigidbodyInterpolation.Interpolate;
                        transform.GetComponent<Rigidbody>().collisionDetectionMode = CollisionDetectionMode.Discrete;
                        transform.GetComponent<Rigidbody>().AddForce(ragdoll);
                        transform.GetComponent<Rigidbody>().drag = 1f;
                        transform.GetComponent<Rigidbody>().angularDrag = 1f;
                        UnityEngine.Object.Destroy(transform.gameObject, 8f);
                        if (stump != null && isEnabled && asset.ShouldIgnoreCollisionBetweenStumpAndDebris)
                        {
                            Collider component = transform.GetComponent<Collider>();
                            if (component != null)
                            {
                                stump.GetComponents(colliders);
                                for (int i = 0; i < colliders.Count; i++)
                                {
                                    Physics.IgnoreCollision(component, colliders[i]);
                                }
                            }
                        }
                    }
                }
            }
        }
        if ((bool)skybox)
        {
            skybox.gameObject.SetActive(value: false);
        }
    }

    public void forceFullEnable()
    {
        isEnabled = true;
        if (model != null)
        {
            model.gameObject.SetActive(value: true);
        }
        if (stump != null)
        {
            stump.gameObject.SetActive(value: true);
        }
    }

    public void enable()
    {
        isEnabled = true;
        if (asset != null && asset.isForage)
        {
            if (model != null && areConditionsMet)
            {
                model.gameObject.SetActive(value: true);
            }
            return;
        }
        if (model != null && areConditionsMet)
        {
            model.gameObject.SetActive(isAlive);
        }
        if (stump != null && areConditionsMet)
        {
            stump.gameObject.SetActive(!isAlive);
        }
    }

    public void enableSkybox()
    {
        isSkyboxEnabled = true;
        if (skybox != null && areConditionsMet)
        {
            skybox.gameObject.SetActive(isAlive);
        }
    }

    public void disable()
    {
        isEnabled = false;
        if (model != null)
        {
            model.gameObject.SetActive(value: false);
        }
        if (stump != null)
        {
            stump.gameObject.SetActive(value: false);
        }
    }

    public void disableSkybox()
    {
        isSkyboxEnabled = false;
        if (skybox != null)
        {
            skybox.gameObject.SetActive(value: false);
        }
    }

    public void destroy()
    {
        if (model != null)
        {
            UnityEngine.Object.Destroy(model.gameObject);
        }
        if (stump != null)
        {
            UnityEngine.Object.Destroy(stump.gameObject);
        }
        if (skybox != null)
        {
            UnityEngine.Object.Destroy(skybox.gameObject);
        }
    }

    internal Vector3 GetEffectSpawnPosition()
    {
        if (model == null)
        {
            return point;
        }
        Transform transform = model.Find("Effect");
        if (transform != null)
        {
            return transform.position;
        }
        if (asset.hasDebris)
        {
            return model.position + Vector3.up * 8f;
        }
        return model.position;
    }

    /// <summary>
    /// Used if the asset has holiday restrictions.
    /// </summary>
    private void updateConditions()
    {
        if (asset == null)
        {
            return;
        }
        bool flag = HolidayUtil.isHolidayActive(asset.holidayRestriction);
        if (areConditionsMet == flag)
        {
            return;
        }
        areConditionsMet = flag;
        if (areConditionsMet)
        {
            if (isEnabled)
            {
                enable();
            }
            if (isSkyboxEnabled)
            {
                enableSkybox();
            }
        }
        else
        {
            if (isEnabled)
            {
                disable();
            }
            if (isSkyboxEnabled)
            {
                disableSkybox();
            }
        }
    }

    public ResourceSpawnpoint(byte newType, ushort newID, Guid newGuid, Vector3 newPoint, bool newGenerated, NetId netId)
    {
        type = newType;
        id = newID;
        guid = newGuid;
        _point = newPoint;
        _isGenerated = newGenerated;
        if (guid == Guid.Empty)
        {
            _asset = Assets.find(EAssetType.RESOURCE, id) as ResourceAsset;
            if (asset != null)
            {
                UnturnedLog.info($"Tree without GUID loaded by legacy ID {id}, updating to {asset.GUID:N} \"{asset.FriendlyName}\"");
                guid = asset.GUID;
            }
        }
        else
        {
            _asset = Assets.find(guid) as ResourceAsset;
            if (!Dedicator.IsDedicatedServer)
            {
                ClientAssetIntegrity.QueueRequest(guid, asset, "Tree");
            }
            if (asset == null)
            {
                ClientAssetIntegrity.ServerAddKnownMissingAsset(guid, "Tree");
            }
        }
        if (asset == null)
        {
            return;
        }
        health = asset.health;
        isAlive = true;
        areConditionsMet = true;
        float num = Mathf.Sin((point.x + 4096f) * 32f + (point.z + 4096f) * 32f);
        float t = (num + 1f) * 0.5f;
        float x = Mathf.Lerp(asset.MinRandomAngleDeviation, asset.MaxRandomAngleDeviation, t);
        _angle = Quaternion.Euler(x, num * 360f, 0f);
        float num2 = Mathf.Lerp(asset.MinRandomUniformScale, asset.MaxRandomUniformScale, t);
        _scale = new Vector3(num2, num2, num2);
        GameObject gameObject = null;
        if (asset.modelGameObject != null)
        {
            gameObject = asset.modelGameObject;
        }
        Vector3 position = point + Vector3.up * scale.y * asset.verticalOffset;
        if (gameObject != null)
        {
            GameObject gameObject2 = UnityEngine.Object.Instantiate(gameObject, position, angle);
            _model = gameObject2.transform;
            gameObject2.GetOrAddComponent<TreeRefComponent>().owner = this;
            model.name = asset.name;
            model.localScale = scale;
            if (Dedicator.IsDedicatedServer)
            {
                isEnabled = true;
            }
            else
            {
                model.gameObject.SetActive(value: false);
                if (!Level.isEditor && asset.isForage)
                {
                    Transform transform = model.Find("Forage");
                    if (transform != null)
                    {
                        transform.gameObject.AddComponent<InteractableForage>().asset = asset;
                    }
                }
                if (asset.skyboxGameObject != null)
                {
                    Quaternion rotation = angle * Quaternion.Euler(-90f, 0f, 0f);
                    GameObject gameObject3 = UnityEngine.Object.Instantiate(asset.skyboxGameObject, position, rotation);
                    _skybox = gameObject3.transform;
                    skybox.name = asset.name + "_Skybox";
                    skybox.localScale = new Vector3(skybox.localScale.x * scale.x, skybox.localScale.z * scale.z, skybox.localScale.z * scale.z);
                    if (asset.skyboxMaterial != null)
                    {
                        skybox.GetComponent<MeshRenderer>().sharedMaterial = asset.skyboxMaterial;
                    }
                    if (GraphicsSettings.landmarkQuality >= EGraphicQuality.MEDIUM)
                    {
                        enableSkybox();
                    }
                    else
                    {
                        disableSkybox();
                    }
                }
            }
            if (!netId.IsNull())
            {
                NetIdRegistry.AssignTransform(netId, model.transform);
            }
        }
        GameObject gameObject4 = null;
        if (asset.stumpGameObject != null)
        {
            gameObject4 = asset.stumpGameObject;
        }
        if (gameObject4 != null)
        {
            _stump = UnityEngine.Object.Instantiate(gameObject4, position, angle).transform;
            stump.name = asset.name + "_Stump";
            stump.localScale = scale;
            stump.gameObject.SetActive(value: false);
        }
        if (asset.holidayRestriction != 0 && !Level.isEditor)
        {
            updateConditions();
        }
    }

    public ResourceSpawnpoint(byte newType, ushort newID, Vector3 newPoint, bool newGenerated, NetId netId)
        : this(newType, newID, Guid.Empty, newPoint, newGenerated, netId)
    {
    }

    public ResourceSpawnpoint(ushort newID, Vector3 newPoint, bool newGenerated, NetId netId)
        : this(0, newID, Guid.Empty, newPoint, newGenerated, netId)
    {
    }

    public ResourceSpawnpoint(ushort newID, Guid guid, Vector3 newPoint, bool newGenerated, NetId netId)
        : this(0, newID, guid, newPoint, newGenerated, netId)
    {
    }
}
