using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using ACE.Server.Entity.AdminUi;

namespace ACE.Server.Entity.PlayerUi;

/// <summary>
/// How @playerui sends data to the Daralet Decal plugin: the same chat lines as AdminUiProtocol,
///
///     DAP1|kind|id|index|count|chunk
///
/// with their own prefix, so a staff member running both plugins never has one read the other's replies.
/// </summary>
public static class PlayerUiProtocol
{
    public const string Prefix = "DAP1";

    /// <summary>
    /// Much shorter than @adminui's 4,000: a chat line of a thousand characters or so (a fellowship's vitals, every second)
    /// sometimes reached the chat window instead of being hidden by the plugin, which looks for lines that start with
    /// "DAP1|". Short lines keep every chunk whole and starting with the prefix.
    /// </summary>
    public const int ChunkSize = 200;

    private static readonly JsonSerializerOptions JsonOptions =
        new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    /// <summary>
    /// For replies with many mostly-empty fields (a corpse's items, each a whole market listing): nulls are left out,
    /// which the plugin reads as null anyway.
    /// </summary>
    private static readonly JsonSerializerOptions CompactJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    private static int lastReplyId;

    public static List<string> BuildMessages(string kind, object payload, bool omitNulls = false)
    {
        return BuildMessages(
            kind,
            JsonSerializer.Serialize(payload, omitNulls ? CompactJsonOptions : JsonOptions),
            Interlocked.Increment(ref lastReplyId)
        );
    }

    public static List<string> BuildMessages(string kind, string json, int replyId)
    {
        return AdminUiProtocol.BuildMessages(Prefix, kind, json, replyId, ChunkSize);
    }
}
