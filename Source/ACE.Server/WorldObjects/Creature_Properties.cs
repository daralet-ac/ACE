using System;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Server.Managers;
using ACE.Server.Network.GameMessages.Messages;

namespace ACE.Server.WorldObjects
{
    partial class Creature
    {
        public double? ResistSlash
        {
            get => GetProperty(PropertyFloat.ResistSlash);
            set { if (!value.HasValue) RemoveProperty(PropertyFloat.ResistSlash); else SetProperty(PropertyFloat.ResistSlash, value.Value); }
        }

        public double? ResistPierce
        {
            get => GetProperty(PropertyFloat.ResistPierce);
            set { if (!value.HasValue) RemoveProperty(PropertyFloat.ResistPierce); else SetProperty(PropertyFloat.ResistPierce, value.Value); }
        }

        public double? ResistBludgeon
        {
            get => GetProperty(PropertyFloat.ResistBludgeon);
            set { if (!value.HasValue) RemoveProperty(PropertyFloat.ResistBludgeon); else SetProperty(PropertyFloat.ResistBludgeon, value.Value); }
        }

        public double? ResistFire
        {
            get => GetProperty(PropertyFloat.ResistFire);
            set { if (!value.HasValue) RemoveProperty(PropertyFloat.ResistFire); else SetProperty(PropertyFloat.ResistFire, value.Value); }
        }

        public double? ResistCold
        {
            get => GetProperty(PropertyFloat.ResistCold);
            set { if (!value.HasValue) RemoveProperty(PropertyFloat.ResistCold); else SetProperty(PropertyFloat.ResistCold, value.Value); }
        }

        public double? ResistAcid
        {
            get => GetProperty(PropertyFloat.ResistAcid);
            set { if (!value.HasValue) RemoveProperty(PropertyFloat.ResistAcid); else SetProperty(PropertyFloat.ResistAcid, value.Value); }
        }

        public double? ResistElectric
        {
            get => GetProperty(PropertyFloat.ResistElectric);
            set { if (!value.HasValue) RemoveProperty(PropertyFloat.ResistElectric); else SetProperty(PropertyFloat.ResistElectric, value.Value); }
        }

        public double? ResistHealthDrain
        {
            get => GetProperty(PropertyFloat.ResistHealthDrain);
            set { if (!value.HasValue) RemoveProperty(PropertyFloat.ResistHealthDrain); else SetProperty(PropertyFloat.ResistHealthDrain, value.Value); }
        }

        public double? ResistHealthBoost
        {
            get => GetProperty(PropertyFloat.ResistHealthBoost);
            set { if (!value.HasValue) RemoveProperty(PropertyFloat.ResistHealthBoost); else SetProperty(PropertyFloat.ResistHealthBoost, value.Value); }
        }

        public double? ResistStaminaDrain
        {
            get => GetProperty(PropertyFloat.ResistStaminaDrain);
            set { if (!value.HasValue) RemoveProperty(PropertyFloat.ResistStaminaDrain); else SetProperty(PropertyFloat.ResistStaminaDrain, value.Value); }
        }

        public double? ResistStaminaBoost
        {
            get => GetProperty(PropertyFloat.ResistStaminaBoost);
            set { if (!value.HasValue) RemoveProperty(PropertyFloat.ResistStaminaBoost); else SetProperty(PropertyFloat.ResistStaminaBoost, value.Value); }
        }

        public double? ResistManaDrain
        {
            get => GetProperty(PropertyFloat.ResistManaDrain);
            set { if (!value.HasValue) RemoveProperty(PropertyFloat.ResistManaDrain); else SetProperty(PropertyFloat.ResistManaDrain, value.Value); }
        }

        public double? ResistManaBoost
        {
            get => GetProperty(PropertyFloat.ResistManaBoost);
            set { if (!value.HasValue) RemoveProperty(PropertyFloat.ResistManaBoost); else SetProperty(PropertyFloat.ResistManaBoost, value.Value); }
        }

        public double? ResistNether
        {
            get => GetProperty(PropertyFloat.ResistNether);
            set { if (!value.HasValue) RemoveProperty(PropertyFloat.ResistNether); else SetProperty(PropertyFloat.ResistNether, value.Value); }
        }

