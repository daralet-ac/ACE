using System;
using System.Collections.Generic;
using ACE.Common;
using ACE.Entity.Enum;
using ACE.Server.Entity.Actions;
using ACE.Server.Factories;
using ACE.Server.Factories.Tables;
using ACE.Server.Network.GameMessages.Messages;
using ACE.Server.WorldObjects;
using Serilog;

namespace ACE.Server.Entity;

public class SigilTrinketEvent
{
    private readonly ILogger _log = Log.ForContext<SigilTrinketEvent>();

    // General variables
    public bool OnCrit;
    public Skill Skill;
    public int EffectId;
    private List<string> _effectsUsed = [];
    public Player Player;
    public WorldObject Target;

    // OnCast variables
    public Spell TriggerSpell;
    public bool IsWeaponSpell;
    public Creature CreatureToCastSpellFrom;
    public WorldObject SpellSource;

    // OnAttack variables
    public DamageEvent DamageEvent;

    // OnSpellHitReceived variables
    public int SpellDamageReceived;

    /// <summary>
    /// Check equipped Sigil Trinket to see if any have a relevant effect for the trigger action
    /// </summary>
    public bool HasReadySigilTrinketEffect(SigilTrinket sigilTrinket)
    {
        if (!SigilTrinketValidationChecks(sigilTrinket))
        {
            return false;
        }

        if (!SigilTrinketCooldownChecks(sigilTrinket))
        {
            return false;
        }

        var rollAgainstTrigger = ThreadSafeRandom.Next(0.0f, 1.0f);
        if (sigilTrinket.SigilTrinketTriggerChance < rollAgainstTrigger)
        {
            return false;
        }

        // This trinket's trigger successfully occurred. Add the effect ID to a list to prevent other equipped trinkets with the same effect from triggering for this spell.
        if (sigilTrinket.SigilTrinketEffectId != null && sigilTrinket.WieldSkillType != null)
        {
            var effectName = SigilTrinket.GetEffectName((Skill)sigilTrinket.WieldSkillType, (int)sigilTrinket.SigilTrinketEffectId);
            _effectsUsed.Add(effectName);
        }

        sigilTrinket.NextTrinketTriggerTime = Time.GetUnixTime() + sigilTrinket.CooldownDuration ?? 20.0;
        sigilTrinket.StartCooldown(Player);
        sigilTrinket.DecreaseStructure(1, Player);

        // Also stamp the player with a cooldown time for each slot, to prevent swapping trinkets to bypass cooldown.
        switch (sigilTrinket.SigilTrinketColor)
        {
            case (int)SigilTrinketColor.Blue:
                Player.NextTriggerTimeBlue = Time.GetUnixTime() + sigilTrinket.CooldownDuration ?? 20.0;
                break;
            case (int)SigilTrinketColor.Yellow:
                Player.NextTriggerTimeYellow = Time.GetUnixTime() + sigilTrinket.CooldownDuration ?? 20.0;
                break;
            case (int)SigilTrinketColor.Red:
                Player.NextTriggerTimeRed = Time.GetUnixTime() + sigilTrinket.CooldownDuration ?? 20.0;
                break;
        }

        return true;
    }

