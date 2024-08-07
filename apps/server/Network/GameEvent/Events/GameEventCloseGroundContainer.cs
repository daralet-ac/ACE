using ACE.Server.WorldObjects;

namespace ACE.Server.Network.GameEvent.Events;

public class GameEventCloseGroundContainer : GameEventMessage
{
    public GameEventCloseGroundContainer(Session session, Container container)
        : base(GameEventType.CloseGroundContainer, GameMessageGroup.UIQueue, session, 8)
    {
        Writer.Write(container.Guid.Full);
    }
}
