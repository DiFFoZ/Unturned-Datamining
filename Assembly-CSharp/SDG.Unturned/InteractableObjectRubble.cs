using System;
using System.Collections.Generic;
using UnityEngine;

namespace SDG.Unturned;

public class InteractableObjectRubble : MonoBehaviour, IExplosionDamageable, IEquatable<IExplosionDamageable>
{
    internal RubbleInfo[] rubbleInfos;

    private GameObject aliveGameObject;

    private GameObject deadGameObject;

    private Transform finaleTransform;

    private Transform dropTransform;

    internal LevelObject owningLevelObject;

    private static List<byte> tempIndices = new List<byte>(8);

    public ObjectAsset asset { get; protected set; }

    public bool IsEligibleForExplosionDamage
    {
        get
        {
            if (asset != null && !asset.rubbleProofExplosion)
            {
                return !isAllDead();
            }
            return false;
        }
    }

    public bool Equals(IExplosionDamageable obj)
    {
        return this == obj;
    }

    public Vector3 GetClosestPointToExplosion(Vector3 explosionCenter)
    {
        return CollisionUtil.ClosestPoint(base.gameObject, explosionCenter, includeInactive: false);
    }

    public void ApplyExplosionDamage(in ExplosionParameters explosionParameters, ref ExplosionDamageParameters damageParameters)
    {
        if (!damageParameters.shouldAffectObjects)
        {
            return;
        }
        for (byte b = 0; b < getSectionCount(); b++)
        {
            RubbleInfo sectionInfo = getSectionInfo(b);
            if (!sectionInfo.isDead)
            {
                Vector3 vector = sectionInfo.section.position;
                if (sectionInfo.aliveGameObject != null)
                {
                    vector = CollisionUtil.ClosestPoint(sectionInfo.section.gameObject, explosionParameters.point, includeInactive: false);
                }
                Vector3 vector2 = vector - explosionParameters.point;
                float magnitude = vector2.magnitude;
                if (!(magnitude > explosionParameters.damageRadius))
                {
                    Vector3 direction = vector2 / magnitude;
                    if (!damageParameters.LineOfSightTest(explosionParameters.point, direction, magnitude, out var hit) || !(hit.transform != null) || hit.transform.IsChildOf(base.transform))
                    {
                        ObjectManager.damage(base.transform, direction, b, explosionParameters.objectDamage, 1f - magnitude / explosionParameters.damageRadius, out var kill, out var xp, explosionParameters.killer, explosionParameters.damageOrigin);
                        if (kill != 0)
                        {
                            damageParameters.kills.Add(kill);
                        }
                        damageParameters.xp += xp;
                    }
                }
            }
        }
    }

    public byte getSectionCount()
    {
        return (byte)rubbleInfos.Length;
    }

    public Transform getSection(byte section)
    {
        return rubbleInfos[section].section;
    }

    public RubbleInfo getSectionInfo(byte section)
    {
        return rubbleInfos[section];
    }

    public bool isAllAlive()
    {
        for (byte b = 0; b < rubbleInfos.Length; b++)
        {
            if (rubbleInfos[b].isDead)
            {
                return false;
            }
        }
        return true;
    }

    public bool isAllDead()
    {
        for (byte b = 0; b < rubbleInfos.Length; b++)
        {
            if (!rubbleInfos[b].isDead)
            {
                return false;
            }
        }
        return true;
    }

    public bool TryGetRandomAliveSectionIndex(out byte sectionIndex)
    {
        if (rubbleInfos == null || rubbleInfos.Length < 1)
        {
            sectionIndex = 0;
            return false;
        }
        tempIndices.Clear();
        for (byte b = 0; b < rubbleInfos.Length; b++)
        {
            if (!rubbleInfos[b].isDead)
            {
                tempIndices.Add(b);
            }
        }
        if (tempIndices.IsEmpty())
        {
            sectionIndex = 0;
            return false;
        }
        sectionIndex = tempIndices.RandomOrDefault();
        return true;
    }

    public bool IsSectionIndexValid(byte sectionIndex)
    {
        if (rubbleInfos == null)
        {
            return false;
        }
        return sectionIndex < rubbleInfos.Length;
    }

    public bool isSectionDead(byte section)
    {
        return rubbleInfos[section].isDead;
    }

    public void askDamage(byte section, ushort amount)
    {
        if (section == byte.MaxValue)
        {
            for (section = 0; section < rubbleInfos.Length; section++)
            {
                rubbleInfos[section].askDamage(amount);
            }
        }
        else
        {
            rubbleInfos[section].askDamage(amount);
        }
    }

    public byte checkCanReset(float multiplier)
    {
        for (byte b = 0; b < rubbleInfos.Length; b++)
        {
            if (rubbleInfos[b].isDead && asset.rubbleReset > 1f && Time.realtimeSinceStartup - rubbleInfos[b].lastDead > asset.rubbleReset * multiplier)
            {
                return b;
            }
        }
        return byte.MaxValue;
    }

