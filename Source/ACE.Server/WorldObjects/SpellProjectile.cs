using ACE.Common;
using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Entity.Models;
using ACE.Server.Entity;
using ACE.Server.Entity.Actions;
using ACE.Server.Factories;
using ACE.Server.Managers;
using ACE.Server.Network.GameEvent.Events;
using ACE.Server.Network.GameMessages.Messages;
using ACE.Server.WorldObjects.Entity;
using Lifestoned.DataModel.Shared;
using System;
using System.Collections.Generic;
using System.Numerics;
using DamageType = ACE.Entity.Enum.DamageType;
using Position = ACE.Entity.Position;

namespace ACE.Server.WorldObjects
{
    public class SpellProjectile : WorldObject
    {
        public Spell Spell;
        public ProjectileSpellType SpellType { get; set; }

        public Position SpawnPos { get; set; }
        public float DistanceToTarget { get; set; }
        public uint LifeProjectileDamage { get; set; }

        public PartialEvasion PartialEvasion;

        public int Strikethrough = 0;
        public int StrikethroughLimit = 3;
        public double StrikethroughChance = 0.5f;

        public List<uint> StrikethroughTargets = new List<uint>();

        public SpellProjectileInfo Info { get; set; }

        /// <summary>
        /// Only set to true when this spell was launched by using the built-in spell on a caster
        /// </summary>
        public bool IsWeaponSpell { get; set; }

        /// <summary>
        /// If a spell projectile is from a proc source,
        /// make sure there is no attempt to re-proc again when the spell projectile hits
        /// </summary>
        public bool FromProc { get; set; }

        public int DebugVelocity;

        /// <summary>
        /// A new biota be created taking all of its values from weenie.
        /// </summary>
        public SpellProjectile(Weenie weenie, ObjectGuid guid) : base(weenie, guid)
        {
            SetEphemeralValues();
        }

        /// <summary>
        /// Restore a WorldObject from the database.
        /// </summary>
        public SpellProjectile(Biota biota) : base(biota)
        {
            SetEphemeralValues();
        }

        private void SetEphemeralValues()
        {
            // Override weenie description defaults
            ValidLocations = null;
            DefaultScriptId = null;
        }

        /// <summary>
        /// Perfroms additional set up of the spell projectile based on the spell id or its derived type.
        /// </summary>
        public void Setup(Spell spell, ProjectileSpellType spellType)
        {
            Spell = spell;
            SpellType = spellType;

            InitPhysicsObj();

            // Runtime changes to default state
            ReportCollisions = true;
            Missile = true;
            AlignPath = true;
            PathClipped = true;
            IgnoreCollisions = false;

            // FIXME: use data here
            if (!Spell.Name.Equals("Rolling Death"))
                Ethereal = false;

            if (SpellType == ProjectileSpellType.Bolt || SpellType == ProjectileSpellType.Streak
                || SpellType == ProjectileSpellType.Arc || SpellType == ProjectileSpellType.Volley || SpellType == ProjectileSpellType.Blast
                || WeenieClassId == 7276 || WeenieClassId == 7277 || WeenieClassId == 7279 || WeenieClassId == 7280)
            {
                DefaultScriptId = (uint)PlayScript.ProjectileCollision;
                DefaultScriptIntensity = 1.0f;
            }

            // Some wall spells don't have scripted collisions
            if (WeenieClassId == 7278 || WeenieClassId == 7281 || WeenieClassId == 7282 || WeenieClassId == 23144)
            {
                ScriptedCollision = false;
            }

            AllowEdgeSlide = false;

            // No need to send an ObjScale of 1.0f over the wire since that is the default value
            if (ObjScale == 1.0f)
                ObjScale = null;

            if (SpellType == ProjectileSpellType.Ring)
            {
                if (spell.Id == 3818)
                {
                    DefaultScriptId = (uint)PlayScript.Explode;
                    DefaultScriptIntensity = 1.0f;
                    ScriptedCollision = true;
                }
                else
                {
                    ScriptedCollision = false;
                }
            }

            // Projectiles with RotationSpeed get omega values and "align path" turned off which
            // creates the nice swirling animation
            if ((RotationSpeed ?? 0) != 0)
            {
                AlignPath = false;
                PhysicsObj.Omega = new Vector3((float)(Math.PI * 2 * RotationSpeed), 0, 0);
            }
        }

        public static ProjectileSpellType GetProjectileSpellType(uint spellID)
        {
            var spell = new Spell(spellID);

            if (spell.Wcid == 0)
                return ProjectileSpellType.Undef;

            if (spell.NumProjectiles == 1)
            {
                if (spell.Category >= SpellCategory.AcidStreak && spell.Category <= SpellCategory.SlashingStreak ||
                         spell.Category == SpellCategory.NetherStreak || spell.Category == SpellCategory.Fireworks)
                    return ProjectileSpellType.Streak;

                else if (spell.NonTracking)
                    return ProjectileSpellType.Arc;

                else
                    return ProjectileSpellType.Bolt;
            }

            if (spell.Category >= SpellCategory.AcidRing && spell.Category <= SpellCategory.SlashingRing || spell.SpreadAngle == 360)
                return ProjectileSpellType.Ring;

            if (spell.Category >= SpellCategory.AcidBurst && spell.Category <= SpellCategory.SlashingBurst ||
                spell.Category == SpellCategory.NetherDamageOverTimeRaising3)
                return ProjectileSpellType.Blast;

            // 1481 - Flaming Missile Volley
            if (spell.Category >= SpellCategory.AcidVolley && spell.Category <= SpellCategory.BladeVolley || spell.Name.Contains("Volley"))
                return ProjectileSpellType.Volley;

            if (spell.Category >= SpellCategory.AcidWall && spell.Category <= SpellCategory.SlashingWall)
                return ProjectileSpellType.Wall;

            if (spell.Category >= SpellCategory.AcidStrike && spell.Category <= SpellCategory.SlashingStrike)
                return ProjectileSpellType.Strike;

            return ProjectileSpellType.Undef;
        }

        public float GetProjectileScriptIntensity(ProjectileSpellType spellType)
        {
            if (spellType == ProjectileSpellType.Wall)
            {
                return 0.4f;
            }
            if (spellType == ProjectileSpellType.Ring)
            {
                if (Spell.Level == 6 || Spell.Id == 3818)
                    return 0.4f;
                if (Spell.Level == 7)
                    return 1.0f;
            }

            // Bolt, Blast, Volley, Streak and Arc all seem to use this scale
            // TODO: should this be based on spell level, or power of first scarab?
            // ie. can this use Spell.Formula.ScarabScale?
            switch (Spell.Level)
            {
                case 1:
                    return 0f;
                case 2:
                    return 0.2f;
                case 3:
                    return 0.4f;
                case 4:
                    return 0.6f;
                case 5:
                    return 0.8f;
                case 6:
                case 7:
                case 8:
                    return 1.0f;
                default:
                    return 0f;
            }
        }

        public bool WorldEntryCollision { get; set; }

        public void ProjectileImpact()
        {
            //Console.WriteLine($"{Name}.ProjectileImpact()");

            ReportCollisions = false;
            Ethereal = true;
            IgnoreCollisions = true;
            NoDraw = true;
            Cloaked = true;
            LightsStatus = false;

            PhysicsObj.set_active(false);

            if (PhysicsObj.entering_world)
            {
                // this path should only happen if spell_projectile_ethereal = false
                EnqueueBroadcast(new GameMessageScript(Guid, PlayScript.Launch, GetProjectileScriptIntensity(SpellType)));
                WorldEntryCollision = true;
            }

            EnqueueBroadcast(new GameMessageSetState(this, PhysicsObj.State));
            EnqueueBroadcast(new GameMessageScript(Guid, PlayScript.Explode, GetProjectileScriptIntensity(SpellType)));

            // this should only be needed for spell_projectile_ethereal = true,
            // however it can also fix a display issue on client in default mode,
            // where GameMessageSetState updates projectile to ethereal before it has actually collided on client,
            // causing a 'ghost' projectile to continue to sail through the target

            PhysicsObj.Velocity = Vector3.Zero;
            EnqueueBroadcast(new GameMessageVectorUpdate(this));

            ActionChain selfDestructChain = new ActionChain();
            selfDestructChain.AddDelaySeconds(5.0);
            selfDestructChain.AddAction(this, () => Destroy());
            selfDestructChain.EnqueueChain();
        }