        public bool NonProjectileMagicImmune
        {
            get => GetProperty(PropertyBool.NonProjectileMagicImmune) ?? false;
            set { if (!value) RemoveProperty(PropertyBool.NonProjectileMagicImmune); else SetProperty(PropertyBool.NonProjectileMagicImmune, value); }
        }

        public float GetResistanceMod(DamageType damageType, WorldObject attacker, WorldObject weapon, float weaponResistanceMod = 1.0f)
        {
            var ignoreMagicResist = (weapon?.IgnoreMagicResist ?? false) || (attacker?.IgnoreMagicResist ?? false);

            // hollow weapons also ignore player natural resistances
            if (ignoreMagicResist)
            {
                if (!(attacker is Player) || !(this is Player) || PropertyManager.GetDouble("ignore_magic_resist_pvp_scalar").Item == 1.0)
                    return weaponResistanceMod;
            }

            var protMod = EnchantmentManager.GetProtectionResistanceMod(damageType);
            var vulnMod = EnchantmentManager.GetVulnerabilityResistanceMod(damageType);

            var naturalResistMod = GetNaturalResistance(damageType);

            // protection mod becomes either life protection or natural resistance,
            // whichever is more powerful (more powerful = lower value here)
            if (protMod > naturalResistMod)
                protMod = naturalResistMod;

            // does this stack with natural resistance?
            if (this is Player player)
            {
                var resistAug = player.GetAugmentationResistance(damageType);
                if (resistAug > 0)
                {
                    var augFactor = Math.Min(1.0f, resistAug * 0.1f);
                    protMod *= 1.0f - augFactor;
                }
            }

            // vulnerability mod becomes either life vuln or weapon resistance mod,
            // whichever is more powerful
            if (vulnMod < weaponResistanceMod)
                vulnMod = weaponResistanceMod;

            if (ignoreMagicResist)
            {
                // convert to additive space
                var addProt = -ModToRating(protMod);
                var addVuln = ModToRating(vulnMod);

                // scale
                addProt = IgnoreMagicResistScaled(addProt);
                addVuln = IgnoreMagicResistScaled(addVuln);

                protMod = GetNegativeRatingMod(addProt);
                vulnMod = GetPositiveRatingMod(addVuln);
            }

            return protMod * vulnMod;
        }

        public virtual float GetNaturalResistance(DamageType damageType)
        {
            // overridden for players
            return 1.0f;
        }

        public double GetArmorVsType(DamageType damageType)
        {
            switch (damageType)
            {
                case DamageType.Slash:
                    return GetProperty(PropertyFloat.ArmorModVsSlash) ?? 1.0f;
                case DamageType.Pierce:
                    return GetProperty(PropertyFloat.ArmorModVsPierce) ?? 1.0f;
                case DamageType.Bludgeon:
                    return GetProperty(PropertyFloat.ArmorModVsBludgeon) ?? 1.0f;
                case DamageType.Fire:
                    return GetProperty(PropertyFloat.ArmorModVsFire) ?? 1.0f;
                case DamageType.Cold:
                    return GetProperty(PropertyFloat.ArmorModVsCold) ?? 1.0f;
                case DamageType.Acid:
                    return GetProperty(PropertyFloat.ArmorModVsAcid) ?? 1.0f;
                case DamageType.Electric:
                    return GetProperty(PropertyFloat.ArmorModVsElectric) ?? 1.0f;
                case DamageType.Nether:
                    return GetProperty(PropertyFloat.ArmorModVsNether) ?? 1.0f;
                default:
                    return 1.0f;
            }
        }

