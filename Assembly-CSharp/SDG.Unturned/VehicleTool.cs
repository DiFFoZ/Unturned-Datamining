using System;
using System.Collections.Generic;
using UnityEngine;

namespace SDG.Unturned;

public class VehicleTool : MonoBehaviour
{
    private static Queue<VehicleIconInfo> icons;

    /// <summary>
    /// Handles VehicleRedirectorAsset (if any) and returns actual vehicle asset (if any).
    /// </summary>
    public static VehicleAsset FindVehicleByLegacyIdAndHandleRedirects(ushort legacyId)
    {
        Asset asset = Assets.find(EAssetType.VEHICLE, legacyId);
        if (asset is VehicleRedirectorAsset { TargetVehicle: var targetVehicle })
        {
            asset = targetVehicle.Find();
        }
        return asset as VehicleAsset;
    }

    /// <summary>
    /// Handles VehicleRedirectorAsset returning load paint color override (if any) and returns actual vehicle asset (if any).
    /// </summary>
    public static VehicleAsset FindVehicleByLegacyIdAndHandleRedirectsWithLoadColor(ushort legacyId, out Color32 paintColor)
    {
        paintColor = new Color32(0, 0, 0, 0);
        Asset asset = Assets.find(EAssetType.VEHICLE, legacyId);
        if (asset is VehicleRedirectorAsset { LoadPaintColor: var loadPaintColor } vehicleRedirectorAsset)
        {
            if (loadPaintColor.HasValue)
            {
                paintColor = vehicleRedirectorAsset.LoadPaintColor.Value;
            }
            asset = vehicleRedirectorAsset.TargetVehicle.Find();
        }
        return asset as VehicleAsset;
    }

    /// <summary>
    /// Handles VehicleRedirectorAsset returning spawn paint color override (if any) and returns actual vehicle asset (if any).
    /// </summary>
    public static VehicleAsset FindVehicleByLegacyIdAndHandleRedirectsWithSpawnColor(ushort legacyId, out Color32 paintColor)
    {
        paintColor = new Color32(0, 0, 0, 0);
        Asset asset = Assets.find(EAssetType.VEHICLE, legacyId);
        if (asset is VehicleRedirectorAsset { SpawnPaintColor: var spawnPaintColor } vehicleRedirectorAsset)
        {
            if (spawnPaintColor.HasValue)
            {
                paintColor = vehicleRedirectorAsset.SpawnPaintColor.Value;
            }
            asset = vehicleRedirectorAsset.TargetVehicle.Find();
        }
        return asset as VehicleAsset;
    }

    /// <summary>
    /// Handles VehicleRedirectorAsset (if any) and returns actual vehicle asset (if any).
    /// </summary>
    public static VehicleAsset FindVehicleByGuidAndHandleRedirects(Guid guid)
    {
        Asset asset = Assets.find(guid);
        if (asset is VehicleRedirectorAsset { TargetVehicle: var targetVehicle })
        {
            asset = targetVehicle.Find();
        }
        return asset as VehicleAsset;
    }

    /// <summary>
    /// Handles VehicleRedirectorAsset returning load paint color override (if any) and returns actual vehicle asset (if any).
    /// </summary>
    public static VehicleAsset FindVehicleByGuidAndHandleRedirectsWithLoadColor(Guid guid, out Color32 paintColor)
    {
        paintColor = new Color32(0, 0, 0, 0);
        Asset asset = Assets.find(guid);
        if (asset is VehicleRedirectorAsset { LoadPaintColor: var loadPaintColor } vehicleRedirectorAsset)
        {
            if (loadPaintColor.HasValue)
            {
                paintColor = vehicleRedirectorAsset.LoadPaintColor.Value;
            }
            asset = vehicleRedirectorAsset.TargetVehicle.Find();
        }
        return asset as VehicleAsset;
    }

