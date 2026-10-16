using System;
using System.Linq;
namespace ColdWarStruggle.Core
{
    public static partial class ActionPracticeRules
    {
        static string WarResolutionId(GameState state)=>state.Practice.HasPendingHeadline?state.Practice.Headline.CurrentResolutionId:
            "practice.war.turn."+state.Turn.Number+".action."+state.Practice.CompletedActions;
        public static RuleError CanResolveWar(GameState state,PlayerSide side)
        {
            if(state==null || side!=PlayerSide.US && side!=PlayerSide.USSR)return RuleError.InvalidCommand;
            if(state.Ended)return RuleError.GameAlreadyEnded;
            var p=state.Practice;var choice=p?.War;if(choice==null)return RuleError.ActionNotReady;
            if(side!=choice.Owner)return RuleError.WrongActor;
            if(state.Deployment!=null)return RuleError.SetupInProgress;
            if(state.InfluenceEvent!=null)return RuleError.EventInProgress;
            if(p.Stage!=ActionPracticeStage.ResolvingEvent || !p.ActiveCard.HasValue || !p.ActiveCard.Value.Equals(choice.Effect.Card)
                || !choice.Matches(state) || choice.Id!=WarResolutionId(state) || state.Cards.ResolvingCards.Count!=1
                || !state.Cards.ResolvingCards.Contains(choice.Effect.Card) || state.Operations.Budget!=0 || state.SpentOps!=0
                || p.Blockade!=null || p.FiveYearPlan!=null || p.OpponentOps!=null)return RuleError.StaleContext;
            return RuleError.None;
        }
        static WarEventState BindWar(GameState state)
        {
            var p=state.Practice;var risk=p?.Risk;
            if(p?.Definition.Wars==null || risk==null || risk.Result!=null || state.Cards==null
                || !ReferenceEquals(p.Definition.Wars.Cards,state.Cards.Catalog) || !ReferenceEquals(p.Definition.Risk,risk.Definition)
                || risk.Military.Turn.Number!=state.Turn.Number || !ReferenceEquals(risk.Military.Turn.Parameters,state.Turn.Parameters))throw new ArgumentException("War must bind to live matching authority rules.");
            return new WarEventState(p.Definition.Wars,state.Countries,risk.Military,risk.Defcon,risk.Definition.Victory,p.VpForUS,state.RandomState);
        }
        static CardActionTransition BeginWar(GameState state,CardId card,bool alreadyResolving)
        {
            try
            {
                var p=state.Practice;
                if(p.War!=null || p.Blockade!=null || p.FiveYearPlan!=null || state.InfluenceEvent!=null
                    || (alreadyResolving?(!HasActivePhysicalCard(state) || !p.ActiveCard.Value.Equals(card)):state.Cards.ResolvingCards.Count!=0))return new CardActionTransition(RuleError.InvalidDefinition);
                var before=BindWar(state);var effect=WarEventRules.Effect(before.Catalog,card);
                var cards=alreadyResolving?state.Cards:Move(state,card,true,true);
                var choice=effect.Kind==WarEventKind.IndoPakistani?new WarDecision(state,cards,effect,before,WarResolutionId(state)):null;
                var pending=new ActionPracticeState(p.Definition,ActionPracticeStage.ResolvingEvent,p.CompletedActions,card,p.Space,p.VpForUS,p.Risk,p.OpsEffects,p.OpponentOps,rounds:p.Rounds,headline:p.Headline,war:choice);
                var prepared=Commit(state,pending,cards,state.RandomState).NextState;
                return choice!=null?new CardActionTransition(RuleError.None,prepared):CompleteWar(prepared,effect.Targets[0]);
            }
            catch(OverflowException){return new CardActionTransition(RuleError.NumericOverflow);}
            catch(ArgumentException){return new CardActionTransition(RuleError.InvalidDefinition);}
            catch(InvalidOperationException){return new CardActionTransition(RuleError.InvalidDefinition);}
        }
        static CardActionTransition PlanWarResponse(GameState state,PlayerSide side,string actionId,CardId? card,CountryId? target)
        {
            if(string.IsNullOrWhiteSpace(actionId) || actionId.Length>128 || card.HasValue || !target.HasValue)return new CardActionTransition(RuleError.InvalidCommand);
            var error=CanResolveWar(state,side);if(error!=RuleError.None)return new CardActionTransition(error);
            if(!state.Practice.War.Candidates.Contains(target.Value))return new CardActionTransition(RuleError.InvalidSelection);
            try {return CompleteWar(state,target.Value);}
            catch(OverflowException){return new CardActionTransition(RuleError.NumericOverflow);}
            catch(ArgumentException){return new CardActionTransition(RuleError.InvalidDefinition);}
            catch(InvalidOperationException){return new CardActionTransition(RuleError.InvalidDefinition);}
        }
        static CardActionTransition CompleteWar(GameState state,CountryId target)
        {
            var p=state.Practice;var card=p.ActiveCard.Value;
            // Only the closed supported practice runtime reaches here. It cannot create treaty/Flower Power facts.
            // Registering those handlers or importing external saves must first add authority-derived policies.
            var plan=WarEventService.Plan(p.War?.Before??BindWar(state),card,p.Actor,target,WarResolutionId(state),WarInterferencePolicy.NoActiveInterference,new Pcg32Random(state.RandomState));
            var risk=p.Risk.After(plan);var terminal=plan.Decision.Outcome==VictoryOutcome.Finished;
            var next=new ActionPracticeState(p.Definition,terminal?ActionPracticeStage.Complete:ActionPracticeStage.ResolvingEvent,p.CompletedActions,card,p.Space,plan.After.VpForUS,risk,p.OpsEffects,p.OpponentOps,rounds:p.Rounds,headline:p.Headline);
            var current=Commit(state,next,state.Cards,plan.After.Random,countries:plan.After.Countries).NextState;
            var done=terminal?new CardActionTransition(RuleError.None,current):Commit(current,Next(next,next.Space,next.VpForUS),Move(current,card,false,false,p.Definition.Cards.GetCard(card).RemoveAfterResolvedEvent),current.RandomState);
            return done.Accepted?new CardActionTransition(RuleError.None,done.NextState,warResult:plan):done;
        }
    }
}
