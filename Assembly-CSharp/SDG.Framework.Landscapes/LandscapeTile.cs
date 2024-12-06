using System.Diagnostics;
using System.Globalization;
using System.IO;
using SDG.Framework.Debug;
using SDG.Framework.Foliage;
using SDG.Framework.IO.FormattedFiles;
using SDG.Framework.Utilities;
using SDG.Unturned;
using UnityEngine;
using UnityEngine.Rendering;

namespace SDG.Framework.Landscapes;

public class LandscapeTile : IFormattedFileReadable, IFormattedFileWritable, IFoliageSurface
{
    protected LandscapeCoord _coord;

    public float[,] heightmap;

    public float[,,] splatmap;

    /// <summary>
    /// True is solid and false is empty.
    /// </summary>
    public bool[,] holes;

    /// <summary>
    /// Marked true when level editor or legacy hole volumes modify hole data.
    /// Defaults to false in which case holes do not need to be saved.
    ///
    /// Initially this was not going to be marked by hole volumes because they can re-generate the holes, but saving
    /// hole volume cuts is helpful when upgrading to remove hole volumes from a map.
    /// </summary>
    public bool hasAnyHolesData;

    private TerrainLayer[] terrainLayers;

    /// <summary>
    /// Heightmap-only data used in level editor. Refer to Landscape.DisableHoleColliders for explanation.
    /// </summary>
    public TerrainData dataWithoutHoles;

    private static AssetReference<LandscapeMaterialAsset> DEFAULT_MATERIAL = new AssetReference<LandscapeMaterialAsset>("498ca625072d443a876b2a4f11896018");

    public GameObject gameObject { get; protected set; }

    public LandscapeCoord coord
    {
        get
        {
            return _coord;
        }
        protected set
        {
            _coord = value;
            updateTransform();
        }
    }

    public Bounds localBounds => new Bounds(new Vector3(Landscape.TILE_SIZE / 2f, 0f, Landscape.TILE_SIZE / 2f), new Vector3(Landscape.TILE_SIZE, Landscape.TILE_HEIGHT, Landscape.TILE_SIZE));

    public Bounds worldBounds
    {
        get
        {
            Bounds result = localBounds;
            result.center += new Vector3((float)coord.x * Landscape.TILE_SIZE, 0f, (float)coord.y * Landscape.TILE_SIZE);
            return result;
        }
    }

    public InspectableList<AssetReference<LandscapeMaterialAsset>> materials { get; protected set; }

    public TerrainData data { get; protected set; }

    public Terrain terrain { get; protected set; }

    public TerrainCollider collider { get; protected set; }

    public bool IsValidFoliageSurface => true;

    public void SetHeightsDelayLOD()
    {
        data.SetHeightsDelayLOD(0, 0, heightmap);
        if (dataWithoutHoles != null)
        {
            dataWithoutHoles.SetHeightsDelayLOD(0, 0, heightmap);
        }
    }

    public void SyncHeightmap()
    {
        data.SyncHeightmap();
        if (dataWithoutHoles != null)
        {
            dataWithoutHoles.SyncHeightmap();
        }
    }

    public virtual void read(IFormattedFileReader reader)
    {
        reader = reader.readObject();
        coord = reader.readValue<LandscapeCoord>("Coord");
        int num = reader.readArrayLength("Materials");
        for (int i = 0; i < num; i++)
        {
            AssetReference<LandscapeMaterialAsset> value = reader.readValue<AssetReference<LandscapeMaterialAsset>>(i);
            if (value.isValid)
            {
                if (Level.shouldUseHolidayRedirects)
                {
                    LandscapeMaterialAsset landscapeMaterialAsset = value.Find();
                    if (landscapeMaterialAsset != null)
                    {
                        AssetReference<LandscapeMaterialAsset> holidayRedirect = landscapeMaterialAsset.getHolidayRedirect();
                        if (holidayRedirect.isValid)
                        {
                            value = holidayRedirect;
                        }
                    }
                }
                if (value.Find() == null)
                {
                    ClientAssetIntegrity.ServerAddKnownMissingAsset(value.GUID, $"Landscape tile (x: {coord.x} y: {coord.y} layer: {i})");
                }
            }
            materials[i] = value;
        }
        updatePrototypes();
        readHeightmaps();
        readSplatmaps();
        ReadHoles();
    }

