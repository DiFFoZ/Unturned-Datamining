using UnityEngine;

namespace SDG.Unturned;

public class SleekYouTubeVideoButton : SleekWrapper
{
    private SleekWebLinkButton linkButton;

    private SleekWebImage webImage;

    public void Refresh(string videoId)
    {
        string url = "https://www.youtube.com/watch?v=" + videoId;
        linkButton.Url = url;
        string url2 = "https://img.youtube.com/vi/" + videoId + "/maxresdefault.jpg";
        webImage.Refresh(url2, shouldCache: false);
    }

    public SleekYouTubeVideoButton(Bundle icons)
    {
        base.SizeOffset_X = 980f;
        base.SizeOffset_Y = 560f;
        linkButton = new SleekWebLinkButton();
        linkButton.SizeOffset_X = base.SizeOffset_X;
        linkButton.SizeOffset_Y = base.SizeOffset_Y;
        AddChild(linkButton);
        webImage = new SleekWebImage();
        webImage.PositionOffset_X = 10f;
        webImage.PositionOffset_Y = 10f;
        webImage.SizeOffset_X = 960f;
        webImage.SizeOffset_Y = 540f;
        AddChild(webImage);
        ISleekImage sleekImage = Glazier.Get().CreateImage(icons.load<Texture2D>("PlayVideo"));
        sleekImage.PositionOffset_X = -32f;
        sleekImage.PositionOffset_Y = -32f;
        sleekImage.PositionScale_X = 0.5f;
        sleekImage.PositionScale_Y = 0.5f;
        sleekImage.SizeOffset_X = 64f;
        sleekImage.SizeOffset_Y = 64f;
        sleekImage.TintColor = ESleekTint.FOREGROUND;
        AddChild(sleekImage);
    }
}
