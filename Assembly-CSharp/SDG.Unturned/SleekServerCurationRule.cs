using UnityEngine;

namespace SDG.Unturned;

/// <summary>
/// Entry in the MenuPlayServerCurationRulesUI list.
/// </summary>
public class SleekServerCurationRule : SleekWrapper
{
    internal SleekServerCurationRule(MenuPlayServerCurationRulesUI rulesUI, ServerListCurationRule rule)
    {
        ISleekBox sleekBox = Glazier.Get().CreateBox();
        sleekBox.SizeScale_X = 1f;
        sleekBox.SizeScale_Y = 1f;
        string arg = rule.action switch
        {
            EServerListCurationAction.Label => rulesUI.localization.format("Rule_Action_Label"), 
            EServerListCurationAction.Allow => rulesUI.localization.format("Rule_Action_Allow"), 
            EServerListCurationAction.Deny => rulesUI.localization.format("Rule_Action_Deny"), 
            _ => $"Unknown ({rule.action})", 
        };
        string arg2;
        string arg3;
        switch (rule.ruleType)
        {
        case EServerListCurationRuleType.Name:
            arg2 = rulesUI.localization.format("Rule_Type_Name");
            arg3 = GetValueString(rule.regexes);
            break;
        case EServerListCurationRuleType.IPv4:
            arg2 = rulesUI.localization.format("Rule_Type_IPv4");
            arg3 = GetValueString(rule.ipv4Filters);
            break;
        case EServerListCurationRuleType.ServerID:
            arg2 = rulesUI.localization.format("Rule_Type_ServerID");
            arg3 = GetValueString(rule.steamIds);
            break;
        default:
            arg2 = $"Unknown ({rule.ruleType})";
            arg3 = string.Empty;
            break;
        }
        string key = (rule.inverted ? "Rule_Inverted_Format" : "Rule_NotInverted_Format");
        ISleekLabel sleekLabel = Glazier.Get().CreateLabel();
        sleekLabel.PositionOffset_X = 5f;
        sleekLabel.SizeScale_X = 1f;
        sleekLabel.SizeOffset_X = -10f;
        sleekLabel.TextAlignment = TextAnchor.MiddleLeft;
        sleekLabel.Text = rule.description;
        sleekLabel.SizeOffset_Y = 30f;
        sleekBox.AddChild(sleekLabel);
        ISleekLabel sleekLabel2 = Glazier.Get().CreateLabel();
        sleekLabel2.PositionOffset_X = 5f;
        sleekLabel2.PositionOffset_Y = 15f;
        sleekLabel2.SizeScale_X = 1f;
        sleekLabel2.SizeOffset_X = -10f;
        sleekLabel2.SizeOffset_Y = 30f;
        sleekLabel2.FontSize = ESleekFontSize.Small;
        sleekLabel2.TextAlignment = TextAnchor.MiddleLeft;
        sleekLabel2.Text = rulesUI.localization.format(key, arg, arg2, arg3);
        sleekBox.AddChild(sleekLabel2);
        if (!string.IsNullOrEmpty(rule.label))
        {
            ISleekLabel sleekLabel3 = Glazier.Get().CreateLabel();
            sleekLabel3.PositionOffset_X = 5f;
            sleekLabel3.SizeScale_X = 1f;
            sleekLabel3.SizeOffset_X = -10f;
            sleekLabel3.TextContrastContext = ETextContrastContext.InconspicuousBackdrop;
            sleekLabel3.AllowRichText = true;
            sleekLabel3.TextColor = ESleekTint.RICH_TEXT_DEFAULT;
            sleekLabel3.TextAlignment = TextAnchor.MiddleRight;
            sleekLabel3.Text = rulesUI.localization.format("Rule_ApplyLabel", rule.label);
            sleekLabel3.SizeOffset_Y = 30f;
            sleekBox.AddChild(sleekLabel3);
        }
        AddChild(sleekBox);
    }

    private string GetValueString<T>(T[] values)
    {
        string text = string.Empty;
        if (values != null && values.Length != 0)
        {
            text += values[0].ToString();
            for (int i = 1; i < values.Length; i++)
            {
                text += " ";
                text += values[i].ToString();
            }
        }
        return text;
    }
}
