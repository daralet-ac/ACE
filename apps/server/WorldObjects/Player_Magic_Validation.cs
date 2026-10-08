using System;
using System.Linq;
using ACE.Common;
using ACE.Entity.Enum;
using ACE.Server.Entity;
using ACE.Server.Managers;
using ACE.Server.Network.GameEvent.Events;
using ACE.Server.Network.GameMessages.Messages;

namespace ACE.Server.WorldObjects;

partial class Player
{
    /// <summary>
    /// Verifies spell is contained in player's spellbook,
    /// or in the weapon's spellbook in the case of built-in spells
    /// </summary>
    /// <param name="casterItem">The casting item, when casting one of its built-in spells</param>
    private bool VerifySpell(uint spellId, WorldObject casterItem = null)
    {
        if (casterItem != null)
        {
            return IsWeaponSpell(spellId, casterItem);
        }
        else
        {
            return SpellIsKnown(spellId);
        }
    }

    /// <summary>
    /// The only school a casting item can cast (War, Life or Portal Magic), or null if it isn't restricted
    /// </summary>
    private static MagicSchool? GetRestrictedSchool(WorldObject caster)
    {
        return caster?.NoCompsRequiredForMagicSchool switch
        {
            (int)MagicSchool.WarMagic => MagicSchool.WarMagic,
            (int)MagicSchool.LifeMagic => MagicSchool.LifeMagic,
            (int)MagicSchool.PortalMagic => MagicSchool.PortalMagic,
            _ => null,
        };
    }

    /// <summary>
    /// Returns TRUE if the currently equipped casting implement
    /// has a built-in spell
    /// </summary>
    private bool IsWeaponSpell(uint spellId, WorldObject casterItem)
    {
        var caster = casterItem;

        if (caster == null || caster.SpellDID == null)
        {
            return false;
        }

        return caster.SpellDID == spellId;
    }

    private bool VerifyBusy()
    {
        if (IsBusy || Teleporting || suicideInProgress)
        {
            SendUseDoneEvent(WeenieError.YoureTooBusy);
            return false;
        }
        return true;
    }

    private Spell ValidateSpell(uint spellId, bool isWeaponSpell = false)
    {
        var spell = new Spell(spellId);

        if (spell.NotFound)
        {
            if (spell._spellBase == null)
            {
                Session.Network.EnqueueSend(
                    new GameEventCommunicationTransientString(Session, $"SpellId {spell.Id} Invalid.")
                );
                SendUseDoneEvent(WeenieError.None);
            }
            else
            {
                Session.Network.EnqueueSend(
                    new GameMessageSystemChat($"{spell.Name} spell not implemented, yet!", ChatMessageType.System)
                );
                SendUseDoneEvent(WeenieError.MagicInvalidSpellType);
            }
            return null;
        }
        if (!isWeaponSpell && !HasComponentsForSpell(spell))
        {
            SendUseDoneEvent(WeenieError.YouDontHaveAllTheComponents);
            return null;
        }

        return spell;
    }

    private bool VerifySpellTarget(Spell spell, WorldObject target)
    {
        if (IsInvalidTarget(spell, target))
        {
            Session.Network.EnqueueSend(
                new GameEventCommunicationTransientString(Session, $"{spell.Name} cannot be cast on {target.Name}.")
            );
            SendUseDoneEvent(WeenieError.None);
            return false;
        }
        return true;
    }

