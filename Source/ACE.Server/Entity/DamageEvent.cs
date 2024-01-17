using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using ACE.Common;
using ACE.DatLoader.Entity.AnimationHooks;
using ACE.Entity.Enum;
using ACE.Entity.Models;
using ACE.Server.Managers;
using ACE.Server.Network.GameMessages.Messages;
using ACE.Server.WorldObjects;
using Serilog;

namespace ACE.Server.Entity
{
    public class DamageEvent
    {
        private readonly ILogger _log = Log.ForContext<DamageEvent>();

        // factors:
        // - lifestone protection
        // - evade
        //   - offense mod (heart seeker)
        //      - accuracy mod (missile)
        //   - defense mod (defender)
        //      - stamina mod
        // - base damage / mod
        // - damage rating / mod
        //   - recklessness
        //   - sneak attack
        //   - heritage bonus
        // - damage resistance rating /mod
        // - power meter mod
        // - critical (chance % mod / critical damage mod)
        // - attribute mod
        // - armor / mod (base al, impen / bane, life armor / imperil)
        // - aegis / mod
        // - elemental damage bonus
        // - slayer mod
        // - resistance mod (natural, prot, vuln)
        //   - resistance cleaving
        // - shield mod
        // - rending mod

        public Creature Attacker;
        public Creature Defender;

        public CombatType CombatType;   // melee / missile / magic

        public WorldObject DamageSource;
        public DamageType DamageType;

        public WorldObject Weapon;      // the attacker's weapon. this can be different from DamageSource,
                                        // ie. for a missile attack, the missile would the DamageSource,
                                        // and the buffs would come from the Weapon

        public AttackType AttackType;   // slash / thrust / punch / kick / offhand / multistrike
        public AttackHeight AttackHeight;

        public bool LifestoneProtection;

        public float EvasionChance;
        public uint EffectiveAttackSkill;
        public uint EffectiveDefenseSkill;
        public float AccuracyMod;

        public bool Blocked;
        public bool Evaded;
        public PartialEvasion PartialEvasion;

        public BaseDamageMod BaseDamageMod;
        public float BaseDamage { get; set; }

        public float AttributeMod;
        public float PowerMod;
        public float SlayerMod;

        public float DamageRatingBaseMod;
        public float RecklessnessMod;
        public float SneakAttackMod;
        public float HeritageMod;
        public float PkDamageMod;

        public float DamageRatingMod;

        public bool IsCritical;

        public float CriticalChance;
        public float CriticalDamageMod;

        public float CriticalDamageRatingMod;
        public float CriticalDamageResistanceRatingMod;

        public float DamageBeforeMitigation;

        public float ArmorMod;
        public float ResistanceMod;
        public float ShieldMod;
        public float WeaponResistanceMod;

        public float DamageResistanceRatingBaseMod;
        public float DamageResistanceRatingMod;
        public float PkDamageResistanceMod;

        public float DamageMitigated;

        // creature attacker
        public MotionCommand? AttackMotion;
        public AttackHook AttackHook;
        public KeyValuePair<CombatBodyPart, PropertiesBodyPart> AttackPart;      // the body part this monster is attacking with

        // creature defender
        public Quadrant Quadrant;

        public bool IgnoreMagicArmor =>  (Weapon?.IgnoreMagicArmor ?? false) || (Attacker?.IgnoreMagicArmor ?? false);      // ignores impen / banes

        public bool IgnoreMagicResist => (Weapon?.IgnoreMagicResist ?? false) || (Attacker?.IgnoreMagicResist ?? false);    // ignores life armor / prots

        public bool Overpower;


        // player defender
        public BodyPart BodyPart;
        public List<WorldObject> Armor;

        // creature defender
        public KeyValuePair<CombatBodyPart, PropertiesBodyPart> PropertiesBodyPart;
        public Creature_BodyPart CreaturePart;

        public float Damage;

        public bool GeneralFailure;

        public bool HasDamage => !Evaded && !LifestoneProtection;

        public bool CriticalDefended;

        public static HashSet<uint> AllowDamageTypeUndef = new HashSet<uint>()
        {
            22545,  // Obsidian Spines
            35191,  // Thunder Chicken
            38406,  // Blessed Moar
            38587,  // Ardent Moar
            38588,  // Blessed Moar
            38586,  // Verdant Moar
            40298,  // Ardent Moar
            40300,  // Blessed Moar
            40301,  // Verdant Moar
        };

        public static DamageEvent CalculateDamage(Creature attacker, Creature defender, WorldObject damageSource, MotionCommand? attackMotion = null, AttackHook attackHook = null)
        {
            var damageEvent = new DamageEvent();
            damageEvent.AttackMotion = attackMotion;
            damageEvent.AttackHook = attackHook;
            if (damageSource == null)
                damageSource = attacker;

            var damage = damageEvent.DoCalculateDamage(attacker, defender, damageSource);

            damageEvent.HandleLogging(attacker, defender);

            //Console.WriteLine(damageEvent.Evaded);

            return damageEvent;
        }

