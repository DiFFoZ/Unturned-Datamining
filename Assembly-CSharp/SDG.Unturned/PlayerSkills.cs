using System;
using System.Collections.Generic;
using SDG.NetPak;
using SDG.NetTransport;
using Steamworks;
using UnityEngine;

namespace SDG.Unturned;

public class PlayerSkills : PlayerCaller
{
    public static readonly SpecialitySkillPair[][] SKILLSETS = new SpecialitySkillPair[11][]
    {
        new SpecialitySkillPair[0],
        new SpecialitySkillPair[2]
        {
            new SpecialitySkillPair(0, 3),
            new SpecialitySkillPair(1, 4)
        },
        new SpecialitySkillPair[2]
        {
            new SpecialitySkillPair(0, 4),
            new SpecialitySkillPair(1, 3)
        },
        new SpecialitySkillPair[2]
        {
            new SpecialitySkillPair(0, 1),
            new SpecialitySkillPair(0, 2)
        },
        new SpecialitySkillPair[2]
        {
            new SpecialitySkillPair(2, 5),
            new SpecialitySkillPair(1, 6)
        },
        new SpecialitySkillPair[2]
        {
            new SpecialitySkillPair(2, 4),
            new SpecialitySkillPair(0, 5)
        },
        new SpecialitySkillPair[2]
        {
            new SpecialitySkillPair(1, 5),
            new SpecialitySkillPair(2, 2)
        },
        new SpecialitySkillPair[3]
        {
            new SpecialitySkillPair(2, 1),
            new SpecialitySkillPair(2, 7),
            new SpecialitySkillPair(2, 6)
        },
        new SpecialitySkillPair[2]
        {
            new SpecialitySkillPair(2, 3),
            new SpecialitySkillPair(1, 1)
        },
        new SpecialitySkillPair[2]
        {
            new SpecialitySkillPair(0, 6),
            new SpecialitySkillPair(1, 0)
        },
        new SpecialitySkillPair[2]
        {
            new SpecialitySkillPair(1, 2),
            new SpecialitySkillPair(2, 0)
        }
    };

    public static readonly byte SAVEDATA_VERSION = 7;

    public static readonly byte SPECIALITIES = 3;

    public static readonly byte BOOST_COUNT = 4;

    public static readonly uint BOOST_COST = 25u;

    public ExperienceUpdated onExperienceUpdated;

    public ReputationUpdated onReputationUpdated;

    public BoostUpdated onBoostUpdated;

    public SkillsUpdated onSkillsUpdated;

    private Skill[][] _skills;

    private EPlayerBoost _boost;

    private uint _experience;

    private int _reputation;

    private bool wasLoaded;

    private static readonly ClientInstanceMethod<uint> SendExperience = ClientInstanceMethod<uint>.Get(typeof(PlayerSkills), "ReceiveExperience");

    private static readonly ClientInstanceMethod<int> SendReputation = ClientInstanceMethod<int>.Get(typeof(PlayerSkills), "ReceiveReputation");

    private static readonly ClientInstanceMethod<EPlayerBoost> SendBoost = ClientInstanceMethod<EPlayerBoost>.Get(typeof(PlayerSkills), "ReceiveBoost");

    private static readonly ClientInstanceMethod<byte, byte, byte> SendSingleSkillLevel = ClientInstanceMethod<byte, byte, byte>.Get(typeof(PlayerSkills), "ReceiveSingleSkillLevel");

    private static readonly ServerInstanceMethod<byte, byte, bool> SendUpgradeRequest = ServerInstanceMethod<byte, byte, bool>.Get(typeof(PlayerSkills), "ReceiveUpgradeRequest");

    private static readonly ServerInstanceMethod SendBoostRequest = ServerInstanceMethod.Get(typeof(PlayerSkills), "ReceiveBoostRequest");

    private static readonly ServerInstanceMethod<NetId> SendPurchaseRequest = ServerInstanceMethod<NetId>.Get(typeof(PlayerSkills), "ReceivePurchaseRequest");

    private static readonly ClientInstanceMethod SendMultipleSkillLevels = ClientInstanceMethod.Get(typeof(PlayerSkills), "ReceiveMultipleSkillLevels");

