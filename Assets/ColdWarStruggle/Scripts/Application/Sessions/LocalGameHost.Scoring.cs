using System;
using System.Linq;
using ColdWarStruggle.Core;
namespace ColdWarStruggle.Application
{
    public sealed partial class LocalGameHost
    {
        ScoringPreviewResult QueryScoring(string region,long version)
        {
            lock(gate)
            {
                if(version!=state.Version)return new ScoringPreviewResult(state,RuleError.StaleVersion);
                var definition=state.Practice?.Definition.Scoring;
                if(definition==null)return new ScoringPreviewResult(state,RuleError.UnsupportedCardAction);
                if(string.IsNullOrWhiteSpace(region) || region.Length>256 || (region!=definition.WeightedRegionId && !definition.Regions.Any(r=>r.Id==region)))return new ScoringPreviewResult(state,RuleError.InvalidCommand);
                try
                {
                    // Public facts only; no card ownership lookup and no authoritative event/receipt ID is created.
                    var snapshot=new ScoringSnapshot(state.Map,"preview",state.Version,state.Countries,Array.Empty<string>());
                    return new ScoringPreviewResult(state,RuleError.None,RegionScoringService.CalculateUnmodified(definition,snapshot,region));
                }
                catch(OverflowException) {return new ScoringPreviewResult(state,RuleError.NumericOverflow);}
            }
        }
    }
}
