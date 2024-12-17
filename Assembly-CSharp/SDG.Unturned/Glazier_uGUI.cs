using System;
using System.Collections.Generic;
using System.Diagnostics;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SDG.Unturned;

internal class Glazier_uGUI : GlazierBase, IGlazier
{
    private SleekWindow _root;

    private GlazierElementBase_uGUI rootImpl;

    private List<GlazierElementBase_uGUI> elements = new List<GlazierElementBase_uGUI>();

    private List<GlazierBox_uGUI.BoxPoolData> boxPool = new List<GlazierBox_uGUI.BoxPoolData>();

    private List<GlazierElementBase_uGUI.PoolData> framePool = new List<GlazierElementBase_uGUI.PoolData>();

    private List<GlazierButton_uGUI.ButtonPoolData> buttonPool = new List<GlazierButton_uGUI.ButtonPoolData>();

    private List<GlazierImage_uGUI.ImagePoolData> imagePool = new List<GlazierImage_uGUI.ImagePoolData>();

    private List<GlazierLabel_uGUI.LabelPoolData> labelPool = new List<GlazierLabel_uGUI.LabelPoolData>();

    private Canvas canvas;

    private CanvasScaler canvasScaler;

    private TextMeshProUGUI debugTextComponent;

    private RectTransform cursorTransform;

    private RawImage cursorImage;

    private GameObject tooltipGameObject;

    private RectTransform tooltipTransform;

    private TextMeshProUGUI tooltipTextComponent;

    private Image tooltipShadowImage;

    private GlazieruGUITooltip lastTooltip;

    private float startedTooltip;

    private bool wasCursorVisible;

    private bool wasCursorLocked;

    private Material fontMaterial_Default;

    private Material fontMaterial_Outline;

    private Material fontMaterial_Shadow;

    private Material fontMaterial_Tooltip;

    private static StaticResourceRef<Texture2D> defaultCursor = new StaticResourceRef<Texture2D>("UI/Glazier_uGUI/Cursor");

    private static CommandLineInt clPixelPerfect = new CommandLineInt("-uGUIPixelPerfect");

    public bool SupportsDepth => true;

    public bool SupportsRichTextAlpha => true;

    public bool SupportsAutomaticLayout => true;

    public bool SupportsTilingSprite => true;

    public SleekWindow Root
    {
        get
        {
            return _root;
        }
        set
        {
            if (_root == value)
            {
                return;
            }
            if (rootImpl != null && rootImpl.transform != null)
            {
                rootImpl.transform.SetParent(null, worldPositionStays: false);
            }
            _root = value;
            if (_root != null)
            {
                rootImpl = _root.AttachmentRoot as GlazierElementBase_uGUI;
                if (rootImpl != null)
                {
                    rootImpl.transform.SetParent(base.transform, worldPositionStays: false);
                    rootImpl.transform.SetAsFirstSibling();
                }
                else
                {
                    UnturnedLog.warn("Root must be a uGUI element: {0}", _root.GetType().Name);
                }
            }
            else
            {
                rootImpl = null;
            }
            canvas.sortingOrder = ((_root != null && _root.hackSortOrder) ? 29000 : 15);
        }
    }

    public ISleekBox CreateBox()
    {
        GlazierBox_uGUI glazierBox_uGUI = new GlazierBox_uGUI(this);
        elements.Add(glazierBox_uGUI);
        GlazierBox_uGUI.BoxPoolData boxPoolData = ClaimElementFromPool(boxPool);
        if (boxPoolData == null)
        {
            glazierBox_uGUI.ConstructNew();
        }
        else
        {
            glazierBox_uGUI.ConstructFromBoxPool(boxPoolData);
        }
        glazierBox_uGUI.SynchronizeTheme();
        glazierBox_uGUI.SynchronizeColors();
        return glazierBox_uGUI;
    }

