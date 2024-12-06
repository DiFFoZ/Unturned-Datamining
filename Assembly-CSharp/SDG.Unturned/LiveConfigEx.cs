namespace SDG.Unturned;

public static class LiveConfigEx
{
    public static bool IsNowFeaturedTimeOrBypassed(this MainMenuWorkshopFeaturedLiveConfig config)
    {
        return config.IsNowFeaturedTime;
    }
}
