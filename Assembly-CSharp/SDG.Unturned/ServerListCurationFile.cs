using System.Collections.Generic;
using System.Text.RegularExpressions;
using Steamworks;
using Unturned.SystemEx;

namespace SDG.Unturned;

/// <summary>
/// Data in common between list downloaded from a GET request and a ServerListCurationAsset.
/// </summary>
internal class ServerListCurationFile
{
    internal List<ServerListCurationRule> rules;

    internal Dictionary<string, string> labels;

    public string Name { get; set; }

    /// <summary>
    /// Optional web image path if icon isn't included.
    /// </summary>
    public string IconUrl { get; protected set; }

    public void Populate(IAssetErrorContext errorContext, DatDictionary data, Local localization)
    {
        if (localization != null && localization.has("Name"))
        {
            Name = localization.format("Name");
        }
        else
        {
            Name = data.GetString("Name");
        }
        IconUrl = data.GetString("IconURL");
        if (data.TryGetList("Labels", out var node))
        {
            labels = new Dictionary<string, string>(node.Count);
            foreach (IDatNode item4 in node)
            {
                if (item4 is DatDictionary datDictionary)
                {
                    string @string = datDictionary.GetString("Name");
                    string key = "Label_" + @string;
                    string value = ((localization == null || !localization.has(key)) ? datDictionary.GetString("Text") : localization.format(key));
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        Assets.ReportError(errorContext, "label \"" + @string + "\" text is empty");
                    }
                    else
                    {
                        labels[@string] = value;
                    }
                }
            }
        }
        if (data.TryGetList("Rules", out var node2))
        {
            rules = new List<ServerListCurationRule>(node2.Count);
            for (int i = 0; i < node2.Count; i++)
            {
                if (node2[i] is DatDictionary datDictionary2)
                {
                    if (!datDictionary2.TryParseEnum<EServerListCurationRuleType>("Type", out var value2))
                    {
                        Assets.ReportError(errorContext, $"unable to parse rule index {i} Type");
                        continue;
                    }
                    if (!datDictionary2.TryParseEnum<EServerListCurationAction>("Action", out var value3))
                    {
                        Assets.ReportError(errorContext, $"unable to parse rule index {i} Action");
                        continue;
                    }
                    bool flag = false;
                    Regex[] regexes = null;
                    IPv4Filter[] ipv4Filters = null;
                    CSteamID[] array = null;
                    switch (value2)
                    {
                    case EServerListCurationRuleType.Name:
                    {
                        string value6;
                        if (datDictionary2.TryGetList("Regexes", out var node4))
                        {
                            List<Regex> list2 = new List<Regex>(node4.Count);
                            for (int k = 0; k < node4.Count; k++)
                            {
                                if (node4[k] is DatValue datValue2)
                                {
                                    try
                                    {
                                        Regex item2 = new Regex(datValue2.value);
                                        list2.Add(item2);
                                    }
                                    catch
                                    {
                                        Assets.ReportError(errorContext, $"unable to parse rule at index {i} Regexes list item at index {k} (\"{datValue2.value}\")");
                                    }
                                }
                                else
                                {
                                    Assets.ReportError(errorContext, $"unable to parse rule at index {i} Regexes list item at index {k}");
                                }
                            }
                            if (list2.Count > 0)
                            {
                                flag = true;
                                regexes = list2.ToArray();
                            }
                            else
                            {
                                Assets.ReportError(errorContext, $"rule at index {i} Regexes list is empty");
                            }
                        }
                        else if (datDictionary2.TryGetString("Regex", out value6))
                        {
                            try
                            {
                                regexes = new Regex[1]
                                {
                                    new Regex(value6)
                                };
                                flag = true;
                            }
                            catch
                            {
                                Assets.ReportError(errorContext, $"unable to parse rule at index {i} Regex (\"{value6}\")");
                            }
                        }
                        else
                        {
                            Assets.ReportError(errorContext, $"rule at index {i} missing Regex or Regexes property");
                        }
                        break;
                    }
                    case EServerListCurationRuleType.IPv4:
                    {
                        string value7;
                        if (datDictionary2.TryGetList("Filters", out var node5))
                        {
                            List<IPv4Filter> list3 = new List<IPv4Filter>(node5.Count);
                            for (int l = 0; l < node5.Count; l++)
                            {
                                if (node5[l] is DatValue datValue3)
                                {
                                    if (IPv4Filter.TryParse(datValue3.value, out var filter))
                                    {
                                        list3.Add(filter);
                                    }
                                    else
                                    {
                                        Assets.ReportError(errorContext, $"unable to parse rule at index {i} Filters list item at index {l} (\"{datValue3.value}\")");
                                    }
                                }
                                else
                                {
                                    Assets.ReportError(errorContext, $"unable to parse rule at index {i} Filters list item at index {l}");
                                }
                            }
                            if (list3.Count > 0)
                            {
                                flag = true;
                                ipv4Filters = list3.ToArray();
                            }
                            else
                            {
                                Assets.ReportError(errorContext, $"rule at index {i} Filters list is empty");
                            }
                        }
                        else if (datDictionary2.TryGetString("Filter", out value7))
                        {
                            if (IPv4Filter.TryParse(value7, out var filter2))
                            {
                                ipv4Filters = new IPv4Filter[1] { filter2 };
                                flag = true;
                            }
                            else
                            {
                                Assets.ReportError(errorContext, $"unable to parse rule at index {i} IPv4 Filter (\"{value7}\")");
                            }
                        }
                        else
                        {
                            Assets.ReportError(errorContext, $"rule at index {i} missing Filter or Filters property");
                        }
                        break;
                    }
                    case EServerListCurationRuleType.ServerID:
                    {
                        ulong value5;
                        if (datDictionary2.TryGetList("Values", out var node3))
                        {
                            List<CSteamID> list = new List<CSteamID>(node3.Count);
                            for (int j = 0; j < node3.Count; j++)
                            {
                                if (node3[j] is DatValue datValue)
                                {
                                    if (datValue.TryParseUInt64(out var value4))
                                    {
                                        CSteamID item = new CSteamID(value4);
                                        if (item.BPersistentGameServerAccount())
                                        {
                                            list.Add(item);
                                        }
                                        else
                                        {
                                            Assets.ReportError(errorContext, $"rule at index {i} Values list item at index {j} is not a persistent server ID ({value4})");
                                        }
                                    }
                                    else
                                    {
                                        Assets.ReportError(errorContext, $"unable to parse rule at index {i} Values list item at index {j} (\"{datValue.value}\")");
                                    }
                                }
                                else
                                {
                                    Assets.ReportError(errorContext, $"unable to parse rule at index {i} Values list item at index {j}");
                                }
                            }
                            if (list.Count > 0)
                            {
                                flag = true;
                                array = list.ToArray();
                            }
                            else
                            {
                                Assets.ReportError(errorContext, $"rule at index {i} Values list is empty");
                            }
                        }
                        else if (datDictionary2.TryParseUInt64("Value", out value5))
                        {
                            array = new CSteamID[1]
                            {
                                new CSteamID(value5)
                            };
                            if (array[0].BPersistentGameServerAccount())
                            {
                                flag = true;
                            }
                            else
                            {
                                Assets.ReportError(errorContext, $"rule at index {i} Value is not a persistent server ID ({value5})");
                            }
                        }
                        else
                        {
                            Assets.ReportError(errorContext, $"unable to parse rule at index {i} Value");
                        }
                        break;
                    }
                    }
                    if (!flag)
                    {
                        continue;
                    }
                    string value8;
                    bool flag2 = datDictionary2.TryGetString("Label", out value8);
                    if (value3 == EServerListCurationAction.Label && !flag2)
                    {
                        Assets.ReportError(errorContext, $"rule at index {i} action is Label but no Label is specified");
                        continue;
                    }
                    if (flag2 && string.IsNullOrEmpty(value8))
                    {
                        Assets.ReportError(errorContext, $"rule at index {i} Label is empty");
                        continue;
                    }
                    string value9 = null;
                    if (flag2 && (labels == null || !labels.TryGetValue(value8, out value9)))
                    {
                        Assets.ReportError(errorContext, $"rule at index {i} Label \"{value8}\" is not configured in Labels list");
                        continue;
                    }
                    if (!datDictionary2.TryGetString("Description", out var value10))
                    {
                        value10 = $"Default description for rule at index {i}";
                    }
                    bool inverted = datDictionary2.ParseBool("Inverted");
                    ServerListCurationRule item3 = new ServerListCurationRule
                    {
                        ruleType = value2,
                        action = value3,
                        inverted = inverted,
                        description = value10,
                        label = value9,
                        regexes = regexes,
                        ipv4Filters = ipv4Filters,
                        steamIds = array,
                        owner = this
                    };
                    rules.Add(item3);
                }
                else
                {
                    Assets.ReportError(errorContext, $"unable to parse rule at index {i}");
                }
            }
        }
        else
        {
            Assets.ReportError(errorContext, "missing Rules list");
        }
    }
}
