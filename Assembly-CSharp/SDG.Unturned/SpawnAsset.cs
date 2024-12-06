using System;
using System.Collections.Generic;
using UnityEngine;
using Unturned.SystemEx;

namespace SDG.Unturned;

public class SpawnAsset : Asset
{
    private class SpawnTableWeightComparator : IComparer<SpawnTable>
    {
        public int Compare(SpawnTable a, SpawnTable b)
        {
            return b.weight - a.weight;
        }
    }

    private static SpawnTableWeightComparator comparator = new SpawnTableWeightComparator();

    protected List<SpawnTable> _roots;

    protected List<SpawnTable> _tables;

    /// <summary>
    /// Parent spawn assets this would like to be inserted into.
    /// </summary>
    public List<SpawnTable> insertRoots { get; protected set; }

    public List<SpawnTable> roots => _roots;

    public List<SpawnTable> tables => _tables;

    public override EAssetType assetCategory => EAssetType.SPAWN;

    public bool hasBeenOverridden { get; protected set; }

    /// <summary>
    /// Do tables need to be sorted and normalized?
    /// </summary>
    public bool areTablesDirty { get; protected set; }

    /// <summary>
    /// Zero weights of child spawn tables.
    /// Called when inserting a root marked isOverride.
    /// </summary>
    public void markOverridden()
    {
        if (hasBeenOverridden)
        {
            return;
        }
        hasBeenOverridden = true;
        foreach (SpawnTable table in tables)
        {
            if (!table.isOverride)
            {
                table.weight = 0;
            }
        }
    }

    internal SpawnTable PickRandomEntry(Func<string> errorContextCallback)
    {
        if (tables.Count < 1)
        {
            UnturnedLog.warn("Spawn table " + name + " from " + GetOriginName() + " resolved by " + (errorContextCallback?.Invoke() ?? "Unknown") + " while empty");
            return null;
        }
        if (areTablesDirty)
        {
            UnturnedLog.warn("Spawn table " + name + " from " + GetOriginName() + " resolved by " + (errorContextCallback?.Invoke() ?? "Unknown") + " while dirty");
            sortAndNormalizeWeights();
        }
        if (tables.Count == 1)
        {
            return tables[0];
        }
        float value = UnityEngine.Random.value;
        for (int i = 0; i < tables.Count; i++)
        {
            if (value < tables[i].normalizedWeight || i == tables.Count - 1)
            {
                return tables[i];
            }
        }
        UnturnedLog.error("Spawn table " + name + " from " + GetOriginName() + " resolved by " + (errorContextCallback?.Invoke() ?? "Unknown") + " had no valid entry (should never happen)");
        return null;
    }

    [Obsolete]
    public void resolve(out ushort id, out bool isSpawn)
    {
        id = 0;
        isSpawn = false;
        SpawnTable spawnTable = PickRandomEntry(null);
        if (spawnTable != null)
        {
            if (spawnTable.legacySpawnId != 0)
            {
                id = spawnTable.legacySpawnId;
                isSpawn = true;
            }
            else if (spawnTable.legacyAssetId != 0)
            {
                id = spawnTable.legacyAssetId;
                isSpawn = false;
            }
        }
    }

    /// <summary>
    /// Sort children by weight ascending, and calculate their normalized chance as a percentage of total weight.
    /// </summary>
    public void sortAndNormalizeWeights()
    {
        if (!areTablesDirty)
        {
            return;
        }
        areTablesDirty = false;
        if (tables.Count < 1)
        {
            return;
        }
        if (tables.Count == 1)
        {
            tables[0].normalizedWeight = 1f;
            return;
        }
        tables.Sort(comparator);
        float num = 0f;
        foreach (SpawnTable table in tables)
        {
            num += (float)table.weight;
        }
        float num2 = 0f;
        foreach (SpawnTable table2 in tables)
        {
            num2 += (float)table2.weight;
            table2.normalizedWeight = num2 / num;
        }
    }

    public void markTablesDirty()
    {
        areTablesDirty = true;
    }

    public void EditorAddChild(Asset newChild)
    {
        if (newChild is SpawnAsset spawnAsset)
        {
            SpawnTable spawnTable = new SpawnTable();
            spawnTable.targetGuid = GUID;
            spawnTable.isLink = true;
            spawnAsset.roots.Add(spawnTable);
        }
        SpawnTable spawnTable2 = new SpawnTable();
        spawnTable2.targetGuid = newChild.GUID;
        tables.Add(spawnTable2);
        markTablesDirty();
    }

    /// <summary>
    /// Remove from roots, and if reference is valid remove us from their children.
    /// </summary>
    public void EditorRemoveParentAtIndex(int parentIndex)
    {
        SpawnTable spawnTable = roots[parentIndex];
        SpawnAsset spawnAsset = ((spawnTable.legacySpawnId == 0) ? (Assets.find(spawnTable.targetGuid) as SpawnAsset) : (Assets.find(EAssetType.SPAWN, spawnTable.legacySpawnId) as SpawnAsset));
        if (spawnAsset != null)
        {
            for (int i = 0; i < spawnAsset.tables.Count; i++)
            {
                SpawnTable spawnTable2 = spawnAsset.tables[i];
                if ((spawnTable2.legacySpawnId != 0 && spawnTable2.legacySpawnId == id) || spawnTable2.targetGuid == GUID)
                {
                    spawnAsset.tables.RemoveAt(i);
                    spawnAsset.markTablesDirty();
                    break;
                }
            }
        }
        roots.RemoveAt(parentIndex);
    }

