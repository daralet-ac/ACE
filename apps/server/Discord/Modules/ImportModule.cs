using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ACE.Database;
using ACE.Database.Models.World;
using ACE.Server.Discord.Models;
using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using Serilog;

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

    [SlashCommand("weenies", "Imports a weenie sql file")]
    public async Task ImportWeenies([ComplexParameter] ImportWeenieParameters importWeenieParameters)
    {
        await DeferAsync(importWeenieParameters.Ephemeral);
        var importResults = await ImportFiles(importWeenieParameters, ImportLandblock);
        var message = BuildFollowUpMessage(importResults, "weenies");
        await FollowupAsync(message);
    }

    [SlashCommand("landblocks", "Imports a landblock sql file")]
    public async Task ImportLandblocks([ComplexParameter] ImportLandblockParameters importLandblockParameters)
    {
        await DeferAsync(importLandblockParameters.Ephemeral);
        var importResults = await ImportFiles(importLandblockParameters, ImportLandblock);
        var message = BuildFollowUpMessage(importResults, "landblocks");
        await FollowupAsync(message);
    }

    private async Task<List<ImportResult>> ImportFiles(
        IImportParameters importParameters,
        Func<IAttachment, Task<ImportResult>> importFunction
    )
    {
        var importResults = new List<ImportResult>();
        foreach (var landblockFile in importParameters.Files)
        {
            var importResult = await importFunction(landblockFile);
            importResults.Add(importResult);
        }

        return importResults;
    }

    private string BuildFollowUpMessage(ICollection<ImportResult> importResults, string objectType)
    {
        var successfulImports = importResults.Count(x => x.Success);
        var result = $"Successfully updated {successfulImports} out of {importResults.Count} {objectType}.";
        var formattedResults = FormatResults(importResults);
        var message = $"{result}\n{formattedResults}";
        return message;
    }

    private async Task<ImportResult> ImportWeenie(IAttachment weenieFile)
    {
        var fileName = weenieFile.Filename;

        if (!weenieFile.ContentType.StartsWith("application/sql"))
        {
            return new ImportResult
            {
                Success = false,
                FailureReason =
                    "Discord did not recognize this as a sql file. Ensure you end the filename with `.sql`.",
                FileName = fileName
            };
        }

        const string pattern = @"^(\d+).*\.sql";
        var match = Regex.Match(fileName, pattern);
        if (!match.Success)
        {
            return new ImportResult
            {
                Success = false,
                FailureReason = "File names must begin with the weenie's wcid.",
                FileName = fileName
            };
        }

        var parseSuccessful = uint.TryParse(match.Groups[1].Value, out var wcid);
        if (!parseSuccessful)
        {
            return new ImportResult
            {
                Success = false,
                FailureReason = "Unable to parse wcid from file name.",
                FileName = fileName
            };
        }

        try
        {
            var response = await _httpClient.GetAsync(weenieFile.Url);
            response.EnsureSuccessStatusCode();
            var sql = await response.Content.ReadAsStringAsync();

            ImportSql(sql);

            DatabaseManager.World.ClearCachedWeenie(wcid);
            DatabaseManager.World.GetWeenie(wcid);

            _log.Information(
                "@{DiscordUser} ({DiscordUserId}) updated weenie {WCID} ({WeenieFileName}).",
                Context.User,
                Context.User.Id,
                wcid,
                fileName
            );
            return new ImportResult { Success = true, FileName = fileName };
        }
        catch (Exception ex)
        {
            _log.Error(
                ex,
                "@{DiscordUser} ({DiscordUserId}) failed to update weenie {WCID} ({WeenieFileName}).",
                Context.User,
                Context.User.Id,
                wcid,
                fileName
            );
            return new ImportResult
            {
                Success = false,
                FailureReason = $"Failed to update weenie {wcid}.",
                FileName = fileName
            };
        }
    }

    private async Task<ImportResult> ImportLandblock(IAttachment landblockFile)
    {
        var fileName = landblockFile.Filename;

        if (!landblockFile.ContentType.StartsWith("application/sql"))
        {
            return new ImportResult
            {
                Success = false,
                FailureReason =
                    "Discord did not recognize this as a sql file. Ensure you end the filename with `.sql`.",
                FileName = fileName
            };
        }

        const string pattern = @"^([0-9A-F]{4}).*\.sql";
        var match = Regex.Match(fileName, pattern, RegexOptions.IgnoreCase);
        if (!match.Success)
        {
            return new ImportResult
            {
                Success = false,
                FailureReason = "File names must begin with the landblock's hex id.",
                FileName = fileName
            };
        }

        var parseSuccessful = ushort.TryParse(
            match.Groups[1].Value,
            NumberStyles.HexNumber,
            CultureInfo.InvariantCulture,
            out var landblockId
        );
        if (!parseSuccessful)
        {
            return new ImportResult
            {
                Success = false,
                FailureReason = "Unable to parse Landblock Id from file",
                FileName = fileName
            };
        }

        try
        {
            var response = await _httpClient.GetAsync(landblockFile.Url);
            response.EnsureSuccessStatusCode();
            var sql = await response.Content.ReadAsStringAsync();

            ImportSql(sql);

            DatabaseManager.World.ClearCachedInstancesByLandblock(landblockId);
            DatabaseManager.World.GetCachedInstancesByLandblock(landblockId);

            _log.Information(
                "@{DiscordUser} ({DiscordUserId}) updated landblock {LandblockId:X4} ({LandblockFileName}).",
                Context.User,
                Context.User.Id,
                landblockId,
                fileName
            );
            return new ImportResult { Success = true, FileName = fileName };
        }
        catch (Exception ex)
        {
            _log.Error(
                ex,
                "@{DiscordUser} ({DiscordUserId}) failed to update landblock {LandblockId:X4} ({LandblockFileName}).",
                Context.User,
                Context.User.Id,
                landblockId,
                fileName
            );
            return new ImportResult
            {
                Success = false,
                FailureReason = $"Failed to update landblock {landblockId:X4}.",
                FileName = fileName
            };
        }
    }

    private void ImportSql(string sql)
    {
        var sanitizedSql = sql.Replace("\r\n", "\n");

        using var ctx = new WorldDbContext();
        ctx.Database.ExecuteSqlRaw(sanitizedSql);
    }

    private string FormatResults(IEnumerable<ImportResult> importResults)
    {
        var successEmoji = Emoji.Parse(":white_check_mark:");
        var failureEmoji = Emoji.Parse(":x:");

        var stringBuilder = new StringBuilder();

        foreach (var result in importResults)
        {
            if (result.Success)
            {
                stringBuilder.Append($"- {successEmoji} `{result.FileName}`\n");
            }
            else
            {
                stringBuilder.Append($"- {failureEmoji} `{result.FileName}` **Reason:** {result.FailureReason}\n");
            }
        }

        return stringBuilder.ToString().Trim();
    }
}