    public ISleekButton CreateButton()
    {
        GlazierButton_uGUI glazierButton_uGUI = new GlazierButton_uGUI(this);
        elements.Add(glazierButton_uGUI);
        GlazierButton_uGUI.ButtonPoolData buttonPoolData = ClaimElementFromPool(buttonPool);
        if (buttonPoolData == null)
        {
            glazierButton_uGUI.ConstructNew();
        }
        else
        {
            glazierButton_uGUI.ConstructFromButtonPool(buttonPoolData);
        }
        glazierButton_uGUI.SynchronizeTheme();
        glazierButton_uGUI.SynchronizeColors();
        return glazierButton_uGUI;
    }

    public ISleekElement CreateFrame()
    {
        GlazierEmpty_uGUI glazierEmpty_uGUI = new GlazierEmpty_uGUI(this);
        elements.Add(glazierEmpty_uGUI);
        GlazierElementBase_uGUI.PoolData poolData = ClaimElementFromPool(framePool);
        if (poolData == null)
        {
            glazierEmpty_uGUI.ConstructNew();
        }
        else
        {
            glazierEmpty_uGUI.ConstructFromPool(poolData);
        }
        return glazierEmpty_uGUI;
    }

    public ISleekConstraintFrame CreateConstraintFrame()
    {
        GlazierConstraintFrame_uGUI glazierConstraintFrame_uGUI = new GlazierConstraintFrame_uGUI(this);
        glazierConstraintFrame_uGUI.ConstructNew();
        elements.Add(glazierConstraintFrame_uGUI);
        return glazierConstraintFrame_uGUI;
    }

    public ISleekImage CreateImage()
    {
        return CreateImage(null);
    }

    public ISleekImage CreateImage(Texture texture)
    {
        GlazierImage_uGUI glazierImage_uGUI = new GlazierImage_uGUI(this);
        elements.Add(glazierImage_uGUI);
        GlazierImage_uGUI.ImagePoolData imagePoolData = ClaimElementFromPool(imagePool);
        if (imagePoolData == null)
        {
            glazierImage_uGUI.ConstructNew();
        }
        else
        {
            glazierImage_uGUI.ConstructFromImagePool(imagePoolData);
        }
        glazierImage_uGUI.Texture = texture;
        glazierImage_uGUI.SynchronizeColors();
        return glazierImage_uGUI;
    }

    public ISleekSprite CreateSprite()
    {
        return CreateSprite(null);
    }

    public ISleekSprite CreateSprite(Sprite sprite)
    {
        GlazierSprite_uGUI glazierSprite_uGUI = new GlazierSprite_uGUI(this, sprite);
        glazierSprite_uGUI.ConstructNew();
        elements.Add(glazierSprite_uGUI);
        glazierSprite_uGUI.Sprite = sprite;
        glazierSprite_uGUI.SynchronizeColors();
        return glazierSprite_uGUI;
    }

    public ISleekLabel CreateLabel()
    {
        GlazierLabel_uGUI glazierLabel_uGUI = new GlazierLabel_uGUI(this);
        elements.Add(glazierLabel_uGUI);
        GlazierLabel_uGUI.LabelPoolData labelPoolData = ClaimElementFromPool(labelPool);
        if (labelPoolData == null)
        {
            glazierLabel_uGUI.ConstructNew();
        }
        else
        {
            glazierLabel_uGUI.ConstructFromLabelPool(labelPoolData);
        }
        glazierLabel_uGUI.SynchronizeColors();
        return glazierLabel_uGUI;
    }

    public ISleekScrollView CreateScrollView()
    {
        GlazierScrollView_uGUI glazierScrollView_uGUI = new GlazierScrollView_uGUI(this);
        glazierScrollView_uGUI.ConstructNew();
        elements.Add(glazierScrollView_uGUI);
        glazierScrollView_uGUI.SynchronizeTheme();
        glazierScrollView_uGUI.SynchronizeColors();
        return glazierScrollView_uGUI;
    }

