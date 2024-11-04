using System;

namespace ACE.Server.Commands.Handlers;

[Flags]
public enum CommandHandlerFlag
{
    None = 0x00,
    ConsoleInvoke = 0x01,
    RequiresWorld = 0x02
}
