using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;

namespace ACE.Server.Commands.DeveloperCommands;

public class SpendAllXp
{
    [CommandHandler(
        "spendallxp",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        0,
        "Spend all available XP on Attributes, Vitals and Skills."
    )]
    public static void HandleSpendAllXp(Session session, params string[] parameters)
    {
        session.Player.SpendAllXp();

        ChatPacket.SendServerMessage(
            session,
            "All available xp has been spent. You must now log out for the updated values to take effect.",
            ChatMessageType.Broadcast
        );
    }
}