    private bool wasLoadCalled;

    private static List<Tuple<int, int>> availableSkillsToLoseLevels = new List<Tuple<int, int>>();

    public Skill[][] skills => _skills;

    public EPlayerBoost boost => _boost;

    public uint experience => _experience;

    public int reputation => _reputation;

    public bool doesLevelAllowSkills
    {
        get
        {
            if (Level.info != null && Level.info.configData != null)
            {
                return Level.info.configData.Allow_Skills;
            }
            return true;
        }
    }

    public static event ApplyingDefaultSkillsHandler onApplyingDefaultSkills;

    /// <summary>
    /// Invoked after any player's experience value changes (not including loading).
    /// </summary>
    public static event Action<PlayerSkills, uint> OnExperienceChanged_Global;

    /// <summary>
    /// Invoked after any player's reputation value changes (not including loading).
    /// </summary>
    public static event Action<PlayerSkills, int> OnReputationChanged_Global;

    public static event Action<PlayerSkills, byte, byte, byte> OnSkillUpgraded_Global;

    public float GetSharpshooterRecoilMultiplierForLevel(int level)
    {
        Skill skill = skills[0][1];
        return 1f - skill.NormalizeLevel(level) * 0.4f;
    }

    public float GetSharpshooterRecoilMultiplier()
    {
        Skill skill = skills[0][1];
        return GetSharpshooterRecoilMultiplierForLevel(skill.level);
    }

    /// <summary>
    /// Ugly hack for the awful skills enums. Eventually skills should be replaced.
    /// </summary>
    public static bool TryParseIndices(string input, out int specialityIndex, out int skillIndex)
    {
        if (Enum.TryParse<EPlayerOffense>(input, ignoreCase: true, out var result))
        {
            specialityIndex = 0;
            skillIndex = (int)result;
            return true;
        }
        if (Enum.TryParse<EPlayerDefense>(input, ignoreCase: true, out var result2))
        {
            specialityIndex = 1;
            skillIndex = (int)result2;
            return true;
        }
        if (Enum.TryParse<EPlayerSupport>(input, ignoreCase: true, out var result3))
        {
            specialityIndex = 2;
            skillIndex = (int)result3;
            return true;
        }
        specialityIndex = -1;
        skillIndex = -1;
        return false;
    }

    [Obsolete]
    public void tellExperience(CSteamID steamID, uint newExperience)
    {
        ReceiveExperience(newExperience);
    }

    [SteamCall(ESteamCallValidation.ONLY_FROM_SERVER, legacyName = "tellExperience")]
    public void ReceiveExperience(uint newExperience)
    {
        uint arg = _experience;
        if (base.channel.IsLocalPlayer && newExperience > experience && Level.info.type != ELevelType.HORDE && wasLoaded)
        {
            if (Provider.provider.statisticsService.userStatisticsService.getStatistic("Found_Experience", out int data))
            {
                Provider.provider.statisticsService.userStatisticsService.setStatistic("Found_Experience", data + (int)(newExperience - experience));
            }
            PlayerUI.message(EPlayerMessage.EXPERIENCE, (newExperience - experience).ToString());
        }
        _experience = newExperience;
        onExperienceUpdated?.Invoke(experience);
        PlayerSkills.OnExperienceChanged_Global?.Invoke(this, arg);
    }

    [Obsolete]
    public void tellReputation(CSteamID steamID, int newReputation)
    {
        ReceiveReputation(newReputation);
    }

    [SteamCall(ESteamCallValidation.ONLY_FROM_SERVER, legacyName = "tellReputation")]
    public void ReceiveReputation(int newReputation)
    {
        int arg = _reputation;
        if (base.channel.IsLocalPlayer && newReputation != reputation && Level.info.type != ELevelType.HORDE && wasLoaded)
        {
            bool has2;
            if (newReputation <= -200)
            {
                if (Provider.provider.achievementsService.getAchievement("Villain", out var has) && !has)
                {
                    Provider.provider.achievementsService.setAchievement("Villain");
                }
            }
            else if (newReputation >= 200 && Provider.provider.achievementsService.getAchievement("Paragon", out has2) && !has2)
            {
                Provider.provider.achievementsService.setAchievement("Paragon");
            }
            if (base.player.isPluginWidgetFlagActive(EPluginWidgetFlags.ShowReputationChangeNotification))
            {
                string text = (newReputation - reputation).ToString();
                if (newReputation > reputation)
                {
                    text = "+" + text;
                }
                PlayerUI.message(EPlayerMessage.REPUTATION, text);
            }
        }
        _reputation = newReputation;
        onReputationUpdated?.Invoke(reputation);
        PlayerSkills.OnReputationChanged_Global?.Invoke(this, arg);
    }

