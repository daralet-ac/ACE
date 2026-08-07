using System;
using System.Collections.Generic;
using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Entity.Models;
using ACE.Server.Entity;
using ACE.Server.Entity.Actions;
using ACE.Server.Factories;
using ACE.Server.Managers;
using ACE.Server.Network.GameMessages.Messages;

namespace ACE.Server.WorldObjects;

/// <summary>
/// A generic, reusable reward item: used on a compatible weapon, it permanently transforms the
/// target's appearance to match the relic's reward set (donor model picked per the TARGET's own
/// weapon type, so one relic covers every lootgen weapon type without needing a separate relic per
/// weapon type), forces the target's damage type to Bludgeon regardless of what it was, and grants
/// a flat Slayer bonus per application (see PerApplicationSlayerBonus - no per-damage-type math
/// needed since every weapon this relic touches ends up Bludgeon-typed) - the same native
/// SlayerCreatureType/SlayerDamageBonus properties the loot generator itself uses for naturally-
/// rolled Slayer weapons, so no combat-code changes were needed to make the bonus function. Can be
/// applied up to MaxApplyCount times to the same weapon, stacking the bonus each time. No damage-
/// type gate on the target (Acid included) - there's nothing to gate once every target ends up
/// Bludgeon-typed regardless of what it started as.
///
/// A different reward (different look, different creature/bonus) needs a new weenie of this same
/// WeenieType, plus - if it also needs a type-matched donor model per weapon type, rather than one
/// fixed look for every target - a new entry in RelicAppearanceSets below, keyed by the new
/// relic's own WCID.
/// </summary>
public class WeaponRelic : Stackable
{
    private const int MaxApplyCount = 5;

    private readonly record struct WeaponAppearance(
        uint Setup,
        uint SoundTable,
        uint? PaletteBase,
        uint? ClothingBase,
        uint Icon,
        uint PhysicsTable,
        float DefaultScale,
        string DisplayName
    );

