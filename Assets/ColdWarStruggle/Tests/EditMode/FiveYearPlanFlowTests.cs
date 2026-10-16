using System;
using System.Linq;
using ColdWarStruggle.Core;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class FiveYearPlanFlowTests
    {
        static CardId Id(string n)=>FiveYearPlanPlayTests.Id(n);
        static CountryId C(string n)=>FiveYearPlanPlayTests.C(n);
        [TestCase(true,"independent_reds")][TestCase(false,"independent_reds")]
        [TestCase(true,"containment")][TestCase(false,"containment")]
        [TestCase(true,"asia_scoring")][TestCase(false,"asia_scoring")]
        public void Opponent_sequence_keeps_one_root_and_only_returns_to_its_remaining_step(bool first,string child)
        {
            var s=FiveYearPlanPlayTests.Ready(child);var a=PlayOpponentInfluenceTests.Start(s,"card.five_year_plan",first);Assert.That(a.Accepted,Is.True,a.Error.ToString());
            var entered=first?a:PlayOpponentInfluenceTests.Finish(a.NextState);Assert.That(entered.Accepted,Is.True,entered.Error.ToString());Assert.That(entered.FiveYearPlanResult,Is.Not.Null);
            var current=entered.NextState;
            if(child=="independent_reds")
            {
                Assert.That(current.Practice.Actor,Is.EqualTo(PlayerSide.USSR));Assert.That(current.Practice.OpponentOps.CurrentStep,Is.EqualTo(CardResolutionStep.Event));
                var choice=FiveYearPlanPlayTests.Choose(current);Assert.That(choice.Accepted,Is.True,choice.Error.ToString());current=choice.NextState;
            }
            Assert.That(current.RandomState.DrawCount,Is.EqualTo(s.RandomState.DrawCount+1));
            if(first)
            {
                Assert.That(current.Practice.CompletedActions,Is.Zero);Assert.That(current.Practice.OpponentOps.EventOccurred,Is.True);Assert.That(current.Practice.OpponentOps.CurrentStep,Is.EqualTo(CardResolutionStep.Operations));
                Assert.That(current.Operations.Budget,Is.EqualTo(3));Assert.That(current.Cards.ResolvingCards,Is.EqualTo(new[]{Id("five_year_plan")}));
                var done=PlayOpponentInfluenceTests.Finish(current);Assert.That(done.Accepted,Is.True);current=done.NextState;
            }
            Assert.That(current.Practice.CompletedActions,Is.EqualTo(1));Assert.That(current.Practice.OpponentOps,Is.Null);Assert.That(current.Practice.FiveYearPlan,Is.Null);Assert.That(current.Cards.ResolvingCards,Is.Empty);Assert.That(current.Cards.DiscardPile,Has.Member(Id("five_year_plan")));
        }
        static GameState BeforeCoup(string child="independent_reds",int level=5)
        {
            var s=FiveYearPlanPlayTests.Ready(child,level:level);var board=s.Countries.ToDictionary(x=>x.Key,x=>x.Value);board[C("angola")]=new CountryState(2,0);s=PlayScoringTests.Replace(s,board);
            var first=ActionPracticeRules.Plan(s,PlayerSide.USSR,CardActionKind.OpponentOpsFirst,"root",Id("five_year_plan"));Assert.That(first.Accepted,Is.True,first.Error.ToString());return first.NextState;
        }
        [TestCase("independent_reds")][TestCase("asia_scoring")] public void Coup_then_random_event_retains_both_receipts_and_ordered_random_state(string child)
        {
            var s=BeforeCoup(child);var r=ActionPracticeRules.Plan(s,PlayerSide.USSR,CardActionKind.UseOpponentCoup,"coup",target:C("angola"));Assert.That(r.Accepted,Is.True,r.Error.ToString());
            Assert.That(r.CoupReceipt,Is.Not.Null);Assert.That(r.FiveYearPlanResult,Is.Not.Null);Assert.That(r.FiveYearPlanResult.SelectedCard,Is.EqualTo(Id(child)));Assert.That(r.NextState.Practice.Risk.Defcon.Level,Is.EqualTo(4));
            var expected=new Pcg32Random(s.RandomState);expected.NextInt(0,6);expected.NextInt(0,1);Pcg32RandomTests.Same(expected.Capture(),r.NextState.RandomState);
            if(child=="independent_reds"){Assert.That(r.EventTransition,Is.Not.Null);var choice=FiveYearPlanPlayTests.Choose(r.NextState);Assert.That(choice.Accepted,Is.True);Assert.That(choice.NextState.Practice.CompletedActions,Is.EqualTo(1));}
            else Assert.That(r.NextState.Practice.CompletedActions,Is.EqualTo(1));
        }
        [Test] public void Nuclear_coup_stops_before_five_year_random_selection()
        {
            var s=BeforeCoup(level:2);var r=ActionPracticeRules.Plan(s,PlayerSide.USSR,CardActionKind.UseOpponentCoup,"nuclear",target:C("angola"));Assert.That(r.Accepted,Is.True,r.Error.ToString());
            Assert.That(r.NextState.Ended,Is.True);Assert.That(r.NextState.Practice.Risk.Result.Winner,Is.EqualTo(PlayerSide.US));Assert.That(r.FiveYearPlanResult,Is.Null);Assert.That(r.NextState.Cards.GetHand(PlayerSide.USSR),Has.Member(Id("independent_reds")));
            Assert.That(r.NextState.Cards.ResolvingCards,Is.EqualTo(new[]{Id("five_year_plan")}));var expected=new Pcg32Random(s.RandomState);expected.NextInt(0,6);Pcg32RandomTests.Same(expected.Capture(),r.NextState.RandomState);
        }
        [Test] public void Unsupported_child_after_coup_rejects_only_this_command_draft()
        {
            var s=BeforeCoup("marshall_plan");var r=ActionPracticeRules.Plan(s,PlayerSide.USSR,CardActionKind.UseOpponentCoup,"coup",target:C("angola"));Assert.That(r.Error,Is.EqualTo(RuleError.UnsupportedCardAction));Assert.That(r.NextState,Is.Null);
            Assert.That(s.Practice.Risk.Defcon.Level,Is.EqualTo(5));Assert.That(s.GetInfluence(C("angola"),PlayerSide.US),Is.EqualTo(2));Assert.That(s.Practice.Stage,Is.EqualTo(ActionPracticeStage.ChoosingOperations));Assert.That(s.Cards.ResolvingCards,Is.EqualTo(new[]{Id("five_year_plan")}));
        }
        [Test] public void Realignment_finish_enters_child_and_returns_to_one_root_completion()
        {
            var s=FiveYearPlanPlayTests.Ready();var first=ActionPracticeRules.Plan(s,PlayerSide.USSR,CardActionKind.OpponentOpsFirst,"root",Id("five_year_plan"));Assert.That(first.Accepted,Is.True);
            var mode=ActionPracticeRules.Plan(first.NextState,PlayerSide.USSR,CardActionKind.UseOpponentRealignment,"mode");Assert.That(mode.Accepted,Is.True,mode.Error.ToString());
            var finish=ActionPracticeRules.Plan(mode.NextState,PlayerSide.USSR,CardActionKind.FinishRealignment,"finish");Assert.That(finish.Accepted,Is.True,finish.Error.ToString());Assert.That(finish.FiveYearPlanResult.Outcome,Is.EqualTo(FiveYearPlanOutcome.AwaitingUsEvent));
            var done=FiveYearPlanPlayTests.Choose(finish.NextState);Assert.That(done.Accepted,Is.True,done.Error.ToString());Assert.That(done.NextState.Practice.CompletedActions,Is.EqualTo(1));Assert.That(done.NextState.Cards.ResolvingCards,Is.Empty);
        }
        internal static GameState HeadlinePrepared(string us="five_year_plan",string su="fidel",string child="independent_reds",int level=5)
        {
            var s=HeadlineFlowTests.Prepared(us,su,level:level);var definition=FiveYearPlanPlayTests.Bind(s.Practice.Definition);
            s=new GameState(s.Map,s.Countries,s.Operations,s.GameId,s.Version,s.SpentOps,s.EventSequence,turn:s.Turn,deployment:s.Deployment,cards:s.Cards,randomState:s.RandomState,practice:ActionPracticeState.Create(definition));
            if(child!=null)s=PlayTurnOpsEventTests.Give(s,PlayerSide.USSR,"card."+child);
            var board=s.Countries.ToDictionary(x=>x.Key,x=>x.Value);board[C("yugoslavia")]=new CountryState(1,3);return PlayInfluenceEventTests.Copy(s,board);
        }
        internal static HeadlineResolutionTransition OpenHeadline(GameState s,string us="five_year_plan",string su="fidel")
        {
            s=HeadlineFlowTests.Start(s);var first=HeadlineFlowRules.Submit(s,PlayerSide.US,Id(us));Assert.That(first.Accepted,Is.True,first.Error.ToString());return HeadlineFlowRules.Submit(first.NextState,PlayerSide.USSR,Id(su));
        }
        [TestCase(true)][TestCase(false)] public void Headline_child_choice_finishes_current_entry_then_resumes_queue_without_action_slot(bool parentFirst)
        {
            var us=parentFirst?"five_year_plan":"nuclear_test_ban";var su=parentFirst?"fidel":"five_year_plan";
            var r=OpenHeadline(HeadlinePrepared(us,su),us,su);Assert.That(r.Accepted,Is.True,r.Error.ToString());var s=r.NextState;
            Assert.That(s.Practice.Headline.CompletedCount,Is.EqualTo(parentFirst?0:1));Assert.That(s.Practice.Actor,Is.EqualTo(parentFirst?PlayerSide.US:PlayerSide.USSR));Assert.That(s.InfluenceEvent.Owner,Is.EqualTo(PlayerSide.US));Assert.That(r.Steps.Last().Result.FiveYearPlanResult.Outcome,Is.EqualTo(FiveYearPlanOutcome.AwaitingUsEvent));
            var raw=InfluenceEventFlow.Resolve(s,PlayerSide.US,s.InfluenceEvent.DecisionId,new[]{C("yugoslavia")});var done=HeadlineResolutionRules.CompleteChoice(raw);Assert.That(done.Accepted,Is.True,done.Error.ToString());
            Assert.That(done.Steps[0].Card,Is.EqualTo(Id("five_year_plan")));Assert.That(done.Steps[0].Result.FiveYearPlanResult.Outcome,Is.EqualTo(FiveYearPlanOutcome.UsEventResolved));Assert.That(done.NextState.Practice.Headline.IsComplete,Is.True);Assert.That(done.NextState.Practice.CompletedActions,Is.Zero);Assert.That(done.NextState.Cards.ResolvingCards,Is.Empty);Assert.That(done.NextState.Practice.Actor,Is.EqualTo(PlayerSide.USSR));Pcg32RandomTests.Same(s.RandomState,done.NextState.RandomState);
        }
        [Test] public void Headline_reserved_card_is_excluded_when_USSR_hand_is_otherwise_empty()
        {
            var r=OpenHeadline(HeadlinePrepared(child:null));Assert.That(r.Accepted,Is.True,r.Error.ToString());Assert.That(r.Steps[0].Result.FiveYearPlanResult.Outcome,Is.EqualTo(FiveYearPlanOutcome.NoCard));Assert.That(r.NextState.Practice.Headline.IsComplete,Is.True);Assert.That(r.NextState.Cards.RemovedCards,Has.Member(Id("fidel")));
        }
        [Test] public void Terminal_headline_child_preserves_other_reserved_card_and_parent_responsibility()
        {
            var r=OpenHeadline(HeadlinePrepared(child:"duck_and_cover",level:2));Assert.That(r.Accepted,Is.True,r.Error.ToString());Assert.That(r.NextState.Ended,Is.True);Assert.That(r.NextState.Practice.Risk.Result.Winner,Is.EqualTo(PlayerSide.USSR));
            Assert.That(r.NextState.Cards.GetHeadline(PlayerSide.USSR),Is.EqualTo(Id("fidel")));Assert.That(r.NextState.Practice.Headline.CompletedCount,Is.Zero);Assert.That(r.Steps.Single().Result.FiveYearPlanResult.Outcome,Is.EqualTo(FiveYearPlanOutcome.Interrupted));
        }
        [Test] public void Following_headline_failure_discards_entire_choice_draft_but_keeps_accepted_random_selection()
        {
            var prepared=HeadlinePrepared(su:"nasser");var board=prepared.Countries.ToDictionary(x=>x.Key,x=>x.Value);
            board[C("egypt")]=new CountryState(0,int.MaxValue);
            prepared=PlayInfluenceEventTests.Copy(prepared,board);var r=OpenHeadline(prepared,su:"nasser");Assert.That(r.Accepted,Is.True,r.Error.ToString());var s=r.NextState;
            var raw=InfluenceEventFlow.Resolve(s,PlayerSide.US,s.InfluenceEvent.DecisionId,new[]{C("yugoslavia")});Assert.That(raw.Accepted,Is.True);var done=HeadlineResolutionRules.CompleteChoice(raw);
            Assert.That(done.Error,Is.EqualTo(RuleError.NumericOverflow));Assert.That(done.NextState,Is.Null);Assert.That(done.Steps,Is.Empty);Assert.That(s.Practice.Headline.CompletedCount,Is.Zero);Assert.That(s.Practice.FiveYearPlan.Selection.SelectedCard,Is.EqualTo(Id("independent_reds")));Assert.That(s.Cards.ResolvingCards.Count,Is.EqualTo(2));
        }
    }
}
