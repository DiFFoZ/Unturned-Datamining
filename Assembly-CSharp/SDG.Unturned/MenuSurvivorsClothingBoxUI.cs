using System;
using System.Collections.Generic;
using SDG.Provider;
using Steamworks;
using UnityEngine;

namespace SDG.Unturned;

public class MenuSurvivorsClothingBoxUI
{
    /// <summary>
    /// Internal struct menu uses to sort items in box.
    /// </summary>
    private struct BoxEntry
    {
        /// <summary>
        /// Item definition id.
        /// </summary>
        public int id;

        /// <summary>
        /// Rarity used to sort mythical &gt; legendary &gt; epic &gt; rare.
        /// </summary>
        public EItemRarity rarity;

        /// <summary>
        /// [0, 1] calculated chance of this item being unboxed.
        /// Shown to player in item tooltips.
        /// </summary>
        public float probability;
    }

    /// <summary>
    /// Sorts box entries from highest to lowest rarity.
    /// </summary>
    private class BoxEntryComparer : Comparer<BoxEntry>
    {
        public override int Compare(BoxEntry x, BoxEntry y)
        {
            int num = x.rarity.CompareTo(y.rarity);
            if (num == 0)
            {
                string inventoryName = Provider.provider.economyService.getInventoryName(x.id);
                string inventoryName2 = Provider.provider.economyService.getInventoryName(y.id);
                return -inventoryName.CompareTo(inventoryName2);
            }
            return -num;
        }
    }

    private static Dictionary<EItemRarity, float> qualityRarities = new Dictionary<EItemRarity, float>
    {
        {
            EItemRarity.RARE,
            0.75f
        },
        {
            EItemRarity.EPIC,
            0.2f
        },
        {
            EItemRarity.LEGENDARY,
            0.05f
        },
        {
            EItemRarity.MYTHICAL,
            0.03f
        }
    };

    private static readonly float BONUS_ITEM_RARITY = 0.1f;

    private static Bundle icons;

    private static Local localization;

    private static SleekFullscreenBox container;

    public static bool active;

    private static SleekButtonIcon backButton;

    public static bool isUnboxing;

    private static float lastUnbox;

    private static float lastAngle;

    private static float angle;

    private static int lastRotation;

    private static int rotation;

    private static int target;

    private static int item;

    private static ulong instance;

    /// <summary>
    /// Items server told us we unboxed, but we wait for the animation to finish before showing.
    /// Typically one, but some newer boxes have bonus items occassionally.
    /// </summary>
    private static List<SteamItemDetails_t> unboxedItems;

    /// <summary>
    /// Is one of the unboxed items mythical rarity?
    /// </summary>
    private static bool didUnboxMythical;

    /// <summary>
    /// Items in the box.
    /// </summary>
    private static List<BoxEntry> boxEntries;

    private static int numBoxEntries;

    private static ItemBoxAsset boxAsset;

    private static ItemKeyAsset keyAsset;

    private static float size;

    private static ISleekConstraintFrame inventory;

    private static ISleekBox finalBox;

    private static SleekInventory boxButton;

    private static SleekButtonIcon keyButton;

    private static SleekButtonIcon unboxButton;

    private static ISleekBox disabledBox;

    private static ISleekLabel rareLabel;

    private static ISleekLabel epicLabel;

    private static ISleekLabel legendaryLabel;

    private static ISleekLabel mythicalLabel;

    private static ISleekLabel equalizedLabel;

    private static ISleekLabel bonusLabel;

    private static SleekInventory[] dropButtons;

    /// <summary>
    /// Format qualityRarities as ##.#
    /// Does not use 'P' format because localized strings unfortunately already had % sign.
    /// </summary>
    private static string formatQualityRarity(EItemRarity rarity)
    {
        return (qualityRarities[rarity] * 100f).ToString("0.0");
    }

