using ACE.Common;
using ACE.Database.Models.World;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Server.Entity.Mutations;
using ACE.Server.Factories.Entity;
using ACE.Server.Factories.Enum;
using ACE.Server.Factories.Tables;
using ACE.Server.WorldObjects;
using System;

namespace ACE.Server.Factories
{
    public static partial class LootGenerationFactory
    {
        /// <summary>
        /// Creates Caster (Wand, Staff, Orb)
        /// </summary>
        public static WorldObject CreateCaster(TreasureDeath profile, bool isMagical, int wield = -1, bool forceWar = false, bool mutate = true)
        {
            // Refactored 11/20/19  - HarliQ
            int casterWeenie = 0;
            int subType = 0;
            int element = 0;

            if (wield == -1)
                wield = RollWieldDifficulty(profile.Tier, TreasureWeaponType.Caster);

            // Getting the caster Weenie needed.
            
            // Determine caster type: 0 = Orb, 1 = Scepter, 2 = Baton, 3 = Staff
            subType = ThreadSafeRandom.Next(0, LootTables.TimelineCasterWeaponsMatrix.Length - 1);
            var subTypeLength = ThreadSafeRandom.Next(0, LootTables.TimelineCasterWeaponsMatrix[subType].Length - 1);

            element = ThreadSafeRandom.Next(0, subTypeLength);
            casterWeenie = LootTables.TimelineCasterWeaponsMatrix[subType][element];

            WorldObject wo = WorldObjectFactory.CreateNewWorldObject((uint)casterWeenie);

            if (wo != null && mutate)
                MutateCaster(wo, profile, isMagical, wield, null);

            return wo;
        }

