using System;
using System.Collections.Generic;
using System.Linq;

using ACE.Common;
using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Entity.Models;
using ACE.Server.Entity;
using ACE.Server.Entity.Actions;
using ACE.Server.Factories;
using ACE.Server.Network.GameEvent.Events;
using ACE.Server.Network.GameMessages.Messages;
using ACE.Server.Network.Structure;

namespace ACE.Server.WorldObjects
{
    public class Hotspot : WorldObject
    {
        public Hotspot(Weenie weenie, ObjectGuid guid) : base(weenie, guid)
        {
            SetEphemeralValues();
        }

        public Hotspot(Biota biota) : base(biota)
        {
            SetEphemeralValues();
        }

        public Hotspot()
        {
        }

        private void SetEphemeralValues()
        {
            // If CycleTime is less than 1, player has a very bad time.
            if ((CycleTime ?? 0) < 1)
                CycleTime = 1;
        }

        private HashSet<ObjectGuid> Creatures = new HashSet<ObjectGuid>();

        private ActionChain ActionLoop = null;

        public Player P_HotspotOwner;

        public Player HotspotSummoner;

        public override void OnCollideObject(WorldObject wo)
        {
            if (!(wo is Creature creature))
                return;

            if (AffectsOnlyAis && (wo is Player))
                return;
              
            if (!Creatures.Contains(creature.Guid))
            {
                //Console.WriteLine($"{Name} ({Guid}).OnCollideObject({creature.Name})");
                Creatures.Add(creature.Guid);
            }

            if (ActionLoop == null)
            {
                ActionLoop = NextActionLoop;
                NextActionLoop.EnqueueChain();
            }
        }

        public override void OnCollideObjectEnd(WorldObject wo)
        {
            /*if (!(wo is Player player))
                return;

            if (Players.Contains(player.Guid))
                Players.Remove(player.Guid);*/
        }

        private ActionChain NextActionLoop
        {
            get
            {
                ActionLoop = new ActionChain();
                ActionLoop.AddDelaySeconds(CycleTimeNext);
                ActionLoop.AddAction(this, () =>
                {
                    if (Creatures.Any())
                    {
                        Activate();
                        NextActionLoop.EnqueueChain();
                    }
                    else
                    {
                        ActionLoop = null;
                    }
                });
                return ActionLoop;
            }
        }

        private double CycleTimeNext
        {
            get
            {
                var max = CycleTime;
                var min = max * (1.0f - CycleTimeVariance ?? 0.0f);

                return ThreadSafeRandom.Next((float)min, (float)max);
            }
        }

        public double? CycleTime
        {
            get => GetProperty(PropertyFloat.HotspotCycleTime);
            set { if (value == null) RemoveProperty(PropertyFloat.HotspotCycleTime); else SetProperty(PropertyFloat.HotspotCycleTime, (double)value); }
        }

        public double? CycleTimeVariance
        {
            get => GetProperty(PropertyFloat.HotspotCycleTimeVariance) ?? 0;
            set { if (value == null) RemoveProperty(PropertyFloat.HotspotCycleTimeVariance); else SetProperty(PropertyFloat.HotspotCycleTimeVariance, (double)value); }
        }

        private float DamageNext
        {
            get
            {
                var r = GetBaseDamage();
                var p = (float)ThreadSafeRandom.Next(r.MinDamage, r.MaxDamage);
                return p;
            }
        }

        private int? _DamageType
        {
            get => GetProperty(PropertyInt.DamageType);
            set { if (value == null) RemoveProperty(PropertyInt.DamageType); else SetProperty(PropertyInt.DamageType, (int)value); }
        }

        public DamageType DamageType
        {
            get { return (DamageType)_DamageType; }
        }

        public bool IsHot
        {
            get => GetProperty(PropertyBool.IsHot) ?? false;
            set { if (!value) RemoveProperty(PropertyBool.IsHot); else SetProperty(PropertyBool.IsHot, value); }
        }

        public bool AffectsAis
        {
            get => GetProperty(PropertyBool.AffectsAis) ?? false;
            set { if (!value) RemoveProperty(PropertyBool.AffectsAis); else SetProperty(PropertyBool.AffectsAis, value); }
        }