        private float DoCalculateDamage(Creature attacker, Creature defender, WorldObject damageSource)
        {
            var playerAttacker = attacker as Player;
            var playerDefender = defender as Player;

            var pkBattle = playerAttacker != null && playerDefender != null;

            Attacker = attacker;
            Defender = defender;

            CombatType = damageSource.ProjectileSource == null ? CombatType.Melee : CombatType.Missile;

            DamageSource = damageSource;

            Weapon = damageSource.ProjectileSource == null ? attacker.GetEquippedMeleeWeapon() : (damageSource.ProjectileLauncher ?? damageSource.ProjectileAmmo);

            AttackType = attacker.AttackType;
            AttackHeight = attacker.AttackHeight ?? AttackHeight.Medium;

            // ---- COMBAT TECHNIQUE REF ----
            GetCombatAbilities(attacker, defender, out var attackerCombatAbility, out var defenderCombatAbility);

            // ---- SNEAKING? ----
            var isAttackFromSneaking = false;
            if (playerAttacker != null)
            {
                isAttackFromSneaking = playerAttacker.IsAttackFromStealth;
                playerAttacker.IsAttackFromStealth = false;
            }

            // ---- LIFESTONE PROTECTION ----
            if (playerDefender != null && playerDefender.UnderLifestoneProtection)
            {
                LifestoneProtection = true;
                playerDefender.HandleLifestoneProtection();
                return 0.0f;
            }

            if (defender.Invincible)
                return 0.0f;

            // ---- OVERPOWER ----
            if (attacker.Overpower != null)
                Overpower = Creature.GetOverpower(attacker, defender);

            // ---- BLOCK ----
            Blocked = IsBlocked(attacker, defender);

            // ---- EVASION ----
            var evasionMod = GetEvasionMod(attacker, defender);
            //Console.WriteLine(EvasionChance + " " + partialEvasion);

            // ---- BASE DAMAGE ----
            if (playerAttacker != null)
                GetBaseDamage(playerAttacker);
            else
                GetBaseDamage(attacker, AttackMotion ?? MotionCommand.Invalid, AttackHook);

            if (DamageType == DamageType.Undef)
            {
                if ((attacker?.Guid.IsPlayer() ?? false) || (damageSource?.Guid.IsPlayer() ?? false))
                {
                    _log.Error($"DamageEvent.DoCalculateDamage({attacker?.Name} ({attacker?.Guid}), {defender?.Name} ({defender?.Guid}), {damageSource?.Name} ({damageSource?.Guid})) - DamageType == DamageType.Undef");
                    GeneralFailure = true;
                }
            }

            if (GeneralFailure) return 0.0f;

            // ---- DAMAGE RATING ----
            PowerMod = attacker.GetPowerMod(Weapon);

            AttributeMod = attacker.GetAttributeMod(Weapon);

            SlayerMod = WorldObject.GetWeaponCreatureSlayerModifier(Weapon, attacker, defender);

            DamageRatingBaseMod = Creature.GetPositiveRatingMod(attacker.GetDamageRating());

            RecklessnessMod = Creature.GetRecklessnessMod(attacker, defender);

            SneakAttackMod = attacker.GetSneakAttackMod(defender, out var backstabMod);
            var backstabPenalty = backstabMod > 0.0f ? 0.8f : 1.0f;

            var powershotMod = attacker.IsPowerShot(Weapon, attackerCombatAbility) ? 2.0f : 1.0f;

            HeritageMod = attacker.GetHeritageBonus(Weapon) ? 1.05f : 1.0f;

            // ATTACK HEIGHT BONUS: High (10 damage rating, 20 if weapon is specialized)
            var extraDamageMod = 1.0f;
            if (playerAttacker != null)
            {
                if (playerAttacker.AttackHeight == AttackHeight.High)
                {
                    if (WeaponIsSpecialized(playerAttacker))
                        extraDamageMod += 0.20f;
                    else
                        extraDamageMod += 0.10f;
                }
            }

            // COMBAT ABILITY - Reckless (20 damage rating, if active) 
            var recklessMod = GetRecklessMod(attacker, defender, attackerCombatAbility);

            // Dual Wield Damage Mod
            var dualWieldDamageMod = 1.0f;
            if (playerAttacker != null && playerAttacker.IsDualWieldAttack && !playerAttacker.DualWieldAlternate)
                dualWieldDamageMod = playerAttacker.GetDualWieldDamageMod();

            // Two-handed Combat Damage Mod
            var twohandedCombatDamageMod = 1.0f;
            if (playerAttacker != null && playerAttacker.GetEquippedWeapon() != null)
                if(playerAttacker.GetEquippedWeapon().W_WeaponType == WeaponType.TwoHanded)
                    twohandedCombatDamageMod = playerAttacker.GetTwoHandedCombatDamageMod();

            DamageRatingMod = Creature.AdditiveCombine(DamageRatingBaseMod, RecklessnessMod, SneakAttackMod, HeritageMod, extraDamageMod, recklessMod);

            if (pkBattle)
            {
                PkDamageMod = Creature.GetPositiveRatingMod(attacker.GetPKDamageRating());
                DamageRatingMod = Creature.AdditiveCombine(DamageRatingMod, PkDamageMod);
            }

            // ---- DAMAGE BEFORE MITIGATION ----
            DamageBeforeMitigation = BaseDamage * AttributeMod * PowerMod * SlayerMod * DamageRatingMod * powershotMod * dualWieldDamageMod * twohandedCombatDamageMod;

            // ---- CRIT ----
            var attackSkill = attacker.GetCreatureSkill(attacker.GetCurrentWeaponSkill());

            CriticalChance = WorldObject.GetWeaponCriticalChance(Weapon, attacker, attackSkill, defender);

            if (playerAttacker != null)
            {
                // Backstab combat ability bonus
                CriticalChance += backstabMod;

                // Iron Fist combat ability bonus
                if (attackerCombatAbility == CombatAbility.IronFist)
                    CriticalChance += 0.1f;

                if (CombatType == CombatType.Missile)
                {
                    // critical chance bonus from accuracy bar
                    CriticalChance += playerAttacker.GetAccuracyCritChanceMod(Weapon);
                }
                        
                if (isAttackFromSneaking)
                {
                    CriticalChance = 1.0f;
                    if (playerDefender == null)
                        SneakAttackMod = 3.0f;
                }

                // SPEC BONUS: Martial Weapons (Axe) - +10% crit chance (additively)
                if (playerAttacker.GetEquippedWeapon() != null)
                    if (playerAttacker.GetEquippedWeapon().WeaponSkill == Skill.Axe && playerAttacker.GetCreatureSkill(Skill.HeavyWeapons).AdvancementClass == SkillAdvancementClass.Specialized)
                        CriticalChance += 0.1f;

                // SPEC BONUS: Dagger - +10% crit chance (additively)
                if (playerAttacker.GetEquippedWeapon() != null)
                    if (playerAttacker.GetEquippedWeapon().WeaponSkill == Skill.Dagger && playerAttacker.GetCreatureSkill(Skill.Dagger).AdvancementClass == SkillAdvancementClass.Specialized)
                        CriticalChance += 0.1f;
            }

            // https://asheron.fandom.com/wiki/Announcements_-_2002/08_-_Atonement
            // It should be noted that any time a character is logging off, PK or not, all physical attacks against them become automatically critical.
            // (Note that spells do not share this behavior.) We hope this will stress the need to log off in a safe place.

            if (playerDefender != null && (playerDefender.IsLoggingOut || playerDefender.PKLogout))
                CriticalChance = 1.0f;

            // Cannot crit if Phalanx is equipped
            if (defenderCombatAbility == CombatAbility.Phalanx)
                CriticalChance = 0.0f;

            if (CriticalChance > ThreadSafeRandom.Next(0.0f, 1.0f))
            {
                if (playerDefender != null && playerDefender.AugmentationCriticalDefense > 0)
                {
                    var criticalDefenseMod = playerAttacker != null ? 0.05f : 0.25f;
                    var criticalDefenseChance = playerDefender.AugmentationCriticalDefense * criticalDefenseMod;

                    if (criticalDefenseChance > ThreadSafeRandom.Next(0.0f, 1.0f))
                        CriticalDefended = true;
                }

                if (!CriticalDefended)
                {
                    IsCritical = true;

                    // verify: CriticalMultiplier only applied to the additional crit damage,
                    // whereas CD/CDR applied to the total damage (base damage + additional crit damage)
                    CriticalDamageMod = 1.0f + WorldObject.GetWeaponCritDamageMod(Weapon, attacker, attackSkill, defender);

                    // Iron Fist combat ability penalty
                    if (attackerCombatAbility == CombatAbility.IronFist)
                        CriticalDamageMod -= 0.2f;

                    if (playerAttacker != null && playerAttacker.GetEquippedWeapon() != null)
                    {
                        // SPEC BONUS: Martial Weapons (Mace) - +100% crit damage (additively)
                        if (playerAttacker.GetEquippedWeapon().WeaponSkill == Skill.Mace && playerAttacker.GetCreatureSkill(Skill.HeavyWeapons).AdvancementClass == SkillAdvancementClass.Specialized)
                            CriticalDamageMod += 1.0f;

                        // SPEC BONUS: Staff - +100% crit damage (additively)
                        if (playerAttacker.GetEquippedWeapon().WeaponSkill == Skill.Staff && playerAttacker.GetCreatureSkill(Skill.Staff).AdvancementClass == SkillAdvancementClass.Specialized)
                            CriticalDamageMod += 1.0f;
                    }

                    if (CombatType == CombatType.Missile && playerAttacker != null)
                    {
                        CriticalDamageMod += playerAttacker.GetAccuracyCritDamageMod(Weapon);
                    }

                    CriticalDamageRatingMod = Creature.GetPositiveRatingMod(attacker.GetCritDamageRating());

                    // recklessness excluded from crits
                    RecklessnessMod = 1.0f;
                    DamageRatingMod = Creature.AdditiveCombine(DamageRatingBaseMod, CriticalDamageRatingMod, SneakAttackMod, HeritageMod, extraDamageMod);

                    if (pkBattle)
                        DamageRatingMod = Creature.AdditiveCombine(DamageRatingMod, PkDamageMod);

                    DamageBeforeMitigation = BaseDamageMod.MaxDamage * AttributeMod * PowerMod * SlayerMod * DamageRatingMod * CriticalDamageMod;
                }
            }

            // ---- ARMOR ----
            var armorRendingMod = 1.0f;
            if (Weapon != null && Weapon.HasImbuedEffect(ImbuedEffectType.ArmorRending))
                armorRendingMod = 1.0f - WorldObject.GetArmorRendingMod(attackSkill);

            var armorCleavingMod = attacker.GetArmorCleavingMod(Weapon);

            var ignoreArmorMod = Math.Min(armorRendingMod, armorCleavingMod);

            if (playerAttacker != null && playerAttacker.GetEquippedWeapon() != null)
            {
                // SPEC BONUS: Two-handed combat (Spear) - +10% armor penetration (additively)
                if (playerAttacker.GetEquippedWeapon().W_WeaponType == WeaponType.TwoHanded && Weapon.WeaponSkill == Skill.Spear && playerAttacker.GetCreatureSkill(Skill.TwoHandedCombat).AdvancementClass == SkillAdvancementClass.Specialized)
                    ignoreArmorMod -= 0.1f;

                // SPEC BONUS: Martial Weapons (Spear) - +10% armor penetration (additively)
                if (playerAttacker.GetEquippedWeapon().WeaponSkill == Skill.Spear && playerAttacker.GetCreatureSkill(Skill.HeavyWeapons).AdvancementClass == SkillAdvancementClass.Specialized)
                    ignoreArmorMod -= 0.1f;
            }

            if (playerDefender != null)
            {
                // select random body part @ current attack height
                GetBodyPart(AttackHeight);

                // get player armor pieces
                Armor = attacker.GetArmorLayers(playerDefender, BodyPart);

                // get armor modifiers
                ArmorMod = attacker.GetArmorMod(playerDefender, DamageType, Armor, Weapon, ignoreArmorMod);
            }
            else
            {
                // determine height quadrant
                Quadrant = GetQuadrant(Defender, Attacker, AttackHeight, DamageSource);

                // select random body part @ current attack height
                GetBodyPart(Defender, Quadrant);
                if (Evaded || Blocked)
                    return 0.0f;

                Armor = CreaturePart.GetArmorLayers(PropertiesBodyPart.Key);

                // get target armor
                ArmorMod = CreaturePart.GetArmorMod(DamageType, Armor, Attacker, Weapon, ignoreArmorMod);
            }

            if (Weapon != null && Weapon.HasImbuedEffect(ImbuedEffectType.IgnoreAllArmor))
                ArmorMod = 1.0f;

            // ---- RESISTANCE ----
            WeaponResistanceMod = WorldObject.GetWeaponResistanceModifier(Weapon, attacker, attackSkill, DamageType);

            if (playerDefender != null)
            {
                ResistanceMod = playerDefender.GetResistanceMod(DamageType, Attacker, Weapon, WeaponResistanceMod);
            }
            else
            {
                var resistanceType = Creature.GetResistanceType(DamageType);
                ResistanceMod = (float)Math.Max(0.0f, defender.GetResistanceMod(resistanceType, Attacker, Weapon, WeaponResistanceMod));
            }

            // ---- DAMAGE RESIST RATING ----
            DamageResistanceRatingMod = DamageResistanceRatingBaseMod = defender.GetDamageResistRatingMod(CombatType);

            if (IsCritical)
            {
                CriticalDamageResistanceRatingMod = Creature.GetNegativeRatingMod(defender.GetCritDamageResistRating());
                DamageResistanceRatingMod = Creature.AdditiveCombine(DamageResistanceRatingBaseMod, CriticalDamageResistanceRatingMod);
            }

            if (pkBattle)
            {
                PkDamageResistanceMod = Creature.GetNegativeRatingMod(defender.GetPKDamageResistRating());
                DamageResistanceRatingMod = Creature.AdditiveCombine(DamageResistanceRatingMod, PkDamageResistanceMod);
            }

            // SPEC BONUS: Physical Defense
            var specDefenseMod = 1.0f;
            if(playerDefender != null && playerDefender.GetCreatureSkill(Skill.MeleeDefense).AdvancementClass == SkillAdvancementClass.Specialized)
            {
                var physicalDefenseSkill = playerDefender.GetCreatureSkill(Skill.MeleeDefense);
                var bonusAmount = (float)Math.Min(physicalDefenseSkill.Current, 500) / 50;

                specDefenseMod = 0.9f - bonusAmount * 0.01f;
            }

            // ---- SHIELD ----
            ShieldMod = defender.GetShieldMod(attacker, DamageType, Weapon);

            // ---- FINAL CALCULATIONS ----
            Damage = DamageBeforeMitigation * ArmorMod * ShieldMod * ResistanceMod * DamageResistanceRatingMod * evasionMod * backstabPenalty * specDefenseMod;
            DamageMitigated = DamageBeforeMitigation - Damage;

            // ---- OPTIONAL GLOBAL MULTIPLIERS FOR PLAYERS or MONSTERS ----
            if(attacker.IsMonster)
                Damage *= 1.0f;

            if(!attacker.IsMonster)
                Damage *= 1.0f;

            return Damage;
        }

