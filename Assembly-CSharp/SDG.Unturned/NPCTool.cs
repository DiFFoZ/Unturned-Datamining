using System;
using System.Collections.Generic;
using UnityEngine;

namespace SDG.Unturned;

public class NPCTool
{
    /// <summary>
    /// Was redirected to HolidayUtil but kept for plugin backwards compatibility.
    /// Refer to HolidayUtil for explanation of this weird situation.
    /// </summary>
    public static ENPCHoliday getActiveHoliday()
    {
        return Provider.authorityHoliday;
    }

    /// <summary>
    /// Was redirected to HolidayUtil but kept for plugin backwards compatibility.
    /// Refer to HolidayUtil for explanation of this weird situation.
    /// </summary>
    public static bool isHolidayActive(ENPCHoliday holiday)
    {
        return holiday == Provider.authorityHoliday;
    }

    public static bool doesLogicPass<T>(ENPCLogicType logicType, T a, T b) where T : IComparable
    {
        int num = a.CompareTo(b);
        return logicType switch
        {
            ENPCLogicType.LESS_THAN => num < 0, 
            ENPCLogicType.LESS_THAN_OR_EQUAL_TO => num <= 0, 
            ENPCLogicType.EQUAL => num == 0, 
            ENPCLogicType.NOT_EQUAL => num != 0, 
            ENPCLogicType.GREATER_THAN_OR_EQUAL_TO => num >= 0, 
            ENPCLogicType.GREATER_THAN => num > 0, 
            _ => false, 
        };
    }

