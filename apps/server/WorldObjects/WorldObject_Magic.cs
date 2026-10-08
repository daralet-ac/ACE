using System.Collections.Generic;
using System.Linq;
using ACE.Entity.Enum;
using ACE.Entity.Models;
using ACE.Server.Entity;
using ACE.Server.Factories;
using ACE.Server.Network.GameMessages.Messages;

namespace ACE.Server.WorldObjects;

partial class WorldObject
{
    public float SigilTrinketSpellDamageReduction;
    private bool _isSigilTrinketSpell = false;
    private PartialEvasion _partialEvasion;

    /// <summary>
    /// Instantly casts a spell for a WorldObject (ie. spell traps)
    /// </summary>
    public void TryCastSpell(
        Spell spell,
        WorldObject target,
        WorldObject itemCaster = null,
        WorldObject weapon = null,
        bool isWeaponSpell = false,
        bool fromProc = false,
        bool tryResist = true,
        bool showMsg = true,
        int? weaponSpellcraft = null,
        double damageMultiplier = 1.0
    )
    {
        // TODO: look into further normalizing this / caster / weapon

        // verify spell exists in database
        if (spell._spell == null)
        {
            if (target is Player targetPlayer)
            {
                targetPlayer.Session.Network.EnqueueSend(
                    new GameMessageSystemChat($"{spell.Name} spell not implemented, yet!", ChatMessageType.System)
                );
            }

            return;
        }

        if (spell.IsFellowshipSpell)
        {
            if (target is not Player targetPlayer || targetPlayer.Fellowship == null)
            {
                return;
            }

            var fellows = targetPlayer.Fellowship.GetFellowshipMembers();

            foreach (var fellow in fellows.Values)
            {
                TryCastSpell_Inner(spell, fellow, itemCaster, weapon, isWeaponSpell, fromProc, tryResist, showMsg, weaponSpellcraft, damageMultiplier);
            }
        }
        else
        {
            TryCastSpell_Inner(spell, target, itemCaster, weapon, isWeaponSpell, fromProc, tryResist, showMsg, weaponSpellcraft, damageMultiplier);
        }
    }

    public void TryCastSpell_Inner(
        Spell spell,
        WorldObject target,
        WorldObject itemCaster = null,
        WorldObject weapon = null,
        bool isWeaponSpell = false,
        bool fromProc = false,
        bool tryResist = true,
        bool showMsg = true,
        int? weaponSpellcraft = null,
        double damageMultiplier = 1.0
    )
    {
        // verify before resist, still consumes source item
        if (spell.MetaSpellType == SpellType.Dispel && !VerifyDispelPkStatus(itemCaster, target))
        {
            return;
        }

        // perform resistance check, if applicable
        var weaponAttackMod = weapon?.WeaponOffense;

        if (tryResist && TryResistSpell(target, spell, out _, itemCaster, false, weaponSpellcraft, weaponAttackMod))
        {
            return;
        }

        // if not resisted, cast spell
        HandleCastSpell(spell, target, itemCaster, weapon, isWeaponSpell, fromProc, false, showMsg, false, weaponSpellcraft, damageMultiplier);
    }

    /// <summary>
    /// Instantly casts a spell for a WorldObject, with optional redirects for item enchantments
    /// </summary>
    public bool TryCastSpell_WithRedirects(
        Spell spell,
        WorldObject target,
        WorldObject itemCaster = null,
        WorldObject weapon = null,
        bool isWeaponSpell = false,
        bool fromProc = false,
        bool tryResist = true,
        double damageMultiplier = 1.0
    )
    {
        if (target is Creature creatureTarget)
        {
            var targets = GetNonComponentTargetTypes(spell, creatureTarget);

            if (targets != null)
            {
                foreach (var itemTarget in targets)
                {
                    TryCastSpell(spell, itemTarget, itemCaster, weapon, isWeaponSpell, fromProc, tryResist, true, null, damageMultiplier);
                }

                return targets.Count > 0;
            }
        }

        TryCastSpell(spell, target, itemCaster, weapon, isWeaponSpell, fromProc, tryResist, true, null, damageMultiplier);

        return true;
    }

