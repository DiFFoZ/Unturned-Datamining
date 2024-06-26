using UnityEngine;

namespace SDG.Unturned;

public class ItemToolAsset : ItemAsset
{
    protected AudioClip _use;

    public AudioClip use => _use;

    public override bool shouldFriendlySentryTargetUser => base.useableType != typeof(UseableWalkieTalkie);

    /// <summary>
    /// Tools like carjacks and tires can be used in safezone by admins for maintenance.
    /// </summary>
    public override bool canBeUsedInSafezone(SafezoneNode safezone, bool byAdmin)
    {
        if (byAdmin)
        {
            return true;
        }
        if (base.useableType == typeof(UseableCarjack))
        {
            return true;
        }
        return base.canBeUsedInSafezone(safezone, byAdmin);
    }

    public override void PopulateAsset(Bundle bundle, DatDictionary data, Local localization)
    {
        base.PopulateAsset(bundle, data, localization);
        _use = LoadRedirectableAsset<AudioClip>(bundle, "Use", data, "UseAudioClip");
    }
}
