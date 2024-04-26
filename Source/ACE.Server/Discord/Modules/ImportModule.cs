using System;
using System.Net.Http;
using Discord.Interactions;
using System.Threading.Tasks;
using Discord;
using System.Text.RegularExpressions;
using ACE.Database;
using ACE.Database.Models.World;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Globalization;

namespace ACE.Server.Discord.Modules;

[RequireRole("Admin")]
[Group("import", "Import weenie and landblock commands")]
public class ImportModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly HttpClient _httpClient;
    private readonly ILogger _log = Log.ForContext<ImportModule>();

    public ImportModule(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    [SlashCommand("weenie", "Imports a weenie sql file")]
    public async Task ImportSql(IAttachment weenie, bool ephemeral = true)
    {
        await DeferAsync(ephemeral);

        if (!weenie.ContentType.StartsWith("application/sql"))
        {
            await RespondAsync("File must have a sql filetype.");
            return;
        }

        var fileName = weenie.Filename;

        const string pattern = @"^(\d+).*\.sql";
        var match = Regex.Match(fileName, pattern);
        if (!match.Success)
        {
            await RespondAsync($"Weenie file did not follow the correct file name pattern. File names must begin with the wcid and end in `.sql`. Received `{fileName}`");
            return;
        }

        var parseSuccessful = uint.TryParse(match.Groups[1].Value, out var wcid);
        if (!parseSuccessful)
        {
            await RespondAsync($"Unable to parse wcid from filename: `{fileName}`.");
            return;
        }

        try
        {
            var response = await _httpClient.GetAsync(weenie.Url);
            response.EnsureSuccessStatusCode();
            var sql = await response.Content.ReadAsStringAsync();

            ImportSql(sql);

            DatabaseManager.World.ClearCachedWeenie(wcid);
            DatabaseManager.World.GetWeenie(wcid);

            _log.Information("@{DiscordUser} ({DiscordUserId}) updated weenie {WCID} ({WeenieFileName}).", Context.User, Context.User.Id, wcid, fileName);
            await FollowupAsync($"Updated weenie {wcid} using file `{fileName}`");
        }
        catch (Exception ex)
        {
            _log.Error(ex, "@{DiscordUser} ({DiscordUserId}) failed to update weenie {WCID} ({WeenieFileName}).", Context.User, Context.User.Id, wcid, fileName);
            await FollowupAsync($"Failed to update weenie {wcid} using file `{fileName}`");
        }
    }


    [SlashCommand("landblock", "Imports a landblock sql file")]
    public async Task ImportLandblock(IAttachment landblock, bool ephemeral = true)
    {
        await DeferAsync(ephemeral);

        if (!landblock.ContentType.StartsWith("application/sql"))
        {
            await RespondAsync("File must have a sql filetype.");
            return;
        }

        var fileName = landblock.Filename;

        const string pattern = @"^([0-9A-F]{4}).*\.sql";
        var match = Regex.Match(fileName, pattern, RegexOptions.IgnoreCase);
        if (!match.Success)
        {
            await RespondAsync($"Landblock file did not follow the correct file name pattern. File names must begin with the Landblock Id in hex and end in `.sql`. Received `{fileName}`");
            return;
        }

        var parseSuccessful = ushort.TryParse(match.Groups[1].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var landblockId);
        if (!parseSuccessful)
        {
            await RespondAsync($"Unable to parse Landblock Id from filename: `{fileName}`.");
            return;
        }

        try
        {
            var response = await _httpClient.GetAsync(landblock.Url);
            response.EnsureSuccessStatusCode();
            var sql = await response.Content.ReadAsStringAsync();

            ImportSql(sql);

            DatabaseManager.World.ClearCachedInstancesByLandblock(landblockId);
            DatabaseManager.World.GetCachedInstancesByLandblock(landblockId);

            _log.Information("@{DiscordUser} ({DiscordUserId}) updated landblock {LandblockId:X4} ({LandblockFileName}).", Context.User, Context.User.Id, landblockId, fileName);
            await FollowupAsync($"Updated landblock {landblockId:X4} using file `{landblock.Filename}`");
        }
        catch (Exception ex)
        {
            _log.Error(ex, "@{DiscordUser} ({DiscordUserId}) failed to update landblock {LandblockId:X4} ({LandblockFileName}).", Context.User, Context.User.Id, landblockId, fileName);
            await FollowupAsync($"Failed to update landblock {landblockId:X4} using file `{landblock.Filename}");
        }
    }

    private void ImportSql(string sql)
    {
        var sanitizedSql = sql.Replace("\r\n", "\n");

        using var ctx = new WorldDbContext();
        ctx.Database.ExecuteSqlRaw(sanitizedSql);
    }
}
