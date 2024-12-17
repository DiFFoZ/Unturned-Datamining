namespace SDG.Unturned;

public class ItemCraftingLiveConfigRecipe
{
    public LiveConfigItemCraftingRecipe[] recipes;

    public void Parse(DatDictionary data)
    {
        recipes = data.ParseArrayOfStructs<LiveConfigItemCraftingRecipe>("Recipes");
        if (recipes == null)
        {
            recipes = new LiveConfigItemCraftingRecipe[0];
        }
    }
}
