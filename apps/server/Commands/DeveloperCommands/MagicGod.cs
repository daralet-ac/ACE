using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;

namespace ACE.Server.Commands.DeveloperCommands;

public class MagicGod
{
    // magic god
    [CommandHandler("magic god", AccessLevel.Developer, CommandHandlerFlag.RequiresWorld, 0)]
    public static void HandleMagicGod(Session session, params string[] parameters)
    {
        // @magic god - Sets your magic stats to the specfied level.

        // TODO: output

        // output: You are now a magic god!!!

        ChatPacket.SendServerMessage(session, "You are now a magic god!!!", ChatMessageType.Broadcast);
    }
}
