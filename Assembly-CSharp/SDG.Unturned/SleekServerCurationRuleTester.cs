using System;
using System.Collections.Generic;
using Steamworks;
using Unturned.SystemEx;

namespace SDG.Unturned;

internal class SleekServerCurationRuleTester : SleekWrapper
{
    private Local localization;

    private ISleekField nameField;

    private ISleekField addressField;

    private ISleekField serverIdField;

    private ISleekLabel matchBox;

    private List<ServerListCurationRule> matchedRules = new List<ServerListCurationRule>();

    public event System.Action OnInputChanged;

    public void TestRules(List<ServerListCurationRule> rules, bool mergedRules)
    {
        if (rules == null && !mergedRules)
        {
            matchBox.Text = localization.format("Test_NoMatch");
            return;
        }
        string text = nameField.Text;
        if (string.IsNullOrWhiteSpace(text))
        {
            matchBox.Text = localization.format("Test_NameInvalid");
            return;
        }
        IPv4Address address = IPv4Address.Zero;
        if (!IPv4Address.TryParseWithOptionalPort(addressField.Text, out address, out ushort? optionalPort) || !optionalPort.HasValue)
        {
            matchBox.Text = localization.format("Test_AddressInvalid");
            return;
        }
        ushort value = optionalPort.Value;
        CSteamID nil = CSteamID.Nil;
        if (!string.IsNullOrWhiteSpace(serverIdField.Text) && !ulong.TryParse(serverIdField.Text, out nil.m_SteamID))
        {
            matchBox.Text = localization.format("Test_ServerIdInvalid");
            return;
        }
        ServerListCurationInput input = new ServerListCurationInput(text, address, value, nil);
        ServerListCurationOutput output = default(ServerListCurationOutput);
        output.matchedRules = matchedRules;
        ServerListCuration serverListCuration = ServerListCuration.Get();
        if (mergedRules)
        {
            serverListCuration.RefreshIfDirty();
            serverListCuration.MergeRulesIfDirty();
            serverListCuration.EvaluateMergedRules(in input, ref output);
        }
        else
        {
            serverListCuration.Evaluate(rules, in input, ref output);
        }
        if (output.matchedAnyRules)
        {
            string arg = localization.format("Test_Match_Count", output.matchedRules.Count);
            string arg2 = localization.format(output.allowed ? "Test_Match_Allowed" : "Test_Match_Denied");
            string arg3 = (string.IsNullOrEmpty(output.labels) ? localization.format("Test_Match_NoLabels") : localization.format("Test_Match_HasLabels", output.labels));
            string text2 = localization.format("Test_Match_Format", arg, arg2, arg3);
            if (output.allowOrDenyRule != null)
            {
                string text3 = localization.format("Test_Match_Rule", output.allowOrDenyRule.description, output.allowOrDenyRule.owner.Name);
                text2 = text2 + "\n" + text3;
            }
            matchBox.Text = text2;
        }
        else
        {
            matchBox.Text = localization.format("Test_NoMatch");
        }
    }

    internal SleekServerCurationRuleTester(Local localization)
    {
        this.localization = localization;
        nameField = Glazier.Get().CreateStringField();
        nameField.SizeOffset_X = -200f;
        nameField.SizeOffset_Y = 30f;
        nameField.SizeScale_X = 0.333f;
        nameField.AddLabel(localization.format("Test_Input_Name_Label"), ESleekSide.RIGHT);
        nameField.TooltipText = localization.format("Test_Input_Name_Tooltip");
        nameField.OnTextChanged += OnTextChanged;
        AddChild(nameField);
        addressField = Glazier.Get().CreateStringField();
        addressField.PositionScale_X = 0.333f;
        addressField.SizeOffset_X = -200f;
        addressField.SizeOffset_Y = 30f;
        addressField.SizeScale_X = 0.333f;
        addressField.Text = "127.0.0.1:27015";
        addressField.AddLabel(localization.format("Test_Input_Address_Label"), ESleekSide.RIGHT);
        addressField.TooltipText = localization.format("Test_Input_Address_Tooltip");
        addressField.OnTextChanged += OnTextChanged;
        AddChild(addressField);
        serverIdField = Glazier.Get().CreateStringField();
        serverIdField.PositionScale_X = 0.666f;
        serverIdField.SizeOffset_X = -200f;
        serverIdField.SizeOffset_Y = 30f;
        serverIdField.SizeScale_X = 0.333f;
        serverIdField.AddLabel(localization.format("Test_Input_ServerID_Label"), ESleekSide.RIGHT);
        serverIdField.TooltipText = localization.format("Test_Input_ServerID_Tooltip");
        serverIdField.OnTextChanged += OnTextChanged;
        AddChild(serverIdField);
        matchBox = Glazier.Get().CreateBox();
        matchBox.PositionOffset_Y = 30f;
        matchBox.SizeOffset_Y = 50f;
        matchBox.SizeScale_X = 1f;
        matchBox.AllowRichText = true;
        matchBox.TextColor = ESleekTint.RICH_TEXT_DEFAULT;
        matchBox.TextContrastContext = ETextContrastContext.InconspicuousBackdrop;
        AddChild(matchBox);
    }

    private void OnTextChanged(ISleekField field, string text)
    {
        this.OnInputChanged?.Invoke();
    }

    private void OnInputTypeChanged(SleekButtonState button, int state)
    {
        this.OnInputChanged?.Invoke();
    }
}
