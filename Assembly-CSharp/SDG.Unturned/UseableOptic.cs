using System;
using UnityEngine;

namespace SDG.Unturned;

public class UseableOptic : Useable
{
    private bool isZoomed;

    public override bool startPrimary()
    {
        if (base.channel.IsLocalPlayer && isZoomed)
        {
            if (Physics.Raycast(new Ray(base.player.look.aim.position, base.player.look.aim.forward), out var hitInfo, 2048f, RayMasks.DAMAGE_CLIENT))
            {
                base.player.quests.sendSetMarker(newIsMarkerPlaced: true, hitInfo.point);
            }
            return true;
        }
        return false;
    }

    public override bool startSecondary()
    {
        if (base.channel.IsLocalPlayer && !isZoomed && base.player.look.perspective == EPlayerPerspective.FIRST)
        {
            isZoomed = true;
            startZoom();
            return true;
        }
        return false;
    }

    public override void stopSecondary()
    {
        if (base.channel.IsLocalPlayer && isZoomed)
        {
            isZoomed = false;
            stopZoom();
        }
    }

    private void startZoom()
    {
        base.player.animator.viewmodelCameraLocalPositionOffset = Vector3.up;
        base.player.animator.viewmodelSwayMultiplier = 0f;
        base.player.look.enableZoom(((ItemOpticAsset)base.player.equipment.asset).zoom);
        base.player.look.shouldUseZoomFactorForSensitivity = true;
        PlayerUI.updateBinoculars(isBinoculars: true);
    }

    private void stopZoom()
    {
        base.player.animator.viewmodelCameraLocalPositionOffset = Vector3.zero;
        base.player.animator.viewmodelSwayMultiplier = 1f;
        base.player.look.disableZoom();
        base.player.look.shouldUseZoomFactorForSensitivity = false;
        PlayerUI.updateBinoculars(isBinoculars: false);
    }

    private void onPerspectiveUpdated(EPlayerPerspective newPerspective)
    {
        if (isZoomed && newPerspective == EPlayerPerspective.THIRD)
        {
            stopZoom();
        }
    }

    public override void equip()
    {
        base.player.animator.play("Equip", smooth: true);
        if (base.channel.IsLocalPlayer)
        {
            PlayerLook look = base.player.look;
            look.onPerspectiveUpdated = (PerspectiveUpdated)Delegate.Combine(look.onPerspectiveUpdated, new PerspectiveUpdated(onPerspectiveUpdated));
        }
    }

    public override void dequip()
    {
        if (base.channel.IsLocalPlayer)
        {
            if (isZoomed)
            {
                stopZoom();
            }
            PlayerLook look = base.player.look;
            look.onPerspectiveUpdated = (PerspectiveUpdated)Delegate.Remove(look.onPerspectiveUpdated, new PerspectiveUpdated(onPerspectiveUpdated));
        }
    }
}
