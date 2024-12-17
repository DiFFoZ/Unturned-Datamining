using System;
using System.Collections.Generic;
using SDG.Framework.Water;
using Steamworks;
using UnityEngine;

namespace SDG.Unturned;

public class Animal : MonoBehaviour, IExplosionDamageable, IEquatable<IExplosionDamageable>
{
    private Animation animator;

    private Transform skeleton;

    private Renderer renderer_0;

    private Renderer renderer_1;

    private double lastEat;

    private double lastGlance;

    private double startleAnimationCompletionTime;

    private double lastWander;

    private double lastStuck;

    private double lastTarget;

    private double lastAttack;

    private double lastRegen;

    private float eatTime;

    private float glanceTime;

    private float attackDuration;

    private float attackInterval;

    private double startedRoar;

    private double startedPanic;

    private float eatDelay;

    private float glanceDelay;

    private float wanderDelay;

    private bool isPlayingEat;

    private bool isPlayingGlance;

    private bool isPlayingStartleAnimation;

    private bool isPlayingAttack;

    private Player currentTargetPlayer;

    private Vector3 lastUpdatePos;

    private float lastUpdateAngle;

    private NetworkSnapshotBuffer<YawSnapshotInfo> nsb;

    private bool isMoving;

    private bool isRunning;

    private bool isTicking;

    private bool _isFleeing;

    private bool isWandering;

    private bool isStuck;

    private bool isAttacking;

    private float _lastDead;

    public bool isDead;

    public ushort index;

    public ushort id;

    public PackInfo pack;

    private ushort health;

    private Vector3 ragdoll;

    private AnimalAsset _asset;

    private CharacterController controller;

    /// <summary>
    /// Whether this animal was updated in this network tick and needs to be resent.
    /// </summary>
    public bool isUpdated;

    /// <summary>
    /// Reduces frequency of UndergroundAllowlist checks because it can be expensive with lots of entities and volumes. 
    /// </summary>
    private float undergroundTestTimer = 10f;

    private double lastTick;

    private bool hasIdleAnimation;

    private bool hasRunAnimation;

    private bool hasWalkAnimation;

    public Vector3 target { get; private set; }

    public bool IsEligibleForExplosionDamage => IsAlive;

    public bool isFleeing => _isFleeing;

    public bool isHunting { get; private set; }

    public float lastDead => _lastDead;

    public bool IsAlive => !isDead;

    public AnimalAsset asset => _asset;

    public bool Equals(IExplosionDamageable obj)
    {
        return this == obj;
    }

    public Vector3 GetClosestPointToExplosion(Vector3 explosionCenter)
    {
        return CollisionUtil.ClosestPoint(base.gameObject, explosionCenter, includeInactive: false, -4194305);
    }

    public void ApplyExplosionDamage(in ExplosionParameters explosionParameters, ref ExplosionDamageParameters damageParameters)
    {
        if (!damageParameters.shouldAffectAnimals)
        {
            return;
        }
        Vector3 vector = damageParameters.closestPoint - explosionParameters.point;
        float magnitude = vector.magnitude;
        if (magnitude > explosionParameters.damageRadius)
        {
            return;
        }
        Vector3 vector2 = vector / magnitude;
        if (damageParameters.LineOfSightTest(explosionParameters.point, vector2, magnitude, out var hit) && hit.transform != null && !hit.transform.IsChildOf(base.transform))
        {
            return;
        }
        if (explosionParameters.playImpactEffect)
        {
            EffectAsset effectAsset = DamageTool.FleshDynamicRef.Find();
            if (effectAsset != null)
            {
                TriggerEffectParameters parameters = new TriggerEffectParameters(effectAsset);
                parameters.relevantDistance = EffectManager.SMALL;
                parameters.position = base.transform.position + Vector3.up + Vector3.up;
                EffectManager.triggerEffect(parameters);
                parameters.SetDirection(-vector2);
                EffectManager.triggerEffect(parameters);
            }
        }
        DamageTool.damage(this, vector2, explosionParameters.animalDamage, 1f - magnitude / explosionParameters.damageRadius, out var kill, out var xp, explosionParameters.ragdollEffect);
        if (kill != 0)
        {
            damageParameters.kills.Add(kill);
        }
        damageParameters.xp += xp;
    }