    public void StartSigilTrinketEffect(SigilTrinket sigilTrinket)
    {
        if (sigilTrinket.SigilTrinketEffectId == null)
        {
            return;
        }

        sigilTrinket.TriggerSpell = TriggerSpell;
        sigilTrinket.IsWeaponSpell = IsWeaponSpell;
        sigilTrinket.SigilTrinketTarget = Target;

        switch ((sigilTrinket.WieldSkillType, sigilTrinket.SigilTrinketEffectId))
        {
            case ((int)Skill.LifeMagic, (int)SigilTrinketLifeMagicEffect.ScarabCastVuln):
            case ((int)Skill.LifeMagic, (int)SigilTrinketLifeMagicEffect.ScarabCastProt):
            case ((int)Skill.LifeMagic, (int)SigilTrinketLifeMagicEffect.ScarabCastItemBuff):
            case ((int)Skill.LifeMagic, (int)SigilTrinketLifeMagicEffect.ScarabCastVitalRate):
                var maxLevel = sigilTrinket.SigilTrinketMaxTier ?? 1;
                sigilTrinket.SpellLevel = (uint)Math.Min(sigilTrinket.TriggerSpell.Level, maxLevel);

                CastSigilTrinketSpell(sigilTrinket);
                break;
            case ((int)Skill.LifeMagic, (int)SigilTrinketLifeMagicEffect.ScarabIntensity):
            case ((int)Skill.WarMagic, (int)SigilTrinketWarMagicEffect.ScarabIntensity):

                if (sigilTrinket.SigilTrinketIntensity != null)
                {
                    sigilTrinket.TriggerSpell.SpellPowerMod = (float)sigilTrinket.SigilTrinketIntensity + 1;
                    Player.Session.Network.EnqueueSend(
                        new GameMessageSystemChat(
                            $"Sigil Scarab of Intensity increased the spell's intensity by " +
                            $"{Math.Round((sigilTrinket.SigilTrinketIntensity ?? 1) * 100, 0)}%!",
                            ChatMessageType.Magic
                        )
                    );
                }
                break;
            case ((int)Skill.LifeMagic, (int)SigilTrinketLifeMagicEffect.ScarabShield):
            case ((int)Skill.WarMagic, (int)SigilTrinketWarMagicEffect.ScarabShield):

                maxLevel = sigilTrinket.SigilTrinketMaxTier ?? 1;
                sigilTrinket.SpellLevel = (uint)Math.Min(sigilTrinket.TriggerSpell.Level, maxLevel);
                sigilTrinket.SigilTrinketCastSpellId = 5206; // Surge of Protection

                CastSigilTrinketSpell(sigilTrinket, false);
                break;
            case ((int)Skill.WarMagic, (int)SigilTrinketWarMagicEffect.ScarabDuplicate):

                maxLevel = sigilTrinket.SigilTrinketMaxTier ?? 1;
                sigilTrinket.SpellLevel = (uint)Math.Min(sigilTrinket.TriggerSpell.Level, maxLevel);
                sigilTrinket.SigilTrinketCastSpellId = sigilTrinket.TriggerSpell.Id;

                CastSigilTrinketSpell(sigilTrinket);
                break;
            case ((int)Skill.WarMagic, (int)SigilTrinketWarMagicEffect.ScarabDetonate):

                maxLevel = sigilTrinket.SigilTrinketMaxTier ?? 1;
                sigilTrinket.SpellLevel = (uint)Math.Min(sigilTrinket.TriggerSpell.Level, maxLevel);
                sigilTrinket.SigilTrinketCastSpellId = DetonateSpellId(sigilTrinket.TriggerSpell);
                sigilTrinket.SpellIntensityMultiplier = AoeSpellIntensity(sigilTrinket.TriggerSpell);
                sigilTrinket.CreatureToCastSpellFrom = CreatureToCastSpellFrom;

                if (sigilTrinket.SigilTrinketCastSpellId != null)
                {
                    CastSigilTrinketSpell(sigilTrinket, false);
                }
                break;
            case ((int)Skill.WarMagic, (int)SigilTrinketWarMagicEffect.ScarabCrit):

                maxLevel = sigilTrinket.SigilTrinketMaxTier ?? 1;
                sigilTrinket.SpellLevel = (uint)Math.Min(sigilTrinket.TriggerSpell.Level, maxLevel);
                sigilTrinket.SigilTrinketCastSpellId = (int)SpellId.SigilTrinketCriticalDamageBoost;

                CastSigilTrinketSpell(sigilTrinket, false);
                break;
            case ((int)Skill.TwoHandedCombat, (int)SigilTrinketTwohandedCombatEffect.Might):
            case ((int)Skill.Shield, (int)SigilTrinketShieldEffect.Might):

                if (DamageEvent is null)
                {
                    _log.Error("StartSigilTrinketEffect({SigilTrinket}) - DamageEvent is null for Compass of Might case.", sigilTrinket.Name);
                    break;
                }

                DamageEvent.CriticalOverridedByTrinket = true;

                Player.Session.Network.EnqueueSend(
                    new GameMessageSystemChat(
                        $"Sigil Compass of Might coverted a normal hit into a critical hit!",
                        ChatMessageType.CombatSelf
                    )
                );
                
                break;
            case ((int)Skill.TwoHandedCombat, (int)SigilTrinketTwohandedCombatEffect.Aggression):
            case ((int)Skill.Shield, (int)SigilTrinketShieldEffect.Aggression):
            {
                if (Target is Creature creatureTarget)
                {
                    creatureTarget.DoubleThreatFromNextAttackTargets.Add(Player);

                    Player.Session.Network.EnqueueSend(
                        new GameMessageSystemChat(
                            $"Sigil Compass of Aggression doubled the amount of threat your attack produced against your target!",
                            ChatMessageType.CombatSelf
                        )
                    );
                }

                break;
            }
            case ((int)Skill.DualWield, (int)SigilTrinketDualWieldEffect.Assailment):
                maxLevel = sigilTrinket.SigilTrinketMaxTier ?? 1;
                sigilTrinket.SpellLevel = (uint)maxLevel; // TODO: if wielding a lower tier weapon, cast lower tier spell (like war/life checks)
                sigilTrinket.SigilTrinketCastSpellId = (int)SpellId.SigilTrinketCriticalChanceBoost;

                CastSigilTrinketSpell(sigilTrinket, false);

                Player.Session.Network.EnqueueSend(
                    new GameMessageSystemChat(
                        $"Sigil Puzzle Box of Assailment boosts your critical damage for the next 10 seconds!",
                        ChatMessageType.CombatSelf
                    )
                );

                break;
            case ((int)Skill.Thievery, (int)SigilTrinketThieveryEffect.Treachery):

                if (DamageEvent is null)
                {
                    _log.Error("StartSigilTrinketEffect({SigilTrinket}) - DamageEvent is null for Puzzle Box of Treachery case.", sigilTrinket.Name);
                    break;
                }

                DamageEvent.CriticalDamageBonusFromTrinket += 1.0f;

                Player.Session.Network.EnqueueSend(
                    new GameMessageSystemChat(
                        $"Sigil Puzzle Box of Treachery doubled your critical damage!",
                        ChatMessageType.CombatSelf
                    )
                );

                break;
            case ((int)Skill.PhysicalDefense, (int)SigilTrinketPhysicalDefenseEffect.Evasion):

                if (DamageEvent is null)
                {
                    _log.Error("StartSigilTrinketEffect({SigilTrinket}) - DamageEvent is null for Pocket Watch of Evasion case.", sigilTrinket.Name);
                    break;
                }

                DamageEvent.PartialEvasion = PartialEvasion.All;
                DamageEvent.Evaded = true;

                Player.Session.Network.EnqueueSend(
                    new GameMessageSystemChat(
                        $"Sigil Pocket Watch of Evasion converted a glancing blow into a full evade!",
                        ChatMessageType.CombatSelf
                    )
                );

                break;
            case ((int)Skill.MagicDefense, (int)SigilTrinketMagicDefenseEffect.Absorption):

                if (Target is null)
                {
                    _log.Error("StartSigilTrinketEffect({SigilTrinket}) - Target is null for Top of Absorption case.", sigilTrinket.Name);
                    break;
                }

                const float spellDamageReductionMod = 0.5f;

                Target.SigilTrinketSpellDamageReduction = spellDamageReductionMod;

                var damageReduced = Convert.ToUInt32(SpellDamageReceived * (1 - spellDamageReductionMod));
                Player.UpdateVitalDelta(Player.Mana, damageReduced);

                Player.Session.Network.EnqueueSend(
                    new GameMessageSystemChat(
                        $"Sigil Top of Absorption converted {damageReduced} spell damage into mana!",
                        ChatMessageType.Magic
                    )
                );

                break;
            case ((int)Skill.Perception, (int)SigilTrinketPerceptionEffect.Exposure):

                maxLevel = sigilTrinket.SigilTrinketMaxTier ?? 1;
                sigilTrinket.SpellLevel = (uint)maxLevel; // TODO: if wielding a lower tier weapon, cast lower tier spell (like war/life checks)
                sigilTrinket.SigilTrinketCastSpellId = (int)SpellId.SigilTrinketDamageBoost;

                CastSigilTrinketSpell(sigilTrinket, false);

                Player.Session.Network.EnqueueSend(
                    new GameMessageSystemChat(
                        $"Sigil Goggles of Exposure boosts your damage for the next 10 seconds!",
                        ChatMessageType.Magic
                    )
                );

                break;
            case ((int)Skill.Deception, (int)SigilTrinketDeceptionEffect.Avoidance):
            {
                if (Target is Creature creatureTarget)
                {
                    creatureTarget.SkipThreatFromNextAttackTargets.Add(Player);

                    Player.Session.Network.EnqueueSend(
                        new GameMessageSystemChat(
                            $"Sigil Goggles of Avoidance prevented your attack from adding threat to your target!",
                            ChatMessageType.CombatSelf
                        )
                    );
                }

                break;
            }
        }
    }

