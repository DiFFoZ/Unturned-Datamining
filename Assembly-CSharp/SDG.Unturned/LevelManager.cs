using System;
using System.Collections.Generic;
using SDG.NetPak;
using SDG.NetTransport;
using Steamworks;
using UnityEngine;

namespace SDG.Unturned;

public class LevelManager : SteamCaller
{
    public static readonly byte SAVEDATA_VERSION = 1;

    private static LevelManager manager;

    private static bool isInit;

    private static ELevelType _levelType;

    private static AudioClip timer;

    private static float lastFinaleMessage;

    private static float lastTimerMessage;

    private static float nextAreaModify;

    private static int countTimerMessages;

    public static EArenaState arenaState;

    public static EArenaMessage arenaMessage;

    private static int nonGroups;

    public static List<CSteamID> arenaGroups;

    public static List<ArenaPlayer> arenaPlayers;

    private static Vector3 _arenaCurrentCenter;

    private static Vector3 _arenaOriginCenter;

    private static Vector3 _arenaTargetCenter;

    private static float _arenaCurrentRadius;

    private static float _arenaOriginRadius;

    private static float _arenaTargetRadius;

    private static float _arenaCompactorSpeed;

    private static float arenaSqrRadius;

    private static Transform arenaCurrentArea;

    private static Transform arenaTargetArea;

    public static ArenaMessageUpdated onArenaMessageUpdated;

    public static ArenaPlayerUpdated onArenaPlayerUpdated;

    public static LevelNumberUpdated onLevelNumberUpdated;

    private static readonly ClientStaticMethod<Vector3, float, Vector3, float, Vector3, float, float, byte> SendArenaOrigin = ClientStaticMethod<Vector3, float, Vector3, float, Vector3, float, float, byte>.Get(ReceiveArenaOrigin);

    private static readonly ClientStaticMethod<EArenaMessage> SendArenaMessage = ClientStaticMethod<EArenaMessage>.Get(ReceiveArenaMessage);

    private static readonly ClientStaticMethod SendArenaPlayer = ClientStaticMethod.Get(ReceiveArenaPlayer);

    private static readonly ClientStaticMethod<byte> SendLevelNumber = ClientStaticMethod<byte>.Get(ReceiveLevelNumber);

    private static readonly ClientStaticMethod<byte> SendLevelTimer = ClientStaticMethod<byte>.Get(ReceiveLevelTimer);

    private static List<AirdropDevkitNode> airdropNodes;

    private static List<AirdropInfo> airdrops;

    public static uint airdropFrequency;

    private static bool _hasAirdrop;

    private static float lastAirdrop;

    private static readonly ClientStaticMethod<ushort, Vector3, Vector3, float, float, float> SendAirdropState = ClientStaticMethod<ushort, Vector3, Vector3, float, float, float>.Get(ReceiveAirdropState);

    /// <summary>
    /// Exposed for Rocket transition to modules backwards compatibility.
    /// </summary>
    public static LevelManager instance => manager;

    public static ELevelType levelType => _levelType;

    /// <summary>
    /// Is the active level an Arena mode map?
    /// </summary>
    public static bool isArenaMode => levelType == ELevelType.ARENA;

    public static Vector3 arenaCurrentCenter => _arenaCurrentCenter;

    public static Vector3 arenaOriginCenter => _arenaOriginCenter;

    public static Vector3 arenaTargetCenter => _arenaTargetCenter;

    public static float arenaCurrentRadius => _arenaCurrentRadius;

    public static float arenaOriginRadius => _arenaOriginRadius;

    public static float arenaTargetRadius => _arenaTargetRadius;

    public static float arenaCompactorSpeed => _arenaCompactorSpeed;

    private static uint minPlayers
    {
        get
        {
            if (Dedicator.IsDedicatedServer)
            {
                return Provider.modeConfigData.Events.Arena_Min_Players;
            }
            return 1u;
        }
    }

    public static float compactorSpeed => Level.info.size switch
    {
        ELevelSize.TINY => Provider.modeConfigData.Events.Arena_Compactor_Speed_Tiny, 
        ELevelSize.SMALL => Provider.modeConfigData.Events.Arena_Compactor_Speed_Small, 
        ELevelSize.MEDIUM => Provider.modeConfigData.Events.Arena_Compactor_Speed_Medium, 
        ELevelSize.LARGE => Provider.modeConfigData.Events.Arena_Compactor_Speed_Large, 
        ELevelSize.INSANE => Provider.modeConfigData.Events.Arena_Compactor_Speed_Insane, 
        _ => 0f, 
    };

    public static bool hasAirdrop => _hasAirdrop;

    public static bool isPlayerInArena(Player player)
    {
        if (arenaState == EArenaState.CLEAR || arenaState == EArenaState.PLAY || arenaState == EArenaState.FINALE || arenaState == EArenaState.RESTART)
        {
            foreach (ArenaPlayer arenaPlayer in arenaPlayers)
            {
                if (arenaPlayer.steamPlayer != null && arenaPlayer.steamPlayer.player == player)
                {
                    return true;
                }
            }
        }
        return false;
    }