    [Obsolete]
    public void tellBoost(CSteamID steamID, byte newBoost)
    {
        ReceiveBoost((EPlayerBoost)newBoost);
    }

    [SteamCall(ESteamCallValidation.ONLY_FROM_SERVER, legacyName = "tellBoost")]
    public void ReceiveBoost(EPlayerBoost newBoost)
    {
        _boost = newBoost;
        onBoostUpdated?.Invoke(boost);
        wasLoaded = true;
    }

    [Obsolete]
    public void tellSkill(CSteamID steamID, byte speciality, byte index, byte level)
    {
        ReceiveSingleSkillLevel(speciality, index, level);
    }

    [SteamCall(ESteamCallValidation.ONLY_FROM_SERVER, legacyName = "tellSkill")]
    public void ReceiveSingleSkillLevel(byte speciality, byte index, byte level)
    {
        if (index >= skills[speciality].Length)
        {
            return;
        }
        skills[speciality][index].level = level;
        if (base.channel.IsLocalPlayer)
        {
            bool flag = true;
            bool flag2 = true;
            bool flag3 = true;
            for (int i = 0; i < skills[0].Length; i++)
            {
                if (skills[0][i].level < skills[0][i].max)
                {
                    flag = false;
                    break;
                }
            }
            for (int j = 0; j < skills[1].Length; j++)
            {
                if (skills[1][j].level < skills[1][j].max)
                {
                    flag2 = false;
                    break;
                }
            }
            for (int k = 0; k < skills[2].Length; k++)
            {
                if (skills[2][k].level < skills[2][k].max)
                {
                    flag3 = false;
                    break;
                }
            }
            if (flag && Provider.provider.achievementsService.getAchievement("Offense", out var has) && !has)
            {
                Provider.provider.achievementsService.setAchievement("Offense");
            }
            if (flag2 && Provider.provider.achievementsService.getAchievement("Defense", out var has2) && !has2)
            {
                Provider.provider.achievementsService.setAchievement("Defense");
            }
            if (flag3 && Provider.provider.achievementsService.getAchievement("Support", out var has3) && !has3)
            {
                Provider.provider.achievementsService.setAchievement("Support");
            }
            if (flag && flag2 && flag3 && Provider.provider.achievementsService.getAchievement("Mastermind", out var has4) && !has4)
            {
                Provider.provider.achievementsService.setAchievement("Mastermind");
            }
        }
        onSkillsUpdated?.Invoke();
    }

    public float mastery(int speciality, int index)
    {
        return skills[speciality][index].mastery;
    }

    public uint cost(int speciality, int index)
    {
        if (Level.info != null && Level.info.type != ELevelType.ARENA)
        {
            for (byte b = 0; b < SKILLSETS[(byte)base.channel.owner.skillset].Length; b++)
            {
                SpecialitySkillPair specialitySkillPair = SKILLSETS[(byte)base.channel.owner.skillset][b];
                if (speciality == specialitySkillPair.speciality && index == specialitySkillPair.skill)
                {
                    return skills[speciality][index].cost / 2;
                }
            }
        }
        return skills[speciality][index].cost;
    }

    public void askSpend(uint cost)
    {
        if (base.channel.IsLocalPlayer)
        {
            SendExperience.Invoke(GetNetId(), ENetReliability.Reliable, base.channel.GetOwnerTransportConnection(), experience - cost);
            return;
        }
        uint arg = _experience;
        _experience -= cost;
        SendExperience.Invoke(GetNetId(), ENetReliability.Reliable, base.channel.GetOwnerTransportConnection(), experience);
        PlayerSkills.OnExperienceChanged_Global?.Invoke(this, arg);
    }