        public Quadrant GetQuadrant(Creature defender, Creature attacker, AttackHeight attackHeight, WorldObject damageSource)
        {
            var quadrant = attackHeight.ToQuadrant();

            var wo = damageSource.CurrentLandblock != null ? damageSource : attacker;

            quadrant |= wo.GetRelativeDir(defender);

            return quadrant;
        }

        /// <summary>
        /// Returns the chance for creature to avoid monster attack
        /// </summary>
        public float GetEvadeChance(Creature attacker, Creature defender)
        {
            Player playerAttacker = attacker as Player;
            Player playerDefender = defender as Player;
            bool isPvP = playerAttacker != null && playerDefender != null;

            AccuracyMod = attacker.GetAccuracySkillMod(Weapon);

            EffectiveAttackSkill = attacker.GetEffectiveAttackSkill();

            //var attackType = attacker.GetCombatType();

            EffectiveDefenseSkill = defender.GetEffectiveDefenseSkill(CombatType);

            GetCombatAbilities(attacker, defender, out var attackerCombatAbility, out var defenderCombatAbility);

            // ATTACK HEIGHT BONUS: Medium (+10% attack skill, +20% if weapon specialized)
            if (playerAttacker != null)
            {
                if (playerAttacker.AttackHeight == AttackHeight.Medium)
                {
                    float bonus;

                    if (WeaponIsSpecialized(playerAttacker))
                        bonus = 1.2f;
                    else
                        bonus = 1.1f;

                    EffectiveAttackSkill = (uint)Math.Round(EffectiveAttackSkill * bonus);
                }
            }

            // ATTACK HEIGHT BONUS: Low (+10% physical defense skill, +20% if weapon specialized)
            if (playerDefender != null)
            {
                if (playerDefender != null && playerDefender.AttackHeight == AttackHeight.Low) 
                {
                    float bonus;

                    if (WeaponIsSpecialized(playerAttacker))
                        bonus = 1.2f;
                    else
                        bonus = 1.1f;

                    EffectiveDefenseSkill = (uint)Math.Round(EffectiveDefenseSkill * bonus);
                }
            }

            var evadeChance = 1.0f - SkillCheck.GetSkillChance(EffectiveAttackSkill, EffectiveDefenseSkill);

            // Combat Focus - Smokescreen (+10% chance to evade)
            if (defenderCombatAbility == CombatAbility.Smokescreen)
                evadeChance += 0.1f; // Gain 10% evade chance

            return (float)Math.Min(evadeChance, 1.0f);
        }