    public ISleekSlider CreateSlider()
    {
        GlazierSlider_uGUI glazierSlider_uGUI = new GlazierSlider_uGUI(this);
        glazierSlider_uGUI.ConstructNew();
        elements.Add(glazierSlider_uGUI);
        glazierSlider_uGUI.SynchronizeTheme();
        glazierSlider_uGUI.SynchronizeColors();
        return glazierSlider_uGUI;
    }

    public ISleekField CreateStringField()
    {
        GlazierStringField_uGUI glazierStringField_uGUI = new GlazierStringField_uGUI(this);
        glazierStringField_uGUI.ConstructNew();
        elements.Add(glazierStringField_uGUI);
        glazierStringField_uGUI.SynchronizeTheme();
        glazierStringField_uGUI.SynchronizeColors();
        return glazierStringField_uGUI;
    }

    public ISleekToggle CreateToggle()
    {
        GlazierToggle_uGUI glazierToggle_uGUI = new GlazierToggle_uGUI(this);
        glazierToggle_uGUI.ConstructNew();
        elements.Add(glazierToggle_uGUI);
        glazierToggle_uGUI.SynchronizeTheme();
        glazierToggle_uGUI.SynchronizeColors();
        return glazierToggle_uGUI;
    }

    public ISleekUInt8Field CreateUInt8Field()
    {
        GlazierUInt8Field_uGUI glazierUInt8Field_uGUI = new GlazierUInt8Field_uGUI(this);
        glazierUInt8Field_uGUI.ConstructNew();
        elements.Add(glazierUInt8Field_uGUI);
        glazierUInt8Field_uGUI.SynchronizeTheme();
        glazierUInt8Field_uGUI.SynchronizeColors();
        return glazierUInt8Field_uGUI;
    }

    public ISleekUInt16Field CreateUInt16Field()
    {
        GlazierUInt16Field_uGUI glazierUInt16Field_uGUI = new GlazierUInt16Field_uGUI(this);
        glazierUInt16Field_uGUI.ConstructNew();
        elements.Add(glazierUInt16Field_uGUI);
        glazierUInt16Field_uGUI.SynchronizeTheme();
        glazierUInt16Field_uGUI.SynchronizeColors();
        return glazierUInt16Field_uGUI;
    }

    public ISleekUInt32Field CreateUInt32Field()
    {
        GlazierUInt32Field_uGUI glazierUInt32Field_uGUI = new GlazierUInt32Field_uGUI(this);
        glazierUInt32Field_uGUI.ConstructNew();
        elements.Add(glazierUInt32Field_uGUI);
        glazierUInt32Field_uGUI.SynchronizeTheme();
        glazierUInt32Field_uGUI.SynchronizeColors();
        return glazierUInt32Field_uGUI;
    }

    public ISleekInt32Field CreateInt32Field()
    {
        GlazierInt32Field_uGUI glazierInt32Field_uGUI = new GlazierInt32Field_uGUI(this);
        glazierInt32Field_uGUI.ConstructNew();
        elements.Add(glazierInt32Field_uGUI);
        glazierInt32Field_uGUI.SynchronizeTheme();
        glazierInt32Field_uGUI.SynchronizeColors();
        return glazierInt32Field_uGUI;
    }

    public ISleekFloat32Field CreateFloat32Field()
    {
        GlazierFloat32Field_uGUI glazierFloat32Field_uGUI = new GlazierFloat32Field_uGUI(this);
        glazierFloat32Field_uGUI.ConstructNew();
        elements.Add(glazierFloat32Field_uGUI);
        glazierFloat32Field_uGUI.SynchronizeTheme();
        glazierFloat32Field_uGUI.SynchronizeColors();
        return glazierFloat32Field_uGUI;
    }

    public ISleekFloat64Field CreateFloat64Field()
    {
        GlazierFloat64Field_uGUI glazierFloat64Field_uGUI = new GlazierFloat64Field_uGUI(this);
        glazierFloat64Field_uGUI.ConstructNew();
        elements.Add(glazierFloat64Field_uGUI);
        glazierFloat64Field_uGUI.SynchronizeTheme();
        glazierFloat64Field_uGUI.SynchronizeColors();
        return glazierFloat64Field_uGUI;
    }

