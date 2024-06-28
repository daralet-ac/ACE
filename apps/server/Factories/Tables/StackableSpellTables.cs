using ACE.Entity.Enum;

namespace ACE.Server.Factories.Tables
{
    public static class StackableSpellTables
    {
        public enum StackableSpellType
        {
            None,
            OlthoiNorth
        }

        public static SpellId[] OlthoiNorthDebuffs =
        {
            SpellId.OlthoiStaminaDebuff,
            SpellId.OlthoiManaDebuff,
            SpellId.OlthoiHealthDebuff,
            SpellId.OlthoiDefenseDebuff,
            SpellId.OlthoiAcidVulnerability
        };

        public static uint[] OlthoiNorthCreatureWcids =
        {
            2036000, // worker
            2036001, // soldier
            2036002, // noble
            2036003, // lancer
            2036004, // grub
            2036005, // eviscerator
            2036050, // worker (niffis)
            2036051, // worker (gromnie)
            2036052, // sea gromnie
            2036053, // worker (wanderer) 
            2036054, // soldier (wanderer)
            2036055, // noble (wanderer)
            2036056, // lancer (wanderer)
        };
    }
}
