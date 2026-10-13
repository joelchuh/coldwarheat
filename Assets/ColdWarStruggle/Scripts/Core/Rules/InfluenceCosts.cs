using System;
namespace ColdWarStruggle.Core
{
    public sealed class InfluenceCosts
    {
        public int Normal { get; }
        public int EnemyControlled { get; }
        public InfluenceCosts(int normal,int enemyControlled)
        {
            if(normal<1 || enemyControlled<normal)throw new ArgumentOutOfRangeException();
            Normal=normal;EnemyControlled=enemyControlled;
        }
    }
}
