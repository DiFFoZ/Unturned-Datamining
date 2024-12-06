using System.Collections.Generic;
using UnityEngine;

namespace SDG.Unturned;

/// <summary>
/// Offsets a crawler track's material UV offset in sync with wheels rolling.
/// </summary>
internal struct CrawlerTrackTilingMaterial : IDatParseable
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
    /// Indices of wheels to copy RPM from.
    /// </summary>
    public int[] wheelIndices;

    /// <summary>
    /// How far to travel to offset UV 1x. (1/x)
    ///
    /// You can calculate RepeatDistance by selecting an edge parallel to the crawler track and dividing the UV
    /// distance by the physical 3D distance. For example, if the UV length is 2 and the 3D length is 1.5 m then
    /// the texture repeats 1.33 UV/m.
    /// </summary>
    public float repeatDistance;

    /// <summary>
    /// UV mainTextureOffset per distance traveled.
    /// </summary>
    public Vector2 uvDirection;

    public bool TryParse(IDatNode node)
    {
        if (node is DatDictionary datDictionary)
        {
            repeatDistance = datDictionary.ParseFloat("RepeatDistance");
            if (datDictionary.TryGetList("WheelIndices", out var node2))
            {
                List<int> list = new List<int>(node2.Count);
                foreach (IDatNode item in node2)
                {
                    if (item is DatValue datValue && datValue.TryParseInt32(out var value))
                    {
                        list.Add(value);
                    }
                }
                if (list.Count > 0)
                {
                    wheelIndices = list.ToArray();
                }
            }
            if (wheelIndices == null || wheelIndices.Length < 1)
            {
                Assets.ReportError(Assets.currentAsset, "crawler track tiling material\"" + path + "\" has no WheelIndices");
                return false;
            }
            path = datDictionary.GetString("Path");
            materialIndex = datDictionary.ParseInt32("MaterialIndex");
            uvDirection = datDictionary.ParseVector2("UV_Direction");
            return true;
        }
        return false;
    }
}
