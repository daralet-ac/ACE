using System;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;

namespace ACE.Server.Commands.DeveloperCommands;

public class DebugEmote
{
    /// <summary>
    /// Enables emote debugging for the last appraised object
    /// </summary>
    [CommandHandler(
        "debugemote",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        0,
        "Enables emote debugging for the last appraised object"
    )]
    public static void HandleDebugEmote(Session session, params string[] parameters)
    {
        var obj = CommandHandlerHelper.GetLastAppraisedObject(session);
        if (obj != null)
        {
            Console.WriteLine($"Showing emotes for {obj.Name}");
            obj.EmoteManager.Debug = true;
        }
    }
}
