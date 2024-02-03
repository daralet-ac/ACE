using System;
using System.Collections.Generic;
using System.Text;
using ACE.Entity.Enum.Properties;

namespace ACE.Server.WorldObjects
{
    public class SkillFormula
    {
        // everything else: melee weapons (including finesse), thrown weapons, atlatls
        public static readonly float DefaultMod = 0.011f;

        // bows and crossbows
        public static readonly float BowMod = 0.011f;

        // magic
        public static readonly float SpellMod = 1000.0f;

        // defenses
        public static readonly float ArmorMod = 100.0f;
        public static readonly float ShieldMod = 2000.0f;
        public static readonly float AegisMod = 100.0f;

        public static float GetAttributeMod(int currentAttribute, ACE.Entity.Enum.Skill skill = ACE.Entity.Enum.Skill.None)
        {
            float factor = skill == ACE.Entity.Enum.Skill.Bow ? BowMod : DefaultMod;

            return Math.Max(1.0f + (currentAttribute - 55) * factor, 1.0f);
        }

        /// <summary>
        /// Converts SpellMod from an additive linear value
        /// to a scaled damage multiplier
        /// </summary>
        public static float CalcSpellMod(int magicSkill)
        {
            return 1 / (SpellMod / (magicSkill + SpellMod));
        }

        /// <summary>
        /// Converts AL from an additive linear value
        /// to a scaled damage multiplier
        /// </summary>
        public static float CalcArmorMod(float armorLevel)
        {
            if (armorLevel > 0)
                return ArmorMod / (armorLevel + ArmorMod);
            else if (armorLevel < 0)
                return 1.0f - armorLevel / ArmorMod;
            else
                return 1.0f;
        }

        /// <summary>
        /// Converts SL from an additive linear value
        /// to a scaled damage multiplier
        /// </summary>
        public static float CalcShieldMod(float shieldLevel)
        {
            if (shieldLevel > 0)
                return ShieldMod / (shieldLevel + ShieldMod);
            else if (shieldLevel < 0)
                return 1.0f - shieldLevel / ShieldMod;
            else
                return 1.0f;
        }

        /// <summary>
        /// Converts Aegis Level from an additive linear value
        /// to a scaled damage multiplier
        /// </summary>
        public static float CalcAegisMod(float aegisLevel)
        {
            if (aegisLevel > 0)
                return AegisMod / (aegisLevel + AegisMod);
            else if (aegisLevel < 0)
                return 1.0f - aegisLevel / AegisMod;
            else
                return 1.0f;
        }
    }
}
