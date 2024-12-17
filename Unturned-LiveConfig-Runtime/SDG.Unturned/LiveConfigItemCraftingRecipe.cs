namespace SDG.Unturned;

public struct LiveConfigItemCraftingRecipe : IDatParseable
{
    public int targetItemDefId;

    public int craftingMaterialsRequired;

    public bool TryParse(IDatNode node)
    {
        if (node is DatDictionary datDictionary)
        {
            targetItemDefId = datDictionary.ParseInt32("ItemDefId");
            craftingMaterialsRequired = datDictionary.ParseInt32("Materials");
            if (targetItemDefId > 0)
            {
                return craftingMaterialsRequired > 0;
            }
            return false;
        }
        return false;
    }
}