        /// <summary>
        /// Handles collision with scenery or other static objects that would block a projectile from reaching its target,
        /// in which case the projectile should be removed with no further processing.
        /// </summary>
        public override void OnCollideEnvironment()
        {
            //Console.WriteLine($"{Name}.OnCollideEnvironment()");

            if (Info != null && ProjectileSource is Player player && player.DebugSpell)
            {
                player.Session.Network.EnqueueSend(new GameMessageSystemChat($"{Name}.OnCollideEnvironment()", ChatMessageType.Broadcast));
                player.Session.Network.EnqueueSend(new GameMessageSystemChat(Info.ToString(), ChatMessageType.Broadcast));
            }

            ProjectileImpact();
        }

        public override void OnCollideObject(WorldObject target)
        {
            //Console.WriteLine($"{Name}.OnCollideObject({target.Name})");

            if (StrikethroughTargets.Contains(target.Guid.Full)) return;

            var player = ProjectileSource as Player;

            if (Info != null && player != null && player.DebugSpell)
            {
                player.Session.Network.EnqueueSend(new GameMessageSystemChat($"{Name}.OnCollideObject({target?.Name} ({target?.Guid}))", ChatMessageType.Broadcast));
                player.Session.Network.EnqueueSend(new GameMessageSystemChat(Info.ToString(), ChatMessageType.Broadcast));
            }

            var spellType = GetProjectileSpellType(Spell.Id);
            if (spellType != ProjectileSpellType.Volley)
                ProjectileImpact();
            if (spellType == ProjectileSpellType.Volley && Strikethrough == StrikethroughLimit || ThreadSafeRandom.Next(0.0f, 1.0f) < StrikethroughChance)
                ProjectileImpact();

            // ensure valid creature target
            var creatureTarget = target as Creature;
            if (creatureTarget == null || target == ProjectileSource)
                return;

            if (player != null)
                player.LastHitSpellProjectile = Spell;
            
            // ensure caster can damage target
            var sourceCreature = ProjectileSource as Creature;
            if (sourceCreature != null && !sourceCreature.CanDamage(creatureTarget))
                return;

            // if player target, ensure matching PK status
            var targetPlayer = creatureTarget as Player;

            var pkError = ProjectileSource?.CheckPKStatusVsTarget(creatureTarget, Spell);
            if (pkError != null)
            {
                if (player != null)
                    player.Session.Network.EnqueueSend(new GameEventWeenieErrorWithString(player.Session, pkError[0], creatureTarget.Name));

                if (targetPlayer != null)
                    targetPlayer.Session.Network.EnqueueSend(new GameEventWeenieErrorWithString(targetPlayer.Session, pkError[1], ProjectileSource.Name));

                return;
            }

            var critical = false;
            var critDefended = false;
            var overpower = false;
            var resisted = false;

            var damage = CalculateDamage(ProjectileSource, creatureTarget, ref critical, ref critDefended, ref overpower, ref resisted);

            creatureTarget.OnAttackReceived(sourceCreature, CombatType.Magic, critical, resisted, (int)Spell.Level);

            if (damage != null)
            {
                // LEVEL SCALING - If player has Scaling Spell, check to ensure their level is greater than the monster in question, then scale their damage done/damage taken if so
                float levelScalingMod = 1f;

                if (player != null && player.EnchantmentManager.HasSpell(5379) && player.Level.HasValue && creatureTarget.Level.HasValue && player.Level > creatureTarget.Level)
                    levelScalingMod = Creature.GetPlayerDamageScaler((int)player.Level, (int)creatureTarget.Level);

                if (targetPlayer != null && targetPlayer.EnchantmentManager.HasSpell(5379) && targetPlayer.Level.HasValue && sourceCreature.Level.HasValue && targetPlayer.Level > sourceCreature.Level)
                    levelScalingMod = Creature.GetMonsterDamageScaler((int)targetPlayer.Level, (int)sourceCreature.Level);

                damage *= levelScalingMod;

                if (Spell.MetaSpellType == ACE.Entity.Enum.SpellType.EnchantmentProjectile)
                {
                    // handle EnchantmentProjectile successfully landing on target
                    ProjectileSource.CreateEnchantment(creatureTarget, ProjectileSource, ProjectileLauncher, Spell, false, FromProc);
                }
                else 
                {
                    DamageTarget(creatureTarget, damage.Value, critical, critDefended, overpower);
                }

                Strikethrough++;

                if (creatureTarget != null)
                    StrikethroughTargets.Add(creatureTarget.Guid.Full);

                // if this SpellProjectile has a TargetEffect, play it on successful hit
                DoSpellEffects(Spell, ProjectileSource, creatureTarget, true);

                if (player != null)
                    Proficiency.OnSuccessUse(player, player.GetCreatureSkill(Spell.School), Spell.PowerMod);

                // handle target procs
                // note that for untargeted multi-projectile spells,
                // ProjectileTarget will be null here, so procs will not apply

                // TODO: instead of ProjectileLauncher is Caster, perhaps a SpellProjectile.CanProc bool that defaults to true,
                // but is set to false if the source of a spell is from a proc, to prevent multi procs?

                // EMPOWERED SCARAB - Detonation Check for Cast-On-Strike
                if (player != null && FromProc)
                {
                    player.CheckForEmpoweredScarabOnCastEffects(target, Spell, true, creatureTarget);
                }

                if (sourceCreature != null && ProjectileTarget != null && !FromProc)
                {
                    // TODO figure out why cross-landblock group operations are happening here. We shouldn't need this code Mag-nus 2021-02-09
                    bool threadSafe = true;

                    if (LandblockManager.CurrentlyTickingLandblockGroupsMultiThreaded)
                    {
                        // Ok... if we got here, we're likely in the parallel landblock physics processing.
                        if (sourceCreature.CurrentLandblock == null || creatureTarget.CurrentLandblock == null || sourceCreature.CurrentLandblock.CurrentLandblockGroup != creatureTarget.CurrentLandblock.CurrentLandblockGroup)
                            threadSafe = false;
                    }

                    if (threadSafe)
                    {
                        // This can result in spell projectiles being added to either sourceCreature or creatureTargets landblock.
                        sourceCreature.TryProcEquippedItems(sourceCreature, creatureTarget, false, ProjectileLauncher);

                        // EMPOWERED SCARAB - Detonate
                        if (player != null)
                            player.CheckForEmpoweredScarabOnCastEffects(target, Spell, false, creatureTarget);
                    }
                    else
                    {
                        // sourceCreature and creatureTarget are now in different landblock groups.
                        // What has likely happened is that sourceCreature sent a projectile toward creatureTarget. Before impact, sourceCreature was teleported away.
                        // To perform this fully thread safe, we would enqueue the work onto worldManager.
                        // WorldManager.EnqueueAction(new ActionEventDelegate(() => sourceCreature.TryProcEquippedItems(creatureTarget, false)));
                        // But, to keep it simple, we will just ignore it and not bother with TryProcEquippedItems for this particular impact.
                    }
                }
            }

            // also called on resist
            if (player != null && targetPlayer == null)
                player.OnAttackMonster(creatureTarget);

            if (player == null && targetPlayer == null)
            {
                // check for faction combat
                if (sourceCreature != null && creatureTarget != null && (sourceCreature.AllowFactionCombat(creatureTarget) || sourceCreature.PotentialFoe(creatureTarget)))
                    sourceCreature.MonsterOnAttackMonster(creatureTarget);
            }
        }

