using System;
using System.Collections.Generic;
using SDG.Framework.Water;
using Steamworks;
using UnityEngine;

namespace SDG.Unturned;

public class Zombie : MonoBehaviour
{
    private static List<RegionCoordinate> regionsInRadius = new List<RegionCoordinate>(4);

    private static List<Transform> structuresInRadius = new List<Transform>();

    private static List<InteractableVehicle> vehiclesInRadius = new List<InteractableVehicle>();

    private static List<Transform> barricadesInRadius = new List<Transform>();

    private static readonly float ATTACK_BARRICADE = 16f;

    private static readonly float ATTACK_VEHICLE = 16f;

    private static readonly float ATTACK_PLAYER = 2f;

    private Transform skeleton;

    private Transform rightHook;

    private SkinnedMeshRenderer renderer_0;

    private SkinnedMeshRenderer renderer_1;

    private Transform eyes;

    private Transform radiation;

    private Transform burner;

    private Transform acid;

    private Transform acidNuclear;

    private Transform electric;

    private ParticleSystem sparkSystem;

    private ParticleSystem fireSystem;

    private AudioSource fireAudio;

    private AudioSource rootAudioSource;

    private Material skinMaterial;

    private Transform attachmentModel_0;

    private Transform attachmentModel_1;

    private Material attachmentMaterial_0;

    private Material attachmentMaterial_1;

    public ushort id;

    public byte bound;

    public byte type;

    public EZombieSpeciality speciality;

    public byte shirt;

    public byte pants;

    public byte hat;

    public byte gear;

    private byte _move;

    private string moveAnim;

    private byte _idle;

    public string idleAnim;

    public bool isUpdated;

    private AIPath seeker;

    private Player player;

    private Transform barricade;

    private Transform structure;

    private InteractableVehicle targetObstructionVehicle;

    private InteractableVehicle targetPassengerVehicle;

    private Transform target;

    private Animation animator;

    private float lastHunted;

    private float lastTarget;

    private float lastLeave;

    private float lastRelocate;

    private float lastSpecial;

    private float lastAttack;

    private float lastStartle;

    private float lastStun;

    private float lastGroan;

    private float lastRegen;

    private float lastStuck;

    private Vector3 cameFrom;

    private bool isPulled;

    private float lastPull;

    private float pullDelay;

    private float groanDelay;

    private float leaveTime;

    private float throwTime;

    private float boulderTime;

    private float spitTime;

    private float acidTime;

    private float chargeTime;

    private float sparkTime;

    private float windTime;

    private float fireTime;

    private float attackTime;

    private float startleTime;

    private float stunTime;

    private bool isThrowRelocating;

    private bool isThrowingBoulder;

    private bool isSpittingAcid;

    private bool isChargingSpark;

    private bool isStompingWind;

    private bool isBreathingFire;

    private bool isPlayingBoulder;

    private bool isPlayingSpit;

    private bool isPlayingCharge;

    private bool isPlayingWind;

    private bool isPlayingFire;

    private bool isPlayingAttack;

    private bool isPlayingStartle;

    private bool isPlayingStun;

    private Vector3 lastUpdatedPos;

    private float lastUpdatedAngle;

    private Vector3 interpPositionTarget;

    private float interpYawTarget;

    private bool isMoving;

    private bool isAttacking;

    private bool isVisible;

    private bool isWandering;

    private bool isTicking;

    private bool _isHunting;

    private EHuntType huntType;

    private bool isLeaving;

    private bool isStunned;

    private bool isStuck;

    private Vector3 leaveTo;

    private float _lastDead;

    public bool isDead;

    private ushort health;

    private ushort maxHealth;

    private Vector3 ragdoll;

    private EZombiePath path;

    private float specialStartleDelay;

    private float specialAttackDelay;

    private float specialUseDelay;

    private float flashbangDelay;

    private float lastFlashbang;

    private float boulderThrowDelay;

    private Transform boulderItem;

    private float fireDamage;

    private bool hasUpdateVisibilityBeenCalledYet;

    private bool needsTickForPlacement;

    private float lastTick;

    internal ZombieRegion zombieRegion;

    private bool isCountedAsAliveInZombieRegion;

    private bool isCountedAsAliveBossInZombieRegion;

    private static readonly AssetReference<EffectAsset> KuwaitBossFlashbangRef = new AssetReference<EffectAsset>("5436f56485c841a7bbec8e79a163ad19");

    private static readonly AssetReference<EffectAsset> Zombie_0_Ref = new AssetReference<EffectAsset>("000f550dc3d44586b7fc0f6e5b2530d9");

    private static readonly AssetReference<EffectAsset> Zombie_1_Ref = new AssetReference<EffectAsset>("f2f0d31897024317b32b58c00c1f78dd");

    private static readonly AssetReference<EffectAsset> Zombie_2_Ref = new AssetReference<EffectAsset>("469414f0a1b047c58732bb6076b0c035");

    private static readonly AssetReference<EffectAsset> Zombie_3_Ref = new AssetReference<EffectAsset>("ae477aac40b64d3c8ce8e538daffecf5");

    private static readonly AssetReference<EffectAsset> Zombie_4_Ref = new AssetReference<EffectAsset>("9fd759eda4b746dfb9f2599bf8f27219");

    private static readonly AssetReference<EffectAsset> Zombie_5_Ref = new AssetReference<EffectAsset>("50872061be8e411ea28780fcb7aa7cef");

    private static readonly AssetReference<EffectAsset> Zombie_6_Ref = new AssetReference<EffectAsset>("23363b069ad740819f1d7131656f8ca7");

    private static readonly AssetReference<EffectAsset> Zombie_7_Ref = new AssetReference<EffectAsset>("36b272f5be8c4427b0fdd0625f361c15");

    private ushort hatID
    {
        get
        {
            if (!speciality.IsDLVolatile())
            {
                return 0;
            }
            return 960;
        }
    }

    private ushort gearID
    {
        get
        {
            if (!speciality.IsDLVolatile())
            {
                return 0;
            }
            return 961;
        }
    }

    public byte move
    {
        get
        {
            return _move;
        }
        set
        {
            _move = value;
            moveAnim = "Move_" + move;
        }
    }

    public byte idle
    {
        get
        {
            return _idle;
        }
        set
        {
            _idle = value;
            idleAnim = "Idle_" + idle;
        }
    }

    public bool isHunting
    {
        get
        {
            return _isHunting;
        }
        set
        {
            if (value == isHunting)
            {
                return;
            }
            _isHunting = value;
            if (isHunting)
            {
                needsTickForPlacement = false;
                setTicking(wantsToTick: true);
                if (speciality == EZombieSpeciality.FLANKER_FRIENDLY)
                {
                    ZombieManager.sendZombieSpeciality(this, EZombieSpeciality.FLANKER_STALK);
                }
                return;
            }
            if (!needsTickForPlacement)
            {
                setTicking(wantsToTick: false);
            }
            if (isWandering)
            {
                isWandering = false;
                ZombieManager.wanderingCount--;
            }
            if (speciality == EZombieSpeciality.FLANKER_STALK)
            {
                ZombieManager.sendZombieSpeciality(this, EZombieSpeciality.FLANKER_FRIENDLY);
            }
        }
    }

    public float lastDead => _lastDead;

    public bool isHyper
    {
        get
        {
            if (zombieRegion.isHyper)
            {
                return speciality != EZombieSpeciality.BOSS_ALL;
            }
            return false;
        }
    }

    public bool isRadioactive => zombieRegion.isRadioactive;

    public bool isBoss => speciality.IsBoss();

    public bool isMega
    {
        get
        {
            if (speciality != EZombieSpeciality.MEGA && !isBoss)
            {
                return speciality == EZombieSpeciality.BOSS_ALL;
            }
            return true;
        }
    }

    public bool isCutesy => speciality == EZombieSpeciality.SPIRIT;

    public ZombieDifficultyAsset difficulty { get; private set; }

    private void setTicking(bool wantsToTick)
    {
        if (wantsToTick)
        {
            if (!isTicking)
            {
                isTicking = true;
                ZombieManager.tickingZombies.Add(this);
                lastTick = Time.time;
            }
        }
        else if (isTicking)
        {
            isTicking = false;
            ZombieManager.tickingZombies.RemoveFast(this);
        }
    }

    public float GetHealth()
    {
        return (int)health;
    }

    public float GetMaxHealth()
    {
        return (int)maxHealth;
    }

    private float GetHorizontalAttackRangeSquared()
    {
        if (barricade != null)
        {
            return ATTACK_BARRICADE * (float)((!isMega) ? 1 : 2);
        }
        if (targetObstructionVehicle != null)
        {
            return ATTACK_VEHICLE * (float)((!isMega) ? 1 : 2);
        }
        if (targetPassengerVehicle != null || player?.movement.getVehicle() != null)
        {
            return ATTACK_VEHICLE * (float)((!isMega) ? 1 : 2);
        }
        return ATTACK_PLAYER * ((speciality == EZombieSpeciality.NORMAL) ? 0.5f : 1f) * (float)((!isMega) ? 1 : 2);
    }

