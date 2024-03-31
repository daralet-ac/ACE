using System;
using System.Collections.Generic;
using System.Linq;
using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Entity.Models;
using ACE.Server.Entity;
using ACE.Server.Factories.Tables;
using ACE.Server.Factories.Tables.Wcids;
using ACE.Server.Network.GameEvent.Events;
using ACE.Server.Network.GameMessages.Messages;
using ACE.Server.Network.Structure;
using Serilog;

namespace ACE.Server.WorldObjects
{
    public enum CombatFocusType
    {
        None,
        Warrior,
        Blademaster,
        Archer,
        Vagabond,
        Sorcerer,
        Spellsword
    }

    public class CombatFocus : WorldObject
    {
        private readonly ILogger _log = Log.ForContext<CombatFocus>();

        private List<SpellId> CurrentSpells = new List<SpellId>();

        public int? CombatFocusType
        {
            get => GetProperty(PropertyInt.CombatFocusType);
            set { if (!value.HasValue) RemoveProperty(PropertyInt.CombatFocusType); else SetProperty(PropertyInt.CombatFocusType, value.Value); }
        }

        public int? CombatFocusAttributeSpellRemoved
        {
            get => GetProperty(PropertyInt.CombatFocusAttributeSpellRemoved);
            set { if (!value.HasValue) RemoveProperty(PropertyInt.CombatFocusAttributeSpellRemoved); else SetProperty(PropertyInt.CombatFocusAttributeSpellRemoved, value.Value); }
        }

        public int? CombatFocusAttributeSpellAdded
        {
            get => GetProperty(PropertyInt.CombatFocusAttributeSpellAdded);
            set { if (!value.HasValue) RemoveProperty(PropertyInt.CombatFocusAttributeSpellAdded); else SetProperty(PropertyInt.CombatFocusAttributeSpellAdded, value.Value); }
        }

        public int? CombatFocusSkillSpellRemoved
        {
            get => GetProperty(PropertyInt.CombatFocusSkillSpellRemoved);
            set { if (!value.HasValue) RemoveProperty(PropertyInt.CombatFocusSkillSpellRemoved); else SetProperty(PropertyInt.CombatFocusSkillSpellRemoved, value.Value); }
        }

        public int? CombatFocusSkillSpellAdded
        {
            get => GetProperty(PropertyInt.CombatFocusSkillSpellAdded);
            set { if (!value.HasValue) RemoveProperty(PropertyInt.CombatFocusSkillSpellAdded); else SetProperty(PropertyInt.CombatFocusSkillSpellAdded, value.Value); }
        }

        /// <summary>
        /// A new biota be created taking all of its values from weenie.
        /// </summary>
        public CombatFocus(Weenie weenie, ObjectGuid guid) : base(weenie, guid)
        {
            SetEphemeralValues();
        }

        /// <summary>
        /// Restore a WorldObject from the database.
        /// </summary>
        public CombatFocus(Biota biota) : base(biota)
        {
            SetEphemeralValues();
        }

        private void SetEphemeralValues()
        {
            InitializeSpellList();
        }

        private static readonly List<SpellId> WarriorSpells = new List<SpellId>()
        {
            SpellId.StrengthSelf1,
            SpellId.EnduranceSelf1,
            SpellId.HeavyWeaponsMasterySelf1, // Martial Weapons
            SpellId.ShieldMasterySelf1,
            SpellId.InvulnerabilitySelf1,
            SpellId.ThrownWeaponMasterySelf1,
            SpellId.HealingMasterySelf1,
            SpellId.MonsterAttunementSelf1 // Perception
        };

        private static readonly List<SpellId> BlademasterSpells = new List<SpellId>()
        {
            SpellId.EnduranceSelf1,
            SpellId.CoordinationSelf1,
            SpellId.HeavyWeaponsMasterySelf1, // Martial Weapons
            SpellId.DualWieldMasterySelf1,
            SpellId.InvulnerabilitySelf1,
            SpellId.ThrownWeaponMasterySelf1,
            SpellId.HealingMasterySelf1,
            SpellId.SprintSelf1
        };