    /// <summary>
    /// Remove from tables, and if referencing a child table remove us from their roots.
    /// </summary>
    public void EditorRemoveChildAtIndex(int childIndex)
    {
        SpawnTable spawnTable = tables[childIndex];
        SpawnAsset spawnAsset = ((spawnTable.legacySpawnId == 0) ? (Assets.find(spawnTable.targetGuid) as SpawnAsset) : (Assets.find(EAssetType.SPAWN, spawnTable.legacySpawnId) as SpawnAsset));
        if (spawnAsset != null)
        {
            for (int i = 0; i < spawnAsset.roots.Count; i++)
            {
                SpawnTable spawnTable2 = spawnAsset.roots[i];
                if ((spawnTable2.legacySpawnId != 0 && spawnTable2.legacySpawnId == id) || spawnTable2.targetGuid == GUID)
                {
                    spawnAsset.roots.RemoveAt(i);
                    break;
                }
            }
        }
        tables.RemoveAt(childIndex);
        markTablesDirty();
    }

    public void setTableWeightAtIndex(int tableIndex, int weight)
    {
        tables[tableIndex].weight = weight;
        markTablesDirty();
    }

    public override void PopulateAsset(Bundle bundle, DatDictionary data, Local localization)
    {
        base.PopulateAsset(bundle, data, localization);
        if (data.TryGetList("Roots", out var node))
        {
            insertRoots = new List<SpawnTable>(node.Count);
            _roots = new List<SpawnTable>(node.Count);
            foreach (IDatNode item in node)
            {
                if (item is DatDictionary datDictionary)
                {
                    SpawnTable spawnTable = new SpawnTable();
                    if (spawnTable.TryParse(this, datDictionary))
                    {
                        insertRoots.Add(spawnTable);
                    }
                }
            }
        }
        else
        {
            int num = data.ParseInt32("Roots");
            insertRoots = new List<SpawnTable>(num);
            for (int i = 0; i < num; i++)
            {
                SpawnTable spawnTable2 = new SpawnTable();
                spawnTable2.legacySpawnId = data.ParseUInt16("Root_" + i + "_Spawn_ID", 0);
                spawnTable2.targetGuid = data.ParseGuid("Root_" + i + "_GUID");
                spawnTable2.isOverride = data.ContainsKey("Root_" + i + "_Override");
                spawnTable2.weight = data.ParseInt32("Root_" + i + "_Weight", spawnTable2.isOverride ? 1 : 0);
                spawnTable2.normalizedWeight = 0f;
                if (spawnTable2.legacySpawnId == 0 && spawnTable2.targetGuid.IsEmpty())
                {
                    Assets.ReportError(this, "root " + i + " has neither a Spawn_ID or GUID set!");
                }
                if (spawnTable2.weight <= 0)
                {
                    Assets.ReportError(this, "root " + i + " has no weight!");
                }
                insertRoots.Add(spawnTable2);
            }
            _roots = new List<SpawnTable>(num);
        }
        if (data.TryGetList("Tables", out var node2))
        {
            _tables = new List<SpawnTable>(node2.Count);
            foreach (IDatNode item2 in node2)
            {
                if (item2 is DatDictionary datDictionary2)
                {
                    SpawnTable spawnTable3 = new SpawnTable();
                    if (spawnTable3.TryParse(this, datDictionary2))
                    {
                        tables.Add(spawnTable3);
                    }
                }
            }
        }
        else
        {
            int num2 = data.ParseInt32("Tables");
            _tables = new List<SpawnTable>(num2);
            for (int j = 0; j < num2; j++)
            {
                SpawnTable spawnTable4 = new SpawnTable();
                spawnTable4.legacyAssetId = data.ParseUInt16("Table_" + j + "_Asset_ID", 0);
                spawnTable4.legacySpawnId = data.ParseUInt16("Table_" + j + "_Spawn_ID", 0);
                spawnTable4.targetGuid = data.ParseGuid("Table_" + j + "_GUID");
                spawnTable4.weight = data.ParseInt32("Table_" + j + "_Weight");
                spawnTable4.normalizedWeight = 0f;
                if (spawnTable4.legacySpawnId == 0 && spawnTable4.legacyAssetId == 0 && spawnTable4.targetGuid.IsEmpty())
                {
                    Assets.ReportError(this, "table " + j + " has neither a Spawn_ID, Asset_ID, or GUID set!");
                }
                if (spawnTable4.weight <= 0)
                {
                    Assets.ReportError(this, "table " + j + " has no weight!");
                }
                tables.Add(spawnTable4);
            }
        }
        areTablesDirty = true;
    }

    internal override void OnCreatedAtRuntime()
    {
        base.OnCreatedAtRuntime();
        insertRoots = new List<SpawnTable>();
        _roots = new List<SpawnTable>();
        _tables = new List<SpawnTable>();
    }
}
