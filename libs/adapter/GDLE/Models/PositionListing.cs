using System.Text.Json.Serialization;
using Lifestoned.DataModel.Shared;

namespace ACE.Adapter.GDLE.Models;

public class PositionListing
{
    [JsonPropertyName("key")]
    public int PositionType { get; set; }

    [JsonIgnore]
    public string PositionTypeName => ((PositionType)PositionType).GetName();

    [JsonPropertyName("value")]
    public Position Position { get; set; } = new Position();

    [JsonIgnore]
    public bool Deleted { get; set; }
}
