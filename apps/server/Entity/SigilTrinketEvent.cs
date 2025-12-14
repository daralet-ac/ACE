using System;
using System.Collections.Generic;
using System.Linq;
using ACE.Common;
using ACE.Entity.Enum;
using ACE.Server.Entity.Actions;
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
    private List<string> _effectsUsed = new List<string>();
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
        if (sigilTrinket.Structure < 1)
        {
            return false;
        }

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
        if (sigilTrinket.SigilTrinketEffectId != null && sigilTrinket.AllowedSpecializedSkills != null && sigilTrinket.AllowedSpecializedSkills.Count > 0)
        {
            foreach (var allowedSkill in sigilTrinket.AllowedSpecializedSkills)
            {
                var effectName = SigilTrinket.GetEffectName(allowedSkill, (int)sigilTrinket.SigilTrinketEffectId);
                if (!_effectsUsed.Contains(effectName))
                {
                    _effectsUsed.Add(effectName);
                }
            }
        }
        else if (sigilTrinket.SigilTrinketEffectId != null && sigilTrinket.WieldSkillType != null)
        {
            var effectName = SigilTrinket.GetEffectName((Skill)sigilTrinket.WieldSkillType, (int)sigilTrinket.SigilTrinketEffectId);
            if (!_effectsUsed.Contains(effectName))
            {
                _effectsUsed.Add(effectName);
            }
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

        var skillsToCheck = GetAllowedSkills(sigilTrinket).ToList();
        var handled = false;

        if (skillsToCheck.Count == 0)
        {
            _log.Error("StartSigilTrinketEffect({SigilTrinket}) - No allowed skills found.", sigilTrinket.Name);
            return;
        }

        var multiSkill = skillsToCheck.Count > 1;
        Console.WriteLine($"{sigilTrinket.Name} {multiSkill} {skillsToCheck[0]} {sigilTrinket.SigilTrinketEffectId}");
        foreach (var skill in skillsToCheck)
        {
            switch ((multiSkill, skill, sigilTrinket.SigilTrinketEffectId))
            {
                case (false, Skill.LifeMagic, (int)SigilTrinketLifeMagicEffect.CastVuln):
                case (false, Skill.LifeMagic, (int)SigilTrinketLifeMagicEffect.CastProt):
                case (false, Skill.LifeMagic, (int)SigilTrinketLifeMagicEffect.CastItemBuff):
                case (false, Skill.LifeMagic, (int)SigilTrinketLifeMagicEffect.CastVitalRate):
                    CastSigilTrinketSpell(sigilTrinket, true);
                    handled = true;
                    break;

                case (false, Skill.WarMagic, (int)SigilTrinketWarMagicEffect.Duplicate):
                    sigilTrinket.SigilTrinketCastSpellId = sigilTrinket.TriggerSpell.Id;
                    CastSigilTrinketSpell(sigilTrinket);
                    handled = true;
                    break;

                case (false, Skill.WarMagic, (int)SigilTrinketWarMagicEffect.Detonate):
                    sigilTrinket.SigilTrinketCastSpellId = DetonateSpellId(sigilTrinket.TriggerSpell);
                    sigilTrinket.SpellIntensityMultiplier = AoeSpellIntensity(sigilTrinket.TriggerSpell);
                    sigilTrinket.CreatureToCastSpellFrom = CreatureToCastSpellFrom;

                    if (sigilTrinket.SigilTrinketCastSpellId != null)
                    {
                        CastSigilTrinketSpell(sigilTrinket, false);
                    }
                    handled = true;
                    break;

                case (false, Skill.WarMagic, (int)SigilTrinketWarMagicEffect.Crushing):
                    CastSigilTrinketSpell(sigilTrinket, true);
                    handled = true;
                    break;

                case (true, Skill.LifeMagic, (int)SigilTrinketLifeWarMagicEffect.Intensity):
                case (true, Skill.WarMagic, (int)SigilTrinketLifeWarMagicEffect.Intensity):
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
                    handled = true;
                    break;

                case (true, Skill.LifeMagic, (int)SigilTrinketLifeWarMagicEffect.Shielding):
                case (true, Skill.WarMagic, (int)SigilTrinketLifeWarMagicEffect.Shielding):
                    CastSigilTrinketSpell(sigilTrinket, true);
                    handled = true;
                    break;

                case (true, Skill.TwoHandedCombat, (int)SigilTrinketShieldTwohandedCombatEffect.Might):
                case (true, Skill.Shield, (int)SigilTrinketShieldTwohandedCombatEffect.Might):
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
                    handled = true;
                    break;

                case (true, Skill.TwoHandedCombat, (int)SigilTrinketShieldTwohandedCombatEffect.Aggression):
                case (true, Skill.Shield, (int)SigilTrinketShieldTwohandedCombatEffect.Aggression):
                    if (Target is Creature creatureTargetAgg)
                    {
                        creatureTargetAgg.DoubleThreatFromNextAttackTargets.Add(Player);

                        Player.Session.Network.EnqueueSend(
                            new GameMessageSystemChat(
                                $"Sigil Compass of Aggression doubled the amount of threat your attack produced against your target!",
                                ChatMessageType.CombatSelf
                            )
                        );
                    }
                    handled = true;
                    break;

                case (false, Skill.DualWield, (int)SigilTrinketDualWieldMissileEffect.Assailment):
                    CastSigilTrinketSpell(sigilTrinket, false);

                    Player.Session.Network.EnqueueSend(
                        new GameMessageSystemChat(
                            $"Sigil Puzzle Box of Assailment boosts your critical damage for the next 10 seconds!",
                            ChatMessageType.CombatSelf
                        )
                    );
                    handled = true;
                    break;

                case (false, Skill.Thievery, (int)SigilTrinketThieveryEffect.Treachery):
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
                    handled = true;
                    break;

                case (false, Skill.PhysicalDefense, (int)SigilTrinketPhysicalDefenseEffect.Evasion):
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
                    handled = true;
                    break;

                case (false, Skill.MagicDefense, (int)SigilTrinketMagicDefenseEffect.Absorption):
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
                    handled = true;
                    break;

                case (false, Skill.Perception, (int)SigilTrinketPerceptionEffect.Exposure):
                    CastSigilTrinketSpell(sigilTrinket, false);

                    Player.Session.Network.EnqueueSend(
                        new GameMessageSystemChat(
                            $"Sigil Goggles of Exposure boosts your damage for the next 10 seconds!",
                            ChatMessageType.CombatSelf
                        )
                    );
                    handled = true;
                    break;

                case (false, Skill.Deception, (int)SigilTrinketDeceptionEffect.Avoidance):
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
                    handled = true;
                    break;
            }

            if (handled)
            {
                break;
            }
        }
    }

    /// <summary>
    /// Prepare a spell to be cast, using the CastSpelId on the sigil trinket.
    /// </summary>
    private void CastSigilTrinketSpell(SigilTrinket sigilTrinket, bool useProgression = true)
    {
        var castSpellLevel1IdsToCastSet = new HashSet<SpellId>();

        if (sigilTrinket.TriggerSpell is not null)
        {
            sigilTrinket.SpellLevel = sigilTrinket.TriggerSpell.Level;
        }

        var skillsToCheck = GetAllowedSkills(sigilTrinket).ToList();

        if (skillsToCheck.Count == 0)
        {
            _log.Error("CastSigilTrinketSpell({SigilTrinket}) - No allowed skills found.", sigilTrinket.Name);
            return;
        }

        var multiSkill = skillsToCheck.Count > 1;
        var firstSkill = skillsToCheck[0];
        Console.WriteLine($"{firstSkill} {sigilTrinket.SigilTrinketEffectId}");
        switch ((multiSkill, firstSkill, sigilTrinket.SigilTrinketEffectId))
        {
            case (false, Skill.LifeMagic, (int)SigilTrinketLifeMagicEffect.CastVitalRate):
                switch (sigilTrinket.TriggerSpell.Category)
                {
                    case SpellCategory.HealthRaising:
                    case SpellCategory.HealingRaising:
                        castSpellLevel1IdsToCastSet.Add(SpellId.RegenerationOther1);
                        break;
                    case SpellCategory.StaminaRaising:
                        castSpellLevel1IdsToCastSet.Add(SpellId.RejuvenationOther1);
                        break;
                    case SpellCategory.ManaRaising:
                        castSpellLevel1IdsToCastSet.Add(SpellId.ManaRenewalOther1);
                        break;
                    case SpellCategory.HealthLowering:
                        castSpellLevel1IdsToCastSet.Add(SpellId.FesterOther1);
                        break;
                    case SpellCategory.StaminaLowering:
                        castSpellLevel1IdsToCastSet.Add(SpellId.ExhaustionOther1);
                        break;
                    case SpellCategory.ManaLowering:
                        castSpellLevel1IdsToCastSet.Add(SpellId.ManaDepletionOther1);
                        break;
                    default:
                        break;
                }
                break;

            case (false, Skill.LifeMagic, (int)SigilTrinketLifeMagicEffect.CastItemBuff):
                switch (sigilTrinket.TriggerSpell.Category)
                {
                    case SpellCategory.HealthRaising:
                    case SpellCategory.HealingRaising:
                        castSpellLevel1IdsToCastSet.Add(SpellId.DefenderOther1);
                        break;
                    case SpellCategory.StaminaRaising:
                        castSpellLevel1IdsToCastSet.Add(SpellId.BloodDrinkerOther1);
                        break;
                    case SpellCategory.ManaRaising:
                        castSpellLevel1IdsToCastSet.Add(SpellId.SpiritDrinkerOther1);
                        break;
                    default:
                        break;
                }
                break;

            case (false, Skill.LifeMagic, (int)SigilTrinketLifeMagicEffect.CastProt):
                if (Target is not Creature targetCreature)
                {
                    break;
                }

                var recentDamageTakenTypes = targetCreature.RecentDamageTypesTaken;

                if (recentDamageTakenTypes.Count == 0)
                {
                    break;
                }

                foreach (var damageTakenType in recentDamageTakenTypes)
                {
                    switch (damageTakenType)
                    {
                        case DamageType.Slash:
                            castSpellLevel1IdsToCastSet.Add(SpellId.BladeProtectionOther1);
                            break;
                        case DamageType.Pierce:
                            castSpellLevel1IdsToCastSet.Add(SpellId.PiercingProtectionOther1);
                            break;
                        case DamageType.Bludgeon:
                            castSpellLevel1IdsToCastSet.Add(SpellId.BludgeonProtectionOther1);
                            break;
                        case DamageType.Acid:
                            castSpellLevel1IdsToCastSet.Add(SpellId.AcidProtectionOther1);
                            break;
                        case DamageType.Fire:
                            castSpellLevel1IdsToCastSet.Add(SpellId.FireProtectionOther1);
                            break;
                        case DamageType.Cold:
                            castSpellLevel1IdsToCastSet.Add(SpellId.ColdProtectionOther1);
                            break;
                        case DamageType.Electric:
                            castSpellLevel1IdsToCastSet.Add(SpellId.LightningProtectionOther1);
                            break;
                        default:
                            continue;
                    }
                }
                break;

            case (false, Skill.LifeMagic, (int)SigilTrinketLifeMagicEffect.CastVuln):
                if (Target is not Creature targetCreatureVuln)
                {
                    break;
                }

                var targetWeakestResistances = targetCreatureVuln.WeakestResistances;

                if (targetWeakestResistances.Count == 0)
                {
                    break;
                }

                foreach (var elementalWeakness in targetWeakestResistances)
                {
                    switch (elementalWeakness)
                    {
                        case DamageType.Slash:
                            castSpellLevel1IdsToCastSet.Add(SpellId.BladeVulnerabilityOther1);
                            break;
                        case DamageType.Pierce:
                            castSpellLevel1IdsToCastSet.Add(SpellId.PiercingVulnerabilityOther1);
                            break;
                        case DamageType.Bludgeon:
                            castSpellLevel1IdsToCastSet.Add(SpellId.BludgeonVulnerabilityOther1);
                            break;
                        case DamageType.Acid:
                            castSpellLevel1IdsToCastSet.Add(SpellId.AcidVulnerabilityOther1);
                            break;
                        case DamageType.Fire:
                            castSpellLevel1IdsToCastSet.Add(SpellId.FireVulnerabilityOther1);
                            break;
                        case DamageType.Cold:
                            castSpellLevel1IdsToCastSet.Add(SpellId.ColdVulnerabilityOther1);
                            break;
                        case DamageType.Electric:
                            castSpellLevel1IdsToCastSet.Add(SpellId.LightningVulnerabilityOther1);
                            break;
                        default:
                            continue;
                    }
                }
                break;

            case (false, Skill.WarMagic, (int)SigilTrinketWarMagicEffect.Crushing):
            case (false, Skill.Perception, (int)SigilTrinketPerceptionEffect.Exposure):
                castSpellLevel1IdsToCastSet.Add(SpellId.SigilTrinketCriticalDamageBoost);
                Console.WriteLine("crush");
                break;

            case (true, Skill.LifeMagic, (int)SigilTrinketLifeWarMagicEffect.Shielding):
            case (true, Skill.WarMagic, (int)SigilTrinketLifeWarMagicEffect.Shielding):
                castSpellLevel1IdsToCastSet.Add(SpellId.SigilTrinketShield1);
                break;

            case (true, Skill.DualWield, (int)SigilTrinketDualWieldMissileEffect.Assailment):
            case (true, Skill.Bow, (int)SigilTrinketDualWieldMissileEffect.Assailment):
                castSpellLevel1IdsToCastSet.Add(SpellId.SigilTrinketCriticalDamageBoost);
                break;

            default:
                if (sigilTrinket.SigilTrinketCastSpellId != null)
                {
                    castSpellLevel1IdsToCastSet.Add((SpellId)sigilTrinket.SigilTrinketCastSpellId);
                }
                break;
        }
        

        var castSpellLevel1IdsToCast = castSpellLevel1IdsToCastSet.ToList();
        if (castSpellLevel1IdsToCast.Count == 0)
        {
            return;
        }

        foreach (var castSpellLevel1Id in castSpellLevel1IdsToCast)
        {
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

            // Determine delay by checking allowed skills; default 0.5
            var delay = 0.5;
            foreach (var skill in skillsToCheck)
            {
                if (skill == Skill.LifeMagic && sigilTrinket.SigilTrinketEffectId == (int)SigilTrinketLifeMagicEffect.CastVitalRate)
                {
                    delay = 1.0;
                    break;
                }
                if (skill == Skill.WarMagic && sigilTrinket.SigilTrinketEffectId == (int)SigilTrinketWarMagicEffect.Detonate)
                {
                    if (sigilTrinket.CreatureToCastSpellFrom != null)
                    {
                        delay = 0.0;
                    }
                    break;
                }
            }

            CreateSigilTrinketSpell(sigilTrinket, castSpellId, delay);
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
        if (sigilTrinket.SigilTrinketEffectId is null || sigilTrinket.SigilTrinketEffectId != EffectId)
        {
            return false;
        }

        // Validate that the triggering Skill is allowed by the trinket (supports AllowedSpecializedSkills list)
        var allowedSkills = GetAllowedSkills(sigilTrinket).ToList();
        var skillMatches = allowedSkills.Count > 0 && allowedSkills.Contains(Skill);

        if (!skillMatches)
        {
            return false;
        }

        // Check if another trinket with the same effect is already in the process of activating
        foreach (var skl in allowedSkills)
        {
            var effectName = SigilTrinket.GetEffectName(skl, (int)sigilTrinket.SigilTrinketEffectId);
            if (_effectsUsed.Contains(effectName))
            {
                return false;
            }
        }

        var multiSkill = allowedSkills.Count > 1;

        // CHECK CONDITIONS OF EACH TRINKET TYPE
        switch ((multiSkill, Skill, EffectId))
        {
            case (false, Skill.LifeMagic, (int)SigilTrinketLifeMagicEffect.CastVuln):
                return IsValidForCastVuln(sigilTrinket);

            case (false, Skill.LifeMagic, (int)SigilTrinketLifeMagicEffect.CastProt):
                return IsValidForCastProt(sigilTrinket);

            case (false, Skill.LifeMagic, (int)SigilTrinketLifeMagicEffect.CastItemBuff):
                return IsValidForCastItemBuff(sigilTrinket);

            case (false, Skill.LifeMagic, (int)SigilTrinketLifeMagicEffect.CastVitalRate):
                return IsValidForCastVitalRate(sigilTrinket);

            case (true, Skill.LifeMagic, (int)SigilTrinketLifeWarMagicEffect.Intensity):
            case (true, Skill.WarMagic, (int)SigilTrinketLifeWarMagicEffect.Intensity):
                return IsValidForIntensity(sigilTrinket);

            case (true, Skill.LifeMagic, (int)SigilTrinketLifeWarMagicEffect.Shielding):
            case (true, Skill.WarMagic, (int)SigilTrinketLifeWarMagicEffect.Shielding):
                return IsValidForShield(sigilTrinket);

            case (true, Skill.LifeMagic, (int)SigilTrinketLifeWarMagicEffect.Reduction):
            case (true, Skill.WarMagic, (int)SigilTrinketLifeWarMagicEffect.Reduction):
                return IsValidForReduction(sigilTrinket);

            case (false, Skill.WarMagic, (int)SigilTrinketWarMagicEffect.Duplicate):
                return IsValidForDuplicate(sigilTrinket);

            case (false, Skill.WarMagic, (int)SigilTrinketWarMagicEffect.Detonate):
                return IsValidForDetonate(sigilTrinket);

            case (false, Skill.WarMagic, (int)SigilTrinketWarMagicEffect.Crushing):
                return IsValidForCrushing(sigilTrinket);

            case (true, Skill.TwoHandedCombat, (int)SigilTrinketShieldTwohandedCombatEffect.Might):
            case (true, Skill.Shield, (int)SigilTrinketShieldTwohandedCombatEffect.Might):
                return IsValidForMight(sigilTrinket);

            case (true, Skill.TwoHandedCombat, (int)SigilTrinketShieldTwohandedCombatEffect.Aggression):
            case (true, Skill.Shield, (int)SigilTrinketShieldTwohandedCombatEffect.Aggression):
                return IsValidForAggression(sigilTrinket);

            case (true, Skill.DualWield, (int)SigilTrinketDualWieldMissileEffect.Assailment):
                return IsValidForAssailment(sigilTrinket);

            case (false, Skill.Thievery, (int)SigilTrinketThieveryEffect.Treachery):
                return IsValidForTreachery(sigilTrinket);

            case (false, Skill.PhysicalDefense, (int)SigilTrinketPhysicalDefenseEffect.Evasion):
                return IsValidForEvasion(sigilTrinket);

            case (false, Skill.MagicDefense, (int)SigilTrinketMagicDefenseEffect.Absorption):
                return IsValidForAbsorption(sigilTrinket);

            case (false, Skill.AssessPerson, (int)SigilTrinketPerceptionEffect.Exposure):
                return IsValidForExposure(sigilTrinket);

            case (false, Skill.Deception, (int)SigilTrinketDeceptionEffect.Avoidance):
                return IsValidForAvoidance(sigilTrinket);
        }

        return true;
    }
    private bool IsValidForCastVuln(SigilTrinket sigilTrinket)
    {
        var validSkill = GetAllowedSkills(sigilTrinket).Contains(Skill.LifeMagic);
        var validEffectId = sigilTrinket.SigilTrinketEffectId == (int)SigilTrinketLifeMagicEffect.CastVuln;
        var validSpellCategory = SigilTrinket.LifeHarmfulTriggerSpells.Contains(TriggerSpell.Category);

        return validSkill && validEffectId && validSpellCategory && !OnCrit;
    }

    private bool IsValidForCastProt(SigilTrinket sigilTrinket)
    {
        var validSkill = GetAllowedSkills(sigilTrinket).Contains(Skill.LifeMagic);
        var validEffectId = sigilTrinket.SigilTrinketEffectId == (int)SigilTrinketLifeMagicEffect.CastProt;
        var validSpellCategory = SigilTrinket.LifeBeneficialTriggerSpells.Contains(TriggerSpell.Category);

        return validSkill && validEffectId && validSpellCategory && !OnCrit;
    }

    private bool IsValidForCastItemBuff(SigilTrinket sigilTrinket)
    {
        var validSkill = GetAllowedSkills(sigilTrinket).Contains(Skill.LifeMagic);
        var validEffectId = sigilTrinket.SigilTrinketEffectId == (int)SigilTrinketLifeMagicEffect.CastItemBuff;
        var validSpellCategory = SigilTrinket.LifeBeneficialTriggerSpells.Contains(TriggerSpell.Category);

        return validSkill && validEffectId && validSpellCategory && !OnCrit;
    }

    private bool IsValidForCastVitalRate(SigilTrinket sigilTrinket)
    {
        var validSkill = GetAllowedSkills(sigilTrinket).Contains(Skill.LifeMagic);
        var validEffectId = sigilTrinket.SigilTrinketEffectId == (int)SigilTrinketLifeMagicEffect.CastVitalRate;
        var validSpellCategory = SigilTrinket.LifeBeneficialTriggerSpells.Contains(TriggerSpell.Category) || SigilTrinket.LifeHarmfulTriggerSpells.Contains(TriggerSpell.Category);

        return validSkill && validEffectId && validSpellCategory && !OnCrit;
    }

    private bool IsValidForIntensity(SigilTrinket sigilTrinket)
    {
        var validSkillLifeWar = GetAllowedSkills(sigilTrinket).Contains(Skill.LifeMagic) && GetAllowedSkills(sigilTrinket).Contains(Skill.WarMagic);

        var validEffectId = sigilTrinket.SigilTrinketEffectId == (int)SigilTrinketLifeWarMagicEffect.Intensity;

        return validSkillLifeWar && validEffectId && !OnCrit;
    }

    private bool IsValidForShield(SigilTrinket sigilTrinket)
    {
        var validSkillLifeWar = GetAllowedSkills(sigilTrinket).Contains(Skill.LifeMagic) && GetAllowedSkills(sigilTrinket).Contains(Skill.WarMagic);

        var validEffectId = sigilTrinket.SigilTrinketEffectId == (int)SigilTrinketLifeWarMagicEffect.Shielding;

        return validSkillLifeWar && validEffectId && !OnCrit;
    }

    private bool IsValidForReduction(SigilTrinket sigilTrinket)
    {
        var validSkillLifeWar = GetAllowedSkills(sigilTrinket).Contains(Skill.LifeMagic) && GetAllowedSkills(sigilTrinket).Contains(Skill.WarMagic);

        var validEffectId = sigilTrinket.SigilTrinketEffectId == (int)SigilTrinketLifeWarMagicEffect.Reduction;

        return validSkillLifeWar && validEffectId && !OnCrit;
    }

    private bool IsValidForDuplicate(SigilTrinket sigilTrinket)
    {
        var validSkill = GetAllowedSkills(sigilTrinket).Contains(Skill.WarMagic);
        var validEffectId = sigilTrinket.SigilTrinketEffectId == (int)SigilTrinketWarMagicEffect.Duplicate;
        var validSpellCategory = SigilTrinket.WarProjectileTriggerCategories.Contains(TriggerSpell.Category);

        return validSkill && validEffectId && validSpellCategory && !OnCrit;
    }

    private bool IsValidForDetonate(SigilTrinket sigilTrinket)
    {
        var validSkill = GetAllowedSkills(sigilTrinket).Contains(Skill.WarMagic);
        var validEffectId = sigilTrinket.SigilTrinketEffectId == (int)SigilTrinketWarMagicEffect.Detonate;
        var validSpellCategory = SigilTrinket.WarProjectileTriggerCategories.Contains(TriggerSpell.Category);

        return Target != null && validSkill && validEffectId && validSpellCategory && !OnCrit;
    }

    private bool IsValidForCrushing(SigilTrinket sigilTrinket)
    {
        var validSkill = GetAllowedSkills(sigilTrinket).Contains(Skill.WarMagic);
        var validEffectId = sigilTrinket.SigilTrinketEffectId == (int)SigilTrinketWarMagicEffect.Crushing;
        var validSpellCategory = SigilTrinket.WarProjectileTriggerCategories.Contains(TriggerSpell.Category);

        return validSkill && validEffectId && validSpellCategory && !OnCrit;
    }

    private bool IsValidForMight(SigilTrinket sigilTrinket)
    {
        if (Player.GetEquippedWeapon() == null)
        {
            return false;
        }

        var validSkillShieldTwohand = GetAllowedSkills(sigilTrinket).Contains(Skill.Shield) && GetAllowedSkills(sigilTrinket).Contains(Skill.TwoHandedCombat);

        var validEffectId = sigilTrinket.SigilTrinketEffectId == (int)SigilTrinketShieldTwohandedCombatEffect.Might;

        var isPowerAttack = Player.GetPowerAccuracyBar() > 0.5f;

        // Ensure the skill/weapon type matches an attack type
        var twohandAttackApplicable = DamageEvent.Weapon is { IsTwoHanded: true };
        var shieldAttackApplicable = DamageEvent.Offhand is { IsShield: true };

        return isPowerAttack && validSkillShieldTwohand && validEffectId && (twohandAttackApplicable || shieldAttackApplicable);
    }

    private bool IsValidForAggression(SigilTrinket sigilTrinket)
    {
        if (Player.GetEquippedWeapon() == null)
        {
            return false;
        }

        var validSkillShieldTwohand = GetAllowedSkills(sigilTrinket).Contains(Skill.Shield) && GetAllowedSkills(sigilTrinket).Contains(Skill.TwoHandedCombat);

        var validEffectId = sigilTrinket.SigilTrinketEffectId == (int)SigilTrinketShieldTwohandedCombatEffect.Aggression;

        var isPowerAttack = Player.GetPowerAccuracyBar() > 0.5f;

        // Ensure the skill/weapon type matches an attack type
        var twohandAttackApplicable = DamageEvent.Weapon is { IsTwoHanded: true };
        var shieldAttackApplicable = DamageEvent.Offhand is { IsShield: true };

        return isPowerAttack && validSkillShieldTwohand && validEffectId && (twohandAttackApplicable || shieldAttackApplicable);
    }

    private bool IsValidForAssailment(SigilTrinket sigilTrinket)
    {
        if (Player.GetEquippedWeapon() == null)
        {
            return false;
        }

        var validSkillDualWieldMissile = GetAllowedSkills(sigilTrinket).Contains(Skill.DualWield) && GetAllowedSkills(sigilTrinket).Contains(Skill.Bow);
        var validEffectId = sigilTrinket.SigilTrinketEffectId == (int)SigilTrinketDualWieldMissileEffect.Assailment;

        if (DamageEvent.Weapon is null)
        {
            return false;
        }

        var isDualWieldAttack = Player is { IsDualWieldAttack: true };

        return validSkillDualWieldMissile && validEffectId && isDualWieldAttack && OnCrit;
    }

    private bool IsValidForTreachery(SigilTrinket sigilTrinket)
    {
        if (Player.GetEquippedWeapon() == null)
        {
            return false;
        }

        var validSkill = GetAllowedSkills(sigilTrinket).Contains(Skill.Thievery);
        var validEffectId = sigilTrinket.SigilTrinketEffectId == (int)SigilTrinketThieveryEffect.Treachery;

        if (DamageEvent.Weapon is null)
        {
            return false;
        }

        var isSneakAttack = false;
        if (Target is Creature creatureTarget)
        {
            var angle = creatureTarget.GetAngle(Player);
            isSneakAttack = Math.Abs(angle) > 90.0f;
        }

        return validSkill && validEffectId && isSneakAttack && OnCrit;
    }

    private bool IsValidForEvasion(SigilTrinket sigilTrinket)
    {
        if (Player.GetEquippedWeapon() == null)
        {
            return false;
        }

        var validSkill = GetAllowedSkills(sigilTrinket).Contains(Skill.PhysicalDefense);
        var validEffectId = sigilTrinket.SigilTrinketEffectId == (int)SigilTrinketPhysicalDefenseEffect.Evasion;

        if (DamageEvent.DefenderWeapon is null)
        {
            return false;
        }

        var isGlancingBlow = DamageEvent.PartialEvasion == PartialEvasion.Some;

        return validSkill && validEffectId && isGlancingBlow;
    }

    private bool IsValidForAbsorption(SigilTrinket sigilTrinket)
    {
        if (Player.GetEquippedWeapon() == null)
        {
            return false;
        }

        var validSkill = GetAllowedSkills(sigilTrinket).Contains(Skill.MagicDefense);
        var validEffectId = sigilTrinket.SigilTrinketEffectId == (int)SigilTrinketMagicDefenseEffect.Absorption;

        if (Player.GetEquippedWeapon() is null)
        {
            return false;
        }

        var isHealthDamageSpell = TriggerSpell.VitalDamageType is not DamageType.Stamina and not DamageType.Mana;

        return validSkill && validEffectId && isHealthDamageSpell;
    }

    private bool IsValidForExposure(SigilTrinket sigilTrinket)
    {
        if (Player.GetEquippedWeapon() == null)
        {
            return false;
        }

        var validSkill = GetAllowedSkills(sigilTrinket).Contains(Skill.AssessPerson);
        var validEffectId = sigilTrinket.SigilTrinketEffectId == (int)SigilTrinketPerceptionEffect.Exposure;

        if (DamageEvent.Weapon is null)
        {
            return false;
        }

        return validSkill && validEffectId;
    }

    private bool IsValidForAvoidance(SigilTrinket sigilTrinket)
    {
        if (Player.GetEquippedWeapon() == null)
        {
            return false;
        }

        var validSkill = GetAllowedSkills(sigilTrinket).Contains(Skill.Deception);
        var validEffectId = sigilTrinket.SigilTrinketEffectId == (int)SigilTrinketDeceptionEffect.Avoidance;

        if (DamageEvent.Weapon is null)
        {
            return false;
        }

        var isPowerAttack = Player.GetPowerAccuracyBar() > 0.5f;

        return validSkill && validEffectId && isPowerAttack;
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

    // Helper: return allowed specialized skills or fallback to single WieldSkillType if configured.
    private IEnumerable<Skill> GetAllowedSkills(SigilTrinket sigilTrinket)
    {
        if (sigilTrinket.AllowedSpecializedSkills != null && sigilTrinket.AllowedSpecializedSkills.Count > 0)
        {
            return sigilTrinket.AllowedSpecializedSkills;
        }

        if (sigilTrinket.WieldSkillType.HasValue && sigilTrinket.WieldSkillType.Value != 0)
        {
            return new List<Skill> { (Skill)sigilTrinket.WieldSkillType.Value };
        }

        return Enumerable.Empty<Skill>();
    }
}