    /// <summary>
    /// Prepare a spell to be cast, using the CastSpelId on the sigil trinket.
    /// </summary>
    private void CastSigilTrinketSpell(SigilTrinket sigilTrinket, bool useProgression = true)
    {
        SpellId castSpellLevel1Id;

        switch ((sigilTrinket.WieldSkillType, sigilTrinket.SigilTrinketEffectId))
        {
            case ((int)Skill.LifeMagic, (int)SigilTrinketLifeMagicEffect.ScarabCastVitalRate):
                switch (sigilTrinket.TriggerSpell.Category)
                {
                    case SpellCategory.HealthRaising:
                    case SpellCategory.HealingRaising:
                        castSpellLevel1Id = SpellId.RegenerationOther1;
                        break;
                    case SpellCategory.StaminaRaising:
                        castSpellLevel1Id = SpellId.RejuvenationOther1;
                        break;
                    case SpellCategory.ManaRaising:
                        castSpellLevel1Id = SpellId.ManaRenewalOther1;
                        break;
                    case SpellCategory.HealthLowering:
                        castSpellLevel1Id = SpellId.FesterOther1;
                        break;
                    case SpellCategory.StaminaLowering:
                        castSpellLevel1Id = SpellId.ExhaustionOther1;
                        break;
                    case SpellCategory.ManaLowering:
                        castSpellLevel1Id = SpellId.ManaDepletionOther1;
                        break;
                    default:
                        return;
                }
                break;
            case ((int)Skill.LifeMagic, (int)SigilTrinketLifeMagicEffect.ScarabCastItemBuff):
                switch (sigilTrinket.TriggerSpell.Category)
                {
                    case SpellCategory.HealthRaising:
                    case SpellCategory.HealingRaising:
                        castSpellLevel1Id = SpellId.DefenderOther1;
                        break;
                    case SpellCategory.StaminaRaising:
                        castSpellLevel1Id = SpellId.BloodDrinkerOther1;
                        break;
                    case SpellCategory.ManaRaising:
                        castSpellLevel1Id = SpellId.SpiritDrinkerOther1;
                        break;
                    //case SpellCategory.HealthLowering: castSpellLevel1Id = SpellId.FesterOther1; break;
                    //case SpellCategory.StaminaLowering: castSpellLevel1Id = SpellId.ExhaustionOther1; break;
                    //case SpellCategory.ManaLowering: castSpellLevel1Id = SpellId.ManaDepletionOther1; break;
                    default:
                        return;
                }
                break;
            default:
            {
                castSpellLevel1Id = (SpellId)(sigilTrinket.SigilTrinketCastSpellId ?? 0);
                if (castSpellLevel1Id == SpellId.Undef)
                {
                    return;
                }
                break;
            }
        }

        SpellId castSpellId;
        if (useProgression && sigilTrinket.SpellLevel != null)
        {
            castSpellId = SpellLevelProgression.GetSpellAtLevel(castSpellLevel1Id, (int)sigilTrinket.SpellLevel, true);
            if (castSpellId == SpellId.Undef)
            {
                return;
            }
        }
        else
        {
            castSpellId = castSpellLevel1Id;
        }

        // Create Sigil Trinket Spell with different delays depending on the effect type
        switch ((sigilTrinket.WieldSkillType, sigilTrinket.SigilTrinketEffectId))
        {
            case ((int)Skill.LifeMagic, (int)SigilTrinketLifeMagicEffect.ScarabCastVitalRate):
                CreateSigilTrinketSpell(sigilTrinket, castSpellId, 1.0);
                break;
            case ((int)Skill.WarMagic, (int)SigilTrinketWarMagicEffect.ScarabDetonate):
                if (sigilTrinket.CreatureToCastSpellFrom != null)
                {
                    CreateSigilTrinketSpell(sigilTrinket, castSpellId, 0.0);
                }
                break;
            default:
                CreateSigilTrinketSpell(sigilTrinket, castSpellId, 0.5);
                break;
        }
    }

