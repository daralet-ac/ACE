using System.Collections.Generic;
using ACE.Entity.Enum;
using ACE.Server.Factories.Entity;
using ACE.Server.WorldObjects;

namespace ACE.Server.Factories.Tables;

public static class CasterSlotSpells
{
    private static ChanceTable<SpellId> orbSpells = new ChanceTable<SpellId>()
    {
        (SpellId.StrengthOther1, 0.05f),
        (SpellId.EnduranceOther1, 0.05f),
        (SpellId.CoordinationOther1, 0.05f),
        (SpellId.QuicknessOther1, 0.05f),
        (SpellId.FocusOther1, 0.05f),
        (SpellId.WillpowerOther1, 0.05f),
        (SpellId.FealtyOther1, 0.10f),
        (SpellId.HealOther1, 0.10f),
        (SpellId.RevitalizeOther1, 0.10f),
        (SpellId.ManaBoostOther1, 0.10f),
        (SpellId.RegenerationOther1, 0.10f),
        (SpellId.RejuvenationOther1, 0.10f),
        (SpellId.ManaRenewalOther1, 0.10f),
    };

    private static ChanceTable<SpellId> wandStaffSpells = new ChanceTable<SpellId>()
    {
        (SpellId.WhirlingBlade1, 0.14f),
        (SpellId.ForceBolt1, 0.14f),
        (SpellId.ShockWave1, 0.14f),
        (SpellId.AcidStream1, 0.14f),
        (SpellId.FlameBolt1, 0.15f),
        (SpellId.FrostBolt1, 0.14f),
        (SpellId.LightningBolt1, 0.15f),
    };

    private static ChanceTable<SpellId> netherSpells = new ChanceTable<SpellId>()
    {
        (SpellId.Corruption1, 0.20f),
        (SpellId.NetherArc1, 0.20f),
        (SpellId.NetherBolt1, 0.20f),
        (SpellId.Corrosion1, 0.15f),
        (SpellId.CurseWeakness1, 0.10f),
        (SpellId.CurseFestering1, 0.10f),
        (SpellId.CurseDestructionOther1, 0.05f),
    };

    // TIMELINE Tables - DID spells always matches the element of the caster
    private static ChanceTable<SpellId> slashingSpells = new ChanceTable<SpellId>()
    {
        (SpellId.WhirlingBlade1, 1.0f),
        //( SpellId.BladeVolley1,              0.20f ),
        //( SpellId.BladeBlast1,               0.20f ),
    };

    private static ChanceTable<SpellId> piercingSpells = new ChanceTable<SpellId>()
    {
        (SpellId.ForceBolt1, 1.0f),
        //( SpellId.ForceVolley1,          0.20f ),
        //( SpellId.ForceBlast1,           0.20f ),
    };

    private static ChanceTable<SpellId> bludgeoningSpells = new ChanceTable<SpellId>()
    {
        (SpellId.ShockWave1, 1.0f),
        //( SpellId.ShockBlast1,           0.20f ),
        //( SpellId.BludgeoningVolley1,    0.20f ),
    };

    private static ChanceTable<SpellId> acidSpells = new ChanceTable<SpellId>()
    {
        (SpellId.AcidStream1, 1.0f),
        //( SpellId.AcidBlast2,              0.20f ),
        //( SpellId.AcidVolley1,             0.20f ),
    };

    private static ChanceTable<SpellId> fireSpells = new ChanceTable<SpellId>()
    {
        (SpellId.FlameBolt1, 1.0f),
        //( SpellId.FlameBlast2,           0.20f ),
        //( SpellId.FlameVolley1,          0.20f ),
    };

    private static ChanceTable<SpellId> coldSpells = new ChanceTable<SpellId>()
    {
        (SpellId.FrostBolt1, 1.0f),
        //( SpellId.FrostBlast1,           0.20f ),
        //( SpellId.FrostVolley1,          0.20f ),
    };

    private static ChanceTable<SpellId> electricSpells = new ChanceTable<SpellId>()
    {
        (SpellId.LightningBolt1, 1.0f),
        //( SpellId.LightningBlast1,           0.20f ),
        //( SpellId.LightningVolley1,          0.20f ),
    };

