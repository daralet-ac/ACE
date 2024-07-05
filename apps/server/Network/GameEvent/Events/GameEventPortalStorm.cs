namespace ACE.Server.Network.GameEvent.Events;

class GameEventPortalStorm : GameEventMessage
{
    public GameEventPortalStorm(Session session)
        : base(GameEventType.MiscPortalStorm, GameMessageGroup.UIQueue, session, 4) { }
}
