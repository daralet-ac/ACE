using System;

using ACE.Entity;
using ACE.Server.WorldObjects;

namespace ACE.Server.Entity
{
    public class DamageHistoryInfo
    {
        public readonly WeakReference<WorldObject> Attacker;

        public readonly ObjectGuid Guid;
        public readonly string Name;

        public float TotalDamage;

        public readonly WeakReference<Player> PetOwner;

        public readonly WeakReference<Player> HotspotOwner;

        public bool IsPlayer => Guid.IsPlayer();

        public readonly bool IsOlthoiPlayer;

        public int Level;

        public DamageHistoryInfo(WorldObject attacker, float totalDamage = 0.0f)
        {
            Attacker = new WeakReference<WorldObject>(attacker);

            Guid = attacker.Guid;
            Name = attacker.Name;
            Level = attacker.Level ?? 1;

            IsOlthoiPlayer = attacker is Player player && player.IsOlthoiPlayer;

            TotalDamage = totalDamage;

            if (attacker is CombatPet combatPet && combatPet.P_PetOwner != null)
                PetOwner = new WeakReference<Player>(combatPet.P_PetOwner);

            if (attacker is Hotspot hotspot && hotspot.P_HotspotOwner != null)
            {
                HotspotOwner = new WeakReference<Player>(hotspot.P_HotspotOwner);
                // Console.WriteLine("Referenced hotspot owner)");
            }
        }

        public WorldObject TryGetAttacker()
        {
            Attacker.TryGetTarget(out var attacker);

            return attacker;
        }

        public Player TryGetPetOwner()
        {
            PetOwner.TryGetTarget(out var petOwner);

            return petOwner;
        }

        public Player TryGetHotspotOwner()
        {
            HotspotOwner.TryGetTarget(out var hotspotOwner);

            return hotspotOwner;
        }

        public WorldObject TryGetPetOwnerOrAttacker()
        {
            if (PetOwner != null)
                return TryGetPetOwner();
            else
                return TryGetAttacker();
        }
    }
}
