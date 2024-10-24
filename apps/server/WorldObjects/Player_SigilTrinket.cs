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
    private double _lastHotspotHintTick;
    private const double HotspotHintTickTime = 3;

    private double _nextTriggerTimeBlue;
    private double _nextTriggerTimeYellow;
    private double _nextTriggerTimeRed;

    /// <summary>
    /// Check which Menhir Field we are in and recharge valid sigil trinkets
    /// Recharges sigil trinkets that are in the player's pack or equipped, and only of lower tiers
    /// </summary>
    public void RechargeSigilTrinkets(Hotspot manaField)
    {
        var equippedSigilTrinkets = GetEquippedSigilTrinkets();
        var heldSigilTrinketsBlue = GetHeldSigilTrinketsBlue();
        var heldSigilTrinketsYellow = GetHeldSigilTrinketsYellow();
        var heldSigilTrinketsRed = GetHeldSigilTrinketsRed();

        // if player is within the mana field, is not meditating, and has any sigil trinkets on them: trigger a blue effect flash on the player every 3 seconds.
        var forwardCommand = CurrentMotionState.MotionState.ForwardCommand;
        if (forwardCommand != MotionCommand.MeditateState)
        {
            if (_lastHotspotHintTick + HotspotHintTickTime < Time.GetUnixTime())
            {
                var showHintEffect = false;

                var allHeldScarabs = new List<SigilTrinket>()
                    .Concat(equippedSigilTrinkets)
                    .Concat(heldSigilTrinketsBlue)
                    .Concat(heldSigilTrinketsYellow)
                    .Concat(heldSigilTrinketsRed)
                    .ToList();
                if (allHeldScarabs.Count > 0)
                {
                    showHintEffect = true;
                }

                if (showHintEffect)
                {
                    PlayParticleEffect(PlayScript.RestrictionEffectBlue, Guid);
                    _lastHotspotHintTick = Time.GetUnixTime();
                }
            }
            return;
        }

        if (manaField.Tier == 0) // Low
        {
            foreach (var sigilTrinket in equippedSigilTrinkets)
            {
                if (sigilTrinket.SigilTrinketColor == 0)
                {
                    sigilTrinket.RechargeSigilTrinket(manaField);
                }

                SetSigilTrinketsBonus(manaField);
            }
        }

        if (manaField.Tier == 1) // Moderate
        {
            foreach (var sigilTrinket in equippedSigilTrinkets)
            {
                if (sigilTrinket.SigilTrinketColor == 0 || sigilTrinket.SigilTrinketColor == 1)
                {
                    sigilTrinket.RechargeSigilTrinket(manaField);
                }

                SetSigilTrinketsBonus(manaField);
            }

            foreach (var sigilTrinket in heldSigilTrinketsBlue)
            {
                sigilTrinket.RechargeSigilTrinket(manaField);
            }
        }

        if (manaField.Tier == 2) // High
        {
            foreach (var sigilTrinket in equippedSigilTrinkets)
            {
                sigilTrinket.RechargeSigilTrinket(manaField);

                SetSigilTrinketsBonus(manaField);
            }

            foreach (var sigilTrinket in heldSigilTrinketsBlue)
            {
                sigilTrinket.RechargeSigilTrinket(manaField);
            }

            foreach (var sigilTrinket in heldSigilTrinketsYellow)
            {
                sigilTrinket.RechargeSigilTrinket(manaField);
            }
        }

        if (manaField.Tier == 3) // Lyceum
        {
            foreach (var sigilTrinket in heldSigilTrinketsBlue)
            {
                sigilTrinket.RechargeSigilTrinket(manaField);
            }

            foreach (var sigilTrinket in heldSigilTrinketsYellow)
            {
                sigilTrinket.RechargeSigilTrinket(manaField);
            }

            foreach (var sigilTrinket in heldSigilTrinketsRed)
            {
                sigilTrinket.RechargeSigilTrinket(manaField);
            }
        }
    }

    /// <summary>
    /// Check which Menhir Field we are in and recharge EQUIPPED valid scarabs
    /// Additionally, reset the scarab's current bonus and assign it a new one based on which mana field they are in
    /// </summary>
    private void SetSigilTrinketsBonus(Hotspot manaField)
    {
        var equippedSigilTrinkets = GetEquippedSigilTrinkets();

        var bonusStat = manaField.SigilTrinketBonusStat ?? 0;
        var bonusAmount = manaField.SigilTrinketBonusStatAmount ?? 0;

        foreach (var sigilTrinket in equippedSigilTrinkets)
        {
            if (sigilTrinket.SigilTrinketColor == 0 && manaField.Tier == 0) // Blue
            {
                sigilTrinket.SetSigilTrinketBonusStat(bonusStat, bonusAmount);
            }
            else if (sigilTrinket.SigilTrinketColor == 1 && manaField.Tier == 1) // Yellow
            {
                sigilTrinket.SetSigilTrinketBonusStat(bonusStat, bonusAmount);
            }
            else if (sigilTrinket.SigilTrinketColor == 2 && manaField.Tier == 2) // Red
            {
                sigilTrinket.SetSigilTrinketBonusStat(bonusStat, bonusAmount);
            }
        }
    }

    public float GetSigilTrinketManaReductionMod()
    {
        var equippedSigilTrinkets = GetEquippedSigilTrinkets();
        if (equippedSigilTrinkets.Count < 1)
        {
            return 1.0f;
        }

        foreach (var sigilTrinket in equippedSigilTrinkets)
        {
            var currentTime = Time.GetUnixTime();
            if (!SigilTrinketCooldownChecks(sigilTrinket, currentTime))
            {
                continue;
            }

            if (sigilTrinket.SigilTrinketEffectId != null && (SigilTrinketEffect)sigilTrinket.SigilTrinketEffectId != SigilTrinketEffect.ScarabManaReduction)
            {
                continue;
            }

            sigilTrinket.NextTrinketTriggerTime = currentTime + sigilTrinket.CooldownDuration ?? 20.0;
            sigilTrinket.StartCooldown(this);
            sigilTrinket.DecreaseStructure(1, this);

            // Also stamp the player with a cooldown time for each slot, to prevent swapping sigil trinkets to bypass cooldown.
            switch (sigilTrinket.SigilTrinketColor)
            {
                case (int)SigilTrinketColor.Blue:
                    _nextTriggerTimeBlue = currentTime + sigilTrinket.CooldownDuration ?? 20.0;
                    break;
                case (int)SigilTrinketColor.Yellow:
                    _nextTriggerTimeYellow = currentTime + sigilTrinket.CooldownDuration ?? 20.0;
                    break;
                case (int)SigilTrinketColor.Red:
                    _nextTriggerTimeRed = currentTime + sigilTrinket.CooldownDuration ?? 20.0;
                    break;
            }

            if (sigilTrinket.SigilTrinketReductionAmount != null)
            {
                return 1.0f - (float)sigilTrinket.SigilTrinketReductionAmount;
            }
        }

        return 1.0f;
    }

    public void CheckForSigilTrinketOnCastEffects(WorldObject target, Spell spell, bool isWeaponSpell, Creature creatureToCastSpellFrom = null, bool onCrit = false)
    {
        var equippedSigilTrinkets = GetEquippedSigilTrinkets();
        if (equippedSigilTrinkets.Count < 1)
        {
            return;
        }

        CheckForReadySigilTrinketEffects(equippedSigilTrinkets, target, spell, isWeaponSpell, creatureToCastSpellFrom, onCrit);
    }

    /// <summary>
    /// Check each equipped Empowered Scarab to see if any have a relevant effect for the cast spell
    /// </summary>
    private void CheckForReadySigilTrinketEffects(List<SigilTrinket> equippedSigilTrinkets,
        WorldObject target,
        Spell spell,
        bool isWeaponSpell,
        Creature creatureToCastSpellFrom = null,
        bool onCrit = false)
    {
        var effectsUsed = new List<SigilTrinketEffect>();

        foreach (var sigilTrinket in equippedSigilTrinkets)
        {
            if (sigilTrinket.SigilTrinketEffectId != null && effectsUsed.Contains((SigilTrinketEffect)sigilTrinket.SigilTrinketEffectId))
            {
                continue;
            }

            if (!SigilTrinketValidationChecks(sigilTrinket, spell, effectsUsed, creatureToCastSpellFrom, onCrit))
            {
                continue;
            }

            var currentTime = Time.GetUnixTime();
            if (!SigilTrinketCooldownChecks(sigilTrinket, currentTime, onCrit))
            {
                continue;
            }

            if (sigilTrinket.SigilTrinketEffectId != null)
            {
                var effect = (SigilTrinketEffect)sigilTrinket.SigilTrinketEffectId;
                effectsUsed.Add(effect); // This trinket's trigger successfully occurred. Add the effect ID to a list to prevent other equipped trinkets with the same effect from triggering for this spell.
            }

            sigilTrinket.TriggerSpell = spell;
            sigilTrinket.IsWeaponSpell = isWeaponSpell;
            sigilTrinket.SpellTarget = target;

            if (sigilTrinket.SigilTrinketEffectId is (int)SigilTrinketEffect.ScarabDetonate)
            {
                sigilTrinket.CreatureToCastSpellFrom = creatureToCastSpellFrom;
            }

            sigilTrinket.NextTrinketTriggerTime = currentTime + sigilTrinket.CooldownDuration ?? 20.0;
            sigilTrinket.StartCooldown(this);
            sigilTrinket.DecreaseStructure(1, this);

            // Also stamp the player with a cooldown time for each slot, to prevent swapping trinkets to bypass cooldown.
            switch (sigilTrinket.SigilTrinketColor)
            {
                case (int)SigilTrinketColor.Blue:
                    _nextTriggerTimeBlue = currentTime + sigilTrinket.CooldownDuration ?? 20.0;
                    break;
                case (int)SigilTrinketColor.Yellow:
                    _nextTriggerTimeYellow = currentTime + sigilTrinket.CooldownDuration ?? 20.0;
                    break;
                case (int)SigilTrinketColor.Red:
                    _nextTriggerTimeRed = currentTime + sigilTrinket.CooldownDuration ?? 20.0;
                    break;
            }

            StartSigilTrinketEffect(sigilTrinket);
        }
    }

    private void StartSigilTrinketEffect(SigilTrinket sigilTrinket)
    {
        if (sigilTrinket.SigilTrinketEffectId == null)
        {
            return;
        }

        switch ((SigilTrinketEffect)sigilTrinket.SigilTrinketEffectId)
        {
            case SigilTrinketEffect.ScarabCastVuln:
            case SigilTrinketEffect.ScarabCastProt:
            case SigilTrinketEffect.ScarabCastItemBuff:
            case SigilTrinketEffect.ScarabCastVitalRate:
                var maxLevel = sigilTrinket.SigilTrinketMaxLevel ?? 1;
                sigilTrinket.SpellLevel = (uint)Math.Min(sigilTrinket.TriggerSpell.Level, maxLevel);

                CastSigilTrinketSpell(sigilTrinket);
                break;
            case SigilTrinketEffect.ScarabIntensity:

                if (sigilTrinket.SigilTrinketIntensity != null)
                {
                    sigilTrinket.TriggerSpell.SpellPowerMod = (float)sigilTrinket.SigilTrinketIntensity + 1;
                    Session.Network.EnqueueSend(
                        new GameMessageSystemChat(
                            $"Empowered Scarab of Intensity increased the spell's intensity by " +
                            $"{Math.Round((sigilTrinket.SigilTrinketIntensity ?? 1) * 100, 0)}%!",
                            ChatMessageType.Magic
                        )
                    );
                }
                break;
            case SigilTrinketEffect.ScarabShield:

                maxLevel = sigilTrinket.SigilTrinketMaxLevel ?? 1;
                sigilTrinket.SpellLevel = (uint)Math.Min(sigilTrinket.TriggerSpell.Level, maxLevel);
                sigilTrinket.SigilTrinketCastSpellId = 5206; // Surge of Protection

                CastSigilTrinketSpell(sigilTrinket, false);
                break;
            case SigilTrinketEffect.ScarabDuplicate:

                maxLevel = sigilTrinket.SigilTrinketMaxLevel ?? 1;
                sigilTrinket.SpellLevel = (uint)Math.Min(sigilTrinket.TriggerSpell.Level, maxLevel);
                sigilTrinket.SigilTrinketCastSpellId = sigilTrinket.TriggerSpell.Id;

                CastSigilTrinketSpell(sigilTrinket);
                break;
            case SigilTrinketEffect.ScarabDetonate:

                maxLevel = sigilTrinket.SigilTrinketMaxLevel ?? 1;
                sigilTrinket.SpellLevel = (uint)Math.Min(sigilTrinket.TriggerSpell.Level, maxLevel);
                sigilTrinket.SigilTrinketCastSpellId = DetonateSpellId(sigilTrinket.TriggerSpell);
                sigilTrinket.SpellIntensityMultiplier = AoeSpellIntensity(sigilTrinket.TriggerSpell);

                if (sigilTrinket.SigilTrinketCastSpellId != null)
                {
                    CastSigilTrinketSpell(sigilTrinket, false);
                }
                break;
            case SigilTrinketEffect.ScarabCrit:

                maxLevel = sigilTrinket.SigilTrinketMaxLevel ?? 1;
                sigilTrinket.SpellLevel = (uint)Math.Min(sigilTrinket.TriggerSpell.Level, maxLevel);
                sigilTrinket.SigilTrinketCastSpellId = 6328; // Surge of Crushing (Gauntlet Critical Damage Boost)

                CastSigilTrinketSpell(sigilTrinket, false);
                break;
        }
    }

    /// <summary>
    /// Prepare a spell to be cast, using the CastSpelId on the sigil trinket.
    /// </summary>
    private void CastSigilTrinketSpell(SigilTrinket sigilTrinket, bool useProgression = true)
    {
        SpellId castSpellLevel1Id;

        switch (sigilTrinket.SigilTrinketEffectId)
        {
            case (int)SigilTrinketEffect.ScarabCastVitalRate:
                switch (sigilTrinket.TriggerSpell.Category)
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
                break;
            case (int)SigilTrinketEffect.ScarabCastItemBuff:
                switch (sigilTrinket.TriggerSpell.Category)
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
        switch (sigilTrinket.SigilTrinketEffectId)
        {
            case (int)SigilTrinketEffect.ScarabCastVitalRate:
                CreateSigilTrinketSpell(sigilTrinket, castSpellId, 1.0);
                break;
            case (int)SigilTrinketEffect.ScarabDetonate:
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
            this,
            () =>
            {
                // Detonate
                if (sigilTrinket.CreatureToCastSpellFrom != null)
                {
                    CreateSpellProjectiles(castSpell, sigilTrinket.CreatureToCastSpellFrom, this, false, false, 0, true);
                }
                // Trigger spells affect all targets of fellowship boost spells
                else if (sigilTrinket.TriggerSpell.IsFellowshipSpell && Fellowship?.TotalMembers > 1)
                {
                    foreach (var fellowshipMember in Fellowship.GetFellowshipMembers())
                    {
                        CreatePlayerSpell(fellowshipMember.Value, castSpell, sigilTrinket.IsWeaponSpell);
                    }
                }
                else if (sigilTrinket.TriggerSpell.IsSelfTargeted)
                {
                    CreatePlayerSpell(this, castSpell, sigilTrinket.IsWeaponSpell);
                }
                else
                {
                    CreatePlayerSpell(sigilTrinket.SpellTarget, castSpell, sigilTrinket.IsWeaponSpell);
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

    // --- VALIDATION ---
    private bool SigilTrinketValidationChecks(SigilTrinket sigilTrinket, Spell spell, List<SigilTrinketEffect> effectsUsed, Creature creatureTarget = null, bool onCrit = false)
    {
        // CHECK FOR NULL and EFFECT ID
        if (sigilTrinket.SigilTrinketEffectId != null && (effectsUsed.Contains((SigilTrinketEffect)sigilTrinket.SigilTrinketEffectId)))
        {
            return false;
        }

        // CHECK CONDITIONS OF EACH TRINKET TYPE
        switch (sigilTrinket.SigilTrinketEffectId)
        {
            case (int)SigilTrinketEffect.ScarabCastVuln:
                return IsValidForCastVuln(spell);

            case (int)SigilTrinketEffect.ScarabCastProt:
                return IsValidForCastProt(spell);

            case (int)SigilTrinketEffect.ScarabCastItemBuff:
                return IsValidForCastItemBuff(spell);

            case (int)SigilTrinketEffect.ScarabCastVitalRate:
                return IsValidForCastVitalRate(spell);

            case (int)SigilTrinketEffect.ScarabIntensity:
                return IsValidForIntensity(spell, sigilTrinket);

            case (int)SigilTrinketEffect.ScarabShield:
                return IsValidForShield(spell, sigilTrinket);

            case (int)SigilTrinketEffect.ScarabDuplicate:
                return IsValidForDuplicate(spell, sigilTrinket);

            case (int)SigilTrinketEffect.ScarabDetonate:
                return IsValidForDetonate(spell, sigilTrinket, creatureTarget);

            case (int)SigilTrinketEffect.ScarabCrit:
                return IsValidForCrushing(spell, sigilTrinket, onCrit);
        }

        return true;
    }

    private bool SigilTrinketCooldownChecks(SigilTrinket sigilTrinket, double currentTime, bool onCrit = false)
    {
        if (sigilTrinket.NextTrinketTriggerTime > currentTime)
        {
            return false;
        }

        switch (sigilTrinket.SigilTrinketColor)
        {
            case (int)SigilTrinketColor.Blue:
                if (_nextTriggerTimeBlue > currentTime)
                {
                    return false;
                }
                break;
            case (int)SigilTrinketColor.Yellow:
                if (_nextTriggerTimeYellow > currentTime)
                {
                    return false;
                }
                break;
            case (int)SigilTrinketColor.Red:
                if (_nextTriggerTimeRed > currentTime)
                {
                    return false;
                }
                break;
        }

        // ROLL AGAINST TRIGGER CHANCE
        var rng = ThreadSafeRandom.Next(0.0f, 1.0f);

        return !(sigilTrinket.SigilTrinketTriggerChance < rng) || onCrit;
    }

    private static bool IsValidForCastVuln(Spell spell, bool onCrit = false)
    {
        return SigilTrinket.LifeHarmfulTriggerSpells.Contains(spell.Category) && !onCrit;
    }

    private static bool IsValidForCastProt(Spell spell, bool onCrit = false)
    {
        return SigilTrinket.LifeBeneficialTriggerSpells.Contains(spell.Category) && !onCrit;
    }

    private static bool IsValidForCastItemBuff(Spell spell, bool onCrit = false)
    {
        return SigilTrinket.LifeBeneficialTriggerSpells.Contains(spell.Category) && !onCrit;
    }

    private static bool IsValidForCastVitalRate(Spell spell, bool onCrit = false)
    {
        return (SigilTrinket.LifeBeneficialTriggerSpells.Contains(spell.Category) || SigilTrinket.LifeHarmfulTriggerSpells.Contains(spell.Category)) && !onCrit;
    }

    private static bool IsValidForIntensity(Spell spell, SigilTrinket sigilTrinket, bool onCrit = false)
    {
        return sigilTrinket.SigilTrinketMaxLevel >= spell.Level && !onCrit;
    }

    private static bool IsValidForShield(Spell spell, SigilTrinket sigilTrinket, bool onCrit = false)
    {
        return sigilTrinket.SigilTrinketMaxLevel >= spell.Level && !onCrit;
    }

    private static bool IsValidForDuplicate(Spell spell, SigilTrinket sigilTrinket, bool onCrit = false)
    {
        return SigilTrinket.WarProjectileTriggerCategories.Contains(spell.Category) && sigilTrinket.SigilTrinketMaxLevel >= spell.Level && !onCrit;
    }

    private static bool IsValidForDetonate(Spell spell, SigilTrinket sigilTrinket, Creature creatureTarget = null, bool onCrit = false)
    {
        return creatureTarget != null
            && SigilTrinket.WarProjectileTriggerCategories.Contains(spell.Category)
            && sigilTrinket.SigilTrinketMaxLevel >= spell.Level
            && !onCrit;
    }

    private static bool IsValidForCrushing(Spell spell, SigilTrinket sigilTrinket, bool onCrit = false)
    {
        return SigilTrinket.WarProjectileTriggerCategories.Contains(spell.Category)
            && sigilTrinket.SigilTrinketMaxLevel >= spell.Level
            && onCrit;
    }

    private void UpdateSigilTrinketManaReservation()
    {
        var equippedSigilTrinkets = GetEquippedSigilTrinkets();

        if (equippedSigilTrinkets == null)
        {
            return;
        }

        foreach (var sigilTrinket in equippedSigilTrinkets)
        {
            sigilTrinket.OnEquip(this);
        }
    }

    public bool HasRechargeableSigilTrinkets(int hotspotTier)
    {
        var heldSigilTrinketsBlue = GetHeldSigilTrinketsBlue();
        var heldSigilTrinketsYellow = GetHeldSigilTrinketsYellow();
        var heldSigilTrinketsRed = GetHeldSigilTrinketsRed();

        var equippedSigilTrinketBlue = GetEquippedSigilTrinketOfType(SigilTrinketColor.Blue);
        var equippedSigilTrinketYellow = GetEquippedSigilTrinketOfType(SigilTrinketColor.Yellow);
        var equippedSigilTrinketRed = GetEquippedSigilTrinketOfType(SigilTrinketColor.Red);

        return hotspotTier switch
        {
            0 => // Low
                equippedSigilTrinketBlue?.Structure < equippedSigilTrinketBlue?.MaxStructure,
            1 => // Moderate
                equippedSigilTrinketYellow?.Structure < equippedSigilTrinketYellow?.MaxStructure
                || SigilTrinketInListCanBeRecharged(heldSigilTrinketsBlue),
            2 => // High
                equippedSigilTrinketRed?.Structure < equippedSigilTrinketRed?.MaxStructure
                || SigilTrinketInListCanBeRecharged(heldSigilTrinketsYellow)
                || SigilTrinketInListCanBeRecharged(heldSigilTrinketsBlue),
            3 => // Lyceum
                SigilTrinketInListCanBeRecharged(heldSigilTrinketsRed)
                || SigilTrinketInListCanBeRecharged(heldSigilTrinketsYellow)
                || SigilTrinketInListCanBeRecharged(heldSigilTrinketsBlue),
            _ => false
        };
    }

    private bool SigilTrinketInListCanBeRecharged(List<SigilTrinket> listOfSigilTrinkets)
    {
        return listOfSigilTrinkets.Any(sigilTrinket => sigilTrinket.Structure < sigilTrinket.MaxStructure);
    }

    public bool HasMatchingMenhirBonusStat(int sigilTrinketBonusStat, int sigilTrinketBonusStatAmount)
    {
        var equippedSigilTrinkets = GetEquippedSigilTrinkets();

        return equippedSigilTrinkets.Any(sigilTrinket => (sigilTrinket.SigilTrinketBonusStat ?? 0) == sigilTrinketBonusStat && (sigilTrinket.SigilTrinketBonusStatAmount ?? 0) == sigilTrinketBonusStatAmount);
    }
}
