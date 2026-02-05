using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ACE.Entity.Enum;
using Discord;

namespace ACE.Server.Discord;

internal sealed class MarketSnapshotRenderer
{
    // Constants / types
    internal const string SnapshotSentinel = "\u200C";

    internal enum SnapshotSectionKey
    {
        ItemType = 0,
        SigilTrinket = 1,
    }

    private static readonly Regex CamelCaseSplit = new("(?<!^)([A-Z])", RegexOptions.Compiled);

    internal sealed record SnapshotPost(string? Content, EmbedBuilder? Embed);

    internal sealed record SortedListing(
        ACE.Database.Models.Shard.PlayerMarketListing Listing,
        SnapshotSectionKey SectionKey,
        int ItemType,
        int SubType,
        int ListedPrice,
        int ListingId);

    // Public API
    internal List<SnapshotPost> BuildPosts(
        List<SortedListing> sorted,
        string tierTitle,
        int totalActive,
        long updatedUnix,
        DateTime now)
    {
        static string SplitCamel(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
            {
                return s;
            }

            return CamelCaseSplit.Replace(s, " $1");
        }

        static string SectionLabel(SnapshotSectionKey sectionKey, int itemType)
        {
            if (sectionKey == SnapshotSectionKey.SigilTrinket)
            {
                return "Sigil Trinket";
            }

            var itemTypeValue = unchecked((uint)itemType);
            return Enum.IsDefined(typeof(ItemType), itemTypeValue)
                ? SplitCamel(((ItemType)itemTypeValue).ToString())
                : $"ItemType {itemType}";
        }

        var posts = new List<SnapshotPost>(Math.Max(1, sorted.Count + 8));

        int? currentSection = null;
        foreach (var e in sorted)
        {
            var itemType = e.ItemType;
            var sectionKey = e.SectionKey;
            var sectionHeaderKey = sectionKey == SnapshotSectionKey.SigilTrinket ? int.MaxValue : itemType;
            if (!currentSection.HasValue || currentSection.Value != sectionHeaderKey)
            {
                currentSection = sectionHeaderKey;
                var header = $"## __{SectionLabel(sectionKey, itemType)}__\n" +
                             $"updated <t:{updatedUnix}:R>\n" +
                             SnapshotSentinel;
                posts.Add(new SnapshotPost(header, null));
            }

            var listingEmbed = MarketListingFormatter.BuildListingEmbed(e.Listing, now, footer: SnapshotSentinel);
            posts.Add(new SnapshotPost(null, listingEmbed));
        }

        if (posts.Count == 0)
        {
            var empty = $"**{tierTitle} ({totalActive})**\n" +
                        $"(no active listings)\n" +
                        $"updated <t:{updatedUnix}:R>\n" +
                        SnapshotSentinel;
            posts.Add(new SnapshotPost(empty, null));
        }

        return posts;
    }
}
