using ACE.Database;
using ACE.Database.SQLFormatters.World;
using Discord.Interactions;
using System.IO;
using System;
using System.Threading.Tasks;
using ACE.Entity;
using Discord;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ACE.Server.Discord.Modules;

[RequireRole("Admin")]
[Group("export", "Export weenie and landblock commands")]
public class ExportModule : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("weenie", "Export weenie file")]
    public async Task ExportWeenie(uint wcid, bool ephemeral = true)
    {
        await DeferAsync(ephemeral);
        var weenie = DatabaseManager.World.GetWeenie(wcid);
        if (weenie == null)
        {
            await FollowupAsync($"No weenie found with wcid: {wcid}");
            return;
        }

        var weenieSqlWriter = new WeenieSQLWriter
        {
            WeenieNames = DatabaseManager.World.GetAllWeenieNames(),
            SpellNames = DatabaseManager.World.GetAllSpellNames(),
            TreasureDeath = DatabaseManager.World.GetAllTreasureDeath(),
            TreasureWielded = DatabaseManager.World.GetAllTreasureWielded(),
            PacketOpCodes = PacketOpCodeNames.Values
        };

        try
        {
            using var stream = new MemoryStream();
            await using var writer = new StreamWriter(stream);
            writer.AutoFlush = true;

            weenieSqlWriter.CreateSQLDELETEStatement(weenie, writer);
            await writer.WriteLineAsync();
            weenieSqlWriter.CreateSQLINSERTStatement(weenie, writer);
            
            using var file = new FileAttachment(stream, weenieSqlWriter.GetDefaultFileName(weenie));
            await FollowupWithFileAsync(file);
        }
        catch (Exception e)
        {
            await FollowupWithFileAsync($"Failed to export wcid: {wcid}.\n{e.Message}");
        }
    }

    [SlashCommand("landblock", "Export landblock file")]
    public async Task ExportLandblock(string landblock, bool ephemeral = true)
    {
        await DeferAsync(ephemeral);

        var parseSuccessful = ushort.TryParse(Regex.Match(landblock, @"[0-9A-F]{4}", RegexOptions.IgnoreCase).Value, NumberStyles.HexNumber,
            CultureInfo.InvariantCulture, out var landblockId);
        if (!parseSuccessful)
        {
            await FollowupAsync($"Could not parse landblock with id: {landblock}");
            return;
        }

        var instances = DatabaseManager.World.GetCachedInstancesByLandblock(landblockId);
        if (instances == null)
        {
            await FollowupAsync($"Could not find landblock with id: {landblockId:X4}");
            return;
        }

        var landblockInstanceWriter = new LandblockInstanceWriter
        {
            WeenieNames = DatabaseManager.World.GetAllWeenieNames()
        };

        try
        {
            using var stream = new MemoryStream();
            await using var writer = new StreamWriter(stream);
            writer.AutoFlush = true;

            landblockInstanceWriter.CreateSQLDELETEStatement(instances, writer);
            await writer.WriteLineAsync();
            landblockInstanceWriter.CreateSQLINSERTStatement(instances, writer);
            
            using var file = new FileAttachment(stream, $"{landblockId:X4}.sql");
            await FollowupWithFileAsync(file);
        }
        catch (Exception e)
        {
            await FollowupAsync($"Failed to export landblock: {landblockId:X4}\n{e.Message}");
        }
    }
}