        /// <summary>
        /// If evade succeeded, determine if evade was partial or full. Return true if full.
        /// </summary>
        private bool GetEvadedMod(Creature attacker, Creature defender, out float evasionMod)
        {
            evasionMod = 1.0f;

            if (attacker != defender && EvasionChance > ThreadSafeRandom.Next(0.0f, 1.0f))
            {
                var fullEvade = EvasionChance / 3.0f;
                var mostEvade = fullEvade * 2.0f;
                var someEvade = fullEvade * 3.0f;

                var attackRoll = ThreadSafeRandom.Next(0.0f, 1.0f);

                //Console.WriteLine($"BaseEvasionChance: {Math.Round(EvasionChance * 100)}% AttackRoll: {Math.Round(attackRoll * 100)}");
                //Console.WriteLine($"FullEvadeChance: {Math.Round(fullEvade * 100)}% MostEvadeChance: {Math.Round(mostEvade * 100)}% SomeEvadeChance: {Math.Round(someEvade * 100)}");

                // full evade
                if (attacker != defender && fullEvade > attackRoll)
                {
                    //Console.WriteLine($"Full Evade");
                    Evaded = true;
                    return true;
                }
                // partial evade
                else
                {
                    GetCombatAbilities(attacker, defender, out var attackerCombatAbility, out var defenderCombatAbility);

                    if (mostEvade > attackRoll) // Evaded most of
                    {
                        //Console.WriteLine($"Most Evade");

                        if (defenderCombatAbility == CombatAbility.Reckless) // Partial evades are always "Some"
                        {
                            evasionMod = 1 - 0.67f;
                            PartialEvasion = PartialEvasion.Some;
                        }
                        else
                        {
                            evasionMod = 1 - 0.33f;
                            PartialEvasion = PartialEvasion.Most;
                        }
                    }
                    else if (someEvade > attackRoll) // Evaded some of
                    {
                        //Console.WriteLine($"Some Evade");

                        if (defenderCombatAbility == CombatAbility.Provoke || defenderCombatAbility == CombatAbility.Phalanx) // Partial evades are always "Most"
                        {
                            evasionMod = 1 - 0.33f;
                            PartialEvasion = PartialEvasion.Most;
                        }
                        else
                        { 
                            evasionMod = 1 - 0.67f;
                            PartialEvasion = PartialEvasion.Some;
                        }
                    }
                }
                //Console.WriteLine($"EvasionMod: {Math.Round(evasionMod * 100)}%");
                //if (!attacker.IsMonster)
                //{
                //    Console.WriteLine($"{attacker.Name} vs {defender.Name}:\n" +
                //        $" -AttackRoll: {Math.Round(attackRoll * 100)}\n" +
                //        $" -EvasionChance: {Math.Round(EvasionChance * 100)}%\n" +
                //        $" -FullEvadeChance: {Math.Round(fullEvade * 100)}%\n" +
                //        $" -EvasionMod: {Math.Round(evasionMod * 100)}% (IF PARTIAL EVADE)");
                //}
            }
            return false;
        }

