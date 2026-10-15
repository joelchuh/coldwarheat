using System;
using System.Collections.Generic;
using System.Linq;
namespace ColdWarStruggle.Core
{
    public sealed class FinalScoringDefinition
    {
        // Domain identities for the base game, not scoring values. Values remain in shared rule data.
        static readonly HashSet<string> BaseRegionIds=new HashSet<string>(new[] {
            "region.europe","region.asia","region.middle_east","region.africa","region.central_america","region.south_america"
        },StringComparer.Ordinal);

        public RegionScoringDefinition RegionDefinition { get; }
        public IReadOnlyList<string> RegionIds { get; }
        public CardId ChinaCardId { get; }
        public int ChinaHolderVp { get; }

        public FinalScoringDefinition(RegionScoringDefinition regions,IEnumerable<string> regionIds,CardId chinaCardId,int chinaHolderVp)
        {
            if(regions==null || regionIds==null)throw new ArgumentNullException();
            var ids=regionIds.Take(BaseRegionIds.Count+1).ToArray();
            if(regions.Regions.Count!=BaseRegionIds.Count || ids.Length!=BaseRegionIds.Count || ids.Any(string.IsNullOrWhiteSpace)
                || !BaseRegionIds.SetEquals(ids)
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
