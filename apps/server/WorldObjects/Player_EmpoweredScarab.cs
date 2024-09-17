using System;
using System.Collections.Generic;
using System.Linq;
using ACE.Common;
using ACE.Entity.Enum;
using ACE.Server.Entity;
using ACE.Server.Entity.Actions;
using ACE.Server.Factories.Tables;
using ACE.Server.Network.GameMessages.Messages;

namespace ACE.Server.WorldObjects;

partial class Player
{
    private double LastHotspotHintTick = 0;
    private double HotspotHintTickTime = 3;

    private double NextTriggerTimeBlue = 0;
    private double NextTriggerTimeYellow = 0;
    private double NextTriggerTimeRed = 0;

    /// <summary>
    /// Check which Menhir Field we are in and recharge valid scarabs
    /// Recharges scarabs that are in the player's pack or equipped, and only of lower tiers
    /// </summary>
    public void RechargeEmpoweredScarabs(Hotspot manaField)
    {
        var equippedEmpoweredScarabs = GetEquippedEmpoweredScarabs();
        var heldEmpoweredScarabsBlue = GetHeldEmpoweredScarabsBlue();
        var heldEmpoweredScarabsYellow = GetHeldEmpoweredScarabsYellow();
        var heldEmpoweredScarabsRed = GetHeldEmpoweredScarabsRed();

        // if player is within the mana field, is not meditating, and has any empowered scarab on them: trigger a blue effect flash on the player every 3 seconds.
        var forwardCommand =
            CurrentMovementData.MovementType == MovementType.Invalid && CurrentMovementData.Invalid != null
                ? CurrentMovementData.Invalid.State.ForwardCommand
                : MotionCommand.Invalid;
        if (forwardCommand != MotionCommand.MeditateState)
        {
            if (LastHotspotHintTick + HotspotHintTickTime < Time.GetUnixTime())
            {
                var showHintEffect = false;

                var allHeldScarabs = new List<EmpoweredScarab>()
                    .Concat(equippedEmpoweredScarabs)
                    .Concat(heldEmpoweredScarabsBlue)
                    .Concat(heldEmpoweredScarabsYellow)
                    .Concat(heldEmpoweredScarabsRed)
                    .ToList();
                if (allHeldScarabs.Count > 0)
                {
                    showHintEffect = true;
                }

                if (showHintEffect)
                {
                    PlayParticleEffect(PlayScript.RestrictionEffectBlue, Guid);
                    LastHotspotHintTick = Time.GetUnixTime();
                }
            }
            return;
        }

        if (manaField.Tier == 0) // Low
        {
            //Console.WriteLine("Mana Field LOW");
            foreach (var empoweredScarab in equippedEmpoweredScarabs)
            {
                if (empoweredScarab.EmpoweredScarabColor == 0)
                {
                    empoweredScarab.RechargeEmpoweredScarab(manaField);
                }

                SetEmpoweredScarabsBonus(manaField);
            }
        }

        if (manaField.Tier == 1) // Moderate
        {
            //Console.WriteLine("Mana Field MODERATE");
            foreach (var empoweredScarab in equippedEmpoweredScarabs)
            {
                if (empoweredScarab.EmpoweredScarabColor == 0 || empoweredScarab.EmpoweredScarabColor == 1)
                {
                    empoweredScarab.RechargeEmpoweredScarab(manaField);
                }

                SetEmpoweredScarabsBonus(manaField);
            }

            foreach (var empoweredScarab in heldEmpoweredScarabsBlue)
            {
                empoweredScarab.RechargeEmpoweredScarab(manaField);
            }
        }

        if (manaField.Tier == 2) // High
        {
            //Console.WriteLine("Mana Field HIGH");
            foreach (var empoweredScarab in equippedEmpoweredScarabs)
            {
                empoweredScarab.RechargeEmpoweredScarab(manaField);

                SetEmpoweredScarabsBonus(manaField);
            }

            foreach (var empoweredScarab in heldEmpoweredScarabsBlue)
            {
                empoweredScarab.RechargeEmpoweredScarab(manaField);
            }

            foreach (var empoweredScarab in heldEmpoweredScarabsYellow)
            {
                empoweredScarab.RechargeEmpoweredScarab(manaField);
            }
        }

        if (manaField.Tier == 3) // Lyceum
        {
            //Console.WriteLine("Mana Field LYCEUM");
            foreach (var empoweredScarab in heldEmpoweredScarabsBlue)
            {
                empoweredScarab.RechargeEmpoweredScarab(manaField);
            }

            foreach (var empoweredScarab in heldEmpoweredScarabsYellow)
            {
                empoweredScarab.RechargeEmpoweredScarab(manaField);
            }

            foreach (var empoweredScarab in heldEmpoweredScarabsRed)
            {
                empoweredScarab.RechargeEmpoweredScarab(manaField);
            }
        }
    }

