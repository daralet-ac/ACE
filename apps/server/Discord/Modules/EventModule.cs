using System;
using System.Linq;
using System.Threading.Tasks;
using ACE.Server.Managers;
using Discord;
using Discord.Interactions;

namespace ACE.Server.Discord.Modules;

[Group("event", "Game event commands")]
public class EventModule : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("start", "Start a game event")]
    public async Task Start([Summary("event_name"), Autocomplete(typeof(EventNameAutocompleteHandler))] string eventName)
    {
        if (!EventManager.StartEvent(eventName, null, null))
        {
            await RespondAsync($"Unable to start event named **{eventName}**.", ephemeral: true);
            return;
        }

        PlayerManager.BroadcastToAuditChannel(null, $"{Context.User.Username} (Discord) has started event {eventName}.");

        await RespondAsync(embed: BuildEmbed(new Color(75, 181, 67), $"Event **{eventName}** started."));
    }

    [SlashCommand("stop", "Stop a game event")]
    public async Task Stop([Summary("event_name"), Autocomplete(typeof(EventNameAutocompleteHandler))] string eventName)
    {
        if (!EventManager.StopEvent(eventName, null, null))
        {
            await RespondAsync($"Unable to stop event named **{eventName}**.", ephemeral: true);
            return;
        }

        PlayerManager.BroadcastToAuditChannel(null, $"{Context.User.Username} (Discord) has stopped event {eventName}.");

        await RespondAsync(embed: BuildEmbed(new Color(217, 50, 50), $"Event **{eventName}** stopped."));
    }

    [SlashCommand("status", "Get the status of a game event")]
    public async Task Status([Summary("event_name"), Autocomplete(typeof(EventNameAutocompleteHandler))] string eventName)
    {
        await RespondAsync(
            $"Event **{eventName}** - GameEventState.{EventManager.GetEventStatus(eventName)}",
            ephemeral: true
        );
    }

    private Embed BuildEmbed(Color color, string description)
    {
        return new EmbedBuilder()
            .WithColor(color)
            .WithAuthor(Context.Interaction.User)
            .WithDescription(description)
            .WithCurrentTimestamp()
            .Build();
    }
}

public class EventNameAutocompleteHandler : AutocompleteHandler
{
    public override Task<AutocompletionResult> GenerateSuggestionsAsync(
        IInteractionContext context,
        IAutocompleteInteraction autocompleteInteraction,
        IParameterInfo parameter,
        IServiceProvider services
    )
    {
        var input = autocompleteInteraction.Data.Current.Value as string ?? "";

        // Discord caps autocomplete at 25 suggestions
        var suggestions = EventManager
            .Events.Keys.ToList()
            .Where(name => name.Contains(input, StringComparison.OrdinalIgnoreCase))
            .OrderBy(name => name)
            .Take(25)
            .Select(name => new AutocompleteResult(name, name));

        return Task.FromResult(AutocompletionResult.FromSuccess(suggestions));
    }
}
