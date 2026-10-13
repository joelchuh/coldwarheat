using System;
using System.Collections.Generic;
using System.Linq;
namespace ColdWarStruggle.Core
{
    public enum RuleError { None, InvalidCommand, WrongGame, WrongActor, StaleVersion, StaleContext, ModeMismatch, UnknownCountry,
        IllegalTarget, InsufficientBudget, GameAlreadyEnded, NumericOverflow, CommandIdConflict,
        EventNotAvailable, EventAlreadyResolved, EventNotStarted, EventInProgress, StaleDecision, InvalidSelection, EventRequiresRuling, EventForbidden, InvalidDefinition }
    public sealed class InfluencePlaced
    {
        public CountryId Country { get; }
        public PlayerSide Actor { get; }
        public int Cost { get; }
        public long Sequence { get; }
        public InfluencePlaced(CountryId country,PlayerSide actor,int cost,long sequence) { Country=country;Actor=actor;Cost=cost;Sequence=sequence; }
    }
    public sealed class PlacementPlan
    {
        public bool Accepted => Error==RuleError.None;
        public RuleError Error { get; }
        public GameState NextState { get; }
        public IReadOnlyList<int> Costs { get; }
        public IReadOnlyList<InfluencePlaced> Events { get; }
        internal PlacementPlan(RuleError error,GameState next,IReadOnlyList<int> costs,IReadOnlyList<InfluencePlaced> events)
        { Error=error;NextState=next;Costs=costs;Events=events; }
    }
    public static class InfluenceRules
    {
        public static PlacementPlan PlanPlacement(GameState state,OperationsContext context,IReadOnlyList<CountryId> orderedTargets)
        {
            if(state==null || orderedTargets==null || orderedTargets.Count==0 || orderedTargets.Count>128) return Reject(RuleError.InvalidCommand);
            if(state.Ended) return Reject(RuleError.GameAlreadyEnded);
            if(state.InfluenceEvent!=null && state.InfluenceEvent.Stage!=InfluenceEventStage.Resolved)return Reject(RuleError.EventInProgress);
            if(!ReferenceEquals(context,state.Operations)) return Reject(RuleError.StaleContext);
            if(context.Mode!=OperationsMode.Influence) return Reject(RuleError.ModeMismatch);
            var working=state.Countries.ToDictionary(x=>x.Key,x=>x.Value);
            var costs=new List<int>();var events=new List<InfluencePlaced>();int spent=state.SpentOps;
            try
            {
                foreach(var id in orderedTargets.ToArray())
                {
                    if(!state.Map.Contains(id)) return Reject(RuleError.UnknownCountry);
                    if(!context.Reachable.Contains(id)) return Reject(RuleError.IllegalTarget);
                    var before=working[id];var controller=ControlRules.GetController(before.US,before.USSR,state.Map.GetCountry(id).Stability);
                    int cost=controller.HasValue && controller.Value!=context.Actor?context.Costs.EnemyControlled:context.Costs.Normal;
                    if(cost>context.Budget-spent)return Reject(RuleError.InsufficientBudget);
                    working[id]=context.Actor==PlayerSide.US?new CountryState(checked(before.US+1),before.USSR):new CountryState(before.US,checked(before.USSR+1));
                    spent+=cost;costs.Add(cost);events.Add(new InfluencePlaced(id,context.Actor,cost,checked(state.EventSequence+events.Count+1)));
                }
                return new PlacementPlan(RuleError.None,state.Apply(working,spent,events.Count),costs.AsReadOnly(),events.AsReadOnly());
            }
            catch(OverflowException) { return Reject(RuleError.NumericOverflow); }
        }
        static PlacementPlan Reject(RuleError error) => new PlacementPlan(error,null,Array.AsReadOnly(new int[0]),Array.AsReadOnly(new InfluencePlaced[0]));
    }
}
