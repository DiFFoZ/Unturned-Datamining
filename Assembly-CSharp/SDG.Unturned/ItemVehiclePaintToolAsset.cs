using UnityEngine;

namespace SDG.Unturned;

public class ItemVehiclePaintToolAsset : ItemToolAsset
{
    public Color32 PaintColor { get; protected set; }

    public override void PopulateAsset(Bundle bundle, DatDictionary data, Local localization)
    {
        base.PopulateAsset(bundle, data, localization);
        if (data.TryParseColor32RGB("PaintColor", out var value))
        {
            PaintColor = value;
        }
        else
        {
            Assets.ReportError(this, "missing PaintColor");
        }
    }
}