    /// <summary>
    /// Skip unboxing animation.
    /// Initial call rotates to just before the item, next call skips entirely.
    /// </summary>
    public static void skipAnimation()
    {
        if (!isUnboxing || target == -1)
        {
            return;
        }
        if (rotation == target)
        {
            lastAngle -= 1f;
            return;
        }
        float num = MathF.PI / 2f;
        float num2 = (float)target / (float)numBoxEntries * MathF.PI * 2f;
        if (angle > num2 - num)
        {
            rotation = target;
            lastAngle -= 1f;
        }
        else
        {
            angle = num2 - UnityEngine.Random.Range(num / 2f, num);
        }
    }

    public static void open()
    {
        if (!active)
        {
            active = true;
            container.AnimateIntoView();
        }
    }

    public static void close()
    {
        if (active)
        {
            active = false;
            container.AnimateOutOfView(0f, 1f);
        }
    }

    public static void viewItem(int newItem, ushort newQuantity, ulong newInstance)
    {
        item = newItem;
        instance = newInstance;
        unboxedItems = null;
        didUnboxMythical = false;
        angle = 0f;
        lastRotation = 0;
        rotation = 0;
        target = -1;
        disabledBox.IsVisible = false;
        keyButton.IsVisible = true;
        unboxButton.IsVisible = true;
        boxButton.updateInventory(instance, item, newQuantity, isClickable: false, isLarge: true);
        boxAsset = Assets.find<ItemBoxAsset>(Provider.provider.economyService.getInventoryItemGuid(item));
        if (boxAsset != null)
        {
            organizeBoxEntries();
            synchronizeTotalProbabilities();
            string key = null;
            string key2 = null;
            switch (boxAsset.itemOrigin)
            {
            case EBoxItemOrigin.Unbox:
                key = "Unbox_Text";
                key2 = "Unbox_Tooltip";
                break;
            case EBoxItemOrigin.Unwrap:
                key = "Unwrap_Text";
                key2 = "Unwrap_Tooltip";
                break;
            }
            if (!Provider.provider.economyService.doesCountryAllowRandomItems)
            {
                if (Provider.provider.economyService.hasCountryDetails)
                {
                    disabledBox.IsVisible = true;
                    disabledBox.Text = localization.format("Region_Disabled", Provider.provider.economyService.getCountryWarningId());
                }
                else
                {
                    disabledBox.IsVisible = false;
                }
                unboxButton.IsVisible = false;
                keyButton.IsVisible = false;
            }
            else if (boxAsset.destroy == 0)
            {
                keyButton.IsVisible = false;
                unboxButton.icon = null;
                unboxButton.PositionOffset_X = 0f;
                unboxButton.PositionScale_X = 0.3f;
                unboxButton.SizeOffset_X = 0f;
                unboxButton.SizeScale_X = 0.4f;
                unboxButton.text = localization.format(key);
                unboxButton.tooltip = localization.format(key2);
                unboxButton.IsVisible = true;
                keyAsset = null;
            }
            else
            {
                keyButton.IsVisible = true;
                unboxButton.icon = icons.load<Texture2D>("Unbox");
                unboxButton.PositionOffset_X = 5f;
                unboxButton.PositionScale_X = 0.5f;
                unboxButton.SizeOffset_X = -5f;
                unboxButton.SizeScale_X = 0.2f;
                unboxButton.text = localization.format(key);
                unboxButton.tooltip = localization.format(key2);
                unboxButton.IsVisible = true;
                keyAsset = Assets.find<ItemKeyAsset>(Provider.provider.economyService.getInventoryItemGuid(boxAsset.destroy));
                if (keyAsset != null)
                {
                    keyButton.icon = Provider.provider.economyService.LoadItemIcon(boxAsset.destroy);
                }
            }
            size = MathF.PI * 2f / (float)numBoxEntries / 2.75f;
            finalBox.PositionScale_Y = 0.5f - size / 2f;
            finalBox.SizeScale_X = size;
            finalBox.SizeScale_Y = size;
            if (dropButtons != null)
            {
                for (int i = 0; i < dropButtons.Length; i++)
                {
                    inventory.RemoveChild(dropButtons[i]);
                }
            }
            dropButtons = new SleekInventory[numBoxEntries];
            for (int j = 0; j < numBoxEntries; j++)
            {
                BoxEntry boxEntry = boxEntries[j];
                float num = MathF.PI * 2f * (float)j / (float)numBoxEntries + MathF.PI;
                SleekInventory sleekInventory = new SleekInventory();
                sleekInventory.PositionScale_X = 0.5f + Mathf.Cos(0f - num) * (0.5f - size / 2f) - size / 2f;
                sleekInventory.PositionScale_Y = 0.5f + Mathf.Sin(0f - num) * (0.5f - size / 2f) - size / 2f;
                sleekInventory.SizeScale_X = size;
                sleekInventory.SizeScale_Y = size;
                if (boxEntry.probability > -0.5f)
                {
                    sleekInventory.extraTooltip = boxEntry.probability.ToString("P");
                }
                sleekInventory.updateInventory(0uL, boxEntry.id, 1, isClickable: false, isLarge: false);
                inventory.AddChild(sleekInventory);
                dropButtons[j] = sleekInventory;
            }
        }
        Color inventoryColor = Provider.provider.economyService.getInventoryColor(item);
        keyButton.backgroundColor = SleekColor.BackgroundIfLight(inventoryColor);
        keyButton.textColor = inventoryColor;
        unboxButton.backgroundColor = keyButton.backgroundColor;
        unboxButton.textColor = inventoryColor;
    }

