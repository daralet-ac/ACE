using ACE.Server.Network.Motion;
using ACE.Server.Network.Sequence;
using ACE.Server.WorldObjects;

namespace ACE.Server.Network.GameMessages.Messages;

public class GameMessageUpdateMotion : GameMessage
{
    public GameMessageUpdateMotion(WorldObject wo, MovementData movementData)
        : base(GameMessageOpcode.Motion, GameMessageGroup.SmartboxQueue, 88) // 88 is the max seen in retail pcaps
    {
        Send(wo, movementData);
    }

    public GameMessageUpdateMotion(WorldObject wo, ACE.Server.Entity.Motion motion)
        : base(GameMessageOpcode.Motion, GameMessageGroup.SmartboxQueue, 88) // 88 is the max seen in retail pcaps
    {
        Send(wo, new MovementData(wo, motion));
    }

    public void Send(WorldObject wo, MovementData movementData)
    {
        Writer.WriteGuid(wo.Guid);
        Writer.Write(wo.Sequences.GetCurrentSequence(SequenceType.ObjectInstance));
        Writer.Write(movementData);
    }
}
