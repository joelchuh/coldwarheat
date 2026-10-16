using System;
using System.Linq;
using ColdWarStruggle.Core;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class WarContinuationTests
    {
        static CardId Id(string n)=>WarPlayTests.Id(n);
        static CountryId Country(string n)=>WarPlayTests.Country(n);
        [TestCase("korean_war",true)][TestCase("korean_war",false)][TestCase("arab_israeli_war",true)][TestCase("arab_israeli_war",false)]
        public void Opponent_event_and_ops_preserve_the_US_root_until_both_finish(string name,bool eventFirst)
        {
            var s=WarPlayTests.Ready(name,PlayerSide.US);var b=ActionPracticeRules.Plan(s,PlayerSide.US,eventFirst?CardActionKind.OpponentEventFirst:CardActionKind.OpponentOpsFirst,"root",Id(name));Assert.That(b.Accepted,Is.True,b.Error.ToString());
            if(eventFirst){Assert.That(b.WarResult,Is.Not.Null);Assert.That(b.NextState.Practice.Risk.Military.UssrEarned,Is.EqualTo(2));}
            else Assert.That(b.WarResult,Is.Null);
            Assert.That(b.NextState.Practice.Stage,Is.EqualTo(ActionPracticeStage.ChoosingOperations));Assert.That(b.NextState.Practice.Actor,Is.EqualTo(PlayerSide.US));Assert.That(b.NextState.Practice.CompletedActions,Is.EqualTo(1));
            var ops=ActionPracticeRules.Plan(b.NextState,PlayerSide.US,CardActionKind.UseOpponentInfluence,"ops");Assert.That(ops.Accepted,Is.True,ops.Error.ToString());Assert.That(ops.NextState.Operations.Budget,Is.EqualTo(2));
            var done=ActionPracticeRules.Plan(ops.NextState,PlayerSide.US,CardActionKind.FinishInfluence,"finish");Assert.That(done.Accepted,Is.True,done.Error.ToString());var n=done.NextState;
            if(!eventFirst)Assert.That(done.WarResult,Is.Not.Null);
            Assert.That(n.Practice.CompletedActions,Is.EqualTo(2));Assert.That(n.Practice.Actor,Is.EqualTo(PlayerSide.USSR));Assert.That(n.Practice.OpponentOps,Is.Null);Assert.That(n.Practice.Risk.Military.UssrEarned,Is.EqualTo(2));Assert.That(n.Practice.Risk.Military.UsEarned,Is.Zero);Assert.That(n.Cards.ResolvingCards,Is.Empty);
            Assert.That(name=="korean_war"?n.Cards.RemovedCards:n.Cards.DiscardPile,Has.Member(Id(name)));
        }
        [TestCase("korean_war")][TestCase("arab_israeli_war")] public void Event_first_victory_stops_before_US_ops(string name)
        {
            var s=WarPlayTests.Ready(name,PlayerSide.US,vp:-19);var r=ActionPracticeRules.Plan(s,PlayerSide.US,CardActionKind.OpponentEventFirst,"root",Id(name));Assert.That(r.Accepted,Is.True,r.Error.ToString());
            Assert.That(r.NextState.Ended,Is.True);Assert.That(r.NextState.Practice.CompletedActions,Is.EqualTo(1));Assert.That(r.NextState.Practice.Risk.Result.Winner,Is.EqualTo(PlayerSide.USSR));Assert.That(r.NextState.Cards.ResolvingCards,Has.Member(Id(name)));Assert.That(r.NextState.Operations.Budget,Is.Zero);Assert.That(r.WarResult.Calculation.Success,Is.True);
            Assert.That(ActionPracticeRules.Plan(r.NextState,PlayerSide.US,CardActionKind.UseOpponentInfluence,"no").Error,Is.EqualTo(RuleError.GameAlreadyEnded));
        }
        [TestCase(false)][TestCase(true)] public void Coup_then_war_keeps_both_results_but_nuclear_coup_never_starts_war(bool nuclear)
        {
            var s=WarPlayTests.Ready("korean_war",PlayerSide.US,level:nuclear?2:5);var target=Country("angola");var board=s.Countries.ToDictionary(x=>x.Key,x=>x.Value);board[target]=new CountryState(0,2);s=PlayScoringTests.Replace(s,board);
            var b=ActionPracticeRules.Plan(s,PlayerSide.US,CardActionKind.OpponentOpsFirst,"root",Id("korean_war"));Assert.That(b.Accepted,Is.True);
            var r=ActionPracticeRules.Plan(b.NextState,PlayerSide.US,CardActionKind.UseOpponentCoup,"coup",target:target);Assert.That(r.Accepted,Is.True,r.Error.ToString());Assert.That(r.CoupReceipt,Is.Not.Null);
            Assert.That(r.WarResult==null,Is.EqualTo(nuclear));Assert.That(r.NextState.Ended,Is.EqualTo(nuclear));Assert.That(r.NextState.Practice.Risk.Military.UssrEarned,Is.EqualTo(nuclear?0:2));Assert.That(r.NextState.Practice.Risk.Military.UsEarned,Is.EqualTo(2));Assert.That(r.NextState.Practice.CompletedActions,Is.EqualTo(nuclear?1:2));
            var rng=new Pcg32Random(s.RandomState);rng.NextInt(0,6);if(!nuclear)rng.NextInt(0,6);Pcg32RandomTests.Same(rng.Capture(),r.NextState.RandomState);
        }
        internal static GameState Headlines(string us,string su,int vp=0)
        {
            var s=DefectorsHeadlineResolutionTests.Prepared(us,su,collecting:true);var d=WarPlayTests.Bind(s.Practice.Definition);var board=s.Countries.ToDictionary(x=>x.Key,x=>x.Value);
            foreach(var n in new[]{"south_korea","israel","india","pakistan"})board[Country(n)]=new CountryState(3,2);board[Country("west_germany")]=new CountryState(1,2);
            s=new GameState(s.Map,board,s.Operations,s.GameId,s.Version,s.SpentOps,s.EventSequence,turn:s.Turn,deployment:s.Deployment,cards:s.Cards,randomState:WarPlayTests.Die(6),practice:ActionPracticeState.CreateAtTurn(d,1,vp));
            return HeadlineFlowTests.Start(s);
        }
        internal static HeadlineResolutionTransition SubmitBoth(GameState s,string us,string su)
        {
            var first=HeadlineFlowRules.Submit(s,PlayerSide.US,Id(us));Assert.That(first.Accepted,Is.True,first.Error.ToString());var second=HeadlineFlowRules.Submit(first.NextState,PlayerSide.USSR,Id(su));Assert.That(second.Accepted,Is.True,second.Error.ToString());return second;
        }
        [TestCase("korean_war")][TestCase("arab_israeli_war")] public void Fixed_headline_war_reports_result_and_continues_without_root_actions(string name)
        {
            var r=SubmitBoth(Headlines(name,"truman_doctrine"),name,"truman_doctrine");Assert.That(r.Steps.Count,Is.EqualTo(2));Assert.That(r.Steps[0].Result.WarResult,Is.Not.Null);Assert.That(r.NextState.Practice.Headline.CompletedCount,Is.EqualTo(1));Assert.That(r.NextState.InfluenceEvent,Is.Not.Null);Assert.That(r.NextState.Practice.CompletedActions,Is.Zero);Assert.That(r.NextState.Practice.Risk.Military.UssrEarned,Is.EqualTo(2));
        }
        [TestCase(PlayerSide.US)][TestCase(PlayerSide.USSR)] public void Indo_headline_target_response_continues_into_the_next_headline_choice(PlayerSide owner)
        {
            var us=owner==PlayerSide.US?"indo_pakistani_war":"truman_doctrine";var su=owner==PlayerSide.US?"truman_doctrine":"indo_pakistani_war";
            var s=Headlines(us,su);var opened=SubmitBoth(s,us,su);Assert.That(opened.NextState.Practice.War.Owner,Is.EqualTo(owner));Assert.That(opened.Steps.Count,Is.EqualTo(1));Assert.That(opened.NextState.Practice.Headline.CompletedCount,Is.Zero);Pcg32RandomTests.Same(s.RandomState,opened.NextState.RandomState);
            var resolved=WarPlayTests.Choose(opened.NextState);Assert.That(resolved.Accepted,Is.True,resolved.Error.ToString());Assert.That(resolved.HeadlineSteps.Count,Is.EqualTo(2));Assert.That(resolved.HeadlineSteps[0].Result.WarResult.Calculation.Beneficiary,Is.EqualTo(owner));
            var waiting=resolved.NextState;Assert.That(waiting.Practice.War,Is.Null);Assert.That(waiting.InfluenceEvent.Owner,Is.EqualTo(PlayerSide.US));Assert.That(waiting.Practice.Headline.CompletedCount,Is.EqualTo(1));Assert.That(waiting.Practice.CompletedActions,Is.Zero);
            var raw=InfluenceEventFlow.Resolve(waiting,PlayerSide.US,waiting.InfluenceEvent.DecisionId,new[]{Country("west_germany")});var done=HeadlineResolutionRules.CompleteChoice(raw);Assert.That(done.Accepted,Is.True,done.Error.ToString());Assert.That(done.NextState.Practice.Headline.IsComplete,Is.True);Assert.That(done.NextState.Practice.CompletedActions,Is.Zero);Assert.That(done.NextState.Practice.Actor,Is.EqualTo(PlayerSide.USSR));
        }
        [TestCase("korean_war")][TestCase("indo_pakistani_war")] public void Defectors_cancelled_war_has_no_choice_die_or_military(string war)
        {
            var s=Headlines("defectors",war);var r=SubmitBoth(s,"defectors",war);Assert.That(r.NextState.Practice.Headline.IsComplete,Is.True);Assert.That(r.NextState.Practice.War,Is.Null);Assert.That(r.Steps.All(step=>step.Result.WarResult==null),Is.True);Assert.That(r.NextState.Practice.Risk.Military.UssrEarned,Is.Zero);Assert.That(r.NextState.Cards.DiscardPile,Has.Member(Id(war)));Assert.That(r.NextState.Cards.RemovedCards,Has.No.Member(Id(war)));Pcg32RandomTests.Same(s.RandomState,r.NextState.RandomState);
        }
        [Test] public void Two_war_headlines_get_distinct_credits_and_exactly_two_draws()
        {
            var s=Headlines("korean_war","arab_israeli_war");var r=SubmitBoth(s,"korean_war","arab_israeli_war");Assert.That(r.NextState.Practice.Headline.IsComplete,Is.True);Assert.That(r.NextState.Practice.Risk.Military.UssrEarned,Is.EqualTo(4));Assert.That(r.NextState.Practice.Risk.Military.AppliedCredits.Count,Is.EqualTo(2));Assert.That(r.Steps.All(step=>step.Result.WarResult!=null),Is.True);Assert.That(r.NextState.Practice.CompletedActions,Is.Zero);
            var rng=new Pcg32Random(s.RandomState);rng.NextInt(0,6);rng.NextInt(0,6);Pcg32RandomTests.Same(rng.Capture(),r.NextState.RandomState);
        }
        [Test] public void Headline_victory_leaves_the_second_card_reserved_and_unexecuted()
        {
            var r=SubmitBoth(Headlines("korean_war","truman_doctrine",-19),"korean_war","truman_doctrine");Assert.That(r.NextState.Ended,Is.True);Assert.That(r.Steps.Count,Is.EqualTo(1));Assert.That(r.NextState.InfluenceEvent,Is.Null);Assert.That(r.NextState.Cards.GetHeadline(PlayerSide.USSR),Is.EqualTo(Id("truman_doctrine")));Assert.That(r.NextState.Practice.Headline.CompletedCount,Is.Zero);Assert.That(r.NextState.Cards.ResolvingCards,Has.Member(Id("korean_war")));
        }
        [Test] public void Following_headline_commit_failure_returns_no_partial_war_draft()
        {
            var opened=SubmitBoth(Headlines("indo_pakistani_war","truman_doctrine"),"indo_pakistani_war","truman_doctrine");var s=PlayScoringTests.Replace(opened.NextState,sequence:long.MaxValue-2);
            var r=WarPlayTests.Choose(s);Assert.That(r.Error,Is.EqualTo(RuleError.NumericOverflow));Assert.That(r.NextState,Is.Null);Assert.That(r.WarResult,Is.Null);Assert.That(r.HeadlineSteps,Is.Empty);Assert.That(s.Practice.Risk.Military.UsEarned,Is.Zero);Assert.That(s.GetInfluence(Country("india"),PlayerSide.USSR),Is.EqualTo(2));Assert.That(s.Practice.War,Is.Not.Null);Pcg32RandomTests.Same(s.RandomState,WarPlayTests.Die(6));
        }
        [Test] public void Same_action_slot_in_the_next_turn_does_not_reuse_old_war_credit()
        {
            var s=PlayCoupTests.Prepared();var d=s.Practice.Definition;
            d=WarPlayTests.Bind(new ActionPracticeDefinition(d.Cards,d.Ops,d.Space,d.Calendar,d.Risk,
                china:new ChinaCardDefinition(d.Cards,s.Map,ChinaCardPlayTests.China,new OpsRegionalBonus("china","region.asia",1)),turnEndEnabled:true,nextTurnEnabled:true));
            var first=HeadlineFlowTests.Start(new GameState(s.Map,s.Countries,s.Operations,s.GameId,s.Version,turn:s.Turn,deployment:s.Deployment,cards:s.Cards,randomState:s.RandomState,practice:ActionPracticeState.Create(d)));
            first=PlayTurnOpsEventTests.Give(first,PlayerSide.USSR,"card.arab_israeli_war");var previous=WarPlayTests.Play(first,"arab_israeli_war");Assert.That(previous.Accepted,Is.True,previous.Error.ToString());
            var assessed=NextTurnPlayTests.Assessed(previous.NextState);var start=NextTurnPlayTests.Next(assessed);Assert.That(start.Accepted,Is.True,start.Error.ToString());Assert.That(start.NextState.Turn.Number,Is.EqualTo(2));Assert.That(start.NextState.Practice.CompletedActions,Is.Zero);
            var next=PlayTurnOpsEventTests.Give(start.NextState,PlayerSide.USSR,"card.arab_israeli_war");var r=WarPlayTests.Play(next,"arab_israeli_war");Assert.That(r.Accepted,Is.True,r.Error.ToString());Assert.That(r.NextState.Practice.Risk.Military.UssrEarned,Is.EqualTo(2));Assert.That(r.NextState.Practice.Risk.Military.AppliedCredits.Count,Is.EqualTo(2));Assert.That(r.WarResult.Credit.Id,Is.Not.EqualTo(previous.WarResult.Credit.Id));
        }
    }
}
