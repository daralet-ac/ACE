using System;
using ACE.Common;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Server.Entity;
using ACE.Server.Managers;
using ACE.Server.Network.GameMessages.Messages;

namespace ACE.Server.WorldObjects;

partial class WorldObject
{
    /// <summary>
    /// Determines whether a spell will be resisted,
    /// based upon the caster's magic skill vs target's magic defense skill
    /// </summary>
    /// <returns>TRUE if spell is resisted</returns>
    private static bool MagicDefenseCheck(
        uint casterMagicSkill,
        uint targetMagicDefenseSkill,
        out PartialEvasion partialResist,
        out float resistChance,
        Player targetPlayer
    )
    {
        // uses regular 0.03 factor, and not magic casting 0.07 factor
        var chance = (1.0 - SkillCheck.GetSkillChance((int)casterMagicSkill, (int)targetMagicDefenseSkill));
        resistChance = (float)chance;

        // COMBAT ABILITY - Evasive Stance: flat 25% chance to fully resist any spell, independent of magic defense skill.
        if (targetPlayer is { EvasiveStanceIsActive: true } && ThreadSafeRandom.Next(0.0f, 1.0f) < 0.25f)
        {
            partialResist = PartialEvasion.All;
            return true;
        }

        var resistRoll = ThreadSafeRandom.Next(0.0f, 1.0f);

        if (resistRoll < chance)
        {
            // a full resist, a partial resist or a full hit, with an equal chance of each
            partialResist = DamageFormulas.GetEvasionType(ThreadSafeRandom.Next(0.0f, 1.0f));
            return partialResist == PartialEvasion.All;
        }

        partialResist = PartialEvasion.None;
        return false;
    }

    /// <summary>
    /// If this spell has a chance to be resisted, rolls for a chance
    /// Returns TRUE if spell is resistable and was resisted for this attempt
    /// </summary>
    /// <param name="isReflected">
    /// COMBAT ABILITY - Reflect: set when the spell was reflected back at its original caster.
    /// The original caster is then passed as the itemCaster, so the resist check uses its own magic skill vs its own magic defense.
    /// </param>
    public bool TryResistSpell(
        WorldObject target,
        Spell spell,
        out PartialEvasion partialResist,
        WorldObject itemCaster = null,
        bool projectileHit = false,
        int? weaponSpellcraft = null,
        double? weaponAttackMod = null,
        bool isReflected = false
    )
    {
        partialResist = PartialEvasion.None;
        _partialEvasion = partialResist;

        // fix hermetic void?
        if (!spell.IsResistable && spell.Category != SpellCategory.ManaConversionModLowering || spell.IsSelfTargeted)
        {
            return false;
        }

        if (
            spell.MetaSpellType == SpellType.Dispel
            && spell.Align == DispelType.Negative
            && !PropertyManager.GetBool("allow_negative_dispel_resist").Item
        )
        {
            return false;
        }

        if (spell.NumProjectiles > 0 && !projectileHit)
        {
            return false;
        }

        if (itemCaster != null && Cloak.IsCloak(itemCaster))
        {
            return false;
        }

        var caster = itemCaster ?? this;

        var casterCreature = caster as Creature;
        var player = this as Player;
        var targetPlayer = target as Player;

        var magicSkill = GetEffectiveMagicSkill(target, spell, itemCaster, weaponSpellcraft, weaponAttackMod);

        // only creatures can resist spells?
        if (target is not Creature targetCreature)
        {
            return false;
        }

        // Retrieve target's Magic Defense Skill
        var difficulty = targetCreature.GetModdedMagicDefSkill();

        difficulty = Convert.ToUInt32(difficulty * (1.0f + CheckForCombatAbilityReflectMagicDefBonus(targetPlayer)));
        // Familiar Foe (Fire Opal): the ramp stamps live on the casting creature's QuestManager,
        // keyed by the defending player's name (accrued while that player attacked this creature).
        difficulty = Convert.ToUInt32(difficulty * (1.0f + Jewel.GetJewelEffectMod(targetPlayer, PropertyInt.GearFamiliarity, "Familiarity", rampQuestSource: casterCreature)));

        // level scaling goes last, so the bonuses above are worth the same at every level (see LevelScaling.GetScaledPlayerDefenseSkill)
        difficulty = LevelScaling.GetScaledPlayerDefenseSkill(difficulty, targetCreature, casterCreature);

        var resisted = MagicDefenseCheck(magicSkill, difficulty, out var pResist, out var resistChance, targetPlayer);

        partialResist = pResist;
        _partialEvasion = pResist;

        if (targetCreature.Invincible)
        {
            resisted = true;
        }

        if (targetPlayer != null)
        {
            if (targetPlayer.UnderLifestoneProtection)
            {
                targetPlayer.HandleLifestoneProtection();
                resisted = true;
            }
        }

        if (caster == target && !isReflected)
        {
            resisted = false;
        }

        if (resisted)
        {
            if (player != null)
            {
                player.SendChatMessage(
                    targetCreature,
                    $"{targetCreature.Name} resists your spell",
                    ChatMessageType.Magic
                );

                player.Session.Network.EnqueueSend(new GameMessageSound(player.Guid, Sound.ResistSpell));
            }

            if (targetPlayer != null)
            {
                if (targetPlayer.Reprisal)
                {
                    targetPlayer.Reprisal = false;
                    targetPlayer.SendChatMessage(
                        this,
                        $"Reprisal! You resist the spell cast by {Name}",
                        ChatMessageType.Magic
                    );
                }
                targetPlayer.SendChatMessage(this, $"You resist the spell cast by {Name}", ChatMessageType.Magic);

                targetPlayer.Session.Network.EnqueueSend(
                    new GameMessageSound(targetPlayer.Guid, Sound.ResistSpell)
                );

                if (casterCreature != null)
                {
                    targetPlayer.SetCurrentAttacker(casterCreature);
                }

                Proficiency.OnSuccessUse(targetPlayer, targetPlayer.GetCreatureSkill(Skill.MagicDefense), magicSkill);
            }

            if (this is Creature creature)
            {
                targetCreature.EmoteManager.OnResistSpell(creature);
            }
        }

        if (player != null && player.DebugDamage.HasFlag(Creature.DebugDamageType.Attacker))
        {
            ShowResistInfo(player, this, target, spell, magicSkill, difficulty, resistChance, resisted);
        }
        if (targetCreature.DebugDamage.HasFlag(Creature.DebugDamageType.Defender))
        {
            ShowResistInfo(targetCreature, this, target, spell, magicSkill, difficulty, resistChance, resisted);
        }

        return resisted;
    }