        public double GetResistanceMod(ResistanceType resistance, WorldObject attacker = null, WorldObject weapon = null, float weaponResistanceMod = 1.0f)
        {
            switch (resistance)
            {
                case ResistanceType.Slash:
                    return (ResistSlash ?? 1.0) * GetResistanceMod(DamageType.Slash, attacker, weapon, weaponResistanceMod);
                case ResistanceType.Pierce:
                    return (ResistPierce ?? 1.0) * GetResistanceMod(DamageType.Pierce, attacker, weapon, weaponResistanceMod);
                case ResistanceType.Bludgeon:
                    return (ResistBludgeon ?? 1.0) * GetResistanceMod(DamageType.Bludgeon, attacker, weapon, weaponResistanceMod);
                case ResistanceType.Fire:
                    return (ResistFire ?? 1.0) * GetResistanceMod(DamageType.Fire, attacker, weapon, weaponResistanceMod);
                case ResistanceType.Cold:
                    return (ResistCold ?? 1.0) * GetResistanceMod(DamageType.Cold, attacker, weapon, weaponResistanceMod);
                case ResistanceType.Acid:
                    return (ResistAcid ?? 1.0) * GetResistanceMod(DamageType.Acid, attacker, weapon, weaponResistanceMod);
                case ResistanceType.Electric:
                    return (ResistElectric ?? 1.0) * GetResistanceMod(DamageType.Electric, attacker, weapon, weaponResistanceMod);
                case ResistanceType.Nether:
                    return (ResistNether ?? 1.0) * GetResistanceMod(DamageType.Nether, attacker, weapon, weaponResistanceMod);
                case ResistanceType.HealthBoost:
                    return (ResistHealthBoost ?? 1.0) * GetHealingRatingMod();
                case ResistanceType.HealthDrain:
                    return (ResistHealthDrain ?? 1.0) * GetNaturalResistance(DamageType.Health) * GetLifeResistRatingMod();
                case ResistanceType.StaminaBoost:
                    return (ResistStaminaBoost ?? 1.0) * GetHealingRatingMod();     // does healing rating affect these?
                case ResistanceType.StaminaDrain:
                    return (ResistStaminaDrain ?? 1.0) * GetNaturalResistance(DamageType.Stamina);
                case ResistanceType.ManaBoost:
                    return (ResistManaBoost ?? 1.0) * GetHealingRatingMod();
                case ResistanceType.ManaDrain:
                    return (ResistManaDrain ?? 1.0) * GetNaturalResistance(DamageType.Mana);
                default:
                    return 1.0;
            }
        }

        public int GetAegisLevel()
        {
            var aegisLevel = 0;

            if(AegisLevel != null && AegisLevel.HasValue)
                aegisLevel = (int)AegisLevel;
            else
                aegisLevel = GetEquippedItemsAegisSum(PropertyInt.AegisLevel);

            return aegisLevel;
        }

        public double? GetArmorHealthMod()
        {
            double? mod;

            if (ArmorHealthMod != null && ArmorHealthMod.HasValue)
                mod = ArmorHealthMod;
            else
                mod = GetEquippedItemsSkillModSum(PropertyFloat.ArmorHealthMod);

            return mod;
        }

        public double? GetArmorHealthRegenMod()
        {
            double? mod;

            if (ArmorHealthRegenMod != null && ArmorHealthRegenMod.HasValue)
                mod = ArmorHealthRegenMod;
            else
                mod = GetEquippedItemsSkillModSum(PropertyFloat.ArmorHealthRegenMod);

            return mod;
        }

        public uint GetModdedHealthRegenVital()
        {
            var healthVital = GetCreatureVital(PropertyAttribute2nd.Health);
            var armorHealthVitalMod = GetArmorHealthRegenMod() + 1;
            var tempHealthVital = healthVital.MaxValue * armorHealthVitalMod;
            var moddedHealthVital = (uint)tempHealthVital;

            return moddedHealthVital - healthVital.MaxValue;
        }

        public double? GetArmorStaminaMod()
        {
            double? mod;

            if (ArmorStaminaMod != null && ArmorStaminaMod.HasValue)
                mod = ArmorStaminaMod;
            else
                mod = GetEquippedItemsSkillModSum(PropertyFloat.ArmorStaminaMod);

            return mod;
        }

        public double? GetArmorStaminaRegenMod()
        {
            double? mod;

            if (ArmorStaminaRegenMod != null && ArmorStaminaRegenMod.HasValue)
                mod = ArmorStaminaRegenMod;
            else
                mod = GetEquippedItemsSkillModSum(PropertyFloat.ArmorStaminaRegenMod);

            return mod;
        }

