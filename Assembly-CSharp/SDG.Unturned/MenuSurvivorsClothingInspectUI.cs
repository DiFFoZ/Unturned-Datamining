using SDG.Provider;
using UnityEngine;

namespace SDG.Unturned;

public class MenuSurvivorsClothingInspectUI
{
    private static SleekFullscreenBox container;

    public static bool active;

    private static SleekButtonIcon backButton;

    private static ISleekConstraintFrame inventory;

    private static SleekCameraImage image;

    private static ISleekSlider slider;

    private static ISleekToggle previewOnCharacterToggle;

    private static ISleekToggle previewSoloToggle;

    private static int item;

    private static ulong instance;

    private static EMenuSurvivorsClothingInspectUIOpenContext openContext;

    private static Transform inspect;

    private static Transform model;

    private static ItemLook look;

    private static Camera camera;

    public static void open(EMenuSurvivorsClothingInspectUIOpenContext openContext)
    {
        if (!active)
        {
            active = true;
            MenuSurvivorsClothingInspectUI.openContext = openContext;
            camera.gameObject.SetActive(value: true);
            look._yaw = Characters.characterYaw;
            look.yaw = Characters.characterYaw;
            slider.Value = Characters.characterYaw / 360f;
            container.AnimateIntoView();
        }
    }

    public static void close()
    {
        if (active)
        {
            active = false;
            Characters.previewItemDefId = 0;
            Characters.previewItemSolo = false;
            Characters.RefreshPreviewCharacterModel();
            camera.gameObject.SetActive(value: false);
            container.AnimateOutOfView(0f, 1f);
        }
    }

    public static void OpenPreviousMenu()
    {
        switch (openContext)
        {
        case EMenuSurvivorsClothingInspectUIOpenContext.OwnedItem:
            MenuSurvivorsClothingItemUI.open();
            break;
        case EMenuSurvivorsClothingInspectUIOpenContext.ItemStoreDetailsMenu:
            ItemStoreDetailsMenu.instance.OpenCurrentListing();
            break;
        case EMenuSurvivorsClothingInspectUIOpenContext.ItemStoreBundleContents:
            ItemStoreBundleContentsMenu.instance.OpenCurrentListing();
            break;
        }
    }

    private static bool getInspectedItemStatTrackerValue(out EStatTrackerType type, out int kills)
    {
        return Provider.provider.economyService.getInventoryStatTrackerValue(instance, out type, out kills);
    }

    public static void viewItem(int newItem, ulong newInstance)
    {
        item = newItem;
        instance = newInstance;
        if (model != null)
        {
            Object.Destroy(model.gameObject);
            model = null;
        }
        Provider.provider.economyService.getInventoryTargetID(item, out var item_guid, out var vehicle_guid);
        ushort inventorySkinID = Provider.provider.economyService.getInventorySkinID(item);
        ushort num = Provider.provider.economyService.getInventoryMythicID(item);
        if (num == 0 && instance != 0L)
        {
            num = Provider.provider.economyService.getInventoryParticleEffect(instance);
        }
        ItemAsset itemAsset = Assets.find<ItemAsset>(item_guid);
        VehicleAsset vehicleAsset = VehicleTool.FindVehicleByGuidAndHandleRedirects(vehicle_guid);
        if (itemAsset is ItemClothingAsset)
        {
            previewOnCharacterToggle.IsVisible = true;
            previewSoloToggle.IsVisible = true;
            ApplyPreview();
        }
        else
        {
            previewOnCharacterToggle.IsVisible = false;
            previewSoloToggle.IsVisible = false;
            Characters.previewItemDefId = 0;
            Characters.previewItemSolo = false;
            Characters.RefreshPreviewCharacterModel();
        }
        if (itemAsset == null && vehicleAsset == null)
        {
            return;
        }
        bool flag = false;
        if (inventorySkinID != 0)
        {
            SkinAsset skinAsset = Assets.find(EAssetType.SKIN, inventorySkinID) as SkinAsset;
            if (vehicleAsset != null)
            {
                model = VehicleTool.getVehicle(vehicleAsset.id, skinAsset.id, num, vehicleAsset, skinAsset);
            }
            else
            {
                model = ItemTool.getItem(itemAsset.id, inventorySkinID, 100, itemAsset.getState(), viewmodel: false, itemAsset, skinAsset, getInspectedItemStatTrackerValue);
                if (num != 0)
                {
                    ItemTool.ApplyMythicalEffect(model, num, EEffectType.THIRD);
                }
            }
        }
        else
        {
            if (itemAsset.item == null && itemAsset is ItemClothingAsset { ClothingPrefab: var clothingPrefab } && clothingPrefab != null)
            {
                GameObject gameObject = Object.Instantiate(clothingPrefab);
                if (gameObject != null)
                {
                    model = gameObject.transform;
                    flag = true;
                }
            }
            if (model == null)
            {
                model = ItemTool.getItem(itemAsset.id, 0, 100, itemAsset.getState(), viewmodel: false, itemAsset, getInspectedItemStatTrackerValue);
            }
            if (num != 0)
            {
                ItemTool.ApplyMythicalEffect(model, num, EEffectType.HOOK);
            }
        }
        model.parent = inspect;
        model.localPosition = Vector3.zero;
        if (flag)
        {
            model.localRotation = Quaternion.Euler(0f, 180f, -90f);
        }
        else if (vehicleAsset != null)
        {
            model.localRotation = Quaternion.identity;
        }
        else if (itemAsset != null && itemAsset.type == EItemType.MELEE)
        {
            model.localRotation = Quaternion.Euler(0f, -90f, 90f);
        }
        else
        {
            model.localRotation = Quaternion.Euler(-90f, 0f, 0f);
        }
        look.target = model.gameObject;
    }