        /// <summary>
        /// Calculates the damage for a spell projectile
        /// Used by war magic, void magic, and life magic projectiles
        /// </summary>
        public float? CalculateDamage(WorldObject source, Creature target, ref bool criticalHit, ref bool critDefended, ref bool overpower, ref bool resisted)
        {
            var sourcePlayer = source as Player;
            var targetPlayer = target as Player;

            if (source == null || !target.IsAlive || targetPlayer != null && targetPlayer.Invincible)
                return null;

            // check lifestone protection
            if (targetPlayer != null && targetPlayer.UnderLifestoneProtection)
            {
                if (sourcePlayer != null)
                    sourcePlayer.Session.Network.EnqueueSend(new GameMessageSystemChat($"The Lifestone's magic protects {targetPlayer.Name} from the attack!", ChatMessageType.Magic));

                targetPlayer.HandleLifestoneProtection();
                return null;
            }

            var critDamageBonus = 0.0f;
            var criticalDamageMod = 1.0f;
            var weaponCritDamageMod = 1.0f;
            var weaponResistanceMod = 1.0f;
            var resistanceMod = 1.0f;

            // life magic
            var lifeMagicDamage = 0.0f;

            // war/void magic
            var baseDamage = 0;
            var skillBonus = 0.0f;
           
            var finalDamage = 0.0f;

            var resistanceType = Creature.GetResistanceType(Spell.DamageType);

            var sourceCreature = source as Creature;
            if (sourceCreature?.Overpower != null)
                overpower = Creature.GetOverpower(sourceCreature, target);

            var weapon = ProjectileLauncher;

            var resistSource = IsWeaponSpell ? weapon : source;

            resisted = source.TryResistSpell(target, Spell, out PartialEvasion partialEvasion, resistSource, true);

            // Combat Ability - Reflect (reflect resisted spells back to the caster)
            if (resisted && targetPlayer != null && sourceCreature != null)
            {
                if (targetPlayer.EquippedCombatAbility == CombatAbility.Reflect)
                {
                    targetPlayer.TryCastSpell(Spell, sourceCreature, null, null, false, false, false, true);
                }
            }

            var resistedMod = 1.0f;
            PartialEvasion = partialEvasion;

            if (!overpower && sourcePlayer != null)
            {
                if (GetResistedMod(out resistedMod))
                    return null;
            }
            else
            {
                if (resisted && !overpower)
                    return null;
            }

            CreatureSkill attackSkill = null;
            if (sourceCreature != null)
                attackSkill = sourceCreature.GetCreatureSkill(Spell.School);

            // critical hit
            var criticalChance = GetWeaponMagicCritFrequency(weapon, sourceCreature, attackSkill, target);

            // SPEC BONUS - War Magic (Scepter): +5% crit chance (additively)
            if (sourcePlayer != null && weapon != null)
                if (weapon.WeaponSkill == Skill.WarMagic && sourcePlayer.GetCreatureSkill(Skill.WarMagic).AdvancementClass == SkillAdvancementClass.Specialized && LootGenerationFactory.GetCasterSubType(weapon) == 1)
                    criticalChance += 0.05f;

            // Iron Fist Crit Rate Bonus
            if (sourcePlayer != null)
            {
                if (sourcePlayer.EquippedCombatAbility == CombatAbility.IronFist)
                {
                    if (sourcePlayer.LastIronFistActivated > Time.GetUnixTime() - sourcePlayer.IronFistActivatedDuration)
                        criticalChance += 0.25f;
                    else
                        criticalChance += 0.1f;
                }
            }
            // Jewelcrafting Reprisal Bonus - Autocrit

            if (sourcePlayer != null)
            {
                if (sourcePlayer.GetEquippedItemsRatingSum(PropertyInt.GearReprisal) > 0)
                {
                    if (sourcePlayer.QuestManager.HasQuest($"{target.Guid}/Reprisal"))
                    {
                        criticalChance = 1f;
                        sourcePlayer.QuestManager.Erase($"{target.Guid}/Reprisal");
                    }
                }
            }

            if (ThreadSafeRandom.Next(0.0f, 1.0f) < criticalChance)
            {
                if (targetPlayer != null && targetPlayer.AugmentationCriticalDefense > 0)
                {
                    var criticalDefenseMod = sourcePlayer != null ? 0.05f : 0.25f;
                    var criticalDefenseChance = targetPlayer.AugmentationCriticalDefense * criticalDefenseMod;

                    if (criticalDefenseChance > ThreadSafeRandom.Next(0.0f, 1.0f))
                        critDefended = true;
                }

                var perceptionDefended = false;
                // SPEC BONUS: Perception - 50% chance to prevent a critical hit
                if (targetPlayer != null)
                {
                    var perception = targetPlayer.GetCreatureSkill(Skill.AssessCreature);
                    if (perception.AdvancementClass == SkillAdvancementClass.Specialized)
                    {
                        var skillCheck = (float)perception.Current / (float)attackSkill.Current;
                        var criticalDefenseChance = skillCheck > 1f ? 0.5f : skillCheck * 0.5f;

                        if (criticalDefenseChance > ThreadSafeRandom.Next(0f, 1f))
                        {
                            perceptionDefended = true;
                            targetPlayer.Session.Network.EnqueueSend(new GameMessageSystemChat($"Your perception skill allowed you to prevent a critical strike!", ChatMessageType.Magic));
                        }
                    }
                }

                if (!critDefended && perceptionDefended == false)
                    criticalHit = true;

                // Jewelcrafting Reprisal-- Chance to resist an incoming critical
                if (criticalHit)
                {
                    if (targetPlayer != null && sourceCreature != null)
                    {
                        if (targetPlayer.GetEquippedItemsRatingSum(PropertyInt.GearReprisal) > 0)
                        {
                            if ((targetPlayer.GetEquippedItemsRatingSum(PropertyInt.GearReprisal) / 2) >= ThreadSafeRandom.Next(0, 100))
                            {
                                targetPlayer.QuestManager.HandleReprisalQuest();
                                targetPlayer.QuestManager.Stamp($"{sourceCreature.Guid}/Reprisal");
                                resisted = true;
                                targetPlayer.Reprisal = true;
                                PartialEvasion = PartialEvasion.All;
                                targetPlayer.SendChatMessage(this, $"Reprisal! You resist the spell cast by {sourceCreature.Name}.", ChatMessageType.Magic);
                                targetPlayer.Session.Network.EnqueueSend(new GameMessageSound(targetPlayer.Guid, Sound.ResistSpell, 1.0f));
                                return null;
                            }
                        }
                    }
                }

                // EMPOWERED SCARAB - Crushing
                if (criticalHit && sourcePlayer != null && Spell.School == MagicSchool.WarMagic)
                    sourcePlayer.CheckForEmpoweredScarabOnCastEffects(target, Spell, false, null, true);
            }

            // ward mod, rend, and penetration
            var wardRendingMod = 1.0f;
            if (weapon != null && weapon.HasImbuedEffect(ImbuedEffectType.WardRending))
                wardRendingMod = 1.0f - GetWardRendingMod(attackSkill);

            var wardPenMod = 0.0f;

            if(sourcePlayer != null)
                wardPenMod = sourcePlayer.GetIgnoreWardMod(weapon);

            var ignoreWardMod = Math.Min(wardRendingMod, wardPenMod);

            // JEWEL - Tourmaline: Ramping Ward Pen
            if (sourcePlayer != null)
            {
                if (sourcePlayer.GetEquippedItemsRatingSum(PropertyInt.GearWardPen) > 0)
                {
                    var jewelWardPenMod = (float)target.QuestManager.GetCurrentSolves($"{sourcePlayer.Name},WardPen") / 500;
                    jewelWardPenMod *= ((float)sourcePlayer.GetEquippedItemsRatingSum(PropertyInt.GearWardPen) / 66);
                    ignoreWardMod -= jewelWardPenMod;
                }
            }

            // SPEC BONUS - War Magic (Orb): +10% ward penetration (additively)
            if (sourcePlayer != null && weapon != null)
                if (weapon.WeaponSkill == Skill.WarMagic && sourcePlayer.GetCreatureSkill(Skill.WarMagic).AdvancementClass == SkillAdvancementClass.Specialized && LootGenerationFactory.GetCasterSubType(weapon) == 0)
                    ignoreWardMod -= 0.1f;

            var wardMod = GetWardMod(target, ignoreWardMod);

            //Console.WriteLine($"TargetWard: {target.WardLevel} WardRend: {wardRendingMod} Nullification: {NullificationMod} WardMod: {wardMod}");

            // absorb mod
            bool isPVP = sourcePlayer != null && targetPlayer != null;
            var absorbMod = GetAbsorbMod(target, this);

            // JEWEL - Amethyst: Ramping Magic Absorb
            if (targetPlayer != null)
            {
                if (targetPlayer.GetEquippedItemsRatingSum(PropertyInt.GearNullification) > 0)
                {
                    var jewelRampMod = (float)targetPlayer.QuestManager.GetCurrentSolves($"{targetPlayer.Name},Nullification") / 200;
                    absorbMod -= jewelRampMod * ((float)targetPlayer.GetEquippedItemsRatingSum(PropertyInt.GearNullification) / 66);
                }
            } 

            //http://acpedia.org/wiki/Announcements_-_2014/01_-_Forces_of_Nature - Aegis is 72% effective in PvP
            if (isPVP && (target.CombatMode == CombatMode.Melee || target.CombatMode == CombatMode.Missile))
            {
                absorbMod = 1 - absorbMod;
                absorbMod *= 0.72f;
                absorbMod = 1 - absorbMod;
            }

            if (isPVP && Spell.IsHarmful)
                Player.UpdatePKTimers(sourcePlayer, targetPlayer);

            var attributeMod = 1f;
            if (sourcePlayer != null)
                attributeMod = sourcePlayer.GetAttributeMod(weapon, true);

            var elementalDamageMod = GetCasterElementalDamageModifier(weapon, sourceCreature, target, Spell.DamageType);

            // Possible 2x + damage bonus for the slayer property
            var slayerMod = GetWeaponCreatureSlayerModifier(weapon, sourceCreature, target);

            // COMBAT ABILITY - Overload (x1.5) and Battery (scaling penalty with mana lost, 0 penalty when activated)
            var combatFocusDamageMod = 1.0f;
            if (sourcePlayer != null)
            {
                // Overload - Increased effectiveness up to 50%+ with Overload stacks, double bonus + erase stacks on activated ability
                if (sourcePlayer.EquippedCombatAbility == CombatAbility.Overload && sourcePlayer.QuestManager.HasQuest($"{sourcePlayer.Name},Overload"))
                {
                    if (sourcePlayer.OverloadActivated == false)
                    {
                        var overloadStacks = sourcePlayer.QuestManager.GetCurrentSolves($"{sourcePlayer.Name},Overload");
                        var overloadMod = (float)overloadStacks / 2000;
                        combatFocusDamageMod += overloadMod;
                    }
                    if (sourcePlayer.OverloadActivated && sourcePlayer.LastOverloadActivated > Time.GetUnixTime() - sourcePlayer.OverloadActivatedDuration)
                    {
                        sourcePlayer.OverloadActivated = false;
                        var overloadStacks = sourcePlayer.QuestManager.GetCurrentSolves($"{sourcePlayer.Name},Overload");
                        var overloadMod = (float)overloadStacks / 500;
                        combatFocusDamageMod += overloadMod;
                        sourcePlayer.QuestManager.Erase($"{sourcePlayer.Name},Overload");
                    }
                    // reset if player didn't cast overload discharge within ten sec
                    if (sourcePlayer.OverloadActivated && sourcePlayer.LastOverloadActivated < Time.GetUnixTime() - sourcePlayer.OverloadActivatedDuration)
                    {
                        sourcePlayer.OverloadActivated = false;
                        sourcePlayer.QuestManager.Erase($"{sourcePlayer.Name},Overload");
                    }
                }
                else if (sourcePlayer.EquippedCombatAbility == CombatAbility.Battery && sourcePlayer.LastBatteryActivated < Time.GetUnixTime() - sourcePlayer.BatteryActivatedDuration)
                {
                    var maxMana = (float)sourcePlayer.Mana.MaxValue;
                    var currentMana = (float)sourcePlayer.Mana.Current == 0 ? 1 : (float)sourcePlayer.Mana.Current;

                    if ((currentMana / maxMana) > 0.75)
                        combatFocusDamageMod = 1f;
                    else
                    {
                        var newMax = maxMana * 0.75;
                        var manaMod = 1f - 0.25f * ((newMax - currentMana) / newMax);
                        combatFocusDamageMod -= (float)manaMod;
                    }
                }
                else if (sourcePlayer.EquippedCombatAbility == CombatAbility.EnchantedWeapon && sourcePlayer.LastEnchantedWeaponActivated > Time.GetUnixTime() - sourcePlayer.EnchantedWeaponActivatedDuration)
                {
                    if (sourcePlayer.GetEquippedMeleeWeapon != null || sourcePlayer.GetEquippedMissileLauncher != null || sourcePlayer.GetEquippedMissileWeapon != null)
                        combatFocusDamageMod += 0.25f;
                }
            }

            // SPEC BONUS - Magic Defense
            var specDefenseMod = 1.0f;
            if (targetPlayer != null && targetPlayer.GetCreatureSkill(Skill.MagicDefense).AdvancementClass == SkillAdvancementClass.Specialized)
            {
                var magicDefenseSkill = targetPlayer.GetCreatureSkill(Skill.MagicDefense);
                var bonusAmount = (float)Math.Min(magicDefenseSkill.Current, 500) / 50;

                specDefenseMod = 0.9f - bonusAmount * 0.01f;
            }

            // life magic projectiles: ie., martyr's hecatomb
            if (Spell.MetaSpellType == ACE.Entity.Enum.SpellType.LifeProjectile)
            {
                lifeMagicDamage = LifeProjectileDamage * Spell.DamageRatio * combatFocusDamageMod;

                // could life magic projectiles crit?
                // if so, did they use the same 1.5x formula as war magic, instead of 2.0x?
                if (criticalHit)
                {
                    // verify: CriticalMultiplier only applied to the additional crit damage,
                    // whereas CD/CDR applied to the total damage (base damage + additional crit damage)
                    weaponCritDamageMod = GetWeaponCritDamageMod(weapon, sourceCreature, attackSkill, target);

                    criticalDamageMod = 2.0f + weaponCritDamageMod;

                    if (sourcePlayer != null && sourcePlayer.EquippedCombatAbility == CombatAbility.IronFist)
                        criticalDamageMod -= 0.2f;
                }

                weaponResistanceMod = GetWeaponResistanceModifier(weapon, sourceCreature, attackSkill, Spell.DamageType);

                // if attacker/weapon has IgnoreMagicResist directly, do not transfer to spell projectile
                // only pass if SpellProjectile has it directly, such as 2637 - Invoking Aun Tanua

                resistanceMod = (float)Math.Max(0.0f, target.GetResistanceMod(resistanceType, this, null, weaponResistanceMod));

                finalDamage = lifeMagicDamage * criticalDamageMod * elementalDamageMod * slayerMod * attributeMod * resistanceMod * absorbMod * wardMod * resistedMod * specDefenseMod;
            }
            // war/void magic projectiles
            else
            {
                if (criticalHit)
                {
                    // Original:
                    // http://acpedia.org/wiki/Announcements_-_2002/08_-_Atonement#Letter_to_the_Players

                    // Critical Strikes: In addition to the skill-based damage bonus, each projectile spell has a 2% chance of causing a critical hit on the target and doing increased damage.
                    // A magical critical hit is similar in some respects to melee critical hits (although the damage calculation is handled differently).
                    // While a melee critical hit automatically does twice the maximum damage of the weapon, a magical critical hit will do an additional half the minimum damage of the spell.
                    // For instance, a magical critical hit from a level 7 spell, which does 110-180 points of damage, would add an additional 55 points of damage to the spell.

                    // Later updated for PvE only:

                    // http://acpedia.org/wiki/Announcements_-_2004/07_-_Treaties_in_Stone#Letter_to_the_Players

                    // Currently when a War Magic spell scores a critical hit, it adds a multiple of the base damage of the spell to a normal damage roll.
                    // Starting in July, War Magic critical hits will instead add a multiple of the maximum damage of the spell.
                    // No more crits that do less damage than non-crits!

                    if (isPVP) // PvP: 50% of the MIN damage added to normal damage roll
                        critDamageBonus = Spell.MinDamage * 0.5f;
                    else   // PvE: 50% of the MAX damage added to normal damage roll
                        critDamageBonus = Spell.MaxDamage * 0.5f;

                    // verify: CriticalMultiplier only applied to the additional crit damage,
                    // whereas CD/CDR applied to the total damage (base damage + additional crit damage)
                    weaponCritDamageMod = GetWeaponCritDamageMod(weapon, sourceCreature, attackSkill, target);

                    critDamageBonus *= weaponCritDamageMod;

                    // SPEC BONUS - War Magic (Wand/Baton): +50% crit damage (additively)
                    if(sourcePlayer != null && weapon != null)
                        if (weapon.WeaponSkill == Skill.WarMagic && sourcePlayer.GetCreatureSkill(Skill.WarMagic).AdvancementClass == SkillAdvancementClass.Specialized && LootGenerationFactory.GetCasterSubType(weapon) == 2)
                            weaponCritDamageMod += 0.5f;

                    criticalDamageMod = 2.0f + weaponCritDamageMod;

                    if (sourcePlayer != null && sourcePlayer.EquippedCombatAbility == CombatAbility.IronFist)
                        criticalDamageMod -= 0.2f;
                }

                baseDamage = ThreadSafeRandom.Next(Spell.MinDamage, Spell.MaxDamage);

                weaponResistanceMod = GetWeaponResistanceModifier(weapon, sourceCreature, attackSkill, Spell.DamageType);

                // if attacker/weapon has IgnoreMagicResist directly, do not transfer to spell projectile
                // only pass if SpellProjectile has it directly, such as 2637 - Invoking Aun Tanua

                resistanceMod = (float)Math.Max(0.0f, target.GetResistanceMod(resistanceType, this, null, weaponResistanceMod));

                if (sourcePlayer != null && targetPlayer != null && Spell.DamageType == ACE.Entity.Enum.DamageType.Nether)
                {
                    // for direct damage from void spells in pvp,
                    // apply void_pvp_modifier *on top of* the player's natural resistance to nether

                    // this supposedly brings the direct damage from void spells in pvp closer to retail
                    resistanceMod *= (float)PropertyManager.GetDouble("void_pvp_modifier").Item;
                }


                // ----- Jewelcrafting Protection -----

                float jewelcraftingProtection = 1f;

                if (targetPlayer != null)
                {   // JEWEL - Onyx: Protection vs. Slash/Pierce/Bludgeon
                    if (Spell.DamageType == DamageType.Slash || Spell.DamageType == DamageType.Pierce || Spell.DamageType == DamageType.Bludgeon)
                    {
                        if (targetPlayer.GetEquippedItemsRatingSum(PropertyInt.GearPhysicalWard) > 0)
                            jewelcraftingProtection = (1 - ((float)targetPlayer.GetEquippedItemsRatingSum(PropertyInt.GearPhysicalWard) / 100));
                    }
                    // JEWEL - Zircon: Protection vs. Acid/Fire/Cold/Electric
                    if (Spell.DamageType == DamageType.Acid || Spell.DamageType == DamageType.Fire || Spell.DamageType == DamageType.Cold || Spell.DamageType == DamageType.Electric)
                    {
                        if (targetPlayer.GetEquippedItemsRatingSum(PropertyInt.GearElementalWard) > 0)
                            jewelcraftingProtection = (1 - ((float)targetPlayer.GetEquippedItemsRatingSum(PropertyInt.GearElementalWard) / 100));
                    }
                }
                var jewelElementalist = 1f;
                var jewelElemental = 1f;
                var jewelLastStand = 1f;
                var jewelSelfHarm = 1f;

                if (sourcePlayer != null)
                {   // JEWEL - Green Garnet: Ramping War Magic Damage
                    if (sourcePlayer.GetEquippedItemsRatingSum(PropertyInt.GearElementalist) > 0)
                    {
                        var jewelRampMod = (float)sourcePlayer.QuestManager.GetCurrentSolves($"{sourcePlayer.Name},Elementalist") / 500;
                        jewelElementalist += jewelRampMod * ((float)sourcePlayer.GetEquippedItemsRatingSum(PropertyInt.GearElementalist) / 66);
                    }
                    // JEWEL - White Sapphire: Ramping Bludgeon Crit Damage Bonus
                    if (sourcePlayer.GetEquippedItemsRatingSum(PropertyInt.GearBludgeon) > 0)
                    {
                        if (Spell.DamageType == DamageType.Bludgeon)
                        {
                            var jewelRampMod = (float)target.QuestManager.GetCurrentSolves($"{sourcePlayer.Name},Bludgeon") / 500;
                            critDamageBonus *= jewelRampMod * ((float)sourcePlayer.GetEquippedItemsRatingSum(PropertyInt.GearBludgeon) / 50);
                        }

                    }
                    // JEWEL - Black Garnet - Ramping Piercing Resistance Penetration
                    if (sourcePlayer.GetEquippedItemsRatingSum(PropertyInt.GearPierce) > 0)
                    {
                        if (Spell.DamageType == DamageType.Pierce)
                        {
                            var jewelRampMod = (float)target.QuestManager.GetCurrentSolves($"{sourcePlayer.Name},Pierce") / 500;
                            resistanceMod += jewelRampMod * ((float)sourcePlayer.GetEquippedItemsRatingSum(PropertyInt.GearPierce) / 66);
                        }
                    }
                    // JEWEL - Hematite: Deal bonus damage but take the same amount
                    if (sourcePlayer.GetEquippedItemsRatingSum(PropertyInt.GearSelfHarm) > 0)
                        jewelSelfHarm += (float)(sourcePlayer.GetEquippedItemsRatingSum(PropertyInt.GearSelfHarm) / 100);

                    // JEWEL - Aquamarine, Emerald, Jet, Red Garnet: Bonus elemental damage
                    jewelElemental = Jewel.HandleElementalBonuses(sourcePlayer, Spell.DamageType);

                    // JEWEL - Ruby: Bonus damage below 50% HP, reduced damage above
                    if (sourcePlayer.GetEquippedItemsRatingSum(PropertyInt.GearLastStand) > 0)
                        jewelLastStand += Jewel.GetJewelLastStand(sourcePlayer, target);
                }

                var strikethroughMod = 1f / (Strikethrough + 1);

                // ----- FINAL CALCULATION ------------
                var damageBeforeMitigation = baseDamage * criticalDamageMod * attributeMod * elementalDamageMod * slayerMod * combatFocusDamageMod * jewelElementalist * jewelElemental * jewelSelfHarm * jewelLastStand * strikethroughMod;

                finalDamage = damageBeforeMitigation * absorbMod * wardMod * resistanceMod * resistedMod * specDefenseMod * jewelcraftingProtection;

                if (sourcePlayer != null)
                {
                    //Console.WriteLine($"\n{sourcePlayer.Name} casted {Spell.Name} on {target.Name} for {Math.Round(finalDamage, 0)}.\n" +
                    //    $" -baseDamage: {baseDamage}\n" +
                    //    $" -critMultiplier: {criticalDamageMod}\n" +
                    //    $" -attributeMod: {attributeMod}\n" +
                    //    $" -elementalDamageMod: {elementalDamageMod}\n" +
                    //    $" -slayerMod: {slayerMod}\n" +
                    //    $" -absorbMod: {absorbMod}\n" +
                    //    $" -wardMod: {wardMod}\n" +
                    //    $" -resistanceMod: {resistanceMod}\n" +
                    //    $" -resistedMod: {resistedMod}\n" +
                    //    $" -specDefMod: {specDefenseMod}\n" +
                    //    $" -FinalBeforeRatings: {finalDamage}");
                }
            }


            // show debug info
            if (sourceCreature != null && sourceCreature.DebugDamage.HasFlag(Creature.DebugDamageType.Attacker))
            {
                ShowInfo(sourceCreature, Spell, attackSkill, criticalChance, criticalHit, critDefended, overpower, weaponCritDamageMod, skillBonus, baseDamage, critDamageBonus, elementalDamageMod, slayerMod, weaponResistanceMod, resistanceMod, absorbMod, LifeProjectileDamage, lifeMagicDamage, finalDamage);
            }
            if (target.DebugDamage.HasFlag(Creature.DebugDamageType.Defender))
            {
                ShowInfo(target, Spell, attackSkill, criticalChance, criticalHit, critDefended, overpower, weaponCritDamageMod, skillBonus, baseDamage, critDamageBonus, elementalDamageMod, slayerMod, weaponResistanceMod, resistanceMod, absorbMod, LifeProjectileDamage, lifeMagicDamage, finalDamage);
            }
            return finalDamage;
        }

