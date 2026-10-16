using System;
using System.Collections.Generic;
using System.Linq;
namespace ColdWarStruggle.Core
{
    public sealed class MatchOpponentInfluenceEffect : InfluenceEffectDefinition
    {
        public IReadOnlyList<CountryId> Countries { get; }
        public MatchOpponentInfluenceEffect(string card,string effect,PlayerSide beneficiary,IEnumerable<CountryId> countries)
            :base(card,effect,beneficiary)
        {
            var copy=countries?.Take(InfluenceEventSelection.MaxTargets+1).ToArray();
            if(copy==null || copy.Length==0 || copy.Length>InfluenceEventSelection.MaxTargets
                || copy.Any(c=>string.IsNullOrWhiteSpace(c.Value)) || copy.Distinct().Count()!=copy.Length)
                throw new ArgumentException("A bounded, unique country list is required.");
            Countries=Array.AsReadOnly(copy);
        }
    }
}
