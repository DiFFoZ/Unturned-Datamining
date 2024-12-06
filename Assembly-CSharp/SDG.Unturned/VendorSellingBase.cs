using System;

namespace SDG.Unturned;

public abstract class VendorSellingBase : VendorElement
{
    public bool canBuy(Player player)
    {
        if (base.outerAsset.currency.isValid)
        {
            ItemCurrencyAsset itemCurrencyAsset = base.outerAsset.currency.Find();
            if (itemCurrencyAsset == null)
            {
                Assets.ReportError(base.outerAsset, "missing currency asset");
                return false;
            }
            return itemCurrencyAsset.canAfford(player, base.cost);
        }
        return player.skills.experience >= base.cost;
    }

    public virtual void buy(Player player)
    {
        if (base.outerAsset.currency.isValid)
        {
            ItemCurrencyAsset itemCurrencyAsset = base.outerAsset.currency.Find();
            if (itemCurrencyAsset == null)
            {
                Assets.ReportError(base.outerAsset, "missing currency asset");
            }
            else if (!itemCurrencyAsset.spendValue(player, base.cost))
            {
                UnturnedLog.error("Spending {0} currency at vendor went wrong (this should never happen)", base.cost);
            }
        }
        else
        {
            player.skills.askSpend(base.cost);
        }
    }

    public virtual void format(Player player, out ushort total)
    {
        total = 0;
    }

    public VendorSellingBase(VendorAsset newOuterAsset, byte newIndex, Guid newTargetAssetGuid, ushort newLegacyAssetId, uint newCost, INPCCondition[] newConditions, NPCRewardsList newRewardsList)
        : base(newOuterAsset, newIndex, newTargetAssetGuid, newLegacyAssetId, newCost, newConditions, newRewardsList)
    {
    }
}
