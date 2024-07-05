using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Lifestoned.DataModel.Shared;

namespace ACE.Adapter.GDLE.Models;

public class SpellbookEntry
{
    [JsonPropertyName("key")]
    public int SpellId { get; set; }

    [JsonPropertyName("value")]
    public SpellCastingStats Stats { get; set; } = new SpellCastingStats();

    [JsonIgnore]
    public bool Deleted { get; set; }

    public string GetSpellDescription()
    {
        var input = ((SpellId)SpellId).ToString();
        return "(" + SpellId + ") " + Regex.Replace(input, "([A-Z0-9])", " $1", RegexOptions.Compiled).Trim();
    }
}