    /// <summary>
    /// Determines whether the target for the spell being cast is invalid
    /// </summary>
    protected bool IsInvalidTarget(Spell spell, WorldObject target)
    {
        var targetPlayer = target as Player;
        var targetCreature = target as Creature;

        // ensure target is enchantable
        if (!target.IsEnchantable)
        {
            return true;
        }

        // Self targeted spells should have a target of self
        if (spell.Flags.HasFlag(SpellFlags.SelfTargeted) && target != this)
        {
            return true;
        }

        // Invalidate non Item Enchantment spells cast against non Creatures or Players
        if (spell.School != MagicSchool.PortalMagic && targetCreature == null)
        {
            return true;
        }

        // Invalidate beneficial spells against Creature/Non-player targets
        if (targetCreature != null && targetPlayer == null && spell.IsBeneficial)
        {
            return true;
        }

        // check item spells
        if (targetCreature == null && target.WielderId != null)
        {
            var parent = CurrentLandblock.GetObject(target.WielderId.Value) as Player;

            // Invalidate beneficial spells against monster wielded items
            if (parent == null && spell.IsBeneficial)
            {
                return true;
            }

            // Invalidate harmful spells against player wielded items, depending on pk status
            if (parent != null && spell.IsHarmful && CheckPKStatusVsTarget(parent, spell) != null)
            {
                return true;
            }
        }

        // verify target type for item enchantment
        if (spell.School == MagicSchool.PortalMagic && !VerifyNonComponentTargetType(spell, target))
        {
            if (spell.DispelSchool != MagicSchool.PortalMagic || !PropertyManager.GetBool("item_dispel").Item)
            {
                return true;
            }
        }

        // brittlemail / lure / other negative item spells cannot be cast with player as target

        // TODO: by end of retail, players couldn't cast any negative spells on themselves
        // this feature is currently in ace for dev testing...
        if (target == this && spell.IsNegativeRedirectable)
        {
            return true;
        }

        if (
            targetCreature != null
            && targetCreature != this
            && spell.NonComponentTargetType == ItemType.Creature
            && !CanDamage(targetCreature)
        )
        {
            return true;
        }

        return false;
    }

    private bool VerifySpellRange(
        WorldObject target,
        TargetCategory targetCategory,
        Spell spell,
        WorldObject casterItem,
        uint magicSkill
    )
    {
        if (
            targetCategory != TargetCategory.WorldObject && targetCategory != TargetCategory.Wielded
            || target.Guid == Guid
        )
        {
            return true;
        }

        var targetLoc = target;
        if (targetLoc.WielderId != null)
        {
            targetLoc = CurrentLandblock?.GetObject(targetLoc.WielderId.Value);
        }

        var distanceTo = Location.Distance2D(targetLoc.Location);

        if (casterItem == null)
        {
            // range uses the current skill in the spell's school, not the modded skill used for casting
            var playerSkill = GetCreatureSkill(spell.School);
            magicSkill = playerSkill.Current;
        }

        var maxRange = spell.GetMaxCastRange(magicSkill);

        if (distanceTo > maxRange)
        {
            SendUseDoneEvent(WeenieError.MissileOutOfRange);
            return false;
        }

        // bootstrapping this function for indoor/outdoor check, since it is called both before and after windup
        if (spell.Flags.HasFlag(SpellFlags.NotIndoor))
        {
            if (Location.Indoors || target.Location.Indoors)
            {
                SendUseDoneEvent(WeenieError.YourSpellCannotBeCastInside);
                return false;
            }
        }
        if (spell.Flags.HasFlag(SpellFlags.NotOutdoor))
        {
            if (!Location.Indoors || !target.Location.Indoors)
            {
                SendUseDoneEvent(WeenieError.YourSpellCannotBeCastOutside);
                return false;
            }
        }
        return true;
    }

    private CastingPreCheckStatus GetCastingPreCheckStatus(Spell spell, uint magicSkill, bool isWeaponSpell)
    {
        var difficulty = spell.Power;

        var castingPreCheckStatus = CastingPreCheckStatus.CastFailed;

        if (magicSkill > 0 && magicSkill >= (int)difficulty - 50)
        {
            var chance = SkillCheck.GetMagicSkillChance((int)magicSkill, (int)difficulty);
            var rng = ThreadSafeRandom.Next(0.0f, 1.0f);
            if (chance > rng)
            {
                castingPreCheckStatus = CastingPreCheckStatus.Success;
            }
        }

        // portal spells never fizzle
        if (spell.School == MagicSchool.PortalMagic)
        {
            castingPreCheckStatus = GetEquippedWand() is { NoCompsRequiredForMagicSchool: (int)MagicSchool.PortalMagic, ItemCurMana: 0 } ? CastingPreCheckStatus.CastFailed : CastingPreCheckStatus.Success;
        }

        // a caster that can only cast one school always fizzles the other schools
        if (GetRestrictedSchool(GetEquippedWand()) is { } restrictedSchool && spell.School != restrictedSchool)
        {
            castingPreCheckStatus = CastingPreCheckStatus.CastFailed;
        }

        // build-in spells never fizzle
        if (isWeaponSpell)
        {
            castingPreCheckStatus = CastingPreCheckStatus.Success;
        }

        // Check for Nether Dampening preventing Restoration Resonance spells
        if (EnchantmentManager.HasSpell((uint)SpellId.VoidRestorationPenalty) && IsRestorationResonanceSpell(spell.Category))
        {
            Session.Network.EnqueueSend(
                new GameMessageSystemChat(
                    "The Nether energies permeating your form prevent you from casting heal-over-time spells!",
                    ChatMessageType.Magic
                )
            );
            castingPreCheckStatus = CastingPreCheckStatus.CastFailed;
        }

        // Check for Restoration Resonance preventing Void spells
        if (EnchantmentManager.HasSpell((uint)SpellId.RestorationResonance) && spell.School == MagicSchool.VoidMagic)
        {
            Session.Network.EnqueueSend(
                new GameMessageSystemChat(
                    "The Restoration energies flowing through you prevent you from casting Void spells!",
                    ChatMessageType.Magic
                )
            );
            castingPreCheckStatus = CastingPreCheckStatus.CastFailed;
        }

        return castingPreCheckStatus;
    }