    public void tellAlive(byte newType, byte newSpeciality, byte newShirt, byte newPants, byte newHat, byte newGear, Vector3 newPosition, byte newAngle)
    {
        type = newType;
        speciality = (EZombieSpeciality)newSpeciality;
        shirt = newShirt;
        pants = newPants;
        hat = newHat;
        gear = newGear;
        isDead = false;
        SetCountedAsAliveInZombieRegion(newValue: true);
        SetCountedAsAliveBossInZombieRegion(isBoss);
        base.transform.position = newPosition;
        base.transform.rotation = Quaternion.Euler(0f, newAngle * 2, 0f);
        updateDifficulty();
        updateLife();
        apply();
        updateEffects();
        updateVisibility(speciality != EZombieSpeciality.FLANKER_STALK && speciality != EZombieSpeciality.SPIRIT && speciality != EZombieSpeciality.BOSS_SPIRIT, playEffect: false);
        updateStates();
        if (Provider.isServer)
        {
            reset();
        }
    }

    public void tellDead(Vector3 newRagdoll, ERagdollEffect ragdollEffect)
    {
        isDead = true;
        SetCountedAsAliveInZombieRegion(newValue: false);
        SetCountedAsAliveBossInZombieRegion(newValue: false);
        if (zombieRegion.hasBeacon && Provider.isServer)
        {
            BeaconManager.checkBeacon(bound).despawnAlive();
        }
        _lastDead = Time.realtimeSinceStartup;
        updateLife();
        if (Provider.isServer)
        {
            stop();
        }
    }

    [Obsolete]
    public void tellState(Vector3 newPosition, byte newAngle)
    {
        tellState(newPosition, (float)(int)newAngle * 2f);
    }

    public void tellState(Vector3 newPosition, float newYaw)
    {
        lastUpdatedPos = newPosition;
        lastUpdatedAngle = newYaw;
        interpPositionTarget = newPosition;
        interpYawTarget = newYaw;
    }

    public void tellSpeciality(EZombieSpeciality newSpeciality)
    {
        speciality = newSpeciality;
        SetCountedAsAliveBossInZombieRegion(!isDead && isBoss);
        updateEffects();
        updateVisibility(speciality != EZombieSpeciality.FLANKER_STALK && speciality != EZombieSpeciality.SPIRIT && speciality != EZombieSpeciality.BOSS_SPIRIT, playEffect: true);
    }

    public void askThrow()
    {
        if (!isDead)
        {
            lastSpecial = Time.time;
            isThrowingBoulder = true;
            isPlayingBoulder = true;
            boulderItem = ((GameObject)UnityEngine.Object.Instantiate(Resources.Load("Characters/Mega_Boulder_Item"))).transform;
            boulderItem.name = "Boulder";
            boulderItem.parent = rightHook;
            boulderItem.localPosition = Vector3.zero;
            boulderItem.localRotation = Quaternion.Euler(0f, 0f, 90f);
            boulderItem.localScale = Vector3.one;
            UnityEngine.Object.Destroy(boulderItem.gameObject, 2f);
        }
    }

    public void askBoulder(Vector3 origin, Vector3 direction)
    {
        if (!isDead)
        {
            Transform obj = ((GameObject)UnityEngine.Object.Instantiate(Resources.Load("Characters/Mega_Boulder_Projectile_Server"))).transform;
            obj.name = "Boulder";
            EffectManager.RegisterDebris(obj.gameObject);
            obj.position = origin;
            obj.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler((float)UnityEngine.Random.Range(0, 2) * 180f, (float)UnityEngine.Random.Range(0, 2) * 180f, (float)UnityEngine.Random.Range(0, 2) * 180f);
            obj.localScale = Vector3.one * 1.75f;
            obj.GetComponent<Rigidbody>().AddForce(direction * 1500f);
            obj.GetComponent<Rigidbody>().AddRelativeTorque(UnityEngine.Random.Range(-500f, 500f), UnityEngine.Random.Range(-500f, 500f), UnityEngine.Random.Range(-500f, 500f), ForceMode.Force);
            obj.Find("Trap").gameObject.AddComponent<Boulder>();
            UnityEngine.Object.Destroy(obj.gameObject, 8f);
        }
    }

    public void askSpit()
    {
        if (!isDead)
        {
            lastSpecial = Time.time;
            isSpittingAcid = true;
            isPlayingSpit = true;
        }
    }

    public void askAcid(Vector3 origin, Vector3 direction)
    {
        if (!isDead)
        {
            Transform obj = ((GameObject)UnityEngine.Object.Instantiate(Resources.Load("Characters/Acid_Projectile_Server"))).transform;
            obj.name = "Acid";
            EffectManager.RegisterDebris(obj.gameObject);
            obj.position = origin;
            obj.rotation = Quaternion.LookRotation(direction);
            obj.GetComponent<Rigidbody>().AddForce(direction * 1000f);
            obj.Find("Trap").gameObject.AddComponent<Acid>().effectGuid = ((speciality == EZombieSpeciality.BOSS_NUCLEAR) ? Zombie_7_Ref : Zombie_3_Ref).GUID;
            UnityEngine.Object.Destroy(obj.gameObject, 8f);
        }
    }

    public void askCharge()
    {
        if (!isDead)
        {
            lastSpecial = Time.time;
            isChargingSpark = true;
            isPlayingCharge = true;
        }
    }

    public void askSpark(Vector3 target)
    {
        if (isDead)
        {
            return;
        }
        Vector3 vector = target - sparkSystem.transform.position;
        Vector3 normalized = vector.normalized;
        EffectAsset effectAsset = Assets.find(Zombie_4_Ref);
        if (effectAsset != null)
        {
            Transform transform = EffectManager.effect(effectAsset, sparkSystem.transform.position + normalized * 2f, normalized);
            if (transform != null)
            {
                ParticleSystem.MainModule main = transform.GetComponent<ParticleSystem>().main;
                main.startLifetime = (vector.magnitude - 2f) / 128f;
            }
        }
        EffectAsset effectAsset2 = Assets.find(Zombie_6_Ref);
        if (effectAsset2 != null)
        {
            EffectManager.effect(effectAsset2, target, -normalized);
        }
    }

    public void askStomp()
    {
        if (!isDead)
        {
            lastSpecial = Time.time;
            isStompingWind = true;
            isPlayingWind = true;
        }
    }

    public void askBreath()
    {
        if (!isDead)
        {
            lastSpecial = Time.time;
            isBreathingFire = true;
            isPlayingFire = true;
            fireDamage = 0f;
        }
    }

    public void askAttack(byte id)
    {
        if (!isDead)
        {
            lastAttack = Time.time;
            specialAttackDelay = UnityEngine.Random.Range(2f, 4f);
            isPlayingAttack = true;
            if (speciality == EZombieSpeciality.FLANKER_FRIENDLY || speciality == EZombieSpeciality.FLANKER_STALK)
            {
                updateVisibility(newVisible: true, playEffect: true);
            }
        }
    }

    public void askStartle(byte id)
    {
        if (!isDead)
        {
            lastStartle = Time.time;
            specialStartleDelay = UnityEngine.Random.Range(1f, 2f);
            isPlayingStartle = true;
        }
    }

    public void askStun(byte id)
    {
        if (!isDead)
        {
            lastStun = Time.time;
            isPlayingStun = true;
        }
    }

    public int getStunDamageThreshold()
    {
        if (isMega)
        {
            int num = ((difficulty != null) ? difficulty.Mega_Stun_Threshold : (-1));
            if (num > 0)
            {
                return num;
            }
            return 150;
        }
        int num2 = ((difficulty != null) ? difficulty.Normal_Stun_Threshold : (-1));
        if (num2 > 0)
        {
            return num2;
        }
        return 20;
    }

    public void killWithFireExplosion()
    {
        if (isDead)
        {
            return;
        }
        DamageTool.damageZombie(DamageZombieParameters.makeInstakill(this), out var _, out var _);
        if (isDead && burner != null)
        {
            EffectAsset effectAsset = Assets.find(Zombie_2_Ref);
            if (effectAsset != null)
            {
                TriggerEffectParameters parameters = new TriggerEffectParameters(effectAsset);
                parameters.position = burner.position;
                parameters.relevantDistance = EffectManager.MEDIUM;
                EffectManager.triggerEffect(parameters);
            }
            DamageTool.explode(base.transform.position + new Vector3(0f, 0.25f, 0f), 4f, EDeathCause.BURNER, CSteamID.Nil, 40f, 0f, 40f, 0f, 0f, 0f, 0f, 0f, out var _, EExplosionDamageType.ZOMBIE_FIRE, 4f, playImpactEffect: true, penetrateBuildables: false, EDamageOrigin.Flamable_Zombie_Explosion);
        }
    }