    private void updateTicking()
    {
        if (isFleeing || isWandering || isHunting)
        {
            if (!isTicking)
            {
                isTicking = true;
                AnimalManager.tickingAnimals.Add(this);
                lastTick = Time.timeAsDouble;
            }
        }
        else if (isTicking)
        {
            isTicking = false;
            AnimalManager.tickingAnimals.RemoveFast(this);
        }
    }

    public float GetHealth()
    {
        return (int)health;
    }

    public Player GetTargetPlayer()
    {
        return currentTargetPlayer;
    }

    public void askEat()
    {
        if (isDead)
        {
            return;
        }
        lastEat = Time.timeAsDouble;
        eatDelay = UnityEngine.Random.Range(4f, 8f);
        isPlayingEat = true;
        if (!Dedicator.IsDedicatedServer || asset.shouldPlayAnimsOnDedicatedServer)
        {
            string text = ((asset.eatAnimVariantsCount != 1) ? ("Eat_" + UnityEngine.Random.Range(0, asset.eatAnimVariantsCount)) : "Eat");
            AnimationClip clip = animator.GetClip(text);
            if (clip != null)
            {
                eatTime = clip.length;
                animator.Play(text);
            }
            else if ((bool)Assets.shouldValidateAssets)
            {
                Assets.ReportError(asset, "missing AnimationClip \"" + text + "\"");
            }
        }
    }

    public void askGlance()
    {
        if (isDead)
        {
            return;
        }
        lastGlance = Time.timeAsDouble;
        glanceDelay = UnityEngine.Random.Range(4f, 8f);
        isPlayingGlance = true;
        if (!Dedicator.IsDedicatedServer || asset.shouldPlayAnimsOnDedicatedServer)
        {
            string text = "Glance_" + UnityEngine.Random.Range(0, asset.glanceAnimVariantsCount);
            AnimationClip clip = animator.GetClip(text);
            if (clip != null)
            {
                glanceTime = clip.length;
                animator.Play(text);
            }
            else if ((bool)Assets.shouldValidateAssets)
            {
                Assets.ReportError(asset, "missing AnimationClip \"" + text + "\"");
            }
        }
    }

    public void PlayStartleAnimation()
    {
        if (isDead)
        {
            return;
        }
        startleAnimationCompletionTime = Time.timeAsDouble + 0.5;
        isPlayingStartleAnimation = true;
        if (!Dedicator.IsDedicatedServer || asset.shouldPlayAnimsOnDedicatedServer)
        {
            string text = ((asset.startleAnimVariantsCount != 1) ? ("Startle_" + UnityEngine.Random.Range(0, asset.startleAnimVariantsCount)) : "Startle");
            AnimationClip clip = animator.GetClip(text);
            if (clip != null)
            {
                startleAnimationCompletionTime = Time.timeAsDouble + (double)clip.length;
                animator.Play(text);
            }
            else if ((bool)Assets.shouldValidateAssets)
            {
                Assets.ReportError(asset, "missing AnimationClip \"" + text + "\"");
            }
        }
    }

    public void askAttack()
    {
        if (isDead)
        {
            return;
        }
        lastAttack = Time.timeAsDouble;
        isPlayingAttack = true;
        if (!Dedicator.IsDedicatedServer || asset.shouldPlayAnimsOnDedicatedServer)
        {
            string text = ((asset.attackAnimVariantsCount != 1) ? ("Attack_" + UnityEngine.Random.Range(0, asset.attackAnimVariantsCount)) : "Attack");
            AnimationClip clip = animator.GetClip(text);
            if (clip != null)
            {
                attackDuration = clip.length;
                attackInterval = Mathf.Max(attackDuration, asset.attackInterval);
                animator.Play(text);
            }
            else if ((bool)Assets.shouldValidateAssets)
            {
                Assets.ReportError(asset, "missing AnimationClip \"" + text + "\"");
            }
            if (asset != null && asset.roars != null && asset.roars.Length != 0 && Time.timeAsDouble - startedRoar > 1.0)
            {
                startedRoar = Time.timeAsDouble;
                AudioClip clip2 = asset.roars[UnityEngine.Random.Range(0, asset.roars.Length)];
                OneShotAudioParameters oneShotAudioParameters = new OneShotAudioParameters(base.transform, clip2);
                oneShotAudioParameters.volume = 0.5f;
                oneShotAudioParameters.RandomizePitch(0.9f, 1.1f);
                oneShotAudioParameters.SetLinearRolloff(1f, 32f);
                oneShotAudioParameters.Play();
            }
        }
    }

