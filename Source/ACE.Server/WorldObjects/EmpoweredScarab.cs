using System;
using System.Collections.Generic;
using System.Linq;
using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Entity.Models;
using ACE.Server.Entity;
using ACE.Server.Network.GameEvent.Events;
using ACE.Server.Network.GameMessages.Messages;
using ACE.Server.Network.Structure;
using Serilog;

namespace ACE.Server.WorldObjects
{
    public enum EmpoweredScarabEffect
    {
        None,
        CastProt,
        CastVuln,
        CastItemBuff,
        CastVitalRate,
        Intensity,
        Shield,
        ManaReduction,
        Duplicate,
        Detonate,
        Crit
    }
    public enum EmpoweredScarabColor
    {
        Blue,
        Yellow,
        Red
    }

    public enum EmpoweredScarabSchool
    {
        War = 1,
        Life = 2
    }

    public enum EmpoweredScarabBonusStat
    {
        None,
        GearCrit,
        GearCritDamage,
        GearDamageResist,
        GearMaxHealth,
        AegisLevel
    }


    public class EmpoweredScarab : WorldObject
    {
        private readonly ILogger _log = Log.ForContext<EmpoweredScarab>();

        public static readonly List<SpellCategory> LifeBeneficialTriggerSpells = new List<SpellCategory>()
        {
            SpellCategory.HealingRaising,

            SpellCategory.HealthRaising,
            SpellCategory.StaminaRaising,
            SpellCategory.ManaRaising
        };

        public static readonly List<SpellCategory> LifeHarmfulTriggerSpells = new List<SpellCategory>()
        {
            SpellCategory.HealingLowering,

            SpellCategory.HealthLowering,
            SpellCategory.StaminaLowering,
            SpellCategory.ManaLowering
        };

        public static readonly List<SpellCategory> LifeIntensityTriggerCategories = new List<SpellCategory>()
        {
            SpellCategory.HealingRaising,
            SpellCategory.HealthRaising,
            SpellCategory.HealthRestoring,
            SpellCategory.HealingLowering,
            SpellCategory.HealthLowering,
            SpellCategory.HealthDepleting,

            SpellCategory.StaminaRaising,
            SpellCategory.StaminaRestoring,
            SpellCategory.StaminaLowering,
            SpellCategory.StaminaDepleting,

            SpellCategory.ManaRaising,
            SpellCategory.ManaRestoring,
            SpellCategory.ManaLowering,
            SpellCategory.ManaDepleting
        };

        public static readonly List<SpellCategory> WarProjectileTriggerCategories = new List<SpellCategory>()
        {
            SpellCategory.SlashingMissile,
            SpellCategory.SlashingSeeker,
            SpellCategory.SlashingStrike,
            SpellCategory.SlashingStreak,
            SpellCategory.SlashingBlast,
            SpellCategory.SlashingBurst,
            SpellCategory.BladeVolley,
            SpellCategory.SlashingRing,
            SpellCategory.SlashingWall,

            SpellCategory.PiercingMissile,
            SpellCategory.PiercingSeeker,
            SpellCategory.PiercingStrike,
            SpellCategory.PiercingStreak,
            SpellCategory.PiercingBlast,
            SpellCategory.PiercingBurst,
            SpellCategory.ForceVolley,
            SpellCategory.PiercingRing,
            SpellCategory.PiercingWall,

            SpellCategory.BludgeoningMissile,
            SpellCategory.BludgeoningSeeker,
            SpellCategory.BludgeoningStrike,
            SpellCategory.BludgeoningStreak,
            SpellCategory.BludgeoningBlast,
            SpellCategory.BludgeoningBurst,
            SpellCategory.BludgeoningVolley,
            SpellCategory.BludgeoningRing,
            SpellCategory.BludgeoningWall,

            SpellCategory.AcidMissile,
            SpellCategory.AcidSeeker,
            SpellCategory.AcidStrike,
            SpellCategory.AcidStreak,
            SpellCategory.AcidBlast,
            SpellCategory.AcidBurst,
            SpellCategory.AcidVolley,
            SpellCategory.AcidRing,
            SpellCategory.AcidWall,

            SpellCategory.FireMissile,
            SpellCategory.FireSeeker,
            SpellCategory.FireStrike,
            SpellCategory.FireStreak,
            SpellCategory.FireBlast,
            SpellCategory.FireBurst,
            SpellCategory.FlameVolley,
            SpellCategory.FireRing,
            SpellCategory.FireWall,

            SpellCategory.ColdMissile,
            SpellCategory.ColdSeeker,
            SpellCategory.ColdStrike,
            SpellCategory.ColdStreak,
            SpellCategory.ColdBlast,
            SpellCategory.ColdBurst,
            SpellCategory.FrostVolley,
            SpellCategory.ColdRing,
            SpellCategory.ColdWall,

            SpellCategory.ElectricMissile,
            SpellCategory.ElectricSeeker,
            SpellCategory.ElectricStrike,
            SpellCategory.ElectricStreak,
            SpellCategory.ElectricBlast,
            SpellCategory.ElectricBurst,
            SpellCategory.LightningVolley,
            SpellCategory.ElectricRing,
            SpellCategory.ElectricWall
        };