        public float GetAbsorbMod(Creature target, WorldObject source)
        {
            switch (target.CombatMode)
            {
                case CombatMode.Melee:

                    // does target have shield equipped?
                    var shield = target.GetEquippedShield();
                    if (shield != null && shield.GetAbsorbMagicDamage() != null)
                        return GetShieldMod(target, shield, source);

                    break;

                case CombatMode.Missile:

                    var missileLauncherOrShield = target.GetEquippedMissileLauncher() ?? target.GetEquippedShield();
                    if (missileLauncherOrShield != null && missileLauncherOrShield.GetAbsorbMagicDamage() != null)
                        return AbsorbMagic(target, missileLauncherOrShield);

                    break;

                case CombatMode.Magic:

                    var caster = target.GetEquippedWand();
                    if (caster != null && caster.GetAbsorbMagicDamage() != null)
                        return AbsorbMagic(target, caster);

                    break;
            }
            return 1.0f;
        }

        public float GetWardMod(Creature target, float ignoreWardMod)
        {
            var wardLevel = target.GetWardLevel();
            //var wardLevel = target.Level * 5;

            return SkillFormula.CalcWardMod(wardLevel * ignoreWardMod);
        }

        /// <summary>
        /// Calculates the amount of damage a shield absorbs from magic projectile
        /// </summary>
        public static float GetShieldMod(Creature target, WorldObject shield, WorldObject source)
        {
            bool bypassShieldAngleCheck = false;

            // COMBAT ABILITY - Phalanx
            var combatAbilityTrinket = target.GetEquippedTrinket();
            if (combatAbilityTrinket != null && combatAbilityTrinket.CombatAbilityId == (int)CombatAbility.Phalanx)
                bypassShieldAngleCheck = true;

            if (!bypassShieldAngleCheck)
            {
                // is spell projectile in front of creature target,
                // within shield effectiveness area?
                var effectiveAngle = 180.0f;
                var angle = target.GetAngle(source);
                if (Math.Abs(angle) > effectiveAngle / 2.0f)
                    return 1.0f;
            }

            // https://asheron.fandom.com/wiki/Shield
            // The formula to determine magic absorption for shields is:
            // Reduction Percent = (cap * specMod * baseSkill * 0.003f) - (cap * specMod * 0.3f)
            // Cap = Maximum reduction
            // SpecMod = 1.0 for spec, 0.8 for trained
            // BaseSkill = 100 to 433 (above 433 base shield you always achieve the maximum %)

            var shieldSkill = target.GetCreatureSkill(Skill.Shield);
            // ensure trained?
            if (shieldSkill.AdvancementClass < SkillAdvancementClass.Trained || shieldSkill.Base < 100)
                return 1.0f;

            var baseSkill = Math.Min(shieldSkill.Base, 433);
            var specMod = shieldSkill.AdvancementClass == SkillAdvancementClass.Specialized ? 1.0f : 0.8f;
            var cap = (float)(shield.GetAbsorbMagicDamage() ?? 0.0f);

            // speced, 100 skill = 0%
            // trained, 100 skill = 0%
            // speced, 200 skill = 30%
            // trained, 200 skill = 24%
            // speced, 300 skill = 60%
            // trained, 300 skill = 48%
            // speced, 433 skill = 100%
            // trained, 433 skill = 80%

            var reduction = (cap * specMod * baseSkill * 0.003f) - (cap * specMod * 0.3f);

            var shieldMod = Math.Min(1.0f, 1.0f - reduction);
            return shieldMod;
        }

