using System;
using System.Linq;
using System.Collections.Generic;

namespace ColdWarStruggle.Core
{
    public static class InfluenceEventRules
    {
        public static InfluenceEventQuery Query(GameState state, InfluenceEffectDefinition definition, InfluenceEventPolicy policy)
        {
            if(state==null)throw new ArgumentNullException(nameof(state));
            var owner=definition?.Beneficiary??PlayerSide.US;
            InfluenceEventQuery Stop(InfluenceEventStatus status,InfluenceEventError error)
                => new InfluenceEventQuery(status,error,owner,state.Version,0,Array.Empty<CountryId>());
            if(state.Ended)return Stop(InfluenceEventStatus.Rejected,InfluenceEventError.GameAlreadyEnded);
            if(state.Deployment!=null)return Stop(InfluenceEventStatus.Rejected,InfluenceEventError.SetupInProgress);
            if(definition==null)return Stop(InfluenceEventStatus.Rejected,InfluenceEventError.InvalidDefinition);
            if(policy==InfluenceEventPolicy.Forbidden)return Stop(InfluenceEventStatus.Blocked,InfluenceEventError.Forbidden);
            if(policy!=InfluenceEventPolicy.NoActiveInterference)return Stop(InfluenceEventStatus.RequiresRuling,InfluenceEventError.InterferenceUnknown);
            IEnumerable<CountryId> candidates;int required=0;
            switch(definition)
            {
                case RelocateOwnInfluenceEffect relocation:return InfluenceRelocationRules.Query(state,relocation);
                case MatchOpponentInfluenceEffect match:
                    if(match.Countries.Any(c=>!state.Map.Contains(c)))return Stop(InfluenceEventStatus.Rejected,InfluenceEventError.InvalidDefinition);
                    candidates=match.Countries;required=1;break;
                case BudgetedRemovalEffect budget:
                    return BudgetedRemovalRules.Query(state,budget);
                case EraCountryRemovalEffect eraRemoval:
                    if(state.Turn==null)return Stop(InfluenceEventStatus.Rejected,InfluenceEventError.MissingTurnContext);
                    var eraRegion=state.Map.GetCountriesInRegion(eraRemoval.Region);
                    if(eraRegion.Count==0)return Stop(InfluenceEventStatus.Rejected,InfluenceEventError.InvalidDefinition);
                    var removedSide=eraRemoval.Beneficiary==PlayerSide.US?PlayerSide.USSR:PlayerSide.US;
                    var positive=eraRegion.Where(c=>state.GetInfluence(c.Id,removedSide)>0).Select(c=>c.Id).ToArray();
                    candidates=positive;required=Math.Min(eraRemoval.TargetCount,positive.Length);break;
                case AddFixedInfluenceEffect fixedGrant:
                    if(!state.Map.Contains(fixedGrant.Country))return Stop(InfluenceEventStatus.Rejected,InfluenceEventError.InvalidDefinition);
                    candidates=new[]{fixedGrant.Country};break;
                case AddToControlInfluenceEffect control:
                    if(!state.Map.Contains(control.Country))return Stop(InfluenceEventStatus.Rejected,InfluenceEventError.InvalidDefinition);
                    candidates=new[]{control.Country};break;
                case ClearThenControlEffect clear:
                    if(!state.Map.Contains(clear.Country))return Stop(InfluenceEventStatus.Rejected,InfluenceEventError.InvalidDefinition);
                    candidates=new[]{clear.Country};break;
                case AddThenRemoveFractionEffect fraction:
                    if(!state.Map.Contains(fraction.Country))return Stop(InfluenceEventStatus.Rejected,InfluenceEventError.InvalidDefinition);
                    candidates=new[]{fraction.Country};break;
                case RemoveFromUncontrolledEffect remove:
                    var region=state.Map.GetCountriesInRegion(remove.Region);
                    if(region.Count==0)return Stop(InfluenceEventStatus.Rejected,InfluenceEventError.InvalidDefinition);
                    var uncontrolled=region.Where(c=>ControlRules.GetController(state.Countries[c.Id].US,state.Countries[c.Id].USSR,c.Stability)==null).Select(c=>c.Id).ToArray();
                    candidates=uncontrolled;required=uncontrolled.Length==0?0:1;break;
                case AddToDistinctCountriesEffect add:
                    if(add.Regions.Any(r=>state.Map.GetCountriesInRegion(r).Count==0))return Stop(InfluenceEventStatus.Rejected,InfluenceEventError.InvalidDefinition);
                    var opponent=add.Beneficiary==PlayerSide.US?PlayerSide.USSR:PlayerSide.US;
                    var union=add.Regions.SelectMany(r=>state.Map.GetCountriesInRegion(r))
                        .Where(c=>add.ControlFilter==InfluenceControlFilter.Any
                            || ControlRules.GetController(state.Countries[c.Id].US,state.Countries[c.Id].USSR,c.Stability)!=opponent)
                        .Select(c=>c.Id).Distinct().ToArray();
                    if(union.Length<add.TargetCount && add.ShortagePolicy==InfluenceCandidateShortagePolicy.RequiresRuling)
                        return Stop(InfluenceEventStatus.RequiresRuling,InfluenceEventError.CandidateShortage);
                    candidates=union;required=Math.Min(add.TargetCount,union.Length);break;
                default:return Stop(InfluenceEventStatus.Rejected,InfluenceEventError.InvalidDefinition);
            }
            return new InfluenceEventQuery(required==0?InfluenceEventStatus.Ready:InfluenceEventStatus.AwaitingChoice,
                InfluenceEventError.None,owner,state.Version,required,candidates.OrderBy(c=>c.Value,StringComparer.Ordinal));
        }
        public static InfluenceEventPlan Plan(GameState state, InfluenceEffectDefinition definition, InfluenceEventSelection selection, InfluenceEventPolicy policy)
        {
            var query=Query(state,definition,policy);
            InfluenceEventPlan Reject(InfluenceEventError error)
                => new InfluenceEventPlan(InfluenceEventStatus.Rejected,error,definition,state);
            if(query.Error!=InfluenceEventError.None)return new InfluenceEventPlan(query.Status,query.Error,definition,state);
            if(definition is RelocateOwnInfluenceEffect relocation)return InfluenceRelocationRules.Plan(state,relocation,selection,query);
            if(query.Status==InfluenceEventStatus.AwaitingChoice)
            {
                if(selection==null)return new InfluenceEventPlan(InfluenceEventStatus.AwaitingChoice,InfluenceEventError.None,definition,state);
                if(selection.Owner!=query.Owner)return Reject(InfluenceEventError.WrongDecisionOwner);
                if(selection.SourceVersion!=state.Version)return Reject(InfluenceEventError.StaleSelection);
                if(selection.Kind!=query.Kind)return Reject(InfluenceEventError.InvalidSelection);
                if(query.Kind==InfluenceDecisionKind.Allocation)
                {
                    if(!BudgetedRemovalRules.IsValid(query,selection))return Reject(InfluenceEventError.InvalidSelection);
                }
                else if(selection.Targets==null || selection.Targets.Count!=query.RequiredTargets || selection.Targets.Count>InfluenceEventSelection.MaxTargets
                    || selection.Targets.Distinct().Count()!=selection.Targets.Count || selection.Targets.Any(c=>!query.LegalCountries.Contains(c)))
                    return Reject(InfluenceEventError.InvalidSelection);
            }
            else if(selection!=null)return Reject(InfluenceEventError.InvalidSelection);
            try
            {
                var work=new InfluenceMutation(state);
                var opponent=definition.Beneficiary==PlayerSide.US?PlayerSide.USSR:PlayerSide.US;
                switch(definition)
                {
                    case MatchOpponentInfluenceEffect _:
                        work.MatchOpponentByAdding(selection.Targets[0],definition.Beneficiary);break;
                    case EraCountryRemovalEffect eraRemoval:
                        if(query.RequiredTargets>0)
                            foreach(var country in selection.Targets.OrderBy(c=>c.Value,StringComparer.Ordinal))
                                work.RemoveExact(country,opponent,Math.Min(state.GetInfluence(country,opponent),eraRemoval.AmountByEra[state.Turn.Era]));
                        break;
                    case BudgetedRemovalEffect _:
                        if(query.RequiredAmount>0)
                            foreach(var allocation in selection.Allocations.OrderBy(a=>a.Country.Value,StringComparer.Ordinal))
                                work.RemoveExact(allocation.Country,opponent,allocation.Amount);
                        break;
                    case AddFixedInfluenceEffect fixedGrant:
                        work.Add(fixedGrant.Country,definition.Beneficiary,fixedGrant.Amount);break;
                    case AddToControlInfluenceEffect control:
                        work.EnsureControlByAdding(control.Country,definition.Beneficiary);break;
                    case ClearThenControlEffect clear:
                        work.RemoveAll(clear.Country,opponent);
                        work.EnsureControlByAdding(clear.Country,definition.Beneficiary);break;
                    case AddThenRemoveFractionEffect fraction:
                        work.Add(fraction.Country,definition.Beneficiary,fraction.AddAmount);
                        work.RemoveFractionCeiling(fraction.Country,opponent,fraction.Divisor);break;
                    case RemoveFromUncontrolledEffect _:
                        if(query.RequiredTargets>0)work.RemoveAll(selection.Targets[0],opponent);break;
                    case AddToDistinctCountriesEffect add:
                        if(query.RequiredTargets==0)break;
                        IEnumerable<CountryId> targets=selection.Targets;
                        if(add.ControlFilter!=InfluenceControlFilter.Any)targets=targets.OrderBy(c=>c.Value,StringComparer.Ordinal);
                        foreach(var country in targets)work.Add(country,definition.Beneficiary,add.Amount);break;
                }
                return work.Complete(definition);
            }
            catch(OverflowException) { return Reject(InfluenceEventError.NumericOverflow); }
        }
    }
}