        public int? EmpoweredScarabColor
        {
            get => GetProperty(PropertyInt.EmpoweredScarabColor);
            set { if (!value.HasValue) RemoveProperty(PropertyInt.EmpoweredScarabColor); else SetProperty(PropertyInt.EmpoweredScarabColor, value.Value); }
        }

        public int? EmpoweredScarabSchool
        {
            get => GetProperty(PropertyInt.EmpoweredScarabSchool);
            set { if (!value.HasValue) RemoveProperty(PropertyInt.EmpoweredScarabSchool); else SetProperty(PropertyInt.EmpoweredScarabSchool, value.Value); }
        }

        public int? EmpoweredScarabEffectId
        {
            get => GetProperty(PropertyInt.EmpoweredScarabEffectId);
            set { if (!value.HasValue) RemoveProperty(PropertyInt.EmpoweredScarabEffectId); else SetProperty(PropertyInt.EmpoweredScarabEffectId, value.Value); }
        }

        public int? EmpoweredScarabMaxLevel
        {
            get => GetProperty(PropertyInt.EmpoweredScarabMaxLevel);
            set { if (!value.HasValue) RemoveProperty(PropertyInt.EmpoweredScarabMaxLevel); else SetProperty(PropertyInt.EmpoweredScarabMaxLevel, value.Value); }
        }

        public uint? EmpoweredScarabTriggerSpellId
        {
            get => GetProperty(PropertyDataId.EmpoweredScarabTriggerSpellId);
            set { if (!value.HasValue) RemoveProperty(PropertyDataId.EmpoweredScarabTriggerSpellId); else SetProperty(PropertyDataId.EmpoweredScarabTriggerSpellId, value.Value); }
        }

        public uint? EmpoweredScarabCastSpellId
        {
            get => GetProperty(PropertyDataId.EmpoweredScarabCastSpellId);
            set { if (!value.HasValue) RemoveProperty(PropertyDataId.EmpoweredScarabCastSpellId); else SetProperty(PropertyDataId.EmpoweredScarabCastSpellId, value.Value); }
        }

        public double? EmpoweredScarabTriggerChance
        {
            get => GetProperty(PropertyFloat.EmpoweredScarabTriggerChance);
            set { if (!value.HasValue) RemoveProperty(PropertyFloat.EmpoweredScarabTriggerChance); else SetProperty(PropertyFloat.EmpoweredScarabTriggerChance, value.Value); }
        }

