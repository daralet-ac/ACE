using System.Text.Json.Serialization;
using Lifestoned.DataModel.Shared;

namespace ACE.Adapter.GDLE.Models;

public class SkillListing
{
    [JsonPropertyName("key")]
    public int? SkillId { get; set; }

    [JsonPropertyName("value")]
    public Skill Skill { get; set; }

    [JsonIgnore]
    public string SkillName => ((SkillId?)SkillId).GetName();

    [JsonIgnore]
    public bool Deleted { get; set; }
}