    private void findGroups()
    {
        nonGroups = 0;
        arenaGroups.Clear();
        for (int i = 0; i < Provider.clients.Count; i++)
        {
            SteamPlayer steamPlayer = Provider.clients[i];
            if (steamPlayer != null && !(steamPlayer.player == null) && !steamPlayer.player.life.isDead)
            {
                if (!steamPlayer.player.quests.isMemberOfAGroup)
                {
                    nonGroups++;
                }
                else if (!arenaGroups.Contains(steamPlayer.player.quests.groupID))
                {
                    arenaGroups.Add(steamPlayer.player.quests.groupID);
                }
            }
        }
    }

    private void updateGroups(SteamPlayer steamPlayer)
    {
        if (!steamPlayer.player.quests.isMemberOfAGroup)
        {
            nonGroups--;
            return;
        }
        for (int num = arenaPlayers.Count - 1; num >= 0; num--)
        {
            if (arenaPlayers[num].steamPlayer.player.quests.isMemberOfSameGroupAs(steamPlayer.player))
            {
                return;
            }
        }
        arenaGroups.Remove(steamPlayer.player.quests.groupID);
    }

    private void arenaLobby()
    {
        findGroups();
        if (nonGroups + arenaGroups.Count < minPlayers)
        {
            if (arenaMessage != 0)
            {
                SendArenaMessage.InvokeAndLoopback(ENetReliability.Reliable, Provider.GatherRemoteClientConnections(), EArenaMessage.LOBBY);
            }
        }
        else
        {
            arenaState = EArenaState.CLEAR;
        }
    }

    /// <summary>
    /// Find a new smaller circle within the old circle and clamp it to the playable level area.
    /// </summary>
    private void getArenaTarget(Vector3 currentCenter, float currentRadius, out Vector3 targetCenter, out float targetRadius)
    {
        targetCenter = currentCenter;
        targetRadius = currentRadius * Provider.modeConfigData.Events.Arena_Compactor_Shrink_Factor;
        float f = UnityEngine.Random.Range(0f, MathF.PI * 2f);
        float num = Mathf.Cos(f);
        float num2 = Mathf.Sin(f);
        float num3 = UnityEngine.Random.Range(0f, currentRadius - targetRadius);
        targetCenter += new Vector3(num * num3, 0f, num2 * num3);
        if (targetCenter.x - targetRadius < (float)(-Level.size / 2 + Level.border))
        {
            targetRadius = targetCenter.x - (float)(-Level.size / 2 + Level.border);
        }
        if (targetCenter.x + targetRadius > (float)(Level.size / 2 - Level.border))
        {
            targetRadius = (float)(Level.size / 2 - Level.border) - targetCenter.x;
        }
        if (targetCenter.z - targetRadius < (float)(-Level.size / 2 + Level.border))
        {
            targetRadius = targetCenter.z - (float)(-Level.size / 2 + Level.border);
        }
        if (targetCenter.z + targetRadius > (float)(Level.size / 2 - Level.border))
        {
            targetRadius = (float)(Level.size / 2 - Level.border) - targetCenter.z;
        }
    }

    private void arenaClear()
    {
        AnimalManager.askClearAllAnimals();
        VehicleManager.askVehicleDestroyAll();
        BarricadeManager.askClearAllBarricades();
        StructureManager.askClearAllStructures();
        ItemManager.askClearAllItems();
        EffectManager.askEffectClearAll();
        ObjectManager.askClearAllObjects();
        ResourceManager.askClearAllResources();
        arenaPlayers.Clear();
        Vector3 vector = Vector3.zero;
        float num = (float)(int)Level.size / 2f;
        if (Level.info.configData.Use_Arena_Compactor)
        {
            ArenaCompactorVolume randomVolumeOrNull = VolumeManager<ArenaCompactorVolume, ArenaCompactorVolumeManager>.Get().GetRandomVolumeOrNull();
            if (randomVolumeOrNull != null)
            {
                vector = randomVolumeOrNull.transform.position;
                vector.y = 0f;
                num = randomVolumeOrNull.GetSphereRadius();
            }
        }
        else
        {
            num = 16384f;
        }
        float arg = compactorSpeed;
        Vector3 targetCenter;
        float targetRadius;
        if (Level.info.configData.Use_Arena_Compactor)
        {
            if (Provider.modeConfigData.Events.Arena_Use_Compactor_Pause)
            {
                getArenaTarget(vector, num, out targetCenter, out targetRadius);
            }
            else
            {
                targetCenter = vector;
                targetRadius = 0.5f;
            }
        }
        else
        {
            targetCenter = vector;
            targetRadius = num;
        }
        SendArenaOrigin.InvokeAndLoopback(ENetReliability.Reliable, Provider.GatherRemoteClientConnections(), vector, num, vector, num, targetCenter, targetRadius, arg, (byte)(Provider.modeConfigData.Events.Arena_Clear_Timer + Provider.modeConfigData.Events.Arena_Compactor_Delay_Timer));
        arenaState = EArenaState.WARMUP;
        SendLevelTimer.InvokeAndLoopback(ENetReliability.Reliable, Provider.GatherRemoteClientConnections(), (byte)Provider.modeConfigData.Events.Arena_Clear_Timer);
    }