        public bool AffectsOnlyAis
        {
            get => GetProperty(PropertyBool.AffectsOnlyAis) ?? false;
            set { if (!value) RemoveProperty(PropertyBool.AffectsOnlyAis); else SetProperty(PropertyBool.AffectsOnlyAis, value); }
        }

        private void Activate()
        {
            foreach (var creatureGuid in Creatures.ToList())
            {
                var creature = CurrentLandblock.GetObject(creatureGuid) as Creature;

                // verify current state of collision here
                if (creature == null || !creature.PhysicsObj.is_touching(PhysicsObj))
                {
                    //Console.WriteLine($"{Name} ({Guid}).OnCollideObjectEnd({creature?.Name})");
                    Creatures.Remove(creatureGuid);
                    continue;
                }
                Activate(creature);
            }
        }
         
        private void Activate(Creature creature)
        {
            if (!IsHot) return;

            if (!(creature is Player))
                if (!AffectsAis && !AffectsOnlyAis)
                    return;

            var amount = DamageNext;
            var iAmount = (int)Math.Round(amount);

            var player = creature as Player;

            var currentTime = Time.GetUnixTime();

            if (creature.HotspotImmunityTimestamp > currentTime)
                return;
            else
            {
                var immunityTime = (CycleTime ?? 0) * (1.0f - CycleTimeVariance ?? 0.0f) * 0.9f; // Multiplying the minimum possible CycleTime by 0.9 just to be extra sure that we wont be immune for the next tick.
                creature.HotspotImmunityTimestamp = currentTime + immunityTime;
            }
            
            if (player != null)
                player.RechargeEmpoweredScarabs(this);
            
            if (player != null && CampfireHotspot == true)
            {   
                var forwardCommand = player.CurrentMovementData.MovementType == MovementType.Invalid && player.CurrentMovementData.Invalid != null ? player.CurrentMovementData.Invalid.State.ForwardCommand : MotionCommand.Invalid;
                if (forwardCommand == MotionCommand.Sitting || forwardCommand == MotionCommand.Sleeping || forwardCommand == MotionCommand.Crouch)
                {

                    if (player.CampfireTimer == 0)
                        player.CampfireTimer = Time.GetUnixTime() + 10;

                    if (player.CampfireTimer < Time.GetUnixTime())
                    {
                        if(player.WellRestedHotspot == null)
                            player.Session.Network.EnqueueSend(new GameMessageSystemChat($"Gazing into the fire calms your soul.", ChatMessageType.Magic));

                        player.CampfireTimer = 0;
                        player.WellRestedHotspot = this;
                    }
                }
                else
                    player.CampfireTimer = 0;
            }

            switch (DamageType)
            {
                default:

                    if (creature.Invincible) return;

                    amount *= creature.GetResistanceMod(DamageType, this, null);

                    if (player != null)
                        iAmount = player.TakeDamage(this, DamageType, amount, Server.Entity.BodyPart.Foot, PartialEvasion.None);
                    else
                        iAmount = (int)creature.TakeDamage(this, DamageType, amount);

                    if (creature.IsDead && Creatures.Contains(creature.Guid))
                        Creatures.Remove(creature.Guid);

                    break;

                case DamageType.Mana:
                    iAmount = creature.UpdateVitalDelta(creature.Mana, -iAmount);
                    break;

                case DamageType.Stamina:
                    iAmount = creature.UpdateVitalDelta(creature.Stamina, -iAmount);
                    break;

                case DamageType.Health:
                    iAmount = creature.UpdateVitalDelta(creature.Health, -iAmount);

                    if (iAmount > 0)
                        creature.DamageHistory.OnHeal((uint)iAmount);
                    else
                        creature.DamageHistory.Add(this, DamageType.Health, (uint)-iAmount);

                    break;
            }

            if (!Visibility)
                EnqueueBroadcast(new GameMessageSound(Guid, Sound.TriggerActivated, 1.0f));

            if (player != null && !string.IsNullOrWhiteSpace(ActivationTalk) && iAmount != 0)
                player.Session.Network.EnqueueSend(new GameMessageSystemChat(ActivationTalk.Replace("%i", Math.Abs(iAmount).ToString()), ChatMessageType.Broadcast));

            // perform activation emote
            if (ActivationResponse.HasFlag(ActivationResponse.Emote))
                OnEmote(creature);
        }