    /// <summary>
    /// Per-relic-WCID sets of donor appearances, keyed by the TARGET's (ItemType, WeaponType).
    /// Casters are keyed on ItemType alone (WeaponType.Undef) since lootgen casters don't carry a
    /// meaningful WeaponType of their own.
    ///
    /// A relic WCID with no entry here falls back to a simple 1:1 copy of the relic's own
    /// Setup/Icon/PaletteBase/Shade in ApplyRelic below, regardless of target type - that's the
    /// right behavior for a future reward that's always the same single model.
    ///
    /// IMPORTANT: every value here is sourced from the official ACEmulator ACE-World-16PY-Patches
    /// repo (github.com/ACEmulator/ACE-World-16PY-Patches, Database/Patches/9 WeenieDefaults/...),
    /// confirmed against the actual per-item weenie SQL files there - NOT from treestats.net, whose
    /// ace_world_patches/ace_world_base reference data turned out to not match this project's own
    /// base.sql at all (base.sql has zero weenie_properties_d_i_d rows for any of these "Paradox-
    /// touched Olthoi" WCIDs) and caused broken icons and invisible held weapons across the board.
    /// </summary>
    private static readonly Dictionary<uint, Dictionary<(ItemType ItemType, WeaponType WeaponType), WeaponAppearance>> RelicAppearanceSets =
        new()
        {
            [1034443] = new() // Olthoi Queen's Carapace Shard -> retail "Paradox-touched Olthoi" weapon set
            {
                [(ItemType.MeleeWeapon, WeaponType.Unarmed)] = new(
                    Setup: 0x02001712,
                    SoundTable: 0x20000014,
                    PaletteBase: 0x04001114,
                    ClothingBase: 0x100006DB,
                    Icon: 0x0600669A,
                    PhysicsTable: 0x3400002B,
                    DefaultScale: 1f,
                    // The only Paradox unarmed model is shaped like a Katar - naming it that way so a
                    // transformed Cestus/Nekode/whatever doesn't visually lie about what it is.
                    DisplayName: "Katar"
                ), // Katar
                [(ItemType.MeleeWeapon, WeaponType.Sword)] = new(
                    Setup: 0x02001714,
                    SoundTable: 0x20000014,
                    PaletteBase: 0x040001BF,
                    ClothingBase: 0x1000021E,
                    Icon: 0x06001CCA,
                    PhysicsTable: 0x3400002B,
                    DefaultScale: 1f,
                    DisplayName: "Sword"
                ), // Sword
                [(ItemType.MeleeWeapon, WeaponType.Axe)] = new(
                    Setup: 0x02001711,
                    SoundTable: 0x20000014,
                    PaletteBase: 0x04001114,
                    ClothingBase: 0x100006DB,
                    Icon: 0x06006699,
                    PhysicsTable: 0x3400002B,
                    DefaultScale: 0.75f,
                    DisplayName: "Axe"
                ), // Axe
                [(ItemType.MeleeWeapon, WeaponType.Mace)] = new(
                    Setup: 0x020019FC,
                    SoundTable: 0x20000014,
                    PaletteBase: null,
                    ClothingBase: null,
                    Icon: 0x06006D97,
                    PhysicsTable: 0x3400002B,
                    DefaultScale: 0.75f,
                    DisplayName: "Mace"
                ), // Mace
                [(ItemType.MeleeWeapon, WeaponType.Spear)] = new(
                    Setup: 0x02001713,
                    SoundTable: 0x20000014,
                    PaletteBase: 0x04001114,
                    ClothingBase: 0x100006DB,
                    Icon: 0x0600669B,
                    PhysicsTable: 0x3400002B,
                    DefaultScale: 0.9f,
                    DisplayName: "Spear"
                ), // Spear
                [(ItemType.MeleeWeapon, WeaponType.Dagger)] = new(
                    Setup: 0x020019FB,
                    SoundTable: 0x20000014,
                    PaletteBase: null,
                    ClothingBase: null,
                    Icon: 0x06006D96,
                    PhysicsTable: 0x3400002B,
                    DefaultScale: 0.75f,
                    DisplayName: "Dagger"
                ), // Dagger
                [(ItemType.MeleeWeapon, WeaponType.Staff)] = new(
                    Setup: 0x020019F7,
                    SoundTable: 0x20000014,
                    PaletteBase: null,
                    ClothingBase: null,
                    Icon: 0x06006D91,
                    PhysicsTable: 0x3400002B,
                    DefaultScale: 0.75f,
                    DisplayName: "Staff"
                ), // Staff
                [(ItemType.MeleeWeapon, WeaponType.TwoHanded)] = new(
                    Setup: 0x020019F8,
                    SoundTable: 0x20000014,
                    PaletteBase: null,
                    ClothingBase: null,
                    Icon: 0x06006D92,
                    PhysicsTable: 0x3400002B,
                    DefaultScale: 0.75f,
                    DisplayName: "Great Sword"
                ), // Great Sword
                [(ItemType.Caster, WeaponType.Undef)] = new(
                    Setup: 0x020019F9,
                    SoundTable: 0x20000014,
                    PaletteBase: null,
                    ClothingBase: null,
                    Icon: 0x06006D93,
                    PhysicsTable: 0x3400002B,
                    DefaultScale: 1.5f,
                    DisplayName: "Wand"
                ), // Wand
                [(ItemType.MissileWeapon, WeaponType.Bow)] = new(
                    Setup: 0x020019F6,
                    SoundTable: 0x20000014,
                    PaletteBase: null,
                    ClothingBase: null,
                    Icon: 0x06006D94,
                    PhysicsTable: 0x3400002B,
                    DefaultScale: 1f,
                    DisplayName: "Bow"
                ), // Bow
                [(ItemType.MissileWeapon, WeaponType.Crossbow)] = new(
                    Setup: 0x020019FA,
                    SoundTable: 0x20000014,
                    PaletteBase: null,
                    ClothingBase: null,
                    Icon: 0x06006D95,
                    PhysicsTable: 0x3400002B,
                    DefaultScale: 1f,
                    DisplayName: "Crossbow"
                ), // Crossbow
                [(ItemType.MissileWeapon, WeaponType.Thrown)] = new(
                    Setup: 0x02001710,
                    SoundTable: 0x20000014,
                    PaletteBase: 0x04001114,
                    ClothingBase: 0x100006DB,
                    Icon: 0x06006698,
                    PhysicsTable: 0x3400002B,
                    DefaultScale: 1f,
                    DisplayName: "Atlatl"
                ), // Atlatl - generic fallback for a thrown weapon TryGetAppearance can't classify by
                   // subtype (see the Thrown handling there); NOT used for Axe/Club/Dagger/Javelin,
                   // which get their own scaled-down look from RelicThrownAppearanceSets below.
            },
        };

