using System;
using System.Collections.Generic;
using System.Linq;
using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Entity.Models;
using ACE.Server.Entity;
using ACE.Server.Factories.Tables;
using ACE.Server.Network.GameEvent.Events;
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

    public partial class CombatFocus : WorldObject
    {
        private readonly ILogger _log = Log.ForContext<CombatFocus>();

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
        }

        public int? CombatFocusType
        {
            get => GetProperty(PropertyInt.CombatFocusType);
            set { if (!value.HasValue) RemoveProperty(PropertyInt.CombatFocusType); else SetProperty(PropertyInt.CombatFocusType, value.Value); }
        }

        private static readonly List<SpellId> WarriorSpells = new List<SpellId>()
        {
            SpellId.StrengthSelf1,
            SpellId.EnduranceSelf1,
            SpellId.ShieldMasterySelf1,
            SpellId.HeavyWeaponsMasterySelf1, // Martial Weapons
            SpellId.InvulnerabilitySelf1,
            SpellId.ThrownWeaponMasterySelf1,
            SpellId.TwoHandedMasterySelf1,
            SpellId.HealingMasterySelf1,
            SpellId.MonsterAttunementSelf1   // Perception
        };

        private static readonly List<SpellId> BlademasterSpells = new List<SpellId>()
        {
            SpellId.EnduranceSelf1,
            SpellId.CoordinationSelf1,
            SpellId.ShieldMasterySelf1,
            SpellId.HeavyWeaponsMasterySelf1,           // Martial Weapons
            SpellId.FinesseWeaponsMasterySelf1,         // Dagger
            SpellId.StaffMasterySelf1,                  
            SpellId.UnarmedCombatMasterySelf1,
            SpellId.InvulnerabilitySelf1,
            SpellId.ThrownWeaponMasterySelf1,
            SpellId.TwoHandedMasterySelf1,
            SpellId.HealingMasterySelf1,
            SpellId.DualWieldMasterySelf1,
            SpellId.MissileWeaponsMasterySelf1,         // Bows
            SpellId.LockpickMasterySelf1,               // Thievery
        };

        private static readonly List<SpellId> ArcherSpells = new List<SpellId>()
        {
            SpellId.CoordinationSelf1,
            SpellId.QuicknessSelf1,
            SpellId.DeceptionMasterySelf1,
            SpellId.InvulnerabilitySelf1,
            SpellId.ThrownWeaponMasterySelf1,
            SpellId.HealingMasterySelf1,
            SpellId.DualWieldMasterySelf1,
            SpellId.MissileWeaponsMasterySelf1,         // Bows
            SpellId.LockpickMasterySelf1,               // Thievery
            SpellId.FinesseWeaponsMasterySelf1,         // Dagger
            SpellId.StaffMasterySelf1,                  
            SpellId.UnarmedCombatMasterySelf1,
        };

        private static readonly List<SpellId> VagabondSpells = new List<SpellId>()
        {
            SpellId.QuicknessSelf1,
            SpellId.FocusSelf1,
            SpellId.DeceptionMasterySelf1,
            SpellId.MagicResistanceSelf1,
            SpellId.ArcaneEnlightenmentSelf1,
            SpellId.WarMagicMasterySelf1,
            SpellId.LifeMagicMasterySelf1,
            SpellId.ManaMasterySelf1,
            SpellId.DualWieldMasterySelf1,
            SpellId.MissileWeaponsMasterySelf1,         // Bows
            SpellId.LockpickMasterySelf1,               // Thievery
            SpellId.FinesseWeaponsMasterySelf1,         // Dagger
            SpellId.StaffMasterySelf1,
            SpellId.UnarmedCombatMasterySelf1,
        };

        private static readonly List<SpellId> SorcererSpells = new List<SpellId>()
        {
            SpellId.FocusSelf1,
            SpellId.WillpowerSelf1,
            SpellId.DeceptionMasterySelf1,
            SpellId.MagicResistanceSelf1,
            SpellId.ArcaneEnlightenmentSelf1,
            SpellId.WarMagicMasterySelf1,
            SpellId.LifeMagicMasterySelf1,
            SpellId.ManaMasterySelf1,
            SpellId.MonsterAttunementSelf1              // Perception
        };

        private static readonly List<SpellId> SpellswordSpells = new List<SpellId>()
        {
            SpellId.WillpowerSelf1,
            SpellId.StrengthSelf1,
            SpellId.ShieldMasterySelf1,
            SpellId.HeavyWeaponsMasterySelf1,           // Martial Weapons
            SpellId.TwoHandedMasterySelf1,
            SpellId.MagicResistanceSelf1,
            SpellId.ArcaneEnlightenmentSelf1,
            SpellId.WarMagicMasterySelf1,
            SpellId.LifeMagicMasterySelf1,
            SpellId.ManaMasterySelf1,
            SpellId.MonsterAttunementSelf1              // Perception
        };

        private static readonly List<List<SpellId>> CombatFocusSpells = new List<List<SpellId>>()
        {
            WarriorSpells,
            BlademasterSpells,
            ArcherSpells,
            VagabondSpells,
            SorcererSpells,
            SpellswordSpells
        };

        public void OnEquip(Player player)
        {
            if (player == null)
                return;
            
            var combatFocusType = CombatFocusType;
            if (combatFocusType == null || combatFocusType < 1)
                return;

            var spellLevel = player.GetPlayerTier(player.Level) - 1;
            if (spellLevel < 1)
                return;

            var combatFocusSpells = CombatFocusSpells;
            foreach (SpellId spellId in combatFocusSpells[(int)combatFocusType - 1])
            {
                var leveledSpell = SpellLevelProgression.GetSpellAtLevel(spellId, spellLevel);
                ActivateSpell(player, new Spell(leveledSpell));
            }
        }

        public void OnDequip(Player player, bool onLevelUp = false, int? startingLevel = null)
        {
            if (player == null)
                return;

            var combatFocusType = CombatFocusType;
            if (combatFocusType == null || combatFocusType < 1)
                return;

            var spellLevel = player.GetPlayerTier(player.Level) - 1;
            int startingSpellLevel;



            if (spellLevel < 1)
                return;

            var combatFocusSpells = CombatFocusSpells;

            if (onLevelUp)
            {
                startingSpellLevel = player.GetPlayerTier(startingLevel) - 1;
                foreach (SpellId spellId in combatFocusSpells[(int)combatFocusType - 1])
                {
                    var leveledSpell = SpellLevelProgression.GetSpellAtLevel(spellId, startingSpellLevel);
                    DeactivateSpell(player, new Spell(leveledSpell));
                }
            }
            else
            {
                foreach (SpellId spellId in combatFocusSpells[(int)combatFocusType - 1])
                {
                    var leveledSpell = SpellLevelProgression.GetSpellAtLevel(spellId, spellLevel);
                    DeactivateSpell(player, new Spell(leveledSpell));
                }
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

        private void ActivateSpell(Player player, Spell spell)
        {
            var addResult = player.EnchantmentManager.Add(spell, null, null, true);
            //Console.WriteLine($"ActivateSpell: {spell.Name} Beneficial? {spell.IsBeneficial}");

            player.Session.Network.EnqueueSend(new GameEventMagicUpdateEnchantment(player.Session, new Enchantment(player, addResult.Enchantment)));
            player.HandleSpellHooks(spell);
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

        public List<SpellId> GetCombatFocusSpellList(CombatFocusType combatFocusType)
        {
            switch (combatFocusType)
            {
                case WorldObjects.CombatFocusType.Warrior: return WarriorSpells;
                case WorldObjects.CombatFocusType.Blademaster: return BlademasterSpells;
                case WorldObjects.CombatFocusType.Archer: return ArcherSpells;
                case WorldObjects.CombatFocusType.Vagabond: return VagabondSpells;
                case WorldObjects.CombatFocusType.Sorcerer: return SorcererSpells;
                case WorldObjects.CombatFocusType.Spellsword: return SpellswordSpells;
                default: return null;
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
    }
}
