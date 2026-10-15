using System;
namespace ColdWarStruggle.Core
{
    public static partial class ActionPracticeRules
    {
        static string EventResolutionId(GameState state)=>state.Practice.HasPendingHeadline?state.Practice.Headline.CurrentResolutionId:"practice.event."+state.Practice.CompletedActions;
        internal static CardActionTransition OpenHeadlines(GameState state,HeadlineOrderParameters parameters)
        {
            var p=state.Practice;var queue=HeadlineResolutionState.CreateOrdinary(parameters,p.Definition.Cards,state.Cards,state.Turn);
            var pending=new ActionPracticeState(p.Definition,ActionPracticeStage.HeadlineReady,0,null,p.Space,p.VpForUS,p.Risk,p.OpsEffects,headline:queue);
            return Commit(state,pending,state.Cards,state.RandomState);
        }
        internal static CardActionTransition ResolveCurrentHeadline(GameState state)
        {
            var card=state.Practice.Headline.Current.Card;var printed=state.Practice.Definition.Cards.GetCard(card);
            return printed.Kind==CardKind.Scoring?ResolveScoringCard(state,card):ResolveEventCard(state,state.Practice.Actor,card);
        }
    }
}
