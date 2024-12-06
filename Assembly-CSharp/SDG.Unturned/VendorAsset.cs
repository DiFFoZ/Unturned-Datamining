using System;
using UnityEngine;

namespace SDG.Unturned;

public class VendorAsset : Asset
{
    public string vendorName { get; protected set; }

    public override string FriendlyName => RichTextUtil.replaceColorTags(vendorName);

    public string vendorDescription { get; protected set; }

    public VendorBuying[] buying { get; protected set; }

    public VendorSellingBase[] selling { get; protected set; }

    /// <summary>
    /// Should the buying and selling lists be alphabetically sorted?
    /// </summary>
    public bool enableSorting { get; protected set; }

    public AssetReference<ItemCurrencyAsset> currency { get; protected set; }

    public byte? faceOverride { get; private set; }

    public override EAssetType assetCategory => EAssetType.NPC;

    public override void PopulateAsset(Bundle bundle, DatDictionary data, Local localization)
    {
        base.PopulateAsset(bundle, data, localization);
        if (id < 2000 && !base.OriginAllowsVanillaLegacyId && !data.ContainsKey("Bypass_ID_Limit"))
        {
            throw new NotSupportedException("ID < 2000");
        }
        vendorName = localization.format("Name");
        vendorName = ItemTool.filterRarityRichText(vendorName);
        string desc = localization.format("Description");
        desc = ItemTool.filterRarityRichText(desc);
        RichTextUtil.replaceNewlineMarkup(ref desc);
        vendorDescription = desc;
        if (data.ContainsKey("FaceOverride"))
        {
            faceOverride = data.ParseUInt8("FaceOverride", 0);
        }
        else
        {
            faceOverride = null;
        }
        buying = new VendorBuying[data.ParseUInt8("Buying", 0)];
        for (byte b = 0; b < buying.Length; b++)
        {
            data.ParseGuidOrLegacyId("Buying_" + b + "_ID", out var guid, out var legacyId);
            uint newCost = data.ParseUInt32("Buying_" + b + "_Cost");
            INPCCondition[] array = new INPCCondition[data.ParseUInt8("Buying_" + b + "_Conditions", 0)];
            NPCTool.readConditions(data, localization, "Buying_" + b + "_Condition_", array, this);
            NPCRewardsList newRewardsList = default(NPCRewardsList);
            newRewardsList.Parse(data, localization, this, "Buying_" + b + "_Rewards", "Buying_" + b + "_Reward_");
            buying[b] = new VendorBuying(this, b, guid, legacyId, newCost, array, newRewardsList);
        }
        selling = new VendorSellingBase[data.ParseUInt8("Selling", 0)];
        for (byte b2 = 0; b2 < selling.Length; b2++)
        {
            string text = null;
            if (data.ContainsKey("Selling_" + b2 + "_Type"))
            {
                text = data.GetString("Selling_" + b2 + "_Type");
            }
            data.ParseGuidOrLegacyId("Selling_" + b2 + "_ID", out var guid2, out var legacyId2);
            uint newCost2 = data.ParseUInt32("Selling_" + b2 + "_Cost");
            INPCCondition[] array2 = new INPCCondition[data.ParseUInt8("Selling_" + b2 + "_Conditions", 0)];
            NPCTool.readConditions(data, localization, "Selling_" + b2 + "_Condition_", array2, this);
            NPCRewardsList newRewardsList2 = default(NPCRewardsList);
            newRewardsList2.Parse(data, localization, this, "Selling_" + b2 + "_Rewards", "Selling_" + b2 + "_Reward_");
            if (text == null || text.Equals("Item", StringComparison.InvariantCultureIgnoreCase))
            {
                int newSight = data.ParseInt32("Selling_" + b2 + "_Sight", -1);
                int newTactical = data.ParseInt32("Selling_" + b2 + "_Tactical", -1);
                int newGrip = data.ParseInt32("Selling_" + b2 + "_Grip", -1);
                int newBarrel = data.ParseInt32("Selling_" + b2 + "_Barrel", -1);
                int newMagazine = data.ParseInt32("Selling_" + b2 + "_Magazine", -1);
                int newAmmo = data.ParseInt32("Selling_" + b2 + "_Ammo", -1);
                selling[b2] = new VendorSellingItem(this, b2, guid2, legacyId2, newCost2, array2, newRewardsList2, newSight, newTactical, newGrip, newBarrel, newMagazine, newAmmo);
            }
            else
            {
                if (!text.Equals("Vehicle", StringComparison.InvariantCultureIgnoreCase))
                {
                    throw new NotSupportedException("unknown selling type: '" + text + "'");
                }
                string text2 = "Selling_" + b2 + "_Spawnpoint";
                string @string = data.GetString(text2);
                if (string.IsNullOrEmpty(@string))
                {
                    Assets.ReportError(this, "missing \"" + text2 + "\" for vehicle");
                }
                Color32? newPaintColor = null;
                if (data.TryParseColor32RGB("Selling_" + b2 + "_PaintColor", out var value))
                {
                    newPaintColor = value;
                }
                selling[b2] = new VendorSellingVehicle(this, b2, guid2, legacyId2, newCost2, @string, newPaintColor, array2, newRewardsList2);
            }
        }
        enableSorting = !data.ContainsKey("Disable_Sorting");
        currency = data.readAssetReference<ItemCurrencyAsset>("Currency");
    }
}