        public uint GetModdedStaminaRegenVital()
        {
            var staminaVital = GetCreatureVital(PropertyAttribute2nd.MaxStamina);
            var armorStaminaVitalMod = GetArmorStaminaRegenMod() + 1;
            var tempStaminaVital = staminaVital.Current * armorStaminaVitalMod;
            var moddedStaminaVital = (uint)tempStaminaVital;

            return moddedStaminaVital;
        }

        public double? GetArmorManaMod()
        {
            double? mod;

            if (ArmorManaMod != null && ArmorManaMod.HasValue)
                mod = ArmorManaMod;
            else
                mod = GetEquippedItemsSkillModSum(PropertyFloat.ArmorManaMod);

            return mod;
        }

        public double? GetArmorManaRegenMod()
        {
            double? mod;

            if (ArmorManaRegenMod != null && ArmorManaRegenMod.HasValue)
                mod = ArmorManaRegenMod;
            else
                mod = GetEquippedItemsSkillModSum(PropertyFloat.ArmorManaRegenMod);

            return mod;
        }

        public uint GetModdedManaRegenVital()
        {
            var manaVital = GetCreatureVital(PropertyAttribute2nd.MaxMana);
            var armorManaVitalMod = GetArmorManaRegenMod() + 1;
            var tempManaVital = manaVital.Current * armorManaVitalMod;
            var moddedManaVital = (uint)tempManaVital;

            return moddedManaVital;
        }

        public double? GetArmorAttackMod()
        {
            double? mod;

            if (ArmorAttackMod != null && ArmorAttackMod.HasValue)
                mod = ArmorAttackMod;
            else
                mod = GetEquippedItemsSkillModSum(PropertyFloat.ArmorAttackMod);

            return mod;
        }

        public double? GetArmorPhysicalDefMod()
        {
            double? mod;

            if (ArmorPhysicalDefMod != null && ArmorPhysicalDefMod.HasValue)
                mod = ArmorPhysicalDefMod;
            else
                mod = GetEquippedItemsSkillModSum(PropertyFloat.ArmorPhysicalDefMod);

            return mod;
        }

        public uint GetModdedMeleeDefSkill()
        {
            var meleeDefSkill = GetCreatureSkill(Skill.MeleeDefense);
            var armorMeleeDefSkillMod = GetArmorPhysicalDefMod() + 1;
            var tempMeleeDefSkill = meleeDefSkill.Current * armorMeleeDefSkillMod;
            var moddedMeleeDefSkill = (uint)tempMeleeDefSkill;

            return moddedMeleeDefSkill;
        }

        public double? GetArmorMissileDefMod()
        {
            double? mod;

            if (ArmorMissileDefMod != null && ArmorMissileDefMod.HasValue)
                mod = ArmorMissileDefMod;
            else
                mod = GetEquippedItemsSkillModSum(PropertyFloat.ArmorMissileDefMod);

            return mod;
        }

        public uint GetModdedMissileDefSkill()
        {
            var missileDefSkill = GetCreatureSkill(Skill.MissileDefense);
            var armorMissileDefSkillMod = GetArmorMissileDefMod() + 1;
            var tempMissileDefSkill = missileDefSkill.Current * armorMissileDefSkillMod;
            var moddedMissileDefSkill = (uint)tempMissileDefSkill;

            return moddedMissileDefSkill;
        }

        public double? GetArmorMagicDefMod()
        {
            double? mod;

            if (ArmorMagicDefMod != null && ArmorMagicDefMod.HasValue)
                mod = ArmorMagicDefMod;
            else
                mod = GetEquippedItemsSkillModSum(PropertyFloat.ArmorMagicDefMod);

            return mod;
        }

        public uint GetModdedMagicDefSkill()
        {
            var magicDefSkill = GetCreatureSkill(Skill.MagicDefense);
            var armorMagicDefSkillMod = GetArmorMagicDefMod() + 1;
            var tempMagicDefSkill = magicDefSkill.Current * armorMagicDefSkillMod;
            var moddedMagicDefSkill = (uint)tempMagicDefSkill;

            return moddedMagicDefSkill;
        }