    private static void ApplyPreview()
    {
        Characters.previewItemDefId = (previewOnCharacterToggle.Value ? item : 0);
        Characters.previewItemSolo = previewOnCharacterToggle.Value && previewSoloToggle.Value;
        Characters.RefreshPreviewCharacterModel();
    }

    private static void OnPreviewOnCharacterToggled(ISleekToggle toggle, bool value)
    {
        previewSoloToggle.IsInteractable = value;
        ApplyPreview();
    }

    private static void OnPreviewSoloToggled(ISleekToggle toggle, bool value)
    {
        ApplyPreview();
    }

    private static void onDraggedSlider(ISleekSlider slider, float state)
    {
        look.yaw = state * 360f;
        Characters.characterYaw = look.yaw;
    }

    private static void onClickedBackButton(ISleekElement button)
    {
        OpenPreviousMenu();
        close();
    }

    public MenuSurvivorsClothingInspectUI()
    {
        container = new SleekFullscreenBox();
        container.PositionOffset_X = 10f;
        container.PositionOffset_Y = 10f;
        container.PositionScale_Y = 1f;
        container.SizeOffset_X = -20f;
        container.SizeOffset_Y = -20f;
        container.SizeScale_X = 1f;
        container.SizeScale_Y = 1f;
        MenuUI.container.AddChild(container);
        active = false;
        inventory = Glazier.Get().CreateConstraintFrame();
        inventory.PositionScale_X = 0.5f;
        inventory.PositionOffset_Y = 10f;
        inventory.PositionScale_Y = 0.125f;
        inventory.SizeScale_X = 0.5f;
        inventory.SizeScale_Y = 0.75f;
        inventory.SizeOffset_Y = -20f;
        inventory.Constraint = ESleekConstraint.FitInParent;
        container.AddChild(inventory);
        ISleekBox sleekBox = Glazier.Get().CreateBox();
        sleekBox.SizeScale_X = 1f;
        sleekBox.SizeScale_Y = 1f;
        inventory.AddChild(sleekBox);
        image = new SleekCameraImage();
        image.SizeScale_X = 1f;
        image.SizeScale_Y = 1f;
        sleekBox.AddChild(image);
        previewOnCharacterToggle = Glazier.Get().CreateToggle();
        previewOnCharacterToggle.PositionOffset_X = 10f;
        previewOnCharacterToggle.PositionOffset_Y = 10f;
        previewOnCharacterToggle.AddLabel(MenuSurvivorsClothingUI.localization.format("PreviewOnCharacterLabel"), ESleekSide.RIGHT);
        previewOnCharacterToggle.OnValueChanged += OnPreviewOnCharacterToggled;
        inventory.AddChild(previewOnCharacterToggle);
        previewSoloToggle = Glazier.Get().CreateToggle();
        previewSoloToggle.PositionOffset_X = 10f;
        previewSoloToggle.PositionOffset_Y = 60f;
        previewSoloToggle.AddLabel(MenuSurvivorsClothingUI.localization.format("PreviewSoloLabel"), ESleekSide.RIGHT);
        previewSoloToggle.OnValueChanged += OnPreviewSoloToggled;
        previewSoloToggle.IsInteractable = false;
        inventory.AddChild(previewSoloToggle);
        slider = Glazier.Get().CreateSlider();
        slider.PositionOffset_Y = 10f;
        slider.PositionScale_Y = 1f;
        slider.SizeOffset_Y = 20f;
        slider.SizeScale_X = 1f;
        slider.Orientation = ESleekOrientation.HORIZONTAL;
        slider.OnValueChanged += onDraggedSlider;
        sleekBox.AddChild(slider);
        inspect = GameObject.Find("Inspect").transform;
        look = inspect.GetComponent<ItemLook>();
        camera = look.inspectCamera;
        image.SetCamera(camera);
        backButton = new SleekButtonIcon(MenuDashboardUI.icons.load<Texture2D>("Exit"));
        backButton.PositionOffset_Y = -50f;
        backButton.PositionScale_Y = 1f;
        backButton.SizeOffset_X = 200f;
        backButton.SizeOffset_Y = 50f;
        backButton.text = MenuDashboardUI.localization.format("BackButtonText");
        backButton.tooltip = MenuDashboardUI.localization.format("BackButtonTooltip");
        backButton.onClickedButton += onClickedBackButton;
        backButton.fontSize = ESleekFontSize.Medium;
        backButton.iconColor = ESleekTint.FOREGROUND;
        container.AddChild(backButton);
    }
}