    public void askPanic()
    {
        if (!isDead && (!Dedicator.IsDedicatedServer || asset.shouldPlayAnimsOnDedicatedServer) && asset != null && asset.panics != null && asset.panics.Length != 0 && Time.timeAsDouble - startedPanic > 1.0)
        {
            startedPanic = Time.timeAsDouble;
            AudioClip clip = asset.panics[UnityEngine.Random.Range(0, asset.panics.Length)];
            OneShotAudioParameters oneShotAudioParameters = new OneShotAudioParameters(base.transform, clip);
            oneShotAudioParameters.volume = 0.5f;
            oneShotAudioParameters.RandomizePitch(0.9f, 1.1f);
            oneShotAudioParameters.SetLinearRolloff(1f, 32f);
            oneShotAudioParameters.Play();
        }
    }

    public void askDamage(ushort amount, Vector3 newRagdoll, out EPlayerKill kill, out uint xp, bool trackKill = true, bool dropLoot = true, ERagdollEffect ragdollEffect = ERagdollEffect.NONE)
    {
        kill = EPlayerKill.NONE;
        xp = 0u;
        if (amount == 0 || isDead || !IsAlive)
        {
            return;
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
            kill = EPlayerKill.ANIMAL;
            if (asset != null)
            {
                xp = asset.rewardXP;
            }
            if (dropLoot)
            {
                AnimalManager.dropLoot(this);
            }
            AnimalManager.sendAnimalDead(this, ragdoll, ragdollEffect);
            if (trackKill)
            {
                for (int i = 0; i < Provider.clients.Count; i++)
                {
                    SteamPlayer steamPlayer = Provider.clients[i];
                    if (!(steamPlayer.player == null) && !(steamPlayer.player.movement == null) && !(steamPlayer.player.life == null) && !steamPlayer.player.life.isDead && (steamPlayer.player.transform.position - base.transform.position).sqrMagnitude < 262144f)
                    {
                        steamPlayer.player.quests.trackAnimalKill(this);
                    }
                }
            }
        }
        else if (asset != null && asset.panics != null && asset.panics.Length != 0)
        {
            AnimalManager.sendAnimalPanic(this);
        }
        lastRegen = Time.timeAsDouble;
    }

    public void sendRevive(Vector3 position, float angle)
    {
        AnimalManager.sendAnimalAlive(this, position, MeasurementTool.angleToByte(angle));
    }

    private bool checkTargetValid(Vector3 point)
    {
        if (!Level.checkSafeIncludingClipVolumes(point))
        {
            return false;
        }
        float height = LevelGround.getHeight(point);
        return !WaterUtility.isPointUnderwater(new Vector3(point.x, height - 1f, point.z));
    }

    private Vector3 getFleeTarget(Vector3 normal)
    {
        Vector3 vector = base.transform.position + normal * 64f + new Vector3(UnityEngine.Random.Range(-8f, 8f), 0f, UnityEngine.Random.Range(-8f, 8f));
        if (checkTargetValid(vector))
        {
            return vector;
        }
        Vector3 vector2 = base.transform.position + normal * 32f + new Vector3(UnityEngine.Random.Range(-8f, 8f), 0f, UnityEngine.Random.Range(-8f, 8f));
        if (checkTargetValid(vector2))
        {
            return vector2;
        }
        vector2 = base.transform.position + normal * -32f + new Vector3(UnityEngine.Random.Range(-8f, 8f), 0f, UnityEngine.Random.Range(-8f, 8f));
        if (checkTargetValid(vector2))
        {
            return vector2;
        }
        vector2 = base.transform.position + normal * -16f + new Vector3(UnityEngine.Random.Range(-8f, 8f), 0f, UnityEngine.Random.Range(-8f, 8f));
        if (checkTargetValid(vector2))
        {
            return vector2;
        }
        return vector;
    }

