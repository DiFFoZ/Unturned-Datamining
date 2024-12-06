using System;
using Pathfinding;
using UnityEngine;

namespace SDG.Unturned;

public class InteractableObjectBinaryState : InteractableObject
{
    public delegate void UsedChanged(InteractableObjectBinaryState sender);

    private bool _isUsed;

    private bool isInit;

    private float lastUsed = -9999f;

    private Animation animationComponent;

    private AudioSource audioSourceComponent;

    private NavmeshCut cutComponent;

    private float cutHeight;

    private Material material;

    private GameObject toggleGameObject;

    /// <summary>
    /// Number of event hooks monitoring or controlling this.
    /// Used to allow client to control remote objects on server.
    /// </summary>
    public int modHookCounter;

    private float lastEffect;

    public bool isUsed => _isUsed;

    public bool isUsable
    {
        get
        {
            if (Time.realtimeSinceStartup - lastUsed > base.objectAsset.interactabilityDelay)
            {
                if (base.objectAsset.interactabilityPower != 0)
                {
                    return base.isWired;
                }
                return true;
            }
            return false;
        }
    }

    /// <summary>
    /// Invoked after state is first loaded, synced from server when entering relevancy, or reset.
    /// </summary>
    public event UsedChanged onStateInitialized;

    /// <summary>
    /// Invoked after interaction changes state.
    /// </summary>
    public event UsedChanged onStateChanged;

    public bool checkCanReset(float multiplier)
    {
        if (isUsed && base.objectAsset.interactabilityReset > 1f)
        {
            return Time.realtimeSinceStartup - lastUsed > base.objectAsset.interactabilityReset * multiplier;
        }
        return false;
    }

    private void initAnimationComponent()
    {
        string text = _objectAsset.interactabilityChildPathOverride;
        if (string.IsNullOrEmpty(text))
        {
            text = "Root";
        }
        Transform transform = base.transform.Find(text);
        if (transform != null)
        {
            animationComponent = transform.GetComponent<Animation>();
            animationComponent.playAutomatically = false;
            animationComponent.clip = null;
        }
    }

    private void updateAnimationComponent(bool applyInstantly)
    {
        if (animationComponent == null)
        {
            return;
        }
        string animation = (isUsed ? "Open" : "Close");
        if (!(animationComponent.GetClip(animation) == null))
        {
            animationComponent.Play(animation);
            if (applyInstantly)
            {
                animationComponent[animation].normalizedTime = 1f;
            }
        }
    }

    private void initAudioSourceComponent()
    {
        audioSourceComponent = base.transform.GetComponent<AudioSource>();
    }

    private void updateAudioSourceComponent()
    {
        if (audioSourceComponent != null && !Dedicator.IsDedicatedServer)
        {
            audioSourceComponent.Play();
        }
    }

    private NavmeshCut initCutComponentFromBox(BoxCollider box)
    {
        NavmeshCut navmeshCut = box.gameObject.AddComponent<NavmeshCut>();
        navmeshCut.type = NavmeshCut.MeshType.Rectangle;
        navmeshCut.updateDistance = 0.4f;
        navmeshCut.isDual = false;
        navmeshCut.cutsAddedGeom = true;
        navmeshCut.updateRotationDistance = 10f;
        navmeshCut.useRotation = true;
        navmeshCut.center = box.center;
        navmeshCut.rectangleSize = new Vector2(box.size.x, box.size.z);
        navmeshCut.height = box.size.y;
        UnityEngine.Object.Destroy(box);
        return navmeshCut;
    }

    private void InitNav()
    {
        if (base.objectAsset.interactabilityNav == EObjectInteractabilityNav.NONE)
        {
            return;
        }
        Transform transform = base.transform.Find("Nav");
        if (!(transform != null))
        {
            return;
        }
        Transform transform2 = transform.Find("Blocker");
        if (!(transform2 != null))
        {
            return;
        }
        cutComponent = transform2.GetComponent<NavmeshCut>();
        if (cutComponent == null)
        {
            BoxCollider component = transform2.GetComponent<BoxCollider>();
            if (component != null)
            {
                cutComponent = initCutComponentFromBox(component);
            }
        }
        if (cutComponent != null)
        {
            cutHeight = cutComponent.height;
        }
    }

