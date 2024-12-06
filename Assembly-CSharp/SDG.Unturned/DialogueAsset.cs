using System;
using System.Collections.Generic;

namespace SDG.Unturned;

public class DialogueAsset : Asset
{
    public DialogueMessage[] messages { get; protected set; }

    public DialogueResponse[] responses { get; protected set; }

    public override EAssetType assetCategory => EAssetType.NPC;

    public DialogueMessage GetAvailableMessage(Player player)
    {
        for (int i = 0; i < messages.Length; i++)
        {
            DialogueMessage dialogueMessage = messages[i];
            if (dialogueMessage.areConditionsMet(player))
            {
                return dialogueMessage;
            }
        }
        return null;
    }

    internal void GetAllResponsesForMessage(int messageIndex, List<DialogueResponse> messageResponses)
    {
        DialogueMessage dialogueMessage = messages[messageIndex];
        if (dialogueMessage.responses != null && dialogueMessage.responses.Length != 0)
        {
            for (int i = 0; i < dialogueMessage.responses.Length; i++)
            {
                DialogueResponse item = responses[dialogueMessage.responses[i]];
                messageResponses.Add(item);
            }
            return;
        }
        for (int j = 0; j < responses.Length; j++)
        {
            DialogueResponse dialogueResponse = responses[j];
            if (dialogueResponse.messages != null && dialogueResponse.messages.Length != 0)
            {
                bool flag = false;
                for (int k = 0; k < dialogueResponse.messages.Length; k++)
                {
                    if (dialogueResponse.messages[k] == messageIndex)
                    {
                        flag = true;
                        break;
                    }
                }
                if (!flag)
                {
                    continue;
                }
            }
            messageResponses.Add(dialogueResponse);
        }
    }

    public void getAvailableResponses(Player player, int messageIndex, List<DialogueResponse> availableResponses)
    {
        DialogueMessage dialogueMessage = messages[messageIndex];
        if (dialogueMessage.responses != null && dialogueMessage.responses.Length != 0)
        {
            for (int i = 0; i < dialogueMessage.responses.Length; i++)
            {
                DialogueResponse dialogueResponse = responses[dialogueMessage.responses[i]];
                if (dialogueResponse.areConditionsMet(player))
                {
                    availableResponses.Add(dialogueResponse);
                }
            }
            return;
        }
        for (int j = 0; j < responses.Length; j++)
        {
            DialogueResponse dialogueResponse2 = responses[j];
            if (dialogueResponse2.messages != null && dialogueResponse2.messages.Length != 0)
            {
                bool flag = false;
                for (int k = 0; k < dialogueResponse2.messages.Length; k++)
                {
                    if (dialogueResponse2.messages[k] == messageIndex)
                    {
                        flag = true;
                        break;
                    }
                }
                if (!flag)
                {
                    continue;
                }
            }
            if (dialogueResponse2.areConditionsMet(player))
            {
                availableResponses.Add(dialogueResponse2);
            }
        }
    }

