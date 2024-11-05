using System;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;

namespace ACE.Server.Commands.DeveloperCommands;

public class AddAllTitles
{
    /// <summary>
    /// Add all titles to yourself
    /// </summary>
    [CommandHandler(
        "addalltitles",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        "Add all titles to yourself"
    )]
    public static void HandleAddAllTitles(Session session, params string[] parameters)
    {
        foreach (CharacterTitle title in Enum.GetValues(typeof(CharacterTitle)))
        {
            session.Player.AddTitle((uint)title);
        }
    }
}