        /// <summary>
        /// Calculates the damage reduction modifier for bows and casters
        /// with 'Magic Absorbing' property
        /// </summary>
        public float AbsorbMagic(Creature target, WorldObject item)
        {
            // https://asheron.fandom.com/wiki/Category:Magic_Absorbing

            // Tomes and Bows
            // The formula to determine magic absorption for Tomes and the Fetish of the Dark Idols:
            // - For a 25% maximum item: (magic absorbing %) = 25 - (0.1 * (319 - base magic defense))
            // - For a 10% maximum item: (magic absorbing %) = 10 - (0.04 * (319 - base magic defense))

            // wiki currently has what is likely a typo for the 10% formula,
            // where it has a factor of 0.4 instead of 0.04
            // with 0.4, the 10% items would not start to become effective until base magic defense 294
            // with 0.04, both formulas start to become effective at base magic defense 69

            // using an equivalent formula that produces the correct results for 10% and 25%,
            // and also produces the correct results for any %

            var absorbMagicDamage = item.GetAbsorbMagicDamage();

            if (absorbMagicDamage == null)
                return 1.0f;

            var maxPercent = absorbMagicDamage.Value;

            var baseCap = 319;
            var magicDefBase = target.GetCreatureSkill(Skill.MagicDefense).Base;
            var diff = Math.Max(0, baseCap - magicDefBase);

            var percent = maxPercent - maxPercent * diff * 0.004f;

            return Math.Min(1.0f, 1.0f - (float)percent);
        }