    public virtual void readHeightmaps()
    {
        readHeightmap("_Source", heightmap);
        SetHeightsDelayLOD();
    }

    protected virtual void readHeightmap(string suffix, float[,] heightmap)
    {
        string text = "Tile_" + coord.x.ToString(CultureInfo.InvariantCulture) + "_" + coord.y.ToString(CultureInfo.InvariantCulture) + suffix + ".heightmap";
        string text2 = Level.info.path + "/Landscape/Heightmaps/" + text;
        if (!File.Exists(text2))
        {
            UnturnedLog.warn("LandscapeTile missing heightmap file: {0}", text2);
            return;
        }
        using FileStream underlyingStream = new FileStream(text2, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using SHA1Stream sHA1Stream = new SHA1Stream(underlyingStream);
        for (int i = 0; i < Landscape.HEIGHTMAP_RESOLUTION; i++)
        {
            for (int j = 0; j < Landscape.HEIGHTMAP_RESOLUTION; j++)
            {
                float num = (float)(int)(ushort)((sHA1Stream.ReadByte() << 8) | sHA1Stream.ReadByte()) / 65535f;
                heightmap[i, j] = num;
            }
        }
        Level.includeHash(text, sHA1Stream.Hash);
    }

    public virtual void readSplatmaps()
    {
        readSplatmap("_Source", splatmap);
        if (!Dedicator.IsDedicatedServer)
        {
            data.SetAlphamaps(0, 0, splatmap);
        }
    }

    protected virtual void readSplatmap(string suffix, float[,,] splatmap)
    {
        string text = "Tile_" + coord.x.ToString(CultureInfo.InvariantCulture) + "_" + coord.y.ToString(CultureInfo.InvariantCulture) + suffix + ".splatmap";
        string text2 = Level.info.path + "/Landscape/Splatmaps/" + text;
        if (!File.Exists(text2))
        {
            UnturnedLog.warn("LandscapeTile missing splatmap file: {0}", text2);
            return;
        }
        using FileStream underlyingStream = new FileStream(text2, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using SHA1Stream sHA1Stream = new SHA1Stream(underlyingStream);
        for (int i = 0; i < Landscape.SPLATMAP_RESOLUTION; i++)
        {
            for (int j = 0; j < Landscape.SPLATMAP_RESOLUTION; j++)
            {
                for (int k = 0; k < Landscape.SPLATMAP_LAYERS; k++)
                {
                    float num = (float)(int)(byte)sHA1Stream.ReadByte() / 255f;
                    splatmap[i, j, k] = num;
                }
            }
        }
        Level.includeHash(text, sHA1Stream.Hash);
    }

    private void ReadHoles()
    {
        string text = "Tile_" + coord.x.ToString(CultureInfo.InvariantCulture) + "_" + coord.y.ToString(CultureInfo.InvariantCulture) + ".bin";
        string path = Level.info.path + "/Landscape/Holes/" + text;
        if (!File.Exists(path))
        {
            return;
        }
        using (FileStream underlyingStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
            using SHA1Stream sHA1Stream = new SHA1Stream(underlyingStream);
            sHA1Stream.ReadByte();
            for (int i = 0; i < 256; i++)
            {
                for (int j = 0; j < 256; j += 8)
                {
                    byte b = (byte)sHA1Stream.ReadByte();
                    for (int k = 0; k < 8; k++)
                    {
                        holes[i, j + k] = (b & (1 << k)) > 0;
                    }
                }
            }
            Level.includeHash(text, sHA1Stream.Hash);
        }
        data.SetHoles(0, 0, holes);
        hasAnyHolesData = true;
    }

    public virtual void write(IFormattedFileWriter writer)
    {
        writer.beginObject();
        writer.writeValue("Coord", coord);
        writer.beginArray("Materials");
        for (int i = 0; i < Landscape.SPLATMAP_LAYERS; i++)
        {
            writer.writeValue(materials[i]);
        }
        writer.endArray();
        writer.endObject();
        writeHeightmaps();
        writeSplatmaps();
        if (hasAnyHolesData)
        {
            WriteHoles();
        }
    }

    public virtual void writeHeightmaps()
    {
        writeHeightmap("_Source", heightmap);
    }

    protected virtual void writeHeightmap(string suffix, float[,] heightmap)
    {
        string path = Level.info.path + "/Landscape/Heightmaps/Tile_" + coord.x.ToString(CultureInfo.InvariantCulture) + "_" + coord.y.ToString(CultureInfo.InvariantCulture) + suffix + ".heightmap";
        string directoryName = Path.GetDirectoryName(path);
        if (!Directory.Exists(directoryName))
        {
            Directory.CreateDirectory(directoryName);
        }
        using FileStream fileStream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite);
        for (int i = 0; i < Landscape.HEIGHTMAP_RESOLUTION; i++)
        {
            for (int j = 0; j < Landscape.HEIGHTMAP_RESOLUTION; j++)
            {
                ushort num = (ushort)Mathf.RoundToInt(heightmap[i, j] * 65535f);
                fileStream.WriteByte((byte)((uint)(num >> 8) & 0xFFu));
                fileStream.WriteByte((byte)(num & 0xFFu));
            }
        }
    }

    public virtual void writeSplatmaps()
    {
        writeSplatmap("_Source", splatmap);
    }

    protected virtual void writeSplatmap(string suffix, float[,,] splatmap)
    {
        string path = Level.info.path + "/Landscape/Splatmaps/Tile_" + coord.x.ToString(CultureInfo.InvariantCulture) + "_" + coord.y.ToString(CultureInfo.InvariantCulture) + suffix + ".splatmap";
        string directoryName = Path.GetDirectoryName(path);
        if (!Directory.Exists(directoryName))
        {
            Directory.CreateDirectory(directoryName);
        }
        using FileStream fileStream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite);
        for (int i = 0; i < Landscape.SPLATMAP_RESOLUTION; i++)
        {
            for (int j = 0; j < Landscape.SPLATMAP_RESOLUTION; j++)
            {
                for (int k = 0; k < Landscape.SPLATMAP_LAYERS; k++)
                {
                    byte value = (byte)Mathf.RoundToInt(splatmap[i, j, k] * 255f);
                    fileStream.WriteByte(value);
                }
            }
        }
    }