        private static readonly List<SpellId> ArcherSpells = new List<SpellId>()
        {
            SpellId.CoordinationSelf1,
            SpellId.QuicknessSelf1,
            SpellId.MissileWeaponsMasterySelf1, // Bows
            SpellId.FinesseWeaponsMasterySelf1, // Dagger
            SpellId.StaffMasterySelf1,
            SpellId.UnarmedCombatMasterySelf1,
            SpellId.InvulnerabilitySelf1,
            SpellId.MonsterAttunementSelf1, // Perception
            SpellId.HealingMasterySelf1,
            SpellId.SprintSelf1,
        };

        private static readonly List<SpellId> VagabondSpells = new List<SpellId>()
        {
            SpellId.QuicknessSelf1,
            SpellId.FocusSelf1,
            SpellId.FinesseWeaponsMasterySelf1, // Dagger
            SpellId.StaffMasterySelf1,
            SpellId.UnarmedCombatMasterySelf1,
            SpellId.DualWieldMasterySelf1,
            SpellId.MagicResistanceSelf1,
            SpellId.MonsterAttunementSelf1, // Perception
            SpellId.DeceptionMasterySelf1,
            SpellId.LockpickMasterySelf1, // Thievery
        };

        private static readonly List<SpellId> SorcererSpells = new List<SpellId>()
        {
            SpellId.FocusSelf1,
            SpellId.WillpowerSelf1,
            SpellId.WarMagicMasterySelf1,
            SpellId.LifeMagicMasterySelf1,
            SpellId.ManaMasterySelf1,
            SpellId.ArcaneEnlightenmentSelf1,
            SpellId.DeceptionMasterySelf1,
            SpellId.MagicResistanceSelf1
        };

        private static readonly List<SpellId> SpellswordSpells = new List<SpellId>()
        {
            SpellId.WillpowerSelf1,
            SpellId.StrengthSelf1,
            SpellId.HeavyWeaponsMasterySelf1, // Martial Weapons
            SpellId.TwoHandedMasterySelf1,
            SpellId.ArcaneEnlightenmentSelf1,
            SpellId.WarMagicMasterySelf1,
            SpellId.LifeMagicMasterySelf1,
            SpellId.MagicResistanceSelf1
        };

        public void InitializeSpellList()
        {
            var spellList = new List<SpellId>();
            switch (CombatFocusType)
            {
                case 1: spellList = WarriorSpells; break;
                case 2: spellList = BlademasterSpells; break;
                case 3: spellList = ArcherSpells; break;
                case 4: spellList = VagabondSpells; break;
                case 5: spellList = SorcererSpells; break;
                case 6: spellList = SpellswordSpells; break;
            }
            
            foreach (var spellId in spellList)
                CurrentSpells.Add(spellId);

            if (CombatFocusAttributeSpellAdded != null)
                CurrentSpells.Add((SpellId)CombatFocusAttributeSpellAdded);

            if (CombatFocusAttributeSpellRemoved != null)
                CurrentSpells.Remove((SpellId)CombatFocusAttributeSpellRemoved);

            if (CombatFocusSkillSpellAdded != null)
                CurrentSpells.Add((SpellId)CombatFocusSkillSpellAdded);

            if (CombatFocusSkillSpellRemoved != null)
                CurrentSpells.Remove((SpellId)CombatFocusSkillSpellRemoved);

            UpdateDescriptionText();
        }

        public List<SpellId> GetCurrentSpellList()
        {
            return CurrentSpells;
        }

        public void OnEquip(Player player)
        {
            if (player == null)
                return;
            
            var combatFocusType = CombatFocusType;
            if (combatFocusType == null || combatFocusType < 1)
                return;

            if (CurrentSpells.Count == 0)
                _log.Warning("{Player} tried to equip pre-patch combat focus.", player.Name);

            ActivateSpells(player, CurrentSpells);

            var particalEffect = GetFocusParticleEffect();
            player.PlayParticleEffect(particalEffect, player.Guid);
        }

        public void OnDequip(Player player, bool onLevelUp = false, int? startingLevel = null)
        {
            if (player == null)
                return;

            var combatFocusType = CombatFocusType;
            if (combatFocusType == null || combatFocusType < 1)
                return;

            DeactivateSpells(player, CurrentSpells, onLevelUp, startingLevel);
        }