    public void askAward(uint award)
    {
        if (base.channel.IsLocalPlayer)
        {
            SendExperience.Invoke(GetNetId(), ENetReliability.Reliable, base.channel.GetOwnerTransportConnection(), experience + award);
            return;
        }
        uint arg = _experience;
        _experience += award;
        SendExperience.Invoke(GetNetId(), ENetReliability.Reliable, base.channel.GetOwnerTransportConnection(), experience);
        PlayerSkills.OnExperienceChanged_Global?.Invoke(this, arg);
    }

    public void ServerSetExperience(uint newExperience)
    {
        if (newExperience > _experience)
        {
            askAward(newExperience - _experience);
        }
        else if (newExperience < _experience)
        {
            askSpend(_experience - newExperience);
        }
    }

    public void ServerModifyExperience(int delta)
    {
        if (delta > 0)
        {
            askAward((uint)delta);
        }
        else if (delta < 0)
        {
            uint a = (uint)(-delta);
            a = MathfEx.Min(a, _experience);
            askSpend(a);
        }
    }

    public void askRep(int rep)
    {
        SendReputation.InvokeAndLoopback(GetNetId(), ENetReliability.Reliable, Provider.GatherRemoteClientConnections(), reputation + rep);
    }

    public void askPay(uint pay)
    {
        if (pay != 0)
        {
            pay = (uint)((float)pay * Provider.modeConfigData.Players.Experience_Multiplier);
            if (base.channel.IsLocalPlayer)
            {
                SendExperience.Invoke(GetNetId(), ENetReliability.Reliable, base.channel.GetOwnerTransportConnection(), experience + pay);
                return;
            }
            uint arg = _experience;
            _experience += pay;
            SendExperience.Invoke(GetNetId(), ENetReliability.Reliable, base.channel.GetOwnerTransportConnection(), experience);
            PlayerSkills.OnExperienceChanged_Global?.Invoke(this, arg);
        }
    }

    public void modRep(int rep)
    {
        int arg = _reputation;
        _reputation += rep;
        onReputationUpdated?.Invoke(reputation);
        PlayerSkills.OnReputationChanged_Global?.Invoke(this, arg);
    }

    public void modXp(uint xp)
    {
        uint arg = _experience;
        _experience += xp;
        onExperienceUpdated?.Invoke(experience);
        PlayerSkills.OnExperienceChanged_Global?.Invoke(this, arg);
    }

    public void modXp2(uint xp)
    {
        uint arg = _experience;
        _experience -= xp;
        onExperienceUpdated?.Invoke(experience);
        PlayerSkills.OnExperienceChanged_Global?.Invoke(this, arg);
    }

    [Obsolete]
    public void askUpgrade(CSteamID steamID, byte speciality, byte index, bool force)
    {
        ReceiveUpgradeRequest(speciality, index, force);
    }

    [SteamCall(ESteamCallValidation.ONLY_FROM_OWNER, ratelimitHz = 10, legacyName = "askUpgrade")]
    public void ReceiveUpgradeRequest(byte speciality, byte index, bool force)
    {
        if (!doesLevelAllowSkills || speciality >= SPECIALITIES || index >= skills[speciality].Length)
        {
            return;
        }
        Skill skill = skills[speciality][index];
        byte level = skill.level;
        uint num = _experience;
        while (experience >= cost(speciality, index) && skill.level < skill.GetClampedMaxUnlockableLevel())
        {
            _experience -= cost(speciality, index);
            skill.level++;
            if (!force)
            {
                break;
            }
        }
        if (skill.level > level)
        {
            SendExperience.Invoke(GetNetId(), ENetReliability.Reliable, base.channel.GetOwnerTransportConnection(), experience);
            SendSingleSkillLevel.InvokeAndLoopback(GetNetId(), ENetReliability.Reliable, Provider.GatherRemoteClientConnections(), speciality, index, skill.level);
            if (_experience != num && PlayerSkills.OnExperienceChanged_Global != null)
            {
                PlayerSkills.OnExperienceChanged_Global(this, num);
            }
            PlayerSkills.OnSkillUpgraded_Global.TryInvoke("OnSkillUpgraded_Global", this, speciality, index, level);
        }
    }

