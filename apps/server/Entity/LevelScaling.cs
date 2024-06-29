using ACE.Entity.Enum;
using ACE.Server.Managers;
using ACE.Server.WorldObjects;
using Serilog;
using System;

namespace ACE.Server.Entity
{
    public static class LevelScaling
    {
        /* Level scaling is active when two main conditions are met:
         *  -A player has a Weakening Curse active from using a Stone of Shrouding.
         *  -The player is in combat with an enemy that is lower level than them.
         * 
         * When level scaling is active, the following adjustments are made to the player and enemy:
         *  -During player attacks on the enemy:
         *   -Player damage is scaled down based on monster average max health.
         *   -Player attack skill is scaled down.
         *   -Player main attribute is scaled down for attribute mod calculations.
         *   -Monster armor is scaled up.
         *   -Monster ward is scaled up.
         *  -During enemy attacks on the player
         *   -Monster damage is scaled up based on player average max health.
         *   -Player defense skill is scaled down.
         *   -Player armor is scaled down.
         *   -Player ward is scaled down.
         *   -Player resistance is scaled down.
         *  -During player heals on other players:
         *   -Player heal amount is scaled down based on the target's level.
         *   
         *   NOTE: May need an additional modifier to add more difficulty to higher level players if imbues and other crafting are too good.
         *   Handled previously with this formula: (float gapMod = 1f - (float)(levelGap / 10f) / 12.5f)
         */

        private static readonly int[] AvgPlayerHealthPerTier = { 45, 95, 125, 155, 185, 215, 245, 320 };
        private static readonly int[] AvgPlayerArmorWardPerTier = { 50, 100, 130, 160, 190, 220, 250, 300 };
        private static readonly int[] AvgPlayerAttributePerTier = { 125, 175, 200, 215, 230, 250, 270, 290 };
        private static readonly int[] AvgPlayerAttackSkillPerTier = { 75, 150, 175, 200, 225, 275, 350, 500 };
        private static readonly int[] AvgPlayerDefenseSkillPerTier = { 75, 150, 175, 200, 225, 275, 350, 500 };
        private static readonly float[] AvgPlayerResistancePerTier = { 1.0f, 0.9f, 0.9f, 0.85f, 0.85f, 0.8f, 0.8f, 0.75f, 0.75f };
        private static readonly int[] AvgPlayerBoostPerTier = { 10, 15, 20, 25, 30, 35, 40, 40 };

        private static readonly int[] AvgMonsterArmorWardPerTier = { 20, 45, 68, 101, 152, 228, 342, 513 };
        private static readonly int[] AvgMonsterHealthPerTier = { 50, 150, 350, 500, 800, 1200, 1600, 2000 };

        public static float GetMonsterDamageDealtHealthScalar(Creature player, Creature monster)
        {
            if (!CanScalePlayer(player, monster))
                return 1.0f;

            var statAtPlayerLevel = GetPlayerHealthAtLevel(player.Level.Value);
            var statAtMonsterLevel = GetPlayerHealthAtLevel(monster.Level.Value);

            if (PropertyManager.GetBool("debug_level_scaling_system").Item)
                Console.WriteLine($"\nGetMonsterDamageDealtHealthScalar(Player {player.Name}, Monster {monster.Name})" +
                    $"\n  statAtPlayerLevel: {statAtPlayerLevel}, statAtMonsterLevel: {statAtMonsterLevel}, scalarMod: {(float)statAtPlayerLevel / statAtMonsterLevel}");

            return (float)statAtPlayerLevel / statAtMonsterLevel;
        }

        public static float GetMonsterDamageTakenHealthScalar(Creature player, Creature monster)
        {
            if (!CanScalePlayer(player, monster))
                return 1.0f;

            var statAtPlayerLevel = GetMonsterHealthAtLevel(player.Level.Value);
            var statAtMonsterLevel = GetMonsterHealthAtLevel(monster.Level.Value);

            if (PropertyManager.GetBool("debug_level_scaling_system").Item)
                Console.WriteLine($"\nGetMonsterDamageTakenHealthScalar(Player {player.Name}, Monster {monster.Name})" +
                    $"\n  statAtPlayerLevel: {statAtPlayerLevel}, statAtMonsterLevel: {statAtMonsterLevel}, scalarMod: {((float)statAtMonsterLevel / statAtPlayerLevel + 1) / 2}");

            return ((float)statAtMonsterLevel / statAtPlayerLevel + 1) / 2;
        }

        public static float GetMonsterArmorWardScalar(Creature player, Creature monster)
        {
            if (!CanScalePlayer(player, monster))
                return 1.0f;

            var statAtPlayerLevel = GetMonsterArmorWardAtLevel(player.Level.Value);
            var statAtMonsterLevel = GetMonsterArmorWardAtLevel(monster.Level.Value);

            if (PropertyManager.GetBool("debug_level_scaling_system").Item)
                Console.WriteLine($"\nGetMonsterArmorWardScalar(Player {player.Name}, Monster {monster.Name})" +
                    $"\n  statAtPlayerLevel: {statAtPlayerLevel}, statAtMonsterLevel: {statAtMonsterLevel}, scalarMod: {(float)statAtPlayerLevel / statAtMonsterLevel}");

            return (float)statAtPlayerLevel / statAtMonsterLevel;
        }