    /// <summary>
    /// Handles VehicleRedirectorAsset returning spawn paint color override (if any) and returns actual vehicle asset (if any).
    /// </summary>
    public static VehicleAsset FindVehicleByGuidAndHandleRedirectsWithSpawnColor(Guid guid, out Color32 paintColor)
    {
        paintColor = new Color32(0, 0, 0, 0);
        Asset asset = Assets.find(guid);
        if (asset is VehicleRedirectorAsset { SpawnPaintColor: var spawnPaintColor } vehicleRedirectorAsset)
        {
            if (spawnPaintColor.HasValue)
            {
                paintColor = vehicleRedirectorAsset.SpawnPaintColor.Value;
            }
            asset = vehicleRedirectorAsset.TargetVehicle.Find();
        }
        return asset as VehicleAsset;
    }

    /// <summary>
    /// Handles VehicleRedirectorAsset (if any) and returns actual vehicle asset (if any).
    /// </summary>
    public static VehicleAsset HandleRedirects(Asset asset)
    {
        if (asset is VehicleRedirectorAsset { TargetVehicle: var targetVehicle })
        {
            asset = targetVehicle.Find();
        }
        return asset as VehicleAsset;
    }

    /// <summary>
    /// Handles VehicleRedirectorAsset returning load paint color override (if any) and returns actual vehicle asset (if any).
    /// </summary>
    public static VehicleAsset HandleRedirectsWithLoadColor(Asset asset, out Color32 paintColor)
    {
        paintColor = new Color32(0, 0, 0, 0);
        if (asset is VehicleRedirectorAsset { LoadPaintColor: var loadPaintColor } vehicleRedirectorAsset)
        {
            if (loadPaintColor.HasValue)
            {
                paintColor = vehicleRedirectorAsset.LoadPaintColor.Value;
            }
            asset = vehicleRedirectorAsset.TargetVehicle.Find();
        }
        return asset as VehicleAsset;
    }

    /// <summary>
    /// Handles VehicleRedirectorAsset returning spawn paint color override (if any) and returns actual vehicle asset (if any).
    /// </summary>
    public static VehicleAsset HandleRedirectsWithSpawnColor(Asset asset, out Color32 paintColor)
    {
        paintColor = new Color32(0, 0, 0, 0);
        if (asset is VehicleRedirectorAsset { SpawnPaintColor: var spawnPaintColor } vehicleRedirectorAsset)
        {
            if (spawnPaintColor.HasValue)
            {
                paintColor = vehicleRedirectorAsset.SpawnPaintColor.Value;
            }
            asset = vehicleRedirectorAsset.TargetVehicle.Find();
        }
        return asset as VehicleAsset;
    }

    public static Transform getVehicle(ushort id, ushort skin, ushort mythic, VehicleAsset vehicleAsset, SkinAsset skinAsset)
    {
        GameObject gameObject = vehicleAsset?.GetOrLoadModel();
        if (gameObject != null)
        {
            if (id != vehicleAsset.id)
            {
                UnturnedLog.error("ID and asset ID are not in sync!");
            }
            Transform transform = UnityEngine.Object.Instantiate(gameObject).transform;
            transform.name = id.ToString();
            if (skinAsset != null)
            {
                InteractableVehicle interactableVehicle = transform.gameObject.AddComponent<InteractableVehicle>();
                interactableVehicle.id = id;
                interactableVehicle.skinID = skin;
                interactableVehicle.mythicID = mythic;
                interactableVehicle.fuel = 10000;
                interactableVehicle.isExploded = false;
                interactableVehicle.health = 10000;
                interactableVehicle.batteryCharge = 10000;
                interactableVehicle.safeInit(vehicleAsset);
                interactableVehicle.updateFires();
                interactableVehicle.updateSkin();
            }
            return transform;
        }
        Transform obj = new GameObject().transform;
        obj.name = id.ToString();
        obj.tag = "Vehicle";
        obj.gameObject.layer = 26;
        return obj;
    }

