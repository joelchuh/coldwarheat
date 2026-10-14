using System;
using System.Linq;
using System.Collections.Generic;
using ColdWarStruggle.Core;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class TurnEndPlayTests
    {
        // Explicit all-era completed-turn fixture; never replaces the default deck.
        internal static GameState Ready(bool usHolds=false,bool ussrHolds=false,int vp=0,int usMilitary=5,int ussrMilitary=5,int completed=12,bool enabled=true,VictoryDefinition victory=null,bool effect=false)
        {
            var s=PlayScoringTests.Ready("card.olympic_games",vp,configure:d=> {
                var rd=d.Risk;var military=new MilitaryOperationsDefinition(Math.Min(usMilitary,ussrMilitary),rd.Military.ResetEarned,rd.Military.PenaltyVpPerShortfall,rd.Military.CreditSources);
                var risk=new PracticeRiskDefinition(rd.Coup,rd.Defcon,military,victory??rd.Victory);
                return new ActionPracticeDefinition(d.Cards,d.Ops,d.Space,d.Calendar,risk,d.Scoring,PlayInfluenceEventTests.Effects(rd.Defcon.Map,d.Cards),PlayDefconVpEventTests.Effects(d.Cards),PlayTurnOpsEventTests.Effects(d.Cards),RealignmentTests.Definition(),new ChinaCardDefinition(d.Cards,rd.Defcon.Map,ChinaCardPlayTests.China,new OpsRegionalBonus("china","region.asia",1)),enabled);
            });
            var board=s.Countries.ToDictionary(x=>x.Key,x=>x.Value);board[ChinaCardPlayTests.C("afghanistan")]=new CountryState(100,100);s=PlayScoringTests.Replace(s,board);
            for(int i=0;i<completed;i++)
            {
                var actor=s.Practice.Actor;var need=actor==PlayerSide.US?usMilitary-s.Practice.Risk.Military.UsEarned:ussrMilitary-s.Practice.Risk.Military.UssrEarned;
                if(effect && i==11) {s=PlayTurnOpsEventTests.Give(s,actor,"card.containment");var e=PlayTurnOpsEventTests.Play(s,"card.containment");Assert.That(e.Accepted,Is.True);s=e.NextState;}
                else if(need>0) {var card=actor==PlayerSide.US?"card.cia_created":"card.fidel";s=PlayTurnOpsEventTests.Give(s,actor,card);var coup=ActionPracticeRules.Plan(s,actor,CardActionKind.Coup,"fixture-coup-"+i,new CardId(card),ChinaCardPlayTests.C("afghanistan"));Assert.That(coup.Accepted,Is.True,coup.Error.ToString());s=coup.NextState;}
                else s=ChinaCardPlayTests.AdvanceOrdinary(s);
            }
            var d=s.Practice.Definition;
            var us=usHolds?new[]{new CardId("card.europe_scoring")}:Array.Empty<CardId>();var su=ussrHolds?new[]{new CardId("card.asia_scoring")}:Array.Empty<CardId>();
            var used=us.Concat(su).ToHashSet();var hands=new Dictionary<PlayerSide,IReadOnlyList<CardId>>{{PlayerSide.US,us},{PlayerSide.USSR,su}};
            var cards=new CardZones(d.Cards,d.Cards.Cards.Where(c=>c.Kind!=CardKind.China && !used.Contains(c.Id)).Select(c=>c.Id),hands,Array.Empty<CardId>(),Array.Empty<CardId>(),Array.Empty<CardId>(),Array.Empty<CardId>(),new ChinaCardState(ChinaCardPlayTests.China,PlayerSide.US,CardFace.FaceDown),new[]{GameEra.Early,GameEra.Mid,GameEra.Late});
            return PlayInfluenceEventTests.Copy(s,cards:cards);
        }
        internal static CardActionTransition End(GameState s,PlayerSide side=PlayerSide.US)=>ActionPracticeRules.Plan(s,side,CardActionKind.AssessTurnEnd,"assess");
        [TestCase(false,false,null,null)] [TestCase(false,false,"US","US")] [TestCase(false,false,"USSR","USSR")]
        [TestCase(true,false,null,"USSR")] [TestCase(true,false,"US","USSR")] [TestCase(true,false,"USSR","USSR")]
        [TestCase(false,true,null,"US")] [TestCase(false,true,"US","US")] [TestCase(false,true,"USSR","US")]
        [TestCase(true,true,null,"US")] [TestCase(true,true,"US","USSR")] [TestCase(true,true,"USSR","US")]
        public void Authoritative_hands_and_military_net_resolve_the_shared_twelve_row_matrix(bool us,bool su,string candidate,string winner)
        {
            var before=candidate=="US"?19:candidate=="USSR"?-19:0;
            var s=Ready(us,su,before,candidate=="USSR"?4:5,candidate=="US"?4:5,effect:true);var result=End(s);Assert.That(result.Accepted,Is.True,result.Error.ToString());var n=result.NextState;var r=n.Practice.TurnEnd;
            Assert.That(r,Is.Not.Null);Assert.That(r.MilitaryCandidate,Is.EqualTo(VictoryTests.Side(candidate)));Assert.That(r.Decision.Winner,Is.EqualTo(VictoryTests.Side(winner)));Assert.That(r.UsHolds,Is.EqualTo(us));Assert.That(r.UssrHolds,Is.EqualTo(su));Assert.That(r.BeforeVp,Is.EqualTo(before));
            Assert.That(n.Practice.VpForUS,Is.EqualTo(candidate=="US"?20:candidate=="USSR"?-20:0));Assert.That(n.Ended,Is.EqualTo(winner!=null));Assert.That(n.Practice.Risk.Military.UsEarned,Is.Zero);Assert.That(n.Practice.Risk.Military.UssrEarned,Is.Zero);Assert.That(n.Practice.Risk.Military.Assessment,Is.SameAs(r.Assessment));
            Assert.That(n.Cards.China.Face,Is.EqualTo(winner==null?CardFace.FaceUp:CardFace.FaceDown));Assert.That(n.Practice.OpsEffects.Instances.Count,Is.EqualTo(winner==null?0:1));Assert.That(r.ChinaFlipped,Is.EqualTo(winner==null));Assert.That(r.ExpiredOpsEffects,Is.EqualTo(winner==null?1:0));
            Assert.That(n.Cards.GetHand(PlayerSide.US),Is.EqualTo(s.Cards.GetHand(PlayerSide.US)));Assert.That(n.Cards.GetHand(PlayerSide.USSR),Is.EqualTo(s.Cards.GetHand(PlayerSide.USSR)));Assert.That(n.Cards.DrawPile,Is.EqualTo(s.Cards.DrawPile));Assert.That(n.Cards.DiscardPile,Is.EqualTo(s.Cards.DiscardPile));Assert.That(n.Turn,Is.SameAs(s.Turn));Assert.That(n.Practice.Space,Is.SameAs(s.Practice.Space));Assert.That(n.Practice.CompletedActions,Is.EqualTo(12));Assert.That(n.Version,Is.EqualTo(s.Version+1));Assert.That(n.EventSequence,Is.EqualTo(s.EventSequence+1));Pcg32RandomTests.Same(s.RandomState,n.RandomState);Assert.That(s.Practice.Risk.Military.Assessment,Is.Null);
        }
        [Test] public void Equal_shortfalls_net_once_without_temporary_twenty_point_victory()
        {var s=Ready(vp:19,usMilitary:0,ussrMilitary:0);var r=End(s);Assert.That(r.Accepted,Is.True);Assert.That(r.NextState.Ended,Is.False);Assert.That(r.NextState.Practice.VpForUS,Is.EqualTo(19));Assert.That(r.NextState.Practice.TurnEnd.Assessment.UsShortfall,Is.EqualTo(5));Assert.That(r.NextState.Practice.TurnEnd.Assessment.UssrShortfall,Is.EqualTo(5));}
        [TestCase(PlayerSide.US)] [TestCase(PlayerSide.USSR)]
        public void Either_fixed_identity_can_assess_only_after_all_actions(PlayerSide side)
        {Assert.That(ActionPracticeRules.CanAssessTurnEnd(Ready(),side),Is.EqualTo(RuleError.None));Assert.That(End(Ready(),side).Accepted,Is.True);Assert.That(End(Ready(completed:11),side).Error,Is.EqualTo(RuleError.ActionNotReady));}
        [Test] public void Unconfigured_invalid_and_repeated_requests_do_not_resume_actions()
        {Assert.That(End(Ready(enabled:false)).Error,Is.EqualTo(RuleError.UnsupportedCardAction));Assert.That(End(Ready(),(PlayerSide)99).Error,Is.EqualTo(RuleError.InvalidCommand));var s=End(Ready()).NextState;Assert.That(s,Is.Not.Null);Assert.That(End(s).Error,Is.EqualTo(RuleError.ActionSliceComplete));Assert.That(ActionPracticeRules.CanPlay(s,PlayerSide.USSR,ChinaCardPlayTests.China,CardActionKind.Influence),Is.EqualTo(RuleError.ActionSliceComplete));}
        [TestCase(true)] [TestCase(false)]
        public void Publication_overflow_keeps_penalties_flip_and_expiry_unpublished(bool version)
        {var s=Ready(effect:true,usMilitary:0);s=PlayInfluenceEventTests.Copy(s,version:version?long.MaxValue:s.Version,sequence:version?s.EventSequence:long.MaxValue);var r=End(s);Assert.That(r.Error,Is.EqualTo(RuleError.NumericOverflow));Assert.That(r.NextState,Is.Null);Assert.That(s.Practice.Risk.Military.Assessment,Is.Null);Assert.That(s.Cards.China.Face,Is.EqualTo(CardFace.FaceDown));Assert.That(s.Practice.OpsEffects.Instances.Count,Is.EqualTo(1));}
        [Test] public void Pending_adjudication_rejects_every_partial_side_effect()
        {var rows=VictoryTests.Rows();var old=rows.Single(x=>!x.UsHolds && !x.UssrHolds && x.MilitaryCandidate==null);rows.Remove(old);rows.Add(new HeldScoringRow(old.Id,false,false,null,VictoryOutcome.UnresolvedAdjudication,null,"pending","pending"));var s=Ready(victory:new VictoryDefinition(0,20,rows),effect:true);var r=End(s);Assert.That(r.Error,Is.EqualTo(RuleError.EventRequiresRuling));Assert.That(r.NextState,Is.Null);Assert.That(s.Practice.Risk.Military.Assessment,Is.Null);}
        [Test] public void Card_target_or_blank_id_are_rejected_as_foreign_inputs()
        {var s=Ready();Assert.That(ActionPracticeRules.Plan(s,PlayerSide.US,CardActionKind.AssessTurnEnd," ").Error,Is.EqualTo(RuleError.InvalidCommand));Assert.That(ActionPracticeRules.Plan(s,PlayerSide.US,CardActionKind.AssessTurnEnd,"id",ChinaCardPlayTests.China).Error,Is.EqualTo(RuleError.InvalidCommand));Assert.That(ActionPracticeRules.Plan(s,PlayerSide.US,CardActionKind.AssessTurnEnd,"id",target:ChinaCardPlayTests.C("japan")).Error,Is.EqualTo(RuleError.InvalidCommand));}
        [Test] public void Completed_action_counter_cannot_hide_a_resolving_physical_card()
        {
            var s=Ready();var c=s.Cards;var id=c.DrawPile.First();var hands=new Dictionary<PlayerSide,IReadOnlyList<CardId>>{{PlayerSide.US,c.GetHand(PlayerSide.US)},{PlayerSide.USSR,c.GetHand(PlayerSide.USSR)}};
            var zones=new CardZones(s.Practice.Definition.Cards,c.DrawPile.Skip(1),hands,c.EraReserve,c.DiscardPile,c.RemovedCards,new[]{id},c.China,c.EnteredEras);
            s=PlayInfluenceEventTests.Copy(s,cards:zones);Assert.That(End(s).Error,Is.EqualTo(RuleError.ActionNotReady));Assert.That(s.Cards.ResolvingCards,Has.Member(id));
        }
        [Test] public void Completed_action_counter_cannot_hide_a_pending_event()
        {
            var s=Ready();var effect=s.Practice.Definition.InfluenceEvents.Effects[new CardId("card.fidel")];
            var pending=new InfluenceEventExecution("pending",effect,PlayerSide.USSR,InfluenceEventPolicy.NoActiveInterference);
            var altered=new GameState(s.Map,s.Countries,s.Operations,s.GameId,s.Version,s.SpentOps,s.EventSequence,influenceEvent:pending,turn:s.Turn,cards:s.Cards,randomState:s.RandomState,practice:s.Practice);
            Assert.That(End(altered).Error,Is.EqualTo(RuleError.EventInProgress));
        }
        [Test] public void Already_face_up_China_is_preserved_without_fake_flip_or_other_zone_changes()
        {var s=ChinaCardPlayTests.SetChina(Ready(),PlayerSide.USSR,CardFace.FaceUp);var result=End(s);Assert.That(result.Accepted,Is.True);Assert.That(result.NextState.Cards,Is.SameAs(s.Cards));Assert.That(result.NextState.Practice.TurnEnd.ChinaFlipped,Is.False);Assert.That(result.NextState.Practice.TurnEnd.ExpiredOpsEffects,Is.Zero);}
        [Test] public void Enabling_audit_requires_explicit_risk_and_China_dependencies()
        {var d=ChinaCardPlayTests.Ready().Practice.Definition;Assert.Throws<ArgumentException>(()=>new ActionPracticeDefinition(d.Cards,d.Ops,d.Space,d.Calendar,turnEndEnabled:true));Assert.Throws<ArgumentException>(()=>new ActionPracticeDefinition(d.Cards,d.Ops,d.Space,d.Calendar,d.Risk,turnEndEnabled:true));}
        [Test] public void Prior_nuclear_terminal_absorbs_turn_end_request()
        {var s=ChinaCardPlayTests.Use(ChinaCardPlayTests.Ready(level:2),CardActionKind.Coup,ChinaCardPlayTests.C("angola")).NextState;Assert.That(End(s).Error,Is.EqualTo(RuleError.GameAlreadyEnded));}
    }
}
