using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Entity.AdminUi;
using ACE.Server.Network;

namespace ACE.Server.Entity.PlayerUi;

/// <summary>
/// How @playerui sends data to the Daralet Decal plugin: the same chat lines as AdminUiProtocol, JSON in base64,
///
///     DAP2|kind|id|index|count|chunk
///
/// with their own prefix, so a staff member running both plugins never has one read the other's replies. Base64 for the
/// same reason as there: Virindi Chat System posts its own copy of any line with coordinates in it (a fellow's location),
/// which the plugin can't hide. DAP1 was the same lines with the JSON as it is.
/// </summary>
public static class PlayerUiProtocol
{
    public const string Prefix = "DAP2";

    /// <summary>Short lines, the same as AdminUiProtocol's.</summary>
    public const int ChunkSize = AdminUiProtocol.ChunkSize;

    private static readonly JsonSerializerOptions JsonOptions =
        new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    /// <summary>
    /// For replies with many mostly-empty fields (a corpse's items, each a whole market listing): nulls are left out,
    /// which the plugin reads as null anyway.
    /// </summary>
    private static readonly JsonSerializerOptions CompactJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
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

    /// <summary>
    /// Sends a reply to the player's plugin: for a request it made, or unasked, when something it shows changed (the bank
    /// opening, with the plugin's bank window on). The plugin reads a reply the same way whether it asked or not.
    /// </summary>
    public static void Send(Session session, string kind, object payload, bool omitNulls = false)
    {
        foreach (var message in BuildMessages(kind, payload, omitNulls))
        {
            CommandHandlerHelper.WriteOutputInfo(session, message, ChatMessageType.Broadcast);
        }
    }
}
