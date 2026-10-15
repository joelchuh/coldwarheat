using System;
using System.Collections.Generic;
using System.Linq;
using ColdWarStruggle.Core;
using ColdWarStruggle.Application;
using Newtonsoft.Json;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class DefectorsHeadlineSessionTests
    {
        static CardId Id(string n)=>HeadlineFlowTests.Id(n);
        static LocalGameHost Host(string us="defectors",string su="fidel",int level=5)
            =>new LocalGameHost(HeadlineFlowTests.Start(DefectorsHeadlineResolutionTests.Prepared(us,su,collecting:true,level:level)),true);
        static HeadlineCommandResult Submit(IGameSession s,string card,string id="headline")=>HeadlineSessionTests.Submit(s,card,id);
        static HeadlineCommandResult Both(LocalGameHost h,string us,string su)
        {Assert.That(Submit(h.UsSession,us).Accepted,Is.True);var r=Submit(h.UssrSession,su);Assert.That(r.Accepted,Is.True,r.Error.ToString());return r;}
        [Test] public void First_defectors_selection_does_not_reveal_priority_or_private_card()
        {
            var h=Host();var own=Submit(h.UsSession,"defectors");Assert.That(own.Accepted,Is.True);var v=h.UssrSession.ReadView();
            Assert.That(v.Headline.OrderStrategy,Is.Null);Assert.That(v.Headline.CompletedRoots,Is.Empty);Assert.That(v.Headline.Order,Is.Empty);Assert.That(v.Headline.Selection.OpponentCard,Is.Null);
            Assert.That(JsonConvert.SerializeObject(v),Does.Not.Contain("card.defectors"));Assert.That(own.Steps,Is.Empty);
        }
        [TestCase("red_scare_purge")][TestCase("fidel")][TestCase("europe_scoring")]
        public void Revealed_priority_and_cancelled_star_or_scoring_are_distinct_public_results(string su)
        {
            var h=Host(su:su);var r=Both(h,"defectors",su);Assert.That(r.View.Headline.OrderStrategy,Is.EqualTo(HeadlineOrderStrategy.USSelectedDefectorsFirst));
            Assert.That(r.View.Headline.CompletedRoots,Has.Count.EqualTo(2));Assert.That(r.Steps[0].Completion.Outcome,Is.EqualTo(HeadlineRootOutcome.Resolved));
            var target=r.Steps[1];Assert.That(target.Completion.Outcome,Is.EqualTo(HeadlineRootOutcome.Cancelled));Assert.That(target.Completion.Card,Is.EqualTo(Id(su)));Assert.That(target.Completion.PlayedBy,Is.EqualTo(PlayerSide.USSR));Assert.That(target.Completion.CancellationSource,Is.EqualTo(Id("defectors")));
            Assert.That(target.Event,Is.Null);Assert.That(target.Scoring,Is.Null);Assert.That(target.DefconVpEvent,Is.Null);Assert.That(target.ActivatedOpsEffect,Is.Null);
            Assert.That(r.View.Practice.CompletedActions,Is.Zero);Assert.That(r.View.Practice.Actor,Is.EqualTo(PlayerSide.USSR));
        }
        [TestCase("containment")][TestCase("nasser")] public void Soviet_self_cancellation_keeps_ordinary_order_and_US_success(string us)
        {
            var r=Both(Host(us,"defectors"),us,"defectors");Assert.That(r.View.Headline.OrderStrategy,Is.EqualTo(HeadlineOrderStrategy.OrdinaryPrintedValue));
            Assert.That(r.Steps.Single(x=>x.PlayedBy==PlayerSide.USSR).Completion.Outcome,Is.EqualTo(HeadlineRootOutcome.Cancelled));Assert.That(r.Steps.Single(x=>x.PlayedBy==PlayerSide.US).Completion.Outcome,Is.EqualTo(HeadlineRootOutcome.Resolved));
        }
        [Test] public void Choice_completion_publishes_success_then_self_cancel_in_one_receipt()
        {
            var h=Host("decolonization","defectors");var waiting=Both(h,"decolonization","defectors");
            Assert.That(waiting.View.Headline.CompletedRoots,Is.Empty);Assert.That(waiting.Steps[0].Completion,Is.Null);Assert.That(waiting.View.Headline.WaitingOwner,Is.EqualTo(PlayerSide.USSR));
            var c=HeadlineSessionContinuationTests.Choice(h.UssrSession);var r=h.UssrSession.SubmitEventAsync(c).Result;
            Assert.That(r.Accepted,Is.True);Assert.That(r.View.Headline.Status,Is.EqualTo(PublicHeadlineStatus.Completed));Assert.That(r.HeadlineSteps.Select(x=>x.Completion.Outcome),Is.EqualTo(new[]{HeadlineRootOutcome.Resolved,HeadlineRootOutcome.Cancelled}));
            var version=r.View.Version;Assert.That(h.UssrSession.SubmitEventAsync(c).Result,Is.SameAs(r));Assert.That(h.UsSession.ReadView().Version,Is.EqualTo(version));
        }
        [Test] public void Historical_first_selection_replay_does_not_reopen_or_reveal_future_results()
        {
            var h=Host();var c=HeadlineSessionTests.Command(h.UsSession,"defectors");var first=h.UsSession.SubmitHeadlineAsync(c).Result;var done=Submit(h.UssrSession,"fidel");Assert.That(done.Accepted,Is.True);
            Assert.That(done.View.Headline.CompletedRoots,Has.Count.EqualTo(2));Assert.That(h.UsSession.SubmitHeadlineAsync(c).Result,Is.SameAs(first));Assert.That(first.View.Headline.CompletedRoots,Is.Empty);Assert.That(first.View.Headline.OrderStrategy,Is.Null);
            Assert.That(h.UsSession.ReadView().Version,Is.EqualTo(done.View.Version));Assert.That(h.UsSession.ReadView().Headline.CompletedRoots,Has.Count.EqualTo(2));
        }
        [Test] public void Second_submission_replay_and_cross_type_conflict_do_not_cancel_twice()
        {
            var h=Host();Submit(h.UsSession,"defectors");var c=HeadlineSessionTests.Command(h.UssrSession,"fidel");var r=h.UssrSession.SubmitHeadlineAsync(c).Result;Assert.That(r.Accepted,Is.True);
            Assert.That(r.Steps[1].Completion,Is.Not.Null);Assert.That(h.UssrSession.SubmitHeadlineAsync(c).Result,Is.SameAs(r));
            var conflict=h.UssrSession.SubmitCardActionAsync(new CardActionCommand(c.CommandId,c.GameId,c.ExpectedVersion,CardActionKind.BeginNextTurn)).Result;
            Assert.That(conflict.Accepted,Is.False);Assert.That(h.UssrSession.ReadView().Version,Is.EqualTo(r.View.Version));
        }
        [Test] public void Public_completion_collections_are_readonly_and_do_not_contain_authority_state()
        {
            var r=Both(Host(),"defectors","fidel");Assert.That(r.View.Headline.CompletedRoots,Is.Not.Null);
            Assert.Throws<NotSupportedException>(()=>((IList<HeadlineRootCompletion>)r.View.Headline.CompletedRoots).Clear());
            var json=JsonConvert.SerializeObject(r);Assert.That(json,Does.Not.Contain("NextState"));Assert.That(json,Does.Not.Contain("RandomState"));Assert.That(json,Does.Not.Contain("\"DrawPile\":"));
            Assert.That(typeof(HeadlineRootCompletion).GetProperties().All(x=>x.SetMethod==null),Is.True);
        }
        [Test] public void Terminal_does_not_report_current_success_or_future_cancellation()
        {
            var r=Both(Host("duck_and_cover","defectors",2),"duck_and_cover","defectors");Assert.That(r.View.Headline.Status,Is.EqualTo(PublicHeadlineStatus.Terminal));
            Assert.That(r.View.Headline.CompletedRoots,Is.Empty);Assert.That(r.Steps,Has.Count.EqualTo(1));Assert.That(r.Steps[0].Completion,Is.Null);
        }
        [Test] public void Ordinary_headlines_also_publish_success_records()
        {
            var r=Both(Host("fidel","romanian_abdication"),"fidel","romanian_abdication");Assert.That(r.View.Headline.CompletedRoots.Select(x=>x.Outcome),Is.EqualTo(new[]{HeadlineRootOutcome.Resolved,HeadlineRootOutcome.Resolved}));
            Assert.That(r.Steps.All(x=>x.Completion!=null && x.Completion.CancellationSource==null),Is.True);
        }
    }
}