    /// <summary>
    /// Create the prepared spell in the world, decrease trinket structure, and start cooldown.
    /// </summary>
    private void CreateSigilTrinketSpell(SigilTrinket sigilTrinket, SpellId castSpellId, double delay)
    {
        var castSpell = new Spell(castSpellId)
        {
            SpellPowerMod = sigilTrinket.SpellIntensityMultiplier,
            SpellStatModVal = sigilTrinket.SpellStatModValMultiplier
        };

        var spellChain = new ActionChain();
        spellChain.AddDelaySeconds(delay);
        spellChain.AddAction(
            Player,
            () =>
            {
                // Detonate
                if (sigilTrinket.CreatureToCastSpellFrom != null)
                {
                    Player.CreateSigilSpellProjectilesFromTarget(castSpell, sigilTrinket.CreatureToCastSpellFrom);
                }
                // Trigger spells affect all targets of fellowship boost spells
                else if (sigilTrinket.TriggerSpell is {IsFellowshipSpell: true} && Player.Fellowship?.TotalMembers > 1)
                {
                    foreach (var fellowshipMember in Player.Fellowship.GetFellowshipMembers())
                    {
                        Player.CreateSigilPlayerSpell(fellowshipMember.Value, castSpell, sigilTrinket.IsWeaponSpell);
                    }
                }
                else if (sigilTrinket.TriggerSpell is {IsSelfTargeted: true})
                {
                    Player.CreateSigilPlayerSpell(Player, castSpell, sigilTrinket.IsWeaponSpell);
                }
                else
                {
                    Player.CreateSigilPlayerSpell(sigilTrinket.SigilTrinketTarget, castSpell, sigilTrinket.IsWeaponSpell);
                }

            }
        );

        spellChain.EnqueueChain();
    }

    private uint? DetonateSpellId(Spell spell)
    {
        var element = spell.DamageType;

        switch (element)
        {
            case DamageType.Slash:
                return (uint)SpellId.BladeRing;
            case DamageType.Pierce:
                return (uint)SpellId.ForceRing;
            case DamageType.Bludgeon:
                return (uint)SpellId.ShockwaveRing;
            case DamageType.Acid:
                return (uint)SpellId.AcidRing;
            case DamageType.Fire:
                return (uint)SpellId.FlameRing;
            case DamageType.Cold:
                return (uint)SpellId.FrostRing;
            case DamageType.Electric:
                return (uint)SpellId.LightningRing;
        }

        _log.Error("Damage type not found for {spell}.", spell);

        return null;
    }

    private float AoeSpellIntensity(Spell spell)
    {
        switch (spell.Level)
        {
            default:
                return 0.2f;
            case 2:
                return 0.4f;
            case 3:
                return 0.6f;
            case 4:
                return 0.8f;
            case 5:
                return 1.0f;
            case 6:
                return 1.25f;
            case 7:
                return 1.5f;
        }
    }