    public byte getSection(Transform hitTransform)
    {
        if (hitTransform != null)
        {
            for (byte b = 0; b < rubbleInfos.Length; b++)
            {
                RubbleInfo rubbleInfo = rubbleInfos[b];
                if (hitTransform.IsChildOf(rubbleInfo.section))
                {
                    return b;
                }
            }
        }
        return byte.MaxValue;
    }

    public void updateRubble(byte section, bool isAlive, bool playEffect, Vector3 ragdoll)
    {
        if (rubbleInfos == null || section >= rubbleInfos.Length)
        {
            return;
        }
        RubbleInfo rubbleInfo = rubbleInfos[section];
        if (isAlive)
        {
            rubbleInfo.health = asset.rubbleHealth;
        }
        else
        {
            rubbleInfo.lastDead = Time.realtimeSinceStartup;
            rubbleInfo.health = 0;
        }
        bool flag = isAllDead();
        if (rubbleInfo.aliveGameObject != null)
        {
            rubbleInfo.aliveGameObject.SetActive(!rubbleInfo.isDead);
        }
        if (rubbleInfo.deadGameObject != null)
        {
            rubbleInfo.deadGameObject.SetActive(rubbleInfo.isDead && (!flag || asset.IsRubbleFinaleEffectRefNull()));
        }
        if (aliveGameObject != null)
        {
            aliveGameObject.SetActive(!flag);
        }
        if (deadGameObject != null)
        {
            deadGameObject.SetActive(flag);
        }
        if (!Dedicator.IsDedicatedServer && playEffect)
        {
            if (rubbleInfo.ragdolls != null && GraphicsSettings.debris && rubbleInfo.isDead)
            {
                for (int i = 0; i < rubbleInfo.ragdolls.Length; i++)
                {
                    RubbleRagdollInfo rubbleRagdollInfo = rubbleInfo.ragdolls[i];
                    if (rubbleRagdollInfo != null)
                    {
                        Vector3 force = ragdoll;
                        if (rubbleRagdollInfo.forceTransform != null)
                        {
                            force = rubbleRagdollInfo.forceTransform.forward * force.magnitude * rubbleRagdollInfo.forceTransform.localScale.z;
                            force += rubbleRagdollInfo.forceTransform.right * UnityEngine.Random.Range(-16f, 16f) * rubbleRagdollInfo.forceTransform.localScale.x;
                            force += rubbleRagdollInfo.forceTransform.up * UnityEngine.Random.Range(-16f, 16f) * rubbleRagdollInfo.forceTransform.localScale.y;
                        }
                        else
                        {
                            force.y += 8f;
                            force.x += UnityEngine.Random.Range(-16f, 16f);
                            force.z += UnityEngine.Random.Range(-16f, 16f);
                        }
                        force *= (float)((Player.player != null && Player.player.skills.boost == EPlayerBoost.FLIGHT) ? 4 : 2);
                        GameObject obj = UnityEngine.Object.Instantiate(rubbleRagdollInfo.ragdollGameObject, rubbleRagdollInfo.ragdollGameObject.transform.position, rubbleRagdollInfo.ragdollGameObject.transform.rotation);
                        obj.name = "Ragdoll";
                        EffectManager.RegisterDebris(obj);
                        obj.transform.localScale = base.transform.localScale;
                        obj.SetActive(value: true);
                        obj.gameObject.AddComponent<Rigidbody>();
                        obj.GetComponent<Rigidbody>().interpolation = RigidbodyInterpolation.Interpolate;
                        obj.GetComponent<Rigidbody>().collisionDetectionMode = CollisionDetectionMode.Discrete;
                        obj.GetComponent<Rigidbody>().AddForce(force);
                        obj.GetComponent<Rigidbody>().drag = 0.5f;
                        obj.GetComponent<Rigidbody>().angularDrag = 0.1f;
                        UnityEngine.Object.Destroy(obj, 8f);
                    }
                }
            }
            if (rubbleInfo.isDead)
            {
                EffectAsset effectAsset = asset.FindRubbleEffectAsset();
                if (effectAsset != null)
                {
                    if (rubbleInfo.effectTransform != null)
                    {
                        EffectManager.effect(effectAsset, rubbleInfo.effectTransform.position, rubbleInfo.effectTransform.forward);
                    }
                    else
                    {
                        EffectManager.effect(effectAsset, rubbleInfo.section.position, Vector3.up);
                    }
                }
            }
            if (flag)
            {
                EffectAsset effectAsset2 = asset.FindRubbleFinaleEffectAsset();
                if (effectAsset2 != null)
                {
                    if (finaleTransform != null)
                    {
                        EffectManager.effect(effectAsset2, finaleTransform.position, finaleTransform.forward);
                    }
                    else
                    {
                        EffectManager.effect(effectAsset2, base.transform.position, Vector3.up);
                    }
                }
            }
        }
        if (Provider.isServer && dropTransform != null && asset.rubbleRewardID != 0 && playEffect && flag && (asset.holidayRestriction == ENPCHoliday.NONE || Provider.modeConfigData.Objects.Allow_Holiday_Drops) && UnityEngine.Random.value <= asset.rubbleRewardProbability)
        {
            int value = UnityEngine.Random.Range(asset.rubbleRewardsMin, asset.rubbleRewardsMax + 1);
            value = Mathf.Clamp(value, 0, 100);
            for (int j = 0; j < value; j++)
            {
                ushort num = SpawnTableTool.ResolveLegacyId(asset.rubbleRewardID, EAssetType.ITEM, OnGetSpawnTableErrorContext);
                if (num != 0)
                {
                    ItemManager.dropItem(new Item(num, EItemOrigin.NATURE), dropTransform.position, playEffect: false, Dedicator.IsDedicatedServer, wideSpread: false);
                }
            }
        }
        if (asset.RubbleNavMode == EObjectRubbleNavMode.DeactivateIfAllDead)
        {
            owningLevelObject?.SetRubbleWantsNavActive(!flag);
        }
    }