    /// <summary>
    /// Creates a spell based on MetaSpellType
    /// </summary>
    protected bool HandleCastSpell(
        Spell spell,
        WorldObject target,
        WorldObject itemCaster = null,
        WorldObject weapon = null,
        bool isWeaponSpell = false,
        bool fromProc = false,
        bool equip = false,
        bool showMsg = true,
        bool sigilTrinketSpell = false,
        int? weaponSpellcraft = null,
        double damageMultiplier = 1.0
    )
    {
        _isSigilTrinketSpell = sigilTrinketSpell;

        var targetCreature = !spell.IsSelfTargeted || spell.IsFellowshipSpell ? target as Creature : this as Creature;

        if (this is Gem || this is Food || this is Hook)
        {
            targetCreature = target as Creature;
        }

        if (spell.School == MagicSchool.LifeMagic || spell.MetaSpellType == SpellType.Dispel)
        {
            // NonComponentTargetType should be 0 for untargeted spells.
            // Return if the spell type is targeted with no target defined or the target is already dead.
            if (
                (targetCreature == null || !targetCreature.IsAlive)
                && spell.NonComponentTargetType != ItemType.None
                && spell.DispelSchool != MagicSchool.PortalMagic
            )
            {
                return false;
            }
        }

        // Sigil Scarabs
        // Excludes spells triggered by consuming a Food/Drink/Gem item (e.g. Alchemy potions) -
        // those are cast as `player.TryCastSpell(spell, player, item, ...)`, so `this` is
        // still the player even though the player didn't actively cast the spell themselves.
        if (this is Player && itemCaster is not (Food or Gem))
        {
            if (targetCreature != null && !equip)
            {
                var player = this as Player;
                switch (spell.School)
                {
                    case MagicSchool.LifeMagic:
                        player?.CheckForSigilTrinketOnCastEffects(targetCreature, spell, true, Skill.LifeMagic, SigilTrinketLifeWarMagicEffect.Intensity, null, false, _isSigilTrinketSpell);
                        player?.CheckForSigilTrinketOnCastEffects(targetCreature, spell, true, Skill.LifeMagic, SigilTrinketLifeWarMagicEffect.Shielding, null, false, _isSigilTrinketSpell);
                        player?.CheckForSigilTrinketOnCastEffects(targetCreature, spell, true, Skill.LifeMagic, SigilTrinketLifeMagicEffect.CastProt, null, false, _isSigilTrinketSpell);
                        player?.CheckForSigilTrinketOnCastEffects(targetCreature, spell, true, Skill.LifeMagic, SigilTrinketLifeMagicEffect.CastVuln, null, false, _isSigilTrinketSpell);
                        player?.CheckForSigilTrinketOnCastEffects(targetCreature, spell, true, Skill.LifeMagic, SigilTrinketLifeMagicEffect.CastItemBuff, null, false, _isSigilTrinketSpell);
                        player?.CheckForSigilTrinketOnCastEffects(targetCreature, spell, true, Skill.LifeMagic, SigilTrinketLifeMagicEffect.CastVitalRate, null, false, _isSigilTrinketSpell);
                        break;
                    case MagicSchool.WarMagic:
                        player?.CheckForSigilTrinketOnCastEffects(targetCreature, spell, true, Skill.WarMagic, SigilTrinketLifeWarMagicEffect.Intensity, null, false, _isSigilTrinketSpell);
                        player?.CheckForSigilTrinketOnCastEffects(targetCreature, spell, true, Skill.WarMagic, SigilTrinketLifeWarMagicEffect.Shielding, null, false, _isSigilTrinketSpell);
                        player?.CheckForSigilTrinketOnCastEffects(targetCreature, spell, true, Skill.WarMagic, SigilTrinketWarMagicEffect.Duplicate, null, false, _isSigilTrinketSpell);
                        break;
                    case MagicSchool.VoidMagic:
                        player?.CheckForSigilTrinketOnCastEffects(targetCreature, spell, true, Skill.VoidMagic, SigilTrinketLifeWarMagicEffect.Intensity, null, false, _isSigilTrinketSpell);
                        player?.CheckForSigilTrinketOnCastEffects(targetCreature, spell, true, Skill.VoidMagic, SigilTrinketLifeWarMagicEffect.Shielding, null, false, _isSigilTrinketSpell);
                        break;
                }
            }
        }

        switch (spell.MetaSpellType)
        {
            case SpellType.Enchantment:
            case SpellType.FellowEnchantment:

                if (itemCaster == null && targetCreature != null)
                {
                    GenerateSupportSpellThreat(spell, targetCreature);
                }

                var playerCaster = this as Player;

                if (playerCaster is { OverloadStanceIsActive: true } or { BatteryStanceIsActive: true } &&
                    spell.School is MagicSchool.VoidMagic &&
                    targetCreature != playerCaster)
                {
                    playerCaster.IncreaseChargedMeter(spell, fromProc);
                }
                
                // TODO: replace with some kind of 'rootOwner unless equip' concept?
                if (itemCaster != null && (equip || itemCaster is Gem || itemCaster is Food))
                {
                    CreateEnchantment(targetCreature ?? target, itemCaster, itemCaster, spell, equip, false, showMsg);
                }
                else
                {
                    CreateEnchantment(targetCreature ?? target, this, this, spell, equip, false, showMsg);
                }

                break;

            case SpellType.Boost:
            case SpellType.FellowBoost:

                HandleCastSpell_Boost(spell, targetCreature, fromProc, showMsg, weapon, damageMultiplier);
                break;

            case SpellType.Transfer:

                if (itemCaster == null && targetCreature != null)
                {
                    GenerateSupportSpellThreat(spell, targetCreature);
                }

                HandleCastSpell_Transfer(spell, targetCreature, showMsg, weapon, fromProc);
                break;

            case SpellType.Projectile:
            case SpellType.LifeProjectile:
            case SpellType.EnchantmentProjectile:

                HandleCastSpell_Projectile(spell, targetCreature, itemCaster, weapon, isWeaponSpell, fromProc, weaponSpellcraft, damageMultiplier);
                break;

            case SpellType.PortalLink:

                HandleCastSpell_PortalLink(spell, target);
                break;

            case SpellType.PortalRecall:

                HandleCastSpell_PortalRecall(spell, targetCreature);
                break;

            case SpellType.PortalSummon:

                HandleCastSpell_PortalSummon(spell, targetCreature, itemCaster);
                break;

            case SpellType.PortalSending:

                HandleCastSpell_PortalSending(spell, targetCreature, itemCaster);
                break;

            case SpellType.FellowPortalSending:

                HandleCastSpell_FellowPortalSending(spell, targetCreature, itemCaster);
                break;

            case SpellType.Dispel:
            case SpellType.FellowDispel:

                if (itemCaster == null && targetCreature != null)
                {
                    GenerateSupportSpellThreat(spell, targetCreature);
                }

                HandleCastSpell_Dispel(spell, targetCreature ?? target, showMsg);
                break;

            default:

                if (this is Player player)
                {
                    player.Session.Network.EnqueueSend(
                        new GameMessageSystemChat("Spell not implemented, yet!", ChatMessageType.Magic)
                    );
                }

                return false;
        }

        // play spell effects
        DoSpellEffects(spell, this, target);

        return true;
    }

