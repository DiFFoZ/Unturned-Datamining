using SDG.NetTransport;
using UnityEngine;

namespace SDG.Unturned;

public class InteractableOxygenator : InteractablePower
{
    private bool _isPowered;

    private Transform engine;

    private OxygenBubble bubble;

    internal static readonly ClientInstanceMethod<bool> SendPowered = ClientInstanceMethod<bool>.Get(typeof(InteractableOxygenator), "ReceivePowered");

    private static readonly ServerInstanceMethod<bool> SendToggleRequest = ServerInstanceMethod<bool>.Get(typeof(InteractableOxygenator), "ReceiveToggleRequest");

    public bool isPowered => _isPowered;

    private void UpdateEngine()
    {
        if (engine != null)
        {
            engine.gameObject.SetActive(base.isWired && isPowered);
        }
    }

    protected override void updateWired()
    {
        UpdateEngine();
        updateBubble();
    }

    public void updatePowered(bool newPowered)
    {
        if (_isPowered != newPowered)
        {
            _isPowered = newPowered;
            UpdateEngine();
            updateBubble();
        }
    }

    private void updateBubble()
    {
        if (base.isWired && isPowered)
        {
            registerBubble();
        }
        else
        {
            deregisterBubble();
        }
    }

    public override void updateState(Asset asset, byte[] state)
    {
        base.updateState(asset, state);
        _isPowered = state[0] == 1;
        engine = base.transform.Find("Engine");
        RefreshIsConnectedToPowerWithoutNotify();
        UpdateEngine();
        updateBubble();
    }

    public override void use()
    {
        ClientToggle();
    }

    public override bool checkHint(out EPlayerMessage message, out string text, out Color color)
    {
        if (isPowered)
        {
            message = EPlayerMessage.SPOT_OFF;
        }
        else
        {
            message = EPlayerMessage.SPOT_ON;
        }
        text = "";
        color = Color.white;
        return true;
    }

    private void registerBubble()
    {
        if (bubble == null)
        {
            bubble = OxygenManager.registerBubble(base.transform, 24f);
        }
    }

    private void deregisterBubble()
    {
        if (bubble != null)
        {
            OxygenManager.deregisterBubble(bubble);
            bubble = null;
        }
    }

    private void OnDisable()
    {
        deregisterBubble();
    }

    [SteamCall(ESteamCallValidation.ONLY_FROM_SERVER, deferMode = ENetInvocationDeferMode.Queue)]
    public void ReceivePowered(bool newPowered)
    {
        updatePowered(newPowered);
    }

    public void ClientToggle()
    {
        SendToggleRequest.Invoke(GetNetId(), ENetReliability.Unreliable, !isPowered);
    }

    [SteamCall(ESteamCallValidation.SERVERSIDE, ratelimitHz = 2)]
    public void ReceiveToggleRequest(in ServerInvocationContext context, bool desiredPowered)
    {
        if (isPowered != desiredPowered && BarricadeManager.tryGetRegion(base.transform, out var x, out var y, out var plant, out var region))
        {
            Player player = context.GetPlayer();
            if (!(player == null) && !player.life.isDead && !((base.transform.position - player.transform.position).sqrMagnitude > 400f))
            {
                BarricadeManager.ServerSetOxygenatorPoweredInternal(this, x, y, plant, region, !isPowered);
                EffectManager.TriggerFiremodeEffect(base.transform.position);
            }
        }
    }
}
