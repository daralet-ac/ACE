using ACE.Common;
using ACE.Database.Models.World;
using ACE.Entity.Enum;
using ACE.Server.Factories.Entity;
using ACE.Server.Factories.Enum;
using ACE.Server.Factories.Tables;
using ACE.Server.WorldObjects;
using System;
using System.Linq;

namespace ACE.Server.Factories
{
    public static partial class LootGenerationFactory
    {
        /// <summary>
        /// Creates and optionally mutates a new MeleeWeapon
        /// </summary>
        public static WorldObject CreateMeleeWeapon(TreasureDeath profile, bool isMagical, MeleeWeaponSkill weaponSkill = MeleeWeaponSkill.Undef, bool mutate = true)
        {
            var wcid = 0;
            var weaponType = 0;

            var eleType = ThreadSafeRandom.Next(0, 4);

            if (weaponSkill == MeleeWeaponSkill.Undef)
                weaponSkill = (MeleeWeaponSkill)ThreadSafeRandom.Next(5, 11);

            switch (weaponSkill)                
            {
                case MeleeWeaponSkill.HeavyWeapons:

                    weaponType = ThreadSafeRandom.Next(0, LootTables.HeavyWeaponsMatrix.Length - 1);
                    wcid = LootTables.HeavyWeaponsMatrix[weaponType][eleType];
                    break;

                case MeleeWeaponSkill.LightWeapons:

                    weaponType = ThreadSafeRandom.Next(0, LootTables.LightWeaponsMatrix.Length - 1);
                    wcid = LootTables.LightWeaponsMatrix[weaponType][eleType];
                    break;

                case MeleeWeaponSkill.FinesseWeapons:

                    weaponType = ThreadSafeRandom.Next(0, LootTables.FinesseWeaponsMatrix.Length - 1);
                    wcid = LootTables.FinesseWeaponsMatrix[weaponType][eleType];
                    break;

                case MeleeWeaponSkill.TwoHandedCombat:

                    weaponType = ThreadSafeRandom.Next(0, LootTables.TwoHandedWeaponsMatrix.Length - 1);
                    wcid = LootTables.TwoHandedWeaponsMatrix[weaponType][eleType];
                    break;

                case MeleeWeaponSkill.Axe:

                    weaponType = ThreadSafeRandom.Next(0, LootTables.AxesMatrix.Length - 1);
                    wcid = LootTables.AxesMatrix[weaponType][eleType];
                    break;

                case MeleeWeaponSkill.Dagger:

                    weaponType = ThreadSafeRandom.Next(0, LootTables.DaggersMatrix.Length - 1);
                    wcid = LootTables.DaggersMatrix[weaponType][eleType];
                    break;

                case MeleeWeaponSkill.Mace:

                    weaponType = ThreadSafeRandom.Next(0, LootTables.MacesMatrix.Length - 1);
                    wcid = LootTables.MacesMatrix[weaponType][eleType];
                    break;

                case MeleeWeaponSkill.Spear:

                    weaponType = ThreadSafeRandom.Next(0, LootTables.SpearsMatrix.Length - 1);
                    wcid = LootTables.SpearsMatrix[weaponType][eleType];
                    break;

                case MeleeWeaponSkill.Staff:

                    weaponType = ThreadSafeRandom.Next(0, LootTables.StavesMatrix.Length - 1);
                    wcid = LootTables.StavesMatrix[weaponType][eleType];
                    break;

                case MeleeWeaponSkill.Sword:

                    weaponType = ThreadSafeRandom.Next(0, LootTables.SwordsMatrix.Length - 1);
                    wcid = LootTables.SwordsMatrix[weaponType][eleType];
                    break;

                case MeleeWeaponSkill.UnarmedCombat:

                    weaponType = ThreadSafeRandom.Next(0, LootTables.UnarmedMatrix.Length - 1);
                    wcid = LootTables.UnarmedMatrix[weaponType][eleType];
                    break;
            }

            var wo = WorldObjectFactory.CreateNewWorldObject((uint)wcid);

            if (wo != null && mutate)
            {
                if (!MutateMeleeWeapon(wo, profile, isMagical))
                {
                    _log.Warning($"[LOOT] {wo.WeenieClassId} - {wo.Name} is not a MeleeWeapon");
                    return null;
                }
            }
            return wo;
        }

        private static bool MutateMeleeWeapon(WorldObject wo, TreasureDeath profile, bool isMagical, TreasureRoll roll = null)
        {
            if (!(wo is MeleeWeapon || wo.IsThrownWeapon))
                return false;

            if (roll == null)
            {
                _log.Error($"MutateMeleeWeapon reverting to old method({wo.Name}, {profile.TreasureType}).");
                // previous method
                var wieldDifficulty = RollWieldDifficulty(profile.Tier, TreasureWeaponType.MeleeWeapon);

                if (!MutateStats_OldMethod(wo, profile, wieldDifficulty))
                    return false;
            }
            else
            {
                // thanks to 4eyebiped for helping with the data analysis of magloot retail logs
                // that went into reversing these mutation scripts

                var weaponSkill = wo.WeaponSkill.ToMeleeWeaponSkill();
            }

            // Wield Difficulty
            wo.WieldRequirements = WieldRequirement.RawAttrib;
            wo.WieldDifficulty = RollWieldDifficulty(profile.Tier, TreasureWeaponType.MeleeWeapon);
            wo.WieldSkillType = GetWeaponPrimaryAttribute(wo.WeaponSkill);

            wo.WieldRequirements2 = WieldRequirement.Training;
            wo.WieldDifficulty2 = 1;
            wo.WieldSkillType2 = GetWeaponWieldSkill(wo.WeaponSkill);

            // Max damage
            TryMutateMeleeWeaponDamage(wo, roll, profile, out var maxPossibleDamage);

            // Variance (min damage)
            var baseVariance = wo.DamageVariance ?? 1.0f;
            wo.DamageVariance = baseVariance + ThreadSafeRandom.Next(-0.1f, 0.1f);

            var damagePercentile = ((float)wo.Damage / maxPossibleDamage);

            // weapon speed
            if (wo.WeaponTime != null)
            {
                var weaponSpeedMod = 1.0f;

                weaponSpeedMod += (float)ThreadSafeRandom.Next(-0.05f, 0.05f);

                wo.WeaponTime = (int)(wo.WeaponTime * weaponSpeedMod);
            }

            // weapon mods
            TryMutateWeaponMods(wo, profile, out var modsPercentile);

            TryMutateWeaponSubtypeBonuses(wo, profile, out var subtypeBonusesPercentile);

            // material type
            var materialType = GetMaterialType(wo, profile.Tier);
            if (materialType > 0)
                wo.MaterialType = materialType;

            // item color
            MutateColor(wo);

            // gem count / gem material
            if (wo.GemCode != null)
                wo.GemCount = GemCountChance.Roll(wo.GemCode.Value, profile.Tier);
            else
                wo.GemCount = ThreadSafeRandom.Next(1, 5);

            wo.GemType = RollGemType(profile.Tier);


            // burden
            MutateBurden(wo, profile, true);

            // spells
            AssignMagic(wo, profile, roll, false, isMagical);

            // item value
            //if (wo.HasMutateFilter(MutateFilter.Value))   // fixme: data
                MutateValue(wo, profile.Tier, roll);

            // long description
            wo.LongDesc = GetLongDesc(wo);

            // workmanship
            wo.ItemWorkmanship = GetWeaponWorkmanship(wo, damagePercentile, modsPercentile, subtypeBonusesPercentile);

            wo.BaseDamage = (wo.Damage == null ? 0 : wo.Damage);
            wo.BaseWeaponTime = (wo.WeaponTime == null ? 0 : wo.WeaponTime);
            // assign jewel slots
            AssignJewelSlots(wo);

            return true;
        }