    /// <summary>
    /// Experimental "scaled-up one-handed model" alternatives for two-handed targets, tried before
    /// falling back to the plain Great Sword entry above (see ResolveTwoHandedAppearance). Same
    /// Setup/Icon/Palette as the 1H Axe/Mace/Spear entries, just bigger - first pass, needs in-game
    /// judgment on whether this actually looks better than just using the Great Sword for everything.
    /// </summary>
    private static readonly WeaponAppearance TwoHandedAxeLook = new(
        Setup: 0x02001711,
        SoundTable: 0x20000014,
        PaletteBase: 0x04001114,
        ClothingBase: 0x100006DB,
        Icon: 0x06006699,
        PhysicsTable: 0x3400002B,
        DefaultScale: 1.2f,
        DisplayName: "Axe"
    );

    private static readonly WeaponAppearance TwoHandedMaceLook = new(
        Setup: 0x020019FC,
        SoundTable: 0x20000014,
        PaletteBase: null,
        ClothingBase: null,
        Icon: 0x06006D97,
        PhysicsTable: 0x3400002B,
        DefaultScale: 1.2f,
        DisplayName: "Mace"
    );

    private static readonly WeaponAppearance TwoHandedSpearLook = new(
        Setup: 0x02001713,
        SoundTable: 0x20000014,
        PaletteBase: 0x04001114,
        ClothingBase: 0x100006DB,
        Icon: 0x0600669B,
        PhysicsTable: 0x3400002B,
        DefaultScale: 1.3f,
        DisplayName: "Spear"
    );

    /// <summary>
    /// Per-relic-WCID donor appearances for Thrown weapons, keyed by the SAME subtype index
    /// LootGenerationFactory.GetThrownWeaponsSubType returns (0=Axe, 1=Club, 2=Dagger, 3=Dart,
    /// 4=Javelin, 5=Shuriken - see LootTables.ThrownWeaponMatrix for the authoritative order; the
    /// doc comment on GetThrownWeaponsSubType itself has Dart/Javelin backwards, don't trust it).
    /// Dart (3) and Shuriken (5) should never get an entry - not allowed, by design.
    ///
    /// Reuses the melee Axe/Mace/Dagger/Spear donor models above, just at a smaller scale for a
    /// hand-thrown implement - first pass, may need in-game tuning.
    /// </summary>
    private static readonly Dictionary<uint, Dictionary<int, WeaponAppearance>> RelicThrownAppearanceSets =
        new()
        {
            [1034443] = new()
            {
                [0] = new(
                    Setup: 0x02001711,
                    SoundTable: 0x20000014,
                    PaletteBase: 0x04001114,
                    ClothingBase: 0x100006DB,
                    Icon: 0x06006699,
                    PhysicsTable: 0x3400002B,
                    DefaultScale: 0.4f,
                    DisplayName: "Axe"
                ), // Axe
                [1] = new(
                    Setup: 0x020019FC,
                    SoundTable: 0x20000014,
                    PaletteBase: null,
                    ClothingBase: null,
                    Icon: 0x06006D97,
                    PhysicsTable: 0x3400002B,
                    DefaultScale: 0.4f,
                    DisplayName: "Mace"
                ), // Club/Mace
                [2] = new(
                    Setup: 0x020019FB,
                    SoundTable: 0x20000014,
                    PaletteBase: null,
                    ClothingBase: null,
                    Icon: 0x06006D96,
                    PhysicsTable: 0x3400002B,
                    DefaultScale: 0.4f,
                    DisplayName: "Dagger"
                ), // Dagger
                [4] = new(
                    Setup: 0x02001713,
                    SoundTable: 0x20000014,
                    PaletteBase: 0x04001114,
                    ClothingBase: 0x100006DB,
                    Icon: 0x0600669B,
                    PhysicsTable: 0x3400002B,
                    DefaultScale: 0.45f,
                    DisplayName: "Spear"
                ), // Javelin/Spear
            },
        };

