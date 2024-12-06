using UnityEngine;

namespace SDG.Unturned;

/// <summary>
/// Button in a list of levels.
/// </summary>
public class SleekLevel : SleekWrapper
{
    public ClickedLevel onClickedLevel;

    private bool hasCreatedStatusLabel;

    protected ISleekButton button;

    protected ISleekImage icon;

    protected ISleekLabel nameLabel;

    protected ISleekLabel infoLabel;

    public LevelInfo level { get; private set; }

    protected void onClickedButton(ISleekElement button)
    {
        onClickedLevel?.Invoke(this, (byte)(base.PositionOffset_Y / 110f));
    }

    private void OnLiveConfigRefreshed()
    {
        if (hasCreatedStatusLabel)
        {
            return;
        }
        MainMenuWorkshopFeaturedLiveConfig featured = LiveConfig.Get().mainMenuWorkshop.featured;
        if (featured.status != 0 && featured.IsNowFeaturedTimeOrBypassed() && featured.IsFeatured(level.publishedFileId))
        {
            SleekNew sleekNew = new SleekNew(featured.status == EMapStatus.Updated);
            if (icon != null)
            {
                icon.AddChild(sleekNew);
            }
            else
            {
                AddChild(sleekNew);
            }
            hasCreatedStatusLabel = true;
        }
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        LiveConfig.OnRefreshed -= OnLiveConfigRefreshed;
    }

    public SleekLevel(LevelInfo level)
    {
        this.level = level;
        base.SizeOffset_X = 400f;
        base.SizeOffset_Y = 100f;
        button = Glazier.Get().CreateButton();
        button.SizeOffset_X = 0f;
        button.SizeOffset_Y = 0f;
        button.SizeScale_X = 1f;
        button.SizeScale_Y = 1f;
        button.OnClicked += onClickedButton;
        AddChild(button);
        icon = Glazier.Get().CreateImage();
        icon.PositionOffset_X = 10f;
        icon.PositionOffset_Y = 10f;
        icon.SizeOffset_X = -20f;
        icon.SizeOffset_Y = -20f;
        icon.SizeScale_X = 1f;
        icon.SizeScale_Y = 1f;
        icon.Texture = LevelIconCache.GetOrLoadIcon(level);
        button.AddChild(icon);
        nameLabel = Glazier.Get().CreateLabel();
        nameLabel.PositionOffset_Y = 10f;
        nameLabel.SizeScale_X = 1f;
        nameLabel.SizeOffset_Y = 50f;
        nameLabel.TextAlignment = TextAnchor.MiddleCenter;
        nameLabel.FontSize = ESleekFontSize.Medium;
        nameLabel.TextContrastContext = ETextContrastContext.ColorfulBackdrop;
        button.AddChild(nameLabel);
        Local localization = level.getLocalization();
        if (localization != null && localization.has("Name"))
        {
            nameLabel.Text = localization.format("Name");
        }
        else
        {
            nameLabel.Text = level.name;
        }
        infoLabel = Glazier.Get().CreateLabel();
        infoLabel.PositionOffset_Y = 60f;
        infoLabel.SizeScale_X = 1f;
        infoLabel.SizeOffset_Y = 30f;
        infoLabel.TextAlignment = TextAnchor.MiddleCenter;
        infoLabel.TextContrastContext = ETextContrastContext.ColorfulBackdrop;
        string arg = "#SIZE";
        if (level.size == ELevelSize.TINY)
        {
            arg = MenuPlaySingleplayerUI.localization.format("Tiny");
        }
        else if (level.size == ELevelSize.SMALL)
        {
            arg = MenuPlaySingleplayerUI.localization.format("Small");
        }
        else if (level.size == ELevelSize.MEDIUM)
        {
            arg = MenuPlaySingleplayerUI.localization.format("Medium");
        }
        else if (level.size == ELevelSize.LARGE)
        {
            arg = MenuPlaySingleplayerUI.localization.format("Large");
        }
        else if (level.size == ELevelSize.INSANE)
        {
            arg = MenuPlaySingleplayerUI.localization.format("Insane");
        }
        string arg2 = "#TYPE";
        if (localization != null && localization.has("GameModeLabel"))
        {
            arg2 = localization.format("GameModeLabel");
        }
        else if (level.type == ELevelType.SURVIVAL)
        {
            arg2 = MenuPlaySingleplayerUI.localization.format("Survival");
        }
        else if (level.type == ELevelType.HORDE)
        {
            arg2 = MenuPlaySingleplayerUI.localization.format("Horde");
        }
        else if (level.type == ELevelType.ARENA)
        {
            arg2 = MenuPlaySingleplayerUI.localization.format("Arena");
        }
        infoLabel.Text = MenuPlaySingleplayerUI.localization.format("Info_WithVersion", arg, arg2, level.configData.Version);
        infoLabel.TextColor = new SleekColor(ESleekTint.FONT, 0.75f);
        button.AddChild(infoLabel);
        hasCreatedStatusLabel = false;
        LiveConfig.OnRefreshed -= OnLiveConfigRefreshed;
        OnLiveConfigRefreshed();
    }
}
