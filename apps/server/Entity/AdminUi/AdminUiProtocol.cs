using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;

namespace ACE.Server.Entity.AdminUi;

/// <summary>
/// How @adminui sends data to the Daralet Admin Decal plugin. Each reply is JSON, split into system chat lines of
///
///     DAA1|kind|id|index|count|chunk
///
/// so a big player list never goes out as one huge chat line. The plugin puts the chunks with the same id back together
/// and hides these lines from the chat window. The id goes up by one for every reply so chunks from two replies never mix.
/// </summary>
public static class AdminUiProtocol
{
    public const string Prefix = "DAA1";

    public const int ChunkSize = 4000;

    private static readonly JsonSerializerOptions JsonOptions =
        new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private static int lastReplyId;

    public static List<string> BuildMessages(string kind, object payload)
    {
        return BuildMessages(
            kind,
            JsonSerializer.Serialize(payload, JsonOptions),
            Interlocked.Increment(ref lastReplyId)
        );
    }

    public static List<string> BuildMessages(string kind, string json, int replyId)
    {
        return BuildMessages(Prefix, kind, json, replyId);
    }

    /// <summary>
    /// The chunking itself, shared with PlayerUiProtocol, which sends the same lines with its own prefix.
    /// </summary>
    internal static List<string> BuildMessages(string prefix, string kind, string json, int replyId, int chunkSize = ChunkSize)
    {
        var count = Math.Max(1, (json.Length + chunkSize - 1) / chunkSize);
        var messages = new List<string>(count);

        for (var i = 0; i < count; i++)
        {
            var start = i * chunkSize;
            var chunk = json.Substring(start, Math.Min(chunkSize, json.Length - start));

            messages.Add($"{prefix}|{kind}|{replyId}|{i + 1}|{count}|{chunk}");
        }

        return messages;
    }
}