        private void ActivateSpells(Player player, List<SpellId> spellIds)
        {
            var spellLevel = player.GetPlayerTier(player.Level) - 1;
            if (spellLevel < 1)
                return;

            foreach (SpellId spellId in spellIds)
            {
                var leveledSpell = SpellLevelProgression.GetSpellAtLevel(spellId, spellLevel);
                ActivateSpell(player, new Spell(leveledSpell));
            }
        }

        private void ActivateSpell(Player player, Spell spell)
        {
            var addResult = player.EnchantmentManager.Add(spell, null, null, true);
            //Console.WriteLine($"ActivateSpell: {spell.Name} Beneficial? {spell.IsBeneficial}");

            player.Session.Network.EnqueueSend(new GameEventMagicUpdateEnchantment(player.Session, new Enchantment(player, addResult.Enchantment)));
            player.HandleSpellHooks(spell);
        }

        private void DeactivateSpells(Player player, List<SpellId> spellIds, bool onLevelUp, int? startingLevel = null)
        {
            var spellLevel = player.GetPlayerTier(player.Level) - 1;
            if (spellLevel < 1)
                return;

            if (onLevelUp)
            {
                var startingSpellLevel = player.GetPlayerTier(startingLevel) - 1;
                foreach (SpellId spellId in spellIds)
                {
                    var leveledSpell = SpellLevelProgression.GetSpellAtLevel(spellId, startingSpellLevel);
                    DeactivateSpell(player, new Spell(leveledSpell));
                }
            }
            else
            {
                foreach (SpellId spellId in spellIds)
                {
                    var leveledSpell = SpellLevelProgression.GetSpellAtLevel(spellId, spellLevel);
                    DeactivateSpell(player, new Spell(leveledSpell));
                }
            }
        }

        private void DeactivateSpell(Player player, Spell spell)
        {
            var enchantments = player.Biota.PropertiesEnchantmentRegistry.Clone(BiotaDatabaseLock).Where(i => i.Duration == -1 && i.SpellId != (int)SpellId.Vitae).ToList();

            //Console.WriteLine($"DeactivateSpell: {spell.Name} NumberOfEnchantments: {enchantments.Count}");
            foreach (var enchantment in enchantments)
            {
                if (enchantment.SpellId == spell.Id)
                {
                    //Console.WriteLine($"DeactivateSpell: {spell.Name}");
                    player.EnchantmentManager.Dispel(enchantment);
                    player.HandleSpellHooks(spell);
                }
            }
        }

        public void OnLevelUp(Player player, int startingLevel)
        {
            OnDequip(player, true, startingLevel);
            OnEquip(player);
        }

        public CombatAbility GetCombatAbility()
        {
            var value = 0;

            if (CombatAbilityId != null && CombatAbilityId.HasValue)
                value = (int)CombatAbilityId;

            return (CombatAbility)value;
        }

        public void RemoveSpell(Player player, WorldObject source, SpellId spellId, bool isAttribute)
        {
            var spellName = GetSpellName(spellId) + " spell";

            // remove spell (if a heritage skill, remove all 3)
            if (spellId == SpellId.FinesseWeaponsMasterySelf1 || spellId == SpellId.StaffMasterySelf1 || spellId == SpellId.UnarmedCombatMasterySelf1)
            {
                CurrentSpells.Remove(SpellId.FinesseWeaponsMasterySelf1);
                CurrentSpells.Remove(SpellId.StaffMasterySelf1);
                CurrentSpells.Remove(SpellId.UnarmedCombatMasterySelf1);
                spellName = "Dagger, Staff, and Unarmed Combat spells";
            }
            else 
                CurrentSpells.Remove(spellId);

            // track removed spell
            if (isAttribute)
                CombatFocusAttributeSpellRemoved = (int)spellId;
            else
                CombatFocusSkillSpellRemoved = (int)spellId;


            player.Session.Network.EnqueueSend(new GameMessageSystemChat($"You remove the {spellName} from {Name}.", ChatMessageType.Craft));
            player.TryConsumeFromInventoryWithNetworking(source);
        }