        /// <summary>
        /// Called for a spell projectile to damage its target
        /// </summary>
        public void DamageTarget(Creature target, float damage, bool critical, bool critDefended, bool overpower)
        {
            var targetPlayer = target as Player;

            if (targetPlayer != null && targetPlayer.Invincible || target.IsDead)
                return;

            var sourceCreature = ProjectileSource as Creature;
            var sourcePlayer = ProjectileSource as Player;

            var pkBattle = sourcePlayer != null && targetPlayer != null;

            var amount = 0u;
            var percent = 0.0f;

            var damageRatingMod = 1.0f;
            var heritageMod = 1.0f;
            var sneakAttackMod = 1.0f;
            var critDamageRatingMod = 1.0f;
            var pkDamageRatingMod = 1.0f;

            var damageResistRatingMod = 1.0f;
            var critDamageResistRatingMod = 1.0f;
            var pkDamageResistRatingMod = 1.0f;

            WorldObject equippedCloak = null;

            // handle life projectiles for stamina / mana
            if (Spell.Category == SpellCategory.StaminaLowering)
            {
                percent = damage / target.Stamina.MaxValue;
                amount = (uint)-target.UpdateVitalDelta(target.Stamina, (int)-Math.Round(damage));
            }
            else if (Spell.Category == SpellCategory.ManaLowering)
            {
                percent = damage / target.Mana.MaxValue;
                amount = (uint)-target.UpdateVitalDelta(target.Mana, (int)-Math.Round(damage));
            }
            else
            {
                // for possibly applying sneak attack to magic projectiles,
                // only do this for health-damaging projectiles?
                if (sourcePlayer != null)
                {
                    // TODO: use target direction vs. projectile position, instead of player position
                    // could sneak attack be applied to void DoTs?
                    sneakAttackMod = sourcePlayer.GetSneakAttackMod(target, out var backstabMod);
                    //Console.WriteLine("Magic sneak attack:  + sneakAttackMod);
                    heritageMod = sourcePlayer.GetHeritageBonus(sourcePlayer.GetEquippedWand()) ? 1.05f : 1.0f;
                }
                // Calc sneak bonus for monsters
                if (targetPlayer != null && sourceCreature != null)
                    sneakAttackMod = sourceCreature.GetSneakAttackMod(targetPlayer, out var backstabMod);

                var damageRating = sourceCreature?.GetDamageRating() ?? 0;
                damageRatingMod = Creature.AdditiveCombine(Creature.GetPositiveRatingMod(damageRating), heritageMod, sneakAttackMod);

                damageResistRatingMod = target.GetDamageResistRatingMod(CombatType.Magic);

                if (critical)
                {
                    damageRatingMod = Creature.GetPositiveRatingMod(sourceCreature?.GetCritDamageRating() ?? 0);
                    damageResistRatingMod = Creature.GetNegativeRatingMod(target.GetCritDamageResistRating());
                }
                if (pkBattle)
                {
                    pkDamageRatingMod = Creature.GetPositiveRatingMod(sourceCreature?.GetPKDamageRating() ?? 0);
                    pkDamageResistRatingMod = Creature.GetNegativeRatingMod(target.GetPKDamageResistRating());

                    damageRatingMod = Creature.AdditiveCombine(damageRatingMod, pkDamageRatingMod);
                    damageResistRatingMod = Creature.AdditiveCombine(damageResistRatingMod, pkDamageResistRatingMod);
                }

                damage *= damageRatingMod * damageResistRatingMod;

                percent = damage / target.Health.MaxValue;

                //Console.WriteLine($"Damage rating: " + Creature.ModToRating(damageRatingMod));

                equippedCloak = target.EquippedCloak;

                if (equippedCloak != null && Cloak.HasDamageProc(equippedCloak) && Cloak.RollProc(equippedCloak, percent))
                {
                    var reducedDamage = Cloak.GetReducedAmount(ProjectileSource, damage);

                    Cloak.ShowMessage(target, ProjectileSource, damage, reducedDamage);

                    damage = reducedDamage;
                    percent = damage / target.Health.MaxValue;
                }

                // Mana Barrier 
                if (targetPlayer == null || !targetPlayer.ManaBarrierToggle)
                {
                    amount = (uint)-target.UpdateVitalDelta(target.Health, (int)-Math.Round(damage));
                    target.DamageHistory.Add(ProjectileSource, Spell.DamageType, amount);
                }
                if (targetPlayer != null && targetPlayer.ManaBarrierToggle)
                {
                    var toggles = targetPlayer.GetInventoryItemsOfWCID(1051110);
                    var skill = targetPlayer.GetCreatureSkill((Skill)16);

                    var expectedSkill = (float)(targetPlayer.Level * 5);
                    var currentSkill = (float)skill.Current;

                    // create a scaling mod. if expected skill is much higher than currentSkill, you will be multiplying the amount of mana damage singificantly, so low skill players will not get much benefit before bubble bursts.
                    // capped at 1f so high skill gets the proper ratio of health-to-mana, but no better than that.

                    var skillModifier = expectedSkill / currentSkill <= 1f ? 1f : expectedSkill / currentSkill;

                    var manaDamage = (damage * 0.25) * 3 * skillModifier;
                    if (skill.AdvancementClass == SkillAdvancementClass.Specialized)
                        manaDamage = (damage * 0.25) * 1.5 * skillModifier;

                    if (targetPlayer.ManaBarrierToggle && targetPlayer.Mana.Current >= manaDamage)
                    {
                        amount = (uint)(damage * 0.75);
                        PlayParticleEffect(PlayScript.RestrictionEffectBlue, Guid);
                        targetPlayer.UpdateVitalDelta(targetPlayer.Mana, (int)-Math.Round(manaDamage));
                        targetPlayer.UpdateVitalDelta(targetPlayer.Health, (int)-Math.Round((float)amount));
                        targetPlayer.DamageHistory.Add(ProjectileSource, Spell.DamageType, amount);
                    }
                    // if not enough mana, barrier falls and player takes remainder of damage as health
                    if (targetPlayer.ManaBarrierToggle && targetPlayer.Mana.Current < manaDamage)
                    {
                        targetPlayer.ToggleManaBarrierSetting();
                        targetPlayer.Session.Network.EnqueueSend(new GameMessageSystemChat($"Your mana barrier fails and collapses!", ChatMessageType.Magic));
                        if (toggles != null)
                        {
                            foreach (var toggle in toggles)
                                EnchantmentManager.StartCooldown(toggle);
                        }

                        PlayParticleEffect(PlayScript.HealthDownBlue, Guid);
                        // find mana damage overage and reconvert to HP damage
                        var manaRemainder = (manaDamage - targetPlayer.Mana.Current) / skillModifier / 1.5;
                        if (skill.AdvancementClass == SkillAdvancementClass.Specialized)
                            manaRemainder = (manaDamage - targetPlayer.Mana.Current) / skillModifier / 3;

                        amount = (uint)((damage * 0.75) + manaRemainder);
                        targetPlayer.UpdateVitalDelta(targetPlayer.Mana, (int)-(targetPlayer.Mana.Current - 1));
                        targetPlayer.UpdateVitalDelta(targetPlayer.Health, (int)-(amount));
                        targetPlayer.DamageHistory.Add(ProjectileSource, Spell.DamageType, amount);
                    }
                }
            }
                
                    //if (targetPlayer != null && targetPlayer.Fellowship != null)
                    //targetPlayer.Fellowship.OnVitalUpdate(targetPlayer);
            
       
                    /* amount = (uint)Math.Round(damage);    // full amount for debugging

                    Console.WriteLine($" -criticalHit? {critical}\n" +
                        $" -damageRatingMod: {damageRatingMod}\n" +
                        $" -damageResistRatingMod: {damageResistRatingMod}\n" +
                        $" -FINAL: {amount}"); */

            // Overload Stamps + Messages
            var overloadPercent = 0;
            var overload = false;

            if (sourcePlayer != null)
            {
                var combatAbility = CombatAbility.None;
                var combatFocus = sourceCreature.GetEquippedCombatFocus();
                if (combatFocus != null)
                    combatAbility = combatFocus.GetCombatAbility();

                if (combatAbility == CombatAbility.Overload)
                {
                    overload = true;
                    var projectileScaler = 1;
                    if (SpellType == ProjectileSpellType.Streak)
                        projectileScaler = 5;
                    if (SpellType == ProjectileSpellType.Blast)
                        projectileScaler = 3;
                    if (SpellType == ProjectileSpellType.Volley || SpellType == ProjectileSpellType.Ring || SpellType == ProjectileSpellType.Wall)
                        projectileScaler = 6;

                    overloadPercent = Player.HandleOverloadStamps(sourcePlayer, projectileScaler, Spell.Level);
                }
            }
            // add threat to damaged targets
            if (target.IsMonster && sourcePlayer != null)
            {
                var percentOfTargetMaxHealth = (float)amount / target.Health.MaxValue;
                target.IncreaseTargetThreatLevel(sourcePlayer, (int)(percentOfTargetMaxHealth * 1000));
            }

            // Jewelcrafting Post-Damage Handling

            if (sourcePlayer != null)
            {
                var projectileScaler = 1;
                if (SpellType == ProjectileSpellType.Streak)
                    projectileScaler = 5;
                if (SpellType == ProjectileSpellType.Blast)
                    projectileScaler = 3;
                if (SpellType == ProjectileSpellType.Volley || SpellType == ProjectileSpellType.Ring || SpellType == ProjectileSpellType.Wall)
                    projectileScaler = 6;
                Jewel.HandleCasterAttackerBonuses(sourcePlayer, target, SpellType, Spell.DamageType, Spell.Level, projectileScaler);
                Jewel.HandlePlayerAttackerBonuses(sourcePlayer, target, damage, Spell.DamageType);
            }

            if (targetPlayer != null)
            {
                Jewel.HandleCasterDefenderBonuses(targetPlayer, sourceCreature, SpellType);
                Jewel.HandlePlayerDefenderBonuses(targetPlayer, target, damage);
            }

            // show debug info
            if (sourceCreature != null && sourceCreature.DebugDamage.HasFlag(Creature.DebugDamageType.Attacker))
            {
                ShowInfo(sourceCreature, heritageMod, sneakAttackMod, damageRatingMod, damageResistRatingMod, critDamageRatingMod, critDamageResistRatingMod, pkDamageRatingMod, pkDamageResistRatingMod, damage);
            }
            if (target.DebugDamage.HasFlag(Creature.DebugDamageType.Defender))
            {
                ShowInfo(target, heritageMod, sneakAttackMod, damageRatingMod, damageResistRatingMod, critDamageRatingMod, critDamageResistRatingMod, pkDamageRatingMod, pkDamageResistRatingMod, damage);
            }

            if (target.IsAlive)
            {
                string verb = null, plural = null;
                Strings.GetAttackVerb(Spell.DamageType, percent, ref verb, ref plural);
                var type = Spell.DamageType.GetName().ToLower();

                var critMsg = critical ? "Critical hit! " : "";
                var sneakMsg = sneakAttackMod > 1.0f ? "Sneak Attack! " : "";
                var overpowerMsg = overpower ? "Overpower! " : "";
                var overloadMsg = overload ? $"{overloadPercent}% Overload! " : "";
                var resistSome = PartialEvasion == PartialEvasion.Some ? "Minor resist! " : "";
                var resistMost = PartialEvasion == PartialEvasion.Most ? "Major resist! " : "";
                var strikeThrough = Strikethrough > 0 ? "Strikethrough! " : "";

                var nonHealth = Spell.Category == SpellCategory.StaminaLowering || Spell.Category == SpellCategory.ManaLowering;

                if (sourcePlayer != null)
                {
                    var critProt = critDefended ? " Your critical hit was avoided with their augmentation!" : "";

                    var attackerMsg = $"{resistMost}{resistSome}{strikeThrough}{critMsg}{overpowerMsg}{overloadMsg}{sneakMsg}You {verb} {target.Name} for {amount} points with {Spell.Name}.{critProt}";

                    // could these crit / sneak attack?
                    if (nonHealth)
                    {
                        var vital = Spell.Category == SpellCategory.StaminaLowering ? "stamina" : "mana";
                        attackerMsg = $"With {Spell.Name} you drain {amount} points of {vital} from {target.Name}.";
                    }

                    if (!sourcePlayer.SquelchManager.Squelches.Contains(target, ChatMessageType.Magic))
                        sourcePlayer.Session.Network.EnqueueSend(new GameMessageSystemChat(attackerMsg, ChatMessageType.Magic));
                }

                if (targetPlayer != null)
                {
                    var critProt = critDefended ? " Your augmentation allows you to avoid a critical hit!" : "";

                    var defenderMsg = $"{resistMost}{resistSome}{critMsg}{overpowerMsg}{sneakMsg}{ProjectileSource.Name} {plural} you for {amount} points with {Spell.Name}.{critProt}";

                    if (nonHealth)
                    {
                        var vital = Spell.Category == SpellCategory.StaminaLowering ? "stamina" : "mana";
                        defenderMsg = $"{ProjectileSource.Name} casts {Spell.Name} and drains {amount} points of your {vital}.";
                    }

                    if (!targetPlayer.SquelchManager.Squelches.Contains(ProjectileSource, ChatMessageType.Magic))
                        targetPlayer.Session.Network.EnqueueSend(new GameMessageSystemChat(defenderMsg, ChatMessageType.Magic));

                    if (sourceCreature != null)
                        targetPlayer.SetCurrentAttacker(sourceCreature);
                }

                if (!nonHealth)
                {
                    if (equippedCloak != null && Cloak.HasProcSpell(equippedCloak))
                        Cloak.TryProcSpell(target, ProjectileSource, equippedCloak, percent);

                    target.EmoteManager.OnDamage(sourcePlayer);

                    if (critical)
                        target.EmoteManager.OnReceiveCritical(sourcePlayer);
                }
            }
            else
            {
                var lastDamager = ProjectileSource != null ? new DamageHistoryInfo(ProjectileSource) : null;
                target.OnDeath(lastDamager, Spell.DamageType, critical);
                target.Die();
            }

        }