    /// <summary>
    /// Check which Menhir Field we are in and recharge EQUIPPED valid scarabs
    /// Additionally, reset the scarab's current bonus and assign it a new one based on which mana field they are in
    /// </summary>
    public void SetEmpoweredScarabsBonus(Hotspot manaField)
    {
        var equippedEmpoweredScarabs = GetEquippedEmpoweredScarabs();

        var bonusStat = manaField.EmpoweredScarabBonusStat ?? 0;
        var bonusAmount = manaField.EmpoweredScarabBonusStatAmount ?? 0;

        foreach (var empoweredScarab in equippedEmpoweredScarabs)
        {
            if (empoweredScarab.EmpoweredScarabColor == 0 && manaField.Tier == 0) // Blue
            {
                empoweredScarab.SetScarabBonus(bonusStat, bonusAmount);
            }
            else if (empoweredScarab.EmpoweredScarabColor == 1 && manaField.Tier == 1) // Yellow
            {
                empoweredScarab.SetScarabBonus(bonusStat, bonusAmount);
            }
            else if (empoweredScarab.EmpoweredScarabColor == 2 && manaField.Tier == 2) // Red
            {
                empoweredScarab.SetScarabBonus(bonusStat, bonusAmount);
            }
        }
    }

    public float GetEmpoweredScarabManaReductionMod()
    {
        var equippedEmpoweredScarabs = GetEquippedEmpoweredScarabs();
        if (equippedEmpoweredScarabs.Count < 1)
        {
            return 1.0f;
        }

        foreach (var empoweredScarab in equippedEmpoweredScarabs)
        {
            var currentTime = Time.GetUnixTime();
            if (!EmpoweredScarabCooldownChecks(empoweredScarab, currentTime))
            {
                continue;
            }

            if (empoweredScarab.EmpoweredScarabEffectId != null &&
                (EmpoweredScarabEffect)empoweredScarab.EmpoweredScarabEffectId != EmpoweredScarabEffect.ManaReduction)
            {
                continue;
            }

            empoweredScarab.NextEmpoweredScarabTriggerTime = currentTime + empoweredScarab.CooldownDuration ?? 20.0;
            empoweredScarab.StartCooldown(this);
            empoweredScarab.DecreaseStructure(1, this);

            // Also stamp the player with a cooldown time for each slot, to prevent swapping scarabs to bypass cooldown.
            switch (empoweredScarab.EmpoweredScarabColor)
            {
                case 0:
                    NextTriggerTimeBlue = currentTime + empoweredScarab.CooldownDuration ?? 20.0;
                    break;
                case 1:
                    NextTriggerTimeYellow = currentTime + empoweredScarab.CooldownDuration ?? 20.0;
                    break;
                case 2:
                    NextTriggerTimeRed = currentTime + empoweredScarab.CooldownDuration ?? 20.0;
                    break;
            }

            if (empoweredScarab.EmpoweredScarabReductionAmount != null)
            {
                return 1.0f - (float)empoweredScarab.EmpoweredScarabReductionAmount;
            }
        }

        return 1.0f;
    }