    private void arenaWarmUp()
    {
        if (arenaMessage != EArenaMessage.WARMUP)
        {
            SendArenaMessage.InvokeAndLoopback(ENetReliability.Reliable, Provider.GatherRemoteClientConnections(), EArenaMessage.WARMUP);
        }
        if (countTimerMessages < 0)
        {
            findGroups();
            if (nonGroups + arenaGroups.Count < minPlayers)
            {
                arenaState = EArenaState.LOBBY;
            }
            else
            {
                arenaState = EArenaState.SPAWN;
            }
        }
    }

    private void arenaSpawn()
    {
        for (byte b = 0; b < Regions.WORLD_SIZE; b++)
        {
            for (byte b2 = 0; b2 < Regions.WORLD_SIZE; b2++)
            {
                if (LevelItems.spawns[b, b2].Count > 0)
                {
                    for (int i = 0; i < LevelItems.spawns[b, b2].Count; i++)
                    {
                        ItemSpawnpoint itemSpawnpoint = LevelItems.spawns[b, b2][i];
                        ushort item = LevelItems.getItem(itemSpawnpoint);
                        if (item != 0)
                        {
                            ItemManager.dropItem(new Item(item, EItemOrigin.ADMIN), itemSpawnpoint.point, playEffect: false, isDropped: false, wideSpread: false);
                        }
                    }
                }
            }
        }
        List<VehicleSpawnpoint> spawns = LevelVehicles.spawns;
        for (int j = 0; j < spawns.Count; j++)
        {
            VehicleSpawnpoint vehicleSpawnpoint = spawns[j];
            Asset randomAssetForSpawnpoint = LevelVehicles.GetRandomAssetForSpawnpoint(vehicleSpawnpoint);
            if (randomAssetForSpawnpoint != null)
            {
                Vector3 point = vehicleSpawnpoint.point;
                point.y += 1f;
                InteractableVehicle interactableVehicle = VehicleManager.spawnVehicleV2(randomAssetForSpawnpoint, point, Quaternion.Euler(0f, vehicleSpawnpoint.angle, 0f));
                if (interactableVehicle != null)
                {
                    interactableVehicle.WasNaturallySpawned = true;
                }
            }
        }
        foreach (AnimalSpawnpoint spawn in LevelAnimals.spawns)
        {
            ushort animal = LevelAnimals.getAnimal(spawn);
            if (animal != 0)
            {
                Vector3 point2 = spawn.point;
                point2.y += 0.1f;
                AnimalManager.spawnAnimal(animal, point2, Quaternion.Euler(0f, UnityEngine.Random.Range(0, 360), 0f));
            }
        }
        List<PlayerSpawnpoint> altSpawns = LevelPlayers.getAltSpawns();
        float num = arenaCurrentRadius - SafezoneNode.MIN_SIZE;
        num *= num;
        for (int num2 = altSpawns.Count - 1; num2 >= 0; num2--)
        {
            if (MathfEx.HorizontalDistanceSquared(altSpawns[num2].point, arenaCurrentCenter) > num)
            {
                altSpawns.RemoveAt(num2);
            }
        }
        List<SteamPlayer> list = new List<SteamPlayer>(Provider.clients);
        while (altSpawns.Count > 0 && list.Count != 0)
        {
            int index = UnityEngine.Random.Range(0, list.Count);
            SteamPlayer steamPlayer = list[index];
            list.RemoveAtFast(index);
            if (steamPlayer == null || steamPlayer.player == null || steamPlayer.player.life.isDead)
            {
                continue;
            }
            int index2 = UnityEngine.Random.Range(0, altSpawns.Count);
            PlayerSpawnpoint playerSpawnpoint = altSpawns[index2];
            altSpawns.RemoveAt(index2);
            ArenaPlayer arenaPlayer = new ArenaPlayer(steamPlayer);
            arenaPlayer.steamPlayer.player.life.sendRevive();
            arenaPlayer.steamPlayer.player.teleportToLocationUnsafe(playerSpawnpoint.point, playerSpawnpoint.angle);
            arenaPlayers.Add(arenaPlayer);
            foreach (ArenaLoadout arena_Loadout in Level.info.configData.Arena_Loadouts)
            {
                for (ushort num3 = 0; num3 < arena_Loadout.Amount; num3++)
                {
                    ushort num4 = SpawnTableTool.ResolveLegacyId(arena_Loadout.Table_ID, EAssetType.ITEM, OnGetArenaLoadoutsSpawnTableErrorContext);
                    if (num4 != 0)
                    {
                        arenaPlayer.steamPlayer.player.inventory.forceAddItemAuto(new Item(num4, full: true), autoEquipWeapon: true, autoEquipUseable: false, autoEquipClothing: true, playEffect: false);
                    }
                }
            }
        }
        arenaAirdrop();
        arenaState = EArenaState.PLAY;
        SendLevelNumber.InvokeAndLoopback(ENetReliability.Reliable, Provider.GatherRemoteClientConnections(), (byte)arenaPlayers.Count);
    }