    public ISleekElement CreateProxyImplementation(SleekWrapper owner)
    {
        GlazierProxy_uGUI glazierProxy_uGUI = new GlazierProxy_uGUI(this);
        elements.Add(glazierProxy_uGUI);
        GlazierElementBase_uGUI.PoolData poolData = ClaimElementFromPool(framePool);
        if (poolData == null)
        {
            glazierProxy_uGUI.ConstructNew();
        }
        else
        {
            glazierProxy_uGUI.ConstructFromPool(poolData);
        }
        glazierProxy_uGUI.InitOwner(owner);
        return glazierProxy_uGUI;
    }

    public static Glazier_uGUI CreateGlazier()
    {
        GameObject obj = new GameObject("Glazier");
        UnityEngine.Object.DontDestroyOnLoad(obj);
        return obj.AddComponent<Glazier_uGUI>();
    }

    internal void ReleaseBoxToPool(GlazierBox_uGUI.BoxPoolData poolData)
    {
        boxPool.Add(poolData);
    }

    internal void ReleaseButtonToPool(GlazierButton_uGUI.ButtonPoolData poolData)
    {
        buttonPool.Add(poolData);
    }

    internal void ReleaseEmptyToPool(GlazierElementBase_uGUI.PoolData poolData)
    {
        framePool.Add(poolData);
    }

    internal void ReleaseImageToPool(GlazierImage_uGUI.ImagePoolData poolData)
    {
        imagePool.Add(poolData);
    }

    internal void ReleaseLabelToPool(GlazierLabel_uGUI.LabelPoolData poolData)
    {
        labelPool.Add(poolData);
    }

    internal Material GetFontMaterial(ETextContrastStyle shadowStyle)
    {
        return shadowStyle switch
        {
            ETextContrastStyle.Shadow => fontMaterial_Shadow, 
            ETextContrastStyle.Outline => fontMaterial_Outline, 
            ETextContrastStyle.Tooltip => fontMaterial_Tooltip, 
            _ => fontMaterial_Default, 
        };
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        OptionsSettings.OnCustomColorsChanged += OnCustomColorsChanged;
        OptionsSettings.OnThemeChanged += OnThemeChanged;
        canvas = base.gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 15;
        if (clPixelPerfect.hasValue)
        {
            canvas.pixelPerfect = clPixelPerfect.value > 0;
        }
        base.gameObject.AddComponent<GraphicRaycaster>();
        CreateFontMaterials();
        CreateDebugText();
        CreateCursor();
        CreateTooltip();
    }

    private void OnDestroy()
    {
        DestroyFontMaterials();
    }

    private void CreateDebugText()
    {
        GameObject gameObject = new GameObject("Debug", typeof(RectTransform));
        RectTransform rectTransform = gameObject.GetRectTransform();
        rectTransform.SetParent(base.transform, worldPositionStays: false);
        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(0f, 1f);
        rectTransform.pivot = new Vector2(0f, 1f);
        rectTransform.sizeDelta = new Vector2(800f, 30f);
        rectTransform.anchoredPosition = Vector2.zero;
        ETextContrastStyle shadowStyle = SleekShadowStyle.ContextToStyle(ETextContrastContext.ColorfulBackdrop);
        debugTextComponent = gameObject.AddComponent<TextMeshProUGUI>();
        debugTextComponent.font = GlazierResources_uGUI.Font;
        debugTextComponent.fontSharedMaterial = GetFontMaterial(shadowStyle);
        debugTextComponent.characterSpacing = GlazierUtils_uGUI.GetCharacterSpacing(shadowStyle);
        debugTextComponent.fontSize = GlazierUtils_uGUI.GetFontSize(ESleekFontSize.Default);
        debugTextComponent.fontStyle = GlazierUtils_uGUI.GetFontStyleFlags(FontStyle.Normal);
        debugTextComponent.raycastTarget = false;
        debugTextComponent.alignment = TextAlignmentOptions.TopLeft;
        debugTextComponent.margin = GlazierConst_uGUI.DefaultTextMargin;
        debugTextComponent.extraPadding = true;
    }