    public void askDamage(ushort amount, Vector3 newRagdoll, out EPlayerKill kill, out uint xp, bool trackKill = true, bool dropLoot = true, EZombieStunOverride stunOverride = EZombieStunOverride.None, ERagdollEffect ragdollEffect = ERagdollEffect.NONE)
    {
        kill = EPlayerKill.NONE;
        xp = 0u;
        if (amount == 0 || isDead || isDead)
        {
            return;
        }
        if (zombieRegion.hasBeacon)
        {
            amount = MathfEx.CeilToUShort((float)(int)amount / ((float)Mathf.Max(1, BeaconManager.checkBeacon(bound).initialParticipants) * 1.5f));
        }
        if (amount >= health)
        {
            health = 0;
        }
        else
        {
            health -= amount;
        }
        ragdoll = newRagdoll;
        if (health == 0)
        {
            if (isMega)
            {
                kill = EPlayerKill.MEGA;
            }
            else
            {
                kill = EPlayerKill.ZOMBIE;
            }
            xp = LevelZombies.tables[type].xp;
            if (zombieRegion.hasBeacon)
            {
                xp = (uint)((float)xp * Provider.modeConfigData.Zombies.Beacon_Experience_Multiplier);
            }
            else
            {
                if (LightingManager.isFullMoon)
                {
                    xp = (uint)((float)xp * Provider.modeConfigData.Zombies.Full_Moon_Experience_Multiplier);
                }
                if (dropLoot)
                {
                    ZombieManager.dropLoot(this);
                }
            }
            ZombieManager.sendZombieDead(this, ragdoll, ragdollEffect);
            if (isRadioactive)
            {
                DamageTool.explode(base.transform.position + new Vector3(0f, 0.25f, 0f), 2f, EDeathCause.ACID, CSteamID.Nil, 20f, 0f, 20f, 0f, 0f, 0f, 0f, 0f, out var _, EExplosionDamageType.ZOMBIE_ACID, 2f, playImpactEffect: true, penetrateBuildables: false, EDamageOrigin.Radioactive_Zombie_Explosion);
            }
            if (speciality == EZombieSpeciality.BURNER || speciality == EZombieSpeciality.BOSS_FIRE || speciality == EZombieSpeciality.BOSS_MAGMA)
            {
                DamageTool.explode(base.transform.position + new Vector3(0f, 0.25f, 0f), 4f, EDeathCause.BURNER, CSteamID.Nil, 40f, 0f, 40f, 0f, 0f, 0f, 0f, 0f, out var _, EExplosionDamageType.ZOMBIE_FIRE, 4f, playImpactEffect: true, penetrateBuildables: false, EDamageOrigin.Flamable_Zombie_Explosion);
            }
            if (trackKill)
            {
                for (int i = 0; i < Provider.clients.Count; i++)
                {
                    SteamPlayer steamPlayer = Provider.clients[i];
                    if (!(steamPlayer.player == null) && !(steamPlayer.player.movement == null) && !(steamPlayer.player.life == null) && !steamPlayer.player.life.isDead && (steamPlayer.player.movement.bound == bound || steamPlayer.player.movement.bound == byte.MaxValue))
                    {
                        steamPlayer.player.quests.trackZombieKill(this);
                    }
                }
            }
        }
        else if (Provider.modeConfigData.Zombies.Can_Stun)
        {
            if (stunOverride == EZombieStunOverride.None && !Provider.modeConfigData.Zombies.Only_Critical_Stuns)
            {
                if (amount > getStunDamageThreshold())
                {
                    stun();
                }
            }
            else if (stunOverride == EZombieStunOverride.Always)
            {
                stun();
            }
        }
        lastRegen = Time.time;
    }

    public void sendRevive(byte type, byte speciality, byte shirt, byte pants, byte hat, byte gear, Vector3 position, float angle)
    {
        ZombieManager.sendZombieAlive(this, type, speciality, shirt, pants, hat, gear, position, MeasurementTool.angleToByte(angle));
    }

    public bool checkAlert(Player newPlayer)
    {
        return player != newPlayer;
    }

    public void alert(Player newPlayer)
    {
        if (isDead || player == newPlayer)
        {
            return;
        }
        if (player == null)
        {
            if (!isHunting && !isLeaving)
            {
                if (speciality == EZombieSpeciality.CRAWLER)
                {
                    if (UnityEngine.Random.value < 0.5f)
                    {
                        ZombieManager.sendZombieStartle(this, 3);
                    }
                    else
                    {
                        ZombieManager.sendZombieStartle(this, 6);
                    }
                }
                else if (speciality == EZombieSpeciality.SPRINTER)
                {
                    if (UnityEngine.Random.value < 0.5f)
                    {
                        ZombieManager.sendZombieStartle(this, 4);
                    }
                    else
                    {
                        ZombieManager.sendZombieStartle(this, 5);
                    }
                }
                else
                {
                    ZombieManager.sendZombieStartle(this, (byte)UnityEngine.Random.Range(0, 3));
                }
            }
            isHunting = true;
            huntType = EHuntType.PLAYER;
            isPulled = true;
            lastPull = Time.time;
            if (isWandering)
            {
                isWandering = false;
                ZombieManager.wanderingCount--;
            }
            isLeaving = false;
            isMoving = false;
            isStuck = false;
            lastHunted = Time.time;
            lastStuck = Time.time;
            player = newPlayer;
            target.position = player.transform.position;
            seeker.canSearch = true;
            seeker.canMove = true;
            if (isMega)
            {
                path = EZombiePath.RUSH;
            }
            else if (speciality == EZombieSpeciality.FLANKER_FRIENDLY || speciality == EZombieSpeciality.FLANKER_STALK)
            {
                if ((double)UnityEngine.Random.value < 0.5)
                {
                    path = EZombiePath.LEFT_FLANK;
                }
                else
                {
                    path = EZombiePath.RIGHT_FLANK;
                }
            }
            else if (player.agro % 3 == 0)
            {
                path = EZombiePath.RUSH;
            }
            else if ((double)UnityEngine.Random.value < 0.5)
            {
                path = EZombiePath.LEFT;
            }
            else
            {
                path = EZombiePath.RIGHT;
            }
            player.agro++;
        }
        else if ((newPlayer.transform.position - base.transform.position).sqrMagnitude < (player.transform.position - base.transform.position).sqrMagnitude)
        {
            player.agro--;
            player = newPlayer;
            target.position = player.transform.position;
            if (isMega)
            {
                path = EZombiePath.RUSH;
            }
            else if (player.agro % 3 == 0)
            {
                path = EZombiePath.RUSH;
            }
            else if ((double)UnityEngine.Random.value < 0.5)
            {
                path = EZombiePath.LEFT;
            }
            else
            {
                path = EZombiePath.RIGHT;
            }
            player.agro++;
        }
    }

    public void alert(Vector3 newPosition, bool isStartling)
    {
        if (isDead || !(player == null))
        {
            return;
        }
        if (!isHunting)
        {
            if (isStartling)
            {
                if (speciality == EZombieSpeciality.CRAWLER)
                {
                    if (UnityEngine.Random.value < 0.5f)
                    {
                        ZombieManager.sendZombieStartle(this, 3);
                    }
                    else
                    {
                        ZombieManager.sendZombieStartle(this, 6);
                    }
                }
                else if (speciality == EZombieSpeciality.SPRINTER)
                {
                    if (UnityEngine.Random.value < 0.5f)
                    {
                        ZombieManager.sendZombieStartle(this, 4);
                    }
                    else
                    {
                        ZombieManager.sendZombieStartle(this, 5);
                    }
                }
                else
                {
                    ZombieManager.sendZombieStartle(this, (byte)UnityEngine.Random.Range(0, 3));
                }
                isPulled = true;
                lastPull = Time.time;
                if (isWandering)
                {
                    isWandering = false;
                    ZombieManager.wanderingCount--;
                }
            }
            isHunting = true;
            huntType = EHuntType.POINT;
            isLeaving = false;
            isMoving = false;
            isStuck = false;
            lastHunted = Time.time;
            lastStuck = Time.time;
            target.position = newPosition;
            seeker.canSearch = true;
            seeker.canMove = true;
        }
        else if ((newPosition - base.transform.position).sqrMagnitude < (target.position - base.transform.position).sqrMagnitude)
        {
            target.position = newPosition;
        }
    }

    public void updateStates()
    {
        lastUpdatedPos = base.transform.position;
        lastUpdatedAngle = base.transform.rotation.eulerAngles.y;
        interpPositionTarget = lastUpdatedPos;
        interpYawTarget = lastUpdatedAngle;
    }