    /// <summary>
    /// A new biota be created taking all of its values from weenie.
    /// </summary>
    public WeaponRelic(Weenie weenie, ObjectGuid guid)
        : base(weenie, guid)
    {
        SetEphemeralValues();
    }

    /// <summary>
    /// Restore a WorldObject from the database.
    /// </summary>
    public WeaponRelic(Biota biota)
        : base(biota)
    {
        SetEphemeralValues();
    }

    private void SetEphemeralValues() { }

    public override void HandleActionUseOnTarget(Player player, WorldObject target)
    {
        UseObjectOnTarget(player, this, target);
    }

    public static void UseObjectOnTarget(Player player, WorldObject source, WorldObject target, bool confirmed = false)
    {
        if (player.IsBusy)
        {
            player.SendUseDoneEvent(WeenieError.YoureTooBusy);
            return;
        }

        if (!RecipeManager.VerifyUse(player, source, target, true))
        {
            player.SendUseDoneEvent(WeenieError.YouDoNotPassCraftingRequirements);
            return;
        }

        if (DestabilizedLootForge.TryBlockFurtherAlteration(player, target))
        {
            return;
        }

        if (target.ItemType is not (ItemType.MeleeWeapon or ItemType.MissileWeapon or ItemType.Caster))
        {
            player.Session.Network.EnqueueSend(
                new GameMessageSystemChat($"The {source.Name} may only be used on a weapon.", ChatMessageType.Craft)
            );
            player.SendUseDoneEvent();
            return;
        }

        // No damage-type gate (Acid included) - ApplyRelic forces every target's damage type to
        // Bludgeon regardless of what it was, so there's nothing left to disallow on that basis.
        var hasMatchedAppearance = TryGetAppearance(source, target, out var appearance);

        if (RelicAppearanceSets.ContainsKey(source.WeenieClassId) && !hasMatchedAppearance)
        {
            player.Session.Network.EnqueueSend(
                new GameMessageSystemChat(
                    $"The {source.Name} has no matching form for that kind of weapon.",
                    ChatMessageType.Craft
                )
            );
            player.SendUseDoneEvent();
            return;
        }

        var applyCount = target.WeaponRelicApplyCount ?? 0;

        if (applyCount >= MaxApplyCount)
        {
            player.Session.Network.EnqueueSend(
                new GameMessageSystemChat(
                    $"The {target.NameWithMaterial} has already been infused the maximum number of times ({MaxApplyCount}).",
                    ChatMessageType.Craft
                )
            );
            player.SendUseDoneEvent();
            return;
        }

        if (!confirmed)
        {
            var confirmationText = $"Infusing the {target.NameWithMaterial} with the {source.Name} ({applyCount + 1} of {MaxApplyCount}). This will permanently transform its appearance";

            if (hasMatchedAppearance)
            {
                var totalBonusPercent = Math.Round((GetCumulativeSlayerBonus(applyCount + 1) - 1.0) * 100, 1);

                confirmationText += $", convert its damage type to Bludgeoning, and bring its total Olthoi Slayer bonus to +{totalBonusPercent}%";
            }
            else
            {
                confirmationText += " and grant it a bonus";
            }

            confirmationText += ", consuming the relic. This cannot be undone.";

            if (
                !player.ConfirmationManager.EnqueueSend(
                    new Confirmation_CraftInteration(player.Guid, source.Guid, target.Guid),
                    confirmationText
                )
            )
            {
                player.SendUseDoneEvent(WeenieError.ConfirmationInProgress);
            }
            else
            {
                player.SendUseDoneEvent();
            }

            return;
        }

        var actionChain = new ActionChain();

        var animTime = 0.0f;

        player.IsBusy = true;

        if (player.CombatMode != CombatMode.NonCombat)
        {
            var stanceTime = player.SetCombatMode(CombatMode.NonCombat);
            actionChain.AddDelaySeconds(stanceTime);

            animTime += stanceTime;
        }

        animTime += player.EnqueueMotion(actionChain, MotionCommand.ClapHands);

        actionChain.AddAction(
            player,
            () =>
            {
                ApplyRelic(source, target);

                player.EnqueueBroadcast(new GameMessageUpdateObject(target));
                player.Session.Network.EnqueueSend(
                    new GameMessageSystemChat(
                        $"You infuse the {target.NameWithMaterial} with the {source.Name}.",
                        ChatMessageType.Craft
                    )
                );
                player.TryConsumeFromInventoryWithNetworking(source);
            }
        );

        player.EnqueueMotion(actionChain, MotionCommand.Ready);

        actionChain.AddAction(
            player,
            () =>
            {
                player.IsBusy = false;
            }
        );

        actionChain.EnqueueChain();

        player.NextUseTime = DateTime.UtcNow.AddSeconds(animTime);
    }

