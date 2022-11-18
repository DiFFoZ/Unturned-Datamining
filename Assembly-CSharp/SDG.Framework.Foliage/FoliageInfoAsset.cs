using SDG.Framework.IO.FormattedFiles;
using SDG.Framework.Utilities;
using SDG.Unturned;
using UnityEngine;

namespace SDG.Framework.Foliage;

public abstract class FoliageInfoAsset : Asset
{
    protected delegate void BakeFoliageStepHandler(IFoliageSurface surface, Bounds bounds, float surfaceWeight, float collectionWeight);

    public float density;

    public float minNormalPositionOffset;

    public float maxNormalPositionOffset;

    public Vector3 normalRotationOffset;

    public float normalRotationAlignment;

    public float minSurfaceWeight;

    public float maxSurfaceWeight;

    public float minSurfaceAngle;

    public float maxSurfaceAngle;

    public Vector3 minRotation;

    public Vector3 maxRotation;

    public Vector3 minScale;

    public Vector3 maxScale;

    public virtual float randomNormalPositionOffset => Random.Range(minNormalPositionOffset, maxNormalPositionOffset);

    public virtual Quaternion randomRotation => Quaternion.Euler(new Vector3(Random.Range(minRotation.x, maxRotation.x), Random.Range(minRotation.y, maxRotation.y), Random.Range(minRotation.z, maxRotation.z)));

    public virtual Vector3 randomScale => new Vector3(Random.Range(minScale.x, maxScale.x), Random.Range(minScale.y, maxScale.y), Random.Range(minScale.z, maxScale.z));

    public virtual void bakeFoliage(FoliageBakeSettings bakeSettings, IFoliageSurface surface, Bounds bounds, float surfaceWeight, float collectionWeight)
    {
        if (isSurfaceWeightValid(surfaceWeight))
        {
            bakeFoliageSteps(surface, bounds, surfaceWeight, collectionWeight, handleBakeFoliageStep);
        }
    }

    public virtual void addFoliageToSurface(Vector3 surfacePosition, Vector3 surfaceNormal, bool clearWhenBaked, bool followRules)
    {
        if (!followRules || isAngleValid(surfaceNormal))
        {
            Vector3 position = surfacePosition + surfaceNormal * randomNormalPositionOffset;
            if (!followRules || isPositionValid(position))
            {
                Quaternion rotation = Quaternion.Lerp(MathUtility.IDENTITY_QUATERNION, Quaternion.FromToRotation(Vector3.up, surfaceNormal), normalRotationAlignment);
                rotation *= Quaternion.Euler(normalRotationOffset);
                rotation *= randomRotation;
                Vector3 scale = randomScale;
                addFoliage(position, rotation, scale, clearWhenBaked);
            }
        }
    }

    public abstract int getInstanceCountInVolume(IShapeVolume volume);

    protected abstract void addFoliage(Vector3 position, Quaternion rotation, Vector3 scale, bool clearWhenBaked);

    protected virtual void bakeFoliageSteps(IFoliageSurface surface, Bounds bounds, float surfaceWeight, float collectionWeight, BakeFoliageStepHandler callback)
    {
        float num = surfaceWeight * collectionWeight;
        float num2 = bounds.size.x * bounds.size.z / density * num;
        int num3 = Mathf.FloorToInt(num2);
        if (Random.value < num2 - (float)num3)
        {
            num3++;
        }
        for (int i = 0; i < num3; i++)
        {
            callback(surface, bounds, surfaceWeight, collectionWeight);
        }
    }

    protected virtual Vector3 getTestPosition(Bounds bounds)
    {
        float x = Random.Range(-1f, 1f) * bounds.extents.x;
        float z = Random.Range(-1f, 1f) * bounds.extents.z;
        return bounds.center + new Vector3(x, bounds.extents.y, z);
    }

    protected virtual void handleBakeFoliageStep(IFoliageSurface surface, Bounds bounds, float surfaceWeight, float collectionWeight)
    {
        Vector3 testPosition = getTestPosition(bounds);
        if (surface.getFoliageSurfaceInfo(testPosition, out var surfacePosition, out var surfaceNormal))
        {
            addFoliageToSurface(surfacePosition, surfaceNormal, clearWhenBaked: true, followRules: true);
        }
    }

    protected virtual bool isAngleValid(Vector3 surfaceNormal)
    {
        float num = Vector3.Angle(Vector3.up, surfaceNormal);
        if (num >= minSurfaceAngle)
        {
            return num <= maxSurfaceAngle;
        }
        return false;
    }

    protected abstract bool isPositionValid(Vector3 position);

    protected virtual bool isSurfaceWeightValid(float surfaceWeight)
    {
        if (surfaceWeight >= minSurfaceWeight)
        {
            return surfaceWeight <= maxSurfaceWeight;
        }
        return false;
    }

    protected override void readAsset(IFormattedFileReader reader)
    {
        base.readAsset(reader);
        density = reader.readValue<float>("Density");
        minNormalPositionOffset = reader.readValue<float>("Min_Normal_Position_Offset");
        maxNormalPositionOffset = reader.readValue<float>("Max_Normal_Position_Offset");
        normalRotationOffset = reader.readValue<Vector3>("Normal_Rotation_Offset");
        if (reader.containsKey("Normal_Rotation_Alignment"))
        {
            normalRotationAlignment = reader.readValue<float>("Normal_Rotation_Alignment");
        }
        else
        {
            normalRotationAlignment = 1f;
        }
        minSurfaceWeight = reader.readValue<float>("Min_Weight");
        maxSurfaceWeight = reader.readValue<float>("Max_Weight");
        minSurfaceAngle = reader.readValue<float>("Min_Angle");
        maxSurfaceAngle = reader.readValue<float>("Max_Angle");
        minRotation = reader.readValue<Vector3>("Min_Rotation");
        maxRotation = reader.readValue<Vector3>("Max_Rotation");
        minScale = reader.readValue<Vector3>("Min_Scale");
        maxScale = reader.readValue<Vector3>("Max_Scale");
    }

    protected override void writeAsset(IFormattedFileWriter writer)
    {
        base.writeAsset(writer);
        writer.writeValue("Density", density);
        writer.writeValue("Min_Normal_Position_Offset", minNormalPositionOffset);
        writer.writeValue("Max_Normal_Position_Offset", maxNormalPositionOffset);
        writer.writeValue("Normal_Rotation_Offset", normalRotationOffset);
        writer.writeValue("Normal_Rotation_Alignment", normalRotationAlignment);
        writer.writeValue("Min_Weight", minSurfaceWeight);
        writer.writeValue("Max_Weight", maxSurfaceWeight);
        writer.writeValue("Min_Angle", minSurfaceAngle);
        writer.writeValue("Max_Angle", maxSurfaceAngle);
        writer.writeValue("Min_Rotation", minRotation);
        writer.writeValue("Max_Rotation", maxRotation);
        writer.writeValue("Min_Scale", minScale);
        writer.writeValue("Max_Scale", maxScale);
    }

    protected virtual void resetFoliageInfo()
    {
        normalRotationAlignment = 1f;
        maxSurfaceWeight = 1f;
        minScale = Vector3.one;
        maxScale = Vector3.one;
    }

    public FoliageInfoAsset()
    {
        resetFoliageInfo();
    }

    public FoliageInfoAsset(Bundle bundle, Local localization, byte[] hash)
        : base(bundle, localization, hash)
    {
        resetFoliageInfo();
    }
}