    private static void synchronizeTotalProbabilities()
    {
        rareLabel.IsVisible = boxAsset.probabilityModel == EBoxProbabilityModel.Original;
        epicLabel.IsVisible = boxAsset.probabilityModel == EBoxProbabilityModel.Original;
        legendaryLabel.IsVisible = boxAsset.probabilityModel == EBoxProbabilityModel.Original;
        equalizedLabel.IsVisible = boxAsset.probabilityModel == EBoxProbabilityModel.Equalized;
        bonusLabel.IsVisible = boxAsset.containsBonusItems;
    }

    private static void organizeBoxEntries()
    {
        int[] drops = boxAsset.drops;
        int num = (numBoxEntries = drops.Length);
        boxEntries = new List<BoxEntry>(numBoxEntries);
        Dictionary<EItemRarity, int> dictionary = new Dictionary<EItemRarity, int>
        {
            {
                EItemRarity.RARE,
                0
            },
            {
                EItemRarity.EPIC,
                0
            },
            {
                EItemRarity.LEGENDARY,
                0
            },
            {
                EItemRarity.MYTHICAL,
                0
            }
        };
        for (int i = 0; i < num; i++)
        {
            int num2 = drops[i];
            EItemRarity eItemRarity = ((num2 >= 0) ? Provider.provider.economyService.getGameRarity(num2) : EItemRarity.MYTHICAL);
            dictionary[eItemRarity]++;
            BoxEntry boxEntry = default(BoxEntry);
            boxEntry.id = num2;
            boxEntry.rarity = eItemRarity;
            boxEntry.probability = -1f;
            BoxEntry boxEntry2 = boxEntry;
            boxEntries.Add(boxEntry2);
        }
        float num3 = 0f;
        for (int j = 0; j < num; j++)
        {
            BoxEntry value = boxEntries[j];
            if (value.rarity == EItemRarity.MYTHICAL)
            {
                value.probability = qualityRarities[EItemRarity.MYTHICAL];
            }
            else
            {
                if (boxAsset.probabilityModel == EBoxProbabilityModel.Original)
                {
                    int num4 = dictionary[value.rarity];
                    float probability = qualityRarities[value.rarity] / (float)num4;
                    value.probability = probability;
                }
                else
                {
                    int num5 = num - 1;
                    value.probability = 1f / (float)num5;
                }
                num3 += value.probability;
            }
            boxEntries[j] = value;
        }
        if (Mathf.Abs(num3 - 1f) > 0.01f)
        {
            UnturnedLog.warn("Unable to guess box probabilities ({0})", num3);
            for (int k = 0; k < num; k++)
            {
                BoxEntry value2 = boxEntries[k];
                value2.probability = -1f;
                boxEntries[k] = value2;
            }
        }
        boxEntries.Sort(new BoxEntryComparer());
    }

    private static void onClickedKeyButton(ISleekElement button)
    {
        ItemStore.Get().ViewItem(boxAsset.destroy);
    }