        public void AddSpell(Player player, WorldObject source, SpellId spellId, bool isAttribute)
        {
            // Add spell
            CurrentSpells.Add(spellId);

            if (isAttribute)
                CombatFocusAttributeSpellAdded = (int)spellId;
            else
                CombatFocusSkillSpellAdded = (int)spellId;

            var spellName = GetSpellName(spellId);

            player.Session.Network.EnqueueSend(new GameMessageSystemChat($"You add the {spellName} spell to {Name}.", ChatMessageType.Craft));
            player.TryConsumeFromInventoryWithNetworking(source);
        }

        public void UpdateDescriptionText()
        {
            var attributeSpellsText = "";
            var skillSpellsText = "";

            var firstAttribute = true;
            var firstSkill = true;
            foreach (var spellId in CurrentSpells)
            {
                var spellName = GetSpellName(spellId);

                if (IsAttribute(spellId))
                {
                    if (firstAttribute)
                        firstAttribute = false;
                    else
                        spellName = ", " + spellName;

                    attributeSpellsText += spellName;
                }
                else
                {
                    if (firstSkill)
                        firstSkill = false;
                    else
                        spellName = ", " + spellName;

                    skillSpellsText += spellName;
                }
            }

            var description = "Use this focus to gain a boost towards the following attributes and skills:\n\n";
            description += "Attributes: " + attributeSpellsText + ".\n\n";
            description += "Skills: " + skillSpellsText + ".\n\n";

            var advancedDescription = GetCombatAbilityDescription();

            var alteredDescription = "";
            if (CombatFocusAttributeSpellAdded != null)
                alteredDescription = "This combat focus' attribute spell has been altered.";
            if (CombatFocusSkillSpellAdded != null)
                alteredDescription = "This combat focus' skill spell has been altered.";
            if (CombatFocusAttributeSpellAdded != null && CombatFocusSkillSpellAdded != null)
                alteredDescription = "This combat focus' attribute and skill spells have been altered.";

            LongDesc = advancedDescription + description + alteredDescription;
        }

        private static bool IsAttribute(SpellId spellId)
        {
            SpellId[] attributeSpellIds = { SpellId.StrengthSelf1, SpellId.EnduranceSelf1, SpellId.CoordinationSelf1,
                                            SpellId.QuicknessSelf1, SpellId.FocusSelf1, SpellId.WillpowerSelf1};

            if (attributeSpellIds.Contains(spellId))
                return true;

            return false;
        }