    private string OnGetArenaLoadoutsSpawnTableErrorContext()
    {
        return "level config arena loadout";
    }

    private void arenaAirdrop()
    {
        if (!Provider.modeConfigData.Events.Use_Airdrops)
        {
            return;
        }
        Vector3 vector = arenaTargetCenter;
        float num = arenaTargetRadius;
        float num2 = num * num;
        List<AirdropDevkitNode> list = new List<AirdropDevkitNode>();
        foreach (AirdropDevkitNode airdropNode in airdropNodes)
        {
            if ((airdropNode.transform.position - vector).sqrMagnitude < num2)
            {
                list.Add(airdropNode);
            }
        }
        if (list.Count != 0)
        {
            AirdropDevkitNode airdropDevkitNode = list[UnityEngine.Random.Range(0, list.Count)];
            airdrop(airdropDevkitNode.transform.position, airdropDevkitNode.id, Provider.modeConfigData.Events.Airdrop_Speed);
        }
    }

    private void arenaPlay()
    {
        if (arenaMessage != EArenaMessage.PLAY)
        {
            SendArenaMessage.InvokeAndLoopback(ENetReliability.Reliable, Provider.GatherRemoteClientConnections(), EArenaMessage.PLAY);
        }
        if (nonGroups + arenaGroups.Count < minPlayers)
        {
            arenaState = EArenaState.FINALE;
            lastFinaleMessage = Time.realtimeSinceStartup;
            if (arenaPlayers.Count > 0)
            {
                ulong[] playersIDs3 = new ulong[arenaPlayers.Count];
                for (int i = 0; i < arenaPlayers.Count; i++)
                {
                    playersIDs3[i] = arenaPlayers[i].steamPlayer.playerID.steamID.m_SteamID;
                }
                arenaMessage = EArenaMessage.LOSE;
                SendArenaPlayer.InvokeAndLoopback(ENetReliability.Reliable, Provider.GatherRemoteClientConnections(), delegate(NetPakWriter writer)
                {
                    WriteArenaPlayer(writer, playersIDs3, EArenaMessage.WIN);
                });
            }
            else
            {
                SendArenaMessage.InvokeAndLoopback(ENetReliability.Reliable, Provider.GatherRemoteClientConnections(), EArenaMessage.LOSE);
            }
            return;
        }
        float time = Time.time;
        float deltaTime = Time.deltaTime;
        for (int num = arenaPlayers.Count - 1; num >= 0; num--)
        {
            ArenaPlayer arenaPlayer = arenaPlayers[num];
            if (arenaPlayer.steamPlayer == null || arenaPlayer.steamPlayer.player == null)
            {
                ulong[] playersIDs2 = new ulong[1];
                playersIDs2[0] = arenaPlayer.steamPlayer.playerID.steamID.m_SteamID;
                SendArenaPlayer.InvokeAndLoopback(ENetReliability.Reliable, Provider.GatherRemoteClientConnections(), delegate(NetPakWriter writer)
                {
                    WriteArenaPlayer(writer, playersIDs2, EArenaMessage.ABANDONED);
                });
                arenaPlayers.RemoveAt(num);
                updateGroups(arenaPlayer.steamPlayer);
                SendLevelNumber.InvokeAndLoopback(ENetReliability.Reliable, Provider.GatherRemoteClientConnections(), (byte)arenaPlayers.Count);
            }
            else
            {
                if (MathfEx.HorizontalDistanceSquared(arenaPlayer.steamPlayer.player.transform.position, arenaCurrentCenter) > arenaSqrRadius || arenaCurrentRadius < 1f)
                {
                    if (time - arenaPlayer.lastAreaDamage > 1f)
                    {
                        float num2 = Provider.modeConfigData.Events.Arena_Compactor_Extra_Damage_Per_Second * arenaPlayer.timeOutsideArea;
                        byte amount = MathfEx.RoundAndClampToByte((float)Provider.modeConfigData.Events.Arena_Compactor_Damage + num2);
                        arenaPlayer.steamPlayer.player.life.askDamage(amount, Vector3.up * 10f, EDeathCause.ARENA, ELimb.SPINE, CSteamID.Nil, out var _, trackKill: false, ERagdollEffect.NONE, canCauseBleeding: true, bypassSafezone: true);
                        arenaPlayer.lastAreaDamage = time;
                    }
                    arenaPlayer.timeOutsideArea += deltaTime;
                }
                else
                {
                    arenaPlayer.timeOutsideArea = 0f;
                }
                if (arenaPlayer.hasDied)
                {
                    ulong[] playersIDs = new ulong[1];
                    playersIDs[0] = arenaPlayer.steamPlayer.playerID.steamID.m_SteamID;
                    SendArenaPlayer.InvokeAndLoopback(ENetReliability.Reliable, Provider.GatherRemoteClientConnections(), delegate(NetPakWriter writer)
                    {
                        WriteArenaPlayer(writer, playersIDs, EArenaMessage.DIED);
                    });
                    arenaPlayers.RemoveAt(num);
                    updateGroups(arenaPlayer.steamPlayer);
                    SendLevelNumber.InvokeAndLoopback(ENetReliability.Reliable, Provider.GatherRemoteClientConnections(), (byte)arenaPlayers.Count);
                }
            }
        }
    }