    /// <summary>
    /// Plays the caster/target effects for a spell
    /// </summary>
    protected static void DoSpellEffects(Spell spell, WorldObject caster, WorldObject target, bool projectileHit = false)
    {
        if (spell.CasterEffect != 0 && (!spell.IsProjectile || !projectileHit))
        {
            caster.EnqueueBroadcast(new GameMessageScript(caster.Guid, spell.CasterEffect, spell.Formula.Scale));
        }

        if (spell.TargetEffect == 0 || (spell.IsProjectile && !projectileHit))
        {
            return;
        }

        if (!spell.IsFellowshipSpell && (spell.IsSelfTargeted || spell.Id == 5206)) // Surge of Protection
        {
            target = caster;
        }
        else if (target == null)
        {
            _log.Warning("DoSpellEffects(spell = {Spell}, caster = {Caster}, target = null, projectileHit = {ProjectileHit}) - Target is null.", spell, caster, projectileHit);
            return;
        }

        var targetBroadcaster = target.Wielder ?? target;

        targetBroadcaster.EnqueueBroadcast(
            new GameMessageScript(target.Guid, spell.TargetEffect, spell.Formula.Scale)
        );
    }

    /// <summary>
    /// Returns the epic cantrips from this item's spellbook
    /// </summary>
    public Dictionary<
        int,
        float /* probability */
    > EpicCantrips => Biota.GetMatchingSpells(LootTables.EpicCantrips, BiotaDatabaseLock);

