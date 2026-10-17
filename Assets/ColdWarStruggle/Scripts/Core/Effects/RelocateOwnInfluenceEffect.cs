using System;
namespace ColdWarStruggle.Core
{
    public sealed class RelocateOwnInfluenceEffect : InfluenceEffectDefinition
    {
        public int MaximumTotal { get; }
        public int PerDestinationCap { get; }
        public RelocateOwnInfluenceEffect(string card,string effect,PlayerSide beneficiary,int maximumTotal,int perDestinationCap)
            :base(card,effect,beneficiary)
        {
            if(maximumTotal<1 || perDestinationCap<1)throw new ArgumentOutOfRangeException();
            MaximumTotal=maximumTotal;PerDestinationCap=perDestinationCap;
        }
    }
}
