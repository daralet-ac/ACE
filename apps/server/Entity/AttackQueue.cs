using System.Collections.Generic;
using ACE.Server.WorldObjects;

namespace ACE.Server.Entity
{
    public class AttackQueue
    {
        public Player Player;

        public Queue<float> PowerAccuracy;

        public AttackQueue(Player player)
        {
            Player = player;

            PowerAccuracy = new Queue<float>();
        }

        public void Add(float powerAccuracy)
        {
            PowerAccuracy.Enqueue(powerAccuracy);
        }

        public float Fetch()
        {
            if (PowerAccuracy.Count > 1)
                PowerAccuracy.Dequeue();

            if (!PowerAccuracy.TryPeek(out var powerAccuracy))
            {
                return 0.5f;
            }
            return powerAccuracy;
        }

        public void Clear()
        {
            PowerAccuracy.Clear();
        }
    }
}
