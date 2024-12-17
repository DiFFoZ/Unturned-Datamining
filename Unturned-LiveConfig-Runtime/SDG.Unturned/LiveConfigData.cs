namespace SDG.Unturned;

public class LiveConfigData
{
    public MainMenuAlert mainMenuAlert = new MainMenuAlert();

    public MainMenuWorkshopLiveConfig mainMenuWorkshop = new MainMenuWorkshopLiveConfig();

    public ItemStoreLiveConfig itemStore = new ItemStoreLiveConfig();

    public ItemCraftingLiveConfigRecipe itemCrafting = new ItemCraftingLiveConfigRecipe();

    public LinkFilteringLiveConfig linkFiltering = new LinkFilteringLiveConfig();

    public bool shouldAllowJoiningInternetServersWithoutGslt;

    public bool shouldServersWithoutMonetizationTagBeVisibleInInternetServerList;

    public int playtimeGeneratorItemDefId;

    public int queryPingWarningOffsetMs = 200;

    public long craftingPromotionId = -1L;

    public void Parse(DatDictionary data)
    {
        if (data.TryGetDictionary("MainMenuAlert", out var node))
        {
            mainMenuAlert.Parse(node);
        }
        if (data.TryGetDictionary("MainMenuWorkshop", out var node2))
        {
            mainMenuWorkshop.Parse(node2);
        }
        if (data.TryGetDictionary("ItemStore", out var node3))
        {
            itemStore.Parse(node3);
        }
        if (data.TryGetDictionary("ItemCrafting", out var node4))
        {
            itemCrafting.Parse(node4);
        }
        if (data.TryGetDictionary("LinkFiltering", out var node5))
        {
            linkFiltering.Parse(node5);
        }
        shouldAllowJoiningInternetServersWithoutGslt = data.ParseBool("ShouldAllowJoiningInternetServersWithoutGslt");
        shouldServersWithoutMonetizationTagBeVisibleInInternetServerList = data.ParseBool("ShouldServersWithoutMonetizationTagBeVisibleInInternetServerList");
        playtimeGeneratorItemDefId = data.ParseInt32("PlaytimeGeneratorItemDefId");
        queryPingWarningOffsetMs = data.ParseInt32("QueryPingWarningOffsetMs", 200);
        craftingPromotionId = data.ParseInt64("CraftingPromotionId", -1L);
    }
}