        public static float GetPlayerArmorWardScalar(Creature player, Creature monster)
        {
            if (!CanScalePlayer(player, monster))
                return 1.0f;

            var statAtPlayerLevel = GetPlayerArmorWardAtLevel(player.Level.Value);
            var statAtMonsterLevel = GetPlayerArmorWardAtLevel(monster.Level.Value);

            if (PropertyManager.GetBool("debug_level_scaling_system").Item)
                Console.WriteLine($"\nGetPlayerArmorWardScalar(Player {player.Name}, Monster {monster.Name})" +
                    $"\n  statAtPlayerLevel: {statAtPlayerLevel}, statAtMonsterLevel: {statAtMonsterLevel}, scalarMod: {(float)statAtMonsterLevel / statAtPlayerLevel}");

            return (float)statAtMonsterLevel / statAtPlayerLevel;
        }

        public static float GetPlayerAttributeScalar(Creature player, Creature monster)
        {
            if (!CanScalePlayer(player, monster))
                return 1.0f;

            var statAtPlayerLevel = GetPlayerAttributeAtLevel(player.Level.Value);
            var statAtMonsterLevel = GetPlayerAttributeAtLevel(monster.Level.Value);

            if (PropertyManager.GetBool("debug_level_scaling_system").Item)
                Console.WriteLine($"\nGetPlayerAttributeScalar(Player {player.Name}, Monster {monster.Name})" +
                    $"\n  statAtPlayerLevel: {statAtPlayerLevel}, statAtMonsterLevel: {statAtMonsterLevel}, scalarMod: {(float)statAtMonsterLevel / statAtPlayerLevel}");

            return (float)statAtMonsterLevel / statAtPlayerLevel;
        }

        public static float GetPlayerAttackSkillScalar(Creature player, Creature monster)
        {
            if (!CanScalePlayer(player, monster))
                return 1.0f;

            var statAtPlayerLevel = GetPlayerAttackSkillAtLevel(player.Level.Value);
            var statAtMonsterLevel = GetPlayerAttackSkillAtLevel(monster.Level.Value);

            if (PropertyManager.GetBool("debug_level_scaling_system").Item)
                Console.WriteLine($"\nGetPlayerAttackSkillScalar(Player {player.Name}, Monster {monster.Name})" +
                    $"\n  statAtPlayerLevel: {statAtPlayerLevel}, statAtMonsterLevel: {statAtMonsterLevel}, scalarMod: {(float)statAtMonsterLevel / statAtPlayerLevel}");

            return (float)statAtMonsterLevel / statAtPlayerLevel;
        }

        public static float GetPlayerDefenseSkillScalar(Creature player, Creature monster)
        {
            if (!CanScalePlayer(player, monster))
                return 1.0f;

            var statAtPlayerLevel = GetPlayerDefenseSkillAtLevel(player.Level.Value);
            var statAtMonsterLevel = GetPlayerDefenseSkillAtLevel(monster.Level.Value);

            if (PropertyManager.GetBool("debug_level_scaling_system").Item)
                Console.WriteLine($"\nGetPlayerDefenseSkillScalar(Player {player.Name}, Monster {monster.Name})" +
                    $"\n  statAtPlayerLevel: {statAtPlayerLevel}, statAtMonsterLevel: {statAtMonsterLevel}, scalarMod: {(float)statAtMonsterLevel / statAtPlayerLevel}");

            return (float)statAtMonsterLevel / statAtPlayerLevel;
        }

        public static float GetPlayerResistanceScalar(Creature player, Creature monster)
        {
            if (!CanScalePlayer(player, monster))
                return 1.0f;

            var statAtPlayerLevel = GetPlayerResistanceAtLevel(player.Level.Value);
            var statAtMonsterLevel = GetPlayerResistanceAtLevel(monster.Level.Value);

            if (PropertyManager.GetBool("debug_level_scaling_system").Item)
                Console.WriteLine($"\nGetPlayerResistanceScalar(Player {player.Name}, Monster {monster.Name})" +
                    $"\n  statAtPlayerLevel: {statAtPlayerLevel}, statAtMonsterLevel: {statAtMonsterLevel}, scalarMod: {(float)statAtMonsterLevel / statAtPlayerLevel}");

            return (float)statAtMonsterLevel / statAtPlayerLevel;
        }