    private static void onClickedUnboxButton(ISleekElement button)
    {
        if (boxAsset.destroy == 0)
        {
            Provider.provider.economyService.exchangeInventory(boxAsset.generate, new List<EconExchangePair>
            {
                new EconExchangePair(instance, 1)
            });
        }
        else
        {
            ulong inventoryPackage = Provider.provider.economyService.getInventoryPackage(boxAsset.destroy);
            if (inventoryPackage == 0L)
            {
                return;
            }
            List<EconExchangePair> destroy = new List<EconExchangePair>
            {
                new EconExchangePair(instance, 1),
                new EconExchangePair(inventoryPackage, 1)
            };
            Provider.provider.economyService.exchangeInventory(boxAsset.generate, destroy);
        }
        isUnboxing = true;
        backButton.IsVisible = false;
        lastUnbox = Time.realtimeSinceStartup;
        lastAngle = Time.realtimeSinceStartup;
        keyButton.IsVisible = false;
        unboxButton.IsVisible = false;
    }

    /// <summary>
    /// Does client know about all the granted items?
    /// If not, either something is bad in the econ config (uh oh!) or client is out of date.
    /// </summary>
    private static bool hasAssetsForGrantedItems(List<SteamItemDetails_t> grantedItems)
    {
        foreach (SteamItemDetails_t grantedItem in grantedItems)
        {
            Provider.provider.economyService.getInventoryTargetID(grantedItem.m_iDefinition.m_SteamItemDef, out var item_guid, out var vehicle_guid);
            ItemAsset itemAsset = Assets.find<ItemAsset>(item_guid);
            VehicleAsset vehicleAsset = VehicleTool.FindVehicleByGuidAndHandleRedirects(vehicle_guid);
            if (itemAsset == null && vehicleAsset == null)
            {
                return false;
            }
        }
        return true;
    }

    private static bool wasGrantedMythical(List<SteamItemDetails_t> grantedItems)
    {
        foreach (SteamItemDetails_t grantedItem in grantedItems)
        {
            if (Provider.provider.economyService.getInventoryMythicID(grantedItem.m_iDefinition.m_SteamItemDef) != 0)
            {
                return true;
            }
        }
        return false;
    }

    private static int getIndexOfGrantedItemInDrops(List<SteamItemDetails_t> grantedItems)
    {
        for (int i = 1; i < numBoxEntries; i++)
        {
            int id = boxEntries[i].id;
            foreach (SteamItemDetails_t grantedItem in grantedItems)
            {
                if (grantedItem.m_iDefinition.m_SteamItemDef == id)
                {
                    return i;
                }
            }
        }
        return -1;
    }

    private static void exchangeErrorAlert(string message)
    {
        isUnboxing = false;
        backButton.IsVisible = true;
        MenuUI.alert(message);
        MenuSurvivorsClothingUI.open();
        close();
    }

    private static void onInventoryExchanged(List<SteamItemDetails_t> grantedItems)
    {
        if (!isUnboxing)
        {
            return;
        }
        if (!hasAssetsForGrantedItems(grantedItems))
        {
            exchangeErrorAlert(localization.format("Exchange_Missing_Assets"));
            return;
        }
        MenuSurvivorsClothingUI.updatePage();
        int num;
        if (wasGrantedMythical(grantedItems))
        {
            didUnboxMythical = true;
            num = 0;
        }
        else
        {
            didUnboxMythical = false;
            num = getIndexOfGrantedItemInDrops(grantedItems);
            if (num < 0)
            {
                exchangeErrorAlert(localization.format("Exchange_Not_In_Drops"));
                return;
            }
        }
        unboxedItems = grantedItems;
        unboxedItems.Sort(new EconItemRarityComparer());
        if (rotation < numBoxEntries * 2)
        {
            target = numBoxEntries * 3 + num;
        }
        else
        {
            target = ((int)((float)rotation / (float)numBoxEntries) + 2) * numBoxEntries + num;
        }
    }

