namespace ACE.Server.Commands.Handlers;

public enum CommandHandlerResponse
{
    Ok,
    SudoOk,
    InvalidCommand,
    NoConsoleInvoke,
    NotAuthorized,
    InvalidParameterCount,
    NotInWorld
}