    public static void readConditions(DatDictionary data, Local localization, string prefix, INPCCondition[] conditions, Asset assetContext)
    {
        for (int i = 0; i < conditions.Length; i++)
        {
            string text = prefix + i + "_Type";
            if (!data.ContainsKey(text))
            {
                throw new NotSupportedException("Missing condition " + text);
            }
            ENPCConditionType eNPCConditionType = data.ParseEnum(text, ENPCConditionType.NONE);
            if (eNPCConditionType == ENPCConditionType.NONE)
            {
                Assets.ReportError(assetContext, "{0} unknown type", text);
                continue;
            }
            string desc = localization.read(prefix + i);
            desc = ItemTool.filterRarityRichText(desc);
            bool flag = data.ContainsKey(prefix + i + "_Reset");
            ENPCLogicType eNPCLogicType = data.ParseEnum(prefix + i + "_Logic", eNPCConditionType switch
            {
                ENPCConditionType.ITEM => ENPCLogicType.GREATER_THAN_OR_EQUAL_TO, 
                ENPCConditionType.HOLIDAY => ENPCLogicType.EQUAL, 
                _ => ENPCLogicType.NONE, 
            });
            INPCCondition iNPCCondition = null;
            switch (eNPCConditionType)
            {
            case ENPCConditionType.EXPERIENCE:
                if (!data.ContainsKey(prefix + i + "_Value"))
                {
                    Assets.ReportError(assetContext, "Experience condition " + prefix + i + " missing _Value");
                }
                iNPCCondition = new NPCExperienceCondition(data.ParseUInt32(prefix + i + "_Value"), eNPCLogicType, desc, flag);
                break;
            case ENPCConditionType.REPUTATION:
                if (!data.ContainsKey(prefix + i + "_Value"))
                {
                    Assets.ReportError(assetContext, "Reputation condition " + prefix + i + " missing _Value");
                }
                iNPCCondition = new NPCReputationCondition(data.ParseInt32(prefix + i + "_Value"), eNPCLogicType, desc);
                break;
            case ENPCConditionType.FLAG_BOOL:
                if (!data.ContainsKey(prefix + i + "_ID"))
                {
                    Assets.ReportError(assetContext, "Bool flag condition " + prefix + i + " missing _ID");
                }
                if (!data.ContainsKey(prefix + i + "_Value"))
                {
                    Assets.ReportError(assetContext, "Bool flag condition " + prefix + i + " missing _Value");
                }
                iNPCCondition = new NPCBoolFlagCondition(data.ParseUInt16(prefix + i + "_ID", 0), data.ParseBool(prefix + i + "_Value"), data.ContainsKey(prefix + i + "_Allow_Unset"), eNPCLogicType, desc, flag);
                break;
            case ENPCConditionType.FLAG_SHORT:
                if (!data.ContainsKey(prefix + i + "_ID"))
                {
                    Assets.ReportError(assetContext, "Short flag condition " + prefix + i + " missing _ID");
                }
                if (!data.ContainsKey(prefix + i + "_Value"))
                {
                    Assets.ReportError(assetContext, "Short flag condition " + prefix + i + " missing _Value");
                }
                iNPCCondition = new NPCShortFlagCondition(data.ParseUInt16(prefix + i + "_ID", 0), data.ParseInt16(prefix + i + "_Value", 0), data.ContainsKey(prefix + i + "_Allow_Unset"), eNPCLogicType, desc, flag);
                break;
            case ENPCConditionType.QUEST:
            {
                if (!data.ContainsKey(prefix + i + "_ID"))
                {
                    Assets.ReportError(assetContext, "Quest condition " + prefix + i + " missing _ID");
                }
                ENPCQuestStatus eNPCQuestStatus = ENPCQuestStatus.NONE;
                string key3 = prefix + i + "_Status";
                if (!data.ContainsKey(key3))
                {
                    Assets.ReportError(assetContext, "Quest condition " + prefix + i + " missing _Status");
                }
                else
                {
                    eNPCQuestStatus = data.ParseEnum(key3, ENPCQuestStatus.NONE);
                    if (eNPCQuestStatus == ENPCQuestStatus.NONE && flag)
                    {
                        Assets.ReportError(assetContext, "Quest condition " + prefix + i + " has Reset enabled with Status None (probably accidental)");
                    }
                }
                data.ParseGuidOrLegacyId(prefix + i + "_ID", out var guid, out var legacyId);
                iNPCCondition = new NPCQuestCondition(guid, legacyId, eNPCQuestStatus, data.ContainsKey(prefix + i + "_Ignore_NPC"), eNPCLogicType, desc, flag);
                break;
            }
            case ENPCConditionType.SKILLSET:
                if (!data.ContainsKey(prefix + i + "_Value"))
                {
                    Assets.ReportError(assetContext, "Skillset condition " + prefix + i + " missing _Value");
                }
                iNPCCondition = new NPCSkillsetCondition(data.ParseEnum(prefix + i + "_Value", EPlayerSkillset.NONE), eNPCLogicType, desc);
                break;
            case ENPCConditionType.ITEM:
            {
                string key9 = prefix + i + "_ID";
                if (!data.ContainsKey(key9))
                {
                    Assets.ReportError(assetContext, "Item condition " + prefix + i + " missing _ID");
                }
                if (!data.ContainsKey(prefix + i + "_Amount"))
                {
                    Assets.ReportError(assetContext, "Item condition " + prefix + i + " missing _Amount");
                }
                if (flag && eNPCLogicType != ENPCLogicType.GREATER_THAN_OR_EQUAL_TO)
                {
                    eNPCLogicType = ENPCLogicType.GREATER_THAN_OR_EQUAL_TO;
                    Assets.ReportError(assetContext, "Resetting item condition only compatible with >= comparison. If you have a use in mind feel free to email Nelson.");
                }
                data.ParseGuidOrLegacyId(key9, out var guid2, out var legacyId2);
                iNPCCondition = new NPCItemCondition(guid2, legacyId2, data.ParseUInt16(prefix + i + "_Amount", 0), eNPCLogicType, desc, flag);
                break;
            }
            case ENPCConditionType.KILLS_ZOMBIE:
            {
                if (!data.ContainsKey(prefix + i + "_ID"))
                {
                    Assets.ReportError(assetContext, "Zombie kills condition " + prefix + i + " missing _ID");
                }
                if (!data.ContainsKey(prefix + i + "_Value"))
                {
                    Assets.ReportError(assetContext, "Zombie kills condition " + prefix + i + " missing _Value");
                }
                EZombieSpeciality newZombie = EZombieSpeciality.NONE;
                if (data.ContainsKey(prefix + i + "_Zombie"))
                {
                    newZombie = data.ParseEnum(prefix + i + "_Zombie", EZombieSpeciality.NONE);
                }
                else
                {
                    Assets.ReportError(assetContext, "Zombie kills condition " + prefix + i + " missing _Zombie");
                }
                int newSpawnQuantity = 1;
                if (data.ContainsKey(prefix + i + "_Spawn_Quantity"))
                {
                    newSpawnQuantity = data.ParseInt32(prefix + i + "_Spawn_Quantity");
                }
                byte newNav2 = data.ParseUInt8(prefix + i + "_Nav", byte.MaxValue);
                float newRadius = data.ParseFloat(prefix + i + "_Radius", 512f);
                float newMinRadius = data.ParseFloat(prefix + i + "_MinRadius");
                int newLevelTableUniqueId = data.ParseInt32(prefix + i + "_LevelTableOverride", -1);
                iNPCCondition = new NPCZombieKillsCondition(data.ParseUInt16(prefix + i + "_ID", 0), data.ParseInt16(prefix + i + "_Value", 0), newZombie, data.ContainsKey(prefix + i + "_Spawn"), newSpawnQuantity, newNav2, newRadius, newMinRadius, newLevelTableUniqueId, desc, flag);
                break;
            }
            case ENPCConditionType.KILLS_HORDE:
                if (!data.ContainsKey(prefix + i + "_ID"))
                {
                    Assets.ReportError(assetContext, "Horde kills condition " + prefix + i + " missing _ID");
                }
                if (!data.ContainsKey(prefix + i + "_Value"))
                {
                    Assets.ReportError(assetContext, "Horde kills condition " + prefix + i + " missing _Value");
                }
                if (!data.ContainsKey(prefix + i + "_Nav"))
                {
                    Assets.ReportError(assetContext, "Horde kills condition " + prefix + i + " missing _Nav");
                }
                iNPCCondition = new NPCHordeKillsCondition(data.ParseUInt16(prefix + i + "_ID", 0), data.ParseInt16(prefix + i + "_Value", 0), data.ParseUInt8(prefix + i + "_Nav", 0), desc, flag);
                break;
            case ENPCConditionType.KILLS_ANIMAL:
                if (!data.ContainsKey(prefix + i + "_ID"))
                {
                    Assets.ReportError(assetContext, "Animal kills condition " + prefix + i + " missing _ID");
                }
                if (!data.ContainsKey(prefix + i + "_Value"))
                {
                    Assets.ReportError(assetContext, "Animal kills condition " + prefix + i + " missing _Value");
                }
                if (!data.ContainsKey(prefix + i + "_Animal"))
                {
                    Assets.ReportError(assetContext, "Animal kills condition " + prefix + i + " missing _Animal");
                }
                iNPCCondition = new NPCAnimalKillsCondition(data.ParseUInt16(prefix + i + "_ID", 0), data.ParseInt16(prefix + i + "_Value", 0), data.ParseUInt16(prefix + i + "_Animal", 0), desc, flag);
                break;
            case ENPCConditionType.COMPARE_FLAGS:
                if (!data.ContainsKey(prefix + i + "_A_ID"))
                {
                    Assets.ReportError(assetContext, "Compare flags condition " + prefix + i + " missing _A_ID");
                }
                if (!data.ContainsKey(prefix + i + "_B_ID"))
                {
                    Assets.ReportError(assetContext, "Compare flags condition " + prefix + i + " missing _B_ID");
                }
                iNPCCondition = new NPCCompareFlagsCondition(data.ParseUInt16(prefix + i + "_A_ID", 0), data.ParseUInt16(prefix + i + "_B_ID", 0), data.ContainsKey(prefix + i + "_Allow_A_Unset"), data.ContainsKey(prefix + i + "_Allow_B_Unset"), eNPCLogicType, desc, flag);
                break;
            case ENPCConditionType.TIME_OF_DAY:
                if (!data.ContainsKey(prefix + i + "_Second"))
                {
                    Assets.ReportError(assetContext, "Time of day condition " + prefix + i + " missing _Second");
                }
                iNPCCondition = new NPCTimeOfDayCondition(data.ParseInt32(prefix + i + "_Second"), eNPCLogicType, desc, flag);
                break;
            case ENPCConditionType.PLAYER_LIFE_HEALTH:
                if (!data.ContainsKey(prefix + i + "_Value"))
                {
                    Assets.ReportError(assetContext, "Player life health condition " + prefix + i + " missing _Value");
                }
                iNPCCondition = new NPCPlayerLifeHealthCondition(data.ParseInt32(prefix + i + "_Value"), eNPCLogicType, desc);
                break;
            case ENPCConditionType.PLAYER_LIFE_FOOD:
                if (!data.ContainsKey(prefix + i + "_Value"))
                {
                    Assets.ReportError(assetContext, "Player life food condition " + prefix + i + " missing _Value");
                }
                iNPCCondition = new NPCPlayerLifeFoodCondition(data.ParseInt32(prefix + i + "_Value"), eNPCLogicType, desc);
                break;
            case ENPCConditionType.PLAYER_LIFE_WATER:
                if (!data.ContainsKey(prefix + i + "_Value"))
                {
                    Assets.ReportError(assetContext, "Player life water condition " + prefix + i + " missing _Value");
                }
                iNPCCondition = new NPCPlayerLifeWaterCondition(data.ParseInt32(prefix + i + "_Value"), eNPCLogicType, desc);
                break;
            case ENPCConditionType.PLAYER_LIFE_VIRUS:
                if (!data.ContainsKey(prefix + i + "_Value"))
                {
                    Assets.ReportError(assetContext, "Player life virus condition " + prefix + i + " missing _Value");
                }
                iNPCCondition = new NPCPlayerLifeVirusCondition(data.ParseInt32(prefix + i + "_Value"), eNPCLogicType, desc);
                break;
            case ENPCConditionType.HOLIDAY:
            {
                ENPCHoliday eNPCHoliday = ENPCHoliday.NONE;
                if (data.ContainsKey(prefix + i + "_Value"))
                {
                    eNPCHoliday = data.ParseEnum(prefix + i + "_Value", ENPCHoliday.NONE);
                    if (eNPCHoliday == ENPCHoliday.NONE)
                    {
                        Assets.ReportError(assetContext, "Holiday condition " + prefix + i + " _Value is None");
                    }
                }
                else
                {
                    Assets.ReportError(assetContext, "Holiday condition " + prefix + i + " missing _Value");
                }
                iNPCCondition = new NPCHolidayCondition(eNPCHoliday, eNPCLogicType);
                break;
            }
            case ENPCConditionType.KILLS_PLAYER:
                if (!data.ContainsKey(prefix + i + "_ID"))
                {
                    Assets.ReportError(assetContext, "Player kills condition " + prefix + i + " missing _ID");
                }
                if (!data.ContainsKey(prefix + i + "_Value"))
                {
                    Assets.ReportError(assetContext, "Player kills condition " + prefix + i + " missing _Value");
                }
                iNPCCondition = new NPCPlayerKillsCondition(data.ParseUInt16(prefix + i + "_ID", 0), data.ParseInt16(prefix + i + "_Value", 0), desc, flag);
                break;
            case ENPCConditionType.KILLS_OBJECT:
            {
                if (!data.ContainsKey(prefix + i + "_ID"))
                {
                    Assets.ReportError(assetContext, "Object kills condition " + prefix + i + " missing _ID");
                }
                if (!data.ContainsKey(prefix + i + "_Value"))
                {
                    Assets.ReportError(assetContext, "Object kills condition " + prefix + i + " missing _Value");
                }
                Guid newObjectGuid;
                if (data.ContainsKey(prefix + i + "_Object"))
                {
                    newObjectGuid = data.ParseGuid(prefix + i + "_Object");
                }
                else
                {
                    newObjectGuid = default(Guid);
                    Assets.ReportError(assetContext, "Object kills condition " + prefix + i + " missing _Object (GUID)");
                }
                byte newNav = data.ParseUInt8(prefix + i + "_Nav", byte.MaxValue);
                iNPCCondition = new NPCObjectKillsCondition(data.ParseUInt16(prefix + i + "_ID", 0), data.ParseInt16(prefix + i + "_Value", 0), newObjectGuid, newNav, desc, flag);
                break;
            }
            case ENPCConditionType.CURRENCY:
            {
                string key = prefix + i + "_GUID";
                if (!data.ContainsKey(key))
                {
                    Assets.ReportError(assetContext, "Currency condition " + prefix + i + " missing _GUID");
                }
                string key2 = prefix + i + "_Value";
                if (!data.ContainsKey(key2))
                {
                    Assets.ReportError(assetContext, "Currency condition " + prefix + i + " missing _Value");
                }
                AssetReference<ItemCurrencyAsset> newCurrency = data.readAssetReference<ItemCurrencyAsset>(key);
                uint newValue = data.ParseUInt32(key2);
                iNPCCondition = new NPCCurrencyCondition(newCurrency, newValue, eNPCLogicType, desc, flag);
                break;
            }
            case ENPCConditionType.KILLS_TREE:
            {
                if (!data.ContainsKey(prefix + i + "_ID"))
                {
                    Assets.ReportError(assetContext, "Tree kills condition " + prefix + i + " missing _ID");
                }
                if (!data.ContainsKey(prefix + i + "_Value"))
                {
                    Assets.ReportError(assetContext, "Tree kills condition " + prefix + i + " missing _Value");
                }
                Guid newTreeGuid;
                if (data.ContainsKey(prefix + i + "_Tree"))
                {
                    newTreeGuid = data.ParseGuid(prefix + i + "_Tree");
                }
                else
                {
                    newTreeGuid = default(Guid);
                    Assets.ReportError(assetContext, "Tree kills condition " + prefix + i + " missing _Tree (GUID)");
                }
                iNPCCondition = new NPCTreeKillsCondition(data.ParseUInt16(prefix + i + "_ID", 0), data.ParseInt16(prefix + i + "_Value", 0), newTreeGuid, desc, flag);
                break;
            }
            case ENPCConditionType.WEATHER_STATUS:
            {
                string key7 = prefix + i + "_GUID";
                if (!data.ContainsKey(key7))
                {
                    Assets.ReportError(assetContext, "Weather condition " + prefix + i + " missing _GUID");
                }
                string key8 = prefix + i + "_Value";
                if (!data.ContainsKey(key8))
                {
                    Assets.ReportError(assetContext, "Weather condition " + prefix + i + " missing _Value");
                }
                iNPCCondition = new NPCWeatherStatusCondition(data.readAssetReference<WeatherAssetBase>(key7), data.ParseEnum(key8, ENPCWeatherStatus.Active), eNPCLogicType, desc);
                break;
            }
            case ENPCConditionType.WEATHER_BLEND_ALPHA:
            {
                string key5 = prefix + i + "_GUID";
                if (!data.ContainsKey(key5))
                {
                    Assets.ReportError(assetContext, "Weather condition " + prefix + i + " missing _GUID");
                }
                string key6 = prefix + i + "_Value";
                if (!data.ContainsKey(key6))
                {
                    Assets.ReportError(assetContext, "Weather condition " + prefix + i + " missing _Value");
                }
                iNPCCondition = new NPCWeatherBlendAlphaCondition(data.readAssetReference<WeatherAssetBase>(key5), data.ParseFloat(key6), eNPCLogicType, desc);
                break;
            }
            case ENPCConditionType.IS_FULL_MOON:
            {
                string key4 = prefix + i + "_Value";
                if (!data.ContainsKey(key4))
                {
                    Assets.ReportError(assetContext, "Full moon condition " + prefix + i + " missing _Value");
                }
                iNPCCondition = new NPCIsFullMoonCondition(data.ParseBool(key4, defaultValue: true), desc);
                break;
            }
            case ENPCConditionType.DATE_COUNTER:
                if (!data.ContainsKey(prefix + i + "_Value"))
                {
                    Assets.ReportError(assetContext, "Date counter condition " + prefix + i + " missing _Value");
                }
                if (!data.ContainsKey(prefix + i + "_Divisor"))
                {
                    Assets.ReportError(assetContext, "Date counter condition " + prefix + i + " missing _Divisor");
                }
                iNPCCondition = new NPCDateCounterCondition(data.ParseInt64(prefix + i + "_Value", 0L), data.ParseInt64(prefix + i + "_Divisor", 0L), eNPCLogicType, desc, flag);
                break;
            case ENPCConditionType.PLAYER_LIFE_STAMINA:
                if (!data.ContainsKey(prefix + i + "_Value"))
                {
                    Assets.ReportError(assetContext, "Player life stamina condition " + prefix + i + " missing _Value");
                }
                iNPCCondition = new NPCPlayerLifeStaminaCondition(data.ParseInt32(prefix + i + "_Value"), eNPCLogicType, desc);
                break;
            }
            if (iNPCCondition != null)
            {
                List<int> list = null;
                if (data.TryGetString(prefix + i + "_UI_Requirements", out var value))
                {
                    string[] array = value.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    if (array == null || array.Length < 1)
                    {
                        Assets.ReportError(assetContext, prefix + i + " empty _UI_Requirements");
                    }
                    else
                    {
                        list = new List<int>(array.Length);
                        string[] array2 = array;
                        foreach (string text2 in array2)
                        {
                            if (!int.TryParse(text2, out var result))
                            {
                                Assets.ReportError(assetContext, prefix + i + " unable to parse _UI_Requirements index from \"" + text2 + "\"");
                            }
                            else if (result < 0 || result >= conditions.Length)
                            {
                                Assets.ReportError(assetContext, prefix + i + $" UI requirement index {result} out of bounds");
                            }
                            else if (result == i)
                            {
                                Assets.ReportError(assetContext, prefix + i + " UI requirement depends on itself");
                            }
                            else
                            {
                                list.Add(result);
                            }
                        }
                        if (list.Count < 1)
                        {
                            list = null;
                        }
                    }
                }
                iNPCCondition.uiRequirementIndices = list;
            }
            conditions[i] = iNPCCondition;
        }
    }

