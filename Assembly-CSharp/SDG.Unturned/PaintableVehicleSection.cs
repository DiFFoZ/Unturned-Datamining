namespace SDG.Unturned;

public struct PaintableVehicleSection : IDatParseable
{
    /// <summary>
    /// Scene hierarchy path relative to vehicle root.
    /// </summary>
    public string path;

    /// <summary>
    /// Index in renderer's materials array.
    /// </summary>
    public int materialIndex;

    /// <summary>
    /// If true, apply to every item in renderer's materials array.
    /// </summary>
    public bool allMaterials;

    public bool TryParse(IDatNode node)
    {
        if (node is DatDictionary datDictionary)
        {
            path = datDictionary.GetString("Path");
            materialIndex = datDictionary.ParseInt32("MaterialIndex");
            allMaterials = datDictionary.ParseBool("AllMaterials");
            return true;
        }
        return false;
    }
}
