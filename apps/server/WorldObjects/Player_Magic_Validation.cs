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
    public bool VerifySpell(uint spellId, WorldObject casterItem = null)
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
    /// Returns TRUE if the currently equipped casting implement
    /// has a built-in spell
    /// </summary>
    public bool IsWeaponSpell(uint spellId, WorldObject casterItem)
    {
        var caster = casterItem;

        if (caster == null || caster.SpellDID == null)
        {
            return false;
        }

        return caster.SpellDID == spellId;
    }

    public bool VerifyBusy()
    {
        if (IsBusy || Teleporting || suicideInProgress)
        {
            SendUseDoneEvent(WeenieError.YoureTooBusy);
            return false;
        }
        return true;
    }

    public Spell ValidateSpell(uint spellId, bool isWeaponSpell = false)
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

    public bool VerifySpellTarget(Spell spell, WorldObject target)
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

    public bool VerifySpellRange(
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

        var maxRange = Math.Min(spell.BaseRangeConstant + magicSkill * spell.BaseRangeMod, MaxRadarRange_Outdoors);

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

    public CastingPreCheckStatus GetCastingPreCheckStatus(Spell spell, uint magicSkill, bool isWeaponSpell)
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

        // casting non-portal spells with a NoCompsForPortalSpells caster will always fizzle
        if (spell.School is not MagicSchool.WarMagic && GetEquippedWand() is { NoCompsRequiredForMagicSchool: (int)MagicSchool.WarMagic}
            || spell.School is not MagicSchool.LifeMagic && GetEquippedWand() is { NoCompsRequiredForMagicSchool: (int)MagicSchool.LifeMagic }
            || spell.School is not MagicSchool.PortalMagic && GetEquippedWand() is { NoCompsRequiredForMagicSchool: (int)MagicSchool.PortalMagic})
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

    public bool VerifyNonComponentTargetType(Spell spell, WorldObject target)
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

    private bool IsAdvancedSpell(Spell spell)
    {
        SpellCategory[] advancedSpellCategories =
        {
            // War
            SpellCategory.AcidBurst,
            SpellCategory.BludgeoningBurst,
            SpellCategory.ColdBurst,
            SpellCategory.ElectricBurst,
            SpellCategory.FireBurst,
            SpellCategory.PiercingBurst,
            SpellCategory.SlashingBurst,
            SpellCategory.AcidBlast,
            SpellCategory.BludgeoningBlast,
            SpellCategory.ColdBlast,
            SpellCategory.ElectricBlast,
            SpellCategory.FireBlast,
            SpellCategory.PiercingBlast,
            SpellCategory.SlashingBlast,
            SpellCategory.AcidVolley,
            SpellCategory.BladeVolley,
            SpellCategory.BludgeoningVolley,
            SpellCategory.FlameVolley,
            SpellCategory.ForceVolley,
            SpellCategory.FrostVolley,
            SpellCategory.LightningVolley,
            SpellCategory.AcidStreak,
            SpellCategory.BludgeoningStreak,
            SpellCategory.ColdStreak,
            SpellCategory.ElectricStreak,
            SpellCategory.FireStreak,
            SpellCategory.PiercingStreak,
            SpellCategory.SlashingStreak,
            SpellCategory.AcidWall,
            SpellCategory.BludgeoningWall,
            SpellCategory.ColdWall,
            SpellCategory.ElectricWall,
            SpellCategory.FireWall,
            SpellCategory.PiercingWall,
            SpellCategory.SlashingWall,
            SpellCategory.AcidRing,
            SpellCategory.BludgeoningRing,
            SpellCategory.ColdRing,
            SpellCategory.ElectricRing,
            SpellCategory.FireRing,
            SpellCategory.PiercingRing,
            SpellCategory.SlashingRing,
            // Life
            SpellCategory.NetherDamageRatingLowering,
            SpellCategory.NetherDamageHealingReductionRaising,
            SpellCategory.NetherDamageOverTimeRaising,
            SpellCategory.NetherDamageOverTimeRaising2,
            SpellCategory.NetherDamageOverTimeRaising3,
        };

        SpellId[] advancedSpellIds =
        {
            // War

            // Life
            SpellId.HealFellow1,
            SpellId.DispelLifeBadFellow1,
            SpellId.FellowshipHeal1,
            SpellId.FellowshipHeal2,
            SpellId.FellowshipHeal3,
            SpellId.FellowshipHeal4,
            SpellId.FellowshipHeal5,
            SpellId.FellowshipHeal6,
            SpellId.FellowshipHeal7,
            SpellId.FellowshipRevitalize1,
            SpellId.FellowshipRevitalize2,
            SpellId.FellowshipRevitalize3,
            SpellId.FellowshipRevitalize4,
            SpellId.FellowshipRevitalize5,
            SpellId.FellowshipRevitalize6,
            SpellId.FellowshipRevitalize7,
            SpellId.FellowshipManaBoost1,
            SpellId.FellowshipManaBoost2,
            SpellId.FellowshipManaBoost3,
            SpellId.FellowshipManaBoost4,
            SpellId.FellowshipManaBoost5,
            SpellId.FellowshipManaBoost6,
            SpellId.FellowshipManaBoost7,
            SpellId.HealthBolt1,
            SpellId.HealthBolt2,
            SpellId.HealthBolt3,
            SpellId.HealthBolt4,
            SpellId.HealthBolt5,
            SpellId.HealthBolt6,
            SpellId.HealthBolt7,
            SpellId.StaminaBolt1,
            SpellId.StaminaBolt2,
            SpellId.StaminaBolt3,
            SpellId.StaminaBolt4,
            SpellId.StaminaBolt5,
            SpellId.StaminaBolt6,
            SpellId.StaminaBolt7,
            SpellId.ManaBolt1,
            SpellId.ManaBolt2,
            SpellId.ManaBolt3,
            SpellId.ManaBolt4,
            SpellId.ManaBolt5,
            SpellId.ManaBolt6,
            SpellId.ManaBolt7,
            SpellId.VitalityMend1,
            SpellId.VitalityMend2,
            SpellId.VitalityMend3,
            SpellId.VitalityMend4,
            SpellId.VitalityMend5,
            SpellId.VitalityMend6,
            SpellId.VitalityMend7,
            SpellId.VigorMend1,
            SpellId.VigorMend2,
            SpellId.VigorMend3,
            SpellId.VigorMend4,
            SpellId.VigorMend5,
            SpellId.VigorMend6,
            SpellId.VigorMend7,
            SpellId.ClarityMend1,
            SpellId.ClarityMend2,
            SpellId.ClarityMend3,
            SpellId.ClarityMend4,
            SpellId.ClarityMend5,
            SpellId.ClarityMend6,
            SpellId.ClarityMend7,
            SpellId.VitalityMendOther1,
            SpellId.VitalityMendOther2,
            SpellId.VitalityMendOther3,
            SpellId.VitalityMendOther4,
            SpellId.VitalityMendOther5,
            SpellId.VitalityMendOther6,
            SpellId.VitalityMendOther7,
            SpellId.VigorMendOther1,
            SpellId.VigorMendOther2,
            SpellId.VigorMendOther3,
            SpellId.VigorMendOther4,
            SpellId.VigorMendOther5,
            SpellId.VigorMendOther6,
            SpellId.VigorMendOther7,
            SpellId.ClarityMendOther1,
            SpellId.ClarityMendOther2,
            SpellId.ClarityMendOther3,
            SpellId.ClarityMendOther4,
            SpellId.ClarityMendOther5,
            SpellId.ClarityMendOther6,
            SpellId.ClarityMendOther7,
        };

        if (advancedSpellCategories.Contains(spell.Category) || advancedSpellIds.Contains((SpellId)spell.Id))
        {
            return true;
        }

        return false;
    }

    /// <summary>
     /// Returns TRUE if the spell is one of the Restoration Resonance spells (VitalityMend, VigorMend, or ClarityMend)
     /// </summary>
    private bool IsRestorationResonanceSpell(SpellCategory spellCategory)
    {
        return spellCategory == SpellCategory.VitalityMend
            || spellCategory == SpellCategory.VigorMend
            || spellCategory == SpellCategory.ClarityMend;
    }
}
