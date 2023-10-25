using SDG.Framework.IO.FormattedFiles;
using UnityEngine;

namespace SDG.Framework.Devkit;

public interface IDevkitHierarchyItem : IFormattedFileReadable, IFormattedFileWritable
{
    uint instanceID { get; set; }

    GameObject areaSelectGameObject { get; }

    /// <summary>
    /// If true, write to LevelHierarchy file.
    /// False for externally managed objects like legacy lighting WaterVolume.
    /// </summary>
    bool ShouldSave { get; }

    /// <summary>
    /// If true, editor tools can select and transform.
    /// False for items like the object-owned culling volumes.
    /// </summary>
    bool CanBeSelected { get; }
}
