using System;
using System.Collections.Generic;
using UnityEngine;

namespace SDG.Unturned;

internal abstract class ServerCurationItem
{
    protected bool _isActive;

    private int _sortOrder = -1;

    protected ServerListCuration curation;

    public bool IsActive
    {
        get
        {
            return _isActive;
        }
        set
        {
            if (_isActive != value)
            {
                _isActive = value;
                SaveActive();
                curation.MarkDirty();
            }
        }
    }

    public abstract string DisplayName { get; }

    public abstract string DisplayOrigin { get; }

    public abstract Texture2D Icon { get; }

    public abstract string IconUrl { get; }

    public string ErrorMessage { get; internal set; }

    public abstract bool IsDeletable { get; }

    public bool IsAtFrontOfList => _sortOrder == 0;

    public bool IsAtBackOfList => _sortOrder == curation.GetItems().Count - 1;

    public int SortOrder
    {
        get
        {
            return _sortOrder;
        }
        set
        {
            bool num = _sortOrder != -1 && _sortOrder != value;
            _sortOrder = value;
            if (num)
            {
                this.OnSortOrderChanged?.Invoke();
            }
        }
    }

    public event System.Action OnSortOrderChanged;

    /// <summary>
    /// Invoked when web item is first loaded or reloaded.
    /// </summary>
    public event System.Action OnDataChanged;

    public abstract void Reload();

    public abstract void Delete();

    public abstract List<ServerListCurationRule> GetRules();

    protected abstract void SaveActive();

    protected void InvokeDataChanged()
    {
        this.OnDataChanged?.TryInvoke("OnDataChanged");
    }

    public ServerCurationItem(ServerListCuration curation)
    {
        this.curation = curation;
    }
}
