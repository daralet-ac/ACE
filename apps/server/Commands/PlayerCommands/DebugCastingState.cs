using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using ACE.Server.Network.GameMessages.Messages;

namespace ACE.Server.Commands.PlayerCommands;

public class DebugCastingState
{
    [CommandHandler(
        "debugcast",
        AccessLevel.Player,
        CommandHandlerFlag.RequiresWorld,
        "Shows debug information about the current magic casting state"
    )]
    public static void HandleDebugCast(Session session, params string[] parameters)
    {
        var physicsObj = session.Player.PhysicsObj;

        var pendingActions = physicsObj.MovementManager.MoveToManager.PendingActions;
        var currAnim = physicsObj.PartArray.Sequence.CurrAnim;

        session.Network.EnqueueSend(
            new GameMessageSystemChat(session.Player.MagicState.ToString(), ChatMessageType.Broadcast)
        );
        session.Network.EnqueueSend(
            new GameMessageSystemChat(
                $"IsMovingOrAnimating: {physicsObj.IsMovingOrAnimating}",
                ChatMessageType.Broadcast
            )
        );
        session.Network.EnqueueSend(
            new GameMessageSystemChat($"PendingActions: {pendingActions.Count}", ChatMessageType.Broadcast)
        );
        session.Network.EnqueueSend(
            new GameMessageSystemChat($"CurrAnim: {currAnim?.Value.Anim.ID:X8}", ChatMessageType.Broadcast)
        );
    }
}