        private  string GetCombatAbilityDescription()
        {
            var description = "";

            var combatAbilityId = CombatAbilityId;

            switch ((CombatAbility)combatAbilityId)
            {
                case CombatAbility.Phalanx: description += "With this focus equipped, your shield will reduce damage from all sides, and your enemies cannot make sneak attacks against you.\n\n" +
                        "Activated Combat Ability: For the next 10 seconds, gain 50% chance to block attacks from all directions.\n\n"; break;
                case CombatAbility.Provoke: description += "With this focus equipped, increase the threat you generate with your attacks by 20%. All glancing blows against are always major.\n\n" +
                        "Activated Combat Ability: For the next 10 seconds, increase your damage by 20% and your threat generated by 50%.\n\n"; break;
                case CombatAbility.Parry: description += "With this focus equipped, to gain a 20% chance to block attacks while using a two-handed weapon or dual-wielding.\n\n" +
                        "Activated Combat Ability: For the next 10 seconds, whenever you parry an attack, you strike back at your enemy.\n\n"; break;
                case CombatAbility.Fury: description += "With this focus equipped, attacks you make in melee range build Fury. As Fury increases, your damage receives a bonus, up to a maximum of 25%. " +
                        "However, the Stamina cost of your attacks is increased as well, up to an additional 100%. Beyond 50 Fury, you have a chance to injure yourself on attack.\n\n" +
                        "Activated Combat Ability: The first attack you make within the next ten seconds will gain double the bonus and exhaust all your Fury.\n\n"; break;
                case CombatAbility.Backstab: description += "With this focus equipped, your chance to deal a critical hit is increased by 20% while performing sneak attacks. Normal hits deal 20% less damage.\n\n" +
                        "Activated Combat Ability: For the next 10 seconds, your attacks from behind cannot be evaded\n\n"; break;
                case CombatAbility.SteadyShot: description += "With this focus equipped, the accuracy of your missile weapon attacks is increased by 20%.\n\n" +
                        "Activated Combat Ability: For the next 10 seconds, your arrows cannot be dodged and deal an additional 25% damage.\n\n"; break;
                case CombatAbility.Smokescreen: description += "With this focus equipped, gain 10% increased chance to evade attacks, and make enemies less likely to attack you. Other targets for the enemy must be available.\n\n" +
                        "Activated Combat Ability: For the next 10 seconds, increase your chance to evade by an additional 30%.\n\n"; break;
                case CombatAbility.Multishot: description += "With this focus equipped, to shoot an extra arrow, quarrel or dart at another monster near your main target, if there are any. Your damage is reduced by 75%.\n\n" +
                        "Activated Combat Ability: For the next 10 seconds, each time you attack you will fire at an additional enemy near your primary target.\n\n"; break;
                case CombatAbility.Overload: description += "With this focus equipped, your spells build Overload. As Overload increases, so does your spell effectiveness up to a maximum of 25%. " +
                        "However, your mana costs also increase by up to 100%. Beyond 50% Overpower, you have an increasing chance to harm yourself on spellcast.\n\n" +
                        "Activated Combat Ability: The first spell you cast within the next 10 seconds will gain a double effectiveness bonus and discharge all of your Overload.\n\n"; break;
                case CombatAbility.Battery: description += "With this focus equipped, your mana costs are reduced by 20%, the reduction increasing as your mana pool is depleted. However, below 75% mana, your spell effectiveness also begins to decrease to as little as 25% at 0 mana.\n\n" +
                        "Activated Combat Ability: For the next 10 seconds, your spells cost 0 mana and suffer no effectiveness penalty.\n\n"; break;
                case CombatAbility.Reflect: description += "While equipped, you will reflect fully resisted spells back at the caster. Additionally, gain 20% increased Magic Defense while attempting to resist a spell.\n\n" +
                        "Activated Combat Ability: For the next 10 seconds, your chance to resist spells is increased by an additional 30%.\n\nWhile equipped, gain a boost towards the following attributes and skills:\n\n"; break;
                case CombatAbility.EnchantedWeapon: description += "With this focus equipped, you have a 100% chance to trigger Cast-on-strike spells while attacking at full power/accuracy. " +
                        "The effective spellcraft of equipped weapons is increased by 10%. All weapon attacks also consume mana.\n\n" +
                        "Activated Combat Ability: For the next 10 seconds, your weapon proc spell effectiveness is increased by 25%, as is the Mana cost.\n\n"; break;
            }

            return description;
        }

        private static string GetSpellName(SpellId spellId)
        {
            var name = "";

            switch(spellId)
            {
                case SpellId.StrengthSelf1: name = "Strength"; break;
                case SpellId.EnduranceSelf1: name = "Endurance"; break;
                case SpellId.CoordinationSelf1: name = "Coordination"; break;
                case SpellId.QuicknessSelf1: name = "Quickness"; break;
                case SpellId.FocusSelf1: name = "Focus"; break;
                case SpellId.WillpowerSelf1: name = "Self"; break;

                case SpellId.HeavyWeaponsMasterySelf1: name = "Martial Weapons"; break;
                case SpellId.FinesseWeaponsMasterySelf1: name = "Dagger"; break;
                case SpellId.StaffMasterySelf1: name = "Staff"; break;
                case SpellId.UnarmedCombatMasterySelf1: name = "Unarmed Combat"; break;
                case SpellId.MissileWeaponsMasterySelf1: name = "Bows"; break;
                case SpellId.ThrownWeaponMasterySelf1: name = "Thrown Weapons"; break;
                case SpellId.TwoHandedMasterySelf1: name = "Two-Handed Combat"; break;
                case SpellId.DualWieldMasterySelf1: name = "Dual Wield"; break;
                case SpellId.ShieldMasterySelf1: name = "Shield"; break;
                case SpellId.WarMagicMasterySelf1: name = "War Magic"; break;
                case SpellId.LifeMagicMasterySelf1: name = "Life Magic"; break;
                case SpellId.InvulnerabilitySelf1: name = "Physical Defense"; break;
                case SpellId.MagicResistanceSelf1: name = "Magic Defense"; break;
                case SpellId.ArcaneEnlightenmentSelf1: name = "Arcane Lore"; break;
                case SpellId.ManaMasterySelf1: name = "Mana Conversion"; break;
                case SpellId.HealingMasterySelf1: name = "Healing"; break;
                case SpellId.MonsterAttunementSelf1: name = "Perception"; break;
                case SpellId.DeceptionMasterySelf1: name = "Deception"; break;
                case SpellId.LockpickMasterySelf1: name = "Thievery"; break;
                case SpellId.SprintSelf1: name = "Run"; break;
                case SpellId.JumpingMasterySelf1: name = "Jump"; break;
            }

            return name;
        }