        private static bool MutateStats_OldMethod(WorldObject wo, TreasureDeath profile, int wieldDifficulty)
        {
            var success = false;

            switch (wo.WeaponSkill)
            {
                case Skill.HeavyWeapons:

                    success = MutateHeavyWeapon(wo, profile, wieldDifficulty);
                    break;

                case Skill.LightWeapons:

                    success = MutateLightWeapon(wo, profile, wieldDifficulty);
                    break;

                case Skill.FinesseWeapons:

                    success = MutateFinesseWeapon(wo, profile, wieldDifficulty);
                    break;

                case Skill.TwoHandedCombat:

                    success = MutateTwoHandedWeapon(wo, profile, wieldDifficulty);
                    break;

                case Skill.Axe:

                    success = MutateAxe(wo, profile, wieldDifficulty);
                    break;

                case Skill.Dagger:

                    success = MutateDagger(wo, profile, wieldDifficulty);
                    break;

                case Skill.Mace:

                    success = MutateMace(wo, profile, wieldDifficulty);
                    break;

                case Skill.Spear:

                    success = MutateSpear(wo, profile, wieldDifficulty);
                    break;

                case Skill.Sword:

                    success = MutateSword(wo, profile, wieldDifficulty);
                    break;

                case Skill.Staff:

                    success = MutateStaff(wo, profile, wieldDifficulty);
                    break;

                case Skill.UnarmedCombat:

                    success = MutateUnarmed(wo, profile, wieldDifficulty);
                    break;
            }

            if (!success)
                return false;

            // wield requirements
            if (wieldDifficulty > 0)
            {
                wo.WieldDifficulty = wieldDifficulty;
                wo.WieldRequirements = WieldRequirement.RawSkill;
                wo.WieldSkillType = (int)wo.WeaponSkill;

            }
            else
            {
                // if no wield requirements, clear base
                wo.WieldDifficulty = null;
                wo.WieldRequirements = WieldRequirement.Invalid;
                wo.WieldSkillType = null;
            }
            return true;
        }

        private enum LootWeaponType
        {
            Axe         = 0,
            Dagger      = 1,
            DaggerMulti = 2,
            Mace        = 3,
            Spear       = 4,
            Sword       = 5,
            SwordMulti  = 6,
            Staff       = 7,
            Unarmed     = 8,
            Jitte       = 9,
            TwoHanded   = 0,
            Cleaving    = 0,
            Spears      = 1,
        }

