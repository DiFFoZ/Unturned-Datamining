namespace SDG.Unturned;

public class ChatPreferenceData
{
    public float Fade_Delay;

    public int History_Length;

    public int Preview_Length;

    /// <summary>
    /// If true, messages in the HUD will fade out after Fade_Delay.
    /// </summary>
    public bool Enable_Fade_Out;

    public ChatPreferenceData()
    {
        Fade_Delay = 10f;
        History_Length = 16;
        Preview_Length = 5;
        Enable_Fade_Out = true;
    }
}