    private void arenaFinale()
    {
        if (Time.realtimeSinceStartup - lastFinaleMessage > (float)Provider.modeConfigData.Events.Arena_Finale_Timer)
        {
            arenaState = EArenaState.RESTART;
        }
    }

    private void arenaRestart()
    {
        arenaState = EArenaState.INTERMISSION;
        SendLevelTimer.InvokeAndLoopback(ENetReliability.Reliable, Provider.GatherRemoteClientConnections(), (byte)Provider.modeConfigData.Events.Arena_Restart_Timer);
        for (int num = arenaPlayers.Count - 1; num >= 0; num--)
        {
            ArenaPlayer arenaPlayer = arenaPlayers[num];
            if (!arenaPlayer.hasDied && arenaPlayer.steamPlayer != null && !(arenaPlayer.steamPlayer.player == null))
            {
                arenaPlayer.steamPlayer.player.sendStat(EPlayerStat.ARENA_WINS);
                arenaPlayer.steamPlayer.player.life.askDamage(101, Vector3.up * 101f, EDeathCause.ARENA, ELimb.SPINE, CSteamID.Nil, out var _, trackKill: false, ERagdollEffect.NONE, canCauseBleeding: true, bypassSafezone: true);
            }
        }
    }

    private void arenaIntermission()
    {
        if (arenaMessage != EArenaMessage.INTERMISSION)
        {
            SendArenaMessage.InvokeAndLoopback(ENetReliability.Reliable, Provider.GatherRemoteClientConnections(), EArenaMessage.INTERMISSION);
        }
        if (countTimerMessages < 0)
        {
            arenaState = EArenaState.LOBBY;
        }
    }

    private void arenaTick()
    {
        if (Time.realtimeSinceStartup > nextAreaModify)
        {
            _arenaCurrentRadius = arenaCurrentRadius - Time.deltaTime * arenaCompactorSpeed;
            if (arenaCurrentRadius < arenaTargetRadius)
            {
                _arenaCurrentRadius = arenaTargetRadius;
                if (Provider.isServer && Level.info.configData.Use_Arena_Compactor && Provider.modeConfigData.Events.Arena_Use_Compactor_Pause)
                {
                    float arg = compactorSpeed;
                    getArenaTarget(arenaTargetCenter, arenaTargetRadius, out var targetCenter, out var targetRadius);
                    SendArenaOrigin.InvokeAndLoopback(ENetReliability.Reliable, Provider.GatherRemoteClientConnections(), arenaTargetCenter, arenaTargetRadius, arenaTargetCenter, arenaTargetRadius, targetCenter, targetRadius, arg, (byte)Provider.modeConfigData.Events.Arena_Compactor_Pause_Timer);
                }
            }
            arenaSqrRadius = arenaCurrentRadius * arenaCurrentRadius;
            float t = Mathf.InverseLerp(arenaTargetRadius, arenaOriginRadius, arenaCurrentRadius);
            _arenaCurrentCenter = Vector3.Lerp(arenaTargetCenter, arenaOriginCenter, t);
        }
        if (!Dedicator.IsDedicatedServer)
        {
            if (arenaCurrentArea != null)
            {
                arenaCurrentArea.position = arenaCurrentCenter;
                arenaCurrentArea.localScale = new Vector3(arenaCurrentRadius, 300f, arenaCurrentRadius);
            }
            if (arenaTargetArea != null)
            {
                arenaTargetArea.position = arenaTargetCenter;
                arenaTargetArea.localScale = new Vector3(arenaTargetRadius, 300f, arenaTargetRadius);
            }
        }
        if (countTimerMessages >= 0 && Time.realtimeSinceStartup - lastTimerMessage > 1f)
        {
            onLevelNumberUpdated?.Invoke(countTimerMessages);
            lastTimerMessage = Time.realtimeSinceStartup;
            countTimerMessages--;
            if (arenaMessage == EArenaMessage.WARMUP && !Dedicator.IsDedicatedServer && MainCamera.instance != null && OptionsSettings.timer)
            {
                MainCamera.instance.GetComponent<AudioSource>().PlayOneShot(timer, 1f);
            }
        }
        if (Provider.isServer)
        {
            switch (arenaState)
            {
            case EArenaState.LOBBY:
                arenaLobby();
                break;
            case EArenaState.CLEAR:
                arenaClear();
                break;
            case EArenaState.WARMUP:
                arenaWarmUp();
                break;
            case EArenaState.SPAWN:
                arenaSpawn();
                break;
            case EArenaState.PLAY:
                arenaPlay();
                break;
            case EArenaState.FINALE:
                arenaFinale();
                break;
            case EArenaState.RESTART:
                arenaRestart();
                break;
            case EArenaState.INTERMISSION:
                arenaIntermission();
                break;
            }
        }
    }

