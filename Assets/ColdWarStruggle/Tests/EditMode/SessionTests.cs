using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ColdWarStruggle.Core;
using ColdWarStruggle.Application;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class SessionTests
    {
        static CommandEnvelope Request(IGameSession s,string id="one",CountryId[] targets=null)
        { var v=s.ReadView();return new CommandEnvelope(id,v.GameId,v.Version,v.ContextId,targets??new[]{InfluenceFixture.X}); }
        [Test] public async Task Retry_returns_original_receipt_and_changed_content_is_rejected()
        {
            var h=new LocalGameHost(InfluenceFixture.Create(),true);var s=h.UsSession;var cmd=Request(s);var a=await s.SubmitAsync(cmd);
            Assert.That(a.Accepted,Is.True);var b=await s.SubmitAsync(cmd);Assert.That(b,Is.SameAs(a));Assert.That(s.ReadView().Version,Is.EqualTo(1));
            var changed=await s.SubmitAsync(Request(s,"one",new[]{InfluenceFixture.Y}));Assert.That(changed.Error,Is.EqualTo(RuleError.CommandIdConflict));
            var next=await s.SubmitAsync(Request(s,"two"));Assert.That(next.Accepted,Is.True);
            Assert.That(await s.SubmitAsync(cmd),Is.SameAs(a));Assert.That(s.ReadView().Version,Is.EqualTo(2));
        }
        [Test] public async Task Stale_new_request_wrong_actor_and_wrong_context_leave_state_unchanged()
        {
            var h=new LocalGameHost(InfluenceFixture.Create(),true);var old=Request(h.UsSession,"old");await h.UsSession.SubmitAsync(Request(h.UsSession));
            Assert.That((await h.UsSession.SubmitAsync(old)).Error,Is.EqualTo(RuleError.StaleVersion));
            Assert.That((await h.UssrSession.SubmitAsync(Request(h.UssrSession))).Error,Is.EqualTo(RuleError.WrongActor));
            var v=h.UsSession.ReadView();
            Assert.That((await h.UsSession.SubmitAsync(new CommandEnvelope("badctx",v.GameId,v.Version,"other",new[]{InfluenceFixture.X}))).Error,Is.EqualTo(RuleError.StaleContext));
            Assert.That(h.UsSession.ReadView().Version,Is.EqualTo(1));Assert.That(h.UsSession.ReadView().EventSequence,Is.EqualTo(1));
        }
        [Test] public async Task Caller_mutation_does_not_change_payload_and_receipts_are_identity_scoped()
        {
            var h=new LocalGameHost(InfluenceFixture.Create(),true);var targets=new[]{InfluenceFixture.X};var cmd=Request(h.UsSession,targets:targets);targets[0]=InfluenceFixture.Z;
            Assert.That((await h.UsSession.SubmitAsync(cmd)).Accepted,Is.True);
            Assert.That((await h.UssrSession.SubmitAsync(Request(h.UssrSession))).Error,Is.EqualTo(RuleError.WrongActor));
        }
        [Test] public async Task Concurrent_requests_from_same_version_allow_only_one_commit()
        {
            var h=new LocalGameHost(InfluenceFixture.Create(),true);var a=Request(h.UsSession,"a");var b=Request(h.UsSession,"b");
            var results=await Task.WhenAll(Task.Run(()=>h.UsSession.SubmitAsync(a)),Task.Run(()=>h.UsSession.SubmitAsync(b)));
            Assert.That(results.Count(x=>x.Accepted),Is.EqualTo(1));Assert.That(results.Count(x=>x.Error==RuleError.StaleVersion),Is.EqualTo(1));
            Assert.That(h.UsSession.ReadView().Version,Is.EqualTo(1));
        }
        [Test] public async Task Cancellation_before_submission_does_not_commit_and_after_success_does_not_undo()
        {
            var h=new LocalGameHost(InfluenceFixture.Create(),true);var c=new CancellationTokenSource();c.Cancel();
            Assert.That(h.UsSession.SubmitAsync(Request(h.UsSession),c.Token).IsCanceled,Is.True);Assert.That(h.UsSession.ReadView().Version,Is.Zero);
            var live=new CancellationTokenSource();Assert.That((await h.UsSession.SubmitAsync(Request(h.UsSession),live.Token)).Accepted,Is.True);live.Cancel();
            Assert.That(h.UsSession.ReadView().Version,Is.EqualTo(1));
        }
        [Test] public void Queries_are_readonly_versioned_and_identity_scoped()
        {
            var h=new LocalGameHost(InfluenceFixture.Create(),true);var v=h.UsSession.ReadView();var q=h.UsSession.QueryInfluenceTargets(v.ContextId,v.Version);
            Assert.That(q.Error,Is.EqualTo(RuleError.None));Assert.That(q.Targets,Does.Contain(InfluenceFixture.X));Assert.That(q.Viewer,Is.EqualTo(PlayerSide.US));
            Assert.That(h.UsSession.ReadView().Version,Is.EqualTo(v.Version));Assert.That(h.UsSession.ReadView().EventSequence,Is.Zero);
            Assert.That(h.UssrSession.QueryInfluenceTargets(v.ContextId,v.Version).Error,Is.EqualTo(RuleError.WrongActor));
            Assert.That(h.UsSession.QueryInfluenceTargets(v.ContextId,9).Error,Is.EqualTo(RuleError.StaleVersion));
        }
        [Test] public void Standard_views_hide_opponent_hand_but_test_view_explicitly_shows_both()
        {
            var h=new LocalGameHost(InfluenceFixture.Create(),true);var us=h.UsSession.ReadView();var su=h.UssrSession.ReadView();var all=h.ReadLocalTestSnapshot();
            Assert.That(us.OwnHand,Is.EqualTo(new[]{"test.us.card"}));Assert.That(su.OwnHand,Is.EqualTo(new[]{"test.ussr.card"}));Assert.That(us.OpponentHandCount,Is.EqualTo(1));
            Assert.That(all.USHand,Is.EqualTo(us.OwnHand));Assert.That(all.USSRHand,Is.EqualTo(su.OwnHand));Assert.That(all.PublicView.Version,Is.EqualTo(us.Version));
            Assert.Throws<InvalidOperationException>(()=>new LocalGameHost(InfluenceFixture.Create(),false).ReadLocalTestSnapshot());
        }
        [Test] public async Task Malformed_and_wrong_game_requests_have_no_events()
        {
            var s=new LocalGameHost(InfluenceFixture.Create(),true).UsSession;
            Assert.That((await s.SubmitAsync(null)).Error,Is.EqualTo(RuleError.InvalidCommand));
            Assert.That((await s.SubmitAsync(new CommandEnvelope("bad","other",0,"ops",new[]{InfluenceFixture.X}))).Error,Is.EqualTo(RuleError.WrongGame));
            Assert.That((await s.SubmitAsync(Request(s,"empty",new CountryId[0]))).Error,Is.EqualTo(RuleError.InvalidCommand));
            Assert.That(s.ReadView().Version,Is.Zero);
        }
    }
}
