using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;

namespace ACE.Server.Commands.DeveloperCommands.ServerCommands;

public class SaveNow
{
    /// <summary>
    /// Debug command to saves the character from in-game.
    /// </summary>
    /// <remarks>Added a quick way to invoke the character save routine.</remarks>
    [CommandHandler("save-now", AccessLevel.Developer, CommandHandlerFlag.RequiresWorld, "Saves your session.")]
    public static void HandleSaveNow(Session session, params string[] parameters)
    {
        session.Player.SavePlayerToDatabase();
    }
}