    /// <summary>
    /// Returns the legendary cantrips from this item's spellbook
    /// </summary>
    public Dictionary<
        int,
        float /* probability */
    > LegendaryCantrips => Biota.GetMatchingSpells(LootTables.LegendaryCantrips, BiotaDatabaseLock);

    public float ItemManaRateAccumulator { get; set; }

    public bool ItemManaDepletionMessage { get; set; }

    public void OnSpellsActivated()
    {
        IsAffecting = true;
        ItemManaRateAccumulator = 0;
        ItemManaDepletionMessage = false;
    }

    public void OnSpellsDeactivated()
    {
        IsAffecting = false;
    }

    private const double defaultIgnoreSomeMagicProjectileDamage = 0.25;

    public double? GetAbsorbMagicDamage()
    {
        var absorbMagicDamage = AbsorbMagicDamage;

        if (absorbMagicDamage == null && HasImbuedEffect(ImbuedEffectType.IgnoreSomeMagicProjectileDamage))
        {
            absorbMagicDamage = defaultIgnoreSomeMagicProjectileDamage;
        }

        return absorbMagicDamage;
    }

    /// <summary>
    /// For spells with NonComponentTargetType, returns the list of equipped items matching the target type
    /// </summary>
    private static List<WorldObject> GetNonComponentTargetTypes(Spell spell, Creature target)
    {
        switch (spell.NonComponentTargetType)
        {
            case ItemType.Vestements: // impen / bane
            case ItemType.Weapon: // blood drinker
            case ItemType.LockableMagicTarget: // strengthen lock
            case ItemType.Caster: // hermetic void
            case ItemType.WeaponOrCaster: // lure blade, defender cantrip, hermetic link cantrip, mukkir sense
            case ItemType.Item: // essence lull

                return target
                    .EquippedObjects.Values.Where(i =>
                        (i.ItemType & spell.NonComponentTargetType) != 0
                        && (i.ValidLocations & EquipMask.Selectable) != 0
                        && i.IsEnchantable
                    )
                    .ToList();
        }
        return null;
    }

    private void GenerateSupportSpellThreat(Spell spell, Creature playerTarget, int amount = 0)
    {
        var player = this as Player;

        if (player == null || playerTarget == null)
        {
            return;
        }

        if (playerTarget == player) // don't add support threat if player is targeting themself
        {
            return;
        }

        var targetThreatRange = 3.0; // casting support spells on another player adds threat to monsters that are close to the targeted player
        var casterThreatRange = 3.0; // casting any support spell adds threat to all creatures near the caster

        var nearbyMonstersOfTarget = playerTarget.GetNearbyMonsters(targetThreatRange);
        var nearbyMonstersOfCaster = player.GetNearbyMonsters(casterThreatRange);

        var threatAmount = 2 * spell.Level;

        if (spell.MetaSpellType == SpellType.Boost)
        {
            threatAmount = (uint)(amount / 2);
        }

        var threatenedByTargetSupport = new List<Creature>();

        foreach (var creature in nearbyMonstersOfTarget)
        {
            creature.IncreaseTargetThreatLevel(player, (int)threatAmount * 2);
            threatenedByTargetSupport.Add(creature);
        }

        // While increasing threat of enemies nearby caster, don't increase a monster's threat towards them again if they already received threat from a targeted support spell
        foreach (var creature in nearbyMonstersOfCaster)
        {
            var skip = false;

            foreach (var threatenedCreature in threatenedByTargetSupport)
            {
                if (creature.Guid == threatenedCreature.Guid)
                {
                    skip = true;
                }
            }

            if (skip)
            {
                continue;
            }

            creature.IncreaseTargetThreatLevel(player, (int)threatAmount);
        }
    }
}