        private bool IsBlocked(Creature attacker, Creature defender)
        {
            var blockChance = 0.0f;

            var combatAbility = CombatAbility.None;
            var combatFocus = defender.GetEquippedCombatFocus();
            if (combatFocus != null)
                combatAbility = combatFocus.GetCombatAbility();

            var defenderEquippedShield = defender.GetEquippedShield();
            if (defenderEquippedShield != null || defender.GetCreatureSkill(Skill.MeleeDefense).AdvancementClass != SkillAdvancementClass.Specialized)
            {

                Player playerAttacker = attacker as Player;
                Player playerDefender = defender as Player;

                AccuracyMod = attacker.GetAccuracySkillMod(Weapon);
                EffectiveAttackSkill = attacker.GetEffectiveAttackSkill();

                GetCombatAbilities(attacker, defender, out var attackerCombatAbility, out var defenderCombatAbility);

                // ATTACK HEIGHT BONUS: Medium (+10% attack skill, +20% if weapon specialized)
                if (playerAttacker != null)
                {
                    if (playerAttacker.AttackHeight == AttackHeight.Medium)
                    {
                        float bonus;

                        if (WeaponIsSpecialized(playerAttacker))
                            bonus = 1.2f;
                        else
                            bonus = 1.1f;

                        EffectiveAttackSkill = (uint)Math.Round(EffectiveAttackSkill * bonus);
                    }
                }

                if (defenderEquippedShield != null)
                {
                    var shieldArmorLevel = defenderEquippedShield.ArmorLevel ?? 0;

                    var blockChanceMod = SkillCheck.GetSkillChance((uint)shieldArmorLevel, EffectiveAttackSkill);

                    blockChance = 0.1f + 0.1f * (float)blockChanceMod;
                }
            }

            // COMBAT ABILITY - Parry: 20% chance to block attacks while using a two-handed weapon or dual-wielding
            else if (combatAbility == CombatAbility.Parry)
            {
                if (defender.TwoHandedCombat || defender.IsDualWieldAttack)
                    blockChance = 0.2f;
            }

            if (ThreadSafeRandom.Next(0.0f, 1.0f) < blockChance)
                return true;

            return false;
        }