    public void CheckForEmpoweredScarabOnCastEffects(
        WorldObject target,
        Spell spell,
        bool isWeaponSpell,
        Creature creatureToCastSpellFrom = null,
        bool onCrit = false
    )
    {
        var equippedEmpoweredScarabs = GetEquippedEmpoweredScarabs();
        if (equippedEmpoweredScarabs.Count < 1)
        {
            return;
        }

        if (creatureToCastSpellFrom != null)
        {
            foreach (var scarab in equippedEmpoweredScarabs)
            {
                if (scarab.EmpoweredScarabEffectId != (int)EmpoweredScarabEffect.Detonate)
                {
                    creatureToCastSpellFrom = null;
                }
            }
        }

        CheckForReadyEmpoweredScarabEffects(
            equippedEmpoweredScarabs,
            target,
            spell,
            isWeaponSpell,
            creatureToCastSpellFrom,
            onCrit
        );
    }

    /// <summary>
    /// Check each equipped Empowered Scarab to see if any have a relevant effect for the cast spell
    /// </summary>
    public EmpoweredScarab CheckForReadyEmpoweredScarabEffects(
        List<EmpoweredScarab> equippedEmpoweredScarabs,
        WorldObject target,
        Spell spell,
        bool isWeaponSpell,
        Creature creatureToCastSpellFrom = null,
        bool onCrit = false
    )
    {
        var effectsUsed = new List<EmpoweredScarabEffect>();

        foreach (var empoweredScarab in equippedEmpoweredScarabs)
        {
            if (empoweredScarab.EmpoweredScarabEffectId != null &&
                effectsUsed.Contains((EmpoweredScarabEffect)empoweredScarab.EmpoweredScarabEffectId))
            {
                continue;
            }

            if (!EmpoweredScarabValidationChecks(empoweredScarab, spell, effectsUsed, creatureToCastSpellFrom, onCrit))
            {
                continue;
            }

            var currentTime = Time.GetUnixTime();
            if (!EmpoweredScarabCooldownChecks(empoweredScarab, currentTime, onCrit))
            {
                continue;
            }

            if (empoweredScarab.EmpoweredScarabEffectId != null)
            {
                var effect = (EmpoweredScarabEffect)empoweredScarab.EmpoweredScarabEffectId;
                effectsUsed.Add(effect); // This scarab's trigger successfully occurred. Add the effect ID to a list to prevent other equipped scarabs with the same effect from triggering for this spell.
            }

            empoweredScarab.TriggerSpell = spell;
            empoweredScarab.IsWeaponSpell = isWeaponSpell;
            empoweredScarab.SpellTarget = target;
            empoweredScarab.CreatureToCastSpellFrom = creatureToCastSpellFrom;

            empoweredScarab.NextEmpoweredScarabTriggerTime = currentTime + empoweredScarab.CooldownDuration ?? 20.0;
            empoweredScarab.StartCooldown(this);
            empoweredScarab.DecreaseStructure(1, this);

            // Also stamp the player with a cooldown time for each slot, to prevent swapping scarabs to bypass cooldown.
            switch (empoweredScarab.EmpoweredScarabColor)
            {
                case 0:
                    NextTriggerTimeBlue = currentTime + empoweredScarab.CooldownDuration ?? 20.0;
                    break;
                case 1:
                    NextTriggerTimeYellow = currentTime + empoweredScarab.CooldownDuration ?? 20.0;
                    break;
                case 2:
                    NextTriggerTimeRed = currentTime + empoweredScarab.CooldownDuration ?? 20.0;
                    break;
            }

            StartEmpoweredScarabEffect(empoweredScarab);
        }
        return null;
    }