        private static bool MutateHeavyWeapon(WorldObject wo, TreasureDeath profile, int wieldDifficulty)
        {
            switch (wo.W_WeaponType)
            {
                case WeaponType.Axe:

                    wo.Damage = GetMeleeMaxDamage(wo.WeaponSkill, wieldDifficulty, LootWeaponType.Axe);
                    wo.DamageVariance = GetVariance(wo.WeaponSkill, LootWeaponType.Axe);

                    wo.WeaponDefense = GetMaxDamageMod(profile.Tier, 18);
                    wo.WeaponOffense = GetMaxDamageMod(profile.Tier, 22);

                    break;

                case WeaponType.Dagger:

                    if (!wo.W_AttackType.IsMultiStrike())
                    {
                        wo.Damage = GetMeleeMaxDamage(wo.WeaponSkill, wieldDifficulty, LootWeaponType.Dagger);
                        wo.DamageVariance = GetVariance(wo.WeaponSkill, LootWeaponType.Dagger);
                    }
                    else
                    {
                        wo.Damage = GetMeleeMaxDamage(wo.WeaponSkill, wieldDifficulty, LootWeaponType.DaggerMulti);
                        wo.DamageVariance = GetVariance(wo.WeaponSkill, LootWeaponType.DaggerMulti);
                    }

                    wo.WeaponDefense = GetMaxDamageMod(profile.Tier, 20);
                    wo.WeaponOffense = GetMaxDamageMod(profile.Tier, 20);

                    break;

                case WeaponType.Mace:

                    wo.Damage = GetMeleeMaxDamage(wo.WeaponSkill, wieldDifficulty, LootWeaponType.Mace);
                    wo.DamageVariance = GetVariance(wo.WeaponSkill, LootWeaponType.Mace);

                    wo.WeaponDefense = GetMaxDamageMod(profile.Tier, 22);
                    wo.WeaponOffense = GetMaxDamageMod(profile.Tier, 18);

                    break;

                case WeaponType.Spear:

                    wo.Damage = GetMeleeMaxDamage(wo.WeaponSkill, wieldDifficulty, LootWeaponType.Spear);
                    wo.DamageVariance = GetVariance(wo.WeaponSkill, LootWeaponType.Spear);

                    wo.WeaponDefense = GetMaxDamageMod(profile.Tier, 15);
                    wo.WeaponOffense = GetMaxDamageMod(profile.Tier, 25);

                    break;

                case WeaponType.Staff:

                    wo.Damage = GetMeleeMaxDamage(wo.WeaponSkill, wieldDifficulty, LootWeaponType.Staff);
                    wo.DamageVariance = GetVariance(wo.WeaponSkill, LootWeaponType.Staff);

                    wo.WeaponDefense = GetMaxDamageMod(profile.Tier, 25);
                    wo.WeaponOffense = GetMaxDamageMod(profile.Tier, 15);

                    break;

                case WeaponType.Sword:

                    if (!wo.W_AttackType.IsMultiStrike())
                    {
                        wo.Damage = GetMeleeMaxDamage(wo.WeaponSkill, wieldDifficulty, LootWeaponType.Sword);
                        wo.DamageVariance = GetVariance(wo.WeaponSkill, LootWeaponType.Sword);
                    }
                    else
                    {
                        wo.Damage = GetMeleeMaxDamage(wo.WeaponSkill, wieldDifficulty, LootWeaponType.SwordMulti);
                        wo.DamageVariance = GetVariance(wo.WeaponSkill, LootWeaponType.SwordMulti);
                    }

                    wo.WeaponDefense = GetMaxDamageMod(profile.Tier, 20);
                    wo.WeaponOffense = GetMaxDamageMod(profile.Tier, 20);

                    break;

                case WeaponType.Unarmed:

                    wo.Damage = GetMeleeMaxDamage(wo.WeaponSkill, wieldDifficulty, LootWeaponType.Unarmed);
                    wo.DamageVariance = GetVariance(wo.WeaponSkill, LootWeaponType.Unarmed);

                    wo.WeaponDefense = GetMaxDamageMod(profile.Tier, 20);
                    wo.WeaponOffense = GetMaxDamageMod(profile.Tier, 20);

                    break;

                default:
                    return false;
            }

            return true;
        }

        private static bool MutateLightWeapon(WorldObject wo, TreasureDeath profile, int wieldDifficulty)
        {
            switch (wo.W_WeaponType)
            {
                case WeaponType.Axe:

                    wo.Damage = GetMeleeMaxDamage(wo.WeaponSkill, wieldDifficulty, LootWeaponType.Axe);
                    wo.DamageVariance = GetVariance(wo.WeaponSkill, LootWeaponType.Axe);

                    wo.WeaponDefense = GetMaxDamageMod(profile.Tier, 18);
                    wo.WeaponOffense = GetMaxDamageMod(profile.Tier, 22);

                    break;

                case WeaponType.Dagger:

                    if (!wo.W_AttackType.IsMultiStrike())
                    {
                        wo.Damage = GetMeleeMaxDamage(wo.WeaponSkill, wieldDifficulty, LootWeaponType.Dagger);
                        wo.DamageVariance = GetVariance(wo.WeaponSkill, LootWeaponType.Dagger);
                    }
                    else
                    {
                        wo.Damage = GetMeleeMaxDamage(wo.WeaponSkill, wieldDifficulty, LootWeaponType.DaggerMulti);
                        wo.DamageVariance = GetVariance(wo.WeaponSkill, LootWeaponType.DaggerMulti);
                    }

                    wo.WeaponDefense = GetMaxDamageMod(profile.Tier, 20);
                    wo.WeaponOffense = GetMaxDamageMod(profile.Tier, 20);

                    break;

                case WeaponType.Mace:

                    wo.Damage = GetMeleeMaxDamage(wo.WeaponSkill, wieldDifficulty, LootWeaponType.Mace);
                    wo.DamageVariance = GetVariance(wo.WeaponSkill, LootWeaponType.Mace);

                    wo.WeaponDefense = GetMaxDamageMod(profile.Tier, 22);
                    wo.WeaponOffense = GetMaxDamageMod(profile.Tier, 18);

                    break;

                case WeaponType.Spear:

                    wo.Damage = GetMeleeMaxDamage(wo.WeaponSkill, wieldDifficulty, LootWeaponType.Spear);
                    wo.DamageVariance = GetVariance(wo.WeaponSkill, LootWeaponType.Spear);

                    wo.WeaponDefense = GetMaxDamageMod(profile.Tier, 15);
                    wo.WeaponOffense = GetMaxDamageMod(profile.Tier, 25);

                    break;

                case WeaponType.Staff:

                    wo.Damage = GetMeleeMaxDamage(wo.WeaponSkill, wieldDifficulty, LootWeaponType.Staff);
                    wo.DamageVariance = GetVariance(wo.WeaponSkill, LootWeaponType.Staff);

                    wo.WeaponDefense = GetMaxDamageMod(profile.Tier, 25);
                    wo.WeaponOffense = GetMaxDamageMod(profile.Tier, 15);

                    break;

                case WeaponType.Sword:

                    if (!wo.W_AttackType.IsMultiStrike())
                    {
                        wo.Damage = GetMeleeMaxDamage(wo.WeaponSkill, wieldDifficulty, LootWeaponType.Sword);
                        wo.DamageVariance = GetVariance(wo.WeaponSkill, LootWeaponType.Sword);
                    }
                    else
                    {
                        wo.Damage = GetMeleeMaxDamage(wo.WeaponSkill, wieldDifficulty, LootWeaponType.SwordMulti);
                        wo.DamageVariance = GetVariance(wo.WeaponSkill, LootWeaponType.SwordMulti);

                    }

                    wo.WeaponDefense = GetMaxDamageMod(profile.Tier, 20);
                    wo.WeaponOffense = GetMaxDamageMod(profile.Tier, 20);

                    break;

                case WeaponType.Unarmed:

                    wo.Damage = GetMeleeMaxDamage(wo.WeaponSkill, wieldDifficulty, LootWeaponType.Unarmed);
                    wo.DamageVariance = GetVariance(wo.WeaponSkill, LootWeaponType.Unarmed);

                    wo.WeaponDefense = GetMaxDamageMod(profile.Tier, 20);
                    wo.WeaponOffense = GetMaxDamageMod(profile.Tier, 20);

                    break;

                default:
                    return false;
            }

            return true;
        }

