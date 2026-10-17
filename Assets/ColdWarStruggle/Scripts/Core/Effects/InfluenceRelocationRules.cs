using System;
using System.Collections.Generic;
using System.Linq;
namespace ColdWarStruggle.Core
{
    internal static class InfluenceRelocationRules
    {
        internal static InfluenceEventQuery Query(GameState state,RelocateOwnInfluenceEffect definition)
        {
            var owner=definition.Beneficiary;var opponent=owner==PlayerSide.US?PlayerSide.USSR:PlayerSide.US;
            var countries=state.Map.Countries.OrderBy(c=>c.Id.Value,StringComparer.Ordinal).ToArray();
            var sources=countries.Where(c=>state.GetInfluence(c.Id,owner)>0)
                .Select(c=>new InfluenceAllocation(c.Id,Math.Min(definition.MaximumTotal,state.GetInfluence(c.Id,owner))));
            var destinations=countries.Where(c=>ControlRules.GetController(state.Countries[c.Id].US,state.Countries[c.Id].USSR,c.Stability)!=opponent)
                .Select(c=>new InfluenceAllocation(c.Id,definition.PerDestinationCap));
            return new InfluenceEventQuery(owner,state.Version,definition.MaximumTotal,sources,destinations);
        }
        static bool ValidRows(IReadOnlyList<InfluenceAllocation> rows,IReadOnlyList<InfluenceAllocation> capacities,out long total)
        {
            total=0;
            if(rows==null || rows.Count>InfluenceEventSelection.MaxTargets)return false;
            var allowed=capacities.ToDictionary(a=>a.Country,a=>a.Amount);var seen=new HashSet<CountryId>();
            foreach(var row in rows)
            {
                if(row.Amount<=0 || !seen.Add(row.Country) || !allowed.TryGetValue(row.Country,out var limit) || row.Amount>limit)return false;
                total+=row.Amount;
            }
            return true;
        }
        internal static InfluenceEventPlan Plan(GameState state,RelocateOwnInfluenceEffect definition,InfluenceEventSelection selection,InfluenceEventQuery query)
        {
            InfluenceEventPlan Stop(InfluenceEventError error,InfluenceEventStatus status=InfluenceEventStatus.Rejected)
                =>new InfluenceEventPlan(status,error,definition,state);
            if(selection==null && query.Status==InfluenceEventStatus.AwaitingChoice)
                return Stop(InfluenceEventError.None,InfluenceEventStatus.AwaitingChoice);
            if(selection!=null)
            {
                if(selection.Owner!=query.Owner)return Stop(InfluenceEventError.WrongDecisionOwner);
                if(selection.SourceVersion!=state.Version)return Stop(InfluenceEventError.StaleSelection);
                if(selection.Kind!=InfluenceDecisionKind.Relocation
                    || !ValidRows(selection.Sources,query.SourceCapacities,out var removed)
                    || !ValidRows(selection.Destinations,query.DestinationCapacities,out var added)
                    || removed!=added || removed>definition.MaximumTotal)return Stop(InfluenceEventError.InvalidSelection);
                // No netting: same-country re-placement has an unresolved project boundary.
                if(selection.Sources.Select(a=>a.Country).Intersect(selection.Destinations.Select(a=>a.Country)).Any())
                    return Stop(InfluenceEventError.RelocationOverlap,InfluenceEventStatus.RequiresRuling);
            }
            try
            {
                var work=new InfluenceMutation(state);
                if(selection!=null)
                {
                    foreach(var row in selection.Sources.OrderBy(a=>a.Country.Value,StringComparer.Ordinal))work.RemoveExact(row.Country,definition.Beneficiary,row.Amount);
                    foreach(var row in selection.Destinations.OrderBy(a=>a.Country.Value,StringComparer.Ordinal))work.Add(row.Country,definition.Beneficiary,row.Amount);
                }
                return work.Complete(definition);
            }
            catch(OverflowException){return Stop(InfluenceEventError.NumericOverflow);}
        }
    }
}
