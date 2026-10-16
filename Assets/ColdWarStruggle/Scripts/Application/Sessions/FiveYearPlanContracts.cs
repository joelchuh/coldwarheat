using ColdWarStruggle.Core;
namespace ColdWarStruggle.Application
{
    // Only publicly revealed card identity and progress; never retains an authority selection.
    public sealed class PublicFiveYearPlanResult
    {
        public CardId RootCard { get; }
        public CardId? SelectedCard { get; }
        public PlayerSide RootActor { get; }
        public FiveYearPlanOutcome Outcome { get; }
        internal PublicFiveYearPlanResult(FiveYearPlanResult result)
        {RootCard=result.RootCard;SelectedCard=result.SelectedCard;RootActor=result.RootActor;Outcome=result.Outcome;}
        internal PublicFiveYearPlanResult(FiveYearPlanExecution pending,bool terminal)
        {RootCard=pending.Definition.Card.Id;SelectedCard=pending.Selection.SelectedCard;RootActor=pending.Root.Actor;Outcome=terminal?FiveYearPlanOutcome.Interrupted:FiveYearPlanOutcome.AwaitingUsEvent;}
    }
}