        public double? GetArmorRunMod()
        {
            double? mod;

            //if (ArmorRunMod != null && ArmorRunMod.HasValue)
            //    mod = ArmorRunMod;
            if (ArmorRunMod != null && IsMonster)
                mod = ArmorRunMod;
            else
                mod = GetEquippedItemsSkillModSum(PropertyFloat.ArmorRunMod);

            return mod;
        }

        public uint GetModdedRunSkill()
        {
            var runSkill = GetCreatureSkill(Skill.Run);
            var armorRunSkillMod = GetArmorRunMod() + 1;
            var tempRunSkill = runSkill.Current * armorRunSkillMod;
            var moddedRunSkill = (uint)tempRunSkill;

            return moddedRunSkill;
        }

        public double? GetArmorDualWieldMod()
        {
            double? mod;

            if (ArmorDualWieldMod != null && ArmorDualWieldMod.HasValue)
                mod = ArmorDualWieldMod;
            else
                mod = GetEquippedItemsSkillModSum(PropertyFloat.ArmorDualWieldMod);

            return mod;
        }

        public uint GetModdedDualWieldSkill()
        {
            var dualWieldSkill = GetCreatureSkill(Skill.DualWield);
            var armorDualWieldSkillMod = GetArmorDualWieldMod() + 1;
            var tempDualWieldSkill = dualWieldSkill.Current * armorDualWieldSkillMod;
            var moddedDualWieldSkill = (uint)tempDualWieldSkill;

            return moddedDualWieldSkill;
        }

        public double? GetArmorTwohandedCombatMod()
        {
            double? mod;

            if (ArmorTwohandedCombatMod != null && ArmorTwohandedCombatMod.HasValue)
                mod = ArmorTwohandedCombatMod;
            else
                mod = GetEquippedItemsSkillModSum(PropertyFloat.ArmorTwohandedCombatMod);

            return mod;
        }

        public uint GetModdedTwohandedCombatSkill()
        {
            var twohandedCombatSkill = GetCreatureSkill(Skill.TwoHandedCombat);
            var armorTwohandedCombatSkillMod = GetArmorTwohandedCombatMod() + 1;
            var tempTwohandedCombatSkill = twohandedCombatSkill.Current * armorTwohandedCombatSkillMod;
            var moddedTwohandedCombatSkill = (uint)tempTwohandedCombatSkill;

            return moddedTwohandedCombatSkill;
        }

        public double? GetArmorThieveryMod()
        {
            double? mod;

            if (ArmorThieveryMod != null && ArmorThieveryMod.HasValue)
                mod = ArmorThieveryMod;
            else
                mod = GetEquippedItemsSkillModSum(PropertyFloat.ArmorThieveryMod);

            return mod;
        }

        public uint GetModdedThieverySkill()
        {
            var thieverySkill = GetCreatureSkill(Skill.Lockpick); // Thievery
            var armorThieverySkillMod = GetArmorThieveryMod() + 1;
            var tempThieverySkill = thieverySkill.Current * armorThieverySkillMod;
            var moddedThieverySkill = (uint)tempThieverySkill;

            return moddedThieverySkill;
        }

        public double? GetArmorShieldMod()
        {
            double? mod;

            if (ArmorShieldMod != null && ArmorShieldMod.HasValue)
                mod = ArmorShieldMod;
            else
                mod = GetEquippedItemsSkillModSum(PropertyFloat.ArmorShieldMod);

            return mod;
        }

        public uint GetModdedShieldSkill()
        {
            var shieldSkill = GetCreatureSkill(Skill.Shield);
            var armorShieldSkillMod = GetArmorShieldMod() + 1;
            var tempShieldSkill = shieldSkill.Current * armorShieldSkillMod;
            var moddedShieldSkill = (uint)tempShieldSkill;

            return moddedShieldSkill;
        }

        public double? GetArmorAssessMod()
        {
            double? mod;

            if (ArmorPerceptionMod != null && ArmorPerceptionMod.HasValue)
                mod = ArmorPerceptionMod;
            else
                mod = GetEquippedItemsSkillModSum(PropertyFloat.ArmorPerceptionMod);

            return mod;
        }