    public static void readRewards(DatDictionary data, Local localization, string prefix, INPCReward[] rewards, Asset assetContext)
    {
        for (int i = 0; i < rewards.Length; i++)
        {
            string text = prefix + i + "_Type";
            if (!data.ContainsKey(text))
            {
                throw new NotSupportedException("Missing " + text);
            }
            ENPCRewardType eNPCRewardType = data.ParseEnum(text, ENPCRewardType.NONE);
            if (eNPCRewardType == ENPCRewardType.NONE)
            {
                Assets.ReportError(assetContext, "{0} unknown type", text);
                continue;
            }
            string desc = localization.read(prefix + i);
            desc = ItemTool.filterRarityRichText(desc);
            INPCReward iNPCReward = null;
            switch (eNPCRewardType)
            {
            case ENPCRewardType.EXPERIENCE:
                if (!data.ContainsKey(prefix + i + "_Value"))
                {
                    Assets.ReportError(assetContext, "Experience reward " + prefix + i + " missing _Value");
                }
                iNPCReward = new NPCExperienceReward(data.ParseUInt32(prefix + i + "_Value"), desc);
                break;
            case ENPCRewardType.REPUTATION:
                if (!data.ContainsKey(prefix + i + "_Value"))
                {
                    Assets.ReportError(assetContext, "Reputation reward " + prefix + i + " missing _Value");
                }
                iNPCReward = new NPCReputationReward(data.ParseInt32(prefix + i + "_Value"), desc);
                break;
            case ENPCRewardType.FLAG_BOOL:
                if (!data.ContainsKey(prefix + i + "_ID"))
                {
                    Assets.ReportError(assetContext, "Bool flag reward " + prefix + i + " missing _ID");
                }
                if (!data.ContainsKey(prefix + i + "_Value"))
                {
                    Assets.ReportError(assetContext, "Bool flag reward " + prefix + i + " missing _Value");
                }
                iNPCReward = new NPCBoolFlagReward(data.ParseUInt16(prefix + i + "_ID", 0), data.ParseBool(prefix + i + "_Value"), desc);
                break;
            case ENPCRewardType.FLAG_SHORT:
            {
                if (!data.ContainsKey(prefix + i + "_ID"))
                {
                    Assets.ReportError(assetContext, "Short flag reward " + prefix + i + " missing _ID");
                }
                if (!data.ContainsKey(prefix + i + "_Value"))
                {
                    Assets.ReportError(assetContext, "Short flag reward " + prefix + i + " missing _Value");
                }
                string key14 = prefix + i + "_Modification";
                if (!data.ContainsKey(key14))
                {
                    Assets.ReportError(assetContext, "Short flag reward " + prefix + i + " missing _Modification");
                }
                iNPCReward = new NPCShortFlagReward(data.ParseUInt16(prefix + i + "_ID", 0), data.ParseInt16(prefix + i + "_Value", 0), data.ParseEnum(key14, ENPCModificationType.NONE), desc);
                break;
            }
            case ENPCRewardType.FLAG_SHORT_RANDOM:
            {
                if (!data.ContainsKey(prefix + i + "_ID"))
                {
                    Assets.ReportError(assetContext, "Random short flag reward " + prefix + i + " missing _ID");
                }
                if (!data.ContainsKey(prefix + i + "_Min_Value"))
                {
                    Assets.ReportError(assetContext, "Random short flag reward " + prefix + i + " missing _Min_Value");
                }
                if (!data.ContainsKey(prefix + i + "_Max_Value"))
                {
                    Assets.ReportError(assetContext, "Random short flag reward " + prefix + i + " missing _Max_Value");
                }
                string key2 = prefix + i + "_Modification";
                if (!data.ContainsKey(key2))
                {
                    Assets.ReportError(assetContext, "Random short flag reward " + prefix + i + " missing _Modification");
                }
                iNPCReward = new NPCRandomShortFlagReward(data.ParseUInt16(prefix + i + "_ID", 0), data.ParseInt16(prefix + i + "_Min_Value", 0), data.ParseInt16(prefix + i + "_Max_Value", 0), data.ParseEnum(key2, ENPCModificationType.NONE), desc);
                break;
            }
            case ENPCRewardType.QUEST:
            {
                if (!data.ContainsKey(prefix + i + "_ID"))
                {
                    Assets.ReportError(assetContext, "Quest reward " + prefix + i + " missing _ID");
                }
                data.ParseGuidOrLegacyId(prefix + i + "_ID", out var guid, out var legacyId);
                iNPCReward = new NPCQuestReward(guid, legacyId, desc);
                break;
            }
            case ENPCRewardType.ITEM:
            {
                string key11 = prefix + i + "_ID";
                if (!data.ContainsKey(key11))
                {
                    Assets.ReportError(assetContext, "Item reward " + prefix + i + " missing _ID");
                }
                if (!data.ContainsKey(prefix + i + "_Amount"))
                {
                    Assets.ReportError(assetContext, "Item reward " + prefix + i + " missing _Amount");
                }
                data.ParseGuidOrLegacyId(key11, out var guid2, out var legacyId2);
                bool newShouldAutoEquip2 = data.ParseBool(prefix + i + "_Auto_Equip");
                EItemOrigin origin2 = data.ParseEnum(prefix + i + "_Origin", EItemOrigin.CRAFT);
                iNPCReward = new NPCItemReward(guid2, legacyId2, data.ParseUInt8(prefix + i + "_Amount", 0), newShouldAutoEquip2, data.ParseInt32(prefix + i + "_Sight", -1), data.ParseInt32(prefix + i + "_Tactical", -1), data.ParseInt32(prefix + i + "_Grip", -1), data.ParseInt32(prefix + i + "_Barrel", -1), data.ParseInt32(prefix + i + "_Magazine", -1), data.ParseInt32(prefix + i + "_Ammo", -1), origin2, desc);
                break;
            }
            case ENPCRewardType.ITEM_RANDOM:
            {
                if (!data.ContainsKey(prefix + i + "_ID"))
                {
                    Assets.ReportError(assetContext, "Random item reward " + prefix + i + " missing _ID");
                }
                if (!data.ContainsKey(prefix + i + "_Amount"))
                {
                    Assets.ReportError(assetContext, "Random item reward " + prefix + i + " missing _Amount");
                }
                bool newShouldAutoEquip = data.ParseBool(prefix + i + "_Auto_Equip");
                EItemOrigin origin = data.ParseEnum(prefix + i + "_Origin", EItemOrigin.CRAFT);
                iNPCReward = new NPCRandomItemReward(data.ParseUInt16(prefix + i + "_ID", 0), data.ParseUInt8(prefix + i + "_Amount", 0), newShouldAutoEquip, origin, desc);
                break;
            }
            case ENPCRewardType.ACHIEVEMENT:
            {
                if (!data.ContainsKey(prefix + i + "_ID"))
                {
                    Assets.ReportError(assetContext, "Achievement reward " + prefix + i + " missing _ID");
                }
                string string2 = data.GetString(prefix + i + "_ID");
                if (!Provider.statusData.Achievements.canBeGrantedByNPC(string2))
                {
                    Assets.ReportError(assetContext, "achievement \"{0}\" cannot be granted by NPCs", string2);
                }
                iNPCReward = new NPCAchievementReward(string2, desc);
                break;
            }
            case ENPCRewardType.VEHICLE:
            {
                string key12 = prefix + i + "_ID";
                if (!data.ContainsKey(key12))
                {
                    Assets.ReportError(assetContext, "Vehicle reward " + prefix + i + " missing _ID");
                }
                if (!data.ContainsKey(prefix + i + "_Spawnpoint"))
                {
                    Assets.ReportError(assetContext, "Vehicle reward " + prefix + i + " missing _Spawnpoint");
                }
                data.ParseGuidOrLegacyId(key12, out var guid3, out var legacyId3);
                Color32? newPaintColor = null;
                if (data.TryParseColor32RGB(prefix + i + "_PaintColor", out var value))
                {
                    newPaintColor = value;
                }
                iNPCReward = new NPCVehicleReward(guid3, legacyId3, data.GetString(prefix + i + "_Spawnpoint"), newPaintColor, desc);
                break;
            }
            case ENPCRewardType.TELEPORT:
                if (!data.ContainsKey(prefix + i + "_Spawnpoint"))
                {
                    Assets.ReportError(assetContext, "Teleport reward " + prefix + i + " missing _Spawnpoint");
                }
                iNPCReward = new NPCTeleportReward(data.GetString(prefix + i + "_Spawnpoint"), desc);
                break;
            case ENPCRewardType.EVENT:
                if (!data.ContainsKey(prefix + i + "_ID"))
                {
                    Assets.ReportError(assetContext, "Event reward " + prefix + i + " missing _ID");
                }
                iNPCReward = new NPCEventReward(data.GetString(prefix + i + "_ID"), desc);
                break;
            case ENPCRewardType.FLAG_MATH:
            {
                if (!data.ContainsKey(prefix + i + "_A_ID"))
                {
                    Assets.ReportError(assetContext, "Math reward " + prefix + i + " missing _A_ID");
                }
                if (!data.ContainsKey(prefix + i + "_B_ID") && !data.ContainsKey(prefix + i + "_B_Value"))
                {
                    Assets.ReportError(assetContext, "Math reward " + prefix + i + " missing _B_ID or _B_Value");
                }
                string key9 = prefix + i + "_Operation";
                if (!data.ContainsKey(key9))
                {
                    Assets.ReportError(assetContext, "Math reward " + prefix + i + " missing _Operation");
                }
                iNPCReward = new NPCFlagMathReward(data.ParseUInt16(prefix + i + "_A_ID", 0), data.ParseUInt16(prefix + i + "_B_ID", 0), data.ParseInt16(prefix + i + "_B_Value", 0), data.ParseEnum(key9, ENPCOperationType.NONE), desc);
                break;
            }
            case ENPCRewardType.CURRENCY:
            {
                string key5 = prefix + i + "_GUID";
                if (!data.ContainsKey(key5))
                {
                    Assets.ReportError(assetContext, "Currency reward " + prefix + i + " missing _GUID");
                }
                string key6 = prefix + i + "_Value";
                if (!data.ContainsKey(key6))
                {
                    Assets.ReportError(assetContext, "Currency reward " + prefix + i + " missing _Value");
                }
                AssetReference<ItemCurrencyAsset> newCurrency = data.readAssetReference<ItemCurrencyAsset>(key5);
                uint newValue = data.ParseUInt32(key6);
                iNPCReward = new NPCCurrencyReward(newCurrency, newValue, desc);
                break;
            }
            case ENPCRewardType.HINT:
                if (string.IsNullOrEmpty(desc))
                {
                    desc = data.GetString(prefix + i + "_Text");
                }
                iNPCReward = new NPCHintReward(data.ParseFloat(prefix + i + "_Duration", 2f), desc);
                break;
            case ENPCRewardType.PLAYER_SPAWNPOINT:
                iNPCReward = new NPCPlayerSpawnpointReward(data.GetString(prefix + i + "_ID"), desc);
                break;
            case ENPCRewardType.PLAYER_LIFE_HEALTH:
            {
                string key15 = prefix + i + "_Value";
                if (!data.ContainsKey(key15))
                {
                    Assets.ReportError(assetContext, "Player life health reward " + prefix + i + " missing _Value");
                }
                iNPCReward = new NPCPlayerLifeHealthReward(data.ParseInt32(key15), desc);
                break;
            }
            case ENPCRewardType.PLAYER_LIFE_FOOD:
            {
                string key13 = prefix + i + "_Value";
                if (!data.ContainsKey(key13))
                {
                    Assets.ReportError(assetContext, "Player life food reward " + prefix + i + " missing _Value");
                }
                iNPCReward = new NPCPlayerLifeFoodReward(data.ParseInt32(key13), desc);
                break;
            }
            case ENPCRewardType.PLAYER_LIFE_WATER:
            {
                string key10 = prefix + i + "_Value";
                if (!data.ContainsKey(key10))
                {
                    Assets.ReportError(assetContext, "Player life water reward " + prefix + i + " missing _Value");
                }
                iNPCReward = new NPCPlayerLifeWaterReward(data.ParseInt32(key10), desc);
                break;
            }
            case ENPCRewardType.PLAYER_LIFE_VIRUS:
            {
                string key8 = prefix + i + "_Value";
                if (!data.ContainsKey(key8))
                {
                    Assets.ReportError(assetContext, "Player life virus reward " + prefix + i + " missing _Value");
                }
                iNPCReward = new NPCPlayerLifeVirusReward(data.ParseInt32(key8), desc);
                break;
            }
            case ENPCRewardType.REWARDS_LIST_ASSET:
            {
                string key7 = prefix + i + "_GUID";
                if (!data.ContainsKey(key7))
                {
                    Assets.ReportError(assetContext, "Rewards list asset reward " + prefix + i + " missing _GUID");
                }
                iNPCReward = new NPCRewardsListAssetReward(data.readAssetReference<Asset>(key7), desc);
                break;
            }
            case ENPCRewardType.CUTSCENE_MODE:
            {
                string key4 = prefix + i + "_Value";
                if (!data.ContainsKey(key4))
                {
                    Assets.ReportError(assetContext, "Cutscene mode reward " + prefix + i + " missing _Value");
                }
                iNPCReward = new NPCCutsceneModeReward(data.ParseBool(key4), desc);
                break;
            }
            case ENPCRewardType.PLAYER_LIFE_STAMINA:
            {
                string key3 = prefix + i + "_Value";
                if (!data.ContainsKey(key3))
                {
                    Assets.ReportError(assetContext, "Player life stamina reward " + prefix + i + " missing _Value");
                }
                iNPCReward = new NPCPlayerLifeStaminaReward(data.ParseInt32(key3), desc);
                break;
            }
            case ENPCRewardType.EFFECT:
            {
                string key = prefix + i + "_GUID";
                if (!data.ContainsKey(key))
                {
                    Assets.ReportError(assetContext, "Effect reward " + prefix + i + " missing _GUID");
                }
                if (!data.ContainsKey(prefix + i + "_Spawnpoint"))
                {
                    Assets.ReportError(assetContext, "Effect reward " + prefix + i + " missing _Spawnpoint");
                }
                AssetReference<EffectAsset> newAssetRef = data.readAssetReference<EffectAsset>(key);
                string @string = data.GetString(prefix + i + "_Spawnpoint");
                bool newIsReliable = data.ParseBool(prefix + i + "_IsReliable", defaultValue: true);
                float newRelevantDistance = data.ParseFloat(prefix + i + "_RelevantDistance", -1f);
                iNPCReward = new NPCEffectReward(newAssetRef, @string, newIsReliable, newRelevantDistance, desc);
                break;
            }
            }
            if (iNPCReward != null)
            {
                iNPCReward.grantDelaySeconds = data.ParseFloat(prefix + i + "_GrantDelaySeconds", -1f);
                if (iNPCReward.grantDelaySeconds > 0f)
                {
                    iNPCReward.grantDelayApplyWhenInterrupted = data.ParseBool(prefix + i + "_GrantDelayApplyWhenInterrupted");
                }
            }
            rewards[i] = iNPCReward;
        }
    }
}