    private void stop()
    {
        isMoving = false;
        isAttacking = false;
        isHunting = false;
        isStuck = false;
        isThrowRelocating = false;
        lastStuck = Time.time;
        if (player != null)
        {
            player.agro--;
        }
        player = null;
        barricade = null;
        structure = null;
        targetObstructionVehicle = null;
        targetPassengerVehicle = null;
        seeker.canSearch = false;
        seeker.canMove = false;
        target.position = base.transform.position;
        seeker.stop();
    }

    private void stun()
    {
        isStunned = true;
        isMoving = false;
        seeker.canMove = false;
        if (speciality == EZombieSpeciality.CRAWLER)
        {
            float value = UnityEngine.Random.value;
            if (value < 0.33f)
            {
                ZombieManager.sendZombieStun(this, 5);
            }
            else if (value < 0.66f)
            {
                ZombieManager.sendZombieStun(this, 7);
            }
            else
            {
                ZombieManager.sendZombieStun(this, 8);
            }
        }
        else if (speciality == EZombieSpeciality.SPRINTER)
        {
            float value2 = UnityEngine.Random.value;
            if (value2 < 0.33f)
            {
                ZombieManager.sendZombieStun(this, 6);
            }
            else if (value2 < 0.66f)
            {
                ZombieManager.sendZombieStun(this, 9);
            }
            else
            {
                ZombieManager.sendZombieStun(this, 10);
            }
        }
        else
        {
            ZombieManager.sendZombieStun(this, (byte)UnityEngine.Random.Range(0, 5));
        }
    }

    private void leave(bool quick)
    {
        isLeaving = true;
        lastLeave = Time.time;
        if (quick)
        {
            leaveTime = UnityEngine.Random.Range(0.5f, 1f);
        }
        else
        {
            leaveTime = UnityEngine.Random.Range(3f, 6f);
        }
        leaveTo = base.transform.position - 16f * (target.position - base.transform.position).normalized + new Vector3(UnityEngine.Random.Range(-8f, 8f), 0f, UnityEngine.Random.Range(-8f, 8f));
        if (!LevelNavigation.checkNavigation(leaveTo))
        {
            leaveTo = base.transform.position + 16f * (target.position - base.transform.position).normalized + new Vector3(UnityEngine.Random.Range(-8f, 8f), 0f, UnityEngine.Random.Range(-8f, 8f));
        }
        if (!LevelNavigation.checkNavigation(leaveTo))
        {
            leaveTo = base.transform.position;
        }
        stop();
    }

    private void updateEffects()
    {
    }

    public float getBulletResistance()
    {
        EZombieSpeciality eZombieSpeciality = speciality;
        if (eZombieSpeciality == EZombieSpeciality.SPIRIT || eZombieSpeciality == EZombieSpeciality.BOSS_ELVER_STOMPER)
        {
            return 0.1f;
        }
        return 1f;
    }

    private void updateVisibility(bool newVisible, bool playEffect)
    {
    }

    private void apply()
    {
        if (isMega)
        {
            if (Provider.isServer)
            {
                ((CharacterController)GetComponent<Collider>()).radius = 0.75f;
                seeker.speed = 6f;
            }
        }
        else
        {
            if (!Provider.isServer)
            {
                return;
            }
            ((CharacterController)GetComponent<Collider>()).radius = 0.4f;
            if (speciality == EZombieSpeciality.CRAWLER)
            {
                if (Provider.modeConfigData.Zombies.Slow_Movement)
                {
                    seeker.speed = 2.5f;
                }
                else
                {
                    seeker.speed = 3f;
                }
            }
            else if (speciality == EZombieSpeciality.SPRINTER || speciality.IsDLVolatile())
            {
                if (Provider.modeConfigData.Zombies.Slow_Movement)
                {
                    seeker.speed = 6f;
                }
                else
                {
                    seeker.speed = 6.5f;
                }
            }
            else if (speciality == EZombieSpeciality.FLANKER_FRIENDLY || speciality == EZombieSpeciality.FLANKER_STALK)
            {
                if (Provider.modeConfigData.Zombies.Slow_Movement)
                {
                    seeker.speed = 5.5f;
                }
                else
                {
                    seeker.speed = 6f;
                }
            }
            else if (Provider.modeConfigData.Zombies.Slow_Movement)
            {
                seeker.speed = 4.5f;
            }
            else
            {
                seeker.speed = 5.5f;
            }
        }
    }

    private void updateDifficulty()
    {
        if (Provider.isServer)
        {
            difficulty = ZombieManager.getDifficultyInBound(bound);
            if (difficulty == null && type < LevelZombies.tables.Count)
            {
                difficulty = LevelZombies.tables[type].resolveDifficulty();
            }
        }
    }

    private void updateLife()
    {
        CharacterController component = GetComponent<CharacterController>();
        if (component != null)
        {
            component.SetDetectCollisionsDeferred(!isDead);
        }
        GetComponent<Collider>().enabled = !isDead;
    }

    private void RandomizeBoulderThrowDelay()
    {
        boulderThrowDelay = UnityEngine.Random.Range(6f, 12f);
        if (speciality == EZombieSpeciality.BOSS_KUWAIT)
        {
            boulderThrowDelay *= 0.5f;
        }
    }

    private void reset()
    {
        target.position = base.transform.position;
        lastTarget = Time.time;
        lastLeave = Time.time;
        lastRelocate = Time.time;
        lastSpecial = Time.time;
        lastAttack = Time.time;
        lastStartle = Time.time;
        lastStun = Time.time;
        lastStuck = Time.time;
        cameFrom = base.transform.position;
        isPulled = false;
        pullDelay = UnityEngine.Random.Range(24f, 96f);
        specialStartleDelay = UnityEngine.Random.Range(1f, 2f);
        specialAttackDelay = UnityEngine.Random.Range(2f, 4f);
        specialUseDelay = UnityEngine.Random.Range(4f, 8f);
        flashbangDelay = 10f;
        RandomizeBoulderThrowDelay();
        isThrowRelocating = false;
        isThrowingBoulder = false;
        isSpittingAcid = false;
        isChargingSpark = false;
        isStompingWind = false;
        isBreathingFire = false;
        isPlayingBoulder = false;
        isPlayingSpit = false;
        isPlayingCharge = false;
        isPlayingWind = false;
        isPlayingFire = false;
        isPlayingAttack = false;
        isPlayingStartle = false;
        isPlayingStun = false;
        isMoving = false;
        isAttacking = false;
        isHunting = false;
        isLeaving = false;
        isStunned = false;
        isStuck = false;
        leaveTo = base.transform.position;
        if (player != null)
        {
            player.agro--;
        }
        player = null;
        barricade = null;
        structure = null;
        targetObstructionVehicle = null;
        targetPassengerVehicle = null;
        seeker.canSearch = false;
        seeker.canMove = false;
        health = LevelZombies.tables[type].health;
        if (speciality == EZombieSpeciality.CRAWLER || speciality.IsDLVolatile())
        {
            health = (ushort)((float)(int)health * 1.5f);
        }
        else if (speciality == EZombieSpeciality.SPRINTER)
        {
            health = (ushort)((float)(int)health * 0.5f);
        }
        else if (speciality == EZombieSpeciality.BOSS_ALL || speciality == EZombieSpeciality.BOSS_MAGMA)
        {
            health = 12000;
        }
        else if (speciality == EZombieSpeciality.BOSS_ELVER_STOMPER)
        {
            health = 4600;
        }
        else if (speciality == EZombieSpeciality.BOSS_KUWAIT)
        {
            health = 60000;
        }
        else if (isBoss)
        {
            health = 6000;
        }
        if (Level.info.type == ELevelType.HORDE)
        {
            health += (ushort)(Mathf.Min(ZombieManager.waveIndex - 1, 20) * 10);
        }
        maxHealth = health;
        needsTickForPlacement = true;
        setTicking(wantsToTick: true);
    }

    private void findTargetWhileStuck()
    {
        bool can_Target_Structures = Provider.modeConfigData.Zombies.Can_Target_Structures;
        bool can_Target_Barricades = Provider.modeConfigData.Zombies.Can_Target_Barricades;
        bool can_Target_Vehicles = Provider.modeConfigData.Zombies.Can_Target_Vehicles;
        can_Target_Vehicles &= speciality != EZombieSpeciality.BOSS_KUWAIT;
        if (can_Target_Structures || can_Target_Barricades)
        {
            regionsInRadius.Clear();
            Regions.getRegionsInRadius(base.transform.position, 4f, regionsInRadius);
        }
        if (can_Target_Structures)
        {
            structuresInRadius.Clear();
            StructureManager.getStructuresInRadius(base.transform.position, 16f, regionsInRadius, structuresInRadius);
            if (structuresInRadius.Count > 0)
            {
                structure = structuresInRadius[0];
                return;
            }
        }
        if (can_Target_Vehicles)
        {
            vehiclesInRadius.Clear();
            VehicleManager.getVehiclesInRadius(base.transform.position, 16f, vehiclesInRadius);
            if (vehiclesInRadius.Count > 0 && vehiclesInRadius[0].asset != null && vehiclesInRadius[0].asset.isVulnerableToEnvironment)
            {
                targetObstructionVehicle = vehiclesInRadius[0];
                return;
            }
        }
        if (can_Target_Barricades)
        {
            barricadesInRadius.Clear();
            BarricadeManager.getBarricadesInRadius(base.transform.position, 16f, regionsInRadius, barricadesInRadius);
            if (barricadesInRadius.Count > 0)
            {
                barricade = barricadesInRadius[0];
            }
        }
    }