        public uint GetModdedPerceptionSkill()
        {
            var assessSkill = GetCreatureSkill(Skill.AssessCreature);
            var armorAssessSkillMod = GetArmorAssessMod() + 1;
            var tempAssessSkill = assessSkill.Current * armorAssessSkillMod;
            var moddedAssessSkill = (uint)tempAssessSkill;

            return moddedAssessSkill;
        }

        public double? GetArmorDeceptionMod()
        {
            double? mod;

            if (ArmorDeceptionMod != null && ArmorDeceptionMod.HasValue)
                mod = ArmorDeceptionMod;
            else
                mod = GetEquippedItemsSkillModSum(PropertyFloat.ArmorDeceptionMod);

            return mod;
        }

        public uint GetModdedDeceptionSkill()
        {
            var deceptionSkill = GetCreatureSkill(Skill.Deception);
            var armorDeceptionSkillMod = GetArmorDeceptionMod() + 1;
            var tempDeceptionSkill = deceptionSkill.Current * armorDeceptionSkillMod;
            var moddedDeceptionSkill = (uint)tempDeceptionSkill;

            return moddedDeceptionSkill;
        }

        public double? GetArmorWarMagicMod()
        {
            double? mod;

            if (ArmorWarMagicMod != null && ArmorWarMagicMod.HasValue)
                mod = ArmorWarMagicMod;
            else
                mod = GetEquippedItemsSkillModSum(PropertyFloat.ArmorWarMagicMod);

            return mod;
        }

        public double? GetWeaponWarMagicMod()
        {
            double? mod;

            if (ArmorWarMagicMod != null && WeaponWarMagicMod.HasValue)
                mod = WeaponWarMagicMod;
            else
                mod = GetEquippedItemsSkillModSum(PropertyFloat.WeaponWarMagicMod);

            return mod;
        }

        public uint GetModdedWarMagicSkill()
        {
            var warMagicSkill = GetCreatureSkill(Skill.WarMagic);
            var warMagicSkillMod = GetArmorWarMagicMod() + GetWeaponWarMagicMod() + 1;
            var tempWarMagicSkill = warMagicSkill.Current * warMagicSkillMod;
            var moddedWarMagicSkill = (uint)tempWarMagicSkill;

            return moddedWarMagicSkill;
        }

        public double? GetArmorLifeMagicMod()
        {
            double? mod;

            if (ArmorLifeMagicMod != null && ArmorLifeMagicMod.HasValue)
                mod = ArmorLifeMagicMod;
            else
                mod = GetEquippedItemsSkillModSum(PropertyFloat.ArmorLifeMagicMod);

            return mod;
        }

        public double? GetWeaponLifeMagicMod()
        {
            double? mod;

            if (WeaponLifeMagicMod != null && WeaponLifeMagicMod.HasValue)
                mod = WeaponLifeMagicMod;
            else
                mod = GetEquippedItemsSkillModSum(PropertyFloat.WeaponLifeMagicMod);

            return mod;
        }

        public uint GetModdedLifeMagicSkill()
        {
            var lifeMagicSkill = GetCreatureSkill(Skill.LifeMagic);
            var lifeMagicSkillMod = GetArmorLifeMagicMod() + GetWeaponLifeMagicMod() + 1;
            var tempLifeMagicSkill = lifeMagicSkill.Current * lifeMagicSkillMod;
            var moddedLifeMagicSkill = (uint)tempLifeMagicSkill;

            return moddedLifeMagicSkill;
        }

        public double? GetWeaponLifeMagicVitalMod()
        {
            double? mod;

            if (WeaponRestorationSpellsMod != null && WeaponRestorationSpellsMod.HasValue)
                mod = WeaponRestorationSpellsMod;
            else
                mod = GetEquippedItemsSkillModSum(PropertyFloat.WeaponRestorationSpellsMod);

            return mod;
        }

        public double? GetArmorResourcePenalty()
        {
            double? mod;

            if (ArmorResourcePenalty != null && ArmorResourcePenalty.HasValue)
                mod = ArmorResourcePenalty;
            else
                mod = GetEquippedItemsSkillModSum(PropertyFloat.ArmorResourcePenalty);

            return mod;
        }

