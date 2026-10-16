using System;
namespace ColdWarStruggle.Core
{
    // Authority-only collection coordinator; session identity/version gates live in Application.
    public static class HeadlineFlowRules
    {
        static RuleError Check(GameState state)
        {
            if(state==null)return RuleError.InvalidCommand;
            if(state.Ended || state.Practice?.Risk?.Result?.Outcome==VictoryOutcome.Finished)return RuleError.GameAlreadyEnded;
            if(state.Practice==null || state.Cards==null)return RuleError.ActionNotReady;
            if(state.Deployment!=null)return RuleError.SetupInProgress;
            if(state.Practice.Definition.HeadlineParameters==null)return RuleError.UnsupportedCardAction;
            if(state.InfluenceEvent!=null || state.Operations.Budget!=0 || state.SpentOps!=0)return RuleError.ActionNotReady;
            return RuleError.None;
        }
        public static HeadlineResolutionTransition BeginCollection(GameState state)
        {
            var error=Check(state);if(error!=RuleError.None)return new HeadlineResolutionTransition(error);
            var p=state.Practice;
            if(p.Stage!=ActionPracticeStage.ChooseCard || p.CompletedActions!=0 || p.ActiveCard.HasValue || p.OpponentOps!=null
                || p.TurnEnd!=null || p.Headline!=null || p.HeadlineSelection!=null || state.Cards.HeadlineReserved.Count!=0
                || state.Cards.ResolvingCards.Count!=0)return new HeadlineResolutionTransition(RuleError.ActionNotReady);
            try
            {
                var selection=HeadlineSelectionState.Begin(p.Definition.Cards,state.Cards,p.Space.Track,ActionPracticeRules.SpaceEventHeadlineCandidates(state,HeadlineResolutionRules.SupportedCards(p.Definition)));
                return new HeadlineResolutionTransition(RuleError.None,Draft(state,selection));
            }
            catch(OverflowException){return new HeadlineResolutionTransition(RuleError.NumericOverflow);}
            catch(ArgumentException){return new HeadlineResolutionTransition(RuleError.InvalidDefinition);}
        }
        public static HeadlineResolutionTransition Submit(GameState state,PlayerSide side,CardId card)
        {
            if(side!=PlayerSide.US && side!=PlayerSide.USSR)return new HeadlineResolutionTransition(RuleError.InvalidCommand);
            var error=Check(state);if(error!=RuleError.None)return new HeadlineResolutionTransition(error);
            var p=state.Practice;
            if(p.Stage!=ActionPracticeStage.CollectingHeadline || p.HeadlineSelection==null)return new HeadlineResolutionTransition(RuleError.ActionNotReady);
            try
            {
                var result=HeadlineSelectionRules.PlanSelection(p.HeadlineSelection,side,card);
                if(!result.Accepted)return new HeadlineResolutionTransition(result.Error);
                var draft=Draft(state,result.State);
                return result.State.Ready?HeadlineResolutionRules.Begin(draft,p.Definition.HeadlineParameters):new HeadlineResolutionTransition(RuleError.None,draft);
            }
            catch(OverflowException){return new HeadlineResolutionTransition(RuleError.NumericOverflow);}
            catch(ArgumentException){return new HeadlineResolutionTransition(RuleError.InvalidDefinition);}
        }
        static GameState Draft(GameState state,HeadlineSelectionState selection)
        {
            var p=state.Practice;
            var next=new ActionPracticeState(p.Definition,selection.Ready?ActionPracticeStage.ChooseCard:ActionPracticeStage.CollectingHeadline,
                0,null,p.Space,p.VpForUS,p.Risk,p.OpsEffects,rounds:p.Rounds,headlineSelection:selection.Ready?null:selection);
            var ops=OperationsContext.FromRoundStart(state.GameId+".turn."+state.Turn.Number+".headline-selection",PlayerSide.USSR,0,state.Map,state.Countries,state.Operations.Costs);
            return state.WithPractice(next,ops,selection.Cards,state.RandomState);
        }
    }
    public static partial class ActionPracticeRules
    {
        static CardActionTransition CollectHeadlinesIfEnabled(CardActionTransition started)
        {
            if(!started.Accepted || started.NextState.Practice.Definition.HeadlineParameters==null)return started;
            var collection=HeadlineFlowRules.BeginCollection(started.NextState);
            return new CardActionTransition(collection.Error,collection.NextState);
        }
    }
}
