using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ACE.Server.Managers;
using Discord;
using Discord.Interactions;

namespace ACE.Server.Discord.Modules;

public class ServerPropertyModule : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("modifybool", "Modifies a server property that is a bool")]
    public async Task ModifyBool(
        [Summary("property"), Autocomplete(typeof(BoolPropertyAutocompleteHandler))] string property,
        [Summary("value")] bool value
    )
    {
        var prevState = PropertyManager.GetBool(property);

        if (prevState.Item == value && !string.IsNullOrWhiteSpace(prevState.Description))
        {
            await RespondAsync($"Bool property is already **{value}** for **{property}**!", ephemeral: true);
            return;
        }

        if (!PropertyManager.ModifyBool(property, value))
        {
            await RespondAsync(
                $"Unknown bool property **{property}** was not updated. Type showprops for a list of properties.",
                ephemeral: true
            );
            return;
        }

        if (property == "pk_server" || property == "pkl_server")
        {
            PlayerManager.UpdatePKStatusForAllPlayers(property, value);
        }

        await ReportSuccess("bool", property, value.ToString());
    }

    [SlashCommand("modifylong", "Modifies a server property that is a long")]
    public async Task ModifyLong(
        [Summary("property"), Autocomplete(typeof(LongPropertyAutocompleteHandler))] string property,
        [Summary("value")] long value
    )
    {
        if (!PropertyManager.ModifyLong(property, value))
        {
            await RespondAsync(
                $"Unknown long property **{property}** was not updated. Type showprops for a list of properties.",
                ephemeral: true
            );
            return;
        }

        await ReportSuccess("long", property, value.ToString());
    }

    [SlashCommand("modifydouble", "Modifies a server property that is a double")]
    public async Task ModifyDouble(
        [Summary("property"), Autocomplete(typeof(DoublePropertyAutocompleteHandler))] string property,
        [Summary("value")] double value
    )
    {
        if (!PropertyManager.ModifyDouble(property, value))
        {
            await RespondAsync(
                $"Unknown double property **{property}** was not updated. Type showprops for a list of properties.",
                ephemeral: true
            );
            return;
        }

        await ReportSuccess("double", property, value.ToString());
    }

    [SlashCommand("modifystring", "Modifies a server property that is a string")]
    public async Task ModifyString(
        [Summary("property"), Autocomplete(typeof(StringPropertyAutocompleteHandler))] string property,
        [Summary("value")] string value
    )
    {
        if (!PropertyManager.ModifyString(property, value))
        {
            await RespondAsync(
                $"Unknown string property **{property}** was not updated. Type showprops for a list of properties.",
                ephemeral: true
            );
            return;
        }

        await ReportSuccess("string", property, value);
    }

    [SlashCommand("fetchbool", "Fetches a server property that is a bool")]
    public async Task FetchBool(
        [Summary("property"), Autocomplete(typeof(BoolPropertyAutocompleteHandler))] string property
    )
    {
        var boolVal = PropertyManager.GetBool(property, cacheFallback: false);
        await ReportValue(property, boolVal.Description, boolVal.Item.ToString());
    }

    [SlashCommand("fetchlong", "Fetches a server property that is a long")]
    public async Task FetchLong(
        [Summary("property"), Autocomplete(typeof(LongPropertyAutocompleteHandler))] string property
    )
    {
        var longVal = PropertyManager.GetLong(property, cacheFallback: false);
        await ReportValue(property, longVal.Description, longVal.Item.ToString());
    }

    [SlashCommand("fetchdouble", "Fetches a server property that is a double")]
    public async Task FetchDouble(
        [Summary("property"), Autocomplete(typeof(DoublePropertyAutocompleteHandler))] string property
    )
    {
        var doubleVal = PropertyManager.GetDouble(property, cacheFallback: false);
        await ReportValue(property, doubleVal.Description, doubleVal.Item.ToString());
    }

    [SlashCommand("fetchstring", "Fetches a server property that is a string")]
    public async Task FetchString(
        [Summary("property"), Autocomplete(typeof(StringPropertyAutocompleteHandler))] string property
    )
    {
        var stringVal = PropertyManager.GetString(property, cacheFallback: false);
        await ReportValue(property, stringVal.Description, stringVal.Item);
    }

    private async Task ReportValue(string property, string description, string value)
    {
        await RespondAsync(
            $"**{property}** - {description ?? "No Description"}:\n```\n{Truncate(value)}\n```",
            ephemeral: true
        );
    }

    /// <summary>
    /// Discord rejects messages over 2000 characters, and string properties can be long lists
    /// </summary>
    private static string Truncate(string value)
    {
        const int maxValueLength = 1500;

        value ??= "";
        if (value.Length > maxValueLength)
        {
            value = value.Substring(0, maxValueLength) + "… (truncated)";
        }

        return value;
    }

    private async Task ReportSuccess(string type, string property, string value)
    {
        PlayerManager.BroadcastToAuditChannel(
            null,
            $"{Context.User.Username} (Discord) successfully changed server {type} property {property} to {value}"
        );

        var embed = new EmbedBuilder()
            .WithColor(new Color(75, 181, 67))
            .WithAuthor(Context.Interaction.User)
            .WithDescription($"Server {type} property **{property}** set to **{Truncate(value)}**.")
            .WithCurrentTimestamp()
            .Build();

        await RespondAsync(embed: embed);
    }
}

public abstract class PropertyNameAutocompleteHandler : AutocompleteHandler
{
    protected abstract IEnumerable<string> PropertyNames { get; }

    public override Task<AutocompletionResult> GenerateSuggestionsAsync(
        IInteractionContext context,
        IAutocompleteInteraction autocompleteInteraction,
        IParameterInfo parameter,
        IServiceProvider services
    )
    {
        var input = autocompleteInteraction.Data.Current.Value as string ?? "";

        // Discord caps autocomplete at 25 suggestions
        var suggestions = PropertyNames
            .Where(name => name.Contains(input, StringComparison.OrdinalIgnoreCase))
            .OrderBy(name => name)
            .Take(25)
            .Select(name => new AutocompleteResult(name, name));

        return Task.FromResult(AutocompletionResult.FromSuccess(suggestions));
    }
}

public class BoolPropertyAutocompleteHandler : PropertyNameAutocompleteHandler
{
    protected override IEnumerable<string> PropertyNames => DefaultPropertyManager.DefaultBooleanProperties.Keys;
}

public class LongPropertyAutocompleteHandler : PropertyNameAutocompleteHandler
{
    protected override IEnumerable<string> PropertyNames => DefaultPropertyManager.DefaultLongProperties.Keys;
}

public class DoublePropertyAutocompleteHandler : PropertyNameAutocompleteHandler
{
    protected override IEnumerable<string> PropertyNames => DefaultPropertyManager.DefaultDoubleProperties.Keys;
}

public class StringPropertyAutocompleteHandler : PropertyNameAutocompleteHandler
{
    protected override IEnumerable<string> PropertyNames => DefaultPropertyManager.DefaultStringProperties.Keys;
}
