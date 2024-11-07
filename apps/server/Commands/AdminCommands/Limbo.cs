using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;

namespace ACE.Server.Commands.AdminCommands;

public class Limbo
{
    // limbo [on / off]
    [CommandHandler("limbo", AccessLevel.Admin, CommandHandlerFlag.RequiresWorld, 0)]
    public static void HandleLimbo(Session session, params string[] parameters)
    {
        // @limbo[on / off] - Puts the targeted player in 'limbo' which means that the player cannot damage anything or be damaged by anything.The player will not recieve direct tells, or channel messages, such as fellowship messages and allegiance chat.  The player will be unable to salvage.This status times out after 15 minutes, use '@limbo on' again on the player to reset the timer. You and the player will be notifed when limbo wears off.If neither on or off are specified, on is assumed.
        // @limbo - Puts the selected target in limbo.

        // TODO: output
    }
}
