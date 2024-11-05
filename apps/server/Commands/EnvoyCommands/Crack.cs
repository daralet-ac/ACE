using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using ACE.Server.WorldObjects;

namespace ACE.Server.Commands.EnvoyCommands;

public class Crack
{
    [CommandHandler(
        "crack",
        AccessLevel.Envoy,
        CommandHandlerFlag.RequiresWorld,
        0,
        "Cracks the most recently appraised locked target.",
        "[. open it too]"
    )]
    public static void HandleCrack(Session session, params string[] parameters)
    {
        var openIt = (parameters?.Length > 0 && parameters[0] == ".");
        var notOK = false;
        if (session.Player.CurrentAppraisalTarget.HasValue)
        {
            var objectId = new ObjectGuid((uint)session.Player.CurrentAppraisalTarget);
            var wo = session.Player.CurrentLandblock?.GetObject(objectId);
            if (wo is Lock @lock)
            {
                var opening = openIt ? $" Opening {wo.WeenieType}." : "";
                var lockCode = LockHelper.GetLockCode(wo);
                var resistLockpick = LockHelper.GetResistLockpick(wo);
                var res = UnlockResults.IncorrectKey;

                if (!string.IsNullOrWhiteSpace(lockCode))
                {
                    res = @lock.Unlock(session.Player.Guid.Full, null, lockCode);
                    ChatPacket.SendServerMessage(
                        session,
                        $"Crack {wo.WeenieType} via {lockCode} result: {res}.{opening}",
                        ChatMessageType.Broadcast
                    );
                }
                else if (resistLockpick.HasValue && resistLockpick > 0)
                {
                    var difficulty = 0;
                    res = @lock.Unlock(session.Player.Guid.Full, (uint)(resistLockpick * 2), ref difficulty);
                    ChatPacket.SendServerMessage(
                        session,
                        $"Crack {wo.WeenieType} with skill {resistLockpick}*2 result: {res}.{opening}",
                        ChatMessageType.Broadcast
                    );
                }
                else
                {
                    ChatPacket.SendServerMessage(
                        session,
                        $"The {wo.WeenieType} has no key code or lockpick difficulty.  Unable to crack it.{opening}",
                        ChatMessageType.Broadcast
                    );
                }

                if (openIt)
                {
                    if (wo is Door woDoor)
                    {
                        woDoor.Open(session.Player.Guid);
                    }
                    else if (wo is Chest woChest)
                    {
                        ChatPacket.SendServerMessage(
                            session,
                            $"The {wo.WeenieType} cannot be opened because it is not implemented yet!",
                            ChatMessageType.Broadcast
                        );
                    }
                }
            }
            else
            {
                notOK = true;
            }
        }
        else
        {
            notOK = true;
        }

        if (notOK)
        {
            ChatPacket.SendServerMessage(
                session,
                "Appraise a locked target before using @crack",
                ChatMessageType.Broadcast
            );
        }
    }
}
