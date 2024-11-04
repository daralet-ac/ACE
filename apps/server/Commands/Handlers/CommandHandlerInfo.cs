using System;

namespace ACE.Server.Commands.Handlers;

public class CommandHandlerInfo
{
    public Delegate Handler { get; set; }
    public CommandHandlerAttribute Attribute { get; set; }
}
