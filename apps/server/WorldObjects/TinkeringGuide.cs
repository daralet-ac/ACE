using System.Collections.Generic;
using System.Linq;
using System.Text;
using ACE.Entity.Enum;
using MT = ACE.Entity.Enum.MaterialType;

namespace ACE.Server.WorldObjects;

/// <summary>
/// The tinkering pages at the back of each Recipe Book (see RecipeBookService): which salvage a skill applies and what
/// each one does. Which skill a salvage uses comes from Salvage.TinkeringTarget, so moving a material to another skill
/// moves it to that skill's book; the effect text below is written by hand from Salvage.HandleTinkering and has to be
/// kept in step with it. Materials with no effect there (most Jewelcrafting gems) are left out.
/// </summary>
public static class TinkeringGuide
{
    private sealed record Effect(string Name, string Text);

    private static readonly Dictionary<MT, Effect> Effects = new()
    {
        // Blacksmithing
        [MT.Brass] = new("Brass", "Weapon: +1% Physical Defense. Armor: +0.25% Physical Defense and Shield per slot."),
        [MT.Bronze] = new(
            "Bronze",
            "Weapon: +5% Damage, +0.5% Physical Defense. Armor: +5% Armor, +0.125% Physical Defense and Shield per slot."
        ),
        [MT.Copper] = new("Copper", "Weapon: +1% Attack. Armor: +0.25% Attack and Two-Handed Combat per slot."),
        [MT.Gold] = new(
            "Gold",
            "Weapon: +5% Damage, +0.5% Attack. Armor: +5% Armor, +0.125% Attack and Two-Handed Combat per slot."
        ),
        [MT.Iron] = new(
            "Iron",
            "Weapon: +7.5% Damage but +5% Weapon Time. Armor: +7.5% Armor but +0.25% Resource Penalty per slot."
        ),
        [MT.Pyreal] = new("Pyreal", "Weapon: +5% Damage, +0.5% Magic Defense. Armor: +3% Armor, +3% Ward."),
        [MT.Silver] = new("Silver", "Weapon: +1% Magic Defense. Armor: +5% Ward, +0.25% Health Regeneration per slot."),
        [MT.Steel] = new(
            "Steel",
            "Weapon: +0.5% Attack and Physical Defense. Armor: +5% Armor, +0.25% Health Regeneration per slot."
        ),

        // Leatherworking (hides, light armor only) and Weaving (fabrics, cloth armor only). The cloth effects of hides
        // and the light-armor effects of fabrics in HandleTinkering can't be reached since the split, so they're left out.
        [MT.Leather] = new("Leather", "+7.5% Armor."),
        [MT.ArmoredilloHide] = new("Armoredillo Hide", "+10% Armor but +0.25% Resource Penalty per slot."),
        [MT.GromnieHide] = new(
            "Gromnie Hide",
            "+5% Armor, +0.25% Stamina Regeneration per slot."
        ),
        [MT.ReedSharkHide] = new(
            "Reedshark Hide",
            "+5% Armor, +0.125% Attack and Dual Wield per slot."
        ),
        [MT.Linen] = new("Linen", "+5% Armor, -0.25% Resource Penalty per slot. Only on armor with a Resource Penalty."),
        [MT.Satin] = new(
            "Satin",
            "+0.25% Life Magic, Perception and Mana Regeneration per slot."
        ),
        [MT.Silk] = new("Silk", "+5% Armor, +0.25% Perception and Deception per slot."),
        [MT.Velvet] = new(
            "Velvet",
            "+0.25% War Magic, Life Magic and Deception per slot."
        ),
        [MT.Wool] = new(
            "Wool",
            "+0.25% Mana and Mana Regeneration per slot."
        ),

        // Woodworking (missile launchers)
        [MT.Ebony] = new("Ebony", "+1% Magic Defense."),
        [MT.Mahogany] = new("Mahogany", "+7.5% of the untinkered Damage Modifier."),
        [MT.Oak] = new("Oak", "+1% Physical Defense."),
        [MT.Pine] = new("Pine", "+1% Attack."),
        [MT.Teak] = new("Teak", "+0.5% Attack and Physical Defense."),

        // Spellcrafting (wands)
        [MT.GreenGarnet] = new("Green Garnet", "+7.5% of the untinkered Elemental Damage Modifier."),
        [MT.Opal] = new("Opal", "+5% of the untinkered Elemental Damage Modifier, +5% Mana Conversion."),
        [MT.Malachite] = new("Malachite", "+1% Physical Defense."),
        [MT.Amethyst] = new("Amethyst", "+1% Magic Defense."),
        [MT.LavenderJade] = new("Lavender Jade", "+7.5% of the untinkered Restoration Modifier."),
        [MT.RoseQuartz] = new("Rose Quartz", "+5% of the untinkered Restoration Modifier, +5% Mana Conversion."),

        // Jewelcrafting
        [MT.Moonstone] = new("Moonstone", "+250 Maximum Mana."),
        [MT.WhiteJade] = new("White Jade", "+3% Ward."),
    };