    private void arenaInit()
    {
        _arenaCurrentCenter = Vector3.zero;
        _arenaTargetCenter = Vector3.zero;
        _arenaCurrentRadius = 16384f;
        _arenaTargetRadius = 16384f;
        _arenaCompactorSpeed = 0f;
        if (!Dedicator.IsDedicatedServer && !Level.isEditor)
        {
            arenaCurrentArea = ((GameObject)UnityEngine.Object.Instantiate(Resources.Load("Level/Arena_Area_Current"))).transform;
            arenaCurrentArea.name = "Arena_Area_Current";
            arenaCurrentArea.parent = Level.clips;
            arenaTargetArea = ((GameObject)UnityEngine.Object.Instantiate(Resources.Load("Level/Arena_Area_Target"))).transform;
            arenaTargetArea.name = "Arena_Area_Target";
            arenaTargetArea.parent = Level.clips;
        }
        if (Provider.isServer)
        {
            arenaState = EArenaState.LOBBY;
            arenaGroups = new List<CSteamID>();
            arenaPlayers = new List<ArenaPlayer>();
        }
    }

    [Obsolete]
    public void tellArenaOrigin(CSteamID steamID, Vector3 newArenaCurrentCenter, float newArenaCurrentRadius, Vector3 newArenaOriginCenter, float newArenaOriginRadius, Vector3 newArenaTargetCenter, float newArenaTargetRadius, float newArenaCompactorSpeed, byte delay)
    {
        ReceiveArenaOrigin(newArenaCurrentCenter, newArenaCurrentRadius, newArenaOriginCenter, newArenaOriginRadius, newArenaTargetCenter, newArenaTargetRadius, newArenaCompactorSpeed, delay);
    }

    [SteamCall(ESteamCallValidation.ONLY_FROM_SERVER, legacyName = "tellArenaOrigin")]
    public static void ReceiveArenaOrigin(Vector3 newArenaCurrentCenter, float newArenaCurrentRadius, Vector3 newArenaOriginCenter, float newArenaOriginRadius, Vector3 newArenaTargetCenter, float newArenaTargetRadius, float newArenaCompactorSpeed, byte delay)
    {
        _arenaCurrentCenter = newArenaCurrentCenter;
        _arenaCurrentRadius = newArenaCurrentRadius;
        arenaSqrRadius = arenaCurrentRadius * arenaCurrentRadius;
        _arenaOriginCenter = newArenaOriginCenter;
        _arenaOriginRadius = newArenaOriginRadius;
        _arenaTargetCenter = newArenaTargetCenter;
        _arenaTargetRadius = newArenaTargetRadius;
        _arenaCompactorSpeed = newArenaCompactorSpeed;
        if (delay == 0)
        {
            nextAreaModify = 0f;
        }
        else
        {
            nextAreaModify = Time.realtimeSinceStartup + (float)(int)delay;
        }
    }

    [Obsolete]
    public void tellArenaMessage(CSteamID steamID, byte newArenaMessage)
    {
        ReceiveArenaMessage((EArenaMessage)newArenaMessage);
    }

    [SteamCall(ESteamCallValidation.ONLY_FROM_SERVER, legacyName = "tellArenaMessage")]
    public static void ReceiveArenaMessage(EArenaMessage newArenaMessage)
    {
        arenaMessage = newArenaMessage;
        onArenaMessageUpdated?.Invoke(arenaMessage);
    }

    [Obsolete]
    public void tellArenaPlayer(CSteamID steamID, ulong[] newPlayerIDs, byte newArenaMessage)
    {
        onArenaPlayerUpdated?.Invoke(newPlayerIDs, (EArenaMessage)newArenaMessage);
    }

    [SteamCall(ESteamCallValidation.ONLY_FROM_SERVER)]
    public static void ReceiveArenaPlayer(in ClientInvocationContext context)
    {
        NetPakReader reader = context.reader;
        reader.ReadUInt8(out var value);
        ulong[] array = new ulong[value];
        for (byte b = 0; b < value; b++)
        {
            reader.ReadUInt64(out array[b]);
        }
        reader.ReadEnum(out var value2);
        onArenaPlayerUpdated?.Invoke(array, value2);
    }

    private static void WriteArenaPlayer(NetPakWriter writer, ulong[] newPlayerIDs, EArenaMessage newArenaMessage)
    {
        byte b = (byte)newPlayerIDs.Length;
        writer.WriteUInt8(b);
        for (byte b2 = 0; b2 < b; b2++)
        {
            writer.WriteUInt64(newPlayerIDs[b2]);
        }
        writer.WriteEnum(newArenaMessage);
    }

    [Obsolete]
    public void tellLevelNumber(CSteamID steamID, byte newLevelNumber)
    {
        ReceiveLevelNumber(newLevelNumber);
    }

    [SteamCall(ESteamCallValidation.ONLY_FROM_SERVER, legacyName = "tellLevelNumber")]
    public static void ReceiveLevelNumber(byte newLevelNumber)
    {
        countTimerMessages = -1;
        onLevelNumberUpdated?.Invoke(newLevelNumber);
    }

    [Obsolete]
    public void tellLevelTimer(CSteamID steamID, byte newTimerCount)
    {
        ReceiveLevelTimer(newTimerCount);
    }