        public double? GetManaScarabReservedMana()
        {
            double? mod;

            mod = GetEquippedItemsSkillModSum(PropertyFloat.EmpoweredScarabManaReserved);

            return mod;
        }

        public double? HealthRate
        {
            get => GetProperty(PropertyFloat.HealthRate);
            set { if (!value.HasValue) RemoveProperty(PropertyFloat.HealthRate); else SetProperty(PropertyFloat.HealthRate, value.Value); }
        }

        public double? StaminaRate
        {
            get => GetProperty(PropertyFloat.StaminaRate);
            set { if (!value.HasValue) RemoveProperty(PropertyFloat.StaminaRate); else SetProperty(PropertyFloat.StaminaRate, value.Value); }
        }

        public double ResistSlashMod => (ResistSlash ?? 1.0) * EnchantmentManager.GetResistanceMod(DamageType.Slash);
        public double ResistPierceMod => (ResistPierce ?? 1.0) * EnchantmentManager.GetResistanceMod(DamageType.Pierce);
        public double ResistBludgeonMod => (ResistBludgeon ?? 1.0) * EnchantmentManager.GetResistanceMod(DamageType.Bludgeon);
        public double ResistFireMod => (ResistFire ?? 1.0) * EnchantmentManager.GetResistanceMod(DamageType.Fire);
        public double ResistColdMod => (ResistCold ?? 1.0) * EnchantmentManager.GetResistanceMod(DamageType.Cold);
        public double ResistAcidMod => (ResistAcid ?? 1.0) * EnchantmentManager.GetResistanceMod(DamageType.Acid);
        public double ResistElectricMod => (ResistElectric ?? 1.0) * EnchantmentManager.GetResistanceMod(DamageType.Electric);
        public double ResistNetherMod => (ResistNether ?? 1.0) * EnchantmentManager.GetResistanceMod(DamageType.Nether);

        public bool NoCorpse
        {
            get => GetProperty(PropertyBool.NoCorpse) ?? false;
            set { if (!value) RemoveProperty(PropertyBool.NoCorpse); else SetProperty(PropertyBool.NoCorpse, value); }
        }

        public bool TreasureCorpse
        {
            get => GetProperty(PropertyBool.TreasureCorpse) ?? false;
            set { if (!value) RemoveProperty(PropertyBool.TreasureCorpse); else SetProperty(PropertyBool.TreasureCorpse, value); }
        }

        public uint? DeathTreasureType
        {
            get => GetProperty(PropertyDataId.DeathTreasureType);
            set { if (!value.HasValue) RemoveProperty(PropertyDataId.DeathTreasureType); else SetProperty(PropertyDataId.DeathTreasureType, value.Value); }
        }

        public int? LuminanceAward
        {
            get => GetProperty(PropertyInt.LuminanceAward);
            set { if (!value.HasValue) RemoveProperty(PropertyInt.LuminanceAward); else SetProperty(PropertyInt.LuminanceAward, value.Value); }
        }

        public bool AiImmobile
        {
            get => GetProperty(PropertyBool.AiImmobile) ?? false;
            set { if (!value) RemoveProperty(PropertyBool.AiImmobile); else SetProperty(PropertyBool.AiImmobile, value); }
        }

        public int? Overpower
        {
            get => GetProperty(PropertyInt.Overpower);
            set { if (!value.HasValue) RemoveProperty(PropertyInt.Overpower); else SetProperty(PropertyInt.Overpower, value.Value); }
        }

        public int? OverpowerResist
        {
            get => GetProperty(PropertyInt.OverpowerResist);
            set { if (!value.HasValue) RemoveProperty(PropertyInt.OverpowerResist); else SetProperty(PropertyInt.OverpowerResist, value.Value); }
        }

        public string KillQuest
        {
            get => GetProperty(PropertyString.KillQuest);
            set { if (value == null) RemoveProperty(PropertyString.KillQuest); else SetProperty(PropertyString.KillQuest, value); }
        }

