using UnityEngine;

namespace SDG.Unturned;

/// <summary>
/// Mesh Replacement Details
/// .dat Flags:
/// 	Has_1P_Character_Mesh_Override True		Bool
/// 	Character_Mesh_3P_Override_LODs #		Int
/// 	Has_Character_Material_Override True	Bool
/// Asset Bundle Objects:
/// 	Character_Mesh_1P_Override_#			GameObject with MeshFilter (mesh set to a skinned mesh)
/// 	Character_Mesh_3P_Override_#			GameObject with MeshFilter (mesh set to a skinned mesh)
/// 	Character_Material_Override				Material
/// </summary>
public class ItemShirtAsset : ItemBagAsset
{
    protected Texture2D _shirt;

    protected Texture2D _emission;

    protected Texture2D _metallic;

    protected bool _ignoreHand;

    /// <summary>
    /// Replacement for the main character material that typically has clothes and skin color.
    /// </summary>
    public Material characterMaterialOverride;

    public Texture2D shirt => _shirt;

    public Texture2D emission => _emission;

    public Texture2D metallic => _metallic;

    public bool ignoreHand => _ignoreHand;

    /// <summary>
    /// Replacements for the main 1st-person character mesh.
    /// </summary>
    public Mesh[] characterMeshOverride1pLODs { get; protected set; }

    /// <summary>
    /// Replacements for the main 3rd-person character mesh.
    /// </summary>
    public Mesh[] characterMeshOverride3pLODs { get; protected set; }

    public override void PopulateAsset(Bundle bundle, DatDictionary data, Local localization)
    {
        base.PopulateAsset(bundle, data, localization);
        if (Dedicator.IsDedicatedServer)
        {
            characterMeshOverride1pLODs = null;
            characterMeshOverride3pLODs = null;
            characterMaterialOverride = null;
        }
        else
        {
            if (data.ParseBool("Has_1P_Character_Mesh_Override"))
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
                            Assets.ReportError(this, "missing MeshFilter on character mesh 1P override " + i);
                        }
                    }
                    else
                    {
                        Assets.ReportError(this, "missing 'Character_Mesh_1P_Override_" + i + "' GameObject");
                    }
                }
            }
            else
            {
                characterMeshOverride1pLODs = null;
            }
            ushort num = data.ParseUInt16("Character_Mesh_3P_Override_LODs", 0);
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
                            Assets.ReportError(this, "missing MeshFilter on character mesh 3P override " + j);
                        }
                    }
                    else
                    {
                        Assets.ReportError(this, "missing 'Character_Mesh_3P_Override_" + j + "' GameObject");
                    }
                }
            }
            else
            {
                characterMeshOverride3pLODs = null;
            }
            if (data.ParseBool("Has_Character_Material_Override"))
            {
                characterMaterialOverride = bundle.load<Material>("Character_Material_Override");
                if (characterMaterialOverride == null)
                {
                    Assets.ReportError(this, "missing 'Character_Material_Override' Material");
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
                    Assets.ReportError(this, "texture 'Shirt' can save memory by disabling read/write");
                }
                if (shirt.format != TextureFormat.RGBA32 && shirt.format != TextureFormat.RGB24 && (shirt.width <= 128 || shirt.height <= 128))
                {
                    Assets.ReportError(this, $"texture Shirt might look weird because it is relatively low resolution but has compression enabled ({shirt.format})");
                }
            }
            _emission = bundle.load<Texture2D>("Emission");
            if (emission != null && (bool)Assets.shouldValidateAssets)
            {
                if (emission.isReadable)
                {
                    Assets.ReportError(this, "texture 'Emission' can save memory by disabling read/write");
                }
                if (emission.width <= 128 || emission.height <= 128)
                {
                    if (emission.format == TextureFormat.RGBA32)
                    {
                        Assets.ReportError(this, "texture Emission is relatively low resolution so RGB24 format is recommended");
                    }
                    else if (emission.format != TextureFormat.RGB24)
                    {
                        Assets.ReportError(this, $"texture Emission might look weird because it is relatively low resolution but has compression enabled ({emission.format})");
                    }
                }
            }
            _metallic = bundle.load<Texture2D>("Metallic");
            if (metallic != null && (bool)Assets.shouldValidateAssets)
            {
                if (metallic.isReadable)
                {
                    Assets.ReportError(this, "texture 'Metallic' can save memory by disabling read/write");
                }
                if (metallic.format != TextureFormat.RGBA32 && (metallic.width <= 128 || metallic.height <= 128))
                {
                    Assets.ReportError(this, $"texture Metallic might look weird because it is relatively low resolution but has compression enabled ({metallic.format})");
                }
            }
        }
        _ignoreHand = data.ContainsKey("Ignore_Hand");
    }
}