    private static ChanceTable<SpellId> lifeSpells = new ChanceTable<SpellId>()
    {
        (SpellId.HealOther1, 0.34f),
        (SpellId.RevitalizeOther1, 0.33f),
        (SpellId.ManaBoostOther1, 0.33f),
    };

    public static SpellId Roll(WorldObject wo)
    {
        ChanceTable<SpellId> table;

        switch (wo.W_DamageType)
        {
            case DamageType.Undef:
                table = lifeSpells;
                break;
            case DamageType.Slash:
                table = slashingSpells;
                break;
            case DamageType.Pierce:
                table = piercingSpells;
                break;
            case DamageType.Bludgeon:
                table = bludgeoningSpells;
                break;
            case DamageType.Acid:
                table = acidSpells;
                break;
            case DamageType.Fire:
                table = fireSpells;
                break;
            case DamageType.Cold:
                table = coldSpells;
                break;
            case DamageType.Electric:
                table = electricSpells;
                break;
            default:
                table = lifeSpells;
                break;
        }

        return table.Roll();
    }

    public static bool IsOrb(WorldObject wo)
    {
        // todo: any other wcids for obs?
        return wo.WeenieClassId == (int)WeenieClassName.W_ORB_CLASS;
    }

    public static readonly Dictionary<SpellId, string> descriptors = new Dictionary<SpellId, string>
    {
        { SpellId.StrengthOther1, "Strength" },
        { SpellId.StrengthSelf1, "Strength" },
        { SpellId.HealOther1, "Curing" },
        { SpellId.HealSelf1, "Healing" },
        { SpellId.InfuseMana1, "Infuse Mana" },
        { SpellId.InvulnerabilityOther1, "Invulnerability" },
        { SpellId.InvulnerabilitySelf1, "Invulnerability" },
        { SpellId.FireProtectionOther1, "Fire Protection" },
        { SpellId.FireProtectionSelf1, "Fire Protection" },
        { SpellId.ArmorOther1, "Protection" },
        { SpellId.ArmorSelf1, "Armor" },
        { SpellId.FlameBolt1, "Flame Bolt" },
        { SpellId.FrostBolt1, "Frost Bolt" },
        { SpellId.RejuvenationOther1, "Rejuvenation" },
        { SpellId.RejuvenationSelf1, "Rejuvenation" },
        { SpellId.AcidStream1, "Acid Stream" },
        { SpellId.ShockWave1, "Shock Wave" },
        { SpellId.LightningBolt1, "Lightning Bolt" },
        { SpellId.ForceBolt1, "Force Bolt" },
        { SpellId.WhirlingBlade1, "Blades" },
        { SpellId.RegenerationOther1, "Regeneration" },
        { SpellId.RegenerationSelf1, "Regeneration" },
        { SpellId.ManaRenewalOther1, "Mana Renewal" },
        { SpellId.ManaRenewalSelf1, "Mana Renewal" },
        { SpellId.ImpregnabilityOther1, "Impregnability" },
        { SpellId.ImpregnabilitySelf1, "Impregnability" },
        { SpellId.MagicResistanceOther1, "Magic Resistance" },
        { SpellId.MagicResistanceSelf1, "Magic Resistance" },
        { SpellId.LightWeaponsMasteryOther1, "Light Mastery" },
        { SpellId.LightWeaponsMasterySelf1, "Light Mastery" },
        { SpellId.FinesseWeaponsMasteryOther1, "Finesse Mastery" },
        { SpellId.FinesseWeaponsMasterySelf1, "Finesse Mastery" },
        { SpellId.MaceMasteryOther1, "Light Mastery" },
        { SpellId.MaceMasterySelf1, "Light Mastery" },
        { SpellId.SpearMasteryOther1, "Light Mastery" },
        { SpellId.SpearMasterySelf1, "Light Mastery" },
        { SpellId.StaffMasteryOther1, "Light Mastery" },
        { SpellId.StaffMasterySelf1, "Light Mastery" },
        { SpellId.HeavyWeaponsMasteryOther1, "Heavy Mastery" },
        { SpellId.HeavyWeaponsMasterySelf1, "Heavy Mastery" },
        { SpellId.UnarmedCombatMasteryOther1, "Light Mastery" },
        { SpellId.UnarmedCombatMasterySelf1, "Light Mastery" },
        { SpellId.MissileWeaponsMasteryOther1, "Missile Mastery" },
        { SpellId.MissileWeaponsMasterySelf1, "Missile Mastery" },
        { SpellId.CrossbowMasteryOther1, "Missile Mastery" },
        { SpellId.CrossbowMasterySelf1, "Missile Mastery" },
        { SpellId.AcidProtectionOther1, "Acid Protection" },
        { SpellId.AcidProtectionSelf1, "Acid Protection" },
        { SpellId.ThrownWeaponMasteryOther1, "Missile Mastery" },
        { SpellId.ThrownWeaponMasterySelf1, "Missile Mastery" },
        { SpellId.CreatureEnchantmentMasterySelf1, "Creature Enchantment" },
        { SpellId.CreatureEnchantmentMasteryOther1, "Creature Enchantment" },
        { SpellId.ItemEnchantmentMasterySelf1, "Item Enchantment" },
        { SpellId.ItemEnchantmentMasteryOther1, "Item Enchantment" },
        { SpellId.LifeMagicMasterySelf1, "Life Magic" },
        { SpellId.LifeMagicMasteryOther1, "Life Magic" },
        { SpellId.WarMagicMasterySelf1, "War Magic" },
        { SpellId.WarMagicMasteryOther1, "War Magic" },
        { SpellId.ManaMasterySelf1, "Mana Conversion" },
        { SpellId.ManaMasteryOther1, "Mana Conversion" },
        { SpellId.ArcaneEnlightenmentSelf1, "Arcane" },
        { SpellId.ArcaneEnlightenmentOther1, "Arcane" },
        { SpellId.ArmorExpertiseSelf1, "Armor Tinkering" },
        { SpellId.ArmorExpertiseOther1, "Armor Tinkering" },
        { SpellId.ItemExpertiseSelf1, "Item Tinkering" },
        { SpellId.ItemExpertiseOther1, "Item Tinkering" },
        { SpellId.MagicItemExpertiseSelf1, "Magic Item Tinkering" },
        { SpellId.MagicItemExpertiseOther1, "Magic Item Tinkering" },
        { SpellId.WeaponExpertiseSelf1, "Weapon Tinkering" },
        { SpellId.WeaponExpertiseOther1, "Weapon Tinkering" },
        { SpellId.MonsterAttunementSelf1, "Attunement" },
        { SpellId.MonsterAttunementOther1, "Attunement" },
        { SpellId.PersonAttunementSelf1, "Attunement" },
        { SpellId.PersonAttunementOther1, "Attunement" },
        { SpellId.DeceptionMasterySelf1, "Deception" },
        { SpellId.DeceptionMasteryOther1, "Deception" },
        { SpellId.HealingMasterySelf1, "Healing" },
        { SpellId.HealingMasteryOther1, "Healing" },
        { SpellId.LeadershipMasterySelf1, "Leadership" },
        { SpellId.LeadershipMasteryOther1, "Leadership" },
        { SpellId.LockpickMasterySelf1, "Lockpicking" },
        { SpellId.LockpickMasteryOther1, "Lockpicking" },
        { SpellId.FealtySelf1, "Fealty" },
        { SpellId.FealtyOther1, "Fealty" },
        { SpellId.JumpingMasterySelf1, "Jumping" },
        { SpellId.JumpingMasteryOther1, "Jumping" },
        { SpellId.SprintSelf1, "Sprinting" },
        { SpellId.SprintOther1, "Sprinting" },
        { SpellId.BludgeonProtectionSelf1, "Bludgeoning Protection" },
        { SpellId.BludgeonProtectionOther1, "Bludgeoning Protection" },
        { SpellId.ColdProtectionSelf1, "Cold Protection" },
        { SpellId.ColdProtectionOther1, "Cold Protection" },
        { SpellId.LightningProtectionSelf1, "Lightning Protection" },
        { SpellId.LightningProtectionOther1, "Lightning Protection" },
        { SpellId.BladeProtectionSelf1, "Blade Protection" },
        { SpellId.BladeProtectionOther1, "Blade Protection" },
        { SpellId.PiercingProtectionSelf1, "Piercing Protection" },
        { SpellId.PiercingProtectionOther1, "Piercing Protection" },
        { SpellId.RevitalizeSelf1, "Vitality" },
        { SpellId.RevitalizeOther1, "Vitality" },
        { SpellId.ManaBoostSelf1, "Mana" },
        { SpellId.ManaBoostOther1, "Mana" },
        { SpellId.InfuseHealth1, "Infuse Health" },
        { SpellId.InfuseStamina1, "Infuse Stamina" },
        { SpellId.HealthToStaminaOther1, "Health to Stamina" },
        { SpellId.HealthToStaminaSelf1, "Health to Stamina" },
        { SpellId.HealthToManaSelf1, "Health to Mana" },
        { SpellId.ManaToHealthOther1, "Mana to Health" },
        { SpellId.ManaToHealthSelf1, "Mana to Health" },
        { SpellId.ManaToStaminaSelf1, "Mana to Stamina" },
        { SpellId.ManaToStaminaOther1, "Mana to Stamina" },
        { SpellId.EnduranceSelf1, "Endurance" },
        { SpellId.EnduranceOther1, "Endurance" },
        { SpellId.CoordinationSelf1, "Coordination" },
        { SpellId.CoordinationOther1, "Coordination" },
        { SpellId.QuicknessSelf1, "Quickness" },
        { SpellId.QuicknessOther1, "Quickness" },
        { SpellId.FocusSelf1, "Focus" },
        { SpellId.FocusOther1, "Focus" },
        { SpellId.WillpowerSelf1, "Willpower" },
        { SpellId.WillpowerOther1, "Willpower" },
        { SpellId.StaminaToHealthOther1, "Stamina to Health" },
        { SpellId.StaminaToHealthSelf1, "Stamina to Health" },
        { SpellId.StaminaToManaOther1, "Stamina to Mana" },
        { SpellId.StaminaToManaSelf1, "Stamina to Mana" },
        { SpellId.HealthToManaOther1, "Health to Mana" },
        { SpellId.CookingMasteryOther1, "Cooking" },
        { SpellId.CookingMasterySelf1, "Cooking" },
        { SpellId.FletchingMasteryOther1, "Fletching" },
        { SpellId.FletchingMasterySelf1, "Fletching" },
        { SpellId.AlchemyMasteryOther1, "Alchemy" },
        { SpellId.AlchemyMasterySelf1, "Alchemy" },
        { SpellId.ShockwaveStreak1, "Shock Wave" },
        { SpellId.WhirlingBladeStreak1, "Blades" },
        { SpellId.ArcanumSalvagingSelf1, "Arcanum Salvaging" },
        { SpellId.ArcanumSalvagingOther1, "Arcanum Salvaging" },
        { SpellId.GearcraftMastery1, "Item Tinkering" },
        { SpellId.GearcraftMasterySelf1, "Item Tinkering" },
        { SpellId.TwoHandedMasteryOther1, "Two Handed Combat" },
        { SpellId.TwoHandedMasterySelf1, "Two Handed Combat" },
        { SpellId.Corrosion1, "Corrosion" },
        { SpellId.Corruption1, "Corruption" },
        { SpellId.VoidMagicMasteryOther1, "Void Magic" },
        { SpellId.VoidMagicMasterySelf1, "Void Magic" },
        { SpellId.DirtyFightingMasteryOther1, "Dirty Fighting" },
        { SpellId.DirtyFightingMasterySelf1, "Dirty Fighting" },
        { SpellId.DualWieldMasteryOther1, "Dual Wield" },
        { SpellId.DualWieldMasterySelf1, "Dual Wield" },
        { SpellId.RecklessnessMasteryOther1, "Recklessness" },
        { SpellId.RecklessnessMasterySelf1, "Recklessness" },
        { SpellId.ShieldMasteryOther1, "Shield" },
        { SpellId.ShieldMasterySelf1, "Shield" },
        { SpellId.SneakAttackMasteryOther1, "Sneak Attack" },
        { SpellId.SneakAttackMasterySelf1, "Sneak Attack" },
        { SpellId.SummoningMasteryOther1, "Summoning" },
        { SpellId.SummoningMasterySelf1, "Summoning" },
        // added from magloot logs
        { SpellId.BloodDrinkerSelf1, "Blood Drinker" },
        { SpellId.SwiftKillerSelf1, "Swift Killer" },
        { SpellId.DefenderSelf1, "Defender" },
        { SpellId.NetherBolt1, "Nether Bolt" },
        { SpellId.NetherArc1, "Nether Arc" },
        { SpellId.CurseFestering1, "Curse Festering" },
        { SpellId.CurseWeakness1, "Curse Weakness" },
        { SpellId.CurseDestructionOther1, "Curse Destruction" },
    };