    // --- VALIDATION CHECKS ---
    private bool SigilTrinketValidationChecks(SigilTrinket sigilTrinket)
    {
        if (sigilTrinket.SigilTrinketEffectId == null || sigilTrinket.WieldSkillType == null)
        {
            return false;
        }

        // Check if another trinket with the same effect is already in the process of activating
        if (_effectsUsed.Contains(SigilTrinket.GetEffectName((Skill)sigilTrinket.WieldSkillType, (int)sigilTrinket.SigilTrinketEffectId)))
        {
            return false;
        }

        // CHECK CONDITIONS OF EACH TRINKET TYPE
        switch ((Skill, EffectId))
        {
            case (Skill.LifeMagic, (int)SigilTrinketLifeMagicEffect.ScarabCastVuln):
                return IsValidForCastVuln(sigilTrinket);

            case (Skill.LifeMagic, (int)SigilTrinketLifeMagicEffect.ScarabCastProt):
                return IsValidForCastProt(sigilTrinket);

            case (Skill.LifeMagic, (int)SigilTrinketLifeMagicEffect.ScarabCastItemBuff):
                return IsValidForCastItemBuff(sigilTrinket);

            case (Skill.LifeMagic, (int)SigilTrinketLifeMagicEffect.ScarabCastVitalRate):
                return IsValidForCastVitalRate(sigilTrinket);

            case (Skill.LifeMagic, (int)SigilTrinketLifeMagicEffect.ScarabIntensity):
            case (Skill.WarMagic, (int)SigilTrinketWarMagicEffect.ScarabIntensity):
                return IsValidForIntensity(sigilTrinket);

            case (Skill.LifeMagic, (int)SigilTrinketLifeMagicEffect.ScarabShield):
            case (Skill.WarMagic, (int)SigilTrinketWarMagicEffect.ScarabShield):
                return IsValidForShield(sigilTrinket);

            case (Skill.LifeMagic, (int)SigilTrinketLifeMagicEffect.ScarabManaReduction):
            case (Skill.WarMagic, (int)SigilTrinketWarMagicEffect.ScarabManaReduction):
                return IsValidForReduction(sigilTrinket);

            case (Skill.WarMagic, (int)SigilTrinketWarMagicEffect.ScarabDuplicate):
                return IsValidForDuplicate(sigilTrinket);

            case (Skill.WarMagic, (int)SigilTrinketWarMagicEffect.ScarabDetonate):
                return IsValidForDetonate(sigilTrinket);

            case (Skill.WarMagic, (int)SigilTrinketWarMagicEffect.ScarabCrit):
                return IsValidForCrushing(sigilTrinket);

            case (Skill.TwoHandedCombat, (int)SigilTrinketTwohandedCombatEffect.Might):
            case (Skill.Shield, (int)SigilTrinketShieldEffect.Might):
                return IsValidForMight(sigilTrinket);

            case (Skill.TwoHandedCombat, (int)SigilTrinketTwohandedCombatEffect.Aggression):
            case (Skill.Shield, (int)SigilTrinketShieldEffect.Aggression):
                return IsValidForAggression(sigilTrinket);

            case (Skill.DualWield, (int)SigilTrinketDualWieldEffect.Assailment):
                return IsValidForAssailment(sigilTrinket);

            case (Skill.Thievery, (int)SigilTrinketThieveryEffect.Treachery):
                return IsValidForTreachery(sigilTrinket);

            case (Skill.PhysicalDefense, (int)SigilTrinketPhysicalDefenseEffect.Evasion):
                return IsValidForEvasion(sigilTrinket);

            case (Skill.MagicDefense, (int)SigilTrinketMagicDefenseEffect.Absorption):
                return IsValidForAbsorption(sigilTrinket);

            case (Skill.AssessPerson, (int)SigilTrinketPerceptionEffect.Exposure):
                return IsValidForExposure(sigilTrinket);

            case (Skill.Deception, (int)SigilTrinketDeceptionEffect.Avoidance):
                return IsValidForAvoidance(sigilTrinket);
        }

        return true;
    }
    private bool IsValidForCastVuln(SigilTrinket sigilTrinket)
    {
        var validSkill = sigilTrinket.WieldSkillType == (int)Skill.LifeMagic;
        var validEffectId = sigilTrinket.SigilTrinketEffectId == (int)SigilTrinketLifeMagicEffect.ScarabCastVuln;
        var validSpellCategory = SigilTrinket.LifeHarmfulTriggerSpells.Contains(TriggerSpell.Category);
        var validSpellLevel = sigilTrinket.SigilTrinketMaxTier >= TriggerSpell.Level;

        return validSkill && validEffectId && validSpellCategory && validSpellLevel && !OnCrit;
    }

    private bool IsValidForCastProt(SigilTrinket sigilTrinket)
    {
        var validSkill = sigilTrinket.WieldSkillType == (int)Skill.LifeMagic;
        var validEffectId = sigilTrinket.SigilTrinketEffectId == (int)SigilTrinketLifeMagicEffect.ScarabCastProt;
        var validSpellCategory = SigilTrinket.LifeBeneficialTriggerSpells.Contains(TriggerSpell.Category);
        var validSpellLevel = sigilTrinket.SigilTrinketMaxTier >= TriggerSpell.Level;

        return validSkill && validEffectId && validSpellCategory && validSpellLevel && !OnCrit;
    }