        public string KillQuest2
        {
            get => GetProperty(PropertyString.KillQuest2);
            set { if (value == null) RemoveProperty(PropertyString.KillQuest2); else SetProperty(PropertyString.KillQuest2, value); }
        }

        public string KillQuest3
        {
            get => GetProperty(PropertyString.KillQuest3);
            set { if (value == null) RemoveProperty(PropertyString.KillQuest3); else SetProperty(PropertyString.KillQuest3, value); }
        }

        public FactionBits? Faction1Bits
        {
            get => (FactionBits?)GetProperty(PropertyInt.Faction1Bits);
            set { if (!value.HasValue) RemoveProperty(PropertyInt.Faction1Bits); else SetProperty(PropertyInt.Faction1Bits, (int)value); }
        }

        public int? Faction2Bits
        {
            get => GetProperty(PropertyInt.Faction2Bits);
            set { if (!value.HasValue) RemoveProperty(PropertyInt.Faction2Bits); else SetProperty(PropertyInt.Faction2Bits, value.Value); }
        }

        public int? Faction3Bits
        {
            get => GetProperty(PropertyInt.Faction3Bits);
            set { if (!value.HasValue) RemoveProperty(PropertyInt.Faction3Bits); else SetProperty(PropertyInt.Faction3Bits, value.Value); }
        }

        public int? Hatred1Bits
        {
            get => GetProperty(PropertyInt.Hatred1Bits);
            set { if (!value.HasValue) RemoveProperty(PropertyInt.Hatred1Bits); else SetProperty(PropertyInt.Hatred1Bits, value.Value); }
        }

        public int? Hatred2Bits
        {
            get => GetProperty(PropertyInt.Hatred2Bits);
            set { if (!value.HasValue) RemoveProperty(PropertyInt.Hatred2Bits); else SetProperty(PropertyInt.Hatred2Bits, value.Value); }
        }

        public int? Hatred3Bits
        {
            get => GetProperty(PropertyInt.Hatred3Bits);
            set { if (!value.HasValue) RemoveProperty(PropertyInt.Hatred3Bits); else SetProperty(PropertyInt.Hatred3Bits, value.Value); }
        }

        public int? SocietyRankCelhan
        {
            get => GetProperty(PropertyInt.SocietyRankCelhan);
            set { if (!value.HasValue) RemoveProperty(PropertyInt.SocietyRankCelhan); else SetProperty(PropertyInt.SocietyRankCelhan, value.Value); }
        }

        public int? SocietyRankEldweb
        {
            get => GetProperty(PropertyInt.SocietyRankEldweb);
            set { if (!value.HasValue) RemoveProperty(PropertyInt.SocietyRankEldweb); else SetProperty(PropertyInt.SocietyRankEldweb, value.Value); }
        }

        public int? SocietyRankRadblo
        {
            get => GetProperty(PropertyInt.SocietyRankRadblo);
            set { if (!value.HasValue) RemoveProperty(PropertyInt.SocietyRankRadblo); else SetProperty(PropertyInt.SocietyRankRadblo, value.Value); }
        }

        public FactionBits Society => Faction1Bits ?? FactionBits.None;

        public void UpdateProperty(WorldObject obj, PropertyInt prop, int? value, bool broadcast = false)
        {
            if (value != null)
                obj.SetProperty(prop, value.Value);
            else
                obj.RemoveProperty(prop);

            var msg = new GameMessagePublicUpdatePropertyInt(obj, prop, value ?? 0);
        }

        public void UpdateProperty(WorldObject obj, PropertyBool prop, bool? value, bool broadcast = false)
        {
            if (value != null)
                obj.SetProperty(prop, value.Value);
            else
                obj.RemoveProperty(prop);

            var msg = new GameMessagePublicUpdatePropertyBool(obj, prop, value ?? false);

        }

        public void UpdateProperty(WorldObject obj, PropertyFloat prop, double? value, bool broadcast = false)
        {
            if (value != null)
                obj.SetProperty(prop, value.Value);
            else
                obj.RemoveProperty(prop);

            var msg = new GameMessagePublicUpdatePropertyFloat(obj, prop, value ?? 0.0);

        }
    }
}
