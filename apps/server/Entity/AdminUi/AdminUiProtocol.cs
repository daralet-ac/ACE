using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace ACE.Server.Entity.AdminUi;

/// <summary>
/// How @adminui sends data to the Daralet Admin Decal plugin. Each reply is JSON in base64, split into system chat lines of
///
///     DAA2|kind|id|index|count|chunk
///
/// The plugin puts the chunks with the same id back together, decodes them and hides these lines from the chat window. The
/// id goes up by one for every reply so chunks from two replies never mix.
///
/// Base64, because other chat plugins act on readable text before the plugin can hide it: Virindi Chat System posts its own
/// copy of any line with coordinates in it ("Hebian-to (39.0S, 83.0E)" in the player list), and that copy showed in chat.
/// DAA1 was the same lines with the JSON as it is.
/// </summary>
public static class AdminUiProtocol
{
    public const string Prefix = "DAA2";

    /// <summary>Short lines, like PlayerUiProtocol's.</summary>
    public const int ChunkSize = 200;

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
    /// The encoding and chunking itself, shared with PlayerUiProtocol, which sends the same lines with its own prefix.
    /// </summary>
    internal static List<string> BuildMessages(string prefix, string kind, string json, int replyId, int chunkSize = ChunkSize)
    {
        var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
        var count = Math.Max(1, (base64.Length + chunkSize - 1) / chunkSize);
        var messages = new List<string>(count);

        for (var i = 0; i < count; i++)
        {
            var start = i * chunkSize;
            var chunk = base64.Substring(start, Math.Min(chunkSize, base64.Length - start));

            messages.Add($"{prefix}|{kind}|{replyId}|{i + 1}|{count}|{chunk}");
        }

        return messages;
    }
}
