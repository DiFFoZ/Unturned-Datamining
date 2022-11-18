using Steamworks;

namespace SDG.Unturned;

public class CommandHideAdmins : Command
{
    protected override void execute(CSteamID executorID, string parameter)
    {
        if (Provider.isServer)
        {
            CommandWindow.LogError(localization.format("RunningErrorText"));
            return;
        }
        Provider.hideAdmins = true;
        CommandWindow.Log(localization.format("HideAdminsText"));
    }

    public CommandHideAdmins(Local newLocalization)
    {
        localization = newLocalization;
        _command = localization.format("HideAdminsCommandText");
        _info = localization.format("HideAdminsInfoText");
        _help = localization.format("HideAdminsHelpText");
    }
}