    public void updateState(Asset asset, byte[] state)
    {
        this.asset = asset as ObjectAsset;
        Transform transform = base.transform.Find("Sections");
        if (transform != null)
        {
            int num = Mathf.Min(transform.childCount, 8);
            rubbleInfos = new RubbleInfo[num];
            for (int i = 0; i < rubbleInfos.Length; i++)
            {
                Transform section = transform.Find("Section_" + i);
                RubbleInfo rubbleInfo = new RubbleInfo();
                rubbleInfo.section = section;
                rubbleInfos[i] = rubbleInfo;
            }
            Transform transform2 = base.transform.Find("Alive");
            if (transform2 != null)
            {
                aliveGameObject = transform2.gameObject;
            }
            Transform transform3 = base.transform.Find("Dead");
            if (transform3 != null)
            {
                deadGameObject = transform3.gameObject;
            }
            finaleTransform = base.transform.Find("Finale");
        }
        else
        {
            rubbleInfos = new RubbleInfo[1];
            RubbleInfo rubbleInfo2 = new RubbleInfo();
            rubbleInfo2.section = base.transform;
            rubbleInfos[0] = rubbleInfo2;
        }
        dropTransform = base.transform.Find("Drop");
        for (byte b = 0; b < rubbleInfos.Length; b++)
        {
            RubbleInfo rubbleInfo3 = rubbleInfos[b];
            Transform section2 = rubbleInfo3.section;
            Transform transform4 = section2.Find("Alive");
            if (transform4 != null)
            {
                rubbleInfo3.aliveGameObject = transform4.gameObject;
            }
            Transform transform5 = section2.Find("Dead");
            if (transform5 != null)
            {
                rubbleInfo3.deadGameObject = transform5.gameObject;
            }
            Transform transform6 = section2.Find("Ragdolls");
            if (transform6 != null)
            {
                rubbleInfo3.ragdolls = new RubbleRagdollInfo[transform6.childCount];
                for (int j = 0; j < rubbleInfo3.ragdolls.Length; j++)
                {
                    Transform transform7 = transform6.Find("Ragdoll_" + j);
                    Transform transform8 = transform7.Find("Ragdoll");
                    if (transform8 != null)
                    {
                        rubbleInfo3.ragdolls[j] = new RubbleRagdollInfo();
                        rubbleInfo3.ragdolls[j].ragdollGameObject = transform8.gameObject;
                        rubbleInfo3.ragdolls[j].forceTransform = transform7.Find("Force");
                    }
                }
            }
            else
            {
                Transform transform9 = section2.Find("Ragdoll");
                if (transform9 != null)
                {
                    rubbleInfo3.ragdolls = new RubbleRagdollInfo[1];
                    rubbleInfo3.ragdolls[0] = new RubbleRagdollInfo();
                    rubbleInfo3.ragdolls[0].ragdollGameObject = transform9.gameObject;
                    rubbleInfo3.ragdolls[0].forceTransform = section2.Find("Force");
                }
            }
            rubbleInfo3.effectTransform = section2.Find("Effect");
        }
        for (byte b2 = 0; b2 < rubbleInfos.Length; b2++)
        {
            bool isAlive = (state[^1] & Types.SHIFTS[b2]) == Types.SHIFTS[b2];
            updateRubble(b2, isAlive, playEffect: false, Vector3.zero);
        }
    }

    private string OnGetSpawnTableErrorContext()
    {
        return asset?.FriendlyName + " rubble reward";
    }

    void IExplosionDamageable.ApplyExplosionDamage(in ExplosionParameters explosionParameters, ref ExplosionDamageParameters damageParameters)
    {
        ApplyExplosionDamage(in explosionParameters, ref damageParameters);
    }
}