    private void StartEmpoweredScarabEffect(EmpoweredScarab empoweredScarab)
    {
        if (empoweredScarab.EmpoweredScarabEffectId != null)
        {
            switch ((EmpoweredScarabEffect)empoweredScarab.EmpoweredScarabEffectId)
            {
                case EmpoweredScarabEffect.CastVuln:
                case EmpoweredScarabEffect.CastProt:
                case EmpoweredScarabEffect.CastItemBuff:
                case EmpoweredScarabEffect.CastVitalRate:
                    var maxLevel = empoweredScarab.EmpoweredScarabMaxLevel ?? 1;
                    empoweredScarab.SpellLevel = (uint)Math.Min(empoweredScarab.TriggerSpell.Level, maxLevel);

                    CastEmpoweredScarabSpell(empoweredScarab);
                    break;

                case EmpoweredScarabEffect.Intensity:

                    if (empoweredScarab.EmpoweredScarabIntensity != null)
                    {
                        empoweredScarab.TriggerSpell.SpellPowerMod = (float)empoweredScarab.EmpoweredScarabIntensity + 1;
                        Session.Network.EnqueueSend(
                            new GameMessageSystemChat(
                                $"Empowered Scarab of Intensity increased the spell's intensity by " +
                                $"{Math.Round((empoweredScarab.EmpoweredScarabIntensity ?? 1) * 100, 0)}%!",
                                ChatMessageType.Magic
                            )
                        );
                    }

                    break;

                case EmpoweredScarabEffect.Shield:

                    maxLevel = empoweredScarab.EmpoweredScarabMaxLevel ?? 1;
                    empoweredScarab.SpellLevel = (uint)Math.Min(empoweredScarab.TriggerSpell.Level, maxLevel);
                    empoweredScarab.EmpoweredScarabCastSpellId = 5206; // Surge of Protection

                    CastEmpoweredScarabSpell(empoweredScarab, false);
                    break;

                case EmpoweredScarabEffect.Duplicate:

                    maxLevel = empoweredScarab.EmpoweredScarabMaxLevel ?? 1;
                    empoweredScarab.SpellLevel = (uint)Math.Min(empoweredScarab.TriggerSpell.Level, maxLevel);
                    empoweredScarab.EmpoweredScarabCastSpellId = empoweredScarab.TriggerSpell.Id;

                    CastEmpoweredScarabSpell(empoweredScarab);
                    break;

                case EmpoweredScarabEffect.Detonate:

                    maxLevel = empoweredScarab.EmpoweredScarabMaxLevel ?? 1;
                    empoweredScarab.SpellLevel = (uint)Math.Min(empoweredScarab.TriggerSpell.Level, maxLevel);
                    empoweredScarab.EmpoweredScarabCastSpellId = DetonateSpellId(empoweredScarab.TriggerSpell);
                    empoweredScarab.SpellIntensityMultiplier = AoeSpellIntensity(empoweredScarab.TriggerSpell);

                    if (empoweredScarab.EmpoweredScarabCastSpellId != null)
                    {
                        CastEmpoweredScarabSpell(empoweredScarab, false);
                    }

                    break;

                case EmpoweredScarabEffect.Crit:

                    maxLevel = empoweredScarab.EmpoweredScarabMaxLevel ?? 1;
                    empoweredScarab.SpellLevel = (uint)Math.Min(empoweredScarab.TriggerSpell.Level, maxLevel);
                    empoweredScarab.EmpoweredScarabCastSpellId =
                        6328; // Surge of Crushing (Gauntlet Critical Damage Boost)

                    CastEmpoweredScarabSpell(empoweredScarab, false);
                    break;
            }
        }
    }

