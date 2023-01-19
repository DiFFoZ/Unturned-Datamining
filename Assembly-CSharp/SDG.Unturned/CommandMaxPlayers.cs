using System;
using Steamworks;

namespace SDG.Unturned;

public class CommandMaxPlayers : Command
{
    public static readonly byte MIN_NUMBER = 1;

    [Obsolete]
    public static readonly byte RECOMMENDED_NUMBER = 24;

    public static readonly byte MAX_NUMBER = 200;

    protected override void execute(CSteamID executorID, string parameter)
    {
        if (Dedicator.IsDedicatedServer)
        {
            if (!byte.TryParse(parameter, out var result))
            {
                CommandWindow.LogError(localization.format("InvalidNumberErrorText", parameter));
                return;
            }
            if (result < MIN_NUMBER)
            {
                CommandWindow.LogError(localization.format("MinNumberErrorText", MIN_NUMBER));
                return;
            }
            if (result > MAX_NUMBER)
            {
                CommandWindow.LogError(localization.format("MaxNumberErrorText", MAX_NUMBER));
                return;
            }
            Provider.maxPlayers = result;
            CommandWindow.Log(localization.format("MaxPlayersText", result));
        }
    }

    public CommandMaxPlayers(Local newLocalization)
    {
        localization = newLocalization;
        _command = localization.format("MaxPlayersCommandText");
        _info = localization.format("MaxPlayersInfoText");
        _help = localization.format("MaxPlayersHelpText");
    }
}
