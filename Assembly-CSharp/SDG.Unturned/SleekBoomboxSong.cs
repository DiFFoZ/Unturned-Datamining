using UnityEngine;

namespace SDG.Unturned;

public class SleekBoomboxSong : SleekWrapper
{
    public StereoSongAsset songAsset;

    private PlayerBarricadeStereoUI owningUI;

    public SleekBoomboxSong(StereoSongAsset songAsset, PlayerBarricadeStereoUI owningUI)
    {
        this.songAsset = songAsset;
        this.owningUI = owningUI;
        ISleekButton sleekButton = Glazier.Get().CreateButton();
        sleekButton.SizeOffset_Y = 30f;
        sleekButton.SizeScale_X = 1f;
        sleekButton.OnClicked += OnClickedPlayButton;
        sleekButton.TextColor = ESleekTint.RICH_TEXT_DEFAULT;
        sleekButton.TextContrastContext = ETextContrastContext.InconspicuousBackdrop;
        sleekButton.AllowRichText = true;
        AddChild(sleekButton);
        if (!string.IsNullOrEmpty(songAsset.titleText))
        {
            sleekButton.Text = songAsset.titleText;
        }
        else
        {
            sleekButton.Text = songAsset.name;
        }
        if (!string.IsNullOrEmpty(songAsset.linkURL) && WebUtils.CanParseThirdPartyUrl(songAsset.linkURL))
        {
            sleekButton.SizeOffset_X -= 30f;
            SleekButtonIcon sleekButtonIcon = new SleekButtonIcon(MenuDashboardUI.icons.load<Texture2D>("External_Link"));
            sleekButtonIcon.PositionOffset_X = -30f;
            sleekButtonIcon.PositionScale_X = 1f;
            sleekButtonIcon.SizeOffset_X = 30f;
            sleekButtonIcon.SizeOffset_Y = 30f;
            sleekButtonIcon.tooltip = songAsset.linkURL;
            sleekButtonIcon.onClickedButton += OnClickedLinkButton;
            AddChild(sleekButtonIcon);
        }
    }

    private void OnClickedPlayButton(ISleekElement button)
    {
        if (owningUI.stereo != null)
        {
            owningUI.stereo.ClientSetTrack(songAsset.GUID);
        }
    }

    private void OnClickedLinkButton(ISleekElement button)
    {
        if (WebUtils.ParseThirdPartyUrl(songAsset.linkURL, out var _))
        {
            Provider.openURL(songAsset.linkURL);
            return;
        }
        UnturnedLog.warn("Ignoring potentially unsafe song link url {0}", songAsset.linkURL);
    }
}