    private void getWanderTarget()
    {
        Vector3 point;
        if (isStuck)
        {
            point = base.transform.position + new Vector3(UnityEngine.Random.Range(-8f, 8f), 0f, UnityEngine.Random.Range(-8f, 8f));
            if (!checkTargetValid(point))
            {
                return;
            }
        }
        else if (pack != null)
        {
            if ((base.transform.position - pack.getAverageAnimalPoint()).sqrMagnitude > 256f)
            {
                point = pack.getAverageAnimalPoint() + new Vector3(UnityEngine.Random.Range(-8f, 8f), 0f, UnityEngine.Random.Range(-8f, 8f));
            }
            else
            {
                Vector3 wanderDirection = pack.getWanderDirection();
                point = base.transform.position + wanderDirection * UnityEngine.Random.Range(6f, 8f) + new Vector3(UnityEngine.Random.Range(-4f, 4f), 0f, UnityEngine.Random.Range(-4f, 4f));
                if (!checkTargetValid(point))
                {
                    point = base.transform.position - wanderDirection * UnityEngine.Random.Range(6f, 8f) + new Vector3(UnityEngine.Random.Range(-4f, 4f), 0f, UnityEngine.Random.Range(-4f, 4f));
                    if (!checkTargetValid(point))
                    {
                        return;
                    }
                    pack.wanderAngle += UnityEngine.Random.Range(160f, 200f);
                }
                else
                {
                    pack.wanderAngle += UnityEngine.Random.Range(-20f, 20f);
                }
            }
        }
        else
        {
            point = base.transform.position + new Vector3(UnityEngine.Random.Range(-8f, 8f), 0f, UnityEngine.Random.Range(-8f, 8f));
        }
        target = point;
        isWandering = true;
        updateTicking();
    }

    public bool checkAlert(Player potentialTargetPlayer)
    {
        return currentTargetPlayer != potentialTargetPlayer;
    }

    public void alertPlayer(Player potentialTargetPlayer, bool sendToPack)
    {
        if (sendToPack && pack != null)
        {
            for (int i = 0; i < pack.animals.Count; i++)
            {
                Animal animal = pack.animals[i];
                if (!(animal == null) && !(animal == this))
                {
                    animal.alertPlayer(potentialTargetPlayer, sendToPack: false);
                }
            }
        }
        if (!isDead && !(currentTargetPlayer == potentialTargetPlayer))
        {
            if (!isHunting)
            {
                AnimalManager.sendAnimalStartle(this);
            }
            if (currentTargetPlayer == null)
            {
                _isFleeing = false;
                isWandering = false;
                isHunting = true;
                updateTicking();
                lastStuck = Time.timeAsDouble;
                currentTargetPlayer = potentialTargetPlayer;
            }
            else if ((potentialTargetPlayer.transform.position - base.transform.position).sqrMagnitude < (currentTargetPlayer.transform.position - base.transform.position).sqrMagnitude)
            {
                _isFleeing = false;
                isWandering = false;
                isHunting = true;
                updateTicking();
                currentTargetPlayer = potentialTargetPlayer;
            }
        }
    }

    /// <summary>
    /// Alert this animal that it was damaged from a given position.
    /// Offensive animals investigate the position, whereas other animals run away.
    /// </summary>
    public void alertDamagedFromPoint(Vector3 point)
    {
        if (asset != null && asset.behaviour == EAnimalBehaviour.OFFENSE)
        {
            alertGoToPoint(point, sendToPack: true);
        }
        else
        {
            alertRunAwayFromPoint(point, sendToPack: true);
        }
    }

    /// <summary>
    /// Alerts this animal that it needs to run away.
    /// </summary>
    /// <param name="newPosition">The position to run away from.</param>
    public void alertRunAwayFromPoint(Vector3 newPosition, bool sendToPack)
    {
        alertDirection((base.transform.position - newPosition).normalized, sendToPack);
    }

