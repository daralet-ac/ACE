using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ACE.Common;
using Discord.Interactions;

namespace ACE.Server.Discord.Modules;

public class AccountModule : InteractionModuleBase<SocketInteractionContext>
{
    [RequireRole("Admin")]
    [SlashCommand("allow_ip", "Temporarily adds a user's IP to the multiple account allow list.")]
    public async Task AllowIp(string ip)
    {
        if (string.IsNullOrWhiteSpace(ip))
        {
            await RespondAsync($"Cannot add IP: `{ip}`. Please enter a valid IPv4 address.", ephemeral: true);
            return;
        }

        var regex = new Regex(@"^((25[0-5]|(2[0-4]|1\d|[1-9]|)\d)\.?\b){4}$");
        if (!regex.IsMatch(ip))
        {
            await RespondAsync($"Cannot add IP: `{ip}`. Please enter a valid IPv4 address.", ephemeral: true);
            return;
        }

        var allowList = ConfigManager.Config.Server.Network.AllowUnlimitedSessionsFromIPAddresses;
        if (allowList.Contains(ip))
        {
            await RespondAsync($"IP `{ip}` already added to allow list.", ephemeral: true);
            return;
        }

        Array.Resize(ref allowList, allowList.Length + 1);
        allowList[^1] = ip;
        ConfigManager.Config.Server.Network.AllowUnlimitedSessionsFromIPAddresses = allowList;

        await RespondAsync($"Successfully added `{ip}` to the server allow list. **Reminder:** This is temporary and should be added to the config", ephemeral: true);
    }
}