        private static void MutateCaster(WorldObject wo, TreasureDeath profile, bool isMagical, int? wieldDifficulty = null, TreasureRoll roll = null)
        {
            if (wieldDifficulty != null)
            {
                // previous method

                var wieldRequirement = WieldRequirement.RawSkill;
                var wieldSkillType = Skill.None;

                double elementalDamageMod = 0;

                if (wieldDifficulty == 0)
                {
                    if (profile.Tier > 6)
                    {
                        wieldRequirement = WieldRequirement.Level;
                        wieldSkillType = Skill.Axe;  // Set by examples from PCAP data

                        wieldDifficulty = profile.Tier switch
                        {
                            7 => 150, // In this instance, used for indicating player level, rather than skill level
                            _ => 180, // In this instance, used for indicating player level, rather than skill level
                        };
                    }
                }
                else
                {
                    elementalDamageMod = RollElementalDamageMod(wieldDifficulty.Value, profile);

                    if (wo.W_DamageType == DamageType.Nether)
                        wieldSkillType = Skill.VoidMagic;
                    else
                        wieldSkillType = Skill.WarMagic;
                }

                // ManaConversionMod
                var manaConversionMod = RollManaConversionMod(profile.Tier);
                if (manaConversionMod > 0.0f)
                    wo.ManaConversionMod = manaConversionMod;

                // ElementalDamageMod
                if (elementalDamageMod > 1.0f)
                    wo.ElementalDamageMod = elementalDamageMod;

                // WieldRequirements
                if (wieldDifficulty > 0 || wieldRequirement == WieldRequirement.Level)
                {
                    wo.WieldRequirements = wieldRequirement;
                    wo.WieldSkillType = (int)wieldSkillType;
                    wo.WieldDifficulty = wieldDifficulty;
                }
                else
                {
                    wo.WieldRequirements = WieldRequirement.Invalid;
                    wo.WieldSkillType = null;
                    wo.WieldDifficulty = null;
                }

                // WeaponDefense
                wo.WeaponPhysicalDefense = RollWeaponDefense(wieldDifficulty.Value, profile);
            }

            wo.Tier = GetTierValue(profile);

            // Determine caster type: 0 = Orb, 1 = Scepter, 2 = Wand/Baton, 3 = Staff
            var subType = GetCasterSubType(wo);

            // Add element/material to low tier orb/wand/scepter/staff
            if (wo.WeenieClassId == 2366 || wo.WeenieClassId == 2547 || wo.WeenieClassId == 2548 || wo.WeenieClassId == 2472)
            {
                RollCasterElement(profile, wo);
            }
            else
            {
                var materialType = GetMaterialType(wo, profile.Tier);
                if (materialType > 0)
                    wo.MaterialType = materialType;
            }

            // item color
            if (wo.WeenieClassId == 2548)
                MutateScepterColor(wo);
            //else if (wo.WeenieClassId >= 1050100)
            //    MutateOrbColor();
            else
            {
                MutateColor(wo);
            }

            // Bonus Resto/Elemental %
            var damagePercentile = 0.0;
            if (wo.W_DamageType == DamageType.Undef)
                wo.WieldSkillType2 = (int)Skill.LifeMagic;
            else
                wo.WieldSkillType2 = (int)Skill.WarMagic;

            // Wield Reqs
            wo.WieldRequirements = WieldRequirement.RawAttrib;
            wo.WieldDifficulty = RollWieldDifficulty(profile.Tier, TreasureWeaponType.MeleeWeapon);
            wo.WieldSkillType = GetWeaponPrimaryAttribute((Skill)wo.WieldSkillType2);

            wo.WieldRequirements2 = WieldRequirement.Training;
            wo.WieldDifficulty2 = 1;

            // Roll Elemental Damage Mod
            TryMutateCasterWeaponDamage(wo, roll, profile, out damagePercentile);

            // T1 casters have damage penalty
            if (profile.Tier == 1)
            {
                // Orb and Staff receive harsher penalties
                if (subType == 0 || subType == 3)
                {
                    wo.GearDamage = ThreadSafeRandom.Next(-30, -20);
                    wo.DamageRating = ThreadSafeRandom.Next(-30, -20);
                }
                else
                {
                    wo.GearDamage = ThreadSafeRandom.Next(-20, -10);
                    wo.DamageRating = ThreadSafeRandom.Next(-20, -10);
                }
            }

            // Roll Weapon Mods
            TryMutateWeaponMods(wo, profile, out var modsPercentile, true);

            // Bonus Crit Chance for Scepters
            if (subType == 1)
            {
                RollBonusCritDamage(profile, wo);
            }
            // Bonus Crit Damage for Wands/Batons
            else if (subType == 2)
            {
                RollBonusCritChance(profile, wo);
            }


            // gem count / gem material
            if (wo.GemCode != null)
                wo.GemCount = GemCountChance.Roll(wo.GemCode.Value, profile.Tier);
            else
                wo.GemCount = ThreadSafeRandom.Next(1, 5);

            wo.GemType = RollGemType(profile.Tier);

            // workmanship
            wo.ItemWorkmanship = GetCasterWorkmanship(wo, damagePercentile, modsPercentile);

            // burden?

            // spells
            if (!isMagical)
            {
                wo.ItemManaCost = null;
                wo.ItemMaxMana = null;
                wo.ItemCurMana = null;
                wo.ItemSpellcraft = null;
                wo.ItemDifficulty = null;
            }
            else
            {
                // if a caster was from a MagicItem profile, it always had a SpellDID
                MutateCaster_SpellDID(wo, profile);

                AssignMagic(wo, profile, roll);
            }

            // item value
            //if (wo.HasMutateFilter(MutateFilter.Value))   // fixme: data
            MutateValue(wo, profile.Tier, roll);

            // long description
            wo.LongDesc = GetLongDesc(wo);

            wo.ItemDifficulty = null;

            if(profile.Tier == 1)
            {
                wo.Name += " (damaged)";
            }
        }

        private static void MutateCaster_SpellDID(WorldObject wo, TreasureDeath profile)
        {
            var firstSpell = CasterSlotSpells.Roll(wo);

            var spellLevels = SpellLevelProgression.GetSpellLevels(firstSpell);

            if (spellLevels == null)
            {
                _log.Error($"MutateCaster_SpellDID: couldn't find {firstSpell}");
                return;
            }

            if (spellLevels.Count != 8)
            {
                _log.Error($"MutateCaster_SpellDID: found {spellLevels.Count} spell levels for {firstSpell}, expected 8");
                return;
            }

            var spellLevel = SpellLevelChance.Roll(profile.Tier);

            wo.SpellDID = (uint)spellLevels[spellLevel - 1];

            var spell = new Server.Entity.Spell(wo.SpellDID.Value);

            var castableMod = CasterSlotSpells.IsOrb(wo) ? 5.0f : 2.5f;

            //wo.ItemManaCost = (int)(spell.BaseMana * castableMod);
            wo.ItemManaCost = (int)spell.BaseMana;

            wo.ItemUseable = Usable.SourceWieldedTargetRemoteNeverWalk;

            wo.CooldownId = 1001;
            wo.CooldownDuration = 10;
        }

