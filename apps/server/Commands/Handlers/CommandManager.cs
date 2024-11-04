using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using ACE.Entity.Enum;
using ACE.Server.Network;
using Serilog;

namespace ACE.Server.Commands.Handlers;

public static class CommandManager
{
    private static readonly ILogger _log = Log.ForContext(typeof(CommandManager));

    public static readonly bool NonInteractiveConsole = Convert.ToBoolean(
        Environment.GetEnvironmentVariable("ACE_NONINTERACTIVE_CONSOLE")
    );

    private static Dictionary<string, CommandHandlerInfo> commandHandlers = new Dictionary<string, CommandHandlerInfo>(
        StringComparer.OrdinalIgnoreCase
    );

    public static IEnumerable<CommandHandlerInfo> GetCommands()
    {
        return commandHandlers.Select(p => p.Value);
    }

    public static IEnumerable<CommandHandlerInfo> GetCommandByName(string commandname)
    {
        return commandHandlers.Select(p => p.Value).Where(p => p.Attribute.Command == commandname);
    }

    public static CommandHandler GetDelegate(Action<Session, string[]> handler) =>
        (CommandHandler)Delegate.CreateDelegate(typeof(CommandHandler), handler.Method);

    public static bool TryAddCommand(
        MethodInfo handler,
        string command,
        AccessLevel access,
        CommandHandlerFlag flags = CommandHandlerFlag.None,
        string description = "",
        string usage = "",
        bool overrides = true
    )
    {
        var del = (CommandHandler)Delegate.CreateDelegate(typeof(CommandHandler), handler);

        var info = new CommandHandlerInfo()
        {
            Attribute = new CommandHandlerAttribute(command, access, flags, description, usage),
            Handler = del,
        };
        return TryAddCommand(info, overrides);
    }

    public static bool TryAddCommand(
        Action<Session, string[]> handler,
        string command,
        AccessLevel access,
        CommandHandlerFlag flags = CommandHandlerFlag.None,
        string description = "",
        string usage = "",
        bool overrides = true
    )
    {
        var del = (CommandHandler)Delegate.CreateDelegate(typeof(CommandHandler), handler.Method);
        var info = new CommandHandlerInfo()
        {
            Attribute = new CommandHandlerAttribute(command, access, flags, description, usage),
            Handler = del
        };

        if (TryAddCommand(info, overrides))
        {
            return true;
        }

        return false;
    }

    public static bool TryAddCommand(CommandHandlerInfo commandHandler, bool overrides = true)
    {
        if (commandHandler is null)
        {
            return false;
        }

        var command = commandHandler.Attribute.Command;

        //Add if the command doesn't exist
        if (!commandHandlers.ContainsKey(command))
        {
            commandHandlers.Add(command, commandHandler);
            _log.Information("Command created: {Command}", command);
            return true;
        }
        //Update if overriding and the command exists
        else if (overrides)
        {
            _log.Information("Command updated: {Command}", command);
            commandHandlers[command] = commandHandler;
            return true;
        }
        _log.Warning("Failed to add command: {Command}", command);
        return false;
    }

    public static bool TryRemoveCommand(string command)
    {
        if (!commandHandlers.ContainsKey(command))
        {
            return false;
        }

        _log.Information("Removed command: {Command}", command);
        commandHandlers.Remove(command);
        return true;
    }

    public static void Initialize()
    {
        foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
        {
            foreach (var method in type.GetMethods())
            {
                foreach (var attribute in method.GetCustomAttributes<CommandHandlerAttribute>())
                {
                    var commandHandler = new CommandHandlerInfo()
                    {
                        Handler = (CommandHandler)Delegate.CreateDelegate(typeof(CommandHandler), method),
                        Attribute = attribute
                    };

                    commandHandlers[attribute.Command] = commandHandler;
                }
            }
        }

        if (NonInteractiveConsole)
        {
            _log.Information(
                "ACEmulator command prompt disabled - Environment.GetEnvironmentVariable(ACE_NONINTERACTIVE_CONSOLE) was true"
            );
            return;
        }

        var thread = new Thread(new ThreadStart(CommandThread));
        thread.Name = "Command Manager";
        thread.IsBackground = true;
        thread.Start();
    }