    private void CreateFontMaterials()
    {
        fontMaterial_Default = Resources.Load<Material>("UI/Glazier_uGUI/Font_Default");
        fontMaterial_Outline = UnityEngine.Object.Instantiate(Resources.Load<Material>("UI/Glazier_uGUI/Font_Outline"));
        fontMaterial_Shadow = UnityEngine.Object.Instantiate(Resources.Load<Material>("UI/Glazier_uGUI/Font_Shadow"));
        fontMaterial_Tooltip = UnityEngine.Object.Instantiate(Resources.Load<Material>("UI/Glazier_uGUI/Font_Tooltip"));
        SynchronizeFontMaterials();
    }

    private void SynchronizeFontMaterials()
    {
        Color shadowColor;
        Color color = (shadowColor = SleekCustomization.shadowColor);
        shadowColor.a = 0.25f;
        Color value = color;
        value.a = 0.75f;
        fontMaterial_Outline.SetColor("_OutlineColor", shadowColor);
        fontMaterial_Outline.SetColor("_UnderlayColor", value);
        Color color2 = color;
        color2.a = 0.75f;
        fontMaterial_Shadow.SetColor("_UnderlayColor", value);
        Color value2 = color;
        shadowColor.a = 1f;
        Color value3 = color;
        value3.a = 1f;
        fontMaterial_Tooltip.SetColor("_OutlineColor", value2);
        fontMaterial_Tooltip.SetColor("_UnderlayColor", value3);
    }

    private void DestroyFontMaterials()
    {
        UnityEngine.Object.Destroy(fontMaterial_Outline);
        UnityEngine.Object.Destroy(fontMaterial_Shadow);
        UnityEngine.Object.Destroy(fontMaterial_Tooltip);
    }

    private void CreateCursor()
    {
        GameObject gameObject = new GameObject("Cursor", typeof(RectTransform));
        cursorTransform = gameObject.GetRectTransform();
        cursorTransform.SetParent(base.transform, worldPositionStays: false);
        cursorTransform.anchorMin = Vector2.zero;
        cursorTransform.anchorMax = Vector2.zero;
        cursorTransform.pivot = new Vector2(0f, 1f);
        cursorTransform.sizeDelta = new Vector2(20f, 20f);
        cursorImage = gameObject.AddComponent<RawImage>();
        cursorImage.texture = (Texture2D)defaultCursor;
        cursorImage.raycastTarget = false;
        Canvas obj = gameObject.AddComponent<Canvas>();
        obj.overrideSorting = true;
        obj.sortingOrder = 30000;
    }

    private void CreateTooltip()
    {
        tooltipGameObject = new GameObject("Tooltip", typeof(RectTransform));
        tooltipTransform = tooltipGameObject.GetRectTransform();
        tooltipTransform.SetParent(base.transform, worldPositionStays: false);
        VerticalLayoutGroup verticalLayoutGroup = tooltipGameObject.AddComponent<VerticalLayoutGroup>();
        verticalLayoutGroup.childControlWidth = true;
        verticalLayoutGroup.childControlHeight = true;
        verticalLayoutGroup.childForceExpandWidth = false;
        verticalLayoutGroup.childForceExpandHeight = false;
        verticalLayoutGroup.padding = new RectOffset(5, 5, 5, 5);
        ContentSizeFitter contentSizeFitter = tooltipGameObject.AddComponent<ContentSizeFitter>();
        contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        tooltipShadowImage = tooltipGameObject.AddComponent<Image>();
        tooltipShadowImage.raycastTarget = false;
        tooltipShadowImage.type = Image.Type.Sliced;
        tooltipShadowImage.sprite = GlazierResources_uGUI.TooltipShadowSprite;
        SynchronizeTooltipShadowColor();
        GameObject gameObject = new GameObject("Text", typeof(RectTransform));
        gameObject.GetRectTransform().SetParent(tooltipTransform, worldPositionStays: false);
        ETextContrastStyle shadowStyle = SleekShadowStyle.ContextToStyle(ETextContrastContext.Tooltip);
        tooltipTextComponent = gameObject.AddComponent<TextMeshProUGUI>();
        tooltipTextComponent.font = GlazierResources_uGUI.Font;
        tooltipTextComponent.fontSharedMaterial = GetFontMaterial(shadowStyle);
        tooltipTextComponent.characterSpacing = GlazierUtils_uGUI.GetCharacterSpacing(shadowStyle);
        tooltipTextComponent.fontSize = GlazierUtils_uGUI.GetFontSize(ESleekFontSize.Default);
        tooltipTextComponent.fontStyle = GlazierUtils_uGUI.GetFontStyleFlags(FontStyle.Normal);
        tooltipTextComponent.raycastTarget = false;
        tooltipTextComponent.margin = GlazierConst_uGUI.DefaultTextMargin;
        tooltipTextComponent.extraPadding = true;
    }

