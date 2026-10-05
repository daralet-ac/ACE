using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;

namespace ACE.Server.Commands.AdminCommands;

public class VpnBlockList
{
    [CommandHandler(
        "clearvpnblocklist",
        AccessLevel.Admin,
        CommandHandlerFlag.None,
        "Clears the list of IPs that are blocked due to the VPN / proxy check, so they are re-checked on their next login"
    )]
    public static void HandleClearVpnBlockList(Session session, params string[] parameters)
    {
        var count = VpnDetection.ClearBlockedIPs();

        CommandHandlerHelper.WriteOutputInfo(session, $"Removed {count} IP(s) from the VPN block list.");
    }

    [CommandHandler(
        "removeipfromvpnblocklist",
        AccessLevel.Admin,
        CommandHandlerFlag.None,
        1,
        "Removes a single IP from the list of IPs that are blocked due to the VPN / proxy check",
        "removeipfromvpnblocklist (ip)"
    )]
    public static void HandleRemoveIpFromVpnBlockList(Session session, params string[] parameters)
    {
        var ip = parameters[0].Trim();

        if (VpnDetection.RemoveBlockedIP(ip))
        {
            CommandHandlerHelper.WriteOutputInfo(session, $"Removed {ip} from the VPN block list.");
        }
        else
        {
            CommandHandlerHelper.WriteOutputInfo(session, $"{ip} is not on the VPN block list.");
        }
    }
}
