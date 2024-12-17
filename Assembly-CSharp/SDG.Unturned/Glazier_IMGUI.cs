using UnityEngine;

namespace SDG.Unturned;

internal class Glazier_IMGUI : GlazierBase, IGlazier
{
    private SleekWindow _root;

    private GlazierElementBase_IMGUI rootImpl;

    private int cachedScreenWidth = -1;

    private int cachedScreenHeight = -1;

    private string lastTooltip;

    private float startedTooltip;

    private GUIContent tooltipContent;

    private GUIContent tooltipShadowContent;

    private static StaticResourceRef<Texture2D> defaultCursor = new StaticResourceRef<Texture2D>("UI/Glazier_IMGUI/Cursor");

    public bool SupportsDepth => false;

    public bool SupportsRichTextAlpha => false;

    public bool SupportsAutomaticLayout => false;

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
            _root = value;
            if (_root != null)
            {
                rootImpl = _root.AttachmentRoot as GlazierElementBase_IMGUI;
                if (rootImpl != null)
                {
                    rootImpl.isTransformDirty = true;
                    return;
                }
                UnturnedLog.warn("Root must be an IMGUI element: {0}", _root.GetType().Name);
            }
            else
            {
                rootImpl = null;
            }
        }
    }

    public ISleekBox CreateBox()
    {
        return new GlazierBox_IMGUI();
    }

    public ISleekButton CreateButton()
    {
        return new GlazierButton_IMGUI();
    }

    public ISleekElement CreateFrame()
    {
        return new GlazierElementBase_IMGUI();
    }

    public ISleekConstraintFrame CreateConstraintFrame()
    {
        return new GlazierConstraintFrame_IMGUI();
    }

    public ISleekImage CreateImage()
    {
        return new GlazierImage_IMGUI(null);
    }

    public ISleekImage CreateImage(Texture texture)
    {
        return new GlazierImage_IMGUI(texture);
    }

    public ISleekSprite CreateSprite()
    {
        return new GlazierSprite_IMGUI(null);
    }

    public ISleekSprite CreateSprite(Sprite sprite)
    {
        return new GlazierSprite_IMGUI(sprite);
    }

    public ISleekLabel CreateLabel()
    {
        return new GlazierLabel_IMGUI();
    }

    public ISleekScrollView CreateScrollView()
    {
        return new GlazierScrollView_IMGUI();
    }

    public ISleekSlider CreateSlider()
    {
        return new GlazierSlider_IMGUI();
    }

    public ISleekField CreateStringField()
    {
        return new GlazierStringField_IMGUI();
    }

    public ISleekToggle CreateToggle()
    {
        return new GlazierToggle_IMGUI();
    }

    public ISleekUInt8Field CreateUInt8Field()
    {
        return new GlazierUInt8Field_IMGUI();
    }

    public ISleekUInt16Field CreateUInt16Field()
    {
        return new GlazierUInt16Field_IMGUI();
    }

    public ISleekUInt32Field CreateUInt32Field()
    {
        return new GlazierUInt32Field_IMGUI();
    }

    public ISleekInt32Field CreateInt32Field()
    {
        return new GlazierInt32Field_IMGUI();
    }

    public ISleekFloat32Field CreateFloat32Field()
    {
        return new GlazierFloat32Field_IMGUI();
    }

    public ISleekFloat64Field CreateFloat64Field()
    {
        return new GlazierFloat64Field_IMGUI();
    }

    public ISleekElement CreateProxyImplementation(SleekWrapper owner)
    {
        return new GlazierProxy_IMGUI(owner);
    }

    public static Glazier_IMGUI CreateGlazier()
    {
        GameObject obj = new GameObject("Glazier");
        Object.DontDestroyOnLoad(obj);
        return obj.AddComponent<Glazier_IMGUI>();
    }

    private void LateUpdate()
    {
        if (rootImpl != null)
        {
            if (Screen.width != cachedScreenWidth || Screen.height != cachedScreenHeight)
            {
                cachedScreenWidth = Screen.width;
                cachedScreenHeight = Screen.height;
                rootImpl.isTransformDirty = true;
            }
            rootImpl.Update();
        }
        if (OptionsSettings.debug)
        {
            UpdateDebugStats();
            UpdateDebugString();
        }
    }

    private void CursorOnGUI()
    {
        if (Root.ShouldDrawCursor)
        {
            Rect position = new Rect(Input.mousePosition.x, (float)Screen.height - Input.mousePosition.y, 20f, 20f);
            GUI.color = SleekCustomization.cursorColor;
            GUI.DrawTexture(position, (Texture2D)defaultCursor);
            GUI.color = Color.white;
        }
    }

    private void TooltipOnGUI()
    {
        if (!Root.ShouldDrawTooltip)
        {
            return;
        }
        string tooltip = GUI.tooltip;
        if (tooltip != lastTooltip)
        {
            lastTooltip = tooltip;
            startedTooltip = Time.realtimeSinceStartup;
            tooltipContent = new GUIContent(tooltip);
            tooltipShadowContent = RichTextUtil.makeShadowContent(tooltipContent);
        }
        if (!string.IsNullOrWhiteSpace(tooltip) && Time.realtimeSinceStartup - startedTooltip > 0.5f)
        {
            Rect area = new Rect(0f, (float)Screen.height - Input.mousePosition.y, 400f, 200f);
            Color fontColor = OptionsSettings.fontColor;
            if (Input.mousePosition.x > (float)Screen.width - area.width - 30f)
            {
                area.x = Input.mousePosition.x - 30f - area.width;
                GlazierUtils_IMGUI.drawLabel(area, FontStyle.Bold, TextAnchor.UpperRight, 12, tooltipShadowContent, fontColor, tooltipContent, ETextContrastContext.Tooltip);
            }
            else
            {
                area.x = Input.mousePosition.x + 30f;
                GlazierUtils_IMGUI.drawLabel(area, FontStyle.Bold, TextAnchor.UpperLeft, 12, tooltipShadowContent, fontColor, tooltipContent, ETextContrastContext.Tooltip);
            }
        }
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        base.useGUILayout = false;
    }

    private void OnGUI()
    {
        GUI.skin = GlazierResources_IMGUI.ActiveSkin;
        if (_root != null && _root.isEnabled && rootImpl != null)
        {
            rootImpl.OnGUI();
        }
        if (Event.current.type == EventType.Repaint)
        {
            if (OptionsSettings.debug)
            {
                GlazierUtils_IMGUI.drawLabel(new Rect(0f, 0f, 800f, 30f), FontStyle.Normal, TextAnchor.UpperLeft, 12, isRich: false, base.debugStringColor, base.debugString, ETextContrastContext.ColorfulBackdrop);
            }
            CursorOnGUI();
            TooltipOnGUI();
        }
    }
}