    private static void CommandThread()
    {
        Console.WriteLine("");
        Console.WriteLine("ACEmulator command prompt ready.");
        Console.WriteLine("");
        Console.WriteLine("Type \"acecommands\" for help.");
        Console.WriteLine("");

        for (; ; )
        {
            Console.Write("ACE >> ");

            var commandLine = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(commandLine))
            {
                continue;
            }

            string command = null;
            string[] parameters = null;
            try
            {
                ParseCommand(commandLine, out command, out parameters);
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Exception while parsing command: {Line}", commandLine);
                return;
            }
            try
            {
                if (GetCommandHandler(null, command, parameters, out var commandHandler) == CommandHandlerResponse.Ok)
                {
                    try
                    {
                        if (commandHandler.Attribute.IncludeRaw)
                        {
                            parameters = StuffRawIntoParameters(commandLine, command, parameters);
                        }
                        // Add command to world manager's main thread...
                        ((CommandHandler)commandHandler.Handler).Invoke(null, parameters);
                    }
                    catch (Exception ex)
                    {
                        _log.Error(ex, "Exception while invoking command handler for: {Line}", commandLine);
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Exception while getting command handler for: {Line}", commandLine);
            }
        }
    }

    public static string[] StuffRawIntoParameters(string raw, string command, string[] parameters)
    {
        var parametersRehash = new List<string>();
        var regex = new Regex(Regex.Escape(command));
        var newCmdLine = regex.Replace(raw, "", 1).TrimStart();
        parametersRehash.Add(newCmdLine);
        parametersRehash.AddRange(parameters);
        parameters = parametersRehash.ToArray();
        return parameters;
    }

    public static void ParseCommand(string commandLine, out string command, out string[] parameters)
    {
        if (commandLine == "/" || commandLine == "")
        {
            command = null;
            parameters = null;
            return;
        }
        var commandSplit = commandLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        command = commandSplit[0];

        // remove leading '/' or '@' if erroneously entered in console
        if (command.StartsWith("/") || command.StartsWith("@"))
        {
            command = command.Substring(1);
        }

        parameters = new string[commandSplit.Length - 1];

        Array.Copy(commandSplit, 1, parameters, 0, commandSplit.Length - 1);

        if (commandLine.Contains("\""))
        {
            var listParameters = new List<string>();

            for (var start = 0; start < parameters.Length; start++)
            {
                if (!parameters[start].StartsWith("\"") || parameters[start].EndsWith("\"")) // Make sure we catch parameters like: "someParam"
                {
                    listParameters.Add(parameters[start].Replace("\"", ""));
                }
                else
                {
                    listParameters.Add(parameters[start].Replace("\"", ""));
                    for (var end = start + 1; end < parameters.Length; end++)
                    {
                        if (!parameters[end].EndsWith("\""))
                        {
                            listParameters[listParameters.Count - 1] += " " + parameters[end];
                        }
                        else
                        {
                            listParameters[listParameters.Count - 1] += " " + parameters[end].Replace("\"", "");
                            start = end;
                            break;
                        }
                    }
                }
            }
            Array.Resize(ref parameters, listParameters.Count);
            parameters = listParameters.ToArray();
        }
    }

    public static CommandHandlerResponse GetCommandHandler(
        Session session,
        string command,
        string[] parameters,
        out CommandHandlerInfo commandInfo
    )
    {
        if (command == null || parameters == null)
        {
            commandInfo = null;
            return CommandHandlerResponse.InvalidCommand;
        }
        var isSUDOauthorized = false;

        if (command.ToLower() == "sudo")
        {
            var sudoCommand = "";
            if (parameters.Length > 0)
            {
                sudoCommand = parameters[0];
            }

            if (!commandHandlers.TryGetValue(sudoCommand, out commandInfo))
            {
                return CommandHandlerResponse.InvalidCommand;
            }

            if (session == null)
            {
                Console.WriteLine(
                    "SUDO does not work on the console because you already have full access. Remove SUDO from command and execute again."
                );
                return CommandHandlerResponse.InvalidCommand;
            }

            if (commandInfo.Attribute.Access <= session.AccessLevel)
            {
                isSUDOauthorized = true;
            }

            if (isSUDOauthorized)
            {
                command = sudoCommand;
                var sudoParameters = new string[parameters.Length - 1];
                for (var i = 1; i < parameters.Length; i++)
                {
                    sudoParameters[i - 1] = parameters[i];
                }

                parameters = sudoParameters;
            }
        }

        if (!commandHandlers.TryGetValue(command, out commandInfo))
        {
            // Provide some feedback for why the console command failed
            if (session == null)
            {
                Console.WriteLine($"Invalid Command");
            }

            return CommandHandlerResponse.InvalidCommand;
        }

        if ((commandInfo.Attribute.Flags & CommandHandlerFlag.ConsoleInvoke) != 0 && session != null)
        {
            return CommandHandlerResponse.NoConsoleInvoke;
        }

        if (session != null)
        {
            var isAdvocate = session.Player.IsAdvocate;
            var isSentinel = session.Player.IsSentinel;
            var isEnvoy = session.Player.IsEnvoy;
            var isArch = session.Player.IsArch;
            var isAdmin = session.Player.IsAdmin;

            if (
                commandInfo.Attribute.Access == AccessLevel.Advocate
                    && !(isAdvocate || isSentinel || isEnvoy || isArch || isAdmin || isSUDOauthorized)
                || commandInfo.Attribute.Access == AccessLevel.Sentinel
                    && !(isSentinel || isEnvoy || isArch || isAdmin || isSUDOauthorized)
                || commandInfo.Attribute.Access == AccessLevel.Envoy
                    && !(isEnvoy || isArch || isAdmin || isSUDOauthorized)
                || commandInfo.Attribute.Access == AccessLevel.Developer && !(isArch || isAdmin || isSUDOauthorized)
                || commandInfo.Attribute.Access == AccessLevel.Admin && !(isAdmin || isSUDOauthorized)
            )
            {
                return CommandHandlerResponse.NotAuthorized;
            }
        }

        if (commandInfo.Attribute.ParameterCount != -1 && parameters.Length < commandInfo.Attribute.ParameterCount)
        {
            // Provide some feedback for why the console command failed
            if (session == null)
            {
                Console.WriteLine(
                    $"The syntax of the command is incorrect.\nUsage: "
                        + commandInfo.Attribute.Command
                        + " "
                        + commandInfo.Attribute.Usage
                );
            }

            return CommandHandlerResponse.InvalidParameterCount;
        }

        if (
            (commandInfo.Attribute.Flags & CommandHandlerFlag.RequiresWorld) != 0
            && (session == null || session.Player == null || session.Player.CurrentLandblock == null)
        )
        {
            return CommandHandlerResponse.NotInWorld;
        }

        if (isSUDOauthorized)
        {
            return CommandHandlerResponse.SudoOk;
        }

        return CommandHandlerResponse.Ok;
    }
}