    /// <summary>
    /// Returns the caster's effective magic skill for a spell, as used to resist it:
    /// the caster's magic skill for the spell's school (with armor mods), plus proc or weapon spellcraft,
    /// the weapon's attack mod, Overload Discharge, Focus and level scaling.
    /// An item that casts on its own uses its spellcraft, averaged in with its wielder's skill when wielded.
    /// </summary>
    public uint GetEffectiveMagicSkill(
        WorldObject target,
        Spell spell,
        WorldObject itemCaster = null,
        int? weaponSpellcraft = null,
        double? weaponAttackMod = null
    )
    {
        uint magicSkill = 0;

        var caster = itemCaster ?? this;

        if (caster is Creature casterCreature)
        {
            magicSkill = casterCreature.GetModdedMagicSkill(spell.School);

            // Retrieve caster's secondary attribute mod (1% per 20 attributes)
            var secondaryAttributeMod = casterCreature.Focus.Current * 0.0005 + 1;

            // if proc spell or enchanted blade spell
            if (weaponSpellcraft is not null)
            {
                magicSkill += GetSpellcraftSkillBonus(weaponSpellcraft.Value, casterCreature);
            }

            if (weaponAttackMod is not null)
            {
                magicSkill = (uint)(magicSkill * weaponAttackMod);
            }

            // COMBAT ABILITY - Overload Discharge: magic skill increased by the discharged charge (up to 100%)
            if (casterCreature is Player { OverloadDischargeIsActive: true } casterPlayer)
            {
                magicSkill = (uint)(magicSkill * (1.0f + casterPlayer.DischargeLevel));
            }

            magicSkill = (uint)(
                magicSkill
                * secondaryAttributeMod
                * LevelScaling.GetPlayerAttackSkillScalar(casterCreature, target as Creature)
            );
        }
        else if (caster.ItemSpellcraft != null)
        {
            // When an item with spellcraft casts a spell while being wielded by a creature, the wielder's magic skill gets the spellcraft bonus
            if (caster.Wielder is Creature wielder)
            {
                var casterMagicSkill =
                    spell.School == MagicSchool.WarMagic
                        ? wielder.GetModdedWarMagicSkill()
                        : wielder.GetModdedLifeMagicSkill();

                magicSkill = casterMagicSkill + GetSpellcraftSkillBonus(caster.ItemSpellcraft.Value, wielder);
            }
        }
        else if (caster.Wielder is Creature wielder)
        {
            // Receive wielder's skill level in the Magic School?
            magicSkill = wielder.GetCreatureSkill(spell.School).Current;
        }

        return magicSkill;
    }

    /// <summary>
    /// COMBAT ABILITY - Reflect: Gain 50% increased magic defense while attempting to resist a spell
    /// </summary>
    private static float CheckForCombatAbilityReflectMagicDefBonus(Player targetPlayer)
    {
        return targetPlayer is { ReflectIsActive: true } ? 0.5f : 0.0f;
    }

    /// <summary>
    /// The magic skill bonus from an item's spellcraft: 10% of the spellcraft,
    /// including the wielder's Arcane Lore spec bonus
    /// </summary>
    public static uint GetSpellcraftSkillBonus(int itemSpellcraft, Creature wielder)
    {
        return (uint)((itemSpellcraft + CheckForArcaneLoreSpecSpellcraftBonus(wielder)) * 0.1);
    }

