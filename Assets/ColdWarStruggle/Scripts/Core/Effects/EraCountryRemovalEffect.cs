using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
namespace ColdWarStruggle.Core
{
    public sealed class EraCountryRemovalEffect : InfluenceEffectDefinition
    {
        public string Region { get; }
        public int TargetCount { get; }
        public IReadOnlyDictionary<GameEra,int> AmountByEra { get; }
        public EraCountryRemovalEffect(string card,string effect,PlayerSide beneficiary,string region,int targetCount,IReadOnlyDictionary<GameEra,int> amountByEra)
            :base(card,effect,beneficiary)
        {
            if(string.IsNullOrWhiteSpace(region) || amountByEra==null || amountByEra.Count!=3
                || amountByEra.Keys.Any(era=>!Enum.IsDefined(typeof(GameEra),era)) || amountByEra.Values.Any(n=>n<1))
                throw new ArgumentException("A positive removal amount is required for every era.");
            if(targetCount<1 || targetCount>InfluenceEventSelection.MaxTargets)throw new ArgumentOutOfRangeException(nameof(targetCount));
            Region=region;TargetCount=targetCount;AmountByEra=new ReadOnlyDictionary<GameEra,int>(amountByEra.ToDictionary(p=>p.Key,p=>p.Value));
        }
    }
}