    /// <summary>
    /// Keep for plugin backwards compatibility.
    /// </summary>
    [Obsolete("Clarified with alertRunAwayFromPoint, alertGoToPoint and alertDamagedFromPoint.")]
    public void alertPoint(Vector3 newPosition, bool sendToPack)
    {
        alertRunAwayFromPoint(newPosition, sendToPack);
    }

    public void alertDirection(Vector3 newDirection, bool sendToPack)
    {
        if (sendToPack && pack != null)
        {
            for (int i = 0; i < pack.animals.Count; i++)
            {
                Animal animal = pack.animals[i];
                if (!(animal == null) && !(animal == this))
                {
                    animal.alertDirection(newDirection, sendToPack: false);
                }
            }
        }
        if (!isDead && !isStuck && !isHunting)
        {
            if (!isFleeing)
            {
                AnimalManager.sendAnimalStartle(this);
            }
            _isFleeing = true;
            isWandering = false;
            isHunting = false;
            updateTicking();
            target = getFleeTarget(newDirection);
        }
    }

    public void alertGoToPoint(Vector3 point, bool sendToPack)
    {
        if (sendToPack && pack != null)
        {
            for (int i = 0; i < pack.animals.Count; i++)
            {
                Animal animal = pack.animals[i];
                if (!(animal == null) && !(animal == this))
                {
                    animal.alertGoToPoint(point, sendToPack: false);
                }
            }
        }
        if (!isDead && !isFleeing && !isHunting)
        {
            lastWander = Time.timeAsDouble;
            _isFleeing = false;
            isWandering = true;
            isHunting = false;
            target = point;
            updateTicking();
        }
    }

    private void stop()
    {
        isMoving = false;
        isRunning = false;
        _isFleeing = false;
        isWandering = false;
        isHunting = false;
        updateTicking();
        isStuck = false;
        currentTargetPlayer = null;
        target = base.transform.position;
    }

    public void tellAlive(Vector3 newPosition, byte newAngle)
    {
        isDead = false;
        base.transform.position = newPosition;
        base.transform.rotation = Quaternion.Euler(0f, newAngle * 2, 0f);
        updateLife();
        updateStates();
        reset();
    }

