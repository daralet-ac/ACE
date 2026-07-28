namespace ACE.MarketApi;

public class MarketListing
{
    public int Id { get; set; }
    public uint ItemWeenieClassId { get; set; }
    public int ListedPrice { get; set; }
    public int? ItemTier { get; set; }
    public int MarketVendorTier { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public string? ItemName { get; set; }
    public string? ItemDesc { get; set; }
    public string? IconHex { get; set; }
    public string? IconEffect { get; set; }
    public string? IconUnderlay { get; set; }

    // Wield requirements, derived from the item's (up to 4) WieldRequirements/WieldSkillType/
    // WieldDifficulty slots. LevelReq is the first slot found with a Level requirement; ReqAttribute
    // is the first slot found requiring a primary attribute (Strength/Endurance/etc). Any remaining
    // attribute or skill requirements (beyond those two) are listed in OtherWieldRequirements as
    // pre-formatted strings (e.g. "150 War Magic").
    public int? LevelReq { get; set; }
    public string? ReqAttribute { get; set; }
    public int? ReqAttributeAmount { get; set; }
    public List<string> OtherWieldRequirements { get; set; } = new();

    // Category classification, computed server-side from the item snapshot.
    public string? ItemCategory { get; set; }
    public string? WeaponClass { get; set; }
    public string? ArmorWeightClass { get; set; }
    public string? SigilTrinketType { get; set; }
    public string? JewelrySlot { get; set; }
    public List<string> Coverage { get; set; } = new();
    public List<string> ImbuedEffects { get; set; } = new();

    // Named mods/bonuses (Gear* ratings, imbue effects, Cleaving/Crushing/Biting/Primacy) — the
    // client's "Properties"/"Additional Properties" tooltip list, derived from the item's own
    // properties. See MarketRepository.ComputeMods for sourcing.
    public List<string> Properties { get; set; } = new();

    // Spell names on the item (PropertiesSpellBook, resolved against ace_world.spell).
    public List<string> Spells { get; set; } = new();

    public int? Sockets { get; set; }

    // Weapon stats (melee/missile/caster — only the relevant subset is populated).
    public int? BaseDamage { get; set; }
    public double? DamageVariance { get; set; }
    public double? AvgDamage { get; set; }
    // Anim speed multiplier (1.0-2.5, higher = faster) — clamp(2.0 - WeaponTime/100, 1.0, 2.5).
    // Null for casters (no WeaponTime property).
    public double? Speed { get; set; }
    public double? DamageMod { get; set; }
    public double? ElementalDamageMod { get; set; }
    public double? RestorationMod { get; set; }
    public int? ElementalDamageBonus { get; set; }
    public double? WeaponOffense { get; set; }
    public double? WeaponWarMagicMod { get; set; }
    public double? WeaponLifeMagicMod { get; set; }
    public double? WeaponPhysicalDefense { get; set; }
    public double? WeaponMagicalDefense { get; set; }
    public double? Dps { get; set; }
    public int? Spellcraft { get; set; }
    public int? Difficulty { get; set; }
    public int? Mana { get; set; }
    public int? Workmanship { get; set; }
    public int? NumTimesTinkered { get; set; }

    // Armor stats.
    public int? ArmorSlots { get; set; }
    public int? ArmorLevel { get; set; }
    public int? WardLevel { get; set; }
    public double? WardPerSlot { get; set; }
    public double? ArmorModVsSlash { get; set; }
    public double? ArmorModVsPierce { get; set; }
    public double? ArmorModVsBludgeon { get; set; }
    public double? ArmorModVsCold { get; set; }
    public double? ArmorModVsFire { get; set; }
    public double? ArmorModVsAcid { get; set; }
    public double? ArmorModVsElectric { get; set; }
    public double? ArmorModVsNether { get; set; }

    // Weight-class-specific armor mods — which of these an item can actually roll depends on its
    // ArmorWeightClass (see LootGenerationFactory_Clothing.cs's Cloth/Light/Heavy branches):
    // Cloth: WarMagic/LifeMagic/ManaRegen/ManaConversion (+ shared Perception/Deception below).
    // Light: Attack/Shield/DualWield/Thievery/Run/StaminaRegen (+ shared Perception/Deception).
    // Heavy: Attack/PhysicalDef/MagicDef/Shield/TwohandedCombat/HealthRegen (+ shared Perception/Deception).
    public double? ArmorWarMagicMod { get; set; }
    public double? ArmorLifeMagicMod { get; set; }
    public double? ArmorManaRegenMod { get; set; }
    public double? ArmorManaConversionMod { get; set; }
    public double? ArmorAttackMod { get; set; }
    public double? ArmorShieldMod { get; set; }
    public double? ArmorDualWieldMod { get; set; }
    public double? ArmorThieveryMod { get; set; }
    public double? ArmorRunMod { get; set; }
    public double? ArmorStaminaRegenMod { get; set; }
    public double? ArmorPhysicalDefMod { get; set; }
    public double? ArmorMagicDefMod { get; set; }
    public double? ArmorTwohandedCombatMod { get; set; }
    public double? ArmorHealthRegenMod { get; set; }
    public double? ArmorPerceptionMod { get; set; }
    public double? ArmorDeceptionMod { get; set; }

    // Sigil trinket stats.
    public double? SigilTriggerChance { get; set; }
    public double? SigilCooldown { get; set; }
    public double? SigilManaReserved { get; set; }
    public double? SigilStaminaReserved { get; set; }
    public double? SigilHealthReserved { get; set; }
    public int? SigilCurrentLevel { get; set; }
    public int? SigilMaxLevel { get; set; }

    // Salvage bag stats.
    public List<string> SalvageTargetTypes { get; set; } = new();
    public string? SalvageMaterial { get; set; }
    public double? SalvageWorkmanship { get; set; }
    public int? SalvageUnits { get; set; }
}
