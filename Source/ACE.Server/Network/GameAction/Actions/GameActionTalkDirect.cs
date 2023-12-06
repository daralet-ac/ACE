using ACE.Common.Extensions;
using ACE.Entity.Enum;
using ACE.Server.Network.GameEvent.Events;
using ACE.Server.Network.GameMessages.Messages;
using ACE.Server.WorldObjects;
using Serilog;

namespace ACE.Server.Network.GameAction.Actions
{
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

            session.Network.EnqueueSend(new GameMessageSystemChat($"You tell {creature.Name}, \"{message}\"", ChatMessageType.OutgoingTell));

            if (creature is Player targetPlayer)
            {
                if (session.Player.IsGagged)
                {
                    session.Player.SendGagError();
                    return;
                }

                if (targetPlayer.SquelchManager.Squelches.Contains(session.Player, ChatMessageType.Tell))
                {
                    session.Network.EnqueueSend(new GameEventWeenieErrorWithString(session, WeenieErrorWithString.MessageBlocked_, $"{targetPlayer.Name} has you squelched."));
                    // _log.Warning($"Tell from {session.Player.Name} (0x{session.Player.Guid.ToString()}) to {targetPlayer.Name} (0x{targetPlayer.Guid.ToString()}) blocked due to squelch");
                    return;
                }

                var tell = new GameEventTell(targetPlayer.Session, message, session.Player.GetNameWithSuffix(), session.Player.Guid.Full, targetPlayer.Guid.Full, ChatMessageType.Tell);
                targetPlayer.Session.Network.EnqueueSend(tell);
            }
            else
                creature.EmoteManager.OnTalkDirect(session.Player, message);
        }
    }
}