    public static void update()
    {
        if (!isUnboxing)
        {
            return;
        }
        if (Time.realtimeSinceStartup - lastUnbox > (float)Provider.CLIENT_TIMEOUT)
        {
            isUnboxing = false;
            backButton.IsVisible = true;
            MenuUI.alert(localization.format("Exchange_Timed_Out"));
            MenuSurvivorsClothingUI.open();
            close();
            return;
        }
        if (rotation == target)
        {
            if (Time.realtimeSinceStartup - lastAngle > 0.5f)
            {
                isUnboxing = false;
                backButton.IsVisible = true;
                string key = null;
                switch (boxAsset.itemOrigin)
                {
                case EBoxItemOrigin.Unbox:
                    key = "Origin_Unbox";
                    break;
                case EBoxItemOrigin.Unwrap:
                    key = "Origin_Unwrap";
                    break;
                }
                MenuUI.alertNewItems(localization.format(key), unboxedItems);
                SteamItemDetails_t steamItemDetails_t = unboxedItems[0];
                MenuSurvivorsClothingItemUI.viewItem(steamItemDetails_t.m_iDefinition.m_SteamItemDef, steamItemDetails_t.m_unQuantity, steamItemDetails_t.m_itemId.m_SteamItemInstanceID);
                MenuSurvivorsClothingItemUI.open();
                close();
                string path = (didUnboxMythical ? "Economy/Sounds/Mythical" : "Economy/Sounds/Unbox");
                MainCamera.instance.GetComponent<AudioSource>().PlayOneShot(Resources.Load<AudioClip>(path), 0.66f);
            }
            return;
        }
        if (rotation < target - numBoxEntries || target == -1)
        {
            if (angle < MathF.PI * 4f)
            {
                angle += (Time.realtimeSinceStartup - lastAngle) * size * Mathf.Lerp(80f, 20f, angle / (MathF.PI * 4f));
            }
            else
            {
                angle += (Time.realtimeSinceStartup - lastAngle) * size * 20f;
            }
        }
        else
        {
            angle += (Time.realtimeSinceStartup - lastAngle) * Mathf.Max(((float)target - angle / (MathF.PI * 2f / (float)numBoxEntries)) / (float)numBoxEntries, 0.05f) * size * 20f;
        }
        lastAngle = Time.realtimeSinceStartup;
        rotation = (int)(angle / (MathF.PI * 2f / (float)numBoxEntries));
        if (rotation == target)
        {
            angle = (float)rotation * (MathF.PI * 2f / (float)numBoxEntries);
        }
        for (int i = 0; i < numBoxEntries; i++)
        {
            float num = MathF.PI * 2f * (float)i / (float)numBoxEntries + MathF.PI;
            dropButtons[i].PositionScale_X = 0.5f + Mathf.Cos(angle - num) * (0.5f - size / 2f) - size / 2f;
            dropButtons[i].PositionScale_Y = 0.5f + Mathf.Sin(angle - num) * (0.5f - size / 2f) - size / 2f;
        }
        if (rotation != lastRotation)
        {
            lastRotation = rotation;
            boxButton.PositionScale_Y = 0.25f;
            boxButton.AnimatePositionScale(0.3f, 0.3f, ESleekLerp.EXPONENTIAL, 20f);
            finalBox.PositionOffset_X = -20f;
            finalBox.PositionOffset_Y = -20f;
            finalBox.SizeOffset_X = 40f;
            finalBox.SizeOffset_Y = 40f;
            finalBox.AnimatePositionOffset(-10f, -10f, ESleekLerp.EXPONENTIAL, 1f);
            finalBox.AnimateSizeOffset(20f, 20f, ESleekLerp.EXPONENTIAL, 1f);
            boxButton.updateInventory(0uL, boxEntries[rotation % numBoxEntries].id, 1, isClickable: false, isLarge: true);
            if (rotation == target)
            {
                MainCamera.instance.GetComponent<AudioSource>().PlayOneShot((AudioClip)Resources.Load("Economy/Sounds/Drop"), 0.33f);
            }
            else
            {
                MainCamera.instance.GetComponent<AudioSource>().PlayOneShot((AudioClip)Resources.Load("Economy/Sounds/Tick"), 0.33f);
            }
        }
    }

    private static void onClickedBackButton(ISleekElement button)
    {
        MenuSurvivorsClothingItemUI.open();
        close();
    }