    [SteamCall(ESteamCallValidation.ONLY_FROM_SERVER, legacyName = "tellLevelTimer")]
    public static void ReceiveLevelTimer(byte newTimerCount)
    {
        countTimerMessages = newTimerCount;
    }

    [Obsolete]
    public void askArenaState(CSteamID steamID)
    {
    }

    public static void airdrop(Vector3 point, ushort id, float speed)
    {
        if (id != 0)
        {
            Vector3 zero = Vector3.zero;
            if (UnityEngine.Random.value < 0.5f)
            {
                zero.x = (float)(Level.size / 2) * (0f - Mathf.Sign(point.x));
                zero.z = (float)UnityEngine.Random.Range(0, Level.size / 2) * (0f - Mathf.Sign(point.z));
            }
            else
            {
                zero.x = (float)UnityEngine.Random.Range(0, Level.size / 2) * (0f - Mathf.Sign(point.x));
                zero.z = (float)(Level.size / 2) * (0f - Mathf.Sign(point.z));
            }
            float y = point.y + 450f;
            point.y = 0f;
            Vector3 normalized = (point - zero).normalized;
            zero += normalized * -2048f;
            float arg = (point - zero).magnitude / speed;
            zero.y = y;
            float airdrop_Force = Provider.modeConfigData.Events.Airdrop_Force;
            SendAirdropState.InvokeAndLoopback(ENetReliability.Reliable, Provider.GatherRemoteClientConnections(), id, zero, normalized, speed, airdrop_Force, arg);
        }
    }

    private void airdropTick()
    {
        for (int num = airdrops.Count - 1; num >= 0; num--)
        {
            AirdropInfo airdropInfo = airdrops[num];
            airdropInfo.state += airdropInfo.direction * airdropInfo.speed * Time.deltaTime;
            airdropInfo.delay -= Time.deltaTime;
            if (airdropInfo.model != null)
            {
                airdropInfo.model.position = airdropInfo.state;
            }
            if (airdropInfo.dropped)
            {
                if (Mathf.Abs(airdropInfo.state.x) > (float)(Level.size / 2 + 2048) || Mathf.Abs(airdropInfo.state.z) > (float)(Level.size / 2 + 2048))
                {
                    if (airdropInfo.model != null)
                    {
                        UnityEngine.Object.Destroy(airdropInfo.model.gameObject);
                    }
                    airdrops.RemoveAt(num);
                }
            }
            else if (airdropInfo.delay <= 0f)
            {
                airdropInfo.dropped = true;
                AssetReference<AirdropAsset> assetReference = Level.getAsset()?.airdropRef ?? AssetReference<AirdropAsset>.invalid;
                if (assetReference.isNull)
                {
                    assetReference = AirdropAsset.defaultAirdrop;
                }
                AirdropAsset airdropAsset = assetReference.Find();
                MasterBundleReference<GameObject> masterBundleReference = airdropAsset?.model ?? MasterBundleReference<GameObject>.invalid;
                if (masterBundleReference.isNull)
                {
                    masterBundleReference = new MasterBundleReference<GameObject>("core.masterbundle", "Level/Carepackage.prefab");
                }
                Transform obj = UnityEngine.Object.Instantiate(masterBundleReference.loadAsset(), airdropInfo.dropPosition, Quaternion.identity).transform;
                obj.name = "Carepackage";
                Carepackage orAddComponent = obj.GetOrAddComponent<Carepackage>();
                orAddComponent.id = airdropInfo.id;
                if (airdropAsset != null)
                {
                    orAddComponent.barricadeAsset = airdropAsset.barricadeRef.Find();
                }
                ConstantForce component = obj.GetComponent<ConstantForce>();
                if (component != null)
                {
                    component.force = new Vector3(0f, airdropInfo.force, 0f);
                }
                if (Dedicator.IsDedicatedServer)
                {
                    airdrops.RemoveAt(num);
                }
            }
        }
        if (!Provider.isServer || levelType != 0 || !Provider.modeConfigData.Events.Use_Airdrops || airdropNodes.Count <= 0)
        {
            return;
        }
        if (!hasAirdrop)
        {
            airdropFrequency = (uint)(UnityEngine.Random.Range(Provider.modeConfigData.Events.Airdrop_Frequency_Min, Provider.modeConfigData.Events.Airdrop_Frequency_Max) * (float)LightingManager.cycle);
            _hasAirdrop = true;
            lastAirdrop = Time.realtimeSinceStartup;
        }
        if (airdropFrequency != 0)
        {
            if (Time.realtimeSinceStartup - lastAirdrop > 1f)
            {
                airdropFrequency--;
                lastAirdrop = Time.realtimeSinceStartup;
            }
        }
        else
        {
            AirdropDevkitNode airdropDevkitNode = airdropNodes[UnityEngine.Random.Range(0, airdropNodes.Count)];
            airdrop(airdropDevkitNode.transform.position, airdropDevkitNode.id, Provider.modeConfigData.Events.Airdrop_Speed);
            _hasAirdrop = false;
        }
    }