    public bool ServerSetSkillLevel(int specialityIndex, int skillIndex, int newLevel)
    {
        if (specialityIndex >= skills.Length)
        {
            throw new ArgumentOutOfRangeException("specialityIndex");
        }
        if (skillIndex >= skills[specialityIndex].Length)
        {
            throw new ArgumentOutOfRangeException("skillIndex");
        }
        Skill skill = skills[specialityIndex][skillIndex];
        if (newLevel > skill.max)
        {
            throw new ArgumentOutOfRangeException("newLevel");
        }
        if (skill.level != newLevel)
        {
            skill.level = (byte)newLevel;
            SendSingleSkillLevel.InvokeAndLoopback(GetNetId(), ENetReliability.Reliable, Provider.GatherRemoteClientConnections(), (byte)specialityIndex, (byte)skillIndex, skill.level);
            return true;
        }
        return false;
    }

    [Obsolete]
    public void askBoost(CSteamID steamID)
    {
        ReceiveBoostRequest();
    }

    [SteamCall(ESteamCallValidation.ONLY_FROM_OWNER, ratelimitHz = 10, legacyName = "askBoost")]
    public void ReceiveBoostRequest()
    {
        if (doesLevelAllowSkills && experience >= BOOST_COST)
        {
            uint arg = _experience;
            _experience -= BOOST_COST;
            byte b;
            do
            {
                b = (byte)UnityEngine.Random.Range(1, BOOST_COUNT + 1);
            }
            while (b == (byte)boost);
            _boost = (EPlayerBoost)b;
            SendExperience.Invoke(GetNetId(), ENetReliability.Reliable, base.channel.GetOwnerTransportConnection(), experience);
            SendBoost.InvokeAndLoopback(GetNetId(), ENetReliability.Reliable, Provider.GatherRemoteClientConnections(), boost);
            PlayerSkills.OnExperienceChanged_Global?.Invoke(this, arg);
        }
    }

    [Obsolete]
    public void askPurchase(CSteamID steamID, byte index)
    {
    }

    [SteamCall(ESteamCallValidation.ONLY_FROM_OWNER, ratelimitHz = 10, legacyName = "askPurchase")]
    public void ReceivePurchaseRequest(NetId volumeNetId)
    {
        HordePurchaseVolume hordePurchaseVolume = NetIdRegistry.Get<HordePurchaseVolume>(volumeNetId);
        if (!(hordePurchaseVolume == null) && experience >= hordePurchaseVolume.cost)
        {
            uint arg = _experience;
            _experience -= hordePurchaseVolume.cost;
            SendExperience.Invoke(GetNetId(), ENetReliability.Reliable, base.channel.GetOwnerTransportConnection(), experience);
            ItemAsset itemAsset = Assets.find(EAssetType.ITEM, hordePurchaseVolume.id) as ItemAsset;
            if (itemAsset.type == EItemType.GUN && base.player.inventory.has(hordePurchaseVolume.id) != null)
            {
                base.player.inventory.tryAddItem(new Item(((ItemGunAsset)itemAsset).getMagazineID(), EItemOrigin.ADMIN), auto: true);
            }
            else
            {
                base.player.inventory.tryAddItem(new Item(hordePurchaseVolume.id, EItemOrigin.ADMIN), auto: true);
            }
            PlayerSkills.OnExperienceChanged_Global?.Invoke(this, arg);
        }
    }

    public void sendUpgrade(byte speciality, byte index, bool force)
    {
        SendUpgradeRequest.Invoke(GetNetId(), ENetReliability.Unreliable, speciality, index, force);
    }

    public void sendBoost()
    {
        SendBoostRequest.Invoke(GetNetId(), ENetReliability.Unreliable);
    }

    public void sendPurchase(HordePurchaseVolume node)
    {
        SendPurchaseRequest.Invoke(GetNetId(), ENetReliability.Unreliable, node.GetNetIdFromInstanceId());
    }

    [Obsolete]
    public void tellSkills(CSteamID steamID, byte speciality, byte[] newLevels)
    {
    }

