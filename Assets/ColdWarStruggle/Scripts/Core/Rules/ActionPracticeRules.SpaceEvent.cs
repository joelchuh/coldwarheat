using System;
using System.Collections.Generic;
using System.Linq;
namespace ColdWarStruggle.Core
{
    public static partial class ActionPracticeRules
    {
        // The caller authorizes a hand or headline source before dispatch; clients cannot request arbitrary advancement.
        static RuleError CheckSpaceEvent(GameState state,PlayerSide side,CardId card)
        {
            var p=state?.Practice;var d=p?.Definition.SpaceEvent;
            if(d==null || !d.Card.Id.Equals(card))return RuleError.UnsupportedCardAction;
            if(side!=PlayerSide.US && side!=PlayerSide.USSR)return RuleError.InvalidCommand;
            if(p.Risk==null || p.Risk.Result!=null || !ReferenceEquals(p.Risk.Definition,p.Definition.Risk)
                || !ReferenceEquals(d.Cards,state.Cards?.Catalog) || !ReferenceEquals(d.Track,p.Space.Track.Definition)
                || p.Space.Turn.Number!=state.Turn.Number || !ReferenceEquals(p.Space.Turn.Parameters,state.Turn.Parameters))return RuleError.InvalidDefinition;
            return p.Space.Track.PositionFor(side)==d.Track.FinalPosition?RuleError.EventRequiresRuling:RuleError.None;
        }
        internal static IReadOnlyList<CardId> SpaceEventHeadlineCandidates(GameState state,IReadOnlyList<CardId> supported)
        {
            var d=state.Practice.Definition.SpaceEvent;if(d==null)return supported;
            // One physical card has one holder. The filtered set remains authority-only; views project only their own hand.
            var blocked=new[]{PlayerSide.US,PlayerSide.USSR}.Any(side=>state.Cards.GetHand(side).Contains(d.Card.Id)
                && CheckSpaceEvent(state,side,d.Card.Id)!=RuleError.None);
            return blocked?Array.AsReadOnly(supported.Where(id=>!id.Equals(d.Card.Id)).ToArray()):supported;
        }
        static CardActionTransition PlanSpaceEvent(GameState state,PlayerSide side)
        {
            var p=state.Practice;var d=p.Definition.SpaceEvent;
            var error=CheckSpaceEvent(state,side,d.Card.Id);if(error!=RuleError.None)return new CardActionTransition(error);
            try
            {
                var source=p.HasPendingHeadline?p.Headline.CurrentResolutionId:PracticeSlotId(state);
                var request=new SpaceEventRequest("space-event."+source,source,state.Turn.Number,side,d);
                var plan=SpaceEventRules.PlanAdvance(p.Space,request);
                if(plan.AlreadyApplied)return new CardActionTransition(RuleError.StaleContext);
                var award=plan.Receipt.AwardVp;
                var decision=VictoryService.Evaluate(p.Risk.Definition.Victory,new ClosedVpBatchEvidence(request.Id,d.EffectId,p.VpForUS,
                    side==PlayerSide.US?award:0,side==PlayerSide.USSR?award:0));
                var risk=p.Risk.WithVictory(decision);var terminal=decision.Outcome==VictoryOutcome.Finished;
                var next=AfterImmediate(p,d.Card.Id,plan.After,decision.SignedVp,risk);
                var done=Commit(state,next,Move(state,d.Card.Id,true,terminal,!terminal && d.Card.RemoveAfterResolvedEvent),state.RandomState);
                return new CardActionTransition(RuleError.None,done.NextState,spaceEventResult:plan);
            }
            catch(OverflowException){return new CardActionTransition(RuleError.NumericOverflow);}
            catch(ArgumentException){return new CardActionTransition(RuleError.InvalidDefinition);}
            catch(InvalidOperationException){return new CardActionTransition(RuleError.InvalidDefinition);}
        }
    }
}