    public void tick()
    {
        if (needsTickForPlacement)
        {
            needsTickForPlacement = false;
            setTicking(wantsToTick: false);
            GetComponent<CharacterController>().Move(Vector3.down);
            return;
        }
        float delta = Time.time - lastTick;
        lastTick = Time.time;
        lastPull = Time.time;
        if (isStunned)
        {
            return;
        }
        if (!Level.isPointWithinValidHeight(base.transform.position.y))
        {
            ZombieManager.teleportZombieBackIntoMap(this);
            return;
        }
        if (huntType == EHuntType.PLAYER)
        {
            if (player == null)
            {
                stop();
                return;
            }
        }
        else if (huntType == EHuntType.POINT && !isMoving && Time.time - lastHunted > 3f)
        {
            stop();
            return;
        }
        if (player != null)
        {
            if (player.life.isDead)
            {
                leave(quick: false);
                return;
            }
            if (player.movement.nav == byte.MaxValue || (player.stance.stance == EPlayerStance.SWIM && !WaterUtility.isPointUnderwater(base.transform.position)))
            {
                leave(quick: true);
                return;
            }
        }
        if (targetObstructionVehicle != null && targetObstructionVehicle.isDead)
        {
            targetObstructionVehicle = null;
        }
        if (targetPassengerVehicle != null && targetPassengerVehicle.isDead)
        {
            targetPassengerVehicle = null;
        }
        if (isStuck)
        {
            float num = Time.time - lastStuck;
            if (num > 1f && barricade == null && structure == null && targetObstructionVehicle == null && targetPassengerVehicle == null)
            {
                findTargetWhileStuck();
            }
            if (num > 5f && zombieRegion.hasBeacon && Time.time - lastAttack > 10f)
            {
                lastStuck = Time.time;
                ZombieManager.teleportZombieBackIntoMap(this);
                return;
            }
        }
        float num2;
        if (barricade != null)
        {
            num2 = MathfEx.HorizontalDistanceSquared(barricade.position, base.transform.position);
            target.position = barricade.position;
            seeker.canTurn = false;
            seeker.targetDirection = barricade.position - base.transform.position;
        }
        else if (structure != null)
        {
            num2 = 0f;
            target.position = base.transform.position;
            seeker.canTurn = false;
            seeker.targetDirection = structure.position - base.transform.position;
        }
        else if (targetObstructionVehicle != null)
        {
            num2 = MathfEx.HorizontalDistanceSquared(targetObstructionVehicle.transform.position, base.transform.position);
            target.position = targetObstructionVehicle.transform.position;
            seeker.canTurn = false;
            seeker.targetDirection = targetObstructionVehicle.transform.position - base.transform.position;
        }
        else if (player != null)
        {
            targetPassengerVehicle = ((speciality != EZombieSpeciality.BOSS_KUWAIT) ? player.movement.getVehicle() : null);
            if (targetPassengerVehicle != null)
            {
                if (targetPassengerVehicle.isDead)
                {
                    targetPassengerVehicle = null;
                }
                else if (targetPassengerVehicle.asset == null || !targetPassengerVehicle.asset.isVulnerableToEnvironment)
                {
                    targetPassengerVehicle = null;
                }
            }
            if (targetPassengerVehicle != null)
            {
                num2 = MathfEx.HorizontalDistanceSquared(targetPassengerVehicle.transform.position, base.transform.position);
                target.position = targetPassengerVehicle.transform.position;
                seeker.canTurn = false;
                seeker.targetDirection = targetPassengerVehicle.transform.position - base.transform.position;
            }
            else
            {
                num2 = MathfEx.HorizontalDistanceSquared(player.transform.position, base.transform.position);
                if (isThrowRelocating)
                {
                    Vector3 vector = base.transform.position - player.transform.position;
                    vector.y = 0f;
                    target.position = player.transform.position + vector.normalized * 7f;
                    seeker.canTurn = true;
                }
                else
                {
                    target.position = player.transform.position;
                    if (path == EZombiePath.LEFT_FLANK)
                    {
                        if (num2 > 100f)
                        {
                            seeker.canTurn = true;
                            target.position += player.transform.right * 9f + player.transform.forward * -4f;
                        }
                        else if (num2 > 20f || Vector3.Dot((base.transform.position - player.transform.position).normalized, player.transform.forward) > 0f)
                        {
                            seeker.canTurn = true;
                            target.position += player.transform.right * 3f + player.transform.forward * -3f;
                        }
                        else if (num2 > 4f)
                        {
                            seeker.canTurn = true;
                            target.position -= player.transform.forward;
                        }
                        else
                        {
                            seeker.canTurn = false;
                            seeker.targetDirection = player.transform.position - base.transform.position;
                        }
                    }
                    else if (path == EZombiePath.RIGHT_FLANK)
                    {
                        if (num2 > 100f)
                        {
                            seeker.canTurn = true;
                            target.position += player.transform.right * -9f + player.transform.forward * -4f;
                        }
                        else if (num2 > 20f || Vector3.Dot((base.transform.position - player.transform.position).normalized, player.transform.forward) > 0f)
                        {
                            seeker.canTurn = true;
                            target.position += player.transform.right * -3f + player.transform.forward * -3f;
                        }
                        else if (num2 > 4f)
                        {
                            seeker.canTurn = true;
                            target.position -= player.transform.forward;
                        }
                        else
                        {
                            seeker.canTurn = false;
                            seeker.targetDirection = player.transform.position - base.transform.position;
                        }
                    }
                    else if (path == EZombiePath.LEFT)
                    {
                        if (num2 > 4f)
                        {
                            seeker.canTurn = true;
                            target.position -= base.transform.right;
                        }
                        else
                        {
                            seeker.canTurn = false;
                            seeker.targetDirection = player.transform.position - base.transform.position;
                        }
                    }
                    else if (path == EZombiePath.RIGHT)
                    {
                        if (num2 > 4f)
                        {
                            seeker.canTurn = true;
                            target.position += base.transform.right;
                        }
                        else
                        {
                            seeker.canTurn = false;
                            seeker.targetDirection = player.transform.position - base.transform.position;
                        }
                    }
                    else if (path == EZombiePath.RUSH)
                    {
                        if (num2 > 4f)
                        {
                            seeker.canTurn = true;
                            target.position -= base.transform.forward;
                        }
                        else
                        {
                            seeker.canTurn = false;
                            seeker.targetDirection = player.transform.position - base.transform.position;
                        }
                    }
                }
            }
        }
        else
        {
            num2 = MathfEx.HorizontalDistanceSquared(target.position, base.transform.position);
            seeker.canTurn = true;
        }
        float num3 = Mathf.Abs(target.position.y - base.transform.position.y);
        isMoving = num2 > 3f;
        if (!isWandering && num2 > 4096f && (player == null || !zombieRegion.HasInfiniteAgroRange))
        {
            leave(quick: false);
            return;
        }
        if (player != null || barricade != null || structure != null || targetObstructionVehicle != null || targetPassengerVehicle != null)
        {
            if (player != null && (speciality == EZombieSpeciality.MEGA || speciality == EZombieSpeciality.BOSS_KUWAIT || (speciality == EZombieSpeciality.BOSS_ALL && UnityEngine.Random.value < 0.2f)) && Time.time - lastStartle > specialStartleDelay && Time.time - lastAttack > specialAttackDelay && Time.time - lastSpecial > boulderThrowDelay)
            {
                if (num2 < 20f)
                {
                    if (isThrowRelocating)
                    {
                        if (Time.time - lastRelocate > 1.5f)
                        {
                            isThrowRelocating = false;
                            lastSpecial = Time.time;
                            RandomizeBoulderThrowDelay();
                        }
                    }
                    else
                    {
                        isThrowRelocating = true;
                        lastRelocate = Time.time;
                    }
                }
                else
                {
                    isThrowRelocating = false;
                    lastSpecial = Time.time;
                    RandomizeBoulderThrowDelay();
                    seeker.canMove = false;
                    ZombieManager.sendZombieThrow(this);
                }
            }
            else
            {
                isThrowRelocating = false;
            }
            if (player != null && (speciality == EZombieSpeciality.ACID || speciality == EZombieSpeciality.BOSS_NUCLEAR || (speciality == EZombieSpeciality.BOSS_ALL && UnityEngine.Random.value < 0.2f)) && Time.time - lastStartle > specialStartleDelay && Time.time - lastAttack > specialAttackDelay && Time.time - lastSpecial > specialUseDelay)
            {
                lastSpecial = Time.time;
                specialUseDelay = UnityEngine.Random.Range(4f, 8f);
                seeker.canMove = false;
                ZombieManager.sendZombieSpit(this);
            }
            if (player != null && (speciality == EZombieSpeciality.BOSS_WIND || speciality == EZombieSpeciality.BOSS_ELVER_STOMPER || (speciality == EZombieSpeciality.BOSS_ALL && UnityEngine.Random.value < 0.2f)) && Time.time - lastStartle > specialStartleDelay && Time.time - lastAttack > specialAttackDelay && Time.time - lastSpecial > specialUseDelay && (player.transform.position - base.transform.position).sqrMagnitude < 144f)
            {
                lastSpecial = Time.time;
                specialUseDelay = UnityEngine.Random.Range(4f, 8f);
                seeker.canMove = false;
                ZombieManager.sendZombieStomp(this);
            }
            if (player != null && (speciality == EZombieSpeciality.BOSS_FIRE || speciality == EZombieSpeciality.BOSS_MAGMA || (speciality == EZombieSpeciality.BOSS_ALL && UnityEngine.Random.value < 0.2f)) && Time.time - lastStartle > specialStartleDelay && Time.time - lastAttack > specialAttackDelay && Time.time - lastSpecial > specialUseDelay && (player.transform.position - base.transform.position).sqrMagnitude < 529f)
            {
                lastSpecial = Time.time;
                specialUseDelay = UnityEngine.Random.Range(4f, 8f);
                seeker.canMove = false;
                ZombieManager.sendZombieBreath(this);
            }
            if (player != null && (speciality == EZombieSpeciality.BOSS_ELECTRIC || (speciality == EZombieSpeciality.BOSS_ALL && UnityEngine.Random.value < 0.2f)) && Time.time - lastStartle > specialStartleDelay && Time.time - lastAttack > specialAttackDelay && Time.time - lastSpecial > specialUseDelay && (player.transform.position - base.transform.position).sqrMagnitude > 4f && (player.transform.position - base.transform.position).sqrMagnitude < 4096f)
            {
                lastSpecial = Time.time;
                specialUseDelay = UnityEngine.Random.Range(4f, 8f);
                seeker.canMove = false;
                ZombieManager.sendZombieCharge(this);
            }
            if (player != null && speciality == EZombieSpeciality.BOSS_KUWAIT && Time.time - lastStartle > specialStartleDelay && Time.time - lastAttack > specialAttackDelay && Time.time - lastFlashbang > flashbangDelay && (player.transform.position - base.transform.position).sqrMagnitude > 4f && (player.transform.position - base.transform.position).sqrMagnitude < 1024f)
            {
                lastFlashbang = Time.time;
                flashbangDelay = UnityEngine.Random.Range(30f, 45f);
                EffectAsset effectAsset = KuwaitBossFlashbangRef.Find();
                if (effectAsset != null)
                {
                    TriggerEffectParameters parameters = new TriggerEffectParameters(effectAsset);
                    parameters.reliable = true;
                    parameters.position = base.transform.position + new Vector3(0f, 5f, 0f);
                    EffectManager.triggerEffect(parameters);
                }
            }
            if ((structure != null || num2 < GetHorizontalAttackRangeSquared()) && num3 < (isHyper ? 3.5f : 2f) * (isMega ? 1.5f : 1f))
            {
                if (speciality == EZombieSpeciality.SPRINTER || Time.time - lastTarget > 0.5f)
                {
                    if (isAttacking)
                    {
                        if (Time.time - lastAttack > attackTime / 2f)
                        {
                            isAttacking = false;
                            byte b = (byte)((float)(int)LevelZombies.tables[type].damage * (isHyper ? 1.5f : 1f));
                            b = (byte)((float)(int)b * Provider.modeConfigData.Zombies.Damage_Multiplier);
                            if (speciality == EZombieSpeciality.CRAWLER)
                            {
                                b = (byte)((float)(int)b * 2f);
                            }
                            else if (speciality == EZombieSpeciality.SPRINTER)
                            {
                                b = (byte)((float)(int)b * 0.75f);
                            }
                            if (structure != null)
                            {
                                StructureManager.damage(structure, (target.position - base.transform.position).normalized * (int)b, (int)b, 1f, armor: true, default(CSteamID), EDamageOrigin.Zombie_Swipe);
                                if (structure == null || !structure.CompareTag("Structure"))
                                {
                                    structure = null;
                                    isStuck = false;
                                    lastStuck = Time.time;
                                }
                            }
                            else if (barricade != null)
                            {
                                BarricadeManager.damage(barricade, (int)b, 1f, armor: true, default(CSteamID), EDamageOrigin.Zombie_Swipe);
                            }
                            else if (targetObstructionVehicle != null)
                            {
                                VehicleManager.damage(targetObstructionVehicle, (int)b, 1f, canRepair: true, default(CSteamID), EDamageOrigin.Zombie_Swipe);
                            }
                            else if (targetPassengerVehicle != null)
                            {
                                VehicleManager.damage(targetPassengerVehicle, (int)b, 1f, canRepair: true, default(CSteamID), EDamageOrigin.Zombie_Swipe);
                            }
                            else if (player != null)
                            {
                                if (player.skills.boost == EPlayerBoost.HARDENED)
                                {
                                    b = (byte)((float)(int)b * 0.75f);
                                }
                                if (isMega)
                                {
                                    if (player.clothing.hatAsset != null)
                                    {
                                        ItemClothingAsset hatAsset = player.clothing.hatAsset;
                                        if (Provider.modeConfigData.Items.Has_Durability && player.clothing.hatQuality > 0)
                                        {
                                            player.clothing.hatQuality--;
                                            player.clothing.sendUpdateHatQuality();
                                        }
                                        float num4 = hatAsset.armor + (1f - hatAsset.armor) * (1f - (float)(int)player.clothing.hatQuality / 100f);
                                        b = (byte)((float)(int)b * num4);
                                    }
                                    else if (player.clothing.vestAsset != null)
                                    {
                                        ItemClothingAsset vestAsset = player.clothing.vestAsset;
                                        if (Provider.modeConfigData.Items.Has_Durability && player.clothing.vestQuality > 0)
                                        {
                                            player.clothing.vestQuality--;
                                            player.clothing.sendUpdateVestQuality();
                                        }
                                        float num5 = vestAsset.armor + (1f - vestAsset.armor) * (1f - (float)(int)player.clothing.vestQuality / 100f);
                                        b = (byte)((float)(int)b * num5);
                                    }
                                    else if (player.clothing.shirtAsset != null)
                                    {
                                        ItemClothingAsset shirtAsset = player.clothing.shirtAsset;
                                        if (Provider.modeConfigData.Items.Has_Durability && player.clothing.shirtQuality > 0)
                                        {
                                            player.clothing.shirtQuality--;
                                            player.clothing.sendUpdateShirtQuality();
                                        }
                                        float num6 = shirtAsset.armor + (1f - shirtAsset.armor) * (1f - (float)(int)player.clothing.shirtQuality / 100f);
                                        b = (byte)((float)(int)b * num6);
                                    }
                                }
                                else if (speciality == EZombieSpeciality.NORMAL)
                                {
                                    if (player.clothing.vestAsset != null)
                                    {
                                        ItemClothingAsset vestAsset2 = player.clothing.vestAsset;
                                        if (Provider.modeConfigData.Items.Has_Durability && player.clothing.vestQuality > 0)
                                        {
                                            player.clothing.vestQuality--;
                                            player.clothing.sendUpdateVestQuality();
                                        }
                                        float num7 = vestAsset2.armor + (1f - vestAsset2.armor) * (1f - (float)(int)player.clothing.vestQuality / 100f);
                                        b = (byte)((float)(int)b * num7);
                                    }
                                    else if (player.clothing.shirtAsset != null)
                                    {
                                        ItemClothingAsset shirtAsset2 = player.clothing.shirtAsset;
                                        if (Provider.modeConfigData.Items.Has_Durability && player.clothing.shirtQuality > 0)
                                        {
                                            player.clothing.shirtQuality--;
                                            player.clothing.sendUpdateShirtQuality();
                                        }
                                        float num8 = shirtAsset2.armor + (1f - shirtAsset2.armor) * (1f - (float)(int)player.clothing.shirtQuality / 100f);
                                        b = (byte)((float)(int)b * num8);
                                    }
                                }
                                else if (speciality == EZombieSpeciality.CRAWLER)
                                {
                                    if (player.clothing.pantsAsset != null)
                                    {
                                        ItemClothingAsset pantsAsset = player.clothing.pantsAsset;
                                        if (Provider.modeConfigData.Items.Has_Durability && player.clothing.pantsQuality > 0)
                                        {
                                            player.clothing.pantsQuality--;
                                            player.clothing.sendUpdatePantsQuality();
                                        }
                                        float num9 = pantsAsset.armor + (1f - pantsAsset.armor) * (1f - (float)(int)player.clothing.pantsQuality / 100f);
                                        b = (byte)((float)(int)b * num9);
                                    }
                                }
                                else if (speciality == EZombieSpeciality.SPRINTER)
                                {
                                    if (player.clothing.vestAsset != null)
                                    {
                                        ItemClothingAsset vestAsset3 = player.clothing.vestAsset;
                                        if (Provider.modeConfigData.Items.Has_Durability && player.clothing.vestQuality > 0)
                                        {
                                            player.clothing.vestQuality--;
                                            player.clothing.sendUpdateVestQuality();
                                        }
                                        float num10 = vestAsset3.armor + (1f - vestAsset3.armor) * (1f - (float)(int)player.clothing.vestQuality / 100f);
                                        b = (byte)((float)(int)b * num10);
                                    }
                                    else if (player.clothing.shirtAsset != null)
                                    {
                                        ItemClothingAsset shirtAsset3 = player.clothing.shirtAsset;
                                        if (Provider.modeConfigData.Items.Has_Durability && player.clothing.shirtQuality > 0)
                                        {
                                            player.clothing.shirtQuality--;
                                            player.clothing.sendUpdateShirtQuality();
                                        }
                                        float num11 = shirtAsset3.armor + (1f - shirtAsset3.armor) * (1f - (float)(int)player.clothing.shirtQuality / 100f);
                                        b = (byte)((float)(int)b * num11);
                                    }
                                    else if (player.clothing.pantsAsset != null)
                                    {
                                        ItemClothingAsset pantsAsset2 = player.clothing.pantsAsset;
                                        if (Provider.modeConfigData.Items.Has_Durability && player.clothing.pantsQuality > 0)
                                        {
                                            player.clothing.pantsQuality--;
                                            player.clothing.sendUpdatePantsQuality();
                                        }
                                        float num12 = pantsAsset2.armor + (1f - pantsAsset2.armor) * (1f - (float)(int)player.clothing.pantsQuality / 100f);
                                        b = (byte)((float)(int)b * num12);
                                    }
                                }
                                DamageTool.damage(player, EDeathCause.ZOMBIE, ELimb.SKULL, Provider.server, (target.position - base.transform.position).normalized, (int)b, 1f, out var _);
                                player.life.askInfect((byte)((float)((int)b / 3) * (1f - player.skills.mastery(1, 2) * 0.5f)));
                            }
                        }
                    }
                    else if (Time.time - lastAttack > 1f)
                    {
                        isAttacking = true;
                        if (speciality == EZombieSpeciality.CRAWLER)
                        {
                            ZombieManager.sendZombieAttack(this, 5);
                        }
                        else if (speciality == EZombieSpeciality.SPRINTER)
                        {
                            ZombieManager.sendZombieAttack(this, (byte)UnityEngine.Random.Range(6, 9));
                        }
                        else
                        {
                            ZombieManager.sendZombieAttack(this, (byte)UnityEngine.Random.Range(0, 5));
                        }
                    }
                }
            }
            else
            {
                lastTarget = Time.time;
                isAttacking = false;
            }
        }
        if (seeker != null)
        {
            seeker.move(delta);
        }
    }

