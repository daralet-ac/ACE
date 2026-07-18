using System;
using System.Collections.Generic;
using System.Text;
using ACE.Entity;
using ACE.Entity.Enum.Properties;
using ACE.Entity.Models;

namespace ACE.Server.WorldObjects;

/// <summary>
/// An unfinished spell scroll. Players scribe spell components onto it in order
/// (Scroll Writing / Spellcrafting), then finalize it with Scroll Dust via <see cref="SpellDust"/>.
/// </summary>
public class BlankScroll : WorldObject
{
    private const string BaseLongDesc =
        "A blank scroll. Double-click, then target spell components in your inventory, one at a time and in the correct order, to scribe a spell formula onto it. Use Spell Dust on it once you have finished scribing.";

    /// <summary>
    /// A new biota be created taking all of its values from weenie.
    /// </summary>
    public BlankScroll(Weenie weenie, ObjectGuid guid)
        : base(weenie, guid)
    {
        SetEphemeralValues();
    }

    /// <summary>
    /// Restore a WorldObject from the database.
    /// </summary>
    public BlankScroll(Biota biota)
        : base(biota)
    {
        SetEphemeralValues();
    }

    private void SetEphemeralValues()
    {
        RefreshLongDesc();
    }

    /// <summary>
    /// The ordered list of spell component IDs (SpellComponentsTable IDs) scribed onto this scroll so far.
    /// Persisted as a comma-separated list in PropertyString.ScrollWritingComponents, same pattern as
    /// SigilTrinket.AllowedSpecializedSkills, so scribing progress survives relog/trade.
    /// </summary>
    public List<uint> ScribedComponents
    {
        get
        {
            var raw = GetProperty(PropertyString.ScrollWritingComponents);
            if (string.IsNullOrWhiteSpace(raw))
            {
                return new List<uint>();
            }

            var parts = raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var list = new List<uint>(parts.Length);
            foreach (var p in parts)
            {
                if (uint.TryParse(p, out var v))
                {
                    list.Add(v);
                }
            }

            return list;
        }
        set
        {
            if (value == null || value.Count == 0)
            {
                RemoveProperty(PropertyString.ScrollWritingComponents);
            }
            else
            {
                SetProperty(PropertyString.ScrollWritingComponents, string.Join(",", value));
            }

            RefreshLongDesc();
        }
    }

    /// <summary>
    /// Rebuilds LongDesc from the base description plus the scribed component names, in the order
    /// they were added, so examining the scroll shows scribing progress.
    /// </summary>
    private void RefreshLongDesc()
    {
        var scribed = ScribedComponents;

        if (scribed.Count == 0)
        {
            LongDesc = BaseLongDesc;
            return;
        }

        var comps = SpellComponent.SpellComponentsTable.SpellComponents;

        var sb = new StringBuilder(BaseLongDesc);
        sb.Append("\n\nScribed components (in order):");

        for (var i = 0; i < scribed.Count; i++)
        {
            var name = comps.TryGetValue(scribed[i], out var comp) ? comp.Name : "Unknown Component";
            sb.Append($"\n{i + 1}. {name}");
        }

        LongDesc = sb.ToString();
    }

    public override void HandleActionUseOnTarget(Player player, WorldObject target)
    {
        ScrollWritingService.TryScribeComponent(player, this, target);
    }
}
