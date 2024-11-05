using ACE.Database.Models.World;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Entity;
using ACE.Server.Factories;
using ACE.Server.Network;
using ACE.Server.Network.GameMessages.Messages;
using ACE.Server.WorldObjects;

namespace ACE.Server.Commands.DeveloperCommands;

public class LootGen
{
    [CommandHandler(
        "lootgen",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        1,
        "Generate a piece of loot from the LootGenerationFactory.",
        "<wcid or classname> <tier>"
    )]
    public static void HandleLootGen(Session session, params string[] parameters)
    {
        WorldObject wo = null;

        // create base item
        if (uint.TryParse(parameters[0], out var wcid))
        {
            wo = WorldObjectFactory.CreateNewWorldObject(wcid);
        }
        else
        {
            wo = WorldObjectFactory.CreateNewWorldObject(parameters[0]);
        }

        if (wo == null)
        {
            session.Network.EnqueueSend(
                new GameMessageSystemChat($"Couldn't find {parameters[0]}", ChatMessageType.Broadcast)
            );
            return;
        }

        var tier = 1;
        if (parameters.Length > 1)
        {
            int.TryParse(parameters[1], out tier);
        }

        if (tier < 1 || tier > 8)
        {
            session.Network.EnqueueSend(
                new GameMessageSystemChat($"Loot Tier must be a number between 0 and 8", ChatMessageType.Broadcast)
            );
            return;
        }

        if (
            wo.TsysMutationData == null
            && !Aetheria.IsAetheria(wo.WeenieClassId)
            && !SigilTrinket.IsSigilTrinket(wo.WeenieClassId)
            && !(wo is PetDevice)
            && wo.TrophyQuality == null
        )
        {
            session.Network.EnqueueSend(
                new GameMessageSystemChat(
                    $"{wo.Name} ({wo.WeenieClassId}) missing PropertyInt.TsysMutationData",
                    ChatMessageType.Broadcast
                )
            );
            return;
        }

        var profile = new TreasureDeath() { Tier = tier, LootQualityMod = 0 };

        wo.Tier = tier;

        var success = LootGenerationFactory.MutateItem(wo, profile, true);

        session.Player.TryCreateInInventoryWithNetworking(wo);
    }
}
