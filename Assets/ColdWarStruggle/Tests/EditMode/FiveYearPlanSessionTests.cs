using System;
using System.Linq;
using System.Threading.Tasks;
using ColdWarStruggle.Core;
using ColdWarStruggle.Application;
using ColdWarStruggle.Presentation;
using Newtonsoft.Json;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class FiveYearPlanSessionTests
    {
        static CardId Id(string n)=>FiveYearPlanPlayTests.Id(n);
        static CountryId C(string n)=>FiveYearPlanPlayTests.C(n);
        static CardActionCommand Card(IGameSession s,CardActionKind kind,CardId? card=null,string id="root")
        {var v=s.ReadView();return new CardActionCommand(id,v.GameId,v.Version,kind,card);}
        static EventCommandEnvelope Choice(IGameSession s,string id="choice",string target="yugoslavia")
        {var v=s.ReadView();return new EventCommandEnvelope(id,v.GameId,v.Version,EventCommandKind.Resolve,v.Event.ExecutionId,v.Event.DecisionId,new[]{C(target)});}
        static async Task<LocalGameHost> Pending()
        {var h=new LocalGameHost(FiveYearPlanPlayTests.Ready(),true);Assert.That((await h.UssrSession.SubmitCardActionAsync(Card(h.UssrSession,CardActionKind.Event,Id("five_year_plan")))).Accepted,Is.True);return h;}
        [Test] public async Task Public_result_exposes_only_selected_card_and_scalar_parent_context()
        {
            var s=PlayTurnOpsEventTests.Give(FiveYearPlanPlayTests.Ready(),PlayerSide.USSR,"card.fidel");s=PlayTurnOpsEventTests.Give(s,PlayerSide.USSR,"card.asia_scoring");
            var h=new LocalGameHost(s,true);var r=await h.UssrSession.SubmitCardActionAsync(Card(h.UssrSession,CardActionKind.Event,Id("five_year_plan")));Assert.That(r.Accepted,Is.True);Assert.That(r.FiveYearPlan,Is.Not.Null);
            Assert.That(typeof(PublicFiveYearPlanResult).GetProperties().Select(p=>p.Name),Is.EquivalentTo(new[]{"RootCard","SelectedCard","RootActor","Outcome"}));
            Assert.That(r.FiveYearPlan.RootCard,Is.EqualTo(Id("five_year_plan")));Assert.That(r.FiveYearPlan.RootActor,Is.EqualTo(PlayerSide.USSR));
            var json=JsonConvert.SerializeObject(h.UsSession.ReadView());var left=s.Cards.GetHand(PlayerSide.USSR).Where(x=>!x.Equals(Id("five_year_plan")) && !x.Equals(r.FiveYearPlan.SelectedCard.Value));
            foreach(var id in left)Assert.That(json,Does.Not.Contain(id.Value));
            Assert.That(json,Does.Not.Contain("RandomBefore").And.Not.Contain("RandomAfter").And.Not.Contain("CandidateCards"));
        }
        [Test] public async Task Pending_context_survives_viewer_switch_and_child_completion_returns_one_root()
        {
            var h=await Pending();var us=h.UsSession.ReadView();var su=h.UssrSession.ReadView();
            foreach(var v in new[]{us,su}){Assert.That(v.FiveYearPlan,Is.Not.Null);Assert.That(v.FiveYearPlan.SelectedCard,Is.EqualTo(Id("independent_reds")));Assert.That(v.FiveYearPlan.Outcome,Is.EqualTo(FiveYearPlanOutcome.AwaitingUsEvent));Assert.That(v.Practice.ActiveCard,Is.EqualTo(Id("five_year_plan")));Assert.That(v.Event.Owner,Is.EqualTo(PlayerSide.US));}
            var cmd=Choice(h.UsSession);var r=await h.UsSession.SubmitEventAsync(cmd);Assert.That(r.Accepted,Is.True,r.Error.ToString());Assert.That(r.FiveYearPlan,Is.Not.Null);Assert.That(r.FiveYearPlan.Outcome,Is.EqualTo(FiveYearPlanOutcome.UsEventResolved));
            Assert.That(r.View.FiveYearPlan,Is.Null);Assert.That(r.View.Event,Is.Null);Assert.That(r.View.Practice.CompletedActions,Is.EqualTo(1));Assert.That(r.View.Countries.Single(c=>c.Definition.Id.Equals(C("yugoslavia"))).US,Is.EqualTo(3));
            Assert.That(await h.UsSession.SubmitEventAsync(cmd),Is.SameAs(r));Assert.That(h.UsSession.ReadView().Version,Is.EqualTo(r.View.Version));Assert.That(h.UsSession.ReadView().EventSequence,Is.EqualTo(r.View.EventSequence));
        }
        [Test] public async Task Parent_retry_after_child_completion_returns_original_receipt_without_drawing_again()
        {
            var h=new LocalGameHost(FiveYearPlanPlayTests.Ready(),true);var cmd=Card(h.UssrSession,CardActionKind.Event,Id("five_year_plan"));var first=await h.UssrSession.SubmitCardActionAsync(cmd);
            Assert.That(first.FiveYearPlan,Is.Not.Null);var done=await h.UsSession.SubmitEventAsync(Choice(h.UsSession));Assert.That(done.Accepted,Is.True);
            Assert.That(await h.UssrSession.SubmitCardActionAsync(cmd),Is.SameAs(first));Assert.That(first.FiveYearPlan.Outcome,Is.EqualTo(FiveYearPlanOutcome.AwaitingUsEvent));Assert.That(h.UssrSession.ReadView().Version,Is.EqualTo(done.View.Version));Assert.That(h.UssrSession.ReadView().Practice.CompletedActions,Is.EqualTo(1));
        }
        [TestCase("owner",RuleError.WrongActor)][TestCase("version",RuleError.StaleVersion)][TestCase("game",RuleError.WrongGame)][TestCase("decision",RuleError.StaleDecision)]
        public async Task Invalid_choice_keeps_accepted_random_choice_and_does_not_publish_result(string kind,RuleError error)
        {
            var h=await Pending();var session=kind=="owner"?h.UssrSession:h.UsSession;var v=session.ReadView();
            var r=await session.SubmitEventAsync(new EventCommandEnvelope("bad",kind=="game"?"other":v.GameId,kind=="version"?v.Version-1:v.Version,EventCommandKind.Resolve,v.Event.ExecutionId,kind=="decision"?"old":v.Event.DecisionId,new[]{C("yugoslavia")}));
            Assert.That(r.Error,Is.EqualTo(error));Assert.That(r.FiveYearPlan,Is.Null);Assert.That(r.Changes,Is.Empty);Assert.That(r.View.Version,Is.EqualTo(v.Version));Assert.That(r.View.EventSequence,Is.EqualTo(v.EventSequence));Assert.That(r.View.FiveYearPlan,Is.Not.Null);Assert.That(r.View.Cards.ResolvingCount,Is.EqualTo(2));
        }
        [Test] public async Task Choice_command_conflict_cannot_apply_a_second_selection()
        {
            var h=await Pending();var c=Choice(h.UsSession);var first=await h.UsSession.SubmitEventAsync(c);Assert.That(first.Accepted,Is.True);Assert.That(first.FiveYearPlan,Is.Not.Null);
            var r=await h.UsSession.SubmitEventAsync(new EventCommandEnvelope(c.CommandId,c.GameId,c.ExpectedVersion,c.Kind,c.ExecutionId,c.DecisionId,new[]{C("poland")}));Assert.That(r.Error,Is.EqualTo(RuleError.CommandIdConflict));Assert.That(r.FiveYearPlan,Is.Null);Assert.That(r.View.Version,Is.EqualTo(first.View.Version));
        }
        [TestCase("asia_scoring",FiveYearPlanOutcome.Discarded)][TestCase("containment",FiveYearPlanOutcome.UsEventResolved)][TestCase(null,FiveYearPlanOutcome.NoCard)]
        public async Task Immediate_paths_have_public_summary_without_pending_context(string child,FiveYearPlanOutcome outcome)
        {
            var h=new LocalGameHost(FiveYearPlanPlayTests.Ready(child),true);var r=await h.UssrSession.SubmitCardActionAsync(Card(h.UssrSession,CardActionKind.Event,Id("five_year_plan")));
            Assert.That(r.Accepted,Is.True);Assert.That(r.FiveYearPlan,Is.Not.Null);Assert.That(r.FiveYearPlan.Outcome,Is.EqualTo(outcome));Assert.That(r.View.FiveYearPlan,Is.Null);Assert.That(r.View.Practice.CompletedActions,Is.EqualTo(1));if(child=="containment")Assert.That(r.ActivatedOpsEffect,Is.Not.Null);
        }
        [Test] public async Task Terminal_summary_keeps_parent_responsibility_and_defcon_receipt()
        {
            var h=new LocalGameHost(FiveYearPlanPlayTests.Ready("duck_and_cover",level:2),true);var r=await h.UssrSession.SubmitCardActionAsync(Card(h.UssrSession,CardActionKind.Event,Id("five_year_plan")));
            Assert.That(r.Accepted,Is.True);Assert.That(r.FiveYearPlan,Is.Not.Null);Assert.That(r.FiveYearPlan.Outcome,Is.EqualTo(FiveYearPlanOutcome.Interrupted));Assert.That(r.View.FiveYearPlan.Outcome,Is.EqualTo(FiveYearPlanOutcome.Interrupted));Assert.That(r.DefconVpEvent.Terminal,Is.True);Assert.That(r.View.Practice.Winner,Is.EqualTo(PlayerSide.US));Assert.That(r.View.Practice.CompletedActions,Is.Zero);
        }
        [TestCase("independent_reds")][TestCase("asia_scoring")] public async Task Coup_then_child_summary_retains_both_public_receipts(string child)
        {
            var s=FiveYearPlanPlayTests.Ready(child);var board=s.Countries.ToDictionary(x=>x.Key,x=>x.Value);board[C("angola")]=new CountryState(2,0);s=PlayScoringTests.Replace(s,board);var h=new LocalGameHost(s,true);
            Assert.That((await h.UssrSession.SubmitCardActionAsync(Card(h.UssrSession,CardActionKind.OpponentOpsFirst,Id("five_year_plan")))).Accepted,Is.True);var v=h.UssrSession.ReadView();var c=new CardActionCommand("coup",v.GameId,v.Version,CardActionKind.UseOpponentCoup,target:C("angola"));var r=await h.UssrSession.SubmitCardActionAsync(c);
            Assert.That(r.Accepted,Is.True);Assert.That(r.Coup,Is.Not.Null);Assert.That(r.FiveYearPlan,Is.Not.Null);Assert.That(r.FiveYearPlan.SelectedCard,Is.EqualTo(Id(child)));Assert.That(await h.UssrSession.SubmitCardActionAsync(c),Is.SameAs(r));
        }
        [Test] public async Task Headline_wait_and_completion_project_parent_child_and_following_root()
        {
            var h=new LocalGameHost(HeadlineFlowTests.Start(FiveYearPlanFlowTests.HeadlinePrepared()),true);Assert.That(HeadlineSessionTests.Submit(h.UsSession,"five_year_plan").Accepted,Is.True);var r=HeadlineSessionTests.Submit(h.UssrSession,"fidel");Assert.That(r.Accepted,Is.True);Assert.That(r.Steps.Single().FiveYearPlan,Is.Not.Null);Assert.That(r.Steps.Single().FiveYearPlan.Outcome,Is.EqualTo(FiveYearPlanOutcome.AwaitingUsEvent));
            var done=await h.UsSession.SubmitEventAsync(Choice(h.UsSession));Assert.That(done.Accepted,Is.True,done.Error.ToString());Assert.That(done.FiveYearPlan,Is.Not.Null);Assert.That(done.FiveYearPlan.Outcome,Is.EqualTo(FiveYearPlanOutcome.UsEventResolved));Assert.That(done.HeadlineSteps.Count,Is.EqualTo(2));Assert.That(done.HeadlineSteps[0].FiveYearPlan,Is.Not.Null);Assert.That(done.HeadlineSteps[1].FiveYearPlan,Is.Null);Assert.That(done.View.Headline.Status,Is.EqualTo(PublicHeadlineStatus.Completed));Assert.That(done.View.Practice.CompletedActions,Is.Zero);
        }
        [Test] public async Task Controller_side_switch_clears_target_draft_but_keeps_committed_child_then_reset_clears_retry()
        {
            var c=new DemoController(_=>new LocalGameHost(FiveYearPlanPlayTests.Ready(),true));c.LoadScenario("fixture",PlayerSide.USSR,C("yugoslavia"));c.SelectCard(Id("five_year_plan"));Assert.That((await c.PlayCardAsync(CardActionKind.Event)).Accepted,Is.True);Assert.That(c.ReadView().FiveYearPlan,Is.Not.Null);
            Assert.That(c.ToggleEventTarget(C("yugoslavia")),Is.False);c.SelectSide(PlayerSide.US);Assert.That(c.ToggleEventTarget(C("yugoslavia")),Is.True);c.SelectSide(PlayerSide.USSR);Assert.That(c.EventDraft,Is.Empty);c.SelectSide(PlayerSide.US);Assert.That(c.ReadView().FiveYearPlan.SelectedCard,Is.EqualTo(Id("independent_reds")));Assert.That(c.ToggleEventTarget(C("yugoslavia")),Is.True);var done=await c.ConfirmEventAsync();Assert.That(done.Accepted,Is.True);Assert.That(done.FiveYearPlan,Is.Not.Null);Assert.That(await c.RetryEventAsync(),Is.SameAs(done));
            c.LoadScenario("fixture",PlayerSide.USSR,C("yugoslavia"));Assert.That(c.ReadView().FiveYearPlan,Is.Null);Assert.That(c.LastEventResult,Is.Null);Assert.That(c.CanRetryEvent,Is.False);
        }
    }
}
