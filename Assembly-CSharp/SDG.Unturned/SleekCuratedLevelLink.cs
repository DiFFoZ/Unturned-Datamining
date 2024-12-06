using UnityEngine;

namespace SDG.Unturned;

public class SleekCuratedLevelLink : SleekWrapper
{
    private CuratedMapLink curatedMap;

    private ISleekBox backdrop;

    private ISleekButton viewOnWorkshopButton;

    private ISleekButton manageButton;

    private ISleekImage icon;

    private ISleekLabel nameLabel;

    private bool hasCreatedStatusLabel;

    private void onClickedViewButton(ISleekElement button)
    {
        if (curatedMap != null)
        {
            if (Provider.provider.browserService.canOpenBrowser)
            {
                string url = "http://steamcommunity.com/sharedfiles/filedetails/?id=" + curatedMap.Workshop_File_Id;
                Provider.provider.browserService.open(url);
            }
            else
            {
                MenuUI.alert(MenuDashboardUI.localization.format("Overlay"));
            }
        }
    }

    private bool getSubscribed()
    {
        return Provider.provider.workshopService.getSubscribed(curatedMap.Workshop_File_Id);
    }

    private void setSubscribed(bool subscribe)
    {
        Provider.provider.workshopService.setSubscribed(curatedMap.Workshop_File_Id, subscribe);
        if (subscribe)
        {
            ulong[] required_Workshop_File_Ids = curatedMap.Required_Workshop_File_Ids;
            foreach (ulong fileId in required_Workshop_File_Ids)
            {
                Provider.provider.workshopService.setSubscribed(fileId, subscribe);
            }
        }
    }

    private void onClickedManageButton(ISleekElement button)
    {
        if (curatedMap != null)
        {
            bool subscribed = !getSubscribed();
            updateManageLabel(subscribed);
            setSubscribed(subscribed);
        }
    }

    private void updateManageLabel()
    {
        if (curatedMap != null)
        {
            bool subscribed = getSubscribed();
            updateManageLabel(subscribed);
        }
    }

    private void updateManageLabel(bool subscribed)
    {
        manageButton.Text = MenuPlaySingleplayerUI.localization.format(subscribed ? "Retired_Manage_Unsub" : "Retired_Manage_Sub");
    }

    private void OnLiveConfigRefreshed()
    {
        if (hasCreatedStatusLabel)
        {
            return;
        }
        MainMenuWorkshopFeaturedLiveConfig featured = LiveConfig.Get().mainMenuWorkshop.featured;
        if (featured.status != 0 && featured.IsNowFeaturedTimeOrBypassed() && featured.IsFeatured(curatedMap.Workshop_File_Id))
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

    public SleekCuratedLevelLink(CuratedMapLink curatedMap)
    {
        this.curatedMap = curatedMap;
        base.SizeOffset_X = 400f;
        base.SizeOffset_Y = 100f;
        backdrop = Glazier.Get().CreateBox();
        backdrop.SizeOffset_X = 0f;
        backdrop.SizeOffset_Y = 0f;
        backdrop.SizeScale_X = 1f;
        backdrop.SizeScale_Y = 1f;
        AddChild(backdrop);
        string path = "/CuratedMapIcons/" + curatedMap.Workshop_File_Id + ".png";
        if (ReadWrite.fileExists(path, useCloud: false, usePath: true))
        {
            icon = Glazier.Get().CreateImage();
            icon.PositionOffset_X = 10f;
            icon.PositionOffset_Y = 10f;
            icon.SizeOffset_X = -20f;
            icon.SizeOffset_Y = -20f;
            icon.SizeScale_X = 1f;
            icon.SizeScale_Y = 1f;
            icon.Texture = ReadWrite.readTextureFromFile(path, useBasePath: true);
            icon.ShouldDestroyTexture = true;
            backdrop.AddChild(icon);
        }
        nameLabel = Glazier.Get().CreateLabel();
        nameLabel.PositionOffset_Y = 10f;
        nameLabel.SizeScale_X = 1f;
        nameLabel.SizeOffset_Y = 50f;
        nameLabel.TextAlignment = TextAnchor.MiddleCenter;
        nameLabel.FontSize = ESleekFontSize.Medium;
        nameLabel.Text = curatedMap.Name;
        nameLabel.TextContrastContext = ETextContrastContext.ColorfulBackdrop;
        backdrop.AddChild(nameLabel);
        viewOnWorkshopButton = Glazier.Get().CreateButton();
        viewOnWorkshopButton.PositionOffset_X = 15f;
        viewOnWorkshopButton.PositionOffset_Y = -45f;
        viewOnWorkshopButton.PositionScale_Y = 1f;
        viewOnWorkshopButton.SizeOffset_X = 150f;
        viewOnWorkshopButton.SizeOffset_Y = 30f;
        viewOnWorkshopButton.FontSize = ESleekFontSize.Small;
        viewOnWorkshopButton.Text = MenuPlaySingleplayerUI.localization.format("Retired_View_Label");
        viewOnWorkshopButton.TooltipText = MenuPlaySingleplayerUI.localization.format("Retired_View_Tooltip");
        viewOnWorkshopButton.OnClicked += onClickedViewButton;
        backdrop.AddChild(viewOnWorkshopButton);
        manageButton = Glazier.Get().CreateButton();
        manageButton.PositionOffset_X = -165f;
        manageButton.PositionOffset_Y = -45f;
        manageButton.PositionScale_X = 1f;
        manageButton.PositionScale_Y = 1f;
        manageButton.SizeOffset_X = 150f;
        manageButton.SizeOffset_Y = 30f;
        manageButton.FontSize = ESleekFontSize.Small;
        manageButton.TooltipText = MenuPlaySingleplayerUI.localization.format("Retired_Manage_Tooltip");
        updateManageLabel();
        manageButton.OnClicked += onClickedManageButton;
        backdrop.AddChild(manageButton);
        hasCreatedStatusLabel = false;
        LiveConfig.OnRefreshed -= OnLiveConfigRefreshed;
        OnLiveConfigRefreshed();
    }
}