        /// <summary>
        /// Sets the physics state for a launched projectile
        /// </summary>
        public void SetProjectilePhysicsState(WorldObject target, bool useGravity)
        {
            if (useGravity)
                GravityStatus = true;

            CurrentMotionState = null;
            Placement = null;

            // TODO: Physics description timestamps (sequence numbers) don't seem to be getting updated

            //Console.WriteLine("SpellProjectile PhysicsState: " + PhysicsObj.State);

            var pos = Location.Pos;
            var rotation = Location.Rotation;
            PhysicsObj.Position.Frame.Origin = pos;
            PhysicsObj.Position.Frame.Orientation = rotation;

            var velocity = Velocity;
            //velocity = Vector3.Transform(velocity, Matrix4x4.Transpose(Matrix4x4.CreateFromQuaternion(rotation)));
            PhysicsObj.Velocity = velocity;

            if (target != null)
                PhysicsObj.ProjectileTarget = target.PhysicsObj;

            PhysicsObj.set_active(true);
        }

        public static void ShowInfo(Creature observed, Spell spell, CreatureSkill skill, float criticalChance, bool criticalHit, bool critDefended, bool overpower, float weaponCritDamageMod,
            float magicSkillBonus, int baseDamage, float critDamageBonus, float elementalDamageMod, float slayerMod,
            float weaponResistanceMod, float resistanceMod, float absorbMod,
            float lifeProjectileDamage, float lifeMagicDamage, float finalDamage)
        {
            var observer = PlayerManager.GetOnlinePlayer(observed.DebugDamageTarget);
            if (observer == null)
            {
                observed.DebugDamage = Creature.DebugDamageType.None;
                return;
            }

            var info = $"Skill: {skill.Skill.ToSentence()}\n";
            info += $"CriticalChance: {criticalChance}\n";
            info += $"CriticalHit: {criticalHit}\n";

            if (critDefended)
                info += $"CriticalDefended: {critDefended}\n";

            info += $"Overpower: {overpower}\n";
        
            if (spell.MetaSpellType == ACE.Entity.Enum.SpellType.LifeProjectile)
            {
                // life magic projectile
                info += $"LifeProjectileDamage: {lifeProjectileDamage}\n";
                info += $"DamageRatio: {spell.DamageRatio}\n";
                info += $"LifeMagicDamage: {lifeMagicDamage}\n";
            }
            else
            {
                // war/void projectile
                var difficulty = Math.Min(spell.Power, 350);
                info += $"Difficulty: {difficulty}\n";

                if (magicSkillBonus != 0.0f)
                    info += $"SkillBonus: {magicSkillBonus}\n";

                info += $"BaseDamageRange: {spell.MinDamage} - {spell.MaxDamage}\n";
                info += $"BaseDamage: {baseDamage}\n";
                info += $"DamageType: {spell.DamageType}\n";
            }

            if (weaponCritDamageMod != 1.0f)
                info += $"WeaponCritDamageMod: {weaponCritDamageMod}\n";

            if (critDamageBonus != 0)
                info += $"CritDamageBonus: {critDamageBonus}\n";

            if (elementalDamageMod != 1.0f)
                info += $"ElementalDamageMod: {elementalDamageMod}\n";

            if (slayerMod != 1.0f)
                info += $"SlayerMod: {slayerMod}\n";

            if (weaponResistanceMod != 1.0f)
                info += $"WeaponResistanceMod: {weaponResistanceMod}\n";

            if (resistanceMod != 1.0f)
                info += $"ResistanceMod: {resistanceMod}\n";

            if (absorbMod != 1.0f)
                info += $"AbsorbMod: {absorbMod}\n";

            //observer.Session.Network.EnqueueSend(new GameMessageSystemChat(info, ChatMessageType.Broadcast));
            observer.DebugDamageBuffer += info;
        }