        public static void GetCombatAbilities(Creature attacker, Creature defender, out CombatAbility attackerCombatAbility, out CombatAbility defenderCombatAbility)
        {
            attackerCombatAbility = CombatAbility.None;
            defenderCombatAbility = CombatAbility.None;

            var attackerCombatFocus = attacker.GetEquippedCombatFocus();
            if (attackerCombatFocus != null)
                attackerCombatAbility = attackerCombatFocus.GetCombatAbility();

            var defenderCombatFocus = defender.GetEquippedCombatFocus();
            if (defenderCombatFocus != null)
                defenderCombatAbility = defenderCombatFocus.GetCombatAbility();
        }

        private float GetRecklessMod(Creature attacker, Creature defender, CombatAbility attackerAbility)
        {
            var mod = 1.0f;

            if (attackerAbility == CombatAbility.Reckless)
            {
                if (CombatType == CombatType.Melee || attacker.GetDistance(defender) < 3) 
                {
                    mod = 1.20f; 
                }
            }

            return mod;
        }

        private float GetEvasionMod(Creature attacker, Creature defender)
        {
            var evasionMod = 1.0f;

            if (!Overpower)
            {
                var defenderSkillAmount = defender.GetEffectiveDefenseSkill(CombatType);

                // This optionally adds a curve to how effective evasion can be as a character levels
                //var evasionDefenseMod = 1 - (200 / (200 + defenderSkillAmount));
                //EvasionChance = GetEvadeChance(attacker, defender) * evasionDefenseMod;

                EvasionChance = GetEvadeChance(attacker, defender);

                var evaded = GetEvadedMod(attacker, defender, out evasionMod);
                if (evaded)
                    return 0.0f;
        }

            return evasionMod;
        }

        /// <summary>
        /// Returns the base damage for a player attacker
        /// </summary>
        public void GetBaseDamage(Player attacker)
        {
            if (DamageSource.ItemType == ItemType.MissileWeapon)
            {
                DamageType = DamageSource.W_DamageType;

                // handle prismatic arrows
                if (DamageType == DamageType.Base)
                {
                    if (Weapon != null && Weapon.W_DamageType != DamageType.Undef)
                        DamageType = Weapon.W_DamageType;
                    else
                        DamageType = DamageType.Pierce;
                }
            }
            else
                DamageType = attacker.GetDamageType(false, CombatType.Melee);

            // TODO: combat maneuvers for player?
            BaseDamageMod = attacker.GetBaseDamageMod(DamageSource);

            // some quest bows can have built-in damage bonus
            if (Weapon?.WeenieType == WeenieType.MissileLauncher)
                BaseDamageMod.DamageBonus += Weapon.Damage ?? 0;

            if (DamageSource.ItemType == ItemType.MissileWeapon)
                BaseDamageMod.ElementalBonus = WorldObject.GetMissileElementalDamageBonus(Weapon, attacker, DamageType);

            BaseDamage = (float)ThreadSafeRandom.Next(BaseDamageMod.MinDamage, BaseDamageMod.MaxDamage);
        }

        /// <summary>
        /// Returns the base damage for a non-player attacker
        /// </summary>
        public void GetBaseDamage(Creature attacker, MotionCommand motionCommand, AttackHook attackHook)
        {
            AttackPart = attacker.GetAttackPart(motionCommand, attackHook);
            if (AttackPart.Value == null)
            {
                GeneralFailure = true;
                return;
            }

            BaseDamageMod = attacker.GetBaseDamage(AttackPart.Value);
            BaseDamage = (float)ThreadSafeRandom.Next(BaseDamageMod.MinDamage, BaseDamageMod.MaxDamage);

            DamageType = attacker.GetDamageType(AttackPart.Value, CombatType);
        }

        /// <summary>
        /// Returns a body part for a player defender
        /// </summary>
        public void GetBodyPart(AttackHeight attackHeight)
        {
            // select random body part @ current attack height
            BodyPart = BodyParts.GetBodyPart(attackHeight);
        }

        public static readonly Quadrant LeftRight = Quadrant.Left | Quadrant.Right;
        public static readonly Quadrant FrontBack = Quadrant.Front | Quadrant.Back;

