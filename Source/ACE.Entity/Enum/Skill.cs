using System.Collections.Generic;
using System.Linq;

namespace ACE.Entity.Enum
{
    /// <summary>
    /// note: even though these are unnumbered, order is very important.  values of "none" or commented
    /// as retired or unused --ABSOLUTELY CANNOT-- be removed. Skills that are none, retired, or not
    /// implemented have been removed from the SkillHelper.ValidSkills hashset below.
    /// </summary>
    public enum Skill
    {
        None,
        Axe,                 /* Retired */
        Bow,                 /* Retired */
        Crossbow,            /* Retired */
        Dagger,              /* Retired */
        Mace,                /* Retired */
        MeleeDefense,
        MissileDefense,
        Sling,               /* Retired */
        Spear,               /* Retired */
        Staff,               /* Retired */
        Sword,               /* Retired */
        ThrownWeapon,        /* Retired */
        UnarmedCombat,       /* Retired */
        ArcaneLore,
        MagicDefense,
        ManaConversion,
        Spellcraft,          /* Unimplemented */
        ItemTinkering,
        AssessPerson,
        Deception,
        Healing,
        Jump,
        Lockpick,
        Run,
        Awareness,           /* Unimplemented */
        ArmsAndArmorRepair,  /* Unimplemented */
        AssessCreature,
        WeaponTinkering,
        ArmorTinkering,
        MagicItemTinkering,
        CreatureEnchantment,
        PortalMagic,
        LifeMagic,
        WarMagic,
        Leadership,
        Loyalty,
        Fletching,
        Alchemy,
        Cooking,
        Salvaging,
        TwoHandedCombat,
        Gearcraft,           /* Retired */
        VoidMagic,
        HeavyWeapons,
        LightWeapons,
        FinesseWeapons,
        MissileWeapons,
        Shield,
        DualWield,
        Recklessness,
        SneakAttack,
        DirtyFighting,
        Challenge,          /* Unimplemented */
        Summoning,
        Sneaking
    }

    public static class SkillExtensions
    {
        public static List<Skill> RetiredMelee = new List<Skill>()
        {
            Skill.Axe,
            Skill.Dagger,
            Skill.Mace,
            Skill.Spear,
            Skill.Staff,
            Skill.Sword,
            Skill.UnarmedCombat
        };

        public static List<Skill> RetiredMissile = new List<Skill>()
        {
            Skill.Bow,
            Skill.Crossbow,
            Skill.Sling,
            Skill.ThrownWeapon
        };

        public static List<Skill> RetiredWeapons = RetiredMelee.Concat(RetiredMissile).ToList();

        /// <summary>
        /// Will add a space infront of capital letter words in a string
        /// </summary>
        /// <param name="skill"></param>
        /// <returns>string with spaces infront of capital letters</returns>
        public static string ToSentence(this Skill skill)
        {
            return new string(skill.ToString().ToCharArray().SelectMany((c, i) => i > 0 && char.IsUpper(c) ? new char[] { ' ', c } : new char[] { c }).ToArray());
        }
    }

    public static class SkillHelper
    {
        static SkillHelper()
        {
            
        }

        public static HashSet<Skill> ValidSkills = new HashSet<Skill>
        {
            Skill.HeavyWeapons,         // Martial Weapons
            Skill.Dagger,
            Skill.Staff,
            Skill.UnarmedCombat,
            Skill.Bow,                  // Bows (and crossbows)
            Skill.ThrownWeapon,
            Skill.TwoHandedCombat,
            Skill.DualWield,

            Skill.LifeMagic,
            Skill.WarMagic,
            Skill.PortalMagic,
            Skill.ManaConversion,
            Skill.ArcaneLore,

            Skill.MeleeDefense,
            Skill.MissileDefense,
            Skill.MagicDefense,
            Skill.Shield,
            Skill.Healing,

            Skill.AssessCreature,       // Perception
            Skill.Deception,
            Skill.Lockpick,             // Thievery
            Skill.Jump,
            Skill.Run,

            Skill.Leadership,
            Skill.Loyalty,

            Skill.Fletching,            // Woodworking
            Skill.Alchemy,
            Skill.Cooking,
            Skill.WeaponTinkering,      // Blacksmithing
            Skill.ArmorTinkering,       // Tailoring
            Skill.MagicItemTinkering,   // Spellcrafting
            Skill.ItemTinkering,        // Jewelcrafting
            
            //Skill.Axe,
            //Skill.Crossbow,
            //Skill.Mace,
            //Skill.Sword,
            //Skill.Sling,
            //Skill.Spear,
            //Skill.SneakAttack,
            //Skill.AssessPerson,
            //Skill.CreatureEnchantment,
            //Skill.Salvaging,
            //Skill.VoidMagic,
            //Skill.LightWeapons,
            //Skill.FinesseWeapons,
            //Skill.MissileWeapons,
            //Skill.Recklessness,
            //Skill.DirtyFighting,
            //Skill.Summoning
        };

        public static HashSet<Skill> AttackSkills = new HashSet<Skill>
        {

            Skill.HeavyWeapons,         // Martial Weapons
            Skill.Dagger,
            Skill.Staff,
            Skill.UnarmedCombat,
            Skill.Bow,                  // Bows (and crossbows)
            Skill.ThrownWeapon,
            Skill.TwoHandedCombat,
            Skill.DualWield,
            Skill.LifeMagic,
            Skill.WarMagic,
        };

        public static HashSet<Skill> DefenseSkills = new HashSet<Skill>()
        {
            Skill.MeleeDefense,
            Skill.MissileDefense,
            Skill.MagicDefense,
            Skill.Shield            // confirmed in client
        };
    }
}
