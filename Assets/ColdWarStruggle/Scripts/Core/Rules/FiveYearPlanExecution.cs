using System;
namespace ColdWarStruggle.Core
{
    public enum FiveYearPlanOutcome { NoCard, Discarded, AwaitingUsEvent, UsEventResolved, Interrupted }
    public sealed class FiveYearPlanResult
    {
        public CardId RootCard { get; }
        public CardId? SelectedCard { get; }
        public PlayerSide RootActor { get; }
        public FiveYearPlanOutcome Outcome { get; }
        internal FiveYearPlanResult(FiveYearPlanExecution execution,FiveYearPlanOutcome outcome)
        {RootCard=execution.Definition.Card.Id;SelectedCard=execution.Selection.SelectedCard;RootActor=execution.Root.Actor;Outcome=outcome;}
    }
    // Authority-only continuation; the selection includes private zones.
    public sealed class FiveYearPlanExecution
    {
        public FiveYearPlanDefinition Definition { get; }
        public FiveYearPlanSelection Selection { get; }
        public ActionPracticeState Root { get; }
        public string GameId { get; }
        public GameTurnContext Turn { get; }
        public string RootResolutionId { get; }
        public string ChildResolutionId { get; }
        internal FiveYearPlanExecution(GameState state,FiveYearPlanSelection selection,string resolutionId)
        {
            if(state?.Practice?.Definition.FiveYearPlan==null || selection==null || string.IsNullOrWhiteSpace(resolutionId)
                || state.Practice.FiveYearPlan!=null || !ReferenceEquals(state.Cards,selection.Before) || !ReferenceEquals(state.RandomState,selection.RandomBefore)
                || state.Practice.Stage!=ActionPracticeStage.ResolvingEvent || !Nullable.Equals(state.Practice.ActiveCard,(CardId?)state.Practice.Definition.FiveYearPlan.Card.Id))
                throw new ArgumentException("Expected a bound Five Year Plan parent and fresh selection.");
            Definition=state.Practice.Definition.FiveYearPlan;Selection=selection;Root=state.Practice;GameId=state.GameId;Turn=state.Turn;
            RootResolutionId=resolutionId;ChildResolutionId=selection.SelectedCard.HasValue?resolutionId+".child."+selection.SelectedCard.Value.Value:null;
        }
        internal bool MatchesRoot(ActionPracticeState p)=>ReferenceEquals(p.Definition,Root.Definition) && Nullable.Equals(p.ActiveCard,Root.ActiveCard)
            && p.Actor==Root.Actor && ReferenceEquals(p.Rounds,Root.Rounds) && ReferenceEquals(p.Headline,Root.Headline)
            && ReferenceEquals(p.OpponentOps,Root.OpponentOps) && ReferenceEquals(p.Space,Root.Space)
            && (p.Stage==ActionPracticeStage.ResolvingEvent || p.Stage==ActionPracticeStage.Complete && p.Risk?.Result?.Outcome==VictoryOutcome.Finished);
        internal bool MatchesAuthority(string gameId,GameTurnContext turn,CardZones cards,RandomSnapshot random)
            =>gameId==GameId && turn!=null && turn.Number==Turn.Number && turn.Era==Turn.Era && ReferenceEquals(turn.Parameters,Turn.Parameters)
                && ReferenceEquals(cards,Selection.Cards) && ReferenceEquals(random,Selection.RandomAfter);
    }
}