    public static void getIcon(ushort id, ushort skin, VehicleAsset vehicleAsset, SkinAsset skinAsset, int x, int y, bool readableOnCPU, VehicleIconReady callback)
    {
        if (vehicleAsset != null && id != vehicleAsset.id)
        {
            UnturnedLog.error("ID and vehicle asset ID are not in sync!");
        }
        if (skinAsset != null && skin != skinAsset.id)
        {
            UnturnedLog.error("ID and skin asset ID are not in sync!");
        }
        VehicleIconInfo vehicleIconInfo = new VehicleIconInfo();
        vehicleIconInfo.id = id;
        vehicleIconInfo.skin = skin;
        vehicleIconInfo.vehicleAsset = vehicleAsset;
        vehicleIconInfo.skinAsset = skinAsset;
        vehicleIconInfo.x = x;
        vehicleIconInfo.y = y;
        vehicleIconInfo.readableOnCPU = readableOnCPU;
        vehicleIconInfo.callback = callback;
        icons.Enqueue(vehicleIconInfo);
    }

    internal static Vector3 GetPositionForVehicle(Player player)
    {
        Vector3 vector = player.transform.position + player.transform.forward * 6f;
        Physics.Raycast(vector + Vector3.up * 16f, Vector3.down, out var hitInfo, 32f, RayMasks.BLOCK_VEHICLE);
        if (hitInfo.collider != null)
        {
            vector.y = hitInfo.point.y + 16f;
        }
        return vector;
    }

    /// <summary>
    /// Supports redirects by VehicleRedirectorAsset. If redirector's SpawnPaintColor is set, that color is used.
    /// </summary>
    public static InteractableVehicle SpawnVehicleForPlayer(Player player, Asset asset)
    {
        if (player == null || asset == null)
        {
            return null;
        }
        Vector3 positionForVehicle = GetPositionForVehicle(player);
        return VehicleManager.spawnVehicleV2(asset, positionForVehicle, player.transform.rotation);
    }

    /// <summary>
    /// Supports redirects by VehicleRedirectorAsset. If redirector's SpawnPaintColor is set, that color is used.
    /// </summary>
    /// <returns>true if matching vehicle asset was found. (Not necessarily whether vehicle was spawned.)</returns>
    public static bool giveVehicle(Player player, ushort id)
    {
        if (FindVehicleByLegacyIdAndHandleRedirects(id) != null)
        {
            Vector3 positionForVehicle = GetPositionForVehicle(player);
            VehicleManager.spawnVehicleV2(id, positionForVehicle, player.transform.rotation);
            return true;
        }
        return false;
    }

    private void Update()
    {
        if (icons == null || icons.Count == 0)
        {
            return;
        }
        VehicleIconInfo vehicleIconInfo = icons.Dequeue();
        if (vehicleIconInfo != null && vehicleIconInfo.vehicleAsset != null)
        {
            Transform vehicle = getVehicle(vehicleIconInfo.id, vehicleIconInfo.skin, 0, vehicleIconInfo.vehicleAsset, vehicleIconInfo.skinAsset);
            vehicle.position = new Vector3(-256f, -256f, 0f);
            Transform transform = vehicle.Find("Icon2");
            if (transform == null)
            {
                UnityEngine.Object.Destroy(vehicle.gameObject);
                Assets.ReportError(vehicleIconInfo.vehicleAsset, "missing 'Icon2' Transform");
            }
            else
            {
                float size2_z = vehicleIconInfo.vehicleAsset.size2_z;
                Texture2D texture = ItemTool.captureIcon(vehicleIconInfo.id, vehicleIconInfo.skin, vehicle, transform, vehicleIconInfo.x, vehicleIconInfo.y, size2_z, vehicleIconInfo.readableOnCPU);
                vehicleIconInfo.callback?.Invoke(texture);
            }
        }
    }

    private void Start()
    {
        icons = new Queue<VehicleIconInfo>();
    }
}