    static CasterSlotSpells()
    {
        descriptors = new Dictionary<SpellId, string>
        {
            { SpellId.StrengthOther1, "Strength" },
            { SpellId.StrengthSelf1, "Strength" },
            { SpellId.HealOther1, "Curing" },
            { SpellId.HealSelf1, "Healing" },
            { SpellId.InfuseMana1, "Infuse Mana" },
            { SpellId.InvulnerabilityOther1, "Invulnerability" },
            { SpellId.InvulnerabilitySelf1, "Invulnerability" },
            { SpellId.FireProtectionOther1, "Fire Protection" },
            { SpellId.FireProtectionSelf1, "Fire Protection" },
            { SpellId.ArmorOther1, "Protection" },
            { SpellId.ArmorSelf1, "Armor" },
            { SpellId.FlameBolt1, "Flame Bolt" },
            { SpellId.FrostBolt1, "Frost Bolt" },
            { SpellId.RejuvenationOther1, "Rejuvenation" },
            { SpellId.RejuvenationSelf1, "Rejuvenation" },
            { SpellId.AcidStream1, "Acid Stream" },
            { SpellId.ShockWave1, "Shock Wave" },
            { SpellId.LightningBolt1, "Lightning Bolt" },
            { SpellId.ForceBolt1, "Force Bolt" },
            { SpellId.WhirlingBlade1, "Blades" },
            { SpellId.RegenerationOther1, "Regeneration" },
            { SpellId.RegenerationSelf1, "Regeneration" },
            { SpellId.ManaRenewalOther1, "Mana Renewal" },
            { SpellId.ManaRenewalSelf1, "Mana Renewal" },
            { SpellId.ImpregnabilityOther1, "Impregnability" },
            { SpellId.ImpregnabilitySelf1, "Impregnability" },
            { SpellId.MagicResistanceOther1, "Magic Resistance" },
            { SpellId.MagicResistanceSelf1, "Magic Resistance" },
            { SpellId.LightWeaponsMasteryOther1, "Axe and Mace Mastery" },
            { SpellId.LightWeaponsMasterySelf1, "Axe and Mace Mastery" },
            { SpellId.FinesseWeaponsMasteryOther1, "Dagger Mastery" },
            { SpellId.FinesseWeaponsMasterySelf1, "Dagger Mastery" },
            { SpellId.MaceMasteryOther1, "Axe and Mace Mastery" },
            { SpellId.MaceMasterySelf1, "Axe and Mace Mastery" },
            { SpellId.SpearMasteryOther1, "Spear and Staff Mastery" },
            { SpellId.SpearMasterySelf1, "Spear and Staff Mastery" },
            { SpellId.StaffMasteryOther1, "Spear and Staff Mastery" },
            { SpellId.StaffMasterySelf1, "Spear and Staff Mastery" },
            { SpellId.HeavyWeaponsMasteryOther1, "Sword Mastery" },
            { SpellId.HeavyWeaponsMasterySelf1, "Sword Mastery" },
            { SpellId.UnarmedCombatMasteryOther1, "Unarmed Combat Mastery" },
            { SpellId.UnarmedCombatMasterySelf1, "Unarmed Combat Mastery" },
            { SpellId.MissileWeaponsMasteryOther1, "Bow and Crossbow Mastery" },
            { SpellId.MissileWeaponsMasterySelf1, "Bow and Crossbow Mastery" },
            { SpellId.CrossbowMasteryOther1, "Bow and Crossbow Mastery" },
            { SpellId.CrossbowMasterySelf1, "Bow and Crossbow Mastery" },
            { SpellId.AcidProtectionOther1, "Acid Protection" },
            { SpellId.AcidProtectionSelf1, "Acid Protection" },
            { SpellId.ThrownWeaponMasteryOther1, "Thrown Weapon Mastery" },
            { SpellId.ThrownWeaponMasterySelf1, "Thrown Weapon Mastery" },
            { SpellId.CreatureEnchantmentMasterySelf1, "Creature Enchantment" },
            { SpellId.CreatureEnchantmentMasteryOther1, "Creature Enchantment" },
            { SpellId.ItemEnchantmentMasterySelf1, "Item Enchantment" },
            { SpellId.ItemEnchantmentMasteryOther1, "Item Enchantment" },
            { SpellId.LifeMagicMasterySelf1, "Life Magic" },
            { SpellId.LifeMagicMasteryOther1, "Life Magic" },
            { SpellId.WarMagicMasterySelf1, "War Magic" },
            { SpellId.WarMagicMasteryOther1, "War Magic" },
            { SpellId.ManaMasterySelf1, "Mana Conversion" },
            { SpellId.ManaMasteryOther1, "Mana Conversion" },
            { SpellId.ArcaneEnlightenmentSelf1, "Arcane" },
            { SpellId.ArcaneEnlightenmentOther1, "Arcane" },
            { SpellId.ArmorExpertiseSelf1, "Armor Tinkering" },
            { SpellId.ArmorExpertiseOther1, "Armor Tinkering" },
            { SpellId.ItemExpertiseSelf1, "Item Tinkering" },
            { SpellId.ItemExpertiseOther1, "Item Tinkering" },
            { SpellId.MagicItemExpertiseSelf1, "Magic Item Tinkering" },
            { SpellId.MagicItemExpertiseOther1, "Magic Item Tinkering" },
            { SpellId.WeaponExpertiseSelf1, "Weapon Tinkering" },
            { SpellId.WeaponExpertiseOther1, "Weapon Tinkering" },
            { SpellId.MonsterAttunementSelf1, "Attunement" },
            { SpellId.MonsterAttunementOther1, "Attunement" },
            { SpellId.PersonAttunementSelf1, "Attunement" },
            { SpellId.PersonAttunementOther1, "Attunement" },
            { SpellId.DeceptionMasterySelf1, "Deception" },
            { SpellId.DeceptionMasteryOther1, "Deception" },
            { SpellId.HealingMasterySelf1, "Healing" },
            { SpellId.HealingMasteryOther1, "Healing" },
            { SpellId.LeadershipMasterySelf1, "Leadership" },
            { SpellId.LeadershipMasteryOther1, "Leadership" },
            { SpellId.LockpickMasterySelf1, "Lockpicking" },
            { SpellId.LockpickMasteryOther1, "Lockpicking" },
            { SpellId.FealtySelf1, "Fealty" },
            { SpellId.FealtyOther1, "Fealty" },
            { SpellId.JumpingMasterySelf1, "Jumping" },
            { SpellId.JumpingMasteryOther1, "Jumping" },
            { SpellId.SprintSelf1, "Sprinting" },
            { SpellId.SprintOther1, "Sprinting" },
            { SpellId.BludgeonProtectionSelf1, "Bludgeoning Protection" },
            { SpellId.BludgeonProtectionOther1, "Bludgeoning Protection" },
            { SpellId.ColdProtectionSelf1, "Cold Protection" },
            { SpellId.ColdProtectionOther1, "Cold Protection" },
            { SpellId.LightningProtectionSelf1, "Lightning Protection" },
            { SpellId.LightningProtectionOther1, "Lightning Protection" },
            { SpellId.BladeProtectionSelf1, "Blade Protection" },
            { SpellId.BladeProtectionOther1, "Blade Protection" },
            { SpellId.PiercingProtectionSelf1, "Piercing Protection" },
            { SpellId.PiercingProtectionOther1, "Piercing Protection" },
            { SpellId.RevitalizeSelf1, "Vitality" },
            { SpellId.RevitalizeOther1, "Vitality" },
            { SpellId.ManaBoostSelf1, "Mana" },
            { SpellId.ManaBoostOther1, "Mana" },
            { SpellId.InfuseHealth1, "Infuse Health" },
            { SpellId.InfuseStamina1, "Infuse Stamina" },
            { SpellId.HealthToStaminaOther1, "Health to Stamina" },
            { SpellId.HealthToStaminaSelf1, "Health to Stamina" },
            { SpellId.HealthToManaSelf1, "Health to Mana" },
            { SpellId.ManaToHealthOther1, "Mana to Health" },
            { SpellId.ManaToHealthSelf1, "Mana to Health" },
            { SpellId.ManaToStaminaSelf1, "Mana to Stamina" },
            { SpellId.ManaToStaminaOther1, "Mana to Stamina" },
            { SpellId.EnduranceSelf1, "Endurance" },
            { SpellId.EnduranceOther1, "Endurance" },
            { SpellId.CoordinationSelf1, "Coordination" },
            { SpellId.CoordinationOther1, "Coordination" },
            { SpellId.QuicknessSelf1, "Quickness" },
            { SpellId.QuicknessOther1, "Quickness" },
            { SpellId.FocusSelf1, "Focus" },
            { SpellId.FocusOther1, "Focus" },
            { SpellId.WillpowerSelf1, "Willpower" },
            { SpellId.WillpowerOther1, "Willpower" },
            { SpellId.StaminaToHealthOther1, "Stamina to Health" },
            { SpellId.StaminaToHealthSelf1, "Stamina to Health" },
            { SpellId.StaminaToManaOther1, "Stamina to Mana" },
            { SpellId.StaminaToManaSelf1, "Stamina to Mana" },
            { SpellId.HealthToManaOther1, "Health to Mana" },
            { SpellId.CookingMasteryOther1, "Cooking" },
            { SpellId.CookingMasterySelf1, "Cooking" },
            { SpellId.FletchingMasteryOther1, "Fletching" },
            { SpellId.FletchingMasterySelf1, "Fletching" },
            { SpellId.AlchemyMasteryOther1, "Alchemy" },
            { SpellId.AlchemyMasterySelf1, "Alchemy" },
            { SpellId.ShockwaveStreak1, "Shock Wave" },
            { SpellId.WhirlingBladeStreak1, "Blades" },
            { SpellId.ArcanumSalvagingSelf1, "Arcanum Salvaging" },
            { SpellId.ArcanumSalvagingOther1, "Arcanum Salvaging" },
            { SpellId.GearcraftMastery1, "Item Tinkering" },
            { SpellId.GearcraftMasterySelf1, "Item Tinkering" },
            { SpellId.TwoHandedMasteryOther1, "Two Handed Combat" },
            { SpellId.TwoHandedMasterySelf1, "Two Handed Combat" },
            { SpellId.Corrosion1, "Corrosion" },
            { SpellId.Corruption1, "Corruption" },
            { SpellId.VoidMagicMasteryOther1, "Void Magic" },
            { SpellId.VoidMagicMasterySelf1, "Void Magic" },
            { SpellId.DirtyFightingMasteryOther1, "Dirty Fighting" },
            { SpellId.DirtyFightingMasterySelf1, "Dirty Fighting" },
            { SpellId.DualWieldMasteryOther1, "Dual Wield" },
            { SpellId.DualWieldMasterySelf1, "Dual Wield" },
            { SpellId.RecklessnessMasteryOther1, "Recklessness" },
            { SpellId.RecklessnessMasterySelf1, "Recklessness" },
            { SpellId.ShieldMasteryOther1, "Shield" },
            { SpellId.ShieldMasterySelf1, "Shield" },
            { SpellId.SneakAttackMasteryOther1, "Sneak Attack" },
            { SpellId.SneakAttackMasterySelf1, "Sneak Attack" },
            { SpellId.SummoningMasteryOther1, "Summoning" },
            { SpellId.SummoningMasterySelf1, "Summoning" },
            // added from magloot logs
            { SpellId.BloodDrinkerSelf1, "Blood Drinker" },
            { SpellId.SwiftKillerSelf1, "Swift Killer" },
            { SpellId.DefenderSelf1, "Defender" },
            { SpellId.NetherBolt1, "Nether Bolt" },
            { SpellId.NetherArc1, "Nether Arc" },
            { SpellId.CurseFestering1, "Curse Festering" },
            { SpellId.CurseWeakness1, "Curse Weakness" },
            { SpellId.CurseDestructionOther1, "Curse Destruction" },
        };
    }

    public static string GetDescriptor(SpellId spellId)
    {
        var spellLevels = SpellLevelProgression.GetSpellLevels(spellId);

        if (spellLevels == null)
        {
            return string.Empty;
        }

        var firstSpell = spellLevels[0];

        if (!descriptors.TryGetValue(firstSpell, out var descriptor))
        {
            return string.Empty;
        }

        return $" of {descriptor}";
    }
}