    /// <summary>
    /// PropertyInt.WeaponType is reliably set on two-handed weapons, bows/crossbows, and thrown
    /// weapons - but confirmed ABSENT on most one-handed melee templates (e.g. Jambiya, War Hammer
    /// both have no WeaponType at all in retail data, despite being ordinary Dagger/Axe-skill
    /// weapons). PropertyInt.WeaponSkill IS reliably set on essentially everything (it's what the
    /// loot generator itself uses for weapon-shape bonus rolls), so use WeaponType when present and
    /// fall back to translating WeaponSkill otherwise.
    /// </summary>
    private static WeaponType ResolveWeaponCategory(WorldObject target)
    {
        if (target.W_WeaponType != WeaponType.Undef)
        {
            return target.W_WeaponType;
        }

        return target.WeaponSkill switch
        {
            Skill.Axe => WeaponType.Axe,
            Skill.Sword => WeaponType.Sword,
            Skill.Dagger => WeaponType.Dagger,
            Skill.Mace => WeaponType.Mace,
            Skill.Spear => WeaponType.Spear,
            Skill.Staff => WeaponType.Staff,
            Skill.UnarmedCombat => WeaponType.Unarmed,
            Skill.Bow => WeaponType.Bow,
            Skill.Crossbow => WeaponType.Crossbow,
            Skill.ThrownWeapon => WeaponType.Thrown,
            _ => WeaponType.Undef,
        };
    }

