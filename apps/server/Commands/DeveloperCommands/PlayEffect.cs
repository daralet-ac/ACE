using System;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using ACE.Server.Network.GameMessages.Messages;

namespace ACE.Server.Commands.DeveloperCommands;

public class PlayEffect
{
    /// <summary>
    /// effect [Effect] (scale)
    /// </summary>
    [CommandHandler(
        "effect",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        1,
        "Plays an effect.",
        "effect (float)\n" + "Effect can be uint or enum name" + "float is scale level"
    )]
    public static void HandlePlayEffect(Session session, params string[] parameters)
    {
        try
        {
            var scale = 1f;
            var effectEvent = new GameMessageScript(session.Player.Guid, ACE.Entity.Enum.PlayScript.Invalid);

            if (parameters.Length > 1)
            {
                if (parameters[1] != "")
                {
                    scale = float.Parse(parameters[1]);
                }
            }

            var message = $"Unable to find a effect called {parameters[0]} to play.";

            if (Enum.TryParse(parameters[0], true, out ACE.Entity.Enum.PlayScript effect))
            {
                if (Enum.IsDefined(typeof(PlayScript), effect))
                {
                    message = $"Playing effect {Enum.GetName(typeof(PlayScript), effect)}";
                    session.Player.ApplyVisualEffects(effect);
                }
            }

            var sysChatMessage = new GameMessageSystemChat(message, ChatMessageType.Broadcast);
            session.Network.EnqueueSend(sysChatMessage);
        }
        catch (Exception)
        {
            // Do Nothing
        }
    }
}
