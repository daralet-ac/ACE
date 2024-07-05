namespace ACE.Server.Network.GameAction.Actions;

public static class GameActionRemoveSpellFavorite
{
    [GameAction(GameActionType.RemoveSpellFavorite)]
    public static void Handle(ClientMessage message, Session session)
    {
        var spellId = message.Payload.ReadUInt32();
        var spellBarId = message.Payload.ReadUInt32();

        session.Player.HandleActionRemoveSpellFavorite(spellId, spellBarId);
    }
}