    private void Update()
    {
        if (isDead)
        {
            return;
        }
        if (Provider.isServer)
        {
            if (!isUpdated)
            {
                if (Mathf.Abs(lastUpdatedPos.x - base.transform.position.x) > Provider.UPDATE_DISTANCE || Mathf.Abs(lastUpdatedPos.y - base.transform.position.y) > Provider.UPDATE_DISTANCE || Mathf.Abs(lastUpdatedPos.z - base.transform.position.z) > Provider.UPDATE_DISTANCE || Mathf.Abs(lastUpdatedAngle - base.transform.rotation.eulerAngles.y) > 1f)
                {
                    lastUpdatedPos = base.transform.position;
                    lastUpdatedAngle = base.transform.rotation.eulerAngles.y;
                    isUpdated = true;
                    zombieRegion.updates++;
                    isStuck = false;
                    lastStuck = Time.time;
                }
                else if (!isStuck)
                {
                    if (isMoving)
                    {
                        isStuck = true;
                    }
                    else if (zombieRegion.HasInfiniteAgroRange && player != null && (player.transform.position - base.transform.position).sqrMagnitude > 4f)
                    {
                        isStuck = true;
                    }
                }
            }
            if (isPulled && Time.time - lastPull > pullDelay)
            {
                lastPull = Time.time;
                pullDelay = UnityEngine.Random.Range(24f, 96f);
                if (!isLeaving && ZombieManager.canSpareWanderer)
                {
                    float f = UnityEngine.Random.value * (float)Math.PI * 2f;
                    float num = UnityEngine.Random.Range(0.5f, 1f);
                    isWandering = true;
                    ZombieManager.wanderingCount++;
                    isPulled = false;
                    alert(cameFrom + new Vector3(Mathf.Cos(f) * num, 0f, Mathf.Sin(f) * num), isStartling: false);
                }
            }
        }
        else
        {
            if (Mathf.Abs(lastUpdatedPos.x - base.transform.position.x) > 0.01f || Mathf.Abs(lastUpdatedPos.y - base.transform.position.y) > 0.01f || Mathf.Abs(lastUpdatedPos.z - base.transform.position.z) > 0.01f)
            {
                isMoving = true;
            }
            else
            {
                isMoving = false;
            }
            base.transform.position = Vector3.Lerp(base.transform.position, interpPositionTarget, Time.deltaTime * 10f);
            base.transform.rotation = Quaternion.Euler(0f, Mathf.LerpAngle(base.transform.rotation.eulerAngles.y, interpYawTarget, Time.deltaTime * 10f), 0f);
        }
        if ((isThrowingBoulder || isSpittingAcid || isBreathingFire || isChargingSpark) && Provider.isServer && player != null)
        {
            Vector3 normalized = (player.transform.position - base.transform.position).normalized;
            normalized.y = 0f;
            Quaternion rotation = Quaternion.LookRotation(normalized);
            base.transform.rotation = rotation;
        }
        if (isThrowingBoulder && Time.time - lastSpecial > throwTime)
        {
            isThrowingBoulder = false;
            if (boulderItem != null)
            {
                UnityEngine.Object.Destroy(boulderItem.gameObject);
            }
            if (Provider.isServer)
            {
                seeker.canMove = true;
                if (player != null)
                {
                    Vector3 vector = player.transform.position - base.transform.position;
                    float magnitude = vector.magnitude;
                    vector += Vector3.up * magnitude * 0.1f;
                    float magnitude2 = player.movement.velocity.magnitude;
                    if (magnitude2 > 0.1f)
                    {
                        Vector3 vector2 = player.movement.velocity / magnitude2;
                        vector += vector2 * magnitude * UnityEngine.Random.Range(0.1f, 0.2f);
                    }
                    Vector3 direction = vector / magnitude;
                    ZombieManager.sendZombieBoulder(this, base.transform.position + Vector3.up * base.transform.localScale.y * 1.9f, direction);
                }
                else
                {
                    ZombieManager.sendZombieBoulder(this, base.transform.position + Vector3.up * base.transform.localScale.y * 1.9f, Vector3.forward);
                }
            }
        }
        if (isSpittingAcid && Time.time - lastSpecial > acidTime)
        {
            isSpittingAcid = false;
            if (Provider.isServer)
            {
                seeker.canMove = true;
                if (player != null)
                {
                    Vector3 vector3 = player.transform.position - base.transform.position;
                    float magnitude3 = vector3.magnitude;
                    vector3 += Vector3.up * magnitude3 * 0.25f;
                    Vector3 direction2 = vector3 / magnitude3;
                    ZombieManager.sendZombieAcid(this, base.transform.position + Vector3.up * base.transform.localScale.y * 1.75f, direction2);
                }
                else
                {
                    ZombieManager.sendZombieAcid(this, base.transform.position + Vector3.up * base.transform.localScale.y * 1.75f, Vector3.forward);
                }
            }
        }
        if (isChargingSpark && Time.time - lastSpecial > sparkTime)
        {
            isChargingSpark = false;
            if (Provider.isServer && player != null)
            {
                Vector3 vector4 = player.look.aim.position;
                Vector3 direction3 = vector4 - (base.transform.position + new Vector3(0f, 2f, 0f));
                if (Physics.Raycast(new Ray(base.transform.position + new Vector3(0f, 2f, 0f), direction3), out var hitInfo, direction3.magnitude - 0.025f, RayMasks.BLOCK_SENTRY))
                {
                    vector4 = hitInfo.point + hitInfo.normal;
                }
                DamageTool.explode(vector4, 5f, EDeathCause.SPARK, CSteamID.Nil, 25f, 0f, 0f, 250f, 250f, 250f, 250f, 250f, out var _, EExplosionDamageType.ZOMBIE_ELECTRIC, 4f, playImpactEffect: true, penetrateBuildables: false, EDamageOrigin.Zombie_Electric_Shock);
                ZombieManager.sendZombieSpark(this, vector4);
            }
        }
        if (isStompingWind && Time.time - lastSpecial > windTime)
        {
            isStompingWind = false;
            if (Provider.isServer)
            {
                seeker.canMove = true;
                DamageTool.explode(base.transform.position + new Vector3(0f, 1.5f, 0f), 10f, EDeathCause.BOULDER, CSteamID.Nil, 60f, 0f, 0f, 500f, 500f, 500f, 500f, 500f, out var _, EExplosionDamageType.ZOMBIE_ACID, 32f, playImpactEffect: true, penetrateBuildables: false, EDamageOrigin.Zombie_Stomp);
                EffectAsset effectAsset = Boulder.Metal_2_Ref.Find();
                if (effectAsset != null)
                {
                    TriggerEffectParameters parameters = new TriggerEffectParameters(effectAsset);
                    parameters.relevantDistance = EffectManager.MEDIUM;
                    parameters.position = base.transform.position;
                    EffectManager.triggerEffect(parameters);
                }
            }
        }
        if (isBreathingFire)
        {
            if (Provider.isServer && isBreathingFire)
            {
                fireDamage += Time.deltaTime * 50f;
                if (fireDamage > 1f)
                {
                    float playerDamage = fireDamage;
                    float num2 = fireDamage * 10f;
                    fireDamage = 0f;
                    DamageTool.explode(base.transform.position + new Vector3(0f, 1.25f, 0f) + base.transform.forward * 3f, 2f, EDeathCause.BURNER, CSteamID.Nil, playerDamage, 0f, 0f, num2, num2, num2, num2, num2, out var kills3, EExplosionDamageType.ZOMBIE_FIRE, 4f, playImpactEffect: false, penetrateBuildables: false, EDamageOrigin.Zombie_Fire_Breath);
                    DamageTool.explode(base.transform.position + new Vector3(0f, 1.25f, 0f) + base.transform.forward * 7f, 3f, EDeathCause.BURNER, CSteamID.Nil, playerDamage, 0f, 0f, num2, num2, num2, num2, num2, out kills3, EExplosionDamageType.ZOMBIE_FIRE, 4f, playImpactEffect: false, penetrateBuildables: false, EDamageOrigin.Zombie_Fire_Breath);
                    DamageTool.explode(base.transform.position + new Vector3(0f, 1.25f, 0f) + base.transform.forward * 12f, 4f, EDeathCause.BURNER, CSteamID.Nil, playerDamage, 0f, 0f, num2, num2, num2, num2, num2, out kills3, EExplosionDamageType.ZOMBIE_FIRE, 4f, playImpactEffect: false, penetrateBuildables: false, EDamageOrigin.Zombie_Fire_Breath);
                }
            }
            if (Time.time - lastSpecial > fireTime)
            {
                isBreathingFire = false;
                if (fireSystem != null)
                {
                    ParticleSystem.EmissionModule emission = fireSystem.emission;
                    emission.enabled = false;
                }
                if (Provider.isServer)
                {
                    seeker.canMove = true;
                }
            }
        }
        if (isPlayingBoulder)
        {
            if (Time.time - lastSpecial > boulderTime)
            {
                isPlayingBoulder = false;
            }
        }
        else if (isPlayingSpit)
        {
            if (Time.time - lastSpecial > spitTime)
            {
                isPlayingSpit = false;
            }
        }
        else if (isPlayingCharge)
        {
            if (Time.time - lastSpecial > chargeTime)
            {
                isPlayingCharge = false;
                if (Provider.isServer)
                {
                    seeker.canMove = true;
                }
            }
        }
        else if (isPlayingWind)
        {
            if (Time.time - lastSpecial > windTime)
            {
                isPlayingWind = false;
            }
        }
        else if (isPlayingFire)
        {
            if (Time.time - lastSpecial > fireTime)
            {
                isPlayingFire = false;
            }
        }
        else if (isPlayingAttack)
        {
            if (Time.time - lastAttack > attackTime)
            {
                if (speciality == EZombieSpeciality.FLANKER_FRIENDLY || speciality == EZombieSpeciality.FLANKER_STALK)
                {
                    updateVisibility(newVisible: false, playEffect: true);
                }
                isPlayingAttack = false;
            }
        }
        else if (isPlayingStartle)
        {
            if (Time.time - lastStartle > startleTime)
            {
                isPlayingStartle = false;
            }
        }
        else if (isPlayingStun && Time.time - lastStun > stunTime)
        {
            isPlayingStun = false;
        }
        if (Provider.isServer && health < maxHealth && Time.time - lastRegen > LevelZombies.tables[type].regen)
        {
            lastRegen = Time.time;
            health++;
        }
        if (!Provider.isServer)
        {
            return;
        }
        if (isStunned)
        {
            if (!(Time.time - lastStun > 1f))
            {
                return;
            }
            lastTarget = Time.time;
            lastStuck = Time.time;
            isStunned = false;
            seeker.canMove = true;
        }
        if (isLeaving && Time.time - lastLeave > leaveTime)
        {
            alert(leaveTo, isStartling: false);
            isLeaving = false;
        }
    }

