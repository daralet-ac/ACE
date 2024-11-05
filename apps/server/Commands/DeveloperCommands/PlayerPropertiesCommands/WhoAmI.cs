using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;

namespace ACE.Server.Commands.DeveloperCommands.PlayerPropertiesCommands;

public class WhoAmI
{
    /// <summary>
    /// Returns the Player's GUID
    /// </summary>
    /// <remarks>Added a quick way to access the player GUID.</remarks>
    [CommandHandler("whoami", AccessLevel.Developer, CommandHandlerFlag.RequiresWorld, "Shows you your GUIDs.")]
    public static void HandleWhoAmI(Session session, params string[] parameters)
    {
        ChatPacket.SendServerMessage(
            session,
            $"GUID: {session.Player.Guid.Full} (0x{session.Player.Guid}) | ID(low): {session.Player.Guid.Low} High:{session.Player.Guid.High}",
            ChatMessageType.Broadcast
        );
    }
}
