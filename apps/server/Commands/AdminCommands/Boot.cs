using System;
using System.Globalization;
using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Managers;
using ACE.Server.Network;
using ACE.Server.Network.Enum;
using ACE.Server.Network.GameMessages.Messages;
using ACE.Server.Network.Managers;

namespace ACE.Server.Commands.AdminCommands;

public class Boot
{
    // boot { account | char | iid } who
    [CommandHandler(
        "boot",
        AccessLevel.Admin,
        CommandHandlerFlag.None,
        2,
        "Boots the character out of the game.",
        "[account | char | iid] who (, reason) \n"
            + "This command boots the specified character out of the game. You can specify who to boot by account, character name, or player instance id. 'who' is the account / character / instance id to actually boot. You can optionally include a reason for the boot.\n"
            + "Example: @boot char Character Name\n"
            + "         @boot account AccountName\n"
            + "         @boot iid 0x51234567\n"
            + "         @boot char Character Name, Reason for being booted\n"
    )]
    public static void HandleBoot(Session session, params string[] parameters)
    {
        // usage: @boot { account,char, iid} who
        // This command boots the specified character out of the game.You can specify who to boot by account, character name, or player instance id.  'who' is the account / character / instance id to actually boot.
        // @boot - Boots the character out of the game.

        var whomToBoot = parameters[1];
        string specifiedReason = null;

        if (parameters.Length > 1)
        {
            var parametersAfterBootType = "";
            for (var i = 1; i < parameters.Length; i++)
            {
                parametersAfterBootType += parameters[i] + " ";
            }
            parametersAfterBootType = parametersAfterBootType.Trim();
            var completeBootNamePlusCommaSeperatedReason = parametersAfterBootType.Split(",");
            whomToBoot = completeBootNamePlusCommaSeperatedReason[0].Trim();
            if (completeBootNamePlusCommaSeperatedReason.Length > 1)
            {
                specifiedReason = parametersAfterBootType.Replace($"{whomToBoot},", "").Trim();
            }
        }

        string whatToBoot = null;
        Session sessionToBoot = null;
        switch (parameters[0].ToLower())
        {
            case "char":
                whatToBoot = "character";
                sessionToBoot = PlayerManager.GetOnlinePlayer(whomToBoot)?.Session;
                break;
            case "account":
                whatToBoot = "account";
                sessionToBoot = NetworkManager.Find(whomToBoot);
                break;
            case "iid":
                whatToBoot = "instance id";
                if (!whomToBoot.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                {
                    CommandHandlerHelper.WriteOutputInfo(
                        session,
                        $"That is not a valid Instance ID (IID). IIDs must be between 0x{ObjectGuid.PlayerMin:X8} and 0x{ObjectGuid.PlayerMax:X8}",
                        ChatMessageType.Broadcast
                    );
                    return;
                }
                if (
                    uint.TryParse(
                        whomToBoot.Substring(2),
                        NumberStyles.HexNumber,
                        CultureInfo.CurrentCulture,
                        out var iid
                    )
                )
                {
                    sessionToBoot = PlayerManager.GetOnlinePlayer(iid)?.Session;
                }
                else
                {
                    CommandHandlerHelper.WriteOutputInfo(
                        session,
                        $"That is not a valid Instance ID (IID). IIDs must be between 0x{ObjectGuid.PlayerMin:X8} and 0x{ObjectGuid.PlayerMax:X8}",
                        ChatMessageType.Broadcast
                    );
                    return;
                }
                break;
            default:
                CommandHandlerHelper.WriteOutputInfo(
                    session,
                    "You must specify what you are booting with char, account, or iid as the first parameter.",
                    ChatMessageType.Broadcast
                );
                return;
        }

        if (sessionToBoot == null)
        {
            CommandHandlerHelper.WriteOutputInfo(
                session,
                $"Cannot boot \"{whomToBoot}\" because that {whatToBoot} is not currently online or cannot be found. Check syntax/spelling and try again.",
                ChatMessageType.Broadcast
            );
            return;
        }

        // Boot the player
        var bootText =
            $"Booting {whatToBoot} {whomToBoot}.{(specifiedReason != null ? $" Reason: {specifiedReason}" : "")}";
        CommandHandlerHelper.WriteOutputInfo(session, bootText, ChatMessageType.Broadcast);
        sessionToBoot.Terminate(
            SessionTerminationReason.AccountBooted,
            new GameMessageBootAccount($"{(specifiedReason != null ? $" - {specifiedReason}" : null)}"),
            null,
            specifiedReason
        );
        //CommandHandlerHelper.WriteOutputInfo(session, $"...Result: Success!", ChatMessageType.Broadcast);

        PlayerManager.BroadcastToAuditChannel(session?.Player, bootText);
    }
}
