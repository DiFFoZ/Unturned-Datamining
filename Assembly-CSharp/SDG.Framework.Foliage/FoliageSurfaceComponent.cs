using SDG.Unturned;
using UnityEngine;

namespace SDG.Framework.Foliage;

public class FoliageSurfaceComponent : MonoBehaviour, IFoliageSurface
{
    public AssetReference<FoliageInfoCollectionAsset> foliage;

    public Collider surfaceCollider;

    protected bool isRegistered;

    /// <summary>
    /// Nelson 2024-11-11: Collider may have been destroyed by an unexpected mod script configuration (or perhaps
    /// simply missing in the first place). Should fix/prevent public issue #4749.
    /// </summary>
    public bool IsValidFoliageSurface => surfaceCollider != null;

    public FoliageBounds getFoliageSurfaceBounds()
    {
        bool activeSelf = base.gameObject.activeSelf;
        if (!activeSelf)
        {
            base.gameObject.SetActive(value: true);
        }
        FoliageBounds result = new FoliageBounds(surfaceCollider.bounds);
        if (!activeSelf)
        {
            base.gameObject.SetActive(value: false);
        }
        return result;
    }

    public bool getFoliageSurfaceInfo(Vector3 position, out Vector3 surfacePosition, out Vector3 surfaceNormal)
    {
        if (surfaceCollider.Raycast(new Ray(position, Vector3.down), out var hitInfo, 1024f))
        {
            surfacePosition = hitInfo.point;
            surfaceNormal = hitInfo.normal;
            return true;
        }
        surfacePosition = Vector3.zero;
        surfaceNormal = Vector3.up;
        return false;
    }

    public void bakeFoliageSurface(FoliageBakeSettings bakeSettings, FoliageTile foliageTile)
    {
        FoliageInfoCollectionAsset foliageInfoCollectionAsset = Assets.find(foliage);
        if (foliageInfoCollectionAsset != null)
        {
            bool activeSelf = base.gameObject.activeSelf;
            if (!activeSelf)
            {
                base.gameObject.SetActive(value: true);
            }
            Bounds worldBounds = foliageTile.worldBounds;
            Vector3 min = worldBounds.min;
            Vector3 max = worldBounds.max;
            Bounds bounds = surfaceCollider.bounds;
            Vector3 min2 = bounds.min;
            Vector3 max2 = bounds.max;
            foliageInfoCollectionAsset.bakeFoliage(bakeSettings, this, new Bounds
            {
                min = new Vector3(Mathf.Max(min.x, min2.x), min2.y, Mathf.Max(min.z, min2.z)),
                max = new Vector3(Mathf.Min(max.x, max2.x), max2.y, Mathf.Min(max.z, max2.z))
            }, 1f);
            if (!activeSelf)
            {
                base.gameObject.SetActive(value: false);
            }
        }
    }

    protected void OnEnable()
    {
        if (!isRegistered)
        {
            isRegistered = true;
            FoliageSystem.addSurface(this);
        }
    }

    protected void OnDestroy()
    {
        if (isRegistered)
        {
            isRegistered = false;
            FoliageSystem.removeSurface(this);
        }
    }
}
