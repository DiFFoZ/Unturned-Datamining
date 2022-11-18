using System;
using UnityEngine;
using UnityEngine.UI;

namespace SDG.Unturned;

internal class GlazierSprite_uGUI : GlazierElementBase_uGUI, ISleekSprite, ISleekElement
{
    private SleekColor _color;

    private ESleekSpriteType _drawMethod;

    private Image imageComponent;

    private ButtonEx buttonComponent;

    public Sprite sprite
    {
        get
        {
            return imageComponent.sprite;
        }
        set
        {
            imageComponent.sprite = value;
        }
    }

    public SleekColor color
    {
        get
        {
            return _color;
        }
        set
        {
            _color = value;
            SynchronizeColors();
        }
    }

    public ESleekSpriteType drawMethod
    {
        get
        {
            return _drawMethod;
        }
        set
        {
            _drawMethod = value;
            switch (value)
            {
            case ESleekSpriteType.Tiled:
                imageComponent.type = Image.Type.Tiled;
                break;
            case ESleekSpriteType.Sliced:
                imageComponent.type = Image.Type.Sliced;
                break;
            default:
                imageComponent.type = Image.Type.Simple;
                break;
            }
        }
    }

    public bool isRaycastTarget
    {
        get
        {
            throw new NotImplementedException();
        }
        set
        {
            throw new NotImplementedException();
        }
    }

    private event System.Action _onImageClicked;

    public event System.Action onImageClicked
    {
        add
        {
            if (buttonComponent == null)
            {
                CreateButton();
            }
            _onImageClicked += value;
        }
        remove
        {
            _onImageClicked -= value;
        }
    }

    public GlazierSprite_uGUI(Glazier_uGUI glazier, Sprite sprite)
        : base(glazier)
    {
    }

    public override void ConstructNew()
    {
        base.ConstructNew();
        imageComponent = base.gameObject.AddComponent<Image>();
        imageComponent.enabled = false;
        imageComponent.raycastTarget = false;
        imageComponent.sprite = sprite;
        _color = ESleekTint.NONE;
        drawMethod = ESleekSpriteType.Tiled;
    }

    public override void SynchronizeColors()
    {
        if (sprite != null)
        {
            imageComponent.color = _color;
            imageComponent.enabled = true;
        }
        else if (imageComponent.raycastTarget)
        {
            imageComponent.color = ColorEx.BlackZeroAlpha;
            imageComponent.enabled = true;
        }
        else
        {
            imageComponent.enabled = false;
        }
    }

    protected override void EnableComponents()
    {
        imageComponent.enabled = sprite != null || imageComponent.raycastTarget;
    }

    private void CreateButton()
    {
        imageComponent.raycastTarget = true;
        buttonComponent = base.gameObject.AddComponent<ButtonEx>();
        buttonComponent.transition = Selectable.Transition.None;
        buttonComponent.onClick.AddListener(OnUnityClick);
        SynchronizeColors();
    }

    private void OnUnityClick()
    {
        this._onImageClicked?.Invoke();
    }
}
