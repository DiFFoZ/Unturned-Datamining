using System.Collections.Generic;
using System.Text;

namespace SDG.Unturned;

/// <summary>
/// Converts Steam BBcode tokens into widgets displayable using Glazier UI.
/// </summary>
public class BbCodeWidgetConverter
{
    private List<BbCodeToken> inputTokens;

    private int inputIndex;

    private bool hasToken;

    private BbCodeToken currentToken;

    private bool hasError;

    private string errorMessage;

    private StringBuilder richTextStringBuilder;

    public bool HasError => hasError;

    public string ErrorMessage
    {
        get
        {
            return errorMessage;
        }
        private set
        {
            errorMessage = value;
            hasError = !string.IsNullOrEmpty(errorMessage);
        }
    }

    public BbCodeWidgetConverter()
    {
        richTextStringBuilder = new StringBuilder();
    }

    public List<BbCodeWidget> Convert(List<BbCodeToken> tokens)
    {
        richTextStringBuilder.Clear();
        inputTokens = tokens;
        inputIndex = -1;
        hasToken = false;
        List<BbCodeWidget> list = new List<BbCodeWidget>();
        AdvanceToken();
        int num = 0;
        while (hasToken)
        {
            ConvertToken(list);
            num++;
            if (num >= 10000)
            {
                ErrorMessage = "Infinite loop attempting to convert tokens into widgets";
                break;
            }
        }
        return list;
    }

    private void AdvanceToken()
    {
        inputIndex++;
        hasToken = inputIndex < inputTokens.Count;
        if (hasToken)
        {
            currentToken = inputTokens[inputIndex];
        }
    }

    private void ConvertToken(List<BbCodeWidget> outputWidgets)
    {
        if (currentToken.tokenType == EBbCodeTokenType.PreviewYouTubeOpen)
        {
            ConvertPreviewYouTube(outputWidgets);
        }
        else if (currentToken.tokenType == EBbCodeTokenType.ImgOpen)
        {
            ConvertImage(outputWidgets);
        }
        else if (currentToken.tokenType == EBbCodeTokenType.UrlOpen)
        {
            ConvertLinkButton(outputWidgets);
        }
        else
        {
            ConvertRichText(outputWidgets);
        }
    }

    private void ConvertPreviewYouTube(List<BbCodeWidget> outputWidgets)
    {
        if (!string.IsNullOrEmpty(currentToken.tokenValue))
        {
            int num = currentToken.tokenValue.IndexOf(';');
            string widgetData = ((num <= 0) ? currentToken.tokenValue : currentToken.tokenValue.Substring(0, num));
            outputWidgets.Add(new BbCodeWidget(EBbCodeWidgetType.YouTubeButton, widgetData));
        }
        AdvanceToken();
        if (hasToken && currentToken.tokenType == EBbCodeTokenType.PreviewYouTubeClose)
        {
            AdvanceToken();
        }
        if (hasToken && currentToken.tokenType == EBbCodeTokenType.LineBreak)
        {
            AdvanceToken();
        }
    }

    private void ConvertImage(List<BbCodeWidget> outputWidgets)
    {
        AdvanceToken();
        if (hasToken && currentToken.tokenType == EBbCodeTokenType.String)
        {
            outputWidgets.Add(new BbCodeWidget(EBbCodeWidgetType.Image, currentToken.tokenValue));
            AdvanceToken();
        }
        if (hasToken && currentToken.tokenType == EBbCodeTokenType.ImgClose)
        {
            AdvanceToken();
        }
        if (hasToken && currentToken.tokenType == EBbCodeTokenType.LineBreak)
        {
            AdvanceToken();
        }
    }

    private void ConvertLinkButton(List<BbCodeWidget> outputWidgets)
    {
        string tokenValue = currentToken.tokenValue;
        string text = null;
        AdvanceToken();
        if (hasToken && currentToken.tokenType == EBbCodeTokenType.String)
        {
            if (string.IsNullOrEmpty(tokenValue))
            {
                tokenValue = currentToken.tokenValue;
            }
            else
            {
                text = currentToken.tokenValue;
            }
            AdvanceToken();
        }
        if (hasToken && currentToken.tokenType == EBbCodeTokenType.UrlClose)
        {
            AdvanceToken();
        }
        if (hasToken && currentToken.tokenType == EBbCodeTokenType.LineBreak)
        {
            AdvanceToken();
        }
        if (string.IsNullOrEmpty(text))
        {
            outputWidgets.Add(new BbCodeWidget(EBbCodeWidgetType.LinkButton, tokenValue));
        }
        else
        {
            outputWidgets.Add(new BbCodeWidget(EBbCodeWidgetType.LinkButton, tokenValue + "," + text));
        }
    }

    private void ConvertRichText(List<BbCodeWidget> outputWidgets)
    {
        richTextStringBuilder.Clear();
        bool flag = false;
        int num = 0;
        do
        {
            switch (currentToken.tokenType)
            {
            case EBbCodeTokenType.String:
                richTextStringBuilder.Append(currentToken.tokenValue);
                break;
            case EBbCodeTokenType.BoldOpen:
                richTextStringBuilder.Append("<b>");
                break;
            case EBbCodeTokenType.BoldClose:
                richTextStringBuilder.Append("</b>");
                break;
            case EBbCodeTokenType.ItalicOpen:
                richTextStringBuilder.Append("<i>");
                break;
            case EBbCodeTokenType.ItalicClose:
                richTextStringBuilder.Append("</i>");
                break;
            case EBbCodeTokenType.H1Open:
                richTextStringBuilder.Append("<size=20>");
                break;
            case EBbCodeTokenType.H1Close:
                richTextStringBuilder.Append("</size>");
                break;
            case EBbCodeTokenType.H2Open:
                richTextStringBuilder.Append("<size=17>");
                break;
            case EBbCodeTokenType.H2Close:
                richTextStringBuilder.Append("</size>");
                break;
            case EBbCodeTokenType.H3Open:
                richTextStringBuilder.Append("<size=14>");
                break;
            case EBbCodeTokenType.H3Close:
                richTextStringBuilder.Append("</size>");
                break;
            case EBbCodeTokenType.BulletListOpen:
            case EBbCodeTokenType.BulletListClose:
                PushPendingRichText(outputWidgets);
                break;
            case EBbCodeTokenType.OrderedListOpen:
                flag = true;
                num = 0;
                PushPendingRichText(outputWidgets);
                break;
            case EBbCodeTokenType.OrderedListClose:
                flag = false;
                PushPendingRichText(outputWidgets);
                break;
            case EBbCodeTokenType.ListItem:
                if (flag)
                {
                    richTextStringBuilder.Append(num + 1);
                    richTextStringBuilder.Append(". ");
                    num++;
                }
                else
                {
                    richTextStringBuilder.Append("- ");
                }
                break;
            case EBbCodeTokenType.LineBreak:
                richTextStringBuilder.Append('\n');
                break;
            }
            AdvanceToken();
        }
        while (currentToken.tokenType != EBbCodeTokenType.PreviewYouTubeOpen && currentToken.tokenType != EBbCodeTokenType.ImgOpen && currentToken.tokenType != EBbCodeTokenType.UrlOpen && hasToken);
        PushPendingRichText(outputWidgets);
    }

    private void PushPendingRichText(List<BbCodeWidget> outputWidgets)
    {
        if (richTextStringBuilder.Length > 0)
        {
            string widgetData = richTextStringBuilder.ToString();
            outputWidgets.Add(new BbCodeWidget(EBbCodeWidgetType.RichTextLabel, widgetData));
            richTextStringBuilder.Clear();
        }
    }
}