    private sealed record Imbue(MT Material, string Name, string WeaponEffect, string ArmorEffect);

    /// <summary>
    /// Salvage.ImbueSalvage and the effects HandleTinkering gives them. Gems with no armor effect only go on weapons.
    /// </summary>
    private static readonly List<Imbue> Imbues = new()
    {
        new(MT.Aquamarine, "Aquamarine", "Cold Rending", null),
        new(MT.BlackGarnet, "Black Garnet", "Pierce Rending", null),
        new(MT.Emerald, "Emerald", "Acid Rending", null),
        new(MT.ImperialTopaz, "Imperial Topaz", "Slash Rending", null),
        new(MT.Jet, "Jet", "Lightning Rending", null),
        new(MT.RedGarnet, "Red Garnet", "Fire Rending", null),
        new(MT.WhiteSapphire, "White Sapphire", "Bludgeon Rending", null),
        new(MT.BlackOpal, "Black Opal", "Critical Strike", "Critical Resistance"),
        new(MT.FireOpal, "Fire Opal", "Crippling Blow", "Critical Damage Resistance"),
        new(MT.Sunstone, "Sunstone", "Armor Rending", "Armor Tempering"),
        new(MT.Tourmaline, "Tourmaline", "Ward Rending", "Ward Tempering"),
    };

    /// <summary>
    /// What each skill imbues, following Salvage.GetOffensiveImbuingSkill (the imbue gem uses the skill of the item
    /// it's applied to, not its own).
    /// </summary>
    private static readonly Dictionary<Skill, (string Items, bool Weapons, bool Armor)> ImbueTargets = new()
    {
        [Skill.Blacksmithing] = ("melee weapons, thrown weapons and heavy armor", true, true),
        [Skill.Woodworking] = ("bows, crossbows and atlatls", true, false),
        [Skill.Spellcrafting] = ("wands, orbs and staves", true, false),
        [Skill.Leatherworking] = ("light armor", false, true),
        [Skill.Weaving] = ("cloth armor", false, true),
        [Skill.Jewelcrafting] = ("jewelry", true, false),
    };

    private const string Introduction =
        "Tinkering\n\nApply salvage to an item to improve it. Your chance depends on your skill, the workmanship of the "
        + "salvage and the item, and how often the item has been tinkered already. An item can be tinkered as many times as its workmanship. \"Per slot\" bonuses grow with the "
        + "number of body parts the armor covers.";

    /// <summary>
    /// The tinkering blocks for a skill's book, or none when the skill doesn't tinker.
    /// </summary>
    public static List<string> GetBlocks(Skill skill)
    {
        var blocks = new List<string>();

        var materials = Salvage
            .TinkeringTarget.Where(t => t.Value == skill && t.Key.HasValue && Effects.ContainsKey(t.Key.Value))
            .Select(t => Effects[t.Key.Value])
            .ToList();

        ImbueTargets.TryGetValue(skill, out var imbueTargets);
        var hasImbues = imbueTargets.Items != null;

        if (materials.Count == 0 && !hasImbues)
        {
            return blocks;
        }

        blocks.Add(Introduction);

        foreach (var effect in materials)
        {
            blocks.Add($"{effect.Name}\n{effect.Text}");
        }

        if (hasImbues)
        {
            blocks.Add(GetImbueBlock(imbueTargets.Items, imbueTargets.Weapons, imbueTargets.Armor));
        }

        return blocks;
    }

    private static string GetImbueBlock(string items, bool weapons, bool armor)
    {
        var sb = new StringBuilder();
        sb.Append($"Imbues\nThis skill imbues {items}. An item can be imbued once it has been fully tinkered.");

        foreach (var imbue in Imbues)
        {
            if (weapons && armor)
            {
                sb.Append(
                    imbue.ArmorEffect == null
                        ? $"\n{imbue.Name}: {imbue.WeaponEffect} (weapons only)"
                        : $"\n{imbue.Name}: {imbue.WeaponEffect}, or {imbue.ArmorEffect} on armor"
                );
            }
            else if (armor)
            {
                if (imbue.ArmorEffect != null)
                {
                    sb.Append($"\n{imbue.Name}: {imbue.ArmorEffect}");
                }
            }
            else if (items == "jewelry")
            {
                // Jewelry takes only the gems that aren't weapon-only, with their weapon effect
                if (imbue.ArmorEffect != null)
                {
                    sb.Append($"\n{imbue.Name}: {imbue.WeaponEffect}");
                }
            }
            else
            {
                sb.Append($"\n{imbue.Name}: {imbue.WeaponEffect}");
            }
        }

        return sb.ToString();
    }
}
