using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

namespace SDG.Unturned;

internal class Glazier_UIToolkit : GlazierBase, IGlazier
{
    private SleekWindow _root;

    private GlazierElementBase_UIToolkit rootImpl;

    private UIDocument document;

    private FocusController focusController;

    private HashSet<GlazierElementBase_UIToolkit> liveElements = new HashSet<GlazierElementBase_UIToolkit>();

    /// <summary>
    /// Container for SleekWindow element.
    /// </summary>
    private VisualElement gameLayer;

    /// <summary>
    /// Container for top-level visual elements.
    /// </summary>
    private VisualElement overlayLayer;

    private Label debugLabel;

    private Image cursorImage;

    private Label tooltipLabel;

    /// <summary>
    /// Element under the cursor on the previous frame.
    /// </summary>
    private GlazierElementBase_UIToolkit previousTooltipElement;

    /// <summary>
    /// Duration in seconds the cursor has been over the element.
    /// </summary>
    private float tooltipFocusTimer;

    private bool wasCursorLocked;

    public override bool ShouldGameProcessKeyDown
    {
        get
        {
            if (!base.ShouldGameProcessKeyDown)
            {
                return false;
            }
            return !(focusController.focusedElement is TextField);
        }
    }

    public bool SupportsDepth => true;

    public bool SupportsRichTextAlpha => true;

    public bool SupportsAutomaticLayout => true;