    private bool IsValidForCastItemBuff(SigilTrinket sigilTrinket)
    {
        var validSkill = sigilTrinket.WieldSkillType == (int)Skill.LifeMagic;
        var validEffectId = sigilTrinket.SigilTrinketEffectId == (int)SigilTrinketLifeMagicEffect.ScarabCastItemBuff;
        var validSpellCategory = SigilTrinket.LifeBeneficialTriggerSpells.Contains(TriggerSpell.Category);
        var validSpellLevel = sigilTrinket.SigilTrinketMaxTier >= TriggerSpell.Level;

        return validSkill && validEffectId && validSpellCategory && validSpellLevel && !OnCrit;
    }

    private bool IsValidForCastVitalRate(SigilTrinket sigilTrinket)
    {
        var validSkill = sigilTrinket.WieldSkillType == (int)Skill.LifeMagic;
        var validEffectId = sigilTrinket.SigilTrinketEffectId == (int)SigilTrinketLifeMagicEffect.ScarabCastVitalRate;
        var validSpellCategory = SigilTrinket.LifeBeneficialTriggerSpells.Contains(TriggerSpell.Category) || SigilTrinket.LifeHarmfulTriggerSpells.Contains(TriggerSpell.Category);
        var validSpellLevel = sigilTrinket.SigilTrinketMaxTier >= TriggerSpell.Level;

        return validSkill && validEffectId && validSpellCategory && validSpellLevel && !OnCrit;
    }

    private bool IsValidForIntensity(SigilTrinket sigilTrinket)
    {
        var validSkillEffectLife = sigilTrinket.WieldSkillType == (int)Skill.LifeMagic && sigilTrinket.SigilTrinketEffectId == (int)SigilTrinketLifeMagicEffect.ScarabIntensity;
        var validSkillEffectWar = sigilTrinket.WieldSkillType == (int)Skill.WarMagic && sigilTrinket.SigilTrinketEffectId == (int)SigilTrinketWarMagicEffect.ScarabIntensity;
        var validSkillEffect = validSkillEffectLife || validSkillEffectWar;
        var validSpellLevel = sigilTrinket.SigilTrinketMaxTier >= TriggerSpell.Level;

        return validSkillEffect && validSpellLevel && !OnCrit;
    }

    private bool IsValidForShield(SigilTrinket sigilTrinket)
    {
        var validSkillEffectLife = sigilTrinket.WieldSkillType == (int)Skill.LifeMagic && sigilTrinket.SigilTrinketEffectId == (int)SigilTrinketLifeMagicEffect.ScarabShield;
        var validSkillEffectWar = sigilTrinket.WieldSkillType == (int)Skill.WarMagic && sigilTrinket.SigilTrinketEffectId == (int)SigilTrinketWarMagicEffect.ScarabShield;
        var validSkillEffect = validSkillEffectLife || validSkillEffectWar;
        var validSpellLevel = sigilTrinket.SigilTrinketMaxTier >= TriggerSpell.Level;

        return validSkillEffect && validSpellLevel && !OnCrit;
    }

    private bool IsValidForReduction(SigilTrinket sigilTrinket)
    {
        var validSkillEffectLife = sigilTrinket.WieldSkillType == (int)Skill.LifeMagic && sigilTrinket.SigilTrinketEffectId == (int)SigilTrinketLifeMagicEffect.ScarabManaReduction;
        var validSkillEffectWar = sigilTrinket.WieldSkillType == (int)Skill.WarMagic && sigilTrinket.SigilTrinketEffectId == (int)SigilTrinketWarMagicEffect.ScarabManaReduction;
        var validSkillEffect = validSkillEffectLife || validSkillEffectWar;
        var validSpellLevel = sigilTrinket.SigilTrinketMaxTier >= TriggerSpell.Level;

        return validSkillEffect && validSpellLevel && !OnCrit;
    }

    private bool IsValidForDuplicate(SigilTrinket sigilTrinket)
    {
        var validSkill = sigilTrinket.WieldSkillType == (int)Skill.WarMagic;
        var validEffectId = sigilTrinket.SigilTrinketEffectId == (int)SigilTrinketWarMagicEffect.ScarabDuplicate;
        var validSpellCategory = SigilTrinket.WarProjectileTriggerCategories.Contains(TriggerSpell.Category);
        var validSpellLevel = sigilTrinket.SigilTrinketMaxTier >= TriggerSpell.Level;

        return validSkill && validEffectId && validSpellCategory && validSpellLevel && !OnCrit;
    }

    private bool IsValidForDetonate(SigilTrinket sigilTrinket)
    {
        var validSkill = sigilTrinket.WieldSkillType == (int)Skill.WarMagic;
        var validEffectId = sigilTrinket.SigilTrinketEffectId == (int)SigilTrinketWarMagicEffect.ScarabDetonate;
        var validSpellCategory = SigilTrinket.WarProjectileTriggerCategories.Contains(TriggerSpell.Category);
        var validSpellLevel = sigilTrinket.SigilTrinketMaxTier >= TriggerSpell.Level;

        return Target != null && validSkill && validEffectId && validSpellCategory && validSpellLevel && !OnCrit;
    }

