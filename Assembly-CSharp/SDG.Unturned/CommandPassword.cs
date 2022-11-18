using Steamworks;

namespace SDG.Unturned;

public class CommandPassword : Command
{
    protected override void execute(CSteamID executorID, string parameter)
    {
        if (Provider.isServer)
        {
            CommandWindow.LogError(localization.format("RunningErrorText"));
            return;
        }
        if (string.IsNullOrEmpty(parameter))
        {
            Provider.serverPassword = string.Empty;
            CommandWindow.Log(localization.format("DisableText"));
            return;
        }
        Provider.serverPassword = parameter.Trim();
        if (localization.has("PasswordTextV2"))
        {
            CommandWindow.Log(localization.format("PasswordTextV2"));
        }
        else
        {
            CommandWindow.Log(localization.format("PasswordText", "******"));
        }
    }

    public CommandPassword(Local newLocalization)
    {
        localization = newLocalization;
        _command = localization.format("PasswordCommandText");
        _info = localization.format("PasswordInfoText");
        _help = localization.format("PasswordHelpText");
    }
}