    private void UpdateNav()
    {
        bool flag;
        switch (base.objectAsset.interactabilityNav)
        {
        default:
            return;
        case EObjectInteractabilityNav.ON:
            flag = isUsed;
            break;
        case EObjectInteractabilityNav.OFF:
            flag = !isUsed;
            break;
        }
        if (cutComponent != null)
        {
            if (flag)
            {
                cutComponent.height = cutHeight;
            }
            else
            {
                cutComponent.height = 0f;
            }
            cutComponent.ForceUpdate();
        }
        else if (owningLevelObject != null)
        {
            owningLevelObject.SetInteractableWantsNavActive(flag);
        }
    }

    private void initToggleGameObject()
    {
        Transform transform = base.transform.FindChildRecursive("Toggle");
        LightLODTool.applyLightLOD(transform);
        if (transform != null)
        {
            material = HighlighterTool.getMaterialInstance(transform.parent);
            toggleGameObject = transform.gameObject;
        }
    }

    private void updateToggleGameObject()
    {
        if (!(toggleGameObject != null))
        {
            return;
        }
        if (base.objectAsset.interactabilityPower == EObjectInteractabilityPower.STAY)
        {
            if (material != null)
            {
                material.SetColor("_EmissionColor", (isUsed && base.isWired) ? new Color(2f, 2f, 2f) : Color.black);
            }
            toggleGameObject.SetActive(isUsed && base.isWired);
        }
        else
        {
            if (material != null)
            {
                material.SetColor("_EmissionColor", isUsed ? new Color(2f, 2f, 2f) : Color.black);
            }
            toggleGameObject.SetActive(isUsed);
        }
    }

    public void updateToggle(bool newUsed)
    {
        lastUsed = Time.realtimeSinceStartup;
        _isUsed = newUsed;
        updateAnimationComponent(applyInstantly: false);
        UpdateNav();
        updateAudioSourceComponent();
        updateToggleGameObject();
        this.onStateChanged?.Invoke(this);
    }

    protected override void updateWired()
    {
        updateToggleGameObject();
    }

    public override void updateState(Asset asset, byte[] state)
    {
        base.updateState(asset, state);
        _isUsed = state[0] == 1;
        if (!isInit)
        {
            isInit = true;
            initAnimationComponent();
            InitNav();
            initAudioSourceComponent();
            initToggleGameObject();
        }
        updateAnimationComponent(applyInstantly: true);
        UpdateNav();
        updateToggleGameObject();
        this.onStateInitialized?.Invoke(this);
    }

    public void SetUsedFromClientOrServer(bool newUsed, InteractableObjectBinaryStateEventHook.EListenServerHostMode listenServerHostMode)
    {
        if (newUsed != isUsed)
        {
            if (!Dedicator.IsDedicatedServer && (!Provider.isServer || ((listenServerHostMode == InteractableObjectBinaryStateEventHook.EListenServerHostMode.RequestAsClient || listenServerHostMode != InteractableObjectBinaryStateEventHook.EListenServerHostMode.OverrideState) ? true : false)))
            {
                ObjectManager.toggleObjectBinaryState(base.transform, newUsed);
            }
            else
            {
                ObjectManager.forceObjectBinaryState(base.transform, newUsed);
            }
        }
    }