        /// <summary>
        /// Returns a body part for a creature defender
        /// </summary>
        public void GetBodyPart(Creature defender, Quadrant quadrant)
        {
            // get cached body parts table
            var bodyParts = Creature.GetBodyParts(defender.WeenieClassId);

            // rng roll for body part
            var bodyPart = bodyParts.RollBodyPart(quadrant);

            if (bodyPart == CombatBodyPart.Undefined)
            {
                _log.Debug("DamageEvent.GetBodyPart({Defender} ({DefenderGuid}) ) - couldn't find body part for wcid {DefenderWeenieClassId}, Quadrant {BodyPartQuadrant}", defender.Name, defender.Guid, defender.WeenieClassId, quadrant);
                Evaded = true;
                return;
            }

            //Console.WriteLine($"AttackHeight: {AttackHeight}, Quadrant: {quadrant & FrontBack}{quadrant & LeftRight}, AttackPart: {bodyPart}");

            defender.Biota.PropertiesBodyPart.TryGetValue(bodyPart, out var value);
            PropertiesBodyPart = new KeyValuePair<CombatBodyPart, PropertiesBodyPart>(bodyPart, value);

            // select random body part @ current attack height
            /*BiotaPropertiesBodyPart = BodyParts.GetBodyPart(defender, attackHeight);

            if (BiotaPropertiesBodyPart == null)
            {
                Evaded = true;
                return;
            }*/

            CreaturePart = new Creature_BodyPart(defender, PropertiesBodyPart);
        }

        public void ShowInfo(Creature creature)
        {
            var targetInfo = PlayerManager.GetOnlinePlayer(creature.DebugDamageTarget);
            if (targetInfo == null)
            {
                creature.DebugDamage = Creature.DebugDamageType.None;
                return;
            }

            // setup
            var info = $"Attacker: {Attacker.Name} ({Attacker.Guid})\n";
            info += $"Defender: {Defender.Name} ({Defender.Guid})\n";

            info += $"CombatType: {CombatType}\n";

            info += $"DamageSource: {DamageSource.Name} ({DamageSource.Guid})\n";
            info += $"DamageType: {DamageType}\n";

            var weaponName = Weapon != null ? $"{Weapon.Name} ({Weapon.Guid})" : "None\n";
            info += $"Weapon: {weaponName}\n";

            info += $"AttackType: {AttackType}\n";
            info += $"AttackHeight: {AttackHeight}\n";

            // lifestone protection
            if (LifestoneProtection)
                info += $"LifestoneProtection: {LifestoneProtection}\n";

            // evade
            if (AccuracyMod != 0.0f && AccuracyMod != 1.0f)
                info += $"AccuracyMod: {AccuracyMod}\n";

            info += $"EffectiveAttackSkill: {EffectiveAttackSkill}\n";
            info += $"EffectiveDefenseSkill: {EffectiveDefenseSkill}\n";

            if (Attacker.Overpower != null)
                info += $"Overpower: {Overpower} ({Creature.GetOverpowerChance(Attacker, Defender)})\n";

            info += $"EvasionChance: {EvasionChance}\n";
            info += $"Evaded: {Evaded}\n";
            info += $"Blocked: {Blocked}\n";
            info += $"PartialEvaded: {PartialEvasion}\n";

            if (!(Attacker is Player))
            {
                if (AttackMotion != null)
                    info += $"AttackMotion: {AttackMotion}\n";
                if (AttackPart.Value != null)
                    info += $"AttackPart: {AttackPart.Key}\n";
            }

            // base damage
            if (BaseDamageMod != null)
                info += $"BaseDamageRange: {BaseDamageMod.Range}\n";


            info += $"BaseDamage: {BaseDamage}\n";

            // damage modifiers
            info += $"AttributeMod: {AttributeMod}\n";

            if (PowerMod != 0.0f && PowerMod != 1.0f)
                info += $"PowerMod: {PowerMod}\n";

            if (SlayerMod != 0.0f && SlayerMod != 1.0f)
                info += $"SlayerMod: {SlayerMod}\n";

            if (BaseDamageMod != null)
            {
                if (BaseDamageMod.DamageBonus != 0)
                    info += $"DamageBonus: {BaseDamageMod.DamageBonus}\n";

                if (BaseDamageMod.DamageMod != 0.0f && BaseDamageMod.DamageMod != 1.0f)
                    info += $"DamageMod: {BaseDamageMod.DamageMod}\n";

                if (BaseDamageMod.ElementalBonus != 0)
                    info += $"ElementalDamageBonus: {BaseDamageMod.ElementalBonus}\n";
            }

            // critical hit
            info += $"CriticalChance: {CriticalChance}\n";
            info += $"CriticalHit: {IsCritical}\n";

            if (CriticalDefended)
                info += $"CriticalDefended: {CriticalDefended}\n";

            if (CriticalDamageMod != 0.0f && CriticalDamageMod != 1.0f)
                info += $"CriticalDamageMod: {CriticalDamageMod}\n";

            if (CriticalDamageRatingMod != 0.0f && CriticalDamageRatingMod != 1.0f)
                info += $"CriticalDamageRatingMod: {CriticalDamageRatingMod}\n";

            // damage ratings
            if (DamageRatingBaseMod != 0.0f && DamageRatingBaseMod != 1.0f)
                info += $"DamageRatingBaseMod: {DamageRatingBaseMod}\n";

            if (HeritageMod != 0.0f && HeritageMod != 1.0f)
                info += $"HeritageMod: {HeritageMod}\n";

            if (RecklessnessMod != 0.0f && RecklessnessMod != 1.0f)
                info += $"RecklessnessMod: {RecklessnessMod}\n";

            if (SneakAttackMod != 0.0f && SneakAttackMod != 1.0f)
                info += $"SneakAttackMod: {SneakAttackMod}\n";

            if (PkDamageMod != 0.0f && PkDamageMod != 1.0f)
                info += $"PkDamageMod: {PkDamageMod}\n";

            if (DamageRatingMod != 0.0f && DamageRatingMod != 1.0f)
                info += $"DamageRatingMod: {DamageRatingMod}\n";

            if (BodyPart != 0)
            {
                // player body part
                info += $"BodyPart: {BodyPart}\n";
            }
            if (Armor != null && Armor.Count > 0)
            {
                info += $"Armors: {string.Join(", ", Armor.Select(i => i.Name))}\n";
            }

            if (CreaturePart != null)
            {
                // creature body part
                info += $"BodyPart: {PropertiesBodyPart.Key}\n";
                info += $"BaseArmor: {CreaturePart.Biota.Value.BaseArmor}\n";
            }

            // damage mitigation
            if (ArmorMod != 0.0f && ArmorMod != 1.0f)
                info += $"ArmorMod: {ArmorMod}\n";

            if (ResistanceMod != 0.0f && ResistanceMod != 1.0f)
                info += $"ResistanceMod: {ResistanceMod}\n";

            if (ShieldMod != 0.0f && ShieldMod != 1.0f)
                info += $"ShieldMod: {ShieldMod}\n";

            if (WeaponResistanceMod != 0.0f && WeaponResistanceMod != 1.0f)
                info += $"WeaponResistanceMod: {WeaponResistanceMod}\n";

            if (DamageResistanceRatingBaseMod != 0.0f && DamageResistanceRatingBaseMod != 1.0f)
                info += $"DamageResistanceRatingBaseMod: {DamageResistanceRatingBaseMod}\n";

            if (CriticalDamageResistanceRatingMod != 0.0f && CriticalDamageResistanceRatingMod != 1.0f)
                info += $"CriticalDamageResistanceRatingMod: {CriticalDamageResistanceRatingMod}\n";

            if (PkDamageResistanceMod != 0.0f && PkDamageResistanceMod != 1.0f)
                info += $"PkDamageResistanceMod: {PkDamageResistanceMod}\n";

            if (DamageResistanceRatingMod != 0.0f && DamageResistanceRatingMod != 1.0f)
                info += $"DamageResistanceRatingMod: {DamageResistanceRatingMod}\n";

            if (IgnoreMagicArmor)
                info += $"IgnoreMagicArmor: {IgnoreMagicArmor}\n";
            if (IgnoreMagicResist)
                info += $"IgnoreMagicResist: {IgnoreMagicResist}\n";

            // final damage
            info += $"DamageBeforeMitigation: {DamageBeforeMitigation}\n";
            info += $"DamageMitigated: {DamageMitigated}\n";
            info += $"Damage: {Damage}\n";

            info += "----";

            targetInfo.Session.Network.EnqueueSend(new GameMessageSystemChat(info, ChatMessageType.Broadcast));
        }

