using System;
using System.Collections.Generic;
using Steamworks;

namespace SDG.Unturned;

public class CommandWindow
{
    public static CommandWindowInputted onCommandWindowInputted;

    public static CommandWindowOutputted onCommandWindowOutputted;

    public static CommandLineFlag shouldCreateDefaultConsole = new CommandLineFlag(defaultValue: true, "-NoDefaultConsole");

    private static CommandLineFlag shouldCreateLegacyConsole = new CommandLineFlag(defaultValue: false, "-LegacyConsole");

    private string _title;

    public static bool shouldLogChat = true;

    public static bool shouldLogJoinLeave = true;

    public static bool shouldLogDeaths = true;

    public static bool shouldLogAnticheat = false;

    private static bool insideExplicitLogging = false;

    private List<ICommandInputOutput> ioHandlers = new List<ICommandInputOutput>();

    private ICommandInputOutput defaultIOHandler;

    public string title
    {
        get
        {
            return _title;
        }
        set
        {
            _title = value;
            this.onTitleChanged?.Invoke(_title);
        }
    }

    public event CommandWindowTitleChanged onTitleChanged;

    public static void Log(object text)
    {
        if (insideExplicitLogging)
        {
            return;
        }
        try
        {
            insideExplicitLogging = true;
            UnturnedLog.info(text);
            Dedicator.commandWindow?.internalLogInformation(text.ToString());
        }
        finally
        {
            insideExplicitLogging = false;
        }
    }

    public static void LogFormat(string format, params object[] args)
    {
        Log(string.Format(format, args));
    }

    public static void LogWarning(object text)
    {
        if (insideExplicitLogging)
        {
            return;
        }
        try
        {
            insideExplicitLogging = true;
            UnturnedLog.warn(text);
            Dedicator.commandWindow?.internalLogWarning(text.ToString());
        }
        finally
        {
            insideExplicitLogging = false;
        }
    }

    public static void LogWarningFormat(string format, params object[] args)
    {
        LogWarning(string.Format(format, args));
    }

    public static void LogError(object text)
    {
        if (insideExplicitLogging)
        {
            return;
        }
        try
        {
            insideExplicitLogging = true;
            UnturnedLog.error(text);
            Dedicator.commandWindow?.internalLogError(text.ToString());
        }
        finally
        {
            insideExplicitLogging = false;
        }
    }

    public static void LogErrorFormat(string format, params object[] args)
    {
        LogError(string.Format(format, args));
    }

    private void internalLogInformation(string information)
    {
        try
        {
            onCommandWindowOutputted?.Invoke(information, ConsoleColor.White);
        }
        catch (Exception exception)
        {
            HandleException("Plugin threw an exception from info onCommandWindowOutputted:", exception);
        }
        foreach (ICommandInputOutput ioHandler in ioHandlers)
        {
            try
            {
                ioHandler.outputInformation(information);
            }
            catch (Exception exception2)
            {
                HandleException($"Command IO handler {ioHandler} threw an exception from outputInformation:", exception2);
            }
        }
    }

    private void internalLogWarning(string warning)
    {
        try
        {
            onCommandWindowOutputted?.Invoke(warning, ConsoleColor.Yellow);
        }
        catch (Exception exception)
        {
            HandleException("Plugin threw an exception from warning onCommandWindowOutputted:", exception);
        }
        foreach (ICommandInputOutput ioHandler in ioHandlers)
        {
            try
            {
                ioHandler.outputWarning(warning);
            }
            catch (Exception exception2)
            {
                HandleException($"Command IO handler {ioHandler} threw an exception from outputWarning:", exception2);
            }
        }
    }

    private void internalLogError(string error)
    {
        try
        {
            onCommandWindowOutputted?.Invoke(error, ConsoleColor.Red);
        }
        catch (Exception exception)
        {
            HandleException("Plugin threw an exception from error onCommandWindowOutputted:", exception);
        }
        foreach (ICommandInputOutput ioHandler in ioHandlers)
        {
            try
            {
                ioHandler.outputError(error);
            }
            catch (Exception exception2)
            {
                HandleException($"Command IO handler {ioHandler} threw an exception from outputError:", exception2);
            }
        }
    }

    private void onInputCommitted(string input)
    {
        bool shouldExecuteCommand = true;
        try
        {
            onCommandWindowInputted?.Invoke(input, ref shouldExecuteCommand);
        }
        catch (Exception exception)
        {
            HandleException("Plugin threw an exception from onCommandWindowInputted:", exception);
        }
        if (shouldExecuteCommand && !Commander.execute(CSteamID.Nil, input))
        {
            LogErrorFormat("Unable to match \"{0}\" with any built-in commands", input);
        }
    }

    private void HandleException(string message, Exception exception)
    {
        Logs.printLine(message);
        do
        {
            Logs.printLine(exception.Message);
            Logs.printLine(exception.StackTrace);
            exception = exception.InnerException;
        }
        while (exception != null);
    }

    public void update()
    {
        foreach (ICommandInputOutput ioHandler in ioHandlers)
        {
            try
            {
                ioHandler.update();
            }
            catch (Exception exception)
            {
                HandleException($"Command IO handler {ioHandler} threw an exception from update:", exception);
            }
        }
    }

    private void initializeIOHandler(ICommandInputOutput handler)
    {
        try
        {
            handler.initialize(this);
        }
        catch (Exception e)
        {
            UnturnedLog.exception(e);
        }
        handler.inputCommitted += onInputCommitted;
    }

    private void shutdownIOHandler(ICommandInputOutput handler)
    {
        handler.inputCommitted -= onInputCommitted;
        try
        {
            handler.shutdown(this);
        }
        catch (Exception e)
        {
            UnturnedLog.exception(e);
        }
    }

    public void shutdown()
    {
        List<ICommandInputOutput> list = new List<ICommandInputOutput>(ioHandlers);
        ioHandlers.Clear();
        foreach (ICommandInputOutput item in list)
        {
            shutdownIOHandler(item);
        }
    }

    public void removeDefaultIOHandler()
    {
        if (defaultIOHandler != null)
        {
            removeIOHandler(defaultIOHandler);
            defaultIOHandler = null;
        }
    }

    public void removeIOHandler(ICommandInputOutput handler)
    {
        if (handler == null)
        {
            throw new ArgumentNullException("handler");
        }
        ioHandlers.RemoveFast(handler);
        shutdownIOHandler(handler);
    }

    public void addIOHandler(ICommandInputOutput handler)
    {
        if (handler == null)
        {
            throw new ArgumentNullException("handler");
        }
        if (ioHandlers.Contains(handler))
        {
            throw new NotSupportedException("handler already registered");
        }
        ioHandlers.Add(handler);
        initializeIOHandler(handler);
    }

    [Obsolete("Use addIOHandler instead (multiple simultaneous handlers now supported)")]
    public void setIOHandler(ICommandInputOutput newHandler)
    {
        addIOHandler(newHandler);
    }

    protected ICommandInputOutput createDefaultIOHandler()
    {
        if (!shouldCreateDefaultConsole)
        {
            return null;
        }
        if ((bool)shouldCreateLegacyConsole)
        {
            return new LegacyInputOutput();
        }
        return new ThreadedConsoleInputOutput();
    }

    public CommandWindow()
    {
        defaultIOHandler = createDefaultIOHandler();
        if (defaultIOHandler != null)
        {
            addIOHandler(defaultIOHandler);
        }
    }
}