    public override void use()
    {
        bool flag = !isUsed;
        EffectAsset effectAsset = base.objectAsset.FindInteractabilityEffectAsset();
        if (effectAsset != null && Time.realtimeSinceStartup - lastEffect > 1f)
        {
            lastEffect = Time.realtimeSinceStartup;
            Transform transform = base.transform.Find("Effect");
            if (transform != null)
            {
                EffectManager.effect(effectAsset, transform.position, transform.forward);
            }
            else if (flag)
            {
                Transform transform2 = base.transform.Find("Effect_On");
                if (transform2 != null)
                {
                    EffectManager.effect(effectAsset, transform2.position, transform2.forward);
                }
            }
            else if (!flag)
            {
                Transform transform3 = base.transform.Find("Effect_Off");
                if (transform3 != null)
                {
                    EffectManager.effect(effectAsset, transform3.position, transform3.forward);
                }
            }
        }
        ObjectManager.toggleObjectBinaryState(base.transform, flag);
    }

    public override bool checkInteractable()
    {
        return !base.objectAsset.interactabilityRemote;
    }

    public override bool checkUseable()
    {
        if (base.objectAsset.interactabilityPower == EObjectInteractabilityPower.NONE || base.isWired)
        {
            return base.objectAsset.areInteractabilityConditionsMet(Player.player);
        }
        return false;
    }

    public override bool checkHint(out EPlayerMessage message, out string text, out Color color)
    {
        for (int i = 0; i < base.objectAsset.interactabilityConditions.Length; i++)
        {
            INPCCondition iNPCCondition = base.objectAsset.interactabilityConditions[i];
            if (!iNPCCondition.isConditionMet(Player.player))
            {
                message = EPlayerMessage.CONDITION;
                text = iNPCCondition.formatCondition(Player.player);
                color = Color.white;
                return true;
            }
        }
        if (base.objectAsset.interactabilityPower != 0 && !base.isWired)
        {
            message = EPlayerMessage.POWER;
        }
        else if (isUsed)
        {
            switch (base.objectAsset.interactabilityHint)
            {
            case EObjectInteractabilityHint.DOOR:
                message = EPlayerMessage.DOOR_CLOSE;
                break;
            case EObjectInteractabilityHint.SWITCH:
                message = EPlayerMessage.SPOT_OFF;
                break;
            case EObjectInteractabilityHint.FIRE:
                message = EPlayerMessage.FIRE_OFF;
                break;
            case EObjectInteractabilityHint.GENERATOR:
                message = EPlayerMessage.GENERATOR_OFF;
                break;
            case EObjectInteractabilityHint.USE:
                message = EPlayerMessage.USE;
                break;
            case EObjectInteractabilityHint.CUSTOM:
                message = EPlayerMessage.INTERACT;
                text = base.objectAsset.interactabilityText;
                color = Color.white;
                return true;
            default:
                message = EPlayerMessage.NONE;
                break;
            }
        }
        else
        {
            switch (base.objectAsset.interactabilityHint)
            {
            case EObjectInteractabilityHint.DOOR:
                message = EPlayerMessage.DOOR_OPEN;
                break;
            case EObjectInteractabilityHint.SWITCH:
                message = EPlayerMessage.SPOT_ON;
                break;
            case EObjectInteractabilityHint.FIRE:
                message = EPlayerMessage.FIRE_ON;
                break;
            case EObjectInteractabilityHint.GENERATOR:
                message = EPlayerMessage.GENERATOR_ON;
                break;
            case EObjectInteractabilityHint.USE:
                message = EPlayerMessage.USE;
                break;
            case EObjectInteractabilityHint.CUSTOM:
                message = EPlayerMessage.INTERACT;
                text = base.objectAsset.interactabilityText;
                color = Color.white;
                return true;
            default:
                message = EPlayerMessage.NONE;
                break;
            }
        }
        text = "";
        color = Color.white;
        return true;
    }

    private void OnEnable()
    {
        updateAnimationComponent(applyInstantly: true);
    }

    private void OnDestroy()
    {
        if (material != null)
        {
            UnityEngine.Object.DestroyImmediate(material);
        }
    }

    [Obsolete("Matches behavior from before addition of EListenServerHostMode.")]
    public void setUsedFromClientOrServer(bool newUsed)
    {
        SetUsedFromClientOrServer(newUsed, InteractableObjectBinaryStateEventHook.EListenServerHostMode.RequestAsClient);
    }
}