    [SteamCall(ESteamCallValidation.ONLY_FROM_SERVER)]
    public void ReceiveMultipleSkillLevels(in ClientInvocationContext context)
    {
        if (skills == null)
        {
            return;
        }
        NetPakReader reader = context.reader;
        for (int i = 0; i < skills.Length; i++)
        {
            Skill[] array = skills[i];
            for (int j = 0; j < array.Length; j++)
            {
                reader.ReadUInt8(out array[j].level);
            }
        }
        onSkillsUpdated?.Invoke();
    }

    private void WriteSkillLevels(NetPakWriter writer)
    {
        for (int i = 0; i < skills.Length; i++)
        {
            Skill[] array = skills[i];
            for (int j = 0; j < array.Length; j++)
            {
                writer.WriteUInt8(array[j].level);
            }
        }
    }

    [Obsolete]
    public void askSkills(CSteamID steamID)
    {
    }

    internal void SendInitialPlayerState(SteamPlayer client)
    {
        SendMultipleSkillLevels.Invoke(GetNetId(), ENetReliability.Reliable, client.transportConnection, WriteSkillLevels);
        SendExperience.Invoke(GetNetId(), ENetReliability.Reliable, client.transportConnection, experience);
        SendReputation.Invoke(GetNetId(), ENetReliability.Reliable, client.transportConnection, reputation);
        SendBoost.Invoke(GetNetId(), ENetReliability.Reliable, client.transportConnection, boost);
    }

    internal void SendInitialPlayerState(List<ITransportConnection> transportConnections)
    {
        SendMultipleSkillLevels.Invoke(GetNetId(), ENetReliability.Reliable, transportConnections, WriteSkillLevels);
        SendExperience.Invoke(GetNetId(), ENetReliability.Reliable, transportConnections, experience);
        SendReputation.Invoke(GetNetId(), ENetReliability.Reliable, transportConnections, reputation);
        SendBoost.Invoke(GetNetId(), ENetReliability.Reliable, transportConnections, boost);
    }

    /// <summary>
    /// Set every level to max and replicate.
    /// </summary>
    public void ServerUnlockAllSkills()
    {
        Skill[][] array = skills;
        foreach (Skill[] array2 in array)
        {
            for (int j = 0; j < array2.Length; j++)
            {
                array2[j].setLevelToMax();
            }
        }
        SendMultipleSkillLevels.InvokeAndLoopback(GetNetId(), ENetReliability.Reliable, Provider.GatherRemoteClientConnections(), WriteSkillLevels);
    }

    private void onLifeUpdated(bool isDead)
    {
        if (!isDead || !Provider.isServer)
        {
            return;
        }
        if (Level.info == null || Level.info.type == ELevelType.SURVIVAL)
        {
            bool modifiedAnySkillLevels = false;
            float num = (base.player.life.wasPvPDeath ? Provider.modeConfigData.Players.Lose_Skills_PvP : Provider.modeConfigData.Players.Lose_Skills_PvE);
            if (num < 0.999f)
            {
                for (int i = 0; i < skills.Length; i++)
                {
                    Skill[] array = skills[i];
                    for (int j = 0; j < array.Length; j++)
                    {
                        if (CanDecreaseLevelOfSkill(i, j))
                        {
                            byte b = (byte)((float)(int)array[j].level * num);
                            modifiedAnySkillLevels |= array[j].level != b;
                            array[j].level = b;
                        }
                    }
                }
            }
            uint num2 = (base.player.life.wasPvPDeath ? Provider.modeConfigData.Players.Lose_Skill_Levels_PvP : Provider.modeConfigData.Players.Lose_Skill_Levels_PvE);
            if (num2 != 0)
            {
                LoseNumberOfSkills(num2, ref modifiedAnySkillLevels);
            }
            if (modifiedAnySkillLevels)
            {
                SendMultipleSkillLevels.InvokeAndLoopback(GetNetId(), ENetReliability.Reliable, Provider.GatherRemoteClientConnections(), WriteSkillLevels);
            }
            float num3 = (base.player.life.wasPvPDeath ? Provider.modeConfigData.Players.Lose_Experience_PvP : Provider.modeConfigData.Players.Lose_Experience_PvE);
            _experience = (uint)((float)experience * num3);
        }
        else
        {
            for (byte b2 = 0; b2 < skills.Length; b2++)
            {
                for (byte b3 = 0; b3 < skills[b2].Length; b3++)
                {
                    skills[b2][b3].level = 0;
                }
            }
            applyDefaultSkills();
            SendMultipleSkillLevels.InvokeAndLoopback(GetNetId(), ENetReliability.Reliable, Provider.GatherRemoteClientConnections(), WriteSkillLevels);
            if (Level.info.type == ELevelType.ARENA)
            {
                _experience = 0u;
            }
            else
            {
                _experience = (uint)((float)experience * 0.75f);
            }
        }
        _boost = EPlayerBoost.NONE;
        SendExperience.InvokeAndLoopback(GetNetId(), ENetReliability.Reliable, Provider.GatherRemoteClientConnections(), experience);
        SendBoost.InvokeAndLoopback(GetNetId(), ENetReliability.Reliable, Provider.GatherRemoteClientConnections(), boost);
    }

