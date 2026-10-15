using System;
using System.Collections.Generic;
using System.Linq;
namespace ColdWarStruggle.Core
{
    public sealed class FinalScoringResult
    {
        public FinalScoringDefinition Definition { get; }
        public string SnapshotId { get; }
        public long StateVersion { get; }
        public IReadOnlyList<RegionScoringResult> Regions { get; }
        public PlayerSide ChinaOwner { get; }
        public int ChinaVpForUS { get; }
        public int? NetVpForUS { get; }
        public PlayerSide? EuropeControlWinner { get; }

        internal FinalScoringResult(FinalScoringDefinition definition,ScoringSnapshot snapshot,ChinaCardState china,
            RegionScoringResult[] regions,int chinaVp,int? net,PlayerSide? europeWinner)
        {
            Definition=definition;SnapshotId=snapshot.Id;StateVersion=snapshot.StateVersion;
            Regions=Array.AsReadOnly(regions.ToArray());ChinaOwner=china.Owner;
            ChinaVpForUS=chinaVp;NetVpForUS=net;EuropeControlWinner=europeWinner;
        }
    }
    public static class FinalScoringService
    {
        // The host supplies China and the map from one state version, after all turn-end checks.
        // This computes a batch; it does not commit VP, move cards or authorize ending a game.
        public static FinalScoringResult CalculateUnmodified(FinalScoringDefinition definition,ScoringSnapshot snapshot,ChinaCardState china)
        {
            if(definition==null || snapshot==null || china==null)throw new ArgumentNullException();
            if(!ReferenceEquals(definition.RegionDefinition.Map,snapshot.Map) || !china.Id.Equals(definition.ChinaCardId))
                throw new ArgumentException("Final scoring definition/state binding mismatch.");
            if(snapshot.ActiveScoringEffects.Count!=0)
                throw new ArgumentException("Active scoring modifiers require their own verified handlers.");

            var regions=definition.RegionIds.Select(id=>RegionScoringService.CalculateUnmodified(definition.RegionDefinition,snapshot,id)).ToArray();
            var europeWinner=regions.SingleOrDefault(x=>x.EuropeControlWinner.HasValue)?.EuropeControlWinner;
            int chinaVp=china.Owner==PlayerSide.US?definition.ChinaHolderVp:-definition.ChinaHolderVp;
            long net=chinaVp;
            foreach(var region in regions)net+=region.NetVpForUS??0;
            // Europe is a victory signal, never an invented numeric score.
            int? numericNet=europeWinner.HasValue?(int?)null:checked((int)net);
            return new FinalScoringResult(definition,snapshot,china,regions,chinaVp,numericNet,europeWinner);
        }
    }
}