    private void onHyperUpdated(bool isHyper)
    {
        if (eyes != null)
        {
            eyes.gameObject.SetActive(isHyper);
        }
    }

    public void init()
    {
        awake();
        start();
        SetCountedAsAliveInZombieRegion(!isDead);
        SetCountedAsAliveBossInZombieRegion(!isDead && isBoss);
    }

    private void start()
    {
        if (Provider.isServer)
        {
            seeker = GetComponent<AIPath>();
            GetComponent<CharacterController>().enableOverlapRecovery = false;
            target = base.transform.Find("Target");
            target.parent = null;
            seeker.target = target;
            seeker.canSmooth = false;
            reset();
        }
        else
        {
            lastUpdatedPos = base.transform.position;
            lastUpdatedAngle = base.transform.rotation.eulerAngles.y;
            interpPositionTarget = lastUpdatedPos;
            interpYawTarget = lastUpdatedAngle;
        }
        lastGroan = Time.time + UnityEngine.Random.Range(4f, 16f);
        if (isMega)
        {
            groanDelay = UnityEngine.Random.Range(2f, 4f);
        }
        else
        {
            groanDelay = UnityEngine.Random.Range(4f, 8f);
        }
        updateDifficulty();
        updateLife();
        apply();
        updateEffects();
        updateVisibility(speciality != EZombieSpeciality.FLANKER_STALK && speciality != EZombieSpeciality.SPIRIT && speciality != EZombieSpeciality.BOSS_SPIRIT, playEffect: false);
        updateStates();
    }

