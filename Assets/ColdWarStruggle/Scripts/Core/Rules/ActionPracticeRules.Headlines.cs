using System;
namespace ColdWarStruggle.Core
{
    public static partial class ActionPracticeRules
    {
        static string EventResolutionId(GameState state)=>state.Practice.HasPendingHeadline?state.Practice.Headline.CurrentResolutionId:"practice.event."+state.Practice.CompletedActions;
        internal static CardActionTransition OpenHeadlines(GameState state,HeadlineOrderParameters parameters)
        {
            var p=state.Practice;var queue=HeadlineResolutionState.CreateSelected(parameters,p.Definition.Cards,state.Cards,state.Turn,p.Definition.HeadlineDefectors);
            var pending=new ActionPracticeState(p.Definition,ActionPracticeStage.HeadlineReady,0,null,p.Space,p.VpForUS,p.Risk,p.OpsEffects,headline:queue);
            return Commit(state,pending,state.Cards,state.RandomState);
        }
        static ActionPracticeState AfterHeadline(ActionPracticeState p,HeadlineResolutionState queue,SpaceAttemptState space,int vp,PracticeRiskState risk=null,TurnOpsEffectState opsEffects=null)
            =>new ActionPracticeState(p.Definition,queue.IsComplete?ActionPracticeStage.ChooseCard:ActionPracticeStage.HeadlineReady,
                p.CompletedActions,null,space,vp,risk??p.Risk,opsEffects??p.OpsEffects,headline:queue);
        internal static CardActionTransition ResolveCurrentHeadline(GameState state)
        {
            var p=state.Practice;var card=p.Headline.Current.Card;
            if(p.Headline.CurrentCancellationSource.HasValue)
            {
                var queue=p.Headline.AdvanceAfterCancellation(card,p.Actor);
                return Commit(state,AfterHeadline(p,queue,p.Space,p.VpForUS),Move(state,card,true,false),state.RandomState);
            }
            if(p.Definition.HeadlineDefectors?.Card.Equals(card)==true)
                return Commit(state,Next(p,p.Space,p.VpForUS),Move(state,card,true,false),state.RandomState);
            var printed=p.Definition.Cards.GetCard(card);
            return printed.Kind==CardKind.Scoring?ResolveScoringCard(state,card):ResolveEventCard(state,state.Practice.Actor,card);
        }
    }
}
