using System;
using System.Collections.Generic;
using System.Linq;
namespace ColdWarStruggle.Core
{
    public sealed class FinalScoringDefinition
    {
        public RegionScoringDefinition RegionDefinition { get; }
        public IReadOnlyList<string> RegionIds { get; }
        public CardId ChinaCardId { get; }
        public int ChinaHolderVp { get; }

        public FinalScoringDefinition(RegionScoringDefinition regions,IEnumerable<string> regionIds,CardId chinaCardId,int chinaHolderVp)
        {
            if(regions==null || regionIds==null)throw new ArgumentNullException();
            var ids=regionIds.Take(7).ToArray();
            if(regions.Regions.Count!=6 || ids.Length!=6 || ids.Any(string.IsNullOrWhiteSpace)
                || ids.Distinct(StringComparer.Ordinal).Count()!=ids.Length
                || !new HashSet<string>(regions.Regions.Select(x=>x.Id),StringComparer.Ordinal).SetEquals(ids))
                throw new ArgumentException("Final scoring requires exactly the six standard regions, without the weighted subregion.");
            if(chinaCardId.Value!="card.china" || chinaHolderVp<0)
                throw new ArgumentException("Invalid final China card reward.");
            RegionDefinition=regions;
            RegionIds=Array.AsReadOnly(ids.OrderBy(x=>x,StringComparer.Ordinal).ToArray());
            ChinaCardId=chinaCardId;
            ChinaHolderVp=chinaHolderVp;
        }
    }
}
