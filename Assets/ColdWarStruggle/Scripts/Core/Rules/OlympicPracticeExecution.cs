using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
namespace ColdWarStruggle.Core
{
    // Authority-only parent. Public sessions must project an independent DTO.
    public sealed class OlympicPracticeExecution
    {
        public OlympicResolution Flow { get; }
        public OperationsMode? Mode { get; }
        internal ActionPracticeState Root { get; }
        internal IReadOnlyDictionary<CountryId,CountryState> RootCountries { get; }
        internal PracticeRiskState Risk { get; }
        internal OperationsContext Operations { get; }
        readonly long boundVersion,boundSequence;
        readonly int boundSpent;
        readonly RandomSnapshot boundRandom;
        readonly IReadOnlyDictionary<CountryId,CountryState> boundCountries;
        internal OlympicPracticeExecution(GameState state,OlympicResolution flow)
        {Root=state.Practice;RootCountries=state.Countries;Flow=flow;Risk=state.Practice.Risk;}
        OlympicPracticeExecution(OlympicPracticeExecution parent,OlympicResolution flow,PracticeRiskState risk)
        {Root=parent.Root;RootCountries=parent.RootCountries;Flow=flow;Risk=risk;}
        internal OlympicPracticeExecution WithFlow(OlympicResolution flow,PracticeRiskState risk)=>new OlympicPracticeExecution(this,flow,risk);
        OlympicPracticeExecution(OlympicPracticeExecution parent,OperationsContext operations,GameState before,
            IReadOnlyDictionary<CountryId,CountryState> countries,int spent,RandomSnapshot random,int eventCount)
        {
            Root=parent.Root;RootCountries=parent.RootCountries;Flow=parent.Flow;Risk=parent.Risk;Operations=operations;Mode=operations.Mode;
            boundVersion=checked(before.Version+1);boundSequence=checked(before.EventSequence+eventCount);boundSpent=spent;boundRandom=random;
            boundCountries=new ReadOnlyDictionary<CountryId,CountryState>(countries.ToDictionary(x=>x.Key,x=>x.Value));
        }
        internal OlympicPracticeExecution WithOperations(GameState before,OperationsContext operations)
        {
            if(!Matches(before) || Mode.HasValue || Flow.Stage!=OlympicResolutionStage.AwaitingOperations || operations==null
                || operations.Actor!=Flow.Sponsor || operations.RegionalBudget?.HasTargets==true || (operations.RegionalBudget?.Spent??0)!=0
                || (operations.Mode!=OperationsMode.Influence && operations.Mode!=OperationsMode.Realignment))throw new ArgumentException("Invalid Olympic operations binding.");
            return new OlympicPracticeExecution(this,operations,before,before.Countries,0,before.RandomState,1);
        }
        // Only the trusted ordinary OPS transition advances this immutable authority binding.
        internal OlympicPracticeExecution AdvanceOperations(GameState before,IReadOnlyDictionary<CountryId,CountryState> countries,
            int spent,RandomSnapshot random,int eventCount,OperationsContext operations)
        {
            if(!Mode.HasValue || !Matches(before) || spent<=before.SpentOps || operations==null || spent>operations.Budget || !ValidProgressContext(operations,spent) || eventCount<=0
                || (Mode==OperationsMode.Influence && !ReferenceEquals(random,before.RandomState)))throw new ArgumentException("Invalid Olympic operations progress.");
            return new OlympicPracticeExecution(this,operations,before,countries,spent,random,eventCount);
        }
        bool ValidProgressContext(OperationsContext after,int spent)
        {
            if(Operations.RegionalBudget==null)return ReferenceEquals(after,Operations);
            var old=Operations.RegionalBudget;var next=after.RegionalBudget;
            return next!=null && after.Id==Operations.Id && after.Actor==Operations.Actor && after.Mode==Operations.Mode
                && ReferenceEquals(after.Costs,Operations.Costs) && after.Reachable.SequenceEqual(Operations.Reachable)
                && ReferenceEquals(next.Map,old.Map) && ReferenceEquals(next.Quote,old.Quote) && next.Spent==spent
                && next.EligibleBonuses.All(old.EligibleBonuses.Contains);
        }
        internal bool Matches(GameState state)
        {
            var p=state?.Practice;
            if(p==null || !ReferenceEquals(p.Olympic,this) || state.GameId!=Flow.GameId || state.Version!=(Mode.HasValue?boundVersion:Flow.Revision)
                || !ReferenceEquals(p.Definition,Root.Definition) || !ReferenceEquals(state.Map,Root.Definition.Risk.Defcon.Map)
                || !ReferenceEquals(p.Risk,Risk) || !ReferenceEquals(Risk.Defcon,Flow.Current.Defcon) || !ReferenceEquals(Risk.Definition.Victory,Flow.Current.Victory)
                || !ReferenceEquals(state.Cards,Flow.SourceCards) || !ReferenceEquals(state.RandomState,Mode.HasValue?boundRandom:Flow.Current.Random)
                || !ReferenceEquals(p.Space,Root.Space) || !ReferenceEquals(p.OpsEffects,Root.OpsEffects) || !ReferenceEquals(p.Rounds,Root.Rounds)
                || !ReferenceEquals(p.Headline,Root.Headline) || p.HasPendingHeadline || p.OpponentOps!=null || p.Blockade!=null || p.War!=null || p.FiveYearPlan!=null
                || p.Actor!=Flow.Sponsor || p.VpForUS!=Flow.Current.VpForUS || !p.ActiveCard.HasValue || !p.ActiveCard.Value.Equals(Flow.Current.Definition.Card.Id)
                || state.Turn.Number!=Root.Space.Turn.Number || state.Turn.Era!=Root.Space.Turn.Era || !ReferenceEquals(state.Turn.Parameters,Root.Space.Turn.Parameters)
                || state.Cards.ResolvingCards.Count!=1 || !state.Cards.ResolvingCards.Contains(p.ActiveCard.Value)
                || state.Countries.Count!=RootCountries.Count)return false;
            var expectedCountries=Mode.HasValue?boundCountries:RootCountries;
            if(state.Countries.Any(c=>!expectedCountries.TryGetValue(c.Key,out var b) || c.Value.US!=b.US || c.Value.USSR!=b.USSR))return false;
            if(Mode.HasValue)return Flow.Stage==OlympicResolutionStage.AwaitingOperations && ReferenceEquals(state.Operations,Operations)
                && state.SpentOps==boundSpent && state.EventSequence==boundSequence
                && (Mode==OperationsMode.Influence?p.Stage==ActionPracticeStage.PlacingInfluence:p.Stage==ActionPracticeStage.Realigning);
            return Flow.Stage==OlympicResolutionStage.AwaitingOperations?p.Stage==ActionPracticeStage.ChoosingOperations
                :(Flow.Stage==OlympicResolutionStage.AwaitingOpponent || Flow.Stage==OlympicResolutionStage.Rolling) && p.Stage==ActionPracticeStage.ResolvingEvent;
        }
    }
    public enum OlympicPracticeOutcome { AwaitingChoice, Tie, ContestCompleted, AwaitingOperations, OperationsCompleted, OperationsDeclined, Interrupted }
    public sealed class OlympicPracticeResult
    {
        public OlympicResolution Flow { get; }
        public OlympicPracticeOutcome Outcome { get; }
        internal OlympicPracticeResult(OlympicResolution flow,OlympicPracticeOutcome outcome){Flow=flow;Outcome=outcome;}
    }
}