        public static float GetPlayerBoostSpellScalar(Creature player, Creature monster)
        {
            if (!CanScalePlayer(player, monster))
                return 1.0f;

            var statAtPlayerLevel = GetPlayerBoostAtLevel(player.Level.Value);
            var statAtMonsterLevel = GetPlayerBoostAtLevel(monster.Level.Value);

            if (PropertyManager.GetBool("debug_level_scaling_system").Item)
                Console.WriteLine($"\nGetPlayerBoostSpellScalar(Player {player.Name}, Monster {monster.Name})" +
                    $"\n  statAtPlayerLevel: {statAtPlayerLevel}, statAtMonsterLevel: {statAtMonsterLevel}, scalarMod: {(float)statAtPlayerLevel / statAtMonsterLevel}");

            return (float)statAtMonsterLevel / statAtPlayerLevel;
        }

        // --- Private Get At-Level Helpers ---
        private static int GetPlayerHealthAtLevel(int level)
        {
            GetRangeAndStatWeight(level, out var range, out var statweight);

            var stat = (AvgPlayerHealthPerTier[range + 1] - AvgPlayerHealthPerTier[range]) * statweight + AvgPlayerHealthPerTier[range];

            return (int)stat;
        }

        private static int GetPlayerArmorWardAtLevel(int level)
        {
            GetRangeAndStatWeight(level, out var range, out var statweight);

            var stat = (AvgPlayerArmorWardPerTier[range + 1] - AvgPlayerArmorWardPerTier[range]) * statweight + AvgPlayerArmorWardPerTier[range];

            return (int)stat;
        }

        private static int GetPlayerAttributeAtLevel(int level)
        {
            GetRangeAndStatWeight(level, out var range, out var statweight);

            var stat = (AvgPlayerAttributePerTier[range + 1] - AvgPlayerAttributePerTier[range]) * statweight + AvgPlayerAttributePerTier[range];

            return (int)stat;
        }

        private static int GetPlayerAttackSkillAtLevel(int level)
        {
            GetRangeAndStatWeight(level, out var range, out var statweight);

            var stat = (AvgPlayerAttackSkillPerTier[range + 1] - AvgPlayerAttackSkillPerTier[range]) * statweight + AvgPlayerAttackSkillPerTier[range];

            return (int)stat;
        }

        private static int GetPlayerDefenseSkillAtLevel(int level)
        {
            GetRangeAndStatWeight(level, out var range, out var statweight);

            var stat = (AvgPlayerDefenseSkillPerTier[range + 1] - AvgPlayerDefenseSkillPerTier[range]) * statweight + AvgPlayerDefenseSkillPerTier[range];

            return (int)stat;
        }

        private static float GetPlayerResistanceAtLevel(int level)
        {
            GetRangeAndStatWeight(level, out var range, out var statweight);

            var stat = (AvgPlayerResistancePerTier[range + 1] - AvgPlayerResistancePerTier[range]) * statweight + AvgPlayerResistancePerTier[range];

            return stat;
        }

        private static int GetPlayerBoostAtLevel(int level)
        {
            GetRangeAndStatWeight(level, out var range, out var statweight);

            var stat = (AvgPlayerBoostPerTier[range + 1] - AvgPlayerBoostPerTier[range]) * statweight + AvgPlayerBoostPerTier[range];

            return (int)stat;
        }

        private static int GetMonsterHealthAtLevel(int level)
        {
            GetRangeAndStatWeight(level, out var range, out var statweight);

            var stat = (AvgMonsterHealthPerTier[range + 1] - AvgMonsterHealthPerTier[range]) * statweight + AvgMonsterHealthPerTier[range];

            return (int)stat;
        }

        private static int GetMonsterArmorWardAtLevel(int level)
        {
            GetRangeAndStatWeight(level, out var range, out var statweight);

            var stat = (AvgMonsterArmorWardPerTier[range + 1] - AvgMonsterArmorWardPerTier[range]) * statweight + AvgMonsterArmorWardPerTier[range];

            return (int)stat;
        }

        private static void GetRangeAndStatWeight(int level, out int range, out float statweight)
        {
            switch (level)
            {
                case < 20: range = 0; statweight = (level - 10.0f) / 10; break;
                case < 30: range = 1; statweight = (level - 20.0f) / 10; break;
                case < 40: range = 2; statweight = (level - 30.0f) / 10; break;
                case < 50: range = 3; statweight = (level - 40.0f) / 10; break;
                case < 75: range = 4; statweight = (level - 50.0f) / 25; break;
                case < 100: range = 5; statweight = (level - 75.0f) / 25; break;
                default: range = 6; statweight = (level - 100.0f) / 26; break;
            }

            statweight = Math.Min(statweight, 1.0f);
        }

        private static bool CanScalePlayer(Creature player, Creature monster)
        {
            if (player == null || monster == null)
                return false;

            if (!player.EnchantmentManager.HasSpell((uint)SpellId.CurseWeakness1))
                return false;

            if (player.Level.HasValue && monster.Level.HasValue && player.Level <= monster.Level)
                return false;

            return true;
        }
    }
}