    public void tellDead(Vector3 newRagdoll, ERagdollEffect ragdollEffect)
    {
        isDead = true;
        _lastDead = Time.realtimeSinceStartup;
        updateLife();
        if (!Dedicator.IsDedicatedServer)
        {
            ragdoll = newRagdoll;
            RagdollTool.ragdollAnimal(base.transform.position, base.transform.rotation, skeleton, ragdoll, id, ragdollEffect);
        }
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

    public void tellState(Vector3 newPosition, float newAngle)
    {
        lastUpdatePos = newPosition;
        lastUpdateAngle = newAngle;
        if (nsb != null)
        {
            nsb.addNewSnapshot(new YawSnapshotInfo(newPosition, newAngle));
        }
        if (isPlayingEat || isPlayingGlance)
        {
            isPlayingEat = false;
            isPlayingGlance = false;
            animator.Stop();
        }
    }

    private void updateLife()
    {
        if (controller != null)
        {
            controller.SetDetectCollisionsDeferred(IsAlive);
        }
        if (!Dedicator.IsDedicatedServer || asset.shouldPlayAnimsOnDedicatedServer)
        {
            if (renderer_0 != null)
            {
                renderer_0.enabled = IsAlive;
            }
            if (renderer_1 != null)
            {
                renderer_1.enabled = IsAlive;
            }
            skeleton.gameObject.SetActive(IsAlive);
        }
        Collider component = GetComponent<Collider>();
        if (component != null)
        {
            component.enabled = IsAlive;
        }
    }

    public void updateStates()
    {
        lastUpdatePos = base.transform.position;
        lastUpdateAngle = base.transform.rotation.eulerAngles.y;
        if (nsb != null)
        {
            nsb.updateLastSnapshot(new YawSnapshotInfo(base.transform.position, base.transform.rotation.eulerAngles.y));
        }
    }

    private void reset()
    {
        target = base.transform.position;
        lastWander = Time.timeAsDouble;
        lastStuck = Time.timeAsDouble;
        isPlayingEat = false;
        isPlayingGlance = false;
        isPlayingStartleAnimation = false;
        isMoving = false;
        isRunning = false;
        _isFleeing = false;
        isWandering = false;
        isHunting = false;
        updateTicking();
        isStuck = false;
        health = asset.health;
    }

    private void move(float delta)
    {
        Vector3 vector = target - base.transform.position;
        vector.y = 0f;
        Vector3 forward = vector;
        float magnitude = vector.magnitude;
        bool flag = magnitude > 0.75f;
        if ((!Dedicator.IsDedicatedServer || asset.shouldPlayAnimsOnDedicatedServer) && flag && !isMoving)
        {
            if (isPlayingEat)
            {
                animator.Stop();
                isPlayingEat = false;
            }
            if (isPlayingGlance)
            {
                animator.Stop();
                isPlayingGlance = false;
            }
        }
        isMoving = flag;
        isRunning = isMoving && (isFleeing || isHunting);
        float num = Mathf.Clamp01(magnitude / 0.6f);
        Vector3 forward2 = base.transform.forward;
        float a = Vector3.Dot(vector.normalized, forward2);
        float num2 = (isRunning ? asset.speedRun : asset.speedWalk) * Mathf.Max(a, 0.05f) * num;
        if (Time.deltaTime > 0f)
        {
            num2 = Mathf.Clamp(num2, 0f, magnitude / (Time.deltaTime * 2f));
        }
        vector = base.transform.forward * num2;
        vector.y = Physics.gravity.y * 2f;
        if (!isMoving)
        {
            vector.x = 0f;
            vector.z = 0f;
            if (!isStuck)
            {
                _isFleeing = false;
                isWandering = false;
                updateTicking();
            }
        }
        else
        {
            Quaternion rotation = base.transform.rotation;
            Quaternion b = Quaternion.LookRotation(forward);
            Vector3 eulerAngles = Quaternion.Slerp(rotation, b, 8f * delta).eulerAngles;
            eulerAngles.z = 0f;
            eulerAngles.x = 0f;
            rotation = Quaternion.Euler(eulerAngles);
            base.transform.rotation = rotation;
        }
        controller?.Move(vector * delta);
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
                if (Mathf.Abs(lastUpdatePos.x - base.transform.position.x) > Provider.UPDATE_DISTANCE || Mathf.Abs(lastUpdatePos.y - base.transform.position.y) > Provider.UPDATE_DISTANCE || Mathf.Abs(lastUpdatePos.z - base.transform.position.z) > Provider.UPDATE_DISTANCE || Mathf.Abs(lastUpdateAngle - base.transform.rotation.eulerAngles.y) > 1f)
                {
                    lastUpdatePos = base.transform.position;
                    lastUpdateAngle = base.transform.rotation.eulerAngles.y;
                    isUpdated = true;
                    AnimalManager.updates++;
                    if (isStuck && Time.timeAsDouble - lastStuck > 0.5)
                    {
                        isStuck = false;
                        lastStuck = Time.timeAsDouble;
                    }
                }
                else if (isMoving)
                {
                    if (Time.timeAsDouble - lastStuck > 0.125)
                    {
                        isStuck = true;
                    }
                }
                else
                {
                    isStuck = false;
                    lastStuck = Time.timeAsDouble;
                }
            }
        }
        else
        {
            if (Mathf.Abs(lastUpdatePos.x - base.transform.position.x) > 0.01f || Mathf.Abs(lastUpdatePos.y - base.transform.position.y) > 0.01f || Mathf.Abs(lastUpdatePos.z - base.transform.position.z) > 0.01f)
            {
                if (!isMoving)
                {
                    if (isPlayingEat)
                    {
                        animator.Stop();
                        isPlayingEat = false;
                    }
                    if (isPlayingGlance)
                    {
                        animator.Stop();
                        isPlayingGlance = false;
                    }
                }
                isMoving = true;
                isRunning = (lastUpdatePos - base.transform.position).sqrMagnitude > 1f;
            }
            else
            {
                isMoving = false;
                isRunning = false;
            }
            if (nsb != null)
            {
                YawSnapshotInfo currentSnapshot = nsb.getCurrentSnapshot();
                base.transform.position = currentSnapshot.pos;
                base.transform.rotation = Quaternion.Euler(0f, currentSnapshot.yaw, 0f);
            }
        }
        if ((!Dedicator.IsDedicatedServer || asset.shouldPlayAnimsOnDedicatedServer) && !isMoving && !isPlayingEat && !isPlayingGlance && !isPlayingAttack && Time.timeAsDouble - lastAttack > (double)attackInterval + 0.5)
        {
            if (Time.timeAsDouble - lastEat > (double)eatDelay)
            {
                askEat();
            }
            else if (Time.timeAsDouble - lastGlance > (double)glanceDelay)
            {
                askGlance();
            }
        }
        if (Provider.isServer)
        {
            if (isStuck)
            {
                if (Time.timeAsDouble - lastStuck > 0.75)
                {
                    lastStuck = Time.timeAsDouble;
                    getWanderTarget();
                }
            }
            else if (!isFleeing && !isHunting)
            {
                if (Time.timeAsDouble - lastWander > (double)wanderDelay)
                {
                    lastWander = Time.timeAsDouble;
                    wanderDelay = UnityEngine.Random.Range(8f, 16f);
                    getWanderTarget();
                }
            }
            else
            {
                lastWander = Time.timeAsDouble;
            }
        }
        if (isPlayingEat)
        {
            if (Time.timeAsDouble - lastEat > (double)eatTime)
            {
                isPlayingEat = false;
            }
        }
        else if (isPlayingGlance)
        {
            if (Time.timeAsDouble - lastGlance > (double)glanceTime)
            {
                isPlayingGlance = false;
            }
        }
        else if (isPlayingStartleAnimation)
        {
            if (Time.timeAsDouble > startleAnimationCompletionTime)
            {
                isPlayingStartleAnimation = false;
            }
        }
        else if (isPlayingAttack)
        {
            if (Time.timeAsDouble - lastAttack > (double)attackDuration)
            {
                isPlayingAttack = false;
            }
        }
        else if (!Dedicator.IsDedicatedServer || asset.shouldPlayAnimsOnDedicatedServer)
        {
            if (isRunning && hasRunAnimation)
            {
                animator.Play("Run");
            }
            else if (isMoving && hasWalkAnimation)
            {
                animator.Play("Walk");
            }
            else if (hasIdleAnimation)
            {
                animator.Play("Idle");
            }
        }
        if (Provider.isServer && health < asset.health && Time.timeAsDouble - lastRegen > (double)asset.regen)
        {
            lastRegen = Time.timeAsDouble;
            health++;
        }
    }

    public void tick()
    {
        float num = (float)(Time.timeAsDouble - lastTick);
        lastTick = Time.timeAsDouble;
        undergroundTestTimer -= num;
        if (undergroundTestTimer < 0f)
        {
            undergroundTestTimer = UnityEngine.Random.Range(30f, 60f);
            if (!UndergroundAllowlist.IsPositionWithinValidHeight(base.transform.position))
            {
                AnimalManager.TeleportAnimalBackIntoMap(this);
                return;
            }
        }
        if (isHunting)
        {
            if (currentTargetPlayer != null && currentTargetPlayer.life.IsAlive && currentTargetPlayer.stance.stance != EPlayerStance.SWIM)
            {
                target = currentTargetPlayer.transform.position;
                float num2 = MathfEx.HorizontalDistanceSquared(target, base.transform.position);
                float num3 = Mathf.Abs(target.y - base.transform.position.y);
                if (num2 < ((currentTargetPlayer.movement.getVehicle() != null) ? asset.horizontalVehicleAttackRangeSquared : asset.horizontalAttackRangeSquared) && num3 < asset.verticalAttackRange)
                {
                    if (Time.timeAsDouble - lastTarget > (double)(Dedicator.IsDedicatedServer ? 0.3f : 0.1f))
                    {
                        if (isAttacking)
                        {
                            if (Time.timeAsDouble - lastAttack > (double)(attackDuration * 0.5f))
                            {
                                isAttacking = false;
                                byte damage = asset.damage;
                                damage = (byte)((float)(int)damage * Provider.modeConfigData.Animals.Damage_Multiplier);
                                if (currentTargetPlayer.movement.getVehicle() != null)
                                {
                                    if (currentTargetPlayer.movement.getVehicle().asset != null && currentTargetPlayer.movement.getVehicle().asset.isVulnerableToEnvironment)
                                    {
                                        VehicleManager.damage(currentTargetPlayer.movement.getVehicle(), (int)damage, 1f, canRepair: true, default(CSteamID), EDamageOrigin.Animal_Attack);
                                    }
                                }
                                else
                                {
                                    DamagePlayerParameters parameters = new DamagePlayerParameters(currentTargetPlayer);
                                    parameters.cause = EDeathCause.ANIMAL;
                                    parameters.killer = Provider.server;
                                    parameters.direction = (target - base.transform.position).normalized;
                                    parameters.damage = (int)damage;
                                    parameters.respectArmor = true;
                                    DamageTool.damagePlayer(parameters, out var _);
                                }
                            }
                        }
                        else if (Time.timeAsDouble - lastAttack > (double)attackInterval)
                        {
                            isAttacking = true;
                            AnimalManager.sendAnimalAttack(this);
                        }
                    }
                }
                else if (num2 > 4096f)
                {
                    currentTargetPlayer = null;
                    isHunting = false;
                    updateTicking();
                }
                else
                {
                    lastTarget = Time.timeAsDouble;
                    isAttacking = false;
                }
            }
            else
            {
                currentTargetPlayer = null;
                isHunting = false;
                updateTicking();
            }
            lastWander = Time.timeAsDouble;
        }
        move(num);
    }

    public void init()
    {
        _asset = Assets.find(EAssetType.ANIMAL, id) as AnimalAsset;
        attackDuration = 0.5f;
        attackInterval = asset.attackInterval;
        eatTime = 0.5f;
        glanceTime = 0.5f;
        if (!Dedicator.IsDedicatedServer || asset.shouldPlayAnimsOnDedicatedServer)
        {
            animator = base.transform.Find("Character").GetComponent<Animation>();
            skeleton = animator.transform.Find("Skeleton");
            if (animator.transform.Find("Model_0") != null)
            {
                renderer_0 = animator.transform.Find("Model_0").GetComponent<Renderer>();
            }
            if ((bool)animator.transform.Find("Model_1"))
            {
                renderer_1 = animator.transform.Find("Model_1").GetComponent<Renderer>();
            }
            if (animator != null)
            {
                hasIdleAnimation = animator.GetClip("Idle") != null;
                hasRunAnimation = animator.GetClip("Run") != null;
                hasWalkAnimation = animator.GetClip("Walk") != null;
            }
        }
        if (Provider.isServer)
        {
            controller = GetComponent<CharacterController>();
            if (controller != null)
            {
                controller.enableOverlapRecovery = false;
            }
            else
            {
                Assets.ReportError(asset, "missing CharacterController component");
            }
        }
        else
        {
            nsb = new NetworkSnapshotBuffer<YawSnapshotInfo>(Provider.UPDATE_TIME, Provider.UPDATE_DELAY);
        }
        reset();
        lastEat = Time.timeAsDouble + (double)UnityEngine.Random.Range(4f, 16f);
        lastGlance = Time.timeAsDouble + (double)UnityEngine.Random.Range(4f, 16f);
        lastWander = Time.timeAsDouble + (double)UnityEngine.Random.Range(8f, 32f);
        eatDelay = UnityEngine.Random.Range(4f, 8f);
        glanceDelay = UnityEngine.Random.Range(4f, 8f);
        wanderDelay = UnityEngine.Random.Range(8f, 16f);
        updateLife();
        updateStates();
    }

    [Obsolete("Renamed to PlayStartleAnimation")]
    public void askStartle()
    {
        PlayStartleAnimation();
    }

    void IExplosionDamageable.ApplyExplosionDamage(in ExplosionParameters explosionParameters, ref ExplosionDamageParameters damageParameters)
    {
        ApplyExplosionDamage(in explosionParameters, ref damageParameters);
    }
}