        public double? EmpoweredScarabManaReserved
        {
            get => GetProperty(PropertyFloat.EmpoweredScarabManaReserved);
            set { if (!value.HasValue) RemoveProperty(PropertyFloat.EmpoweredScarabManaReserved); else SetProperty(PropertyFloat.EmpoweredScarabManaReserved, value.Value); }
        }
        public double? EmpoweredScarabReductionAmount
        {
            get => GetProperty(PropertyFloat.EmpoweredScarabReductionAmount);
            set { if (!value.HasValue) RemoveProperty(PropertyFloat.EmpoweredScarabReductionAmount); else SetProperty(PropertyFloat.EmpoweredScarabReductionAmount, value.Value); }
        }

        public double? EmpoweredScarabIntensity
        {
            get => GetProperty(PropertyFloat.EmpoweredScarabIntensity);
            set { if (!value.HasValue) RemoveProperty(PropertyFloat.EmpoweredScarabIntensity); else SetProperty(PropertyFloat.EmpoweredScarabIntensity, value.Value); }
        }

        public Spell TriggerSpell { get; set; }
        public uint? SpellLevel { get; set; }
        public bool IsWeaponSpell { get; set; }
        public WorldObject SpellTarget { get; set; }
        public Creature CreatureToCastSpellFrom { get; set; }
        public bool UseProgression { get; set; }
        public float SpellIntensityMultiplier { get; set; }
        public float SpellStatModValMultiplier { get; set; }

        public double NextEmpoweredScarabTriggerTime = 0;

        /// <summary>
        /// A new biota be created taking all of its values from weenie.
        /// </summary>
        public EmpoweredScarab(Weenie weenie, ObjectGuid guid) : base(weenie, guid)
        {
            SetEphemeralValues();
        }

        /// <summary>
        /// Restore a WorldObject from the database.
        /// </summary>
        public EmpoweredScarab(Biota biota) : base(biota)
        {
            SetEphemeralValues();
        }

        private void SetEphemeralValues()
        {
            IsWeaponSpell = true;
            UseProgression = true;
            SpellIntensityMultiplier = 1.0f;
            SpellStatModValMultiplier = 1.0f;
        }

        public virtual void RechargeEmpoweredScarab(Hotspot manaField)
        {
            if (manaField == null)
                return;

            Player playerWielder = Wielder as Player;
            if (playerWielder == null)
                return;

            var forwardCommand = playerWielder.CurrentMovementData.MovementType == MovementType.Invalid && playerWielder.CurrentMovementData.Invalid != null ? playerWielder.CurrentMovementData.Invalid.State.ForwardCommand : MotionCommand.Invalid;
            if (forwardCommand != MotionCommand.MeditateState)
                return;

            if (playerWielder.GetCreatureSkill((MagicSchool)EmpoweredScarabSchool).AdvancementClass < SkillAdvancementClass.Trained)
                return;

            if (Structure < MaxStructure)
            {
                IncreaseStructure(50, playerWielder);
            }
        }

        private void ResetScarabBonus()
        {
            GearCrit = 0;
            GearCritDamage = 0;
            GearDamageResist = 0;
            GearMaxHealth = 0;
            AegisLevel = 0;
        }

        public void SetScarabBonus(int bonusStat, int amount)
        {
            ResetScarabBonus();

            switch(bonusStat)
            {
                case 1: GearCrit = amount; break;
                case 2: GearCritDamage = amount; break;
                case 3: GearDamageResist = amount; break;
                case 4: GearMaxHealth = amount; break;
                case 5: AegisLevel = amount; break;   
            }
        }

        private void IncreaseStructure(int amount, Player player)
        {
            Structure = (ushort)Math.Min((Structure ?? 0) + amount, MaxStructure ?? 0);

            if (player != null)
                player.Session.Network.EnqueueSend(new GameMessagePublicUpdatePropertyInt(this, PropertyInt.Structure, (int)Structure));

            bool isWielded = player != null && player == Wielder;

            if (Structure < MaxStructure)
            {
                if (isWielded)
                {
                    player.Session.Network.EnqueueSend(new GameMessageSystemChat($"Your mana scarab gains a charge!", ChatMessageType.Magic));
                    player.EnqueueBroadcast(new GameMessageScript(player.Guid, PlayScript.PortalStorm));
                }
            }
            else
            {
                if (isWielded)
                {
                    player.Session.Network.EnqueueSend(new GameMessageSystemChat($"Your mana scarab gains a charge, it is now fully charged!", ChatMessageType.Magic));
                    player.EnqueueBroadcast(new GameMessageScript(player.Guid, PlayScript.HealthUpBlue));
                }
            }
        }