        public void HandleLogging(Creature attacker, Creature defender)
        {
            if (attacker != null && (attacker.DebugDamage & Creature.DebugDamageType.Attacker) != 0)
            {
                ShowInfo(attacker);
            }
            if (defender != null && (defender.DebugDamage & Creature.DebugDamageType.Defender) != 0)
            {
                ShowInfo(defender);
            }
        }

        public AttackConditions AttackConditions
        {
            get
            {
                var attackConditions = new AttackConditions();

                if (CriticalDefended)
                    attackConditions |= AttackConditions.CriticalProtectionAugmentation;
                if (RecklessnessMod > 1.0f)
                    attackConditions |= AttackConditions.Recklessness;
                if (SneakAttackMod > 1.0f)
                    attackConditions |= AttackConditions.SneakAttack;
                if (Overpower)
                    attackConditions |= AttackConditions.Overpower;

                return attackConditions;
            }
        }
        private bool WeaponIsSpecialized(Player playerAttacker)
        {
            if (playerAttacker != null)
            {
                if (Weapon != null)
                {
                    switch (Weapon.WeaponSkill)
                    {
                        case Skill.Axe: return playerAttacker.GetCreatureSkill(Skill.HeavyWeapons).AdvancementClass == SkillAdvancementClass.Specialized;
                        case Skill.Mace: return playerAttacker.GetCreatureSkill(Skill.HeavyWeapons).AdvancementClass == SkillAdvancementClass.Specialized;
                        case Skill.Sword: return playerAttacker.GetCreatureSkill(Skill.HeavyWeapons).AdvancementClass == SkillAdvancementClass.Specialized;
                        case Skill.Spear: return playerAttacker.GetCreatureSkill(Skill.HeavyWeapons).AdvancementClass == SkillAdvancementClass.Specialized;
                        case Skill.Dagger: return playerAttacker.GetCreatureSkill(Skill.Dagger).AdvancementClass == SkillAdvancementClass.Specialized;
                        case Skill.Staff: return playerAttacker.GetCreatureSkill(Skill.Staff).AdvancementClass == SkillAdvancementClass.Specialized;
                        case Skill.UnarmedCombat: return playerAttacker.GetCreatureSkill(Skill.UnarmedCombat).AdvancementClass == SkillAdvancementClass.Specialized;
                        case Skill.Bow: return playerAttacker.GetCreatureSkill(Skill.Bow).AdvancementClass == SkillAdvancementClass.Specialized;
                        case Skill.Crossbow: return playerAttacker.GetCreatureSkill(Skill.Bow).AdvancementClass == SkillAdvancementClass.Specialized;
                        case Skill.ThrownWeapon: return playerAttacker.GetCreatureSkill(Skill.ThrownWeapon).AdvancementClass == SkillAdvancementClass.Specialized;
                        default: return false;
                    }
                }
                else
                    return playerAttacker.GetCreatureSkill(Skill.UnarmedCombat).AdvancementClass == SkillAdvancementClass.Specialized;
            }
            return false;
        }
    }
}
