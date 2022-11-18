using System.Collections.Generic;
using UnityEngine;

namespace SDG.Unturned;

public class SleekList<T> : SleekWrapper where T : class
{
    public delegate ISleekElement CreateElement(T item);

    private struct VisibleEntry
    {
        public T item;

        public ISleekElement element;

        public VisibleEntry(T item, ISleekElement element)
        {
            this.item = item;
            this.element = element;
        }
    }

    public int itemHeight;

    public int itemPadding;

    public CreateElement onCreateElement;

    private List<T> data;

    private List<VisibleEntry> visibleEntries = new List<VisibleEntry>();

    private int oldVisibleItemsCount;

    public int IndexOfCreateElementItem { get; private set; }

    public ISleekScrollView scrollView { get; private set; }

    public void SetData(List<T> data)
    {
        this.data = data;
        NotifyDataChanged();
    }

    public void NotifyDataChanged()
    {
        int num = data.Count * itemHeight;
        if (data.Count > 1)
        {
            num += (data.Count - 1) * itemPadding;
        }
        scrollView.contentSizeOffset = new Vector2(0f, num);
        UpdateVisibleRange();
    }

    public void ForceRebuildElements()
    {
        scrollView.RemoveAllChildren();
        visibleEntries.Clear();
        NotifyDataChanged();
    }

    public override void OnUpdate()
    {
        if (data.Count > 0)
        {
            int num = CalculateVisibleItemsCount();
            if (oldVisibleItemsCount != num)
            {
                oldVisibleItemsCount = num;
                UpdateVisibleRange();
            }
        }
    }

    public SleekList()
    {
        scrollView = Glazier.Get().CreateScrollView();
        scrollView.sizeScale_X = 1f;
        scrollView.sizeScale_Y = 1f;
        scrollView.scaleContentToWidth = true;
        scrollView.onValueChanged += onValueChanged;
        AddChild(scrollView);
    }

    private int IndexOfItemWithinRange(T item, int minIndex, int maxIndex)
    {
        for (int i = minIndex; i <= maxIndex; i++)
        {
            if (data[i] == item)
            {
                return i;
            }
        }
        return -1;
    }

    private bool HasElementForItem(T item)
    {
        foreach (VisibleEntry visibleEntry in visibleEntries)
        {
            if (visibleEntry.item == item)
            {
                return true;
            }
        }
        return false;
    }

    private void UpdateVisibleRange(float normalizedValue)
    {
        if (data.Count == 0 || onCreateElement == null)
        {
            scrollView.RemoveAllChildren();
            visibleEntries.Clear();
            return;
        }
        int num = (oldVisibleItemsCount = CalculateVisibleItemsCount());
        int num2 = Mathf.Max(0, Mathf.FloorToInt(normalizedValue * (float)(data.Count - num)));
        int num3 = Mathf.Min(data.Count - 1, num2 + num);
        for (int num4 = visibleEntries.Count - 1; num4 >= 0; num4--)
        {
            VisibleEntry visibleEntry = visibleEntries[num4];
            int num5 = IndexOfItemWithinRange(visibleEntry.item, num2, num3);
            if (num5 == -1)
            {
                scrollView.RemoveChild(visibleEntry.element);
                visibleEntries.RemoveAtFast(num4);
            }
            else
            {
                visibleEntry.element.positionOffset_Y = num5 * (itemHeight + itemPadding);
            }
        }
        for (int i = num2; i <= num3; i++)
        {
            T item = data[i];
            if (!HasElementForItem(item))
            {
                IndexOfCreateElementItem = i;
                ISleekElement sleekElement = onCreateElement(item);
                sleekElement.sizeOffset_Y = itemHeight;
                sleekElement.sizeScale_X = 1f;
                sleekElement.positionOffset_Y = i * (itemHeight + itemPadding);
                scrollView.AddChild(sleekElement);
                visibleEntries.Add(new VisibleEntry(item, sleekElement));
            }
        }
    }

    private void UpdateVisibleRange()
    {
        UpdateVisibleRange(scrollView.normalizedVerticalPosition);
    }

    private int CalculateVisibleItemsCount()
    {
        return Mathf.CeilToInt(scrollView.normalizedViewportHeight * (float)data.Count);
    }

    private void onValueChanged(Vector2 value)
    {
        UpdateVisibleRange(value.y);
    }
}