    private void WriteHoles()
    {
        string path = Level.info.path + "/Landscape/Holes/Tile_" + coord.x.ToString(CultureInfo.InvariantCulture) + "_" + coord.y.ToString(CultureInfo.InvariantCulture) + ".bin";
        string directoryName = Path.GetDirectoryName(path);
        if (!Directory.Exists(directoryName))
        {
            Directory.CreateDirectory(directoryName);
        }
        using FileStream fileStream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite);
        fileStream.WriteByte(1);
        for (int i = 0; i < 256; i++)
        {
            for (int j = 0; j < 256; j += 8)
            {
                byte b = (byte)(holes[i, j] ? 1 : 0);
                for (int k = 1; k < 8; k++)
                {
                    b = (byte)(b | (holes[i, j + k] ? ((byte)(1 << k)) : 0));
                }
                fileStream.WriteByte(b);
            }
        }
    }

    /// <summary>
    /// Call this when done changing material references to grab their textures and pass them to the terrain renderer.
    /// </summary>
    public void updatePrototypes()
    {
        if (Dedicator.IsDedicatedServer)
        {
            return;
        }
        for (int i = 0; i < Landscape.SPLATMAP_LAYERS; i++)
        {
            AssetReference<LandscapeMaterialAsset> assetReference = materials[i];
            LandscapeMaterialAsset landscapeMaterialAsset = assetReference.Find();
            if (assetReference.isValid)
            {
                ClientAssetIntegrity.QueueRequest(assetReference.GUID, landscapeMaterialAsset, $"Landscape tile (x: {coord.x} y: {coord.y} layer: {i})");
            }
            if (landscapeMaterialAsset == null)
            {
                terrainLayers[i] = null;
            }
            else
            {
                terrainLayers[i] = landscapeMaterialAsset.getOrCreateLayer();
            }
        }
        data.terrainLayers = terrainLayers;
    }

    protected void updateTransform()
    {
        gameObject.transform.position = new Vector3((float)coord.x * Landscape.TILE_SIZE, (0f - Landscape.TILE_HEIGHT) / 2f, (float)coord.y * Landscape.TILE_SIZE);
    }

    public void convertLegacyHeightmap()
    {
        for (int i = 0; i < Landscape.HEIGHTMAP_RESOLUTION; i++)
        {
            for (int j = 0; j < Landscape.HEIGHTMAP_RESOLUTION; j++)
            {
                float conversionHeight = LevelGround.getConversionHeight(Landscape.getWorldPosition(heightmapCoord: new HeightmapCoord(i, j), tileCoord: coord, height: heightmap[i, j]));
                conversionHeight /= Landscape.TILE_HEIGHT;
                conversionHeight += 0.5f;
                heightmap[i, j] = conversionHeight;
            }
        }
        data.SetHeights(0, 0, heightmap);
        if (dataWithoutHoles != null)
        {
            dataWithoutHoles.SetHeights(0, 0, heightmap);
        }
    }

    public void convertLegacySplatmap()
    {
        for (int i = 0; i < Landscape.SPLATMAP_RESOLUTION; i++)
        {
            for (int j = 0; j < Landscape.SPLATMAP_RESOLUTION; j++)
            {
                Vector3 point = Landscape.getWorldPosition(splatmapCoord: new SplatmapCoord(i, j), tileCoord: coord);
                for (int k = 0; k < Landscape.SPLATMAP_LAYERS; k++)
                {
                    float conversionWeight = LevelGround.getConversionWeight(point, k);
                    splatmap[i, j, k] = conversionWeight;
                }
            }
        }
        if (!Dedicator.IsDedicatedServer)
        {
            data.SetAlphamaps(0, 0, splatmap);
        }
    }

    public void resetHeightmap()
    {
        for (int i = 0; i < Landscape.HEIGHTMAP_RESOLUTION; i++)
        {
            for (int j = 0; j < Landscape.HEIGHTMAP_RESOLUTION; j++)
            {
                heightmap[i, j] = 0.5f;
            }
        }
        Landscape.reconcileNeighbors(this);
        SyncHeightmap();
    }

    public void resetSplatmap()
    {
        for (int i = 0; i < Landscape.SPLATMAP_RESOLUTION; i++)
        {
            for (int j = 0; j < Landscape.SPLATMAP_RESOLUTION; j++)
            {
                splatmap[i, j, 0] = 1f;
                for (int k = 1; k < Landscape.SPLATMAP_LAYERS; k++)
                {
                    splatmap[i, j, k] = 0f;
                }
            }
        }
        data.SetAlphamaps(0, 0, splatmap);
    }

    public void normalizeSplatmap()
    {
        for (int i = 0; i < Landscape.SPLATMAP_RESOLUTION; i++)
        {
            for (int j = 0; j < Landscape.SPLATMAP_RESOLUTION; j++)
            {
                float num = 0f;
                for (int k = 0; k < Landscape.SPLATMAP_LAYERS; k++)
                {
                    num += splatmap[i, j, k];
                }
                for (int l = 0; l < Landscape.SPLATMAP_LAYERS; l++)
                {
                    splatmap[i, j, l] /= num;
                }
            }
        }
        data.SetAlphamaps(0, 0, splatmap);
    }

    public void applyGraphicsSettings()
    {
        if (Dedicator.IsDedicatedServer)
        {
            return;
        }
        if (SDG.Unturned.GraphicsSettings.blend)
        {
            switch (SDG.Unturned.GraphicsSettings.renderMode)
            {
            case ERenderMode.FORWARD:
                terrain.materialTemplate = Resources.Load<Material>("Materials/Landscapes/Landscape_Forward");
                break;
            case ERenderMode.DEFERRED:
                terrain.materialTemplate = Resources.Load<Material>("Materials/Landscapes/Landscape_Deferred");
                break;
            default:
                terrain.materialTemplate = null;
                UnturnedLog.error("Unknown render mode: " + SDG.Unturned.GraphicsSettings.renderMode);
                break;
            }
        }
        else
        {
            terrain.materialTemplate = Resources.Load<Material>("Materials/Landscapes/Landscape_Classic");
        }
        terrain.basemapDistance = SDG.Unturned.GraphicsSettings.terrainBasemapDistance;
        if (terrain.materialTemplate == null)
        {
            UnturnedLog.warn("LandscapeTile unable to load materialTemplate");
        }
        terrain.heightmapPixelError = SDG.Unturned.GraphicsSettings.terrainHeightmapPixelError;
    }

    public FoliageBounds getFoliageSurfaceBounds()
    {
        return new FoliageBounds(new FoliageCoord(coord.x * Landscape.TILE_SIZE_INT / FoliageSystem.TILE_SIZE_INT, coord.y * Landscape.TILE_SIZE_INT / FoliageSystem.TILE_SIZE_INT), new FoliageCoord((coord.x + 1) * Landscape.TILE_SIZE_INT / FoliageSystem.TILE_SIZE_INT - 1, (coord.y + 1) * Landscape.TILE_SIZE_INT / FoliageSystem.TILE_SIZE_INT - 1));
    }

    public bool getFoliageSurfaceInfo(Vector3 position, out Vector3 surfacePosition, out Vector3 surfaceNormal)
    {
        surfacePosition = position;
        surfacePosition.y = terrain.SampleHeight(position) - Landscape.TILE_HEIGHT / 2f;
        surfaceNormal = data.GetInterpolatedNormal((position.x - (float)coord.x * Landscape.TILE_SIZE) / Landscape.TILE_SIZE, (position.z - (float)coord.y * Landscape.TILE_SIZE) / Landscape.TILE_SIZE);
        return !Landscape.IsPointInsideHole(surfacePosition);
    }

    public void bakeFoliageSurface(FoliageBakeSettings bakeSettings, FoliageTile foliageTile)
    {
        int num = (foliageTile.coord.y * FoliageSystem.TILE_SIZE_INT - coord.y * Landscape.TILE_SIZE_INT) / FoliageSystem.TILE_SIZE_INT * FoliageSystem.SPLATMAP_RESOLUTION_PER_TILE;
        int num2 = num + FoliageSystem.SPLATMAP_RESOLUTION_PER_TILE;
        int num3 = (foliageTile.coord.x * FoliageSystem.TILE_SIZE_INT - coord.x * Landscape.TILE_SIZE_INT) / FoliageSystem.TILE_SIZE_INT * FoliageSystem.SPLATMAP_RESOLUTION_PER_TILE;
        int num4 = num3 + FoliageSystem.SPLATMAP_RESOLUTION_PER_TILE;
        for (int i = num; i < num2; i++)
        {
            for (int j = num3; j < num4; j++)
            {
                SplatmapCoord splatmapCoord = new SplatmapCoord(i, j);
                float num5 = (float)coord.x * Landscape.TILE_SIZE + (float)splatmapCoord.y * Landscape.SPLATMAP_WORLD_UNIT;
                float num6 = (float)coord.y * Landscape.TILE_SIZE + (float)splatmapCoord.x * Landscape.SPLATMAP_WORLD_UNIT;
                Bounds bounds = default(Bounds);
                bounds.min = new Vector3(num5, 0f, num6);
                bounds.max = new Vector3(num5 + Landscape.SPLATMAP_WORLD_UNIT, 0f, num6 + Landscape.SPLATMAP_WORLD_UNIT);
                for (int k = 0; k < Landscape.SPLATMAP_LAYERS; k++)
                {
                    float num7 = splatmap[i, j, k];
                    if (!(num7 < 0.01f))
                    {
                        LandscapeMaterialAsset landscapeMaterialAsset = Assets.find(materials[k]);
                        if (landscapeMaterialAsset != null)
                        {
                            Assets.find(landscapeMaterialAsset.foliage)?.bakeFoliage(bakeSettings, this, bounds, num7);
                        }
                    }
                }
            }
        }
    }

    protected virtual void handleMaterialsInspectorChanged(IInspectableList list)
    {
        updatePrototypes();
    }

    public virtual void enable()
    {
        FoliageSystem.addSurface(this);
    }

    public virtual void disable()
    {
        FoliageSystem.removeSurface(this);
    }

    public LandscapeTile(LandscapeCoord newCoord)
    {
        gameObject = new GameObject();
        gameObject.tag = "Ground";
        gameObject.layer = 20;
        gameObject.transform.rotation = MathUtility.IDENTITY_QUATERNION;
        gameObject.transform.localScale = Vector3.one;
        coord = newCoord;
        heightmap = new float[Landscape.HEIGHTMAP_RESOLUTION, Landscape.HEIGHTMAP_RESOLUTION];
        splatmap = new float[Landscape.SPLATMAP_RESOLUTION, Landscape.SPLATMAP_RESOLUTION, Landscape.SPLATMAP_LAYERS];
        holes = new bool[256, 256];
        for (int i = 0; i < Landscape.HEIGHTMAP_RESOLUTION; i++)
        {
            for (int j = 0; j < Landscape.HEIGHTMAP_RESOLUTION; j++)
            {
                heightmap[i, j] = 0.5f;
            }
        }
        for (int k = 0; k < Landscape.SPLATMAP_RESOLUTION; k++)
        {
            for (int l = 0; l < Landscape.SPLATMAP_RESOLUTION; l++)
            {
                splatmap[k, l, 0] = 1f;
            }
        }
        for (int m = 0; m < 256; m++)
        {
            for (int n = 0; n < 256; n++)
            {
                holes[m, n] = true;
            }
        }
        materials = new InspectableList<AssetReference<LandscapeMaterialAsset>>(Landscape.SPLATMAP_LAYERS);
        materials.Add(DEFAULT_MATERIAL);
        for (int num = 1; num < Landscape.SPLATMAP_LAYERS; num++)
        {
            materials.Add(AssetReference<LandscapeMaterialAsset>.invalid);
        }
        materials.canInspectorAdd = false;
        materials.canInspectorRemove = false;
        materials.inspectorChanged += handleMaterialsInspectorChanged;
        data = new TerrainData();
        if (!Dedicator.IsDedicatedServer)
        {
            terrainLayers = new TerrainLayer[Landscape.SPLATMAP_LAYERS];
            data.terrainLayers = terrainLayers;
        }
        data.heightmapResolution = Landscape.HEIGHTMAP_RESOLUTION;
        data.alphamapResolution = Landscape.SPLATMAP_RESOLUTION;
        data.baseMapResolution = Landscape.BASEMAP_RESOLUTION;
        data.size = new Vector3(Landscape.TILE_SIZE, Landscape.TILE_HEIGHT, Landscape.TILE_SIZE);
        data.SetHeightsDelayLOD(0, 0, heightmap);
        if (!Dedicator.IsDedicatedServer)
        {
            data.SetAlphamaps(0, 0, splatmap);
        }
        data.wavingGrassTint = Color.white;
        terrain = gameObject.AddComponent<Terrain>();
        terrain.drawInstanced = SystemInfo.supportsInstancing;
        terrain.terrainData = data;
        terrain.heightmapPixelError = 200f;
        terrain.reflectionProbeUsage = ReflectionProbeUsage.Off;
        terrain.shadowCastingMode = ShadowCastingMode.Off;
        terrain.drawHeightmap = !Dedicator.IsDedicatedServer;
        terrain.drawTreesAndFoliage = false;
        terrain.collectDetailPatches = false;
        terrain.allowAutoConnect = false;
        terrain.groupingID = 1;
        terrain.Flush();
        if (Level.isEditor)
        {
            dataWithoutHoles = new TerrainData();
            dataWithoutHoles.heightmapResolution = data.heightmapResolution;
            dataWithoutHoles.size = data.size;
            dataWithoutHoles.SetHeightsDelayLOD(0, 0, heightmap);
        }
        collider = gameObject.AddComponent<TerrainCollider>();
        collider.terrainData = ((Level.isEditor && Landscape.DisableHoleColliders) ? dataWithoutHoles : data);
    }

    [Conditional("UNITY_EDITOR")]
    [Conditional("DEVELOPMENT_BUILD")]
    private void UpdateNames()
    {
        gameObject.name = $"Terrain ({coord.x}, {coord.y})";
        data.name = $"x: {coord.x} y: {coord.y}";
        if (dataWithoutHoles != null)
        {
            dataWithoutHoles.name = data.name + " (without holes)";
        }
    }
}
