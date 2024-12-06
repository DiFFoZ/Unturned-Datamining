using UnityEngine;

namespace SDG.Framework.Foliage;

public interface IFoliageSurface
{
    /// <returns>True if other IFoliageSurface methods can be called.</returns>
    bool IsValidFoliageSurface { get; }

    FoliageBounds getFoliageSurfaceBounds();

    bool getFoliageSurfaceInfo(Vector3 position, out Vector3 surfacePosition, out Vector3 surfaceNormal);

    void bakeFoliageSurface(FoliageBakeSettings bakeSettings, FoliageTile foliageTile);
}
