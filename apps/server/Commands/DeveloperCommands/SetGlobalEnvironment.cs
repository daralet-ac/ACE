using System;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Managers;
using ACE.Server.Network;
using ACE.Server.Network.GameMessages.Messages;

namespace ACE.Server.Commands.DeveloperCommands;

public class SetGlobalEnvironment
{
    [CommandHandler(
        "setglobalenviron",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        0,
        "Sets or clears server's global environment option",
        "(name or id of EnvironChangeType)\nleave blank to reset to default.\nlist to get a complete list of options."
    )]
    public static void HandleSetGlobalEnviron(Session session, params string[] parameters)
    {
        var environChange = EnvironChangeType.Clear;

        if (parameters.Length > 0)
        {
            if (parameters[0] == "list")
            {
                session.Network.EnqueueSend(new GameMessageSystemChat(EnvironListMsg(), ChatMessageType.Broadcast));
                return;
            }

            if (Enum.TryParse(parameters[0], true, out environChange))
            {
                if (!Enum.IsDefined(typeof(EnvironChangeType), environChange))
                {
                    environChange = EnvironChangeType.Clear;
                }
            }
        }

        if (environChange.IsFog())
        {
            session.Network.EnqueueSend(
                new GameMessageSystemChat(
                    $"Setting all landblocks to EnvironChangeType.{environChange.ToString()} .",
                    ChatMessageType.Broadcast
                )
            );
            PlayerManager.BroadcastToAuditChannel(
                session.Player,
                $"{session.Player.Name} set all landblocks to EnvironChangeType.{environChange.ToString()} ."
            );
        }
        else
        {
            session.Network.EnqueueSend(
                new GameMessageSystemChat(
                    $"Sending EnvironChangeType.{environChange.ToString()} to all players on all Landblocks.",
                    ChatMessageType.Broadcast
                )
            );
            PlayerManager.BroadcastToAuditChannel(
                session.Player,
                $"{session.Player.Name} sent EnvironChangeType.{environChange.ToString()} to all players on all Landblocks."
            );
        }

        LandblockManager.DoEnvironChange(environChange);
    }

    private static string EnvironListMsg()
    {
        var msg = "Complete list of EnvironChangeType:\n";
        foreach (var name in Enum.GetNames(typeof(EnvironChangeType)))
        {
            msg += name + "\n";
        }

        msg += "Notes about above list:\n";
        msg +=
            "Clear resets to default.\nAll options ending with Fog are continuous.\nAll options ending with Fog2 are continuous and blank radar.\nAll options ending with Sound play once and do not repeat.";

        return msg;
    }
}
