namespace SDG.Unturned;

public enum EServerMonetizationTag
{
    /// <summary>
    /// Host has not specified a value.
    /// </summary>
    Unspecified,
    /// <summary>
    /// Not an actual tag. Used for filtering.
    /// </summary>
    Any,
    /// <summary>
    /// Host has specified that the server does not sell anything for real money.
    /// </summary>
    None,
    /// <summary>
    /// Host has specified that the server does have a real money shop, but does not sell anything which affects gameplay.
    /// </summary>
    NonGameplay,
    /// <summary>
    /// Host has specified that the server does have a real money shop which sells benefits that affect gameplay.
    /// </summary>
    Monetized
}