    private bool IsValidForCrushing(SigilTrinket sigilTrinket)
    {
        var validSkill = sigilTrinket.WieldSkillType == (int)Skill.WarMagic;
        var validEffectId = sigilTrinket.SigilTrinketEffectId == (int)SigilTrinketWarMagicEffect.ScarabCrit;
        var validSpellCategory = SigilTrinket.WarProjectileTriggerCategories.Contains(TriggerSpell.Category);
        var validSpellLevel = sigilTrinket.SigilTrinketMaxTier >= TriggerSpell.Level;

        return validSkill && validEffectId && validSpellCategory && validSpellLevel && !OnCrit;
    }

    private bool IsValidForMight(SigilTrinket sigilTrinket)
    {
        if (Player.GetEquippedWeapon() == null)
        {
            return false;
        }

        var validSkillEffectTwohand = sigilTrinket.WieldSkillType == (int)Skill.TwoHandedCombat && sigilTrinket.SigilTrinketEffectId == (int)SigilTrinketTwohandedCombatEffect.Might;
        var validSkillEffectShield = sigilTrinket.WieldSkillType == (int)Skill.Shield && sigilTrinket.SigilTrinketEffectId == (int)SigilTrinketShieldEffect.Might;

        var sigilTrinketMaxtier = sigilTrinket.SigilTrinketMaxTier ?? 1;
        var maxWield = LootGenerationFactory.GetWieldDifficultyPerTier(sigilTrinketMaxtier + 1);

        var validWieldReqTwohand = false;
        if (DamageEvent.Weapon is {IsTwoHanded: true})
        {
            var weaponWieldReq = DamageEvent.Weapon.WieldDifficulty ?? 1;
            validWieldReqTwohand = weaponWieldReq <= maxWield;
        }

        var validWieldReqShield = false;
        if (DamageEvent.Offhand is { IsShield: true })
        {
            var shieldWieldReq = DamageEvent.Offhand.WieldDifficulty ?? 1;
            validWieldReqShield = shieldWieldReq <= maxWield;
        }

        var isPowerAttack = Player.GetPowerAccuracyBar() > 0.5f;

        return isPowerAttack && ((validSkillEffectTwohand && validWieldReqTwohand) || (validSkillEffectShield && validWieldReqShield));
    }

    private bool IsValidForAggression(SigilTrinket sigilTrinket)
    {
        if (Player.GetEquippedWeapon() == null)
        {
            return false;
        }

        var validSkillEffectTwohand = sigilTrinket.WieldSkillType == (int)Skill.TwoHandedCombat && sigilTrinket.SigilTrinketEffectId == (int)SigilTrinketTwohandedCombatEffect.Aggression;
        var validSkillEffectShield = sigilTrinket.WieldSkillType == (int)Skill.Shield && sigilTrinket.SigilTrinketEffectId == (int)SigilTrinketShieldEffect.Aggression;

        var sigilTrinketMaxtier = sigilTrinket.SigilTrinketMaxTier ?? 1;
        var maxWield = LootGenerationFactory.GetWieldDifficultyPerTier(sigilTrinketMaxtier + 1);

        var validWieldReqTwohand = false;
        if (DamageEvent.Weapon is {IsTwoHanded: true})
        {
            var weaponWieldReq = DamageEvent.Weapon.WieldDifficulty ?? 1;
            validWieldReqTwohand = weaponWieldReq <= maxWield;
        }

        var validWieldReqShield = false;
        if (DamageEvent.Offhand is { IsShield: true })
        {
            var shieldWieldReq = DamageEvent.Offhand.WieldDifficulty ?? 1;
            validWieldReqShield = shieldWieldReq <= maxWield;
        }

        var isPowerAttack = Player.GetPowerAccuracyBar() > 0.5f;

        return isPowerAttack && ((validSkillEffectTwohand && validWieldReqTwohand) || (validSkillEffectShield && validWieldReqShield));
    }

    private bool IsValidForAssailment(SigilTrinket sigilTrinket)
    {
        if (Player.GetEquippedWeapon() == null)
        {
            return false;
        }

        var validSkill = sigilTrinket.WieldSkillType == (int)Skill.DualWield;
        var validEffectId = sigilTrinket.SigilTrinketEffectId == (int)SigilTrinketDualWieldEffect.Assailment;

        var sigilTrinketMaxtier = sigilTrinket.SigilTrinketMaxTier ?? 1;
        var maxWield = LootGenerationFactory.GetWieldDifficultyPerTier(sigilTrinketMaxtier + 1);

        var weaponWieldReq = DamageEvent.Weapon.WieldDifficulty ?? 1;
        var validWieldReq = weaponWieldReq <= maxWield;

        var isDualWieldAttack = Player is { IsDualWieldAttack: true };

        return validSkill && validEffectId && validWieldReq && isDualWieldAttack && OnCrit;
    }

    private bool IsValidForTreachery(SigilTrinket sigilTrinket)
    {
        if (Player.GetEquippedWeapon() == null)
        {
            return false;
        }

        var validSkill = sigilTrinket.WieldSkillType == (int)Skill.Thievery;
        var validEffectId = sigilTrinket.SigilTrinketEffectId == (int)SigilTrinketThieveryEffect.Treachery;

        var sigilTrinketMaxtier = sigilTrinket.SigilTrinketMaxTier ?? 1;
        var maxWield = LootGenerationFactory.GetWieldDifficultyPerTier(sigilTrinketMaxtier + 1);

        var weaponWieldReq = DamageEvent.Weapon.WieldDifficulty ?? 1;
        var validWieldReq = weaponWieldReq <= maxWield;

        var isSneakAttack = false;
        if (Target is Creature creatureTarget)
        {
            var angle = creatureTarget.GetAngle(Player);
            isSneakAttack = Math.Abs(angle) > 90.0f;
        }

        return validSkill && validEffectId && validWieldReq && isSneakAttack && OnCrit;
    }

