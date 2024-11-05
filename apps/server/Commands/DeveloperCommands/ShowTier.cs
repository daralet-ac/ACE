using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using ACE.Server.WorldObjects;

namespace ACE.Server.Commands.DeveloperCommands;

public class ShowTier
{
    /// <summary>
    /// Shows the DeathTreasure tier for the last appraised monster
    /// </summary>
    [CommandHandler(
        "showtier",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        "Shows the DeathTreasure tier for the last appraised monster"
    )]
    public static void HandleShowTier(Session session, params string[] parameters)
    {
        var creature = CommandHandlerHelper.GetLastAppraisedObject(session) as Creature;

        if (creature != null)
        {
            var msg =
                creature.DeathTreasure != null
                    ? $"DeathTreasure - Tier: {creature.DeathTreasure.Tier}"
                    : "doesn't have PropertyDataId.DeathTreasureType";

            CommandHandlerHelper.WriteOutputInfo(session, $"{creature.Name} ({creature.Guid}) {msg}");
        }
    }
}
