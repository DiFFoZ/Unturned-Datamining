using SDG.Framework.IO.FormattedFiles;
using SDG.Framework.Utilities;
using SDG.Unturned;
using UnityEngine;

namespace SDG.Framework.Foliage;

public class FoliageResourceInfoAsset : FoliageInfoAsset
{
    private static readonly Collider[] OBSTRUCTION_COLLIDERS = new Collider[16];

    public AssetReference<ResourceAsset> resource;

    public float obstructionRadius;

    public override void bakeFoliage(FoliageBakeSettings bakeSettings, IFoliageSurface surface, Bounds bounds, float surfaceWeight, float collectionWeight)
    {
        if (bakeSettings.bakeResources && !bakeSettings.bakeClear)
        {
            base.bakeFoliage(bakeSettings, surface, bounds, surfaceWeight, collectionWeight);
        }
    }

    public override int getInstanceCountInVolume(IShapeVolume volume)
    {
        Bounds worldBounds = volume.worldBounds;
        RegionBounds regionBounds = new RegionBounds(worldBounds);
        int num = 0;
        for (byte b = regionBounds.min.x; b <= regionBounds.max.x; b = (byte)(b + 1))
        {
            for (byte b2 = regionBounds.min.y; b2 <= regionBounds.max.y; b2 = (byte)(b2 + 1))
            {
                foreach (ResourceSpawnpoint item in LevelGround.trees[b, b2])
                {
                    if (resource.isReferenceTo(item.asset) && volume.containsPoint(item.point))
                    {
                        num++;
                    }
                }
            }
        }
        return num;
    }

    protected override void addFoliage(Vector3 position, Quaternion rotation, Vector3 scale, bool clearWhenBaked)
    {
        ResourceAsset resourceAsset = Assets.find(resource);
        if (resourceAsset != null)
        {
            LevelGround.addSpawn(position, resourceAsset.id, clearWhenBaked);
        }
    }

    protected override bool isPositionValid(Vector3 position)
    {
        if (!VolumeManager<FoliageVolume, FoliageVolumeManager>.Get().IsPositionBakeable(position, instancedMeshes: false, resources: true, objects: false))
        {
            return false;
        }
        int num = Physics.OverlapSphereNonAlloc(position, obstructionRadius, OBSTRUCTION_COLLIDERS, RayMasks.BLOCK_RESOURCE);
        for (int i = 0; i < num; i++)
        {
            ObjectAsset asset = LevelObjects.getAsset(OBSTRUCTION_COLLIDERS[i].transform);
            if (asset != null && !asset.isSnowshoe)
            {
                return false;
            }
        }
        return true;
    }

    protected override void readAsset(IFormattedFileReader reader)
    {
        base.readAsset(reader);
        resource = reader.readValue<AssetReference<ResourceAsset>>("Resource");
        if (reader.containsKey("Obstruction_Radius"))
        {
            obstructionRadius = reader.readValue<float>("Obstruction_Radius");
        }
    }

    protected override void writeAsset(IFormattedFileWriter writer)
    {
        base.writeAsset(writer);
        writer.writeValue("Resource", resource);
        writer.writeValue("Obstruction_Radius", obstructionRadius);
    }

    protected virtual void resetResource()
    {
        obstructionRadius = 4f;
    }

    public FoliageResourceInfoAsset()
    {
        resetResource();
    }

    public FoliageResourceInfoAsset(Bundle bundle, Local localization, byte[] hash)
        : base(bundle, localization, hash)
    {
        resetResource();
    }
}