    private bool IsValidForEvasion(SigilTrinket sigilTrinket)
    {
        if (Player.GetEquippedWeapon() == null)
        {
            return false;
        }

        var validSkill = sigilTrinket.WieldSkillType == (int)Skill.PhysicalDefense;
        var validEffectId = sigilTrinket.SigilTrinketEffectId == (int)SigilTrinketPhysicalDefenseEffect.Evasion;

        var sigilTrinketMaxtier = sigilTrinket.SigilTrinketMaxTier ?? 1;
        var maxWield = LootGenerationFactory.GetWieldDifficultyPerTier(sigilTrinketMaxtier + 1);

        var weaponWieldReq = DamageEvent.DefenderWeapon.WieldDifficulty ?? 1;
        var validWieldReq = weaponWieldReq <= maxWield;

        var isGlancingBlow = DamageEvent.PartialEvasion == PartialEvasion.Some;

        return validSkill && validEffectId && validWieldReq && isGlancingBlow;
    }

    private bool IsValidForAbsorption(SigilTrinket sigilTrinket)
    {
        if (Player.GetEquippedWeapon() == null)
        {
            return false;
        }

        var validSkill = sigilTrinket.WieldSkillType == (int)Skill.MagicDefense;
        var validEffectId = sigilTrinket.SigilTrinketEffectId == (int)SigilTrinketMagicDefenseEffect.Absorption;

        var sigilTrinketMaxtier = sigilTrinket.SigilTrinketMaxTier ?? 1;
        var maxWield = LootGenerationFactory.GetWieldDifficultyPerTier(sigilTrinketMaxtier + 1);

        var weaponWieldReq = Player.GetEquippedWeapon().WieldDifficulty ?? 1;
        var validWieldReq = weaponWieldReq <= maxWield;

        var isHealthDamageSpell = TriggerSpell.VitalDamageType is not DamageType.Stamina and not DamageType.Mana;

        return validSkill && validEffectId && validWieldReq && isHealthDamageSpell;
    }

    private bool IsValidForExposure(SigilTrinket sigilTrinket)
    {
        if (Player.GetEquippedWeapon() == null)
        {
            return false;
        }

        var validSkill = sigilTrinket.WieldSkillType == (int)Skill.AssessPerson;
        var validEffectId = sigilTrinket.SigilTrinketEffectId == (int)SigilTrinketPerceptionEffect.Exposure;

        var sigilTrinketMaxtier = sigilTrinket.SigilTrinketMaxTier ?? 1;
        var maxWield = LootGenerationFactory.GetWieldDifficultyPerTier(sigilTrinketMaxtier + 1);

        var weaponWieldReq = DamageEvent.Weapon.WieldDifficulty ?? 1;
        var validWieldReq = weaponWieldReq <= maxWield;

        return validSkill && validEffectId && validWieldReq;
    }

    private bool IsValidForAvoidance(SigilTrinket sigilTrinket)
    {
        if (Player.GetEquippedWeapon() == null)
        {
            return false;
        }

        var validSkill = sigilTrinket.WieldSkillType == (int)Skill.Deception;
        var validEffectId = sigilTrinket.SigilTrinketEffectId == (int)SigilTrinketDeceptionEffect.Avoidance;

        var sigilTrinketMaxtier = sigilTrinket.SigilTrinketMaxTier ?? 1;
        var maxWield = LootGenerationFactory.GetWieldDifficultyPerTier(sigilTrinketMaxtier + 1);

        var weaponWieldReq = DamageEvent.Weapon.WieldDifficulty ?? 1;
        var validWieldReq = weaponWieldReq <= maxWield;

        var isPowerAttack = Player.GetPowerAccuracyBar() > 0.5f;

        return validSkill && validEffectId && validWieldReq && isPowerAttack;
    }

    //  --- COOLDOWN CHECKS ---
    private bool SigilTrinketCooldownChecks(SigilTrinket sigilTrinket)
    {
        if (sigilTrinket.NextTrinketTriggerTime > Time.GetUnixTime())
        {
            return false;
        }

        switch (sigilTrinket.SigilTrinketColor)
        {
            case (int)SigilTrinketColor.Blue:
                if (Player.NextTriggerTimeBlue > Time.GetUnixTime())
                {
                    return false;
                }
                break;
            case (int)SigilTrinketColor.Yellow:
                if (Player.NextTriggerTimeYellow > Time.GetUnixTime())
                {
                    return false;
                }
                break;
            case (int)SigilTrinketColor.Red:
                if (Player.NextTriggerTimeRed > Time.GetUnixTime())
                {
                    return false;
                }
                break;
        }

        return true;
    }
}

