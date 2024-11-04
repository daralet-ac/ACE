using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Entity;
using ACE.Server.Factories;
using ACE.Server.Network;
using ACE.Server.WorldObjects;

namespace ACE.Server.Commands.DeveloperCommands;

public class ShowWieldedTreasure
{
    [CommandHandler(
        "show-wielded-treasure",
        AccessLevel.Developer,
        CommandHandlerFlag.None,
        1,
        "Shows the WieldedTreasure table for a Creature",
        "wcid"
    )]
    public static void HandleShowWieldedTreasure(Session session, params string[] parameters)
    {
        if (!uint.TryParse(parameters[0], out var wcid))
        {
            CommandHandlerHelper.WriteOutputInfo(session, $"Invalid wcid {parameters[0]}", ChatMessageType.Broadcast);
            return;
        }
        var creature = WorldObjectFactory.CreateNewWorldObject(wcid) as Creature;

        if (creature == null)
        {
            CommandHandlerHelper.WriteOutputInfo(session, $"Couldn't find weenie {wcid}");
            return;
        }

        if (creature.WieldedTreasure == null)
        {
            CommandHandlerHelper.WriteOutputInfo(
                session,
                $"{creature.Name} ({creature.WeenieClassId}) missing WieldedTreasure"
            );
            return;
        }
        var table = new TreasureWieldedTable(creature.WieldedTreasure);

        foreach (var set in table.Sets)
        {
            OutputWieldedTreasureSet(session, set);
        }
    }

    private static void OutputWieldedTreasureSet(Session session, TreasureWieldedSet set, int depth = 0)
    {
        var prefix = new string(' ', depth * 2);

        var totalProbability = 0.0f;
        var spacer = false;

        foreach (var item in set.Items)
        {
            if (totalProbability >= 1.0f)
            {
                totalProbability = 0.0f;
                //spacer = true;
            }
            totalProbability += item.Item.Probability;

            var wo = WorldObjectFactory.CreateNewWorldObject(item.Item.WeenieClassId);

            var itemName = wo?.Name ?? "Unknown";

            if (spacer)
            {
                CommandHandlerHelper.WriteOutputInfo(session, "");
                spacer = false;
            }
            CommandHandlerHelper.WriteOutputInfo(
                session,
                $"{prefix}- {item.Item.WeenieClassId} - {itemName} ({item.Item.Probability * 100}%)"
            );

            if (item.Subset != null)
            {
                OutputWieldedTreasureSet(session, item.Subset, depth + 1);
                //spacer = true;
            }
        }
    }
}