    public override void PopulateAsset(Bundle bundle, DatDictionary data, Local localization)
    {
        base.PopulateAsset(bundle, data, localization);
        if (id < 2000 && !base.OriginAllowsVanillaLegacyId && !data.ContainsKey("Bypass_ID_Limit"))
        {
            throw new NotSupportedException("ID < 2000");
        }
        int num = data.ParseInt32("Messages");
        int num2 = data.ParseUInt8("Responses", 0);
        messages = new DialogueMessage[num];
        for (byte b = 0; b < messages.Length; b++)
        {
            DialoguePage[] array = new DialoguePage[data.ParseUInt8("Message_" + b + "_Pages", 0)];
            for (byte b2 = 0; b2 < array.Length; b2++)
            {
                string desc = localization.format("Message_" + b + "_Page_" + b2);
                desc = ItemTool.filterRarityRichText(desc);
                RichTextUtil.replaceNewlineMarkup(ref desc);
                if (string.IsNullOrEmpty(desc))
                {
                    throw new NotSupportedException("missing message " + b + " page " + b2);
                }
                array[b2] = new DialoguePage(desc);
            }
            byte[] array2 = new byte[data.ParseUInt8("Message_" + b + "_Responses", 0)];
            for (byte b3 = 0; b3 < array2.Length; b3++)
            {
                string text = "Message_" + b + "_Response_" + b3;
                array2[b3] = data.ParseUInt8(text, 0);
                if (array2[b3] >= num2)
                {
                    Assets.ReportError(this, "{0} out of bounds ({1})", text, num2);
                }
            }
            Guid guid;
            ushort newPrev = data.ParseGuidOrLegacyId("Message_" + b + "_Prev", out guid);
            byte? faceOverride = ((!data.ContainsKey("Message_" + b + "_FaceOverride")) ? null : new byte?(data.ParseUInt8("Message_" + b + "_FaceOverride", 0)));
            INPCCondition[] array3 = new INPCCondition[data.ParseUInt8("Message_" + b + "_Conditions", 0)];
            NPCTool.readConditions(data, localization, "Message_" + b + "_Condition_", array3, this);
            NPCRewardsList newRewardsList = default(NPCRewardsList);
            newRewardsList.Parse(data, localization, this, "Message_" + b + "_Rewards", "Message_" + b + "_Reward_");
            messages[b] = new DialogueMessage(b, array, array2, newPrev, guid, faceOverride, array3, newRewardsList);
        }
        responses = new DialogueResponse[num2];
        for (byte b4 = 0; b4 < responses.Length; b4++)
        {
            byte[] array4 = new byte[data.ParseUInt8("Response_" + b4 + "_Messages", 0)];
            for (byte b5 = 0; b5 < array4.Length; b5++)
            {
                string text2 = "Response_" + b4 + "_Message_" + b5;
                array4[b5] = data.ParseUInt8(text2, 0);
                if (array4[b5] >= num)
                {
                    Assets.ReportError(this, "{0} out of bounds ({1})", text2, num);
                }
            }
            Guid guid2;
            ushort newDialogue = data.ParseGuidOrLegacyId("Response_" + b4 + "_Dialogue", out guid2);
            Guid guid3;
            ushort newQuest = data.ParseGuidOrLegacyId("Response_" + b4 + "_Quest", out guid3);
            Guid guid4;
            ushort newVendor = data.ParseGuidOrLegacyId("Response_" + b4 + "_Vendor", out guid4);
            string desc2 = localization.format("Response_" + b4);
            desc2 = ItemTool.filterRarityRichText(desc2);
            RichTextUtil.replaceNewlineMarkup(ref desc2);
            if (string.IsNullOrEmpty(desc2))
            {
                throw new NotSupportedException("missing response " + b4);
            }
            INPCCondition[] array5 = new INPCCondition[data.ParseUInt8("Response_" + b4 + "_Conditions", 0)];
            NPCTool.readConditions(data, localization, "Response_" + b4 + "_Condition_", array5, this);
            NPCRewardsList newRewardsList2 = default(NPCRewardsList);
            newRewardsList2.Parse(data, localization, this, "Response_" + b4 + "_Rewards", "Response_" + b4 + "_Reward_");
            responses[b4] = new DialogueResponse(b4, array4, newDialogue, guid2, newQuest, guid3, newVendor, guid4, desc2, array5, newRewardsList2);
        }
    }

    [Obsolete("Please use GetAvailableMessage which returns the DialogueMessage rather than index")]
    public int getAvailableMessage(Player player)
    {
        return ((int?)GetAvailableMessage(player)?.index) ?? (-1);
    }

    [Obsolete("Server now tracks dialogue tree")]
    public bool doesPlayerHaveAccessToVendor(Player player, VendorAsset vendorAsset)
    {
        return true;
    }
}