        private static bool MutateFinesseWeapon(WorldObject wo, TreasureDeath profile, int wieldDifficulty)
        {
            switch (wo.W_WeaponType)
            {
                case WeaponType.Axe:

                    wo.Damage = GetMeleeMaxDamage(wo.WeaponSkill, wieldDifficulty, LootWeaponType.Axe);
                    wo.DamageVariance = GetVariance(wo.WeaponSkill, LootWeaponType.Axe);

                    wo.WeaponDefense = GetMaxDamageMod(profile.Tier, 18);
                    wo.WeaponOffense = GetMaxDamageMod(profile.Tier, 22);

                    break;

                case WeaponType.Dagger:

                    if (!wo.W_AttackType.IsMultiStrike())
                    {
                        wo.Damage = GetMeleeMaxDamage(wo.WeaponSkill, wieldDifficulty, LootWeaponType.Dagger);
                        wo.DamageVariance = GetVariance(wo.WeaponSkill, LootWeaponType.Dagger);
                    }
                    else
                    {
                        wo.Damage = GetMeleeMaxDamage(wo.WeaponSkill, wieldDifficulty, LootWeaponType.DaggerMulti);
                        wo.DamageVariance = GetVariance(wo.WeaponSkill, LootWeaponType.DaggerMulti);

                    }

                    wo.WeaponDefense = GetMaxDamageMod(profile.Tier, 20);
                    wo.WeaponOffense = GetMaxDamageMod(profile.Tier, 20);

                    break;

                case WeaponType.Mace:

                    wo.Damage = GetMeleeMaxDamage(wo.WeaponSkill, wieldDifficulty, LootWeaponType.Mace);

                    if (wo.TsysMutationData != 0x06080402)
                    {
                        wo.DamageVariance = GetVariance(wo.WeaponSkill, LootWeaponType.Mace);

                        wo.WeaponDefense = GetMaxDamageMod(profile.Tier, 22);
                        wo.WeaponOffense = GetMaxDamageMod(profile.Tier, 18);
                    }
                    else  // handle jittes
                    {
                        wo.DamageVariance = GetVariance(wo.WeaponSkill, LootWeaponType.Jitte);

                        wo.WeaponDefense = GetMaxDamageMod(profile.Tier, 25);
                        wo.WeaponOffense = GetMaxDamageMod(profile.Tier, 15);
                    }
                    break;

                case WeaponType.Spear:

                    wo.Damage = GetMeleeMaxDamage(wo.WeaponSkill, wieldDifficulty, LootWeaponType.Spear);
                    wo.DamageVariance = GetVariance(wo.WeaponSkill, LootWeaponType.Spear);

                    wo.WeaponDefense = GetMaxDamageMod(profile.Tier, 15);
                    wo.WeaponOffense = GetMaxDamageMod(profile.Tier, 25);

                    break;

                case WeaponType.Staff:

                    wo.Damage = GetMeleeMaxDamage(wo.WeaponSkill, wieldDifficulty, LootWeaponType.Staff);
                    wo.DamageVariance = GetVariance(wo.WeaponSkill, LootWeaponType.Staff);

                    wo.WeaponDefense = GetMaxDamageMod(profile.Tier, 25);
                    wo.WeaponOffense = GetMaxDamageMod(profile.Tier, 15);

                    break;

                case WeaponType.Sword:

                    if (!wo.W_AttackType.IsMultiStrike())
                    {
                        wo.Damage = GetMeleeMaxDamage(wo.WeaponSkill, wieldDifficulty, LootWeaponType.Sword);
                        wo.DamageVariance = GetVariance(wo.WeaponSkill, LootWeaponType.Sword);
                    }
                    else
                    {
                        wo.Damage = GetMeleeMaxDamage(wo.WeaponSkill, wieldDifficulty, LootWeaponType.SwordMulti);
                        wo.DamageVariance = GetVariance(wo.WeaponSkill, LootWeaponType.SwordMulti);
                    }

                    wo.WeaponDefense = GetMaxDamageMod(profile.Tier, 20);
                    wo.WeaponOffense = GetMaxDamageMod(profile.Tier, 20);

                    break;

                case WeaponType.Unarmed:

                    wo.Damage = GetMeleeMaxDamage(wo.WeaponSkill, wieldDifficulty, LootWeaponType.Unarmed);
                    wo.DamageVariance = GetVariance(wo.WeaponSkill, LootWeaponType.Unarmed);

                    wo.WeaponDefense = GetMaxDamageMod(profile.Tier, 20);
                    wo.WeaponOffense = GetMaxDamageMod(profile.Tier, 20);

                    break;

                default:
                    return false;
            }

            return true;
        }

        private static bool MutateTwoHandedWeapon(WorldObject wo, TreasureDeath profile, int wieldDifficulty)
        {
            if (wo.IsCleaving)
            {
                wo.Damage = GetMeleeMaxDamage(wo.WeaponSkill, wieldDifficulty, LootWeaponType.Cleaving);
                wo.DamageVariance = GetVariance(wo.WeaponSkill, LootWeaponType.TwoHanded);

                wo.WeaponDefense = GetMaxDamageMod(profile.Tier, 18);
                wo.WeaponOffense = GetMaxDamageMod(profile.Tier, 22);
            }
            else
            {
                wo.Damage = GetMeleeMaxDamage(wo.WeaponSkill, wieldDifficulty, LootWeaponType.Spears);
                wo.DamageVariance = GetVariance(wo.WeaponSkill, LootWeaponType.TwoHanded);

                wo.WeaponDefense = GetMaxDamageMod(profile.Tier, 20);
                wo.WeaponOffense = GetMaxDamageMod(profile.Tier, 20);
            }
            return true;
        }