        private PlayScript GetFocusParticleEffect()
        {
            switch(CombatFocusType)
            {
                default:
                case 1: return PlayScript.SkillUpRed;
                case 2: return PlayScript.SkillUpOrange;
                case 3: return PlayScript.SkillUpYellow;
                case 4: return PlayScript.SkillUpGreen;
                case 5: return PlayScript.SkillUpBlue;
                case 6: return PlayScript.SkillUpPurple;
            }
        }

        /* ---------------------------------------------------------------------------------------------------------------------------------------
         * This section could alternatively make Combat Focuses activate without needing the Trinket Slot. (instead of the equip functions above)
         * It works by setting a UI Effect on the used Combat Focus and then disabling the UI Effect on all others. It also
         * manages the hotbar automatically when the UI Effect is changed on a Combat Focus (however this causes a VHS issue).
         * 
         * In its current state, it works just fine, however Virindi Hotkey System throws an error whenever a Combat Focus
         * is used while any hotbar slot is filled. This would be the ideal solution for Combat Focuses if a solution can
         * be found for the VHS issue.
         * 
         * ValidLocations must be disabled on the weenie to prevent it from being equipped.
         * ---------------------------------------------------------------------------------------------------------------------------------------

        public override void OnActivate(WorldObject activator)
        {
            if (ItemUseable == Usable.Contained && activator is Player player)
            {
                var containedItem = player.FindObject(Guid.Full, Player.SearchLocations.MyInventory | Player.SearchLocations.MyEquippedItems);
                if (containedItem != null) // item is contained by player
                {
                    if (player.IsBusy || player.Teleporting || player.suicideInProgress)
                    {
                        player.Session.Network.EnqueueSend(new GameEventWeenieError(player.Session, WeenieError.YoureTooBusy));
                        player.EnchantmentManager.StartCooldown(this);
                        return;
                    }

                    if (player.IsDead)
                    {
                        player.Session.Network.EnqueueSend(new GameEventWeenieError(player.Session, WeenieError.Dead));
                        player.EnchantmentManager.StartCooldown(this);
                        return;
                    }
                }
                else
                    return;
            }

            base.OnActivate(activator);
        }

        public override void ActOnUse(WorldObject activator)
        {
            ActOnUse(activator, false);
        }

        public void ActOnUse(WorldObject activator, bool confirmed, WorldObject target = null)
        {
            if (!(activator is Player player))
                return;

            if (player.IsBusy || player.Teleporting || player.suicideInProgress)
            {
                player.SendWeenieError(WeenieError.YoureTooBusy);
                return;
            }

            if (player.IsJumping)
            {
                player.SendWeenieError(WeenieError.YouCantDoThatWhileInTheAir);
                return;
            }

            if (UseUserAnimation != MotionCommand.Invalid)
            {
                // some gems have UseUserAnimation and UseSound, similar to food
                // eg. 7559 - Condensed Dispel Potion

                // the animation is also weird, and differs from food, in that it is the full animation
                // instead of stopping at the 'eat/drink' point... so we pass 0.5 here?

                var animMod = (UseUserAnimation == MotionCommand.MimeDrink || UseUserAnimation == MotionCommand.MimeEat) ? 0.5f : 1.0f;

                player.ApplyConsumable(UseUserAnimation, () => UseGem(player), animMod);
            }
            else
                UseGem(player);
        }

        public void UseGem(Player player)
        {
            Console.WriteLine("\n\n--------------------");
            if (player.IsDead) return;

            // verify item is still valid
            if (player.FindObject(Guid.Full, Player.SearchLocations.MyInventory) == null)
            {
                //player.SendWeenieError(WeenieError.ObjectGone);   // results in 'Unable to move object!' transient error
                player.SendTransientError($"Cannot find the {Name}");   // custom message
                return;
            }

            if (UseSound > 0)
                player.Session.Network.EnqueueSend(new GameMessageSound(player.Guid, UseSound));

            // Disable UI Effect from all other carried Combat Focuses
            var combatFocuses = player.GetInventoryItemsOfTypeWeenieType(WeenieType.CombatFocus);

            // Enable or disable this combat focus UI Effect
            if (UiEffects == ACE.Entity.Enum.UiEffects.Undef)
            {
                UiEffects = ACE.Entity.Enum.UiEffects.Magical;
                IconUnderlayId = 100691489;
            }
            else
            {
                UiEffects = ACE.Entity.Enum.UiEffects.Undef;
                IconUnderlayId = 100683040;
            }

            Console.WriteLine($"ShortcutCount: {player.Session.Player.GetShortcuts().Count}");
            var hasShortcuts = player.Session.Player.GetShortcuts().Count > 0;
            var unHotkeyedFocuses = new List<CombatFocus>();

            //int i = 0;
            foreach (CombatFocus focus in combatFocuses)
            {
                unHotkeyedFocuses.Add(focus);

                if (hasShortcuts)
                {
                    foreach (var shortcut in player.Session.Player.GetShortcuts())
                    {
                        if (focus.Biota.Id == shortcut.ObjectId)
                        {
                            var actionChain = new ActionChain();

                            actionChain.AddAction(player, () =>
                            {
                                Console.WriteLine($"\nRemoveShortcut @ index {shortcut.Index} for shortcut focus.");
                                player.Session.Player.HandleActionRemoveShortcut(shortcut.Index);
                                //player.Session.Network.EnqueueSend(new GameEventPlayerDescription(player.Session));
                                //player.EnqueueBroadcast(new GameEventPlayerDescription(player.Session));
                            });

                            actionChain.AddDelaySeconds(3);

                            actionChain.AddAction(player, () =>
                            {
                                if (focus.Biota.Id != Biota.Id)
                                {
                                    Console.WriteLine("\nDisable UI Effect of untargeted hotkey focus.");
                                    focus.UiEffects = ACE.Entity.Enum.UiEffects.Undef;
                                    IconUnderlayId = 100683040;
                                }
                                player.EnqueueBroadcast(new GameMessageUpdateObject(focus));
                                //player.Session.Network.EnqueueSend(new GameEventPlayerDescription(player.Session));
                            });

                            actionChain.AddDelaySeconds(3);

                            actionChain.AddAction(player, () =>
                            {
                                Console.WriteLine($"\nAddShortcutBack @ index {shortcut.Index} for shortcut focus.");
                                player.Session.Player.HandleActionAddShortcut(shortcut);
                                player.Session.Network.EnqueueSend(new GameEventPlayerDescription(player.Session));
                                //player.EnqueueBroadcast(new GameEventPlayerDescription(player.Session));
                            });
                            actionChain.EnqueueChain();

                            unHotkeyedFocuses.Remove(focus);
                        }
                    }
                }
            }

            Console.WriteLine($"Number of Unhotkeyed Focuses: {unHotkeyedFocuses.Count}");
            foreach (CombatFocus focus in unHotkeyedFocuses)
            {
                if (focus.Biota.Id != Biota.Id)
                {
                    Console.WriteLine("Disabling UI Effect for: " + focus.Biota.Id);
                    focus.UiEffects = ACE.Entity.Enum.UiEffects.Undef;
                    IconUnderlayId = 100683040;
                }
                player.EnqueueBroadcast(new GameMessageUpdateObject(focus));
            }
        }
        */
    }
}