    /// <summary>
    /// SPEC BONUS - Arcane Lore: 50% of skill is added to Spellcraft
    /// </summary>
    public static uint CheckForArcaneLoreSpecSpellcraftBonus(Creature wielder)
    {
        if (wielder == null)
        {
            return 0;
        }

        var arcaneLore = wielder.GetCreatureSkill(Skill.ArcaneLore);

        if (arcaneLore.AdvancementClass == SkillAdvancementClass.Specialized)
        {
            return (uint)(arcaneLore.Current * 0.5);
        }

        return 0;
    }

    private static void ShowResistInfo(
        Creature observed,
        WorldObject attacker,
        WorldObject defender,
        Spell spell,
        uint attackSkill,
        uint defenseSkill,
        float resistChance,
        bool resisted
    )
    {
        var targetInfo = PlayerManager.GetOnlinePlayer(observed.DebugDamageTarget);

        if (targetInfo == null)
        {
            observed.DebugDamage = Creature.DebugDamageType.None;
            return;
        }

        // initial info / resist chance
        var info = $"Attacker: {attacker.Name} ({attacker.Guid})\n";
        info += $"Defender: {defender.Name} ({defender.Guid})\n";

        info += $"CombatType: Magic\n";

        info += $"Spell: {spell.Name} ({spell.Id})\n";

        info += $"EffectiveAttackSkill: {attackSkill}\n";
        info += $"EffectiveDefenseSkill: {defenseSkill}\n";

        info += $"ResistChance: {resistChance}\n";

        info += $"Resisted: {resisted}";

        if (resisted || spell.NumProjectiles == 0)
        {
            targetInfo.Session.Network.EnqueueSend(new GameMessageSystemChat(info, ChatMessageType.Broadcast));
        }
        else
        {
            targetInfo.DebugDamageBuffer = $"{info}\n";
        }
    }

    /// <summary>
    /// COMBAT ABILITY - Reflect: Reflect resisted spells back to the caster.
    /// During the guaranteed window, or by consuming a guaranteed charge, spells that were not resisted are reflected as well.
    /// </summary>
    /// <returns>TRUE if the spell is reflected. A reflected spell must not damage the reflecting player.</returns>
    protected static bool CheckForCombatAbilityReflectSpell(bool resisted, Player targetPlayer, Creature sourceCreature, Spell spell)
    {
        if (targetPlayer is not { ReflectIsActive: true } || sourceCreature == null || targetPlayer == sourceCreature || spell.IsBeneficial)
        {
            return false;
        }

        // guaranteed charges are only spent on spells that would otherwise have hit
        if (!resisted && !targetPlayer.ReflectGuaranteedWindowActive)
        {
            if (targetPlayer.ReflectGuaranteedCharges <= 0)
            {
                return false;
            }

            targetPlayer.ReflectGuaranteedCharges--;
        }

        targetPlayer.SendChatMessage(
            sourceCreature,
            $"Reflect! You reflect {spell.Name} back at {sourceCreature.Name}!",
            ChatMessageType.Magic
        );

        targetPlayer.SetCurrentAttacker(sourceCreature);

        return true;
    }

    /// <summary>
    /// COMBAT ABILITY - Reflect: Casts a reflected spell back at its original caster.
    /// The spell is cast by this player (kill credit, threat, messages),
    /// but its resist check and damage are based on the original caster's stats.
    /// </summary>
    /// <param name="originalProjectile">For projectile spells, the incoming projectile that was reflected</param>
    public void CastReflectedSpell(Spell spell, Creature originalCaster, SpellProjectile originalProjectile, double damageMultiplier = 1.0)
    {
        if (originalCaster == null || !originalCaster.IsAlive)
        {
            return;
        }

        switch (spell.MetaSpellType)
        {
            case SpellType.Projectile:
            case SpellType.LifeProjectile:
            case SpellType.EnchantmentProjectile:

                // resisted on impact, see SpellProjectile.CalculateDamage()
                HandleCastSpell_Projectile(
                    spell,
                    originalCaster,
                    null,
                    null,
                    false,
                    false,
                    null,
                    originalProjectile?.DamageMultiplier ?? damageMultiplier,
                    originalCaster,
                    originalProjectile?.LifeProjectileDamage ?? 0
                );
                break;

            case SpellType.Boost:
            case SpellType.FellowBoost:

                if (TryResistSpell(originalCaster, spell, out _, originalCaster, false, null, null, true))
                {
                    return;
                }

                HandleCastSpell_Boost(spell, originalCaster, false, true, null, damageMultiplier, originalCaster);
                break;

            default:
                return;
        }

        DoSpellEffects(spell, this, originalCaster);
    }

    /// <summary>
    /// If resist succeeded, determine if resist was partial or full.
    /// </summary>
    protected static float GetResistedMod(PartialEvasion partialEvasion)
    {
        switch (partialEvasion)
        {
            case PartialEvasion.None:
                return 1.0f;
            case PartialEvasion.Some:
                return 0.5f;
            case PartialEvasion.All:
            default:
                return 0.0f;
        }
    }
}
