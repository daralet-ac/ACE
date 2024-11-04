using System.Linq;
using ACE.Database.Models.World;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;

namespace ACE.Server.Commands.DeveloperCommands;

public class ShowTierMobs
{
    /// <summary>
    /// Shows a list of monsters for a particular tier #
    /// </summary>
    [CommandHandler(
        "tiermobs",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        1,
        "Shows a list of monsters for a particular tier #",
        "tier"
    )]
    public static void HandleTierMobs(Session session, params string[] parameters)
    {
        if (!uint.TryParse(parameters[0], out var tier))
        {
            CommandHandlerHelper.WriteOutputInfo(session, $"Invalid tier {parameters[0]}");
            return;
        }
        if (tier < 1 || tier > 8)
        {
            CommandHandlerHelper.WriteOutputInfo(session, "Please enter a tier between 1-8");
            return;
        }
        using (var ctx = new WorldDbContext())
        {
            var query =
                from weenie in ctx.Weenie
                join deathTreasure in ctx.WeeniePropertiesDID on weenie.ClassId equals deathTreasure.ObjectId
                join treasureDeath in ctx.TreasureDeath on deathTreasure.Value equals treasureDeath.TreasureType
                where
                    weenie.Type == (int)WeenieType.Creature
                    && deathTreasure.Type == (ushort)PropertyDataId.DeathTreasureType
                    && treasureDeath.Tier == tier
                select weenie;

            var results = query.ToList();

            CommandHandlerHelper.WriteOutputInfo(session, $"Found {results.Count()} monsters for tier {tier}");

            foreach (var result in results)
            {
                CommandHandlerHelper.WriteOutputInfo(session, result.ClassName);
            }
        }
    }
}