    internal void InitializePlayer()
    {
        _skills = new Skill[SPECIALITIES][];
        skills[0] = new Skill[7];
        skills[0][0] = new Skill(0, 7, 10u, 1f);
        skills[0][1] = new Skill(0, 7, 10u, 1f);
        skills[0][2] = new Skill(0, 5, 10u, 0.5f);
        skills[0][3] = new Skill(0, 5, 10u, 0.5f);
        skills[0][4] = new Skill(0, 5, 10u, 0.5f);
        skills[0][5] = new Skill(0, 5, 10u, 0.5f);
        skills[0][6] = new Skill(0, 5, 20u, 0.5f);
        skills[1] = new Skill[7];
        skills[1][0] = new Skill(0, 7, 10u, 1f);
        skills[1][1] = new Skill(0, 5, 10u, 0.5f);
        skills[1][2] = new Skill(0, 5, 10u, 0.5f);
        skills[1][3] = new Skill(0, 5, 10u, 0.5f);
        skills[1][4] = new Skill(0, 5, 10u, 0.5f);
        skills[1][5] = new Skill(0, 5, 10u, 0.5f);
        skills[1][6] = new Skill(0, 5, 10u, 0.5f);
        skills[2] = new Skill[8];
        skills[2][0] = new Skill(0, 7, 10u, 1f);
        skills[2][1] = new Skill(0, 3, 20u, 1.5f);
        skills[2][2] = new Skill(0, 5, 10u, 0.5f);
        skills[2][3] = new Skill(0, 3, 20u, 1.5f);
        skills[2][4] = new Skill(0, 5, 10u, 0.5f);
        skills[2][5] = new Skill(0, 7, 10u, 1f);
        skills[2][6] = new Skill(0, 5, 10u, 0.5f);
        skills[2][7] = new Skill(0, 3, 20u, 1.5f);
        LevelAsset asset = Level.getAsset();
        if (asset != null && asset.skillRules != null)
        {
            for (int i = 0; i < skills.Length; i++)
            {
                for (int j = 0; j < skills[i].Length; j++)
                {
                    LevelAsset.SkillRule skillRule = asset.skillRules[i][j];
                    if (skillRule != null)
                    {
                        if (skillRule.maxUnlockableLevel > -1)
                        {
                            skills[i][j].maxUnlockableLevel = skillRule.maxUnlockableLevel;
                        }
                        skills[i][j].costMultiplier = skillRule.costMultiplier;
                    }
                }
            }
        }
        if (Provider.isServer)
        {
            load();
            PlayerLife life = base.player.life;
            life.onLifeUpdated = (LifeUpdated)Delegate.Combine(life.onLifeUpdated, new LifeUpdated(onLifeUpdated));
        }
        else
        {
            _experience = uint.MaxValue;
            _reputation = 0;
        }
    }

