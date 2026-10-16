using System;
using System.Collections.Generic;
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
        internal OlympicPracticeExecution(GameState state,OlympicResolution flow)
        {Root=state.Practice;RootCountries=state.Countries;Flow=flow;Risk=state.Practice.Risk;}
        OlympicPracticeExecution(OlympicPracticeExecution parent,OlympicResolution flow,PracticeRiskState risk)
        {Root=parent.Root;RootCountries=parent.RootCountries;Flow=flow;Risk=risk;}
        internal OlympicPracticeExecution WithFlow(OlympicResolution flow,PracticeRiskState risk)=>new OlympicPracticeExecution(this,flow,risk);
        internal bool Matches(GameState state)
        {
            var p=state?.Practice;
            if(p==null || !ReferenceEquals(p.Olympic,this) || state.GameId!=Flow.GameId || state.Version!=Flow.Revision
                || !ReferenceEquals(p.Definition,Root.Definition) || !ReferenceEquals(state.Map,Root.Definition.Risk.Defcon.Map)
                || !ReferenceEquals(p.Risk,Risk) || !ReferenceEquals(Risk.Defcon,Flow.Current.Defcon) || !ReferenceEquals(Risk.Definition.Victory,Flow.Current.Victory)
                || !ReferenceEquals(state.Cards,Flow.SourceCards) || !ReferenceEquals(state.RandomState,Flow.Current.Random)
                || !ReferenceEquals(p.Space,Root.Space) || !ReferenceEquals(p.OpsEffects,Root.OpsEffects) || !ReferenceEquals(p.Rounds,Root.Rounds)
                || !ReferenceEquals(p.Headline,Root.Headline) || p.HasPendingHeadline || p.OpponentOps!=null || p.Blockade!=null || p.War!=null || p.FiveYearPlan!=null
                || p.Actor!=Flow.Sponsor || p.VpForUS!=Flow.Current.VpForUS || !p.ActiveCard.HasValue || !p.ActiveCard.Value.Equals(Flow.Current.Definition.Card.Id)
                || state.Turn.Number!=Root.Space.Turn.Number || state.Turn.Era!=Root.Space.Turn.Era || !ReferenceEquals(state.Turn.Parameters,Root.Space.Turn.Parameters)
                || state.Cards.ResolvingCards.Count!=1 || !state.Cards.ResolvingCards.Contains(p.ActiveCard.Value)
                || state.Countries.Count!=RootCountries.Count || state.Countries.Any(c=>!RootCountries.TryGetValue(c.Key,out var b) || c.Value.US!=b.US || c.Value.USSR!=b.USSR))return false;
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