        //ClassicToDo: The following data has been copy pasted from heavy weapons, adjust to proper values. But do we even have to? This is what is referred to as the "Old Method" and is setup as a fallback, is it even used?
        private static bool MutateAxe(WorldObject wo, TreasureDeath profile, int wieldDifficulty)
        {
            if (wo.W_WeaponType == WeaponType.Axe)
            {
                wo.Damage = GetMeleeMaxDamage(wo.WeaponSkill, wieldDifficulty, LootWeaponType.Axe);
                wo.DamageVariance = GetVariance(wo.WeaponSkill, LootWeaponType.Axe);

                wo.WeaponDefense = GetMaxDamageMod(profile.Tier, 18);
                wo.WeaponOffense = GetMaxDamageMod(profile.Tier, 22);
            }
            else
                return false;
            return true;
        }

        private static bool MutateDagger(WorldObject wo, TreasureDeath profile, int wieldDifficulty)
        {
            if (wo.W_WeaponType == WeaponType.Dagger)
            {
                if (!wo.W_AttackType.IsMultiStrike())
                {
                    wo.Damage = GetMeleeMaxDamage(wo.WeaponSkill, wieldDifficulty, LootWeaponType.Dagger);
                    wo.DamageVariance = GetVariance(wo.WeaponSkill, LootWeaponType.Dagger);
                }
                else
                {
                    wo.Damage = GetMeleeMaxDamage(wo.WeaponSkill, wieldDifficulty, LootWeaponType.DaggerMulti);
                    wo.DamageVariance = GetVariance(wo.WeaponSkill, LootWeaponType.DaggerMulti);
                }

                wo.WeaponDefense = GetMaxDamageMod(profile.Tier, 20);
                wo.WeaponOffense = GetMaxDamageMod(profile.Tier, 20);
            }
            else
                return false;
            return true;
        }

        private static bool MutateMace(WorldObject wo, TreasureDeath profile, int wieldDifficulty)
        {
            if (wo.W_WeaponType == WeaponType.Mace)
            {
                wo.Damage = GetMeleeMaxDamage(wo.WeaponSkill, wieldDifficulty, LootWeaponType.Mace);
                wo.DamageVariance = GetVariance(wo.WeaponSkill, LootWeaponType.Mace);

                wo.WeaponDefense = GetMaxDamageMod(profile.Tier, 22);
                wo.WeaponOffense = GetMaxDamageMod(profile.Tier, 18);
            }
            else
                return false;
            return true;
        }

        private static bool MutateSpear(WorldObject wo, TreasureDeath profile, int wieldDifficulty)
        {
            if (wo.W_WeaponType == WeaponType.Spear)
            {
                wo.Damage = GetMeleeMaxDamage(wo.WeaponSkill, wieldDifficulty, LootWeaponType.Spear);
                wo.DamageVariance = GetVariance(wo.WeaponSkill, LootWeaponType.Spear);

                wo.WeaponDefense = GetMaxDamageMod(profile.Tier, 15);
                wo.WeaponOffense = GetMaxDamageMod(profile.Tier, 25);
            }
            else
                return false;
            return true;
        }

        private static bool MutateStaff(WorldObject wo, TreasureDeath profile, int wieldDifficulty)
        {
            if (wo.W_WeaponType == WeaponType.Staff)
            {
                wo.Damage = GetMeleeMaxDamage(wo.WeaponSkill, wieldDifficulty, LootWeaponType.Staff);
                wo.DamageVariance = GetVariance(wo.WeaponSkill, LootWeaponType.Staff);

                wo.WeaponDefense = GetMaxDamageMod(profile.Tier, 25);
                wo.WeaponOffense = GetMaxDamageMod(profile.Tier, 15);
            }
            else
                return false;
            return true;
        }

        private static bool MutateSword(WorldObject wo, TreasureDeath profile, int wieldDifficulty)
        {
            if (wo.W_WeaponType == WeaponType.Sword)
            {
                if (!wo.W_AttackType.IsMultiStrike())
                {
                    wo.Damage = GetMeleeMaxDamage(wo.WeaponSkill, wieldDifficulty, LootWeaponType.Sword);
                    wo.DamageVariance = GetVariance(wo.WeaponSkill, LootWeaponType.Sword);
                }
                else
                {
                    wo.Damage = GetMeleeMaxDamage(wo.WeaponSkill, wieldDifficulty, LootWeaponType.SwordMulti);
                    wo.DamageVariance = GetVariance(wo.WeaponSkill, LootWeaponType.SwordMulti);
                }

                wo.WeaponDefense = GetMaxDamageMod(profile.Tier, 20);
                wo.WeaponOffense = GetMaxDamageMod(profile.Tier, 20);
            }
            else
                return false;
            return true;
        }

        private static bool MutateUnarmed(WorldObject wo, TreasureDeath profile, int wieldDifficulty)
        {
            if (wo.W_WeaponType == WeaponType.Unarmed)
            {
                wo.Damage = GetMeleeMaxDamage(wo.WeaponSkill, wieldDifficulty, LootWeaponType.Unarmed);
                wo.DamageVariance = GetVariance(wo.WeaponSkill, LootWeaponType.Unarmed);

                wo.WeaponDefense = GetMaxDamageMod(profile.Tier, 20);
                wo.WeaponOffense = GetMaxDamageMod(profile.Tier, 20);
            }
            else
                return false;
            return true;
        }

