using UnityEngine;

namespace SDG.Unturned;

public interface IGlazier
{
    SleekWindow Root { get; set; }

    bool ShouldGameProcessInput { get; }

    bool ShouldGameProcessKeyDown { get; }

    bool SupportsDepth { get; }

    bool SupportsRichTextAlpha { get; }

    bool SupportsAutomaticLayout { get; }

    bool SupportsTilingSprite { get; }

    ISleekBox CreateBox();

    ISleekButton CreateButton();

    ISleekElement CreateFrame();

    ISleekConstraintFrame CreateConstraintFrame();

    ISleekImage CreateImage();

    ISleekImage CreateImage(Texture texture);

    ISleekSprite CreateSprite();

    ISleekSprite CreateSprite(Sprite sprite);

    ISleekLabel CreateLabel();

    ISleekScrollView CreateScrollView();

    ISleekSlider CreateSlider();

    ISleekField CreateStringField();

    ISleekToggle CreateToggle();

    ISleekUInt8Field CreateUInt8Field();

    ISleekUInt16Field CreateUInt16Field();

    ISleekUInt32Field CreateUInt32Field();

    ISleekInt32Field CreateInt32Field();

    ISleekFloat32Field CreateFloat32Field();

    ISleekFloat64Field CreateFloat64Field();

    ISleekElement CreateProxyImplementation(SleekWrapper owner);
}