    private void airdropInit()
    {
        lastAirdrop = Time.realtimeSinceStartup;
        airdrops = new List<AirdropInfo>();
        if (!Provider.isServer)
        {
            return;
        }
        airdropNodes = new List<AirdropDevkitNode>();
        foreach (AirdropDevkitNode allNode in AirdropDevkitNodeSystem.Get().GetAllNodes())
        {
            if (allNode.id != 0)
            {
                airdropNodes.Add(allNode);
            }
        }
        load();
    }

    private void airdropSpawn(ushort id, Vector3 state, Vector3 direction, float speed, float force, float delay)
    {
        AirdropInfo airdropInfo = new AirdropInfo();
        airdropInfo.id = id;
        airdropInfo.state = state;
        airdropInfo.direction = direction;
        airdropInfo.speed = speed;
        airdropInfo.force = force;
        airdropInfo.delay = delay;
        airdropInfo.dropped = false;
        airdropInfo.dropPosition = state + direction * speed * delay;
        if (!Dedicator.IsDedicatedServer)
        {
            MasterBundleReference<GameObject> masterBundleReference = Level.getAsset()?.dropshipPrefab ?? default(MasterBundleReference<GameObject>);
            if (masterBundleReference.isNull)
            {
                masterBundleReference = new MasterBundleReference<GameObject>("core.masterbundle", "Level/Dropship.prefab");
            }
            Quaternion rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(-90f, 180f, 0f);
            Transform transform = UnityEngine.Object.Instantiate(masterBundleReference.loadAsset(), state, rotation).transform;
            transform.name = "Dropship";
            airdropInfo.model = transform;
        }
        airdrops.Add(airdropInfo);
    }

    [Obsolete]
    public void tellAirdropState(CSteamID steamID, Vector3 state, Vector3 direction, float speed, float force, float delay)
    {
        ReceiveAirdropState(0, state, direction, speed, force, delay);
    }

    [SteamCall(ESteamCallValidation.ONLY_FROM_SERVER, legacyName = "tellAirdropState")]
    public static void ReceiveAirdropState(ushort id, Vector3 state, Vector3 direction, float speed, float force, float delay)
    {
        manager.airdropSpawn(id, state, direction, speed, force, delay);
    }

    [Obsolete]
    public void askAirdropState(CSteamID steamID)
    {
    }

    internal static void SendInitialGlobalState(SteamPlayer client)
    {
        if (Level.info.type == ELevelType.ARENA)
        {
            SendArenaOrigin.Invoke(ENetReliability.Reliable, client.transportConnection, arenaCurrentCenter, arenaCurrentRadius, arenaOriginCenter, arenaOriginRadius, arenaTargetCenter, arenaTargetRadius, arenaCompactorSpeed, 0);
            SendArenaMessage.Invoke(ENetReliability.Reliable, client.transportConnection, arenaMessage);
            if (countTimerMessages > 0)
            {
                SendLevelTimer.Invoke(ENetReliability.Reliable, client.transportConnection, (byte)countTimerMessages);
            }
            else
            {
                SendLevelNumber.Invoke(ENetReliability.Reliable, client.transportConnection, (byte)arenaPlayers.Count);
            }
        }
        for (int i = 0; i < airdrops.Count; i++)
        {
            AirdropInfo airdropInfo = airdrops[i];
            SendAirdropState.Invoke(ENetReliability.Reliable, client.transportConnection, airdropInfo.id, airdropInfo.state, airdropInfo.direction, airdropInfo.speed, airdropInfo.force, airdropInfo.delay);
        }
    }

    private void onLevelLoaded(int level)
    {
        isInit = false;
        if (level > Level.BUILD_INDEX_SETUP && Level.info != null)
        {
            isInit = true;
            _levelType = Level.info.type;
            if (levelType == ELevelType.ARENA)
            {
                arenaInit();
            }
            if (levelType != ELevelType.HORDE)
            {
                airdropInit();
            }
        }
    }

    private void Update()
    {
        if (isInit)
        {
            if (levelType == ELevelType.ARENA)
            {
                arenaTick();
            }
            if (levelType != ELevelType.HORDE)
            {
                airdropTick();
            }
        }
    }

    private void Start()
    {
        manager = this;
        if (!Dedicator.IsDedicatedServer)
        {
            timer = (AudioClip)Resources.Load("Sounds/General/Timer");
        }
        Level.onLevelLoaded = (LevelLoaded)Delegate.Combine(Level.onLevelLoaded, new LevelLoaded(onLevelLoaded));
    }

    public static void load()
    {
        bool flag = true;
        if (LevelSavedata.fileExists("/Events.dat"))
        {
            River river = LevelSavedata.openRiver("/Events.dat", isReading: true);
            if (river.readByte() > 0)
            {
                airdropFrequency = river.readUInt32();
                _hasAirdrop = river.readBoolean();
                flag = false;
            }
            river.closeRiver();
        }
        if (flag)
        {
            _hasAirdrop = false;
        }
    }

    public static void save()
    {
        River river = LevelSavedata.openRiver("/Events.dat", isReading: false);
        river.writeByte(SAVEDATA_VERSION);
        river.writeUInt32(airdropFrequency);
        river.writeBoolean(hasAirdrop);
        river.closeRiver();
    }
}