        // The percentages for variances need to be fixed
        /// <summary>
        /// Gets Melee Weapon Variance
        /// </summary>
        /// <param name="category"></param><param name="type"></param>
        /// <returns>Returns Melee Weapon Variance</returns>
        private static double GetVariance(Skill category, LootWeaponType type)
        {
            double variance = 0;
            int chance = ThreadSafeRandom.Next(0, 99);

            switch (category)
            {
                case Skill.HeavyWeapons:
                    switch (type)
                    {
                        case LootWeaponType.Axe:
                            if (chance < 10)
                                variance = .90;
                            else if (chance < 30)
                                variance = .93;
                            else if (chance < 70)
                                variance = .95;
                            else if (chance < 90)
                                variance = .97;
                            else
                                variance = .99;
                            break;
                        case LootWeaponType.Dagger:
                            if (chance < 10)
                                variance = .47;
                            else if (chance < 30)
                                variance = .50;
                            else if (chance < 70)
                                variance = .53;
                            else if (chance < 90)
                                variance = .57;
                            else
                                variance = .62;
                            break;
                        case LootWeaponType.DaggerMulti:
                            if (chance < 10)
                                variance = .40;
                            else if (chance < 30)
                                variance = .43;
                            else if (chance < 70)
                                variance = .48;
                            else if (chance < 90)
                                variance = .53;
                            else
                                variance = .58;
                            break;
                        case LootWeaponType.Mace:
                            if (chance < 10)
                                variance = .30;
                            else if (chance < 30)
                                variance = .33;
                            else if (chance < 70)
                                variance = .37;
                            else if (chance < 90)
                                variance = .42;
                            else
                                variance = .46;
                            break;
                        case LootWeaponType.Spear:
                            if (chance < 10)
                                variance = .59;
                            else if (chance < 30)
                                variance = .63;
                            else if (chance < 70)
                                variance = .68;
                            else if (chance < 90)
                                variance = .72;
                            else
                                variance = .75;
                            break;
                        case LootWeaponType.Staff:
                            if (chance < 10)
                                variance = .38;
                            else if (chance < 30)
                                variance = .42;
                            else if (chance < 70)
                                variance = .45;
                            else if (chance < 90)
                                variance = .50;
                            else
                                variance = .52;
                            break;
                        case LootWeaponType.Sword:
                            if (chance < 10)
                                variance = .47;
                            else if (chance < 30)
                                variance = .50;
                            else if (chance < 70)
                                variance = .53;
                            else if (chance < 90)
                                variance = .57;
                            else
                                variance = .62;
                            break;
                        case LootWeaponType.SwordMulti:
                            if (chance < 10)
                                variance = .40;
                            else if (chance < 30)
                                variance = .43;
                            else if (chance < 70)
                                variance = .48;
                            else if (chance < 90)
                                variance = .53;
                            else
                                variance = .60;
                            break;
                        case LootWeaponType.Unarmed:
                            if (chance < 10)
                                variance = .44;
                            else if (chance < 30)
                                variance = .48;
                            else if (chance < 70)
                                variance = .53;
                            else if (chance < 90)
                                variance = .58;
                            else
                                variance = .60;
                            break;
                    }
                    break;
                case Skill.LightWeapons:
                case Skill.FinesseWeapons:
                    switch (type)
                    {
                        case LootWeaponType.Axe:
                            // Axe
                            if (chance < 10)
                                variance = .80;
                            else if (chance < 30)
                                variance = .83;
                            else if (chance < 70)
                                variance = .85;
                            else if (chance < 90)
                                variance = .90;
                            else
                                variance = .95;
                            break;
                        case LootWeaponType.Dagger:
                            // Dagger
                            if (chance < 10)
                                variance = .42;
                            else if (chance < 30)
                                variance = .47;
                            else if (chance < 70)
                                variance = .52;
                            else if (chance < 90)
                                variance = .56;
                            else
                                variance = .60;
                            break;
                        case LootWeaponType.DaggerMulti:
                            // Dagger MultiStrike
                            if (chance < 10)
                                variance = .24;
                            else if (chance < 30)
                                variance = .28;
                            else if (chance < 70)
                                variance = .35;
                            else if (chance < 90)
                                variance = .40;
                            else
                                variance = .45;
                            break;
                        case LootWeaponType.Mace:
                            // Mace
                            if (chance < 10)
                                variance = .23;
                            else if (chance < 30)
                                variance = .28;
                            else if (chance < 70)
                                variance = .32;
                            else if (chance < 90)
                                variance = .37;
                            else
                                variance = .43;
                            break;
                        case LootWeaponType.Jitte:
                            // Jitte
                            if (chance < 10)
                                variance = .325;
                            else if (chance < 30)
                                variance = .35;
                            else if (chance < 70)
                                variance = .40;
                            else if (chance < 90)
                                variance = .45;
                            else
                                variance = .50;
                            break;
                        case LootWeaponType.Spear:
                            // Spear
                            if (chance < 10)
                                variance = .65;
                            else if (chance < 30)
                                variance = .68;
                            else if (chance < 70)
                                variance = .71;
                            else if (chance < 90)
                                variance = .75;
                            else
                                variance = .80;
                            break;
                        case LootWeaponType.Staff:
                            // Staff
                            if (chance < 10)
                                variance = .325;
                            else if (chance < 30)
                                variance = .35;
                            else if (chance < 70)
                                variance = .40;
                            else if (chance < 90)
                                variance = .45;
                            else
                                variance = .50;
                            break;
                        case LootWeaponType.Sword:
                            // Sword
                            if (chance < 10)
                                variance = .42;
                            else if (chance < 30)
                                variance = .47;
                            else if (chance < 70)
                                variance = .52;
                            else if (chance < 90)
                                variance = .56;
                            else
                                variance = .60;
                            break;
                        case LootWeaponType.SwordMulti:
                            // Sword Multistrike
                            if (chance < 10)
                                variance = .24;
                            else if (chance < 30)
                                variance = .28;
                            else if (chance < 70)
                                variance = .35;
                            else if (chance < 90)
                                variance = .40;
                            else
                                variance = .45;
                            break;
                        case LootWeaponType.Unarmed:
                            // UA
                            if (chance < 10)
                                variance = .44;
                            else if (chance < 30)
                                variance = .48;
                            else if (chance < 70)
                                variance = .53;
                            else if (chance < 90)
                                variance = .58;
                            else
                                variance = .60;
                            break;
                    }
                    break;
                case Skill.TwoHandedCombat:
                    // Two Handed only have one set of variances
                    if (chance < 5)
                        variance = .30;
                    else if (chance < 20)
                        variance = .35;
                    else if (chance < 50)
                        variance = .40;
                    else if (chance < 80)
                        variance = .45;
                    else if (chance < 95)
                        variance = .50;
                    else
                        variance = .55;
                    break;
                //ClassicToDo: The following data has been copy pasted from heavy weapons, adjust to proper values. But do we even have to? This is what is referred to as the "Old Method" and is setup as a fallback, is it even used?
                case Skill.Axe:
                    if (chance < 10)
                        variance = .90;
                    else if (chance < 30)
                        variance = .93;
                    else if (chance < 70)
                        variance = .95;
                    else if (chance < 90)
                        variance = .97;
                    else
                        variance = .99;
                    break;
                case Skill.Dagger:
                    switch (type)
                    {
                        case LootWeaponType.Dagger:
                            if (chance < 10)
                                variance = .47;
                            else if (chance < 30)
                                variance = .50;
                            else if (chance < 70)
                                variance = .53;
                            else if (chance < 90)
                                variance = .57;
                            else
                                variance = .62;
                            break;
                        case LootWeaponType.DaggerMulti:
                            if (chance < 10)
                                variance = .40;
                            else if (chance < 30)
                                variance = .43;
                            else if (chance < 70)
                                variance = .48;
                            else if (chance < 90)
                                variance = .53;
                            else
                                variance = .58;
                            break;
                    }
                    break;
                case Skill.Mace:
                    if (chance < 10)
                        variance = .30;
                    else if (chance < 30)
                        variance = .33;
                    else if (chance < 70)
                        variance = .37;
                    else if (chance < 90)
                        variance = .42;
                    else
                        variance = .46;
                    break;
                case Skill.Spear:
                    if (chance < 10)
                        variance = .59;
                    else if (chance < 30)
                        variance = .63;
                    else if (chance < 70)
                        variance = .68;
                    else if (chance < 90)
                        variance = .72;
                    else
                        variance = .75;
                    break;
                case Skill.Staff:
                    if (chance < 10)
                        variance = .38;
                    else if (chance < 30)
                        variance = .42;
                    else if (chance < 70)
                        variance = .45;
                    else if (chance < 90)
                        variance = .50;
                    else
                        variance = .52;
                    break;
                case Skill.Sword:
                    switch (type)
                    {
                        case LootWeaponType.Sword:
                            if (chance < 10)
                                variance = .47;
                            else if (chance < 30)
                                variance = .50;
                            else if (chance < 70)
                                variance = .53;
                            else if (chance < 90)
                                variance = .57;
                            else
                                variance = .62;
                            break;
                        case LootWeaponType.SwordMulti:
                            if (chance < 10)
                                variance = .40;
                            else if (chance < 30)
                                variance = .43;
                            else if (chance < 70)
                                variance = .48;
                            else if (chance < 90)
                                variance = .53;
                            else
                                variance = .60;
                            break;
                    }
                    break;
                case Skill.UnarmedCombat:
                    if (chance < 10)
                        variance = .44;
                    else if (chance < 30)
                        variance = .48;
                    else if (chance < 70)
                        variance = .53;
                    else if (chance < 90)
                        variance = .58;
                    else
                        variance = .60;
                    break;
                default:
                    return 0;
            }

            return variance;
        }