    public bool SupportsTilingSprite => false;

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
            if (rootImpl != null)
            {
                gameLayer.Remove(rootImpl.visualElement);
            }
            _root = value;
            if (_root != null)
            {
                rootImpl = _root.AttachmentRoot as GlazierElementBase_UIToolkit;
                if (rootImpl != null)
                {
                    gameLayer.Add(rootImpl.visualElement);
                    return;
                }
                UnturnedLog.warn("Root must be a UIToolkit element: {0}", _root.GetType().Name);
            }
            else
            {
                rootImpl = null;
            }
        }
    }

    public ISleekBox CreateBox()
    {
        GlazierBox_UIToolkit glazierBox_UIToolkit = new GlazierBox_UIToolkit(this);
        liveElements.Add(glazierBox_UIToolkit);
        glazierBox_UIToolkit.SynchronizeColors();
        return glazierBox_UIToolkit;
    }

    public ISleekButton CreateButton()
    {
        GlazierButton_UIToolkit glazierButton_UIToolkit = new GlazierButton_UIToolkit(this);
        liveElements.Add(glazierButton_UIToolkit);
        glazierButton_UIToolkit.SynchronizeColors();
        return glazierButton_UIToolkit;
    }

    public ISleekElement CreateFrame()
    {
        GlazierEmpty_UIToolkit glazierEmpty_UIToolkit = new GlazierEmpty_UIToolkit(this);
        liveElements.Add(glazierEmpty_UIToolkit);
        glazierEmpty_UIToolkit.SynchronizeColors();
        return glazierEmpty_UIToolkit;
    }

    public ISleekConstraintFrame CreateConstraintFrame()
    {
        GlazierConstraintFrame_UIToolkit glazierConstraintFrame_UIToolkit = new GlazierConstraintFrame_UIToolkit(this);
        liveElements.Add(glazierConstraintFrame_UIToolkit);
        glazierConstraintFrame_UIToolkit.SynchronizeColors();
        return glazierConstraintFrame_UIToolkit;
    }

    public ISleekImage CreateImage()
    {
        return CreateImage(null);
    }

    public ISleekImage CreateImage(Texture texture)
    {
        GlazierImage_UIToolkit glazierImage_UIToolkit = new GlazierImage_UIToolkit(this);
        liveElements.Add(glazierImage_UIToolkit);
        glazierImage_UIToolkit.Texture = texture;
        glazierImage_UIToolkit.SynchronizeColors();
        return glazierImage_UIToolkit;
    }

    public ISleekSprite CreateSprite()
    {
        return CreateSprite(null);
    }

    public ISleekSprite CreateSprite(Sprite sprite)
    {
        GlazierSprite_UIToolkit glazierSprite_UIToolkit = new GlazierSprite_UIToolkit(this);
        liveElements.Add(glazierSprite_UIToolkit);
        glazierSprite_UIToolkit.Sprite = sprite;
        glazierSprite_UIToolkit.SynchronizeColors();
        return glazierSprite_UIToolkit;
    }

    public ISleekLabel CreateLabel()
    {
        GlazierLabel_UIToolkit glazierLabel_UIToolkit = new GlazierLabel_UIToolkit(this);
        liveElements.Add(glazierLabel_UIToolkit);
        glazierLabel_UIToolkit.SynchronizeColors();
        return glazierLabel_UIToolkit;
    }

    public ISleekScrollView CreateScrollView()
    {
        GlazierScrollView_UIToolkit glazierScrollView_UIToolkit = new GlazierScrollView_UIToolkit(this);
        liveElements.Add(glazierScrollView_UIToolkit);
        glazierScrollView_UIToolkit.SynchronizeColors();
        return glazierScrollView_UIToolkit;
    }

    public ISleekSlider CreateSlider()
    {
        GlazierSlider_UIToolkit glazierSlider_UIToolkit = new GlazierSlider_UIToolkit(this);
        liveElements.Add(glazierSlider_UIToolkit);
        glazierSlider_UIToolkit.SynchronizeColors();
        return glazierSlider_UIToolkit;
    }

    public ISleekField CreateStringField()
    {
        GlazierStringField_UIToolkit glazierStringField_UIToolkit = new GlazierStringField_UIToolkit(this);
        liveElements.Add(glazierStringField_UIToolkit);
        glazierStringField_UIToolkit.SynchronizeColors();
        return glazierStringField_UIToolkit;
    }

    public ISleekToggle CreateToggle()
    {
        GlazierToggle_UIToolkit glazierToggle_UIToolkit = new GlazierToggle_UIToolkit(this);
        liveElements.Add(glazierToggle_UIToolkit);
        glazierToggle_UIToolkit.SynchronizeColors();
        return glazierToggle_UIToolkit;
    }

    public ISleekUInt8Field CreateUInt8Field()
    {
        GlazierUInt8Field_UIToolkit glazierUInt8Field_UIToolkit = new GlazierUInt8Field_UIToolkit(this);
        liveElements.Add(glazierUInt8Field_UIToolkit);
        glazierUInt8Field_UIToolkit.SynchronizeColors();
        return glazierUInt8Field_UIToolkit;
    }

    public ISleekUInt16Field CreateUInt16Field()
    {
        GlazierUInt16Field_UIToolkit glazierUInt16Field_UIToolkit = new GlazierUInt16Field_UIToolkit(this);
        liveElements.Add(glazierUInt16Field_UIToolkit);
        glazierUInt16Field_UIToolkit.SynchronizeColors();
        return glazierUInt16Field_UIToolkit;
    }

    public ISleekUInt32Field CreateUInt32Field()
    {
        GlazierUInt32Field_UIToolkit glazierUInt32Field_UIToolkit = new GlazierUInt32Field_UIToolkit(this);
        liveElements.Add(glazierUInt32Field_UIToolkit);
        glazierUInt32Field_UIToolkit.SynchronizeColors();
        return glazierUInt32Field_UIToolkit;
    }

    public ISleekInt32Field CreateInt32Field()
    {
        GlazierInt32Field_UIToolkit glazierInt32Field_UIToolkit = new GlazierInt32Field_UIToolkit(this);
        liveElements.Add(glazierInt32Field_UIToolkit);
        glazierInt32Field_UIToolkit.SynchronizeColors();
        return glazierInt32Field_UIToolkit;
    }

    public ISleekFloat32Field CreateFloat32Field()
    {
        GlazierFloat32Field_UIToolkit glazierFloat32Field_UIToolkit = new GlazierFloat32Field_UIToolkit(this);
        liveElements.Add(glazierFloat32Field_UIToolkit);
        glazierFloat32Field_UIToolkit.SynchronizeColors();
        return glazierFloat32Field_UIToolkit;
    }

    public ISleekFloat64Field CreateFloat64Field()
    {
        GlazierFloat64Field_UIToolkit glazierFloat64Field_UIToolkit = new GlazierFloat64Field_UIToolkit(this);
        liveElements.Add(glazierFloat64Field_UIToolkit);
        glazierFloat64Field_UIToolkit.SynchronizeColors();
        return glazierFloat64Field_UIToolkit;
    }

    public ISleekElement CreateProxyImplementation(SleekWrapper owner)
    {
        GlazierProxy_UIToolkit glazierProxy_UIToolkit = new GlazierProxy_UIToolkit(this, owner);
        liveElements.Add(glazierProxy_UIToolkit);
        glazierProxy_UIToolkit.SynchronizeColors();
        return glazierProxy_UIToolkit;
    }

    public static Glazier_UIToolkit CreateGlazier()
    {
        GameObject obj = new GameObject("Glazier");
        UnityEngine.Object.DontDestroyOnLoad(obj);
        return obj.AddComponent<Glazier_UIToolkit>();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        OptionsSettings.OnThemeChanged += OnThemeChanged;
        OptionsSettings.OnCustomColorsChanged += OnCustomColorsChanged;
        document = base.gameObject.AddComponent<UIDocument>();
        document.panelSettings = Resources.Load<PanelSettings>("UI/Glazier_UIToolkit/PanelSettings");
        document.panelSettings.themeStyleSheet = GlazierResources_UIToolkit.Theme;
        document.visualTreeAsset = Resources.Load<VisualTreeAsset>("UI/Glazier_UIToolkit/DefaultVisualTree");
        focusController = document.rootVisualElement.focusController;
        gameLayer = new VisualElement();
        gameLayer.AddToClassList("unturned-glazier-layer");
        gameLayer.pickingMode = PickingMode.Ignore;
        document.rootVisualElement.Add(gameLayer);
        overlayLayer = new VisualElement();
        overlayLayer.AddToClassList("unturned-glazier-layer");
        overlayLayer.pickingMode = PickingMode.Ignore;
        document.rootVisualElement.Add(overlayLayer);
        CreateDebugLabel();
        CreateCursor();
        CreateTooltip();
    }

    protected void LateUpdate()
    {
        float userInterfaceScale = GraphicsSettings.userInterfaceScale;
        if (MathfEx.IsNearlyEqual(userInterfaceScale, 1f, 0.001f))
        {
            document.panelSettings.scale = 1f;
        }
        else
        {
            document.panelSettings.scale = userInterfaceScale;
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
                    if (focusController.focusedElement != null)
                    {
                        focusController.focusedElement.Blur();
                    }
                }
            }
            rootImpl.IsVisible = Root.isEnabled;
        }
        UpdateDebugLabel();
        UpdateCursor();
        UpdateTooltip();
    }

    internal void RemoveDestroyedElement(GlazierElementBase_UIToolkit element)
    {
        liveElements.Remove(element);
    }

    /// <summary>
    /// Sanity check all returned elements have a gameObject.
    /// </summary>
    [Conditional("VALIDATE_GLAZIER_USE_AFTER_DESTROY")]
    private void ValidateNewElement(GlazierElementBase_UIToolkit element)
    {
        if (element.visualElement == null)
        {
            throw new Exception("UIToolkit element constructed with null visual element");
        }
    }

    /// <summary>
    /// Create software cursor visual element.
    /// </summary>
    private void CreateCursor()
    {
        cursorImage = new Image();
        cursorImage.AddToClassList("unturned-cursor");
        cursorImage.pickingMode = PickingMode.Ignore;
        overlayLayer.Add(cursorImage);
    }

    /// <summary>
    /// Create green label in the upper-left.
    /// </summary>
    private void CreateDebugLabel()
    {
        debugLabel = new Label();
        debugLabel.AddToClassList("unturned-debug");
        debugLabel.pickingMode = PickingMode.Ignore;
        debugLabel.visible = false;
        GlazierUtils_UIToolkit.ApplyTextContrast(debugLabel.style, ETextContrastContext.ColorfulBackdrop, 1f);
        overlayLayer.Add(debugLabel);
    }

    /// <summary>
    /// Create tooltip visual element.
    /// </summary>
    private void CreateTooltip()
    {
        tooltipLabel = new Label();
        tooltipLabel.AddToClassList("unturned-tooltip");
        tooltipLabel.pickingMode = PickingMode.Ignore;
        tooltipLabel.visible = false;
        GlazierUtils_UIToolkit.ApplyTextContrast(debugLabel.style, ETextContrastContext.Tooltip, 1f);
        overlayLayer.Add(tooltipLabel);
        SynchronizeTooltipShadowColor();
    }

    /// <summary>
    /// Update upper-left green text.
    /// </summary>
    private void UpdateDebugLabel()
    {
        if (OptionsSettings.debug && _root != null && (_root.isEnabled || _root.drawCursorWhileDisabled))
        {
            UpdateDebugStats();
            UpdateDebugString();
            debugLabel.style.color = base.debugStringColor;
            debugLabel.text = base.debugString;
            debugLabel.visible = true;
        }
        else
        {
            debugLabel.visible = false;
        }
    }

    /// <summary>
    /// Update software cursor visual element.
    /// </summary>
    private void UpdateCursor()
    {
        cursorImage.visible = Root.ShouldDrawCursor;
        cursorImage.style.unityBackgroundImageTintColor = SleekCustomization.cursorColor;
        Vector2 normalizedMousePosition = InputEx.NormalizedMousePosition;
        normalizedMousePosition.y = 1f - normalizedMousePosition.y;
        cursorImage.style.left = Length.Percent(normalizedMousePosition.x * 100f);
        cursorImage.style.top = Length.Percent(normalizedMousePosition.y * 100f);
    }

    /// <summary>
    /// Find hovered element and update tooltip visibility/text.
    /// </summary>
    private void UpdateTooltip()
    {
        Vector2 screenPosition = Input.mousePosition;
        screenPosition.y = (float)Screen.height - screenPosition.y;
        IPanel panel = document.rootVisualElement.panel;
        Vector2 point = RuntimePanelUtils.ScreenToPanel(panel, screenPosition);
        VisualElement visualElement = panel.Pick(point);
        object obj;
        if (visualElement != null)
        {
            obj = visualElement.userData;
            if (obj == null)
            {
                obj = visualElement.FindAncestorUserData();
            }
        }
        else
        {
            obj = null;
        }
        GlazierElementBase_UIToolkit glazierElementBase_UIToolkit = obj as GlazierElementBase_UIToolkit;
        if (glazierElementBase_UIToolkit != previousTooltipElement)
        {
            previousTooltipElement = glazierElementBase_UIToolkit;
            tooltipFocusTimer = 0f;
        }
        if (glazierElementBase_UIToolkit != null)
        {
            tooltipFocusTimer += Time.unscaledDeltaTime;
        }
        if (Root.ShouldDrawTooltip && glazierElementBase_UIToolkit != null && tooltipFocusTimer >= 0.5f && glazierElementBase_UIToolkit.GetTooltipParameters(out var tooltipText, out var tooltipColor) && !string.IsNullOrEmpty(tooltipText))
        {
            Vector2 normalizedMousePosition = InputEx.NormalizedMousePosition;
            normalizedMousePosition.y = 1f - normalizedMousePosition.y;
            tooltipLabel.style.top = Length.Percent(normalizedMousePosition.y * 100f);
            if (normalizedMousePosition.x > 0.7f)
            {
                tooltipLabel.style.left = StyleKeyword.Null;
                tooltipLabel.style.right = Length.Percent((1f - normalizedMousePosition.x) * 100f);
                tooltipLabel.style.marginLeft = 0f;
                tooltipLabel.style.marginRight = 10f;
                tooltipLabel.style.unityTextAlign = TextAnchor.UpperRight;
            }
            else
            {
                tooltipLabel.style.left = Length.Percent(normalizedMousePosition.x * 100f);
                tooltipLabel.style.right = StyleKeyword.Null;
                tooltipLabel.style.marginLeft = 30f;
                tooltipLabel.style.marginRight = 0f;
                tooltipLabel.style.unityTextAlign = TextAnchor.UpperLeft;
            }
            tooltipLabel.text = tooltipText;
            tooltipLabel.style.color = tooltipColor;
            tooltipLabel.visible = true;
        }
        else
        {
            tooltipLabel.visible = false;
        }
    }

    private void SynchronizeTooltipShadowColor()
    {
        Color shadowColor = SleekCustomization.shadowColor;
        shadowColor.a = 0.5f;
        tooltipLabel.style.unityBackgroundImageTintColor = shadowColor;
    }

    private void OnThemeChanged()
    {
        document.panelSettings.themeStyleSheet = GlazierResources_UIToolkit.Theme;
    }

    private void OnCustomColorsChanged()
    {
        foreach (GlazierElementBase_UIToolkit liveElement in liveElements)
        {
            liveElement.SynchronizeColors();
        }
        SynchronizeTooltipShadowColor();
    }
}