    public void load()
    {
        wasLoadCalled = true;
        if (PlayerSavedata.fileExists(base.channel.owner.playerID, "/Player/Skills.dat") && Level.info.type == ELevelType.SURVIVAL)
        {
            Block block = PlayerSavedata.readBlock(base.channel.owner.playerID, "/Player/Skills.dat", 0);
            byte b = block.readByte();
            if (b <= 4)
            {
                return;
            }
            _experience = block.readUInt32();
            if (b >= 7)
            {
                _reputation = block.readInt32();
            }
            else
            {
                _reputation = 0;
            }
            _boost = (EPlayerBoost)block.readByte();
            if (b < 6)
            {
                return;
            }
            for (byte b2 = 0; b2 < skills.Length; b2++)
            {
                if (skills[b2] != null)
                {
                    for (byte b3 = 0; b3 < skills[b2].Length; b3++)
                    {
                        skills[b2][b3].level = block.readByte();
                        if (skills[b2][b3].level > skills[b2][b3].max)
                        {
                            skills[b2][b3].level = skills[b2][b3].max;
                        }
                    }
                }
            }
        }
        else
        {
            applyDefaultSkills();
        }
    }

    public void save()
    {
        if (!wasLoadCalled)
        {
            return;
        }
        Block block = new Block();
        block.writeByte(SAVEDATA_VERSION);
        block.writeUInt32(experience);
        block.writeInt32(reputation);
        block.writeByte((byte)boost);
        for (byte b = 0; b < skills.Length; b++)
        {
            if (skills[b] != null)
            {
                for (byte b2 = 0; b2 < skills[b].Length; b2++)
                {
                    block.writeByte(skills[b][b2].level);
                }
            }
        }
        PlayerSavedata.writeBlock(base.channel.owner.playerID, "/Player/Skills.dat", block);
    }

    /// <summary>
    /// Serverside only.
    /// Called when skills weren't loaded (no save, or in arena mode), as well as when reseting skills after death.
    /// </summary>
    private void applyDefaultSkills()
    {
        if (Provider.modeConfigData.Players.Spawn_With_Max_Skills)
        {
            for (byte b = 0; b < skills.Length; b++)
            {
                Skill[] array = skills[b];
                for (byte b2 = 0; b2 < array.Length; b2++)
                {
                    array[b2].setLevelToMax();
                }
            }
        }
        else
        {
            LevelAsset asset = Level.getAsset();
            if (asset != null && asset.skillRules != null)
            {
                for (int i = 0; i < skills.Length; i++)
                {
                    for (int j = 0; j < skills[i].Length; j++)
                    {
                        LevelAsset.SkillRule skillRule = asset.skillRules[i][j];
                        if (skillRule != null)
                        {
                            skills[i][j].level = (byte)skillRule.defaultLevel;
                        }
                    }
                }
            }
            if (Provider.modeConfigData.Players.Spawn_With_Stamina_Skills)
            {
                skills[0][3].setLevelToMax();
                skills[0][5].setLevelToMax();
                skills[0][4].setLevelToMax();
                skills[0][6].setLevelToMax();
            }
        }
        PlayerSkills.onApplyingDefaultSkills?.Invoke(base.player, skills);
    }

    private bool CanDecreaseLevelOfSkill(int specialityIndex, int skillIndex)
    {
        int skillset = (int)base.channel.owner.skillset;
        for (int i = 0; i < SKILLSETS[skillset].Length; i++)
        {
            SpecialitySkillPair specialitySkillPair = SKILLSETS[skillset][i];
            if (specialityIndex == specialitySkillPair.speciality && skillIndex == specialitySkillPair.skill)
            {
                return false;
            }
        }
        return true;
    }

    private void LoseNumberOfSkills(uint numberOfSkillsToLose, ref bool modifiedAnySkillLevels)
    {
        availableSkillsToLoseLevels.Clear();
        for (int i = 0; i < skills.Length; i++)
        {
            Skill[] array = skills[i];
            for (int j = 0; j < array.Length; j++)
            {
                if (CanDecreaseLevelOfSkill(i, j) && array[j].level > 0)
                {
                    availableSkillsToLoseLevels.Add(new Tuple<int, int>(i, j));
                }
            }
        }
        while (numberOfSkillsToLose != 0 && availableSkillsToLoseLevels.Count > 0)
        {
            int randomIndex = availableSkillsToLoseLevels.GetRandomIndex();
            Tuple<int, int> tuple = availableSkillsToLoseLevels[randomIndex];
            availableSkillsToLoseLevels.RemoveAtFast(randomIndex);
            skills[tuple.Item1][tuple.Item2].level--;
            modifiedAnySkillLevels = true;
            numberOfSkillsToLose--;
        }
    }
}
