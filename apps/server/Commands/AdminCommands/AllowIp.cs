using System;
using System.Linq;
using System.Text.RegularExpressions;
using ACE.Common;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using Serilog;

namespace ACE.Server.Commands.AdminCommands;

public class AllowIp
{
    private static readonly ILogger _log = Log.ForContext(typeof(AllowIp));

    // allow_ip {ipv4}
    [CommandHandler(
        "allow_ip",
        AccessLevel.Admin,
        CommandHandlerFlag.None,
        1,
        "Temporarily add an IP to the allow list",
        "< ipv4 address >"
    )]
    public static void HandleAllowIp(Session session, params string[] parameters)
    {
        var ip = parameters[0];

        if (string.IsNullOrWhiteSpace(ip))
        {
            ChatPacket.SendServerMessage(
                session,
                $"Cannot add IP: `{ip}`. Please enter a valid IPv4 address.",
                ChatMessageType.Broadcast
            );
            return;
        }

        var regex = new Regex(@"^((25[0-5]|(2[0-4]|1\d|[1-9]|)\d)\.?\b){4}$");
        if (!regex.IsMatch(ip))
        {
            ChatPacket.SendServerMessage(
                session,
                $"Cannot add IP: `{ip}`. Please enter a valid IPv4 address.",
                ChatMessageType.Broadcast
            );
            return;
        }

        var allowList = ConfigManager.Config.Server.Network.AllowUnlimitedSessionsFromIPAddresses;
        if (allowList.Contains(ip))
        {
            ChatPacket.SendServerMessage(session, $"IP `{ip}` already added to allow list.", ChatMessageType.Broadcast);
            return;
        }

        Array.Resize(ref allowList, allowList.Length + 1);
        allowList[^1] = ip;
        ConfigManager.Config.Server.Network.AllowUnlimitedSessionsFromIPAddresses = allowList;

        ChatPacket.SendServerMessage(
            session,
            $"Successfully added `{ip}` to the server allow list. **Reminder:** This is temporary and should be added to the config",
            ChatMessageType.Broadcast
        );
    }
}