        public static void TryGenHotspot(Player playerAttacker, Creature defender, int? tier, ACE.Entity.Enum.DamageType damageType)
        {
            uint wcid = 0;
            int modifiedTier = (int)tier - 1;
            modifiedTier = modifiedTier > 4 ? 4 : modifiedTier;

            int[] flameHotspots = { 1053902, 1053903, 1053904, 1053905, 1053906 };
            int[] frostHotspots = { 1053909, 1053910, 1053911, 1053912, 1053913 };
            int[] acidHotspots = { 1053915, 1053916, 1053917, 1053918, 1053919 };
            int[] lightningHotspots = { 1053921, 1053922, 1053923, 1053924, 1053925 };
            int[] healingHotspots = { 1053928, 1053929, 1053930, 1053931, 1053932 };

            if (damageType == ACE.Entity.Enum.DamageType.Fire && ((double)playerAttacker.GetEquippedItemsRatingSum(PropertyInt.GearFire) / 1000) > ThreadSafeRandom.Next(0f, 1f))
                wcid = (uint)flameHotspots[modifiedTier];

            if (damageType == ACE.Entity.Enum.DamageType.Cold && ((double)playerAttacker.GetEquippedItemsRatingSum(PropertyInt.GearFrost) / 1000) > ThreadSafeRandom.Next(0f, 1f))
                wcid = (uint)frostHotspots[modifiedTier];

            if (damageType == ACE.Entity.Enum.DamageType.Acid && ((double)playerAttacker.GetEquippedItemsRatingSum(PropertyInt.GearAcid) / 1000) > ThreadSafeRandom.Next(0f, 1f))
                wcid = (uint)acidHotspots[modifiedTier];

            if (damageType == ACE.Entity.Enum.DamageType.Electric && ((double)playerAttacker.GetEquippedItemsRatingSum(PropertyInt.GearLightning) / 1000) > ThreadSafeRandom.Next(0f, 1f))
                wcid = (uint)lightningHotspots[modifiedTier];
                
            if (damageType == ACE.Entity.Enum.DamageType.Health || damageType == ACE.Entity.Enum.DamageType.Stamina || damageType == ACE.Entity.Enum.DamageType.Mana)
            {
                if ((double)playerAttacker.GetEquippedItemsRatingSum(PropertyInt.GearHealBubble) / 1000 > ThreadSafeRandom.Next(0f, 1f))
                    wcid = (uint)healingHotspots[modifiedTier];
            }
                

            if (wcid != 0)
                CreateHotspot(playerAttacker, defender, wcid);
        }

        public static bool? CreateHotspot(Player playerAttacker, Creature defender, uint wcid)
        {
            var wo = WorldObjectFactory.CreateNewWorldObject(wcid);

            var hotspot = wo as Hotspot;

            var success = hotspot.Initialize(playerAttacker, defender, hotspot);

            var activator = WorldObjectFactory.CreateNewWorldObject(1053933);

            activator.Location = new ACE.Entity.Position(defender.Location);

            activator.Location.LandblockId = new LandblockId(defender.Location.GetCell());

            activator.Location.PositionZ += 0.05f;

            activator.EnterWorld();

            if (success != true) wo.Destroy();

            return success;
        }


        public virtual bool? Initialize(Player player, Creature defender, Hotspot hotspot)
        {
            Name = player.Name + "'s " + Name;

            HotspotOwner = player.Guid.Full;
            P_HotspotOwner = player;

            hotspot.Location = new ACE.Entity.Position(defender.Location);

            hotspot.Location.LandblockId = new LandblockId(defender.Location.GetCell());

            hotspot.Location.PositionZ += 0.05f;

            var success = hotspot.EnterWorld();

            return true;
        }

    }


}