        private static string GetCasterScript(bool isElemental = false)
        {
            var elementalStr = isElemental ? "elemental" : "non_elemental";

            return $"Casters.caster_{elementalStr}.txt";
        }

        private static bool GetMutateCasterData(uint wcid)
        {
            for (var i = 0; i < LootTables.CasterWeaponsMatrix.Length; i++)
            {
                var table = LootTables.CasterWeaponsMatrix[i];

                for (var element = 0; element < table.Length; element++)
                {
                    if (wcid == table[element])
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Return caster subtype
        /// Orb = 0, Scepter = 1, Wand/Baton = 2, Staff = 3
        /// </summary>
        private static int GetCasterSubType(WorldObject wo)
        {
            var subType = 0;
            if (wo.Tier <= 5)
            {
                for (int i = 0; i < LootTables.TimelineCasterWeaponsMatrix[0].Length; i++)
                {
                    if (wo.WeenieClassId == LootTables.TimelineCasterWeaponsMatrix[0][i])
                        subType = i;
                }
            }
            else
            {
                for (int i = 1; i < LootTables.TimelineCasterWeaponsMatrix.Length; i++)
                {
                    foreach (int type in LootTables.TimelineCasterWeaponsMatrix[i])
                    {
                        if (wo.WeenieClassId == type)
                            subType = i - 1;
                    }
                }
            }

            return subType; 
        }

        private static bool RollMagicSkillMod(TreasureDeath treasureDeath, WorldObject wo)
        {
            var magicSkillMod = 0.0f;
            var maxMagicSkillMod = (treasureDeath.Tier * 2.5f) / 100;
            var lootQualityMod = treasureDeath.LootQualityMod * 100;
            var roll = ThreadSafeRandom.Next((int)lootQualityMod, 100);

            if (treasureDeath.Tier == 1)
            {
                //switch (roll)
                //{
                //    case <= 50: elementBonus += 0.0f; break;
                //    case <= 75: elementBonus += 0.005f; break;
                //    case <= 90: elementBonus += 0.01f; break;
                //    case <= 95: elementBonus += 0.015f; break;
                //    case <= 99: elementBonus += 0.02f; break;
                //    case 100: elementBonus += 0.025f; break;
                //}
            }
            else if (treasureDeath.Tier == 2)
            {
                switch (roll)
                {
                    case <= 50: magicSkillMod += 0.0f; break;
                    case <= 75: magicSkillMod += (float)ThreadSafeRandom.Next(0, 2) / 2 / 100; break;
                    case <= 90: magicSkillMod += (float)ThreadSafeRandom.Next(2, 4) / 2 / 100; break;
                    case <= 95: magicSkillMod += (float)ThreadSafeRandom.Next(4, 6) / 2 / 100; break;
                    case <= 99: magicSkillMod += (float)ThreadSafeRandom.Next(6, 8) / 2 / 100; break;
                    case 100: magicSkillMod += (float)ThreadSafeRandom.Next(8, 10) / 2 / 100; break;
                }
            }
            else if (treasureDeath.Tier == 3)
            {
                switch (roll)
                {
                    case <= 50: magicSkillMod += 0.0f; break;
                    case <= 75: magicSkillMod += (float)ThreadSafeRandom.Next(0, 3) / 2 / 100; break;
                    case <= 90: magicSkillMod += (float)ThreadSafeRandom.Next(3, 6) / 2 / 100; break;
                    case <= 95: magicSkillMod += (float)ThreadSafeRandom.Next(6, 9) / 2 / 100; break;
                    case <= 99: magicSkillMod += (float)ThreadSafeRandom.Next(9, 12) / 2 / 100; break;
                    case 100: magicSkillMod += (float)ThreadSafeRandom.Next(12, 15) / 2 / 100; break;
                }
            }
            else
            {
                switch (roll)
                {
                    case <= 50: magicSkillMod += 0.0f; break;
                    case <= 75: magicSkillMod += (float)(maxMagicSkillMod - 0.10 + (float)ThreadSafeRandom.Next(0, 4) / 2 / 100); break;
                    case <= 90: magicSkillMod += (float)(maxMagicSkillMod - 0.10 + (float)ThreadSafeRandom.Next(4, 8) / 2 / 100); break;
                    case <= 95: magicSkillMod += (float)(maxMagicSkillMod - 0.10 + (float)ThreadSafeRandom.Next(8, 12) / 2 / 100); break;
                    case <= 99: magicSkillMod += (float)(maxMagicSkillMod - 0.10 + (float)ThreadSafeRandom.Next(12, 16) / 2 / 100); break;
                    case 100: magicSkillMod += (float)(maxMagicSkillMod - 0.10 + (float)ThreadSafeRandom.Next(16, 20) / 2 / 100); break;
                }
            }

            // TODO Make MagicSkillMod float for casters
            //if (magicSkillMod >= 0.1f)
            //    wo.WeaponOffense += magicSkillMod;

            return true;
        }

        /// <summary>
        /// Rolls for the Element for casters
        /// </summary>
        private static void RollCasterElement(TreasureDeath profile, WorldObject wo)
        {
            var noElement = ThreadSafeRandom.Next(0, 3) == 0 ? true : false;
            var elementType = 0;
            var materialType = 0;
            var uiEffect = 0;

            if (noElement)
            {
                var material = GetMaterialType(wo, profile.Tier);
                if (material > 0)
                    wo.MaterialType = material;
                wo.UiEffects = UiEffects.BoostHealth;

                return;
            }

            var roll = ThreadSafeRandom.Next(1, 7);
            switch (roll)
            {
                case 1:
                    elementType = 0x1; // slash
                    materialType = 0x0000001A; // imperial topaz
                    uiEffect = 0x0400;
                    break; 
                case 2:
                    elementType = 0x2; // pierce
                    materialType = 0x0000000F; // black garnet
                    uiEffect = 0x0800; 
                    break; 
                case 3:
                    elementType = 0x4; // bludge
                    materialType = 0x0000002F; // white saphhire
                    uiEffect = 0x0200; 
                    break;
                case 4:
                    elementType = 0x8; // cold
                    materialType = 0x0000000D; // aquamarine
                    uiEffect = 0x0080;
                    break;
                case 5:
                    elementType = 0x10; // fire
                    materialType = 0x00000023; // red garnet
                    uiEffect = 0x0020; 
                    break;
                case 6:
                    elementType = 0x20; // acid
                    materialType = 0x00000015; // emerald
                    uiEffect = 0x0100; 
                    break;
                case 7:
                    elementType = 0x40; // electric
                    materialType = 0x0000001B; // jet
                    uiEffect = 0x0040; 
                    break;
            }
            wo.W_DamageType = (DamageType)elementType;
            wo.MaterialType = (MaterialType)materialType;
            wo.UiEffects = (UiEffects)uiEffect;
        }

        /// <summary>
        /// Rolls Bonus Crit Chance for Scepters
        /// </summary>
        private static void RollBonusCritChance(TreasureDeath treasureDeath, WorldObject wo)
        {
            var lootQualityMod = treasureDeath.LootQualityMod * 100;
            var roll = ThreadSafeRandom.Next((int)lootQualityMod, 100);
            var tier = treasureDeath.Tier;

            var critChanceMod = 0.1f * GetDiminishingRoll(treasureDeath);

            wo.SetProperty(PropertyFloat.CriticalFrequency, 0.1f + critChanceMod);
        }

        /// <summary>
        /// Rolls Bonus Crit Chance for Batons
        /// </summary>
        private static void RollBonusCritDamage(TreasureDeath treasureDeath, WorldObject wo)
        {;
            var lootQualityMod = treasureDeath.LootQualityMod * 100;
            var roll = ThreadSafeRandom.Next((int)lootQualityMod, 100);
            var tier = treasureDeath.Tier;

            var critDamageMod = 1.0f * GetDiminishingRoll(treasureDeath);

            if (wo.GetProperty(PropertyFloat.CriticalMultiplier) != null)
            {
                var currentCritMultipier = wo.GetProperty(PropertyFloat.CriticalMultiplier);
                var newCritMultipier = currentCritMultipier + 1.0f + critDamageMod;
                wo.SetProperty(PropertyFloat.CriticalMultiplier, (float)newCritMultipier);
            }
            else
            {
                var newCritMultipier = 1.0f + critDamageMod;
                wo.SetProperty(PropertyFloat.CriticalMultiplier, (float)newCritMultipier);
            }
        }

        /// <summary>
        /// Rolls Bonus Defense Mods for Orbs (magic) and Staffs (melee)
        /// </summary>
        private static float BonusDefenseMod(TreasureDeath treasureDeath, WorldObject wo)
        {
            var lootQualityMod = treasureDeath.LootQualityMod * 100;
            var roll = ThreadSafeRandom.Next((int)lootQualityMod, 100);
            var tier = treasureDeath.Tier;

            var defenseMod = 0.1f + 0.1f * GetDiminishingRoll(treasureDeath);

            return defenseMod;
        }

        /// <summary>
        /// Rolls for the ManaConversionMod for casters
        /// </summary>
        private static double RollManaConversionMod(int tier)
        {
            int magicMod = 0;

            int chance = 0;
            switch (tier)
            {
                case 1:
                case 2:
                    magicMod = 0;
                    break;
                case 3:
                    chance = ThreadSafeRandom.Next(1, 1000);
                    if (chance > 900)
                        magicMod = 5;
                    else if (chance > 800)
                        magicMod = 4;
                    else if (chance > 700)
                        magicMod = 3;
                    else if (chance > 600)
                        magicMod = 2;
                    else if (chance > 500)
                        magicMod = 1;
                    break;
                case 4:
                    chance = ThreadSafeRandom.Next(1, 1000);
                    if (chance > 900)
                        magicMod = 10;
                    else if (chance > 800)
                        magicMod = 9;
                    else if (chance > 700)
                        magicMod = 8;
                    else if (chance > 600)
                        magicMod = 7;
                    else if (chance > 500)
                        magicMod = 6;
                    else
                        magicMod = 5;
                    break;
                case 5:
                    chance = ThreadSafeRandom.Next(1, 1000);
                    if (chance > 900)
                        magicMod = 10;
                    else if (chance > 800)
                        magicMod = 9;
                    else if (chance > 700)
                        magicMod = 8;
                    else if (chance > 600)
                        magicMod = 7;
                    else if (chance > 500)
                        magicMod = 6;
                    else
                        magicMod = 5;
                    break;
                case 6:
                    chance = ThreadSafeRandom.Next(1, 1000);
                    if (chance > 900)
                        magicMod = 10;
                    else if (chance > 800)
                        magicMod = 9;
                    else if (chance > 700)
                        magicMod = 8;
                    else if (chance > 600)
                        magicMod = 7;
                    else if (chance > 500)
                        magicMod = 6;
                    else
                        magicMod = 5;
                    break;
                case 7:
                    chance = ThreadSafeRandom.Next(1, 1000);
                    if (chance > 900)
                        magicMod = 10;
                    else if (chance > 800)
                        magicMod = 9;
                    else if (chance > 700)
                        magicMod = 8;
                    else if (chance > 600)
                        magicMod = 7;
                    else if (chance > 500)
                        magicMod = 6;
                    else
                        magicMod = 5;
                    break;
                default:
                    chance = ThreadSafeRandom.Next(1, 1000);
                    if (chance > 900)
                        magicMod = 10;
                    else if (chance > 800)
                        magicMod = 9;
                    else if (chance > 700)
                        magicMod = 8;
                    else if (chance > 600)
                        magicMod = 7;
                    else if (chance > 500)
                        magicMod = 6;
                    else
                        magicMod = 5;
                    break;
            }

            double manaDMod = magicMod / 100.0;

            return manaDMod;
        }

        /// <summary>
        /// Rolls for ElementalDamageMod for caster weapons
        /// </summary>
        private static double RollElementalDamageMod(int? wield, TreasureDeath treasureDeath = null)
        {
            double elementBonus = 0;

            int[] maxModPerTier = { 10, 20, 30, 40, 50, 75, 100, 126 };
            var maxMod = (float)maxModPerTier[treasureDeath.Tier - 1] / 100;
            var minMod = (float)maxModPerTier[treasureDeath.Tier - 1] / 200;
            var bonusAmount = (maxMod - minMod) * GetDiminishingRoll(treasureDeath);

            elementBonus = minMod + bonusAmount;

            elementBonus += 1;

            return elementBonus;
        }

        /// <summary>
        /// Rolls for RestorationSpellMod for caster weapons
        /// </summary>
        private static double RollRestorationSpellsMod(int? wield, TreasureDeath treasureDeath = null)
        {
            int[] maxModPerTier = { 10, 20, 30, 40, 50, 75, 100, 126 };
            var maxMod = (float)maxModPerTier[treasureDeath.Tier - 1] / 100;
            var minMod = (float)maxModPerTier[treasureDeath.Tier - 1] / 200;
            var bonusAmount = (maxMod - minMod) * GetDiminishingRoll(treasureDeath);

            var restorationMod = minMod + bonusAmount + 1;

            return restorationMod;
        }


        private static void MutateScepterColor(WorldObject wo)
        {
            wo.IgnoreCloIcons = true;

            switch (wo.W_DamageType)
            {
                case DamageType.Undef:
                    wo.IconId = 0x0600200F;
                    wo.PaletteTemplate = 0;
                    wo.UiEffects = UiEffects.BoostHealth;
                    break;
                case DamageType.Slash:
                    wo.IconId = 0x06002011;
                    wo.PaletteTemplate = 4;
                    wo.UiEffects = (UiEffects)0x0400;
                    break;
                case DamageType.Pierce:
                    wo.IconId = 0x06002012;
                    wo.PaletteTemplate = 21;
                    wo.UiEffects = (UiEffects)0x0800;
                    break;
                case DamageType.Bludgeon:
                    wo.IconId = 0x06002013;
                    wo.PaletteTemplate = 61;
                    wo.UiEffects = (UiEffects)0x0200;
                    break;
                case DamageType.Acid:
                    wo.IconId = 0x06002014;
                    wo.PaletteTemplate = 8;
                    wo.UiEffects = (UiEffects)0x0100;
                    break;
                case DamageType.Fire:
                    wo.IconId = 0x06002015;
                    wo.PaletteTemplate = 14;
                    wo.UiEffects = (UiEffects)0x0020;
                    break;
                case DamageType.Cold:
                    wo.IconId = 0x06002016;
                    wo.PaletteTemplate = 2;
                    wo.UiEffects = (UiEffects)0x0080;
                    break;
                case DamageType.Electric:
                    wo.IconId = 0x06002017;
                    wo.PaletteTemplate = 82;
                    wo.UiEffects = (UiEffects)0x0040;
                    break;
            }
        }

        private static void TryMutateCasterWeaponDamage(WorldObject wo, TreasureRoll roll, TreasureDeath profile, out double damagePercentile)
        {
            // If Staff or Orb reduce damage by 50%
            var defensiveCasterMultiplier = GetCasterSubType(wo) == 0 || GetCasterSubType(wo) == 3 ? 0.5f : 1.0f;

            // Calculate Max, Min, and Roll
            var maxDamageMod = GetCasterMaxDamageMod(wo)[profile.Tier - 1] * defensiveCasterMultiplier / 100;
            var minDamageMod = (profile.Tier > 1 ? (float)(GetCasterMaxDamageMod(wo)[profile.Tier - 2] - 5) : 1) / 100 * defensiveCasterMultiplier;
            var diminishedDamageModRoll = (maxDamageMod - minDamageMod) * GetDiminishingRoll(profile);

            var maxPossibleDamage = GetCasterMaxDamageMod(wo)[7] * defensiveCasterMultiplier / 100;

            // Elemental or Resto?
            if (wo.W_DamageType != DamageType.Undef)
            {
                wo.ElementalDamageMod = minDamageMod + diminishedDamageModRoll + 1;
                damagePercentile = ((double)wo.ElementalDamageMod - 1) / maxPossibleDamage;
            }
            else
            {
                wo.WeaponRestorationSpellsMod = minDamageMod + diminishedDamageModRoll + 1;
                damagePercentile = ((double)wo.WeaponRestorationSpellsMod - 1) / maxPossibleDamage;
            }
        }

        private static int GetCasterWorkmanship(WorldObject wo, double damagePercentile, double modsPercentile)
        {
            var divisor = 0;

            // Damage
            divisor++;

            // Weapon Mods
            divisor++;

            // Average Percentile
            var finalPercentile = (damagePercentile + modsPercentile) / divisor;

            //Console.WriteLine($"{wo.Name}\n -Mods %: {modsPercentile}\n" + $" -Damage %: {damagePercentile}\n" + $" -Divisor: {divisor}\n" +
            //    $" --FINAL: {finalPercentile}\n\n");

            // Workmanship Calculation
            return Math.Max((int)Math.Round(finalPercentile * 10, 0), 1);
        }
    }
}
