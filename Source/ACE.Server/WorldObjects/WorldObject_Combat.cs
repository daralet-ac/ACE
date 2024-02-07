using System;
using System.Collections.Generic;
using System.Linq;
using ACE.Common;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Server.Entity;
using ACE.Server.Factories.Tables.Spells;

namespace ACE.Server.WorldObjects
{
    partial class WorldObject
    {
        /// <summary>
        /// Determines if WorldObject can damage a target via PlayerKillerStatus
        /// </summary>
        /// <returns>null if no errors, else pk error list</returns>
        public virtual List<WeenieErrorWithString> CheckPKStatusVsTarget(WorldObject target, Spell spell)
        {
            // no restrictions here
            // player attacker restrictions handled in override
            return null;
        }

        /// <summary>
        /// Tries to proc any relevant items for the attack
        /// </summary>
        public void TryProcEquippedItems(WorldObject attacker, Creature target, bool selfTarget, WorldObject weapon)
        {
            // handle procs directly on this item -- ie. phials
            // this could also be monsters with the proc spell directly on the creature
            if (HasProc && ProcSpellSelfTargeted == selfTarget)
            {
                // projectile
                // monster
                TryProcItem(attacker, target, selfTarget);
            }

            // handle proc spells for weapon
            // this could be a melee weapon, or a missile launcher
            if (weapon != null && weapon.HasProc && weapon.ProcSpellSelfTargeted == selfTarget)
            {
                // weapon
                weapon.TryProcItem(attacker, target, selfTarget);
            }

            if (attacker != this && attacker.HasProc && attacker.ProcSpellSelfTargeted == selfTarget)
            {
                // handle special case -- missile projectiles from monsters w/ a proc directly on the mob
                // monster
                attacker.TryProcItem(attacker, target, selfTarget);
            }

            // handle aetheria/jewelry procs
            if (attacker is Creature wielder)
            {
                var equippedAetheria = wielder.EquippedObjects.Values.Where(i => Aetheria.IsAetheria(i.WeenieClassId) && i.HasProc && i.ProcSpellSelfTargeted == selfTarget);

                var equippedProcJewelry = wielder.EquippedObjects.Values.Where(i => i.ItemType == ItemType.Jewelry && i.HasProc);

                // aetheria
                foreach (var aetheria in equippedAetheria)
                    aetheria.TryProcItem(attacker, target, selfTarget);

                // jewelry
                foreach (var jewelry in equippedProcJewelry)
                {
                    if (attacker is Player)
                    {
                        var player = attacker as Player;
                        if (jewelry.ItemDifficulty != null && player.GetCreatureSkill(Skill.ArcaneLore).Current > jewelry.ItemDifficulty)
                        {
                            //Console.WriteLine($"equippedProcJewelry: {jewelry.Name}");
                            jewelry.TryProcItem(attacker, target, selfTarget);
                        }
                    }
                }
            }


        }
    }
}
