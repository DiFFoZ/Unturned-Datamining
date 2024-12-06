namespace SDG.Unturned;

public class InteractableObject : InteractablePower
{
    protected ObjectAsset _objectAsset;

    internal LevelObject owningLevelObject;

    public ObjectAsset objectAsset => _objectAsset;

    public override void updateState(Asset asset, byte[] state)
    {
        base.updateState(asset, state);
        _objectAsset = asset as ObjectAsset;
    }

    private void Start()
    {
        RefreshIsConnectedToPower();
    }
}
