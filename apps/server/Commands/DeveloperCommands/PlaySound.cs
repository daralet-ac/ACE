using System;
using System.Globalization;
using ACE.Common.Extensions;
using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using ACE.Server.Network.GameMessages.Messages;

namespace ACE.Server.Commands.DeveloperCommands;

public class PlaySound
{
    /// <summary>
    /// playsound [Sound] (volumelevel)
    /// </summary>
    [CommandHandler(
        "playsound",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        1,
        "Plays a sound.",
        "sound (volume) (guid)\n" + "Sound can be uint or enum name\n" + "Volume and source guid are optional"
    )]
    public static void HandlePlaySound(Session session, params string[] parameters)
    {
        try
        {
            var volume = 1f;

            if (parameters.Length > 1)
            {
                float.TryParse(parameters[1], out volume);
            }

            var guid = session.Player.Guid.Full;

            if (parameters.Length > 2)
            {
                uint.TryParse(
                    parameters[2].TrimStart("0x"),
                    NumberStyles.HexNumber,
                    CultureInfo.InvariantCulture,
                    out guid
                );
            }

            var message = $"Unable to find a sound called {parameters[0]} to play.";

            if (Enum.TryParse(parameters[0], true, out Sound sound))
            {
                if (Enum.IsDefined(typeof(Sound), sound))
                {
                    message = $"Playing sound {Enum.GetName(typeof(Sound), sound)}";
                    // add the sound to the player queue for everyone to hear
                    // player action queue items will execute on the landblock
                    // player.playsound will play a sound on only the client session that called the function
                    session.Player.PlaySoundEffect(sound, new ObjectGuid(guid), volume);
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
