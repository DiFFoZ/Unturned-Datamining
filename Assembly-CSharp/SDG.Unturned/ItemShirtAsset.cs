using UnityEngine;

namespace SDG.Unturned;

public class ItemShirtAsset : ItemBagAsset
{
    protected Texture2D _shirt;

    protected Texture2D _emission;

    protected Texture2D _metallic;

    protected bool _ignoreHand;

    public Material characterMaterialOverride;

    public Texture2D shirt => _shirt;

    public Texture2D emission => _emission;

    public Texture2D metallic => _metallic;

    public bool ignoreHand => _ignoreHand;

    public Mesh[] characterMeshOverride1pLODs { get; protected set; }

    public Mesh[] characterMeshOverride3pLODs { get; protected set; }

    public ItemShirtAsset(Bundle bundle, Data data, Local localization, ushort id)
        : base(bundle, data, localization, id)
    {
        if (Dedicator.IsDedicatedServer)
        {
            characterMeshOverride1pLODs = null;
            characterMeshOverride3pLODs = null;
            characterMaterialOverride = null;
        }
        else
        {
            if (data.readBoolean("Has_1P_Character_Mesh_Override"))
            {
                characterMeshOverride1pLODs = new Mesh[1];
                for (int i = 0; i < characterMeshOverride1pLODs.Length; i++)
                {
                    GameObject gameObject = bundle.load<GameObject>("Character_Mesh_1P_Override_" + i);
                    if (gameObject == null)
                    {
                        gameObject = bundle.load<GameObject>("Character_Mesh_Override_" + i);
                    }
                    if (gameObject != null)
                    {
                        MeshFilter component = gameObject.GetComponent<MeshFilter>();
                        if (component != null)
                        {
                            characterMeshOverride1pLODs[i] = component.sharedMesh;
                        }
                        else
                        {
                            Assets.reportError(this, "missing MeshFilter on character mesh 1P override " + i);
                        }
                    }
                    else
                    {
                        Assets.reportError(this, "missing 'Character_Mesh_1P_Override_" + i + "' GameObject");
                    }
                }
            }
            else
            {
                characterMeshOverride1pLODs = null;
            }
            ushort num = data.readUInt16("Character_Mesh_3P_Override_LODs", 0);
            if (num > 0)
            {
                characterMeshOverride3pLODs = new Mesh[num];
                for (int j = 0; j < characterMeshOverride3pLODs.Length; j++)
                {
                    GameObject gameObject2 = bundle.load<GameObject>("Character_Mesh_3P_Override_" + j);
                    if (gameObject2 == null)
                    {
                        gameObject2 = bundle.load<GameObject>("Character_Mesh_Override_" + j);
                    }
                    if (gameObject2 != null)
                    {
                        MeshFilter component2 = gameObject2.GetComponent<MeshFilter>();
                        if (component2 != null)
                        {
                            characterMeshOverride3pLODs[j] = component2.sharedMesh;
                        }
                        else
                        {
                            Assets.reportError(this, "missing MeshFilter on character mesh 3P override " + j);
                        }
                    }
                    else
                    {
                        Assets.reportError(this, "missing 'Character_Mesh_3P_Override_" + j + "' GameObject");
                    }
                }
            }
            else
            {
                characterMeshOverride3pLODs = null;
            }
            if (data.readBoolean("Has_Character_Material_Override"))
            {
                characterMaterialOverride = bundle.load<Material>("Character_Material_Override");
                if (characterMaterialOverride == null)
                {
                    Assets.reportError(this, "missing 'Character_Material_Override' Material");
                }
            }
            else
            {
                characterMaterialOverride = null;
            }
        }
        if (!Dedicator.IsDedicatedServer && characterMaterialOverride == null)
        {
            _shirt = loadRequiredAsset<Texture2D>(bundle, "Shirt");
            if (shirt != null && (bool)Assets.shouldValidateAssets)
            {
                if (shirt.isReadable)
                {
                    Assets.reportError(this, "texture 'Shirt' can save memory by disabling read/write");
                }
                if (shirt.format != TextureFormat.RGBA32 && (shirt.width <= 256 || shirt.height <= 256))
                {
                    Assets.reportError(this, $"texture Shirt looks weird because it is relatively low resolution but has compression enabled ({shirt.format})");
                }
            }
            _emission = bundle.load<Texture2D>("Emission");
            if (emission != null && (bool)Assets.shouldValidateAssets)
            {
                if (emission.isReadable)
                {
                    Assets.reportError(this, "texture 'Emission' can save memory by disabling read/write");
                }
                if (emission.width <= 256 || emission.height <= 256)
                {
                    if (emission.format == TextureFormat.RGBA32)
                    {
                        Assets.reportError(this, "texture Emission is relatively low resolution so RGB24 format is recommended");
                    }
                    else if (emission.format != TextureFormat.RGB24)
                    {
                        Assets.reportError(this, $"texture Emission looks weird because it is relatively low resolution but has compression enabled ({emission.format})");
                    }
                }
            }
            _metallic = bundle.load<Texture2D>("Metallic");
            if (metallic != null && (bool)Assets.shouldValidateAssets)
            {
                if (metallic.isReadable)
                {
                    Assets.reportError(this, "texture 'Metallic' can save memory by disabling read/write");
                }
                if (metallic.format != TextureFormat.RGBA32 && (metallic.width <= 256 || metallic.height <= 256))
                {
                    Assets.reportError(this, $"texture Metallic looks weird because it is relatively low resolution but has compression enabled ({metallic.format})");
                }
            }
        }
        _ignoreHand = data.has("Ignore_Hand");
    }
}