    public void OnDestroy()
    {
        TempSteamworksEconomy economyService = Provider.provider.economyService;
        economyService.onInventoryExchanged = (TempSteamworksEconomy.InventoryExchanged)Delegate.Remove(economyService.onInventoryExchanged, new TempSteamworksEconomy.InventoryExchanged(onInventoryExchanged));
    }

    public MenuSurvivorsClothingBoxUI()
    {
        localization = Localization.read("/Menu/Survivors/MenuSurvivorsClothingBox.dat");
        if (icons != null)
        {
            icons.unload();
        }
        icons = Bundles.getBundle("/Bundles/Textures/Menu/Icons/Survivors/MenuSurvivorsClothingBox/MenuSurvivorsClothingBox.unity3d");
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
        inventory.SizeScale_X = 0.5f;
        inventory.SizeScale_Y = 1f;
        inventory.SizeOffset_Y = -20f;
        inventory.Constraint = ESleekConstraint.FitInParent;
        container.AddChild(inventory);
        finalBox = Glazier.Get().CreateBox();
        finalBox.PositionOffset_X = -10f;
        finalBox.PositionOffset_Y = -10f;
        finalBox.SizeOffset_X = 20f;
        finalBox.SizeOffset_Y = 20f;
        inventory.AddChild(finalBox);
        boxButton = new SleekInventory();
        boxButton.PositionOffset_Y = -30f;
        boxButton.PositionScale_X = 0.3f;
        boxButton.PositionScale_Y = 0.3f;
        boxButton.SizeScale_X = 0.4f;
        boxButton.SizeScale_Y = 0.4f;
        inventory.AddChild(boxButton);
        keyButton = new SleekButtonIcon(null, 40);
        keyButton.PositionOffset_Y = -20f;
        keyButton.PositionScale_X = 0.3f;
        keyButton.PositionScale_Y = 0.7f;
        keyButton.SizeOffset_X = -5f;
        keyButton.SizeOffset_Y = 50f;
        keyButton.SizeScale_X = 0.2f;
        keyButton.text = localization.format("Key_Text");
        keyButton.tooltip = localization.format("Key_Tooltip");
        keyButton.onClickedButton += onClickedKeyButton;
        keyButton.fontSize = ESleekFontSize.Medium;
        keyButton.shadowStyle = ETextContrastContext.InconspicuousBackdrop;
        inventory.AddChild(keyButton);
        keyButton.IsVisible = false;
        unboxButton = new SleekButtonIcon(null);
        unboxButton.PositionOffset_X = 5f;
        unboxButton.PositionOffset_Y = -20f;
        unboxButton.PositionScale_X = 0.5f;
        unboxButton.PositionScale_Y = 0.7f;
        unboxButton.SizeOffset_X = -5f;
        unboxButton.SizeOffset_Y = 50f;
        unboxButton.SizeScale_X = 0.2f;
        unboxButton.text = localization.format("Unbox_Text");
        unboxButton.tooltip = localization.format("Unbox_Tooltip");
        unboxButton.onClickedButton += onClickedUnboxButton;
        unboxButton.fontSize = ESleekFontSize.Medium;
        unboxButton.shadowStyle = ETextContrastContext.InconspicuousBackdrop;
        inventory.AddChild(unboxButton);
        unboxButton.IsVisible = false;
        disabledBox = Glazier.Get().CreateBox();
        disabledBox.PositionOffset_Y = -20f;
        disabledBox.PositionScale_X = 0.3f;
        disabledBox.PositionScale_Y = 0.7f;
        disabledBox.SizeOffset_Y = 50f;
        disabledBox.SizeScale_X = 0.4f;
        inventory.AddChild(disabledBox);
        disabledBox.IsVisible = false;
        rareLabel = Glazier.Get().CreateLabel();
        rareLabel.PositionOffset_X = 50f;
        rareLabel.PositionOffset_Y = 50f;
        rareLabel.SizeOffset_X = 200f;
        rareLabel.SizeOffset_Y = 30f;
        rareLabel.Text = localization.format("Rarity_Rare", formatQualityRarity(EItemRarity.RARE));
        rareLabel.TextColor = ItemTool.getRarityColorUI(EItemRarity.RARE);
        rareLabel.TextAlignment = TextAnchor.MiddleLeft;
        rareLabel.TextContrastContext = ETextContrastContext.ColorfulBackdrop;
        container.AddChild(rareLabel);
        epicLabel = Glazier.Get().CreateLabel();
        epicLabel.PositionOffset_X = 50f;
        epicLabel.PositionOffset_Y = 70f;
        epicLabel.SizeOffset_X = 200f;
        epicLabel.SizeOffset_Y = 30f;
        epicLabel.Text = localization.format("Rarity_Epic", formatQualityRarity(EItemRarity.EPIC));
        epicLabel.TextColor = ItemTool.getRarityColorUI(EItemRarity.EPIC);
        epicLabel.TextAlignment = TextAnchor.MiddleLeft;
        epicLabel.TextContrastContext = ETextContrastContext.ColorfulBackdrop;
        container.AddChild(epicLabel);
        legendaryLabel = Glazier.Get().CreateLabel();
        legendaryLabel.PositionOffset_X = 50f;
        legendaryLabel.PositionOffset_Y = 90f;
        legendaryLabel.SizeOffset_X = 200f;
        legendaryLabel.SizeOffset_Y = 30f;
        legendaryLabel.Text = localization.format("Rarity_Legendary", formatQualityRarity(EItemRarity.LEGENDARY));
        legendaryLabel.TextColor = ItemTool.getRarityColorUI(EItemRarity.LEGENDARY);
        legendaryLabel.TextAlignment = TextAnchor.MiddleLeft;
        legendaryLabel.TextContrastContext = ETextContrastContext.ColorfulBackdrop;
        container.AddChild(legendaryLabel);
        mythicalLabel = Glazier.Get().CreateLabel();
        mythicalLabel.PositionOffset_X = 50f;
        mythicalLabel.PositionOffset_Y = 110f;
        mythicalLabel.SizeOffset_X = 200f;
        mythicalLabel.SizeOffset_Y = 30f;
        mythicalLabel.Text = localization.format("Rarity_Mythical", formatQualityRarity(EItemRarity.MYTHICAL));
        mythicalLabel.TextColor = ItemTool.getRarityColorUI(EItemRarity.MYTHICAL);
        mythicalLabel.TextAlignment = TextAnchor.MiddleLeft;
        mythicalLabel.TextContrastContext = ETextContrastContext.ColorfulBackdrop;
        container.AddChild(mythicalLabel);
        equalizedLabel = Glazier.Get().CreateLabel();
        equalizedLabel.PositionOffset_X = 50f;
        equalizedLabel.PositionOffset_Y = 50f;
        equalizedLabel.SizeOffset_X = 200f;
        equalizedLabel.SizeOffset_Y = 30f;
        equalizedLabel.Text = localization.format("Rarity_Equalized");
        equalizedLabel.TextAlignment = TextAnchor.MiddleLeft;
        equalizedLabel.TextContrastContext = ETextContrastContext.ColorfulBackdrop;
        container.AddChild(equalizedLabel);
        bonusLabel = Glazier.Get().CreateLabel();
        bonusLabel.PositionOffset_X = 50f;
        bonusLabel.PositionOffset_Y = 130f;
        bonusLabel.SizeOffset_X = 200f;
        bonusLabel.SizeOffset_Y = 30f;
        bonusLabel.Text = localization.format("Rarity_Bonus_Items", (BONUS_ITEM_RARITY * 100f).ToString("0.0"));
        bonusLabel.TextAlignment = TextAnchor.MiddleLeft;
        bonusLabel.TextContrastContext = ETextContrastContext.ColorfulBackdrop;
        container.AddChild(bonusLabel);
        dropButtons = null;
        TempSteamworksEconomy economyService = Provider.provider.economyService;
        economyService.onInventoryExchanged = (TempSteamworksEconomy.InventoryExchanged)Delegate.Combine(economyService.onInventoryExchanged, new TempSteamworksEconomy.InventoryExchanged(onInventoryExchanged));
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