        /// <summary>
        /// Gets Melee Weapon Index
        /// </summary>
        private static int GetMeleeWieldToIndex(int wieldDiff)
        {
            int index = 0;

            switch (wieldDiff)
            {
                case 250:
                    index = 1;
                    break;
                case 300:
                    index = 2;
                    break;
                case 325:
                    index = 3;
                    break;
                case 350:
                    index = 4;
                    break;
                case 370:
                    index = 5;
                    break;
                case 400:
                    index = 6;
                    break;
                case 420:
                    index = 7;
                    break;
                case 430:
                    index = 8;
                    break;
                default:
                    index = 0;
                    break;
            }

            return index;
        }

        /// <summary>
        /// Gets Melee Weapon Max Damage
        /// </summary>
        /// <param name="weaponType"></param><param name="wieldDiff"></param><param name="baseWeapon"></param>
        /// <returns>Melee Weapon Max Damage</returns>
        private static int GetMeleeMaxDamage(Skill weaponType, int wieldDiff, LootWeaponType baseWeapon)
        {
            int damageTable = 0;

            switch (weaponType)
            {
                case Skill.HeavyWeapons:
                    damageTable = LootTables.HeavyWeaponDamageTable[(int)baseWeapon, GetMeleeWieldToIndex(wieldDiff)];
                    break;
                case Skill.FinesseWeapons:
                case Skill.LightWeapons:
                    damageTable = LootTables.LightWeaponDamageTable[(int)baseWeapon, GetMeleeWieldToIndex(wieldDiff)];
                    break;
                case Skill.TwoHandedCombat:
                    damageTable = LootTables.TwoHandedWeaponDamageTable[(int)baseWeapon, GetMeleeWieldToIndex(wieldDiff)];
                    break;
                case Skill.Axe:
                    damageTable = LootTables.AxeDamageTable[GetMeleeWieldToIndex(wieldDiff)];
                    break;
                case Skill.Dagger:
                    if (baseWeapon == LootWeaponType.DaggerMulti)
                        damageTable = LootTables.DaggerDamageTable[1, GetMeleeWieldToIndex(wieldDiff)];
                    else
                        damageTable = LootTables.DaggerDamageTable[0, GetMeleeWieldToIndex(wieldDiff)];
                    break;
                case Skill.Mace:
                    damageTable = LootTables.MaceDamageTable[GetMeleeWieldToIndex(wieldDiff)];
                    break;
                case Skill.Staff:
                    damageTable = LootTables.StaffDamageTable[GetMeleeWieldToIndex(wieldDiff)];
                    break;
                case Skill.Spear:
                    damageTable = LootTables.SpearDamageTable[GetMeleeWieldToIndex(wieldDiff)];
                    break;
                case Skill.Sword:
                    if (baseWeapon == LootWeaponType.SwordMulti)
                        damageTable = LootTables.SwordDamageTable[1, GetMeleeWieldToIndex(wieldDiff)];
                    else
                        damageTable = LootTables.SwordDamageTable[0, GetMeleeWieldToIndex(wieldDiff)];
                    break;
                case Skill.UnarmedCombat:
                    damageTable = LootTables.UnarmedDamageTable[GetMeleeWieldToIndex(wieldDiff)];
                    break;
                default:
                    return 0;
            }

            // To add a little bit of randomness to Max weapon damage
            int maxDamageVariance = ThreadSafeRandom.Next(-4, 0);

            return damageTable + maxDamageVariance;
        }

