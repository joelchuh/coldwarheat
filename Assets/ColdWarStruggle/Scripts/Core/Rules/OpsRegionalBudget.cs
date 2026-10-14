using System;
using System.Collections.Generic;
using System.Linq;
namespace ColdWarStruggle.Core
{
    // A trusted conditional contribution. Activation/expiry belong to the card/effect layer.
    public sealed class OpsRegionalBonus
    {
        public string Id { get; } public string Region { get; } public int Delta { get; }
        public OpsRegionalBonus(string id,string region,int delta)
        { OpsGeneralGuard.Id(id);OpsGeneralGuard.Id(region);if(delta<1)throw new ArgumentOutOfRangeException(nameof(delta));Id=id;Region=region;Delta=delta; }
    }
    public sealed class OpsRegionalBudget
    {
        public MapDefinition Map { get; } public OpsGeneralQuote Quote { get; }
        public IReadOnlyList<OpsRegionalBonus> EligibleBonuses { get; }
        public int Spent { get; } public bool HasTargets { get; } public int EffectiveBudget { get; }
        public OpsRegionalBudget(MapDefinition map,OpsGeneralQuote quote,IEnumerable<OpsRegionalBonus> bonuses)
        {
            Map=map??throw new ArgumentNullException(nameof(map));Quote=quote??throw new ArgumentNullException(nameof(quote));
            if(quote.Purpose!=OpsValuePurpose.Influence && quote.Purpose!=OpsValuePurpose.Realignment && quote.Purpose!=OpsValuePurpose.Coup)throw new ArgumentException("Regional OPS only apply to map operations.");
            var copy=bonuses?.Take(129).ToArray();
            if(copy==null || copy.Length>128 || copy.Any(b=>b==null) || copy.Select(b=>b.Id).Distinct(StringComparer.Ordinal).Count()!=copy.Length
                || copy.Any(b=>map.GetCountriesInRegion(b.Region).Count==0))throw new ArgumentException("Invalid regional contributions.");
            EligibleBonuses=Array.AsReadOnly(copy.OrderBy(b=>b.Id,StringComparer.Ordinal).ToArray());EffectiveBudget=Calculate(EligibleBonuses);
        }
        OpsRegionalBudget(OpsRegionalBudget before,OpsRegionalBonus[] eligible,int spent)
        { Map=before.Map;Quote=before.Quote;EligibleBonuses=Array.AsReadOnly(eligible);Spent=spent;HasTargets=true;EffectiveBudget=Calculate(eligible); }
        int Calculate(IEnumerable<OpsRegionalBonus> bonuses)=>checked((int)Math.Max(Quote.Definition.Minimum,Math.Min(Quote.Definition.OrdinaryMaximum,Quote.RawValue)+bonuses.Sum(b=>(long)b.Delta)));
        OpsRegionalBonus[] EligibleFor(CountryId target)
        { var country=Map.GetCountry(target);return EligibleBonuses.Where(b=>country.Region==b.Region || country.Subregions.Contains(b.Region)).ToArray(); }
        public int BudgetFor(CountryId target)=>Calculate(EligibleFor(target));
        // Rejected candidates never remove an already eligible bonus or alter accepted history.
        public bool TrySpend(CountryId target,int cost,out OpsRegionalBudget after)
        {
            after=null;if(cost<1)throw new ArgumentOutOfRangeException(nameof(cost));var eligible=EligibleFor(target);
            var spent=checked(Spent+cost);if(spent>Calculate(eligible))return false;
            after=new OpsRegionalBudget(this,eligible,spent);return true;
        }
    }
}