        public void DecreaseStructure(int amount, Player player, bool showDecreaseEffect = true)
        {
            Structure = (ushort)Math.Max((Structure ?? 0) - amount, 0);

            if (player != null)
                player.Session.Network.EnqueueSend(new GameMessagePublicUpdatePropertyInt(this, PropertyInt.Structure, (int)Structure));
        }

        public void OnEquip(Player player)
        {
            List<EmpoweredScarab> equippedManaScarabs = null;
            equippedManaScarabs = player.GetEquippedEmpoweredScarabs();

            if (equippedManaScarabs == null)
                return;

            var reservedMana = 1.0;
            foreach (EmpoweredScarab manaScarab in equippedManaScarabs)
                reservedMana -= manaScarab.EmpoweredScarabManaReserved ?? 0.0;

            ActivateReservedMana(player, (float)reservedMana);
        }

        public void OnDequip(Player player)
        {
            DeactivateReservedMana(player);
            OnEquip(player);
        }

        private void ActivateReservedMana(Player player, float reservedMana)
        {
            Spell spell = new Spell(SpellId.InfirmedMana);

            var addResult = EnchantmentManager.Add(spell, null, null, true);
            addResult.Enchantment.StatModValue = reservedMana;

            player.Session.Network.EnqueueSend(new GameEventMagicUpdateEnchantment(player.Session, new Enchantment(player, addResult.Enchantment)));
            player.HandleSpellHooks(spell);
        }

        private void DeactivateReservedMana(Player player)
        {
            Spell spell = new Spell(SpellId.InfirmedMana);

            var enchantments = player.Biota.PropertiesEnchantmentRegistry.Clone(BiotaDatabaseLock).Where(i => i.Duration == -1 && i.SpellId != (int)SpellId.InfirmedMana).ToList();

            foreach (var enchantment in enchantments)
            {
                if (enchantment.SpellId == spell.Id)
                {
                    player.EnchantmentManager.Dispel(enchantment);
                    player.HandleSpellHooks(spell);
                }
            }
        }

        public void OnLevelUp()
        {
            MaxStructure += 50;
            EmpoweredScarabManaReserved -= 0.002;

            if (CooldownDuration > 0)
                CooldownDuration -= 0.2;

            if (EmpoweredScarabTriggerChance > 0)
                EmpoweredScarabTriggerChance += 0.01;

            if (EmpoweredScarabReductionAmount > 0)
                EmpoweredScarabReductionAmount += 0.01;

            if (EmpoweredScarabIntensity > 1.0)
                EmpoweredScarabIntensity += 0.01;
        }

        public void StartCooldown(Player player)
        {
            player.EnchantmentManager.StartCooldown(this);
        }

        public static bool IsEmpoweredScarab(uint wcid)
        {
            uint[] manaScarabWcids =
            {
                (int)Factories.Enum.WeenieClassName.empoweredScarabBlue_Life,
                (int)Factories.Enum.WeenieClassName.empoweredScarabBlue_War,
                (int)Factories.Enum.WeenieClassName.empoweredScarabYellow_Life,
                (int)Factories.Enum.WeenieClassName.empoweredScarabYellow_War,
                (int)Factories.Enum.WeenieClassName.empoweredScarabRed_Life,
                (int)Factories.Enum.WeenieClassName.empoweredScarabRed_War
            };

            return manaScarabWcids.Contains(wcid);
        }
    }
}