        private static bool GetMutateMeleeWeaponData(uint wcid)
        {
            // linear search = slow... but this is only called for /lootgen
            // if this ever needs to be fast, create a lookup table

            for (int weaponType = 0; weaponType < LootTables.MeleeWeaponsMatrices.Count; weaponType++)
            {
                var lootTable = LootTables.MeleeWeaponsMatrices[weaponType];
                for (int subtype = 0; subtype < lootTable.Length; subtype++)
                {
                    if (lootTable[subtype].Contains((int)wcid))
                        return true;
                }
            }
            return false;
        }

        private static void TryMutateMeleeWeaponDamage(WorldObject wo, TreasureRoll roll, TreasureDeath profile, out int maxPossibleDamage)
        {
            maxPossibleDamage = 0;

            if (wo.DamageVariance == null)
            {
                _log.Error("TryMutateMeleeWeaponDamage({WeaponName}, {TreasureRoll}, {TreasureDeath}) - DamageVariance is null.", wo.Name, roll, profile);
                return;
            }

            if (wo.WeaponTime == null)
            {
                _log.Error("TryMutateMeleeWeaponDamage({WeaponName}, {TreasureRoll}, {TreasureDeath}) - WeaponTime is null.", wo.Name, roll, profile);
                return;
            }

            // target dps per tier
            var targetBaseDps = GetWeaponBaseDps(wo.Tier ?? 1);
            if (wo.CleaveTargets > 0)
                targetBaseDps *= 0.6f;

            // animation speed
            var baseAnimLength = WeaponAnimationLength.GetAnimLength(wo);

            int[] avgQuickPerTier = { 45, 65, 93, 118, 140, 160, 180, 195 };
            var quick = (float)avgQuickPerTier[profile.Tier - 1];
            var speedMod = 0.8f + (1 - (wo.WeaponTime.Value / 100.0)) + quick / 600;
            var effectiveAttacksPerSecond = 1 / (baseAnimLength / speedMod);

            if (wo.IsTwoHanded || wo.W_AttackType == AttackType.DoubleStrike)
                effectiveAttacksPerSecond *= 2;
            else if (wo.W_AttackType == AttackType.TripleStrike)
                effectiveAttacksPerSecond *= 3;
            else if (wo.W_WeaponType == WeaponType.Thrown)
            {
                var reloadLength = 0.9777778f;
                effectiveAttacksPerSecond = 1 / (baseAnimLength - reloadLength + (reloadLength * speedMod));
            }

            // target weapon hit damage
            var targetAverageHitDamage = targetBaseDps / effectiveAttacksPerSecond;

            // find average max damage, considering variance and critical strikes
            var weaponVariance = wo.DamageVariance.Value;
            var averageBaseMaxDamage = targetAverageHitDamage / ((((1 - weaponVariance) + 1) / 2 * 0.9) + 0.2);

            // get low-end and high-end max damage range
            var damageRangePerTier = 0.25;
            var maximumBaseMaxDamage = (averageBaseMaxDamage * 2) / (1.0 + (1 - damageRangePerTier));
            var minimumBaseMaxDamage = maximumBaseMaxDamage * (1 - damageRangePerTier);

            // roll and assign weapon damage
            var diminishedRoll = (averageBaseMaxDamage - minimumBaseMaxDamage) * GetDiminishingRoll(profile);
            var finalMaxDamage = minimumBaseMaxDamage + diminishedRoll;
            wo.Damage = (int)Math.Round(finalMaxDamage);

            // debug
            //var averageDamage = (averageBaseMaxDamage + (averageBaseMaxDamage * (1 - weaponVariance))) / 2;
            //var averageDamageWithCrits = (averageDamage * 0.9) + (averageBaseMaxDamage * 0.2);
            //Console.WriteLine($"\nTryMutateMeleeWeaponDamage()\n" +
            //    $" TargetBaseDps: {targetBaseDps}\n" +
            //    $" BaseAnimLength: {baseAnimLength}\n" +
            //    $" SpeedMod: {speedMod}\n" +
            //    $" AttacksPerSecond: {effectiveAttacksPerSecond}\n" +
            //    $" TargetAvgWeaponDamage: {targetAverageHitDamage}\n" +
            //    $" WeaponVariance: {weaponVariance}\n\n" +
            //    $" AverageBaseMaxDamage: {averageBaseMaxDamage}\n" +
            //    $" MaximumBaseMaxDamage: {maximumBaseMaxDamage}\n" +
            //    $" MinimumBaseMaxDamage: {minimumBaseMaxDamage}\n" +
            //    $" DiminishedRoll: {diminishedRoll}\n" +
            //    $" FinalMaxDamage: {finalMaxDamage}\n\n" +
            //    $" Non-Crit average Hit Damage: {averageDamage}\n" +
            //    $" Average Hit Damage with Crits: {averageDamageWithCrits}\n" +
            //    $" Avg DPS: {averageDamageWithCrits * effectiveAttacksPerSecond}");

            // max possible damage (for workmanship)
            targetBaseDps = GetWeaponBaseDps(8);
            if (wo.CleaveTargets > 1)
                targetBaseDps *= 0.6f;

            targetAverageHitDamage = targetBaseDps / effectiveAttacksPerSecond;
            averageBaseMaxDamage = targetAverageHitDamage / ((((1 - weaponVariance) + 1) / 2 * 0.9) + 0.2);
            maximumBaseMaxDamage = (averageBaseMaxDamage * 2) / (1.0 + (1 - damageRangePerTier));
            maxPossibleDamage = (int)maximumBaseMaxDamage;
        }
    }
}
