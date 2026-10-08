using ACE.Common.Extensions;
using ACE.Entity.Enum;
using ACE.Server.Arena;
using ACE.Server.Managers;
using ACE.Server.Network.GameEvent.Events;
using ACE.Server.Network.GameMessages.Messages;
using ACE.Server.WorldObjects;
using Serilog;

namespace ACE.Server.Network.GameAction.Actions;

public static class GameActionTalkDirect
{
    private static readonly ILogger _log = Log.ForContext(typeof(GameActionTalkDirect));

    [GameAction(GameActionType.TalkDirect)]
    public static void Handle(ClientMessage clientMessage, Session session)
    {
        var message = clientMessage.Payload.ReadString16L();
        var targetGuid = clientMessage.Payload.ReadUInt32();

        var creature = session.Player.CurrentLandblock?.GetObject(targetGuid) as Creature;
        if (creature == null)
        {
            var statusMessage = new GameEventWeenieError(session, WeenieError.CharacterNotAvailable);
            session.Network.EnqueueSend(statusMessage);
            return;
        }

        if (creature is Player arenaTarget)
        {
            var arenaRefusal = ArenaManager.WhyCantTell(session.Player, arenaTarget);
            if (arenaRefusal != null)
            {
                session.Player.SendMessage(arenaRefusal);
                return;
            }
        }

        session.Network.EnqueueSend(
            new GameMessageSystemChat($"You tell {creature.Name}, \"{message}\"", ChatMessageType.OutgoingTell)
        );

        if (creature is Player targetPlayer)
        {
            if (session.Player.IsGagged)
            {
                session.Player.SendGagError();
                return;
            }

            if (targetPlayer.SquelchManager.Squelches.Contains(session.Player, ChatMessageType.Tell))
            {
                session.Network.EnqueueSend(
                    new GameEventWeenieErrorWithString(
                        session,
                        WeenieErrorWithString.MessageBlocked_,
                        $"{targetPlayer.Name} has you squelched."
                    )
                );
                // _log.Warning($"Tell from {session.Player.Name} (0x{session.Player.Guid.ToString()}) to {targetPlayer.Name} (0x{targetPlayer.Guid.ToString()}) blocked due to squelch");
                PlayerManager.LogPlayerChat(
                    "chat_log_tell",
                    "TELL",
                    session.Player.Name,
                    $"tells {targetPlayer.Name} (squelched, not delivered)",
                    message
                );
                return;
            }

            var tell = new GameEventTell(
                targetPlayer.Session,
                message,
                session.Player.GetNameWithSuffix(),
                session.Player.Guid.Full,
                targetPlayer.Guid.Full,
                ChatMessageType.Tell
            );
            targetPlayer.Session.Network.EnqueueSend(tell);

            PlayerManager.LogPlayerChat(
                "chat_log_tell",
                "TELL",
                session.Player.Name,
                $"tells {targetPlayer.Name}",
                message
            );
        }
        else
        {
            PlayerManager.LogPlayerChat(
                "chat_log_tell",
                "TELL",
                session.Player.Name,
                $"tells {creature.Name}",
                message
            );

            creature.EmoteManager.OnTalkDirect(session.Player, message);
        }
    }
}
