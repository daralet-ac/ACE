using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Managers;
using ACE.Server.Network;
using ACE.Server.WorldObjects;

namespace ACE.Server.Commands.EnvoyCommands;

public class Smite
{
    [CommandHandler(
        "smite",
        AccessLevel.Envoy,
        CommandHandlerFlag.RequiresWorld,
        0,
        "Kills the selected target or all monsters in radar range if \"all\" is specified.",
        "[all, Player's Name]"
    )]
    public static void HandleSmite(Session session, params string[] parameters)
    {
        // @smite [all] - Kills the selected target or all monsters in radar range if "all" is specified.

        if (parameters?.Length > 0)
        {
            if (parameters[0] == "all")
            {
                foreach (var obj in session.Player.PhysicsObj.ObjMaint.GetVisibleObjectsValues())
                {
                    var wo = obj.WeenieObj.WorldObject;

                    if (wo is Player) // I don't recall if @smite all would kill players in range, assuming it didn't
                    {
                        continue;
                    }

                    var useTakeDamage = PropertyManager.GetBool("smite_uses_takedamage").Item;

                    if (wo is Creature creature && creature.Attackable)
                    {
                        creature.Smite(session.Player, useTakeDamage);
                    }
                }

                PlayerManager.BroadcastToAuditChannel(session.Player, $"{session.Player.Name} used smite all.");
            }
            else
            {
                var characterName = "";

                // if parameters are greater then 1, we may have a space in a character name
                if (parameters.Length > 1)
                {
                    foreach (var name in parameters)
                    {
                        // adds a space back inbetween each parameter
                        if (characterName.Length > 0)
                        {
                            characterName += " " + name;
                        }
                        else
                        {
                            characterName = name;
                        }
                    }
                }
                // if there are no spaces, just set the characterName to the first paramter
                else
                {
                    characterName = parameters[0];
                }

                // look up session
                var player = PlayerManager.GetOnlinePlayer(characterName);

                // playerSession will be null when the character is not found
                if (player != null)
                {
                    player.Smite(session.Player, PropertyManager.GetBool("smite_uses_takedamage").Item);

                    PlayerManager.BroadcastToAuditChannel(
                        session.Player,
                        $"{session.Player.Name} used smite on {player.Name}"
                    );
                    return;
                }

                ChatPacket.SendServerMessage(
                    session,
                    "Select a target and use @smite, or use @smite all to kill all creatures in radar range or @smite [player's name].",
                    ChatMessageType.Broadcast
                );
            }
        }
        else
        {
            if (session.Player.HealthQueryTarget.HasValue) // Only Creatures will trigger this.. Excludes vendors automatically as a result (Can change design to mimic @delete command)
            {
                var objectId = new ObjectGuid((uint)session.Player.HealthQueryTarget);

                var wo = session.Player.CurrentLandblock?.GetObject(objectId) as Creature;

                if (objectId == session.Player.Guid) // don't kill yourself
                {
                    return;
                }

                if (wo != null)
                {
                    wo.Smite(session.Player, PropertyManager.GetBool("smite_uses_takedamage").Item);

                    PlayerManager.BroadcastToAuditChannel(
                        session.Player,
                        $"{session.Player.Name} used smite on {wo.Name} (0x{wo.Guid:X8})"
                    );
                }
            }
            else
            {
                ChatPacket.SendServerMessage(
                    session,
                    "Select a target and use @smite, or use @smite all to kill all creatures in radar range or @smite [players' name].",
                    ChatMessageType.Broadcast
                );
            }
        }
    }
}