    private bool VerifyNonComponentTargetType(Spell spell, WorldObject target)
    {
        // untargeted spell projectiles
        if (target == null)
        {
            return spell.NonComponentTargetType == ItemType.None;
        }

        switch (spell.NonComponentTargetType)
        {
            case ItemType.Creature:
                return target is Creature;

            // banes / lures
            case ItemType.Vestements:
                return target is Creature || target is Clothing || target.IsShield;

            case ItemType.Weapon:
                return target is Creature || target is MeleeWeapon || target is MissileLauncher;

            case ItemType.Caster:
                return target is Creature || target is Caster;

            case ItemType.WeaponOrCaster:
                return target is Creature || target is MeleeWeapon || target is MissileLauncher || target is Caster;

            case ItemType.Portal:

                if (spell.MetaSpellType == SpellType.PortalRecall || spell.MetaSpellType == SpellType.PortalSummon)
                {
                    return target is Creature;
                }
                else
                {
                    return target is Portal;
                }

            case ItemType.LockableMagicTarget:
                return target is Door || target is Chest;

            // Essence Lull?
            case ItemType.Item:
                return !(target is Creature);

            case ItemType.LifeStone:
                return target is Lifestone;
        }

        _log.Error(
            $"VerifyNonComponentTargetType({spell.Id} - {spell.Name}, {target.Name}) - unexpected NonComponentTargetType {spell.NonComponentTargetType}"
        );
        return false;
    }

    private bool VerifyAdvancedSpell(Spell spell)
    {
        if (!IsAdvancedSpell(spell))
        {
            return true;
        }

        if (
            spell.School == MagicSchool.WarMagic
            && GetCreatureSkill(Skill.WarMagic).AdvancementClass != SkillAdvancementClass.Specialized
        )
        {
            Session.Network.EnqueueSend(
                new GameMessageSystemChat(
                    $"You must be specialized in War Magic to cast {spell.Name}",
                    ChatMessageType.Broadcast
                )
            );
            SendUseDoneEvent(WeenieError.BadCast);
            return false;
        }
        else if (
            (spell.School == MagicSchool.LifeMagic || spell.School == MagicSchool.VoidMagic)
            && GetCreatureSkill(Skill.LifeMagic).AdvancementClass != SkillAdvancementClass.Specialized
        )
        {
            Session.Network.EnqueueSend(
                new GameMessageSystemChat(
                    $"You must be specialized in Life Magic to cast {spell.Name}",
                    ChatMessageType.Broadcast
                )
            );
            SendUseDoneEvent(WeenieError.BadCast);
            return false;
        }

        return true;
    }

    private static bool IsAdvancedSpell(Spell spell)
    {
        return AdvancedSpells.IsAdvanced(spell.Category, spell.Id);
    }

    /// <summary>
    /// Returns TRUE if the spell is one of the Restoration Resonance spells (VitalityMend, VigorMend, or ClarityMend)
    /// </summary>
    private static bool IsRestorationResonanceSpell(SpellCategory spellCategory)
    {
        return spellCategory == SpellCategory.VitalityMend
            || spellCategory == SpellCategory.VigorMend
            || spellCategory == SpellCategory.ClarityMend;
    }
}