    private void SynchronizeTooltipShadowColor()
    {
        Color shadowColor = SleekCustomization.shadowColor;
        shadowColor.a = 0.5f;
        tooltipShadowImage.color = shadowColor;
    }

    private void UpdateDebug()
    {
        if (OptionsSettings.debug && _root != null && (_root.isEnabled || _root.drawCursorWhileDisabled))
        {
            UpdateDebugStats();
            UpdateDebugString();
            debugTextComponent.color = base.debugStringColor;
            debugTextComponent.text = base.debugString;
            debugTextComponent.enabled = true;
        }
        else
        {
            debugTextComponent.enabled = false;
        }
    }

    private void UpdateCursor()
    {
        bool shouldDrawCursor = Root.ShouldDrawCursor;
        if (shouldDrawCursor != wasCursorVisible)
        {
            wasCursorVisible = shouldDrawCursor;
            cursorImage.gameObject.SetActive(shouldDrawCursor);
        }
        cursorImage.color = SleekCustomization.cursorColor;
        Vector2 normalizedMousePosition = InputEx.NormalizedMousePosition;
        cursorTransform.anchorMin = normalizedMousePosition;
        cursorTransform.anchorMax = normalizedMousePosition;
        cursorTransform.anchoredPosition = Vector2.zero;
        cursorImage.texture = (Texture2D)defaultCursor;
    }

    private void UpdateTooltip()
    {
        bool shouldDrawTooltip = Root.ShouldDrawTooltip;
        GlazieruGUITooltip tooltip = GlazieruGUITooltip.GetTooltip();
        if (tooltip != lastTooltip)
        {
            lastTooltip = tooltip;
            startedTooltip = Time.realtimeSinceStartup;
        }
        if (shouldDrawTooltip && tooltip != null && !string.IsNullOrEmpty(tooltip.text) && Time.realtimeSinceStartup - startedTooltip > 0.5f)
        {
            Vector2 normalizedMousePosition = InputEx.NormalizedMousePosition;
            tooltipTransform.anchorMin = normalizedMousePosition;
            tooltipTransform.anchorMax = normalizedMousePosition;
            if (normalizedMousePosition.x > 0.7f)
            {
                tooltipTransform.anchoredPosition = new Vector2(-10f, 0f);
                tooltipTransform.pivot = new Vector2(1f, 1f);
                tooltipTextComponent.alignment = TextAlignmentOptions.TopRight;
            }
            else
            {
                tooltipTransform.anchoredPosition = new Vector2(30f, 0f);
                tooltipTransform.pivot = new Vector2(0f, 1f);
                tooltipTextComponent.alignment = TextAlignmentOptions.TopLeft;
            }
            tooltipTextComponent.text = tooltip.text;
            tooltipTextComponent.color = tooltip.color;
            tooltipGameObject.SetActive(value: true);
        }
        else
        {
            tooltipGameObject.SetActive(value: false);
        }
    }

