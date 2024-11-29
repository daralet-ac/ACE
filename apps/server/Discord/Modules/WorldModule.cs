using System.Threading.Tasks;
using ACE.Server.Managers;
using Discord;
using Discord.Interactions;

namespace ACE.Server.Discord.Modules;

[Group("world", "World commands")]
public class WorldModule : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("status", "Get the current status of the world")]
    public async Task GetStatus()
    {
        if (WorldManager.WorldStatus == WorldManager.WorldStatusState.Closed)
        {
            await RespondAsync("The world is currently **closed**.", ephemeral: true);
            return;
        }

        await RespondAsync("The world is currently **open**.", ephemeral: true);
    }

    [SlashCommand("close", "Close the world and boot players.")]
    public async Task Close()
    {
        if (WorldManager.WorldStatus == WorldManager.WorldStatusState.Closed)
        {
            await RespondAsync("The world is already **closed**.", ephemeral: true);
            return;
        }

        var button = new ButtonBuilder
        {
            Label = "Close world!",
            CustomId = "closeWorld",
            Style = ButtonStyle.Danger
        };

        var cancelButton = new ButtonBuilder
        {
            Label = "Cancel",
            CustomId = "cancel",
            Style = ButtonStyle.Secondary
        };

        var component = new ComponentBuilder().WithButton(button).WithButton(cancelButton);

        await RespondAsync("Are you sure?", components: component.Build(), ephemeral: true);
    }

    [SlashCommand("open", "Open the world to everyone.")]
    public async Task Open()
    {
        if (WorldManager.WorldStatus == WorldManager.WorldStatusState.Open)
        {
            await RespondAsync("The world is already **open**.", ephemeral: true);
            return;
        }

        var button = new ButtonBuilder
        {
            Label = "Open world!",
            CustomId = "openWorld",
            Style = ButtonStyle.Success
        };

        var cancelButton = new ButtonBuilder
        {
            Label = "Cancel",
            CustomId = "cancel",
            Style = ButtonStyle.Secondary
        };

        var component = new ComponentBuilder().WithButton(button).WithButton(cancelButton);

        await RespondAsync("Are you sure?", components: component.Build(), ephemeral: true);
    }

    [ComponentInteraction("closeWorld", true)]
    public async Task HandleCloseWorld()
    {
        WorldManager.Close(null, true);

        await DeferAsync(true);
        var embed = new EmbedBuilder()
            .WithColor(new Color(217, 50, 50))
            .WithAuthor(Context.Interaction.User)
            .WithDescription("# World is now closed!")
            .WithCurrentTimestamp()
            .Build();

        await Context.Interaction.DeleteOriginalResponseAsync();
        await Context.Interaction.InteractionChannel.SendMessageAsync(embed: embed);
    }

    [ComponentInteraction("openWorld", true)]
    public async Task HandleOpenWorld()
    {
        WorldManager.Open(null);

        await DeferAsync(true);
        var embed = new EmbedBuilder()
            .WithColor(new Color(75, 181, 67))
            .WithAuthor(Context.Interaction.User)
            .WithDescription("# World is now open!")
            .WithCurrentTimestamp()
            .Build();

        await Context.Interaction.DeleteOriginalResponseAsync();
        await Context.Interaction.InteractionChannel.SendMessageAsync(embed: embed);
    }

    [ComponentInteraction("cancel", true)]
    public async Task HandleCancel()
    {
        await DeferAsync(true);
        await Context.Interaction.DeleteOriginalResponseAsync();
    }
}