    /// <summary>
    /// Two-handed weapons in this engine all use Skill.TwoHandedCombat regardless of shape (axe,
    /// mace, sword, and spear-shaped 2H weapons are indistinguishable via WeaponType/WeaponSkill
    /// alone, unlike their 1H counterparts). Experimentally picks a scaled-up 1H Axe/Mace/Spear look
    /// based on real signals rather than guessing:
    ///   - IsCleaving (PropertyInt.Cleaving) is the actual flag LootGenerationFactory_Melee's own
    ///     MutateTwoHandedWeapon uses to split "swung" 2H weapons (sword/axe/mace) from "thrust" 2H
    ///     weapons (spears) for damage-table purposes - a non-cleaving 2H weapon is a spear, full
    ///     stop, so that's checked first and is authoritative (confirmed bug: falling back to the
    ///     name-substring check alone meant an actual 2H spear had nowhere to go and fell all the
    ///     way through to the Great Sword fallback).
    ///   - Bludgeon is checked with HasFlag, not ==, since DamageType is [Flags] and a real weapon
    ///     can have a compound value (confirmed bug: a 2H Mace with a compound damage type failed
    ///     the old exact-match check and also fell through to Great Sword).
    ///   - "Axe" in the name is the last, weakest signal (Slash-damage cleaving weapons are
    ///     ambiguous between axe/sword shape with no better signal available) - falls back to the
    ///     plain Great Sword if even that doesn't match.
    /// </summary>
    private static WeaponAppearance ResolveTwoHandedAppearance(WorldObject target, WeaponAppearance fallback)
    {
        if (!target.IsCleaving)
        {
            return TwoHandedSpearLook;
        }

        if (target.W_DamageType.HasFlag(DamageType.Bludgeon))
        {
            return TwoHandedMaceLook;
        }

        if (target.Name?.Contains("Axe", StringComparison.OrdinalIgnoreCase) == true)
        {
            return TwoHandedAxeLook;
        }

        return fallback;
    }

    /// <summary>
    /// Looks up the donor appearance for this relic matching the TARGET's own weapon type.
    /// Casters are matched on ItemType alone - lootgen casters don't carry a meaningful WeaponType.
    /// Thrown weapons are matched on their actual subtype (axe/club/dagger/javelin only - dart and
    /// shuriken have no matching form and correctly fail here). Two-handed weapons additionally get
    /// routed through ResolveTwoHandedAppearance for the scaled-up Axe/Mace experiment.
    /// </summary>
    private static bool TryGetAppearance(WorldObject source, WorldObject target, out WeaponAppearance appearance)
    {
        appearance = default;

        var category = ResolveWeaponCategory(target);

        if (target.ItemType == ItemType.MissileWeapon && category == WeaponType.Thrown)
        {
            var subType = LootGenerationFactory.GetThrownWeaponsSubType(target);

            if (subType is 3 or 5) // Dart, Shuriken - never allowed, regardless of relic
            {
                return false;
            }

            if (
                RelicThrownAppearanceSets.TryGetValue(source.WeenieClassId, out var thrownSet)
                && thrownSet.TryGetValue(subType, out appearance)
            )
            {
                return true;
            }

            // Subtype 0/1/2/4 (Axe/Club/Dagger/Javelin) with no thrown-specific entry, or an
            // unrecognized subtype (6 - GetThrownWeaponsSubType found no match in
            // LootTables.ThrownWeaponMatrix, e.g. this server's actual thrown-weapon lootgen pool
            // uses different base WCIDs than retail's hardcoded list) - fall back to the generic
            // Atlatl look rather than hard-rejecting a legitimate (non-dart/shuriken) thrown weapon.
            return RelicAppearanceSets.TryGetValue(source.WeenieClassId, out var atlatlSet)
                && atlatlSet.TryGetValue((ItemType.MissileWeapon, WeaponType.Thrown), out appearance);
        }

        if (!RelicAppearanceSets.TryGetValue(source.WeenieClassId, out var appearanceSet))
        {
            return false;
        }

        var key = target.ItemType == ItemType.Caster ? (ItemType.Caster, WeaponType.Undef) : (target.ItemType, category);

        if (!appearanceSet.TryGetValue(key, out appearance))
        {
            return false;
        }

        if (category == WeaponType.TwoHanded)
        {
            appearance = ResolveTwoHandedAppearance(target, appearance);
        }

        return true;
    }

    /// <summary>
    /// Flat +5% Olthoi Slayer bonus per application. Used to scale per damage type (Bludgeon
    /// baseline, everything else scaled by Olthoi's resist ratio to equalize effective DPS) - that's
    /// now moot, since ApplyRelic forces every target's damage type to Bludgeon, so every weapon
    /// this relic touches lands on this exact same rate. Kept as a named constant (not inlined)
    /// since the reasoning - Olthoi take full, unresisted damage from Bludgeon, "weakest to blunt" -
    /// is still the whole justification for the number even though the per-type math is gone.
    /// </summary>
    private const float PerApplicationSlayerBonus = 0.05f;