    private void OnCustomColorsChanged()
    {
        foreach (GlazierElementBase_uGUI item in EnumerateLiveElements())
        {
            item.SynchronizeColors();
        }
        SynchronizeFontMaterials();
        SynchronizeTooltipShadowColor();
    }

    private void OnThemeChanged()
    {
        foreach (GlazierElementBase_uGUI item in EnumerateLiveElements())
        {
            item.SynchronizeTheme();
            item.SynchronizeColors();
        }
    }

    /// <summary>
    /// Enumerate elements that are not in the pool.
    /// </summary>
    private IEnumerable<GlazierElementBase_uGUI> EnumerateLiveElements()
    {
        int index = elements.Count - 1;
        while (index >= 0)
        {
            GlazierElementBase_uGUI glazierElementBase_uGUI = elements[index];
            if (glazierElementBase_uGUI.gameObject == null)
            {
                elements.RemoveAtFast(index);
            }
            else
            {
                yield return glazierElementBase_uGUI;
            }
            int num = index - 1;
            index = num;
        }
    }

    /// <summary>
    /// Sanity check all returned elements have a gameObject.
    /// </summary>
    [Conditional("VALIDATE_GLAZIER_USE_AFTER_DESTROY")]
    private void ValidateNewElement(GlazierElementBase_uGUI element)
    {
        if (element.gameObject == null)
        {
            throw new Exception("uGUI element constructed with null gameObject");
        }
        if (element.transform == null)
        {
            throw new Exception("uGUI element constructed with null transform");
        }
        if (element.gameObject.GetComponent<LayoutElement>() != null)
        {
            throw new Exception("uGUI GameObject has a LayoutElement component, likely not removed before returning to the pool");
        }
        if (element.gameObject.GetComponent<LayoutGroup>() != null)
        {
            throw new Exception("uGUI GameObject has a LayoutGroup component, likely not removed before returning to the pool");
        }
    }

    [Conditional("VALIDATE_GLAZIER_USE_AFTER_DESTROY")]
    private void ValidateGameObjectReturnedToPool(GameObject gameObject)
    {
        if (gameObject == null)
        {
            throw new Exception("uGUI element returned null gameObject to pool");
        }
        if (gameObject.GetComponent<LayoutElement>() != null)
        {
            throw new Exception("uGUI GameObject has a LayoutElement component, should have been removed before returning to the pool");
        }
        if (gameObject.GetComponent<LayoutGroup>() != null)
        {
            throw new Exception("uGUI GameObject has a LayoutGroup component, should have been removed before returning to the pool");
        }
    }

    private T ClaimElementFromPool<T>(List<T> pool) where T : GlazierElementBase_uGUI.PoolData
    {
        while (pool.Count > 0)
        {
            int index = UnityEngine.Random.Range(0, pool.Count - 1);
            T val = pool[index];
            pool.RemoveAtFast(index);
            if (val != null && !(val.gameObject == null))
            {
                return val;
            }
        }
        return null;
    }

    private void LateUpdate()
    {
        float userInterfaceScale = GraphicsSettings.userInterfaceScale;
        if (MathfEx.IsNearlyEqual(userInterfaceScale, 1f, 0.001f))
        {
            if (canvasScaler != null)
            {
                UnityEngine.Object.Destroy(canvasScaler);
                canvasScaler = null;
            }
        }
        else
        {
            if (canvasScaler == null)
            {
                canvasScaler = base.gameObject.AddComponent<CanvasScaler>();
                canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            }
            canvasScaler.scaleFactor = userInterfaceScale;
        }
        if (rootImpl != null)
        {
            rootImpl.Update();
            bool isCursorLocked = _root.isCursorLocked;
            if (wasCursorLocked != isCursorLocked)
            {
                wasCursorLocked = isCursorLocked;
                if (isCursorLocked)
                {
                    EventSystem.current.SetSelectedGameObject(null);
                }
            }
            rootImpl.gameObject.SetActive(_root.isEnabled);
        }
        UpdateDebug();
        UpdateCursor();
        UpdateTooltip();
    }
}