    private void awake()
    {
        throwTime = 1f;
        acidTime = 1f;
        windTime = 0.9f;
        fireTime = 2.75f;
        chargeTime = 1.8f;
        sparkTime = 1.2f;
        boulderTime = 1f;
        spitTime = 1f;
        attackTime = 0.5f;
        startleTime = 0.5f;
        stunTime = 0.5f;
    }

    private void OnDestroy()
    {
        if (Provider.isServer)
        {
            isHunting = false;
        }
        if (target != null && target.parent != this)
        {
            UnityEngine.Object.Destroy(target.gameObject);
        }
    }

    private void PlayOneShot(AudioClip[] clips)
    {
    }

    private float GetRandomPitch()
    {
        float num = (isMega ? UnityEngine.Random.Range(0.5f, 0.7f) : ((!isCutesy) ? UnityEngine.Random.Range(0.9f, 1.1f) : UnityEngine.Random.Range(1.3f, 1.4f)));
        if (isHyper)
        {
            num *= 0.9f;
        }
        return num;
    }

    private void SetCountedAsAliveInZombieRegion(bool newValue)
    {
        if (isCountedAsAliveInZombieRegion != newValue)
        {
            isCountedAsAliveInZombieRegion = newValue;
            if (newValue)
            {
                zombieRegion.alive++;
            }
            else
            {
                zombieRegion.alive--;
            }
        }
    }

    private void SetCountedAsAliveBossInZombieRegion(bool newValue)
    {
        if (isCountedAsAliveBossInZombieRegion != newValue)
        {
            isCountedAsAliveBossInZombieRegion = newValue;
            if (newValue)
            {
                zombieRegion.aliveBossZombieCount++;
            }
            else
            {
                zombieRegion.aliveBossZombieCount--;
            }
        }
    }
}
