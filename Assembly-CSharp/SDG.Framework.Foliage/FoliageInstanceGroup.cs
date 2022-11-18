using SDG.Unturned;
using UnityEngine;

namespace SDG.Framework.Foliage;

public struct FoliageInstanceGroup
{
    public AssetReference<FoliageInstancedMeshInfoAsset> assetReference;

    public Matrix4x4 matrix;

    public bool clearWhenBaked;

    public FoliageInstanceGroup(AssetReference<FoliageInstancedMeshInfoAsset> newAssetReference, Matrix4x4 newMatrix, bool newClearWhenBaked)
    {
        assetReference = newAssetReference;
        matrix = newMatrix;
        clearWhenBaked = newClearWhenBaked;
    }
}
