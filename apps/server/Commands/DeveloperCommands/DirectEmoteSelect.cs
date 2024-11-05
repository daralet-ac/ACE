using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using ACE.Server.Network.GameMessages.Messages;
using ACE.Server.WorldObjects;

namespace ACE.Server.Commands.DeveloperCommands;

public class DirectEmoteSelect
{
    // de_s text
    [CommandHandler(
        "de_s",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        1,
        "Sends text to selected player, formatted exactly as entered, with no prefix of any kind.",
        "<text>"
    )]
    public static void Handlede_s(Session session, params string[] parameters)
    {
        // usage: @de_s text
        // usage: @direct_emote_select text
        // Sends text to selected player, formatted exactly as entered, with no prefix of any kind.
        // @direct_emote_select - Sends text to selected player, formatted exactly as entered.

        Handledirect_emote_select(session, parameters);
    }

    // direct_emote_select text
    [CommandHandler(
        "direct_emote_select",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        1,
        "Sends text to selected player, formatted exactly as entered, with no prefix of any kind.",
        "<text>"
    )]
    public static void Handledirect_emote_select(Session session, params string[] parameters)
    {
        // usage: @de_s text
        // usage: @direct_emote_select text
        // Sends text to selected player, formatted exactly as entered, with no prefix of any kind.
        // @direct_emote_select - Sends text to selected player, formatted exactly as entered.

        var objectId = ObjectGuid.Invalid;

        if (session.Player.HealthQueryTarget.HasValue)
        {
            objectId = new ObjectGuid((uint)session.Player.HealthQueryTarget);
        }
        else if (session.Player.ManaQueryTarget.HasValue)
        {
            objectId = new ObjectGuid((uint)session.Player.ManaQueryTarget);
        }
        else if (session.Player.CurrentAppraisalTarget.HasValue)
        {
            objectId = new ObjectGuid((uint)session.Player.CurrentAppraisalTarget);
        }

        if (objectId == ObjectGuid.Invalid)
        {
            session.Network.EnqueueSend(
                new GameMessageSystemChat(
                    $"You must select a player to send them a message.",
                    ChatMessageType.Broadcast
                )
            );
            return;
        }

        var wo = session.Player.CurrentLandblock?.GetObject(objectId);

        if (wo is null)
        {
            session.Network.EnqueueSend(
                new GameMessageSystemChat($"Unable to locate what you have selected.", ChatMessageType.Broadcast)
            );
        }
        else if (wo is Player player)
        {
            var msg = string.Join(" ", parameters);

            player.SendMessage(msg);
        }
        else
        {
            session.Network.EnqueueSend(
                new GameMessageSystemChat(
                    $"You cannot send text to {wo.Name} because it is not a player.",
                    ChatMessageType.Broadcast
                )
            );
        }
    }
}