    /// <summary>
    /// Total SlayerDamageBonus after `applyCount` applications (1-based) - see
    /// PerApplicationSlayerBonus for the flat per-application rate.
    /// </summary>
    private static float GetCumulativeSlayerBonus(int applyCount)
    {
        return 1.0f + applyCount * PerApplicationSlayerBonus;
    }

    private static void ApplyRelic(WorldObject source, WorldObject target)
    {
        var priorApplyCount = target.WeaponRelicApplyCount ?? 0;
        var newApplyCount = priorApplyCount + 1;

        // Appearance only - deliberately not touching MaterialType, so the target's other stats are
        // untouched, only its look (and, for a type-matched relic, its scale to match the donor
        // model) changes. ClothingBase IS copied (unlike an earlier version of this method) - the
        // real Tailoring system copies it for weapons too, and PhysicsTable/Setup mismatches already
        // taught us the hard way that skipping "looks armor-only" properties can break rendering.
        // Re-applied identically on every application (1st through MaxApplyCount-th) - harmless and
        // simpler than special-casing "only on the first use".
        if (TryGetAppearance(source, target, out var appearance))
        {
            target.SetupTableId = appearance.Setup;
            target.SoundTableId = appearance.SoundTable;
            target.IconId = appearance.Icon;
            target.PaletteBaseId = appearance.PaletteBase;
            target.ClothingBase = appearance.ClothingBase;
            target.PhysicsTableId = appearance.PhysicsTable;
            target.ObjScale = appearance.DefaultScale;

            // Every target's damage type is forced to Bludgeon, regardless of what it was (an
            // earlier version of this method left it as the target's own - superseded by this).
            // Since Olthoi take full, unresisted damage from Bludgeon ("weakest to blunt"), that
            // means every weapon this relic touches now gets the exact same flat per-application
            // Slayer rate too - see GetCumulativeSlayerBonus/PerApplicationSlayerBonus.
            target.W_DamageType = DamageType.Bludgeon;
            target.SlayerCreatureType = source.SlayerCreatureType;
            target.SlayerDamageBonus = GetCumulativeSlayerBonus(newApplyCount);

            // Completely replaces the original name (prefix/suffix/material and all) - per request,
            // the transformed weapon should read as "Black Death <Type>" regardless of what it was.
            target.Name = $"Black Death {appearance.DisplayName}";

            // Replaces whatever icon overlay(s) the original weapon had (elemental glows, boosts,
            // etc.) with just the plain Magical sparkle, regardless of the target's other mods.
            target.UiEffects = ACE.Entity.Enum.UiEffects.Magical;
        }
        else
        {
            // No type-matched set for this relic - fall back to a simple 1:1 copy of the relic's
            // own appearance, regardless of target type. No Slayer stacking here - a future relic
            // without a dictionary entry just uses its own flat SlayerDamageBonus every time, same
            // as before this feature.
            target.SetupTableId = source.SetupTableId;
            target.SoundTableId = source.SoundTableId;
            target.IconId = source.IconId;
            target.PaletteBaseId = source.PaletteBaseId;
            target.ClothingBase = source.ClothingBase;
            target.PhysicsTableId = source.PhysicsTableId;
            target.Shade = source.Shade;
            target.Shade2 = source.Shade2;
            target.Shade3 = source.Shade3;
            target.Shade4 = source.Shade4;

            target.SlayerCreatureType = source.SlayerCreatureType;
            target.SlayerDamageBonus = source.SlayerDamageBonus;
        }

        // Only appended once - re-applying wouldn't add anything new to say, and would just repeat
        // the same paragraph up to 5 times.
        if (priorApplyCount == 0)
        {
            target.LongDesc = string.IsNullOrEmpty(target.LongDesc) ? source.LongDesc : $"{target.LongDesc}\n\n{source.LongDesc}";
        }

        target.WeaponRelicApplyCount = newApplyCount;
    }
}