        public static void ShowInfo(Creature observed, float heritageMod, float sneakAttackMod, float damageRatingMod, float damageResistRatingMod,
            float critDamageRatingMod, float critDamageResistRatingMod, float pkDamageRatingMod, float pkDamageResistRatingMod, float damage)
        {
            var observer = PlayerManager.GetOnlinePlayer(observed.DebugDamageTarget);
            if (observer == null)
            {
                observed.DebugDamage = Creature.DebugDamageType.None;
                return;
            }
            var info = "";

            if (heritageMod != 1.0f)
                info += $"HeritageMod: {heritageMod}\n";

            if (sneakAttackMod != 1.0f)
                info += $"SneakAttackMod: {sneakAttackMod}\n";

            if (critDamageRatingMod != 1.0f)
                info += $"CritDamageRatingMod: {critDamageRatingMod}\n";

            if (pkDamageRatingMod != 1.0f)
                info += $"PkDamageRatingMod: {pkDamageRatingMod}\n";

            if (damageRatingMod != 1.0f)
                info += $"DamageRatingMod: {damageRatingMod}\n";

            if (critDamageResistRatingMod != 1.0f)
                 info += $"CritDamageResistRatingMod: {critDamageResistRatingMod}\n";

            if (pkDamageResistRatingMod != 1.0f)
                info += $"PkDamageResistRatingMod: {pkDamageResistRatingMod}\n";

            if (damageResistRatingMod != 1.0f)
                info += $"DamageResistRatingMod: {damageResistRatingMod}\n";

            info += $"Final damage: {damage}";

            observer.Session.Network.EnqueueSend(new GameMessageSystemChat(observer.DebugDamageBuffer + info, ChatMessageType.Broadcast));

            observer.DebugDamageBuffer = null;
        }

        /// <summary>
        /// If resist succeeded, determine if resist was partial or full. 
        /// </summary>
        private bool GetResistedMod(out float resistedMod)
        {
            if (PartialEvasion == PartialEvasion.None)
            {
                resistedMod = 1.0f;
                return false;
            }
            else if (PartialEvasion == PartialEvasion.Some)
            {
                resistedMod = 2.0f / 3.0f;
                return false;
            }
            else if(PartialEvasion == PartialEvasion.Most)
            {
                resistedMod = 1.0f / 3.0f;
                return false;
            }
            else
            {
                resistedMod = 0.0f;
                return true;
            }
        }
    }
}
