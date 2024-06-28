using System.Threading.Tasks;
using ACE.Entity.Enum;
using ACE.Server.Managers;
using ACE.Server.Network.GameMessages.Messages;
using Discord.Interactions;

namespace ACE.Server.Discord.Modules;

public class ChatModule : InteractionModuleBase<SocketInteractionContext>
{
    [RequireRole("Admin")]
    [SlashCommand("gamecast", "Broadcast messages to the world on behave of SYSTEM")]
    public async Task Gamecast(string message)
    {
        var msg = $"Broadcast from System> {message}";
        var sysMessage = new GameMessageSystemChat(msg, ChatMessageType.WorldBroadcast);
        PlayerManager.BroadcastToAll(sysMessage);
        PlayerManager.LogBroadcastChat(Channel.AllBroadcast, null, msg);

        await RespondAsync("Message successfully broadcast to server.", ephemeral: true);
    }
}