    /// <summary>
    /// Prepare a spell to be cast, using the CastSpelId on the scarab.
    /// </summary>
    private void CastEmpoweredScarabSpell(EmpoweredScarab empoweredScarab, bool useProgression = true)
    {
        SpellId castSpellLevel1Id;

        if (empoweredScarab.EmpoweredScarabEffectId == (int)EmpoweredScarabEffect.CastVitalRate)
        {
            switch (empoweredScarab.TriggerSpell.Category)
            {
                case SpellCategory.HealthRaising:
                    castSpellLevel1Id = SpellId.RegenerationOther1;
                    break;
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
        }
        else if (empoweredScarab.EmpoweredScarabEffectId == (int)EmpoweredScarabEffect.CastItemBuff)
        {
            switch (empoweredScarab.TriggerSpell.Category)
            {
                case SpellCategory.HealthRaising:
                    castSpellLevel1Id = SpellId.DefenderOther1;
                    break;
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
        }
        else
        {
            castSpellLevel1Id = (SpellId)(empoweredScarab.EmpoweredScarabCastSpellId ?? 0);
            if (castSpellLevel1Id == SpellId.Undef)
            {
                return;
            }
        }

        SpellId castSpellId;
        if (useProgression && empoweredScarab.SpellLevel != null)
        {
            castSpellId = SpellLevelProgression.GetSpellAtLevel(
                castSpellLevel1Id,
                (int)empoweredScarab.SpellLevel,
                true
            );
            if (castSpellId == SpellId.Undef)
            {
                return;
            }
        }
        else
        {
            castSpellId = castSpellLevel1Id;
        }

        if (empoweredScarab.EmpoweredScarabEffectId == (int)EmpoweredScarabEffect.CastVitalRate)
        {
            CreateEmpoweredScarabSpell(empoweredScarab, castSpellId, 1.0);
        }
        else if (empoweredScarab.CreatureToCastSpellFrom != null)
        {
            CreateEmpoweredScarabSpell(empoweredScarab, castSpellId, 0.0);
        }
        else
        {
            CreateEmpoweredScarabSpell(empoweredScarab, castSpellId, 0.5);
        }
    }

    /// <summary>
    /// Create the prepared spell in the world, decrease scarab structure, and start cooldown.
    /// </summary>
    private void CreateEmpoweredScarabSpell(EmpoweredScarab empoweredScarab, SpellId castSpellId, double delay)
    {
        var castSpell = new Spell(castSpellId, true);

        castSpell.SpellPowerMod = empoweredScarab.SpellIntensityMultiplier;
        castSpell.SpellStatModVal = empoweredScarab.SpellStatModValMultiplier;

        var SpellChain = new ActionChain();
        SpellChain.AddDelaySeconds(delay);
        SpellChain.AddAction(
            this,
            () =>
            {
                // DETONATE
                if (empoweredScarab.CreatureToCastSpellFrom != null)
                {
                    CreateSpellProjectiles(
                        castSpell,
                        empoweredScarab.CreatureToCastSpellFrom,
                        this,
                        false,
                        false,
                        0,
                        true
                    );
                }
                //
                else
                {
                    if (empoweredScarab.TriggerSpell.IsSelfTargeted)
                    {
                        CreatePlayerSpell(this, castSpell, empoweredScarab.IsWeaponSpell);
                    }
                    else
                    {
                        CreatePlayerSpell(empoweredScarab.SpellTarget, castSpell, empoweredScarab.IsWeaponSpell);
                    }
                }
            }
        );

        SpellChain.EnqueueChain();
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
            case 1:
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

    // --- VALIDATION ---

    private bool EmpoweredScarabValidationChecks(
        EmpoweredScarab empoweredScarab,
        Spell spell,
        List<EmpoweredScarabEffect> effectsUsed,
        Creature creatureTarget = null,
        bool onCrit = false
    )
    {
        // CHECK FOR NULL and EFFECT ID
        if (
            empoweredScarab == null
            || effectsUsed.Contains((EmpoweredScarabEffect)empoweredScarab.EmpoweredScarabEffectId)
        )
        {
            return false;
        }

        // CHECK CONDITIONS OF EACH SCARAB TYPE
        switch (empoweredScarab.EmpoweredScarabEffectId)
        {
            case (int)EmpoweredScarabEffect.CastVuln:
                if (!IsValidForCastVuln(spell))
                {
                    return false;
                }

                break;
            case (int)EmpoweredScarabEffect.CastProt:
                if (!IsValidForCastProt(spell))
                {
                    return false;
                }

                break;
            case (int)EmpoweredScarabEffect.CastItemBuff:
                if (!IsValidForCastItemBuff(spell))
                {
                    return false;
                }

                break;
            case (int)EmpoweredScarabEffect.CastVitalRate:
                if (!IsValidForCastVitalRate(spell))
                {
                    return false;
                }

                break;
            case (int)EmpoweredScarabEffect.Intensity:
                if (!IsValidForIntensity(spell, empoweredScarab))
                {
                    return false;
                }

                break;
            case (int)EmpoweredScarabEffect.Shield:
                if (!IsValidForShield(spell, empoweredScarab))
                {
                    return false;
                }

                break;
            case (int)EmpoweredScarabEffect.Duplicate:
                if (!IsValidForDuplicate(spell, empoweredScarab))
                {
                    return false;
                }

                break;
            case (int)EmpoweredScarabEffect.Detonate:
                if (!IsValidForDetonate(spell, empoweredScarab, creatureTarget))
                {
                    return false;
                }

                break;
            case (int)EmpoweredScarabEffect.Crit:
                if (!IsValidForCrushing(spell, empoweredScarab, onCrit))
                {
                    return false;
                }

                break;
        }

        return true;
    }

    private bool EmpoweredScarabCooldownChecks(EmpoweredScarab empoweredScarab, double currentTime, bool onCrit = false)
    {
        if (empoweredScarab.NextEmpoweredScarabTriggerTime > currentTime)
        {
            return false;
        }

        switch (empoweredScarab.EmpoweredScarabColor)
        {
            case 0:
                if (NextTriggerTimeBlue > currentTime)
                {
                    return false;
                }
                break;
            case 1:
                if (NextTriggerTimeYellow > currentTime)
                {
                    return false;
                }
                break;
            case 2:
                if (NextTriggerTimeRed > currentTime)
                {
                    return false;
                }
                break;
        }

        // ROLL AGAINST TRIGGER CHANCE
        var rng = ThreadSafeRandom.Next(0.0f, 1.0f);
        if (empoweredScarab.EmpoweredScarabTriggerChance < rng && !onCrit)
        {
            return false;
        }

        return true;
    }

    private bool IsValidForCastVuln(Spell spell, bool onCrit = false)
    {
        return EmpoweredScarab.LifeHarmfulTriggerSpells.Contains(spell.Category) && !onCrit;
    }

    private bool IsValidForCastProt(Spell spell, bool onCrit = false)
    {
        return EmpoweredScarab.LifeBeneficialTriggerSpells.Contains(spell.Category) && !onCrit;
    }

    private bool IsValidForCastItemBuff(Spell spell, bool onCrit = false)
    {
        return EmpoweredScarab.LifeBeneficialTriggerSpells.Contains(spell.Category) && !onCrit;
    }

    private bool IsValidForCastVitalRate(Spell spell, bool onCrit = false)
    {
        return (
                EmpoweredScarab.LifeBeneficialTriggerSpells.Contains(spell.Category)
                || EmpoweredScarab.LifeHarmfulTriggerSpells.Contains(spell.Category)
            ) && !onCrit;
    }

    private bool IsValidForIntensity(Spell spell, EmpoweredScarab empoweredScarab, bool onCrit = false)
    {
        return empoweredScarab.EmpoweredScarabMaxLevel >= spell.Level && !onCrit;
    }

    private bool IsValidForShield(Spell spell, EmpoweredScarab empoweredScarab, bool onCrit = false)
    {
        return empoweredScarab.EmpoweredScarabMaxLevel >= spell.Level && !onCrit;
    }

    private bool IsValidForDuplicate(Spell spell, EmpoweredScarab empoweredScarab, bool onCrit = false)
    {
        return EmpoweredScarab.WarProjectileTriggerCategories.Contains(spell.Category)
            && empoweredScarab.EmpoweredScarabMaxLevel >= spell.Level
            && !onCrit;
    }

    private bool IsValidForDetonate(
        Spell spell,
        EmpoweredScarab empoweredScarab,
        Creature creatureTarget = null,
        bool onCrit = false
    )
    {
        return creatureTarget != null
            && EmpoweredScarab.WarProjectileTriggerCategories.Contains(spell.Category)
            && empoweredScarab.EmpoweredScarabMaxLevel >= spell.Level
            && !onCrit;
    }

    private bool IsValidForCrushing(Spell spell, EmpoweredScarab empoweredScarab, bool onCrit = false)
    {
        return EmpoweredScarab.WarProjectileTriggerCategories.Contains(spell.Category)
            && empoweredScarab.EmpoweredScarabMaxLevel >= spell.Level
            && onCrit;
    }
}
