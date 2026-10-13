using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ColdWarStruggle.Core;
using ColdWarStruggle.Application;
using NUnit.Framework;

namespace ColdWarStruggle.Tests
{
    public class DeploymentSessionTests
    {
        static CountryId C(string id)=>new CountryId("country."+id);
        static InfluenceAllocation A(string id,int amount)=>new InfluenceAllocation(C(id),amount);
        static LocalGameHost Host()=>new LocalGameHost(SetupDeploymentTests.Started(),true);
        static DeploymentCommandEnvelope Command(PlayerViewSnapshot v,string id,params InfluenceAllocation[] allocations)
            =>new DeploymentCommandEnvelope(id,v.GameId,v.Version,v.Deployment.ExecutionId,v.Deployment.DecisionId,allocations);
        [Test] public async Task Fixed_sessions_submit_both_stages_and_project_public_deployment_without_private_hands()
        {
            var h=Host();var sv=h.UssrSession.ReadView();var usBefore=h.UsSession.ReadView();
            Assert.That(sv.Deployment.Owner,Is.EqualTo(PlayerSide.USSR));Assert.That(sv.Deployment.RequiredAmount,Is.EqualTo(6));
            Assert.That(sv.Deployment.Region,Is.EqualTo("region.eastern_europe"));Assert.That(sv.Deployment.DecisionSourceVersion,Is.EqualTo(sv.Version));
            Assert.That(usBefore.OwnHand,Is.EqualTo(new[]{"fixture.us.secret"}));Assert.That(usBefore.OpponentHandCount,Is.EqualTo(1));
            Assert.That(typeof(PublicDeploymentView).GetProperties().Any(p=>p.PropertyType==typeof(GameState) || p.PropertyType==typeof(SetupDeploymentDefinition)),Is.False);
            var q=h.UssrSession.QueryDeploymentTargets(sv.Deployment.ExecutionId,sv.Version);
            Assert.That(q.Error,Is.EqualTo(RuleError.None));Assert.That(q.RequiredAmount,Is.EqualTo(6));Assert.That(q.Targets,Does.Contain(C("poland")));
            Assert.That(q.Viewer,Is.EqualTo(PlayerSide.USSR));Assert.That(q.DecisionId,Is.EqualTo(sv.Deployment.DecisionId));
            var first=await h.UssrSession.SubmitDeploymentAsync(Command(sv,"soviet",A("poland",6)));
            Assert.That(first.Accepted,Is.True);Assert.That(first.View.Deployment.Owner,Is.EqualTo(PlayerSide.US));Assert.That(first.Changes.Count,Is.EqualTo(11));
            var uv=h.UsSession.ReadView();var second=await h.UsSession.SubmitDeploymentAsync(Command(uv,"american",A("canada",7)));
            Assert.That(second.Accepted,Is.True);Assert.That(second.View.Deployment.Complete,Is.True);Assert.That(second.View.Deployment.Owner,Is.Null);
            Assert.That(second.View.Deployment.RequiredAmount,Is.Zero);Assert.That(second.CompletionSequence,Is.EqualTo(second.View.EventSequence));
            Assert.That(h.UssrSession.ReadView().Countries.Sum(c=>c.US),Is.EqualTo(25));Assert.That(h.UsSession.ReadView().Countries.Sum(c=>c.USSR),Is.EqualTo(15));
            Assert.That(sv.Deployment.Owner,Is.EqualTo(PlayerSide.USSR));Assert.That(sv.Countries.Sum(c=>c.US),Is.Zero);
            Assert.Throws<NotSupportedException>(()=>((IList<CountryId>)q.Targets).Clear());
        }
        [Test] public async Task Retry_returns_original_receipt_after_other_player_completes()
        {
            var h=Host();var before=h.UssrSession.ReadView();var cmd=Command(before,"same",A("poland",6));
            var first=await h.UssrSession.SubmitDeploymentAsync(cmd);var uv=h.UsSession.ReadView();
            await h.UsSession.SubmitDeploymentAsync(Command(uv,"us",A("canada",7)));
            var duplicate=await h.UssrSession.SubmitDeploymentAsync(cmd);Assert.That(duplicate,Is.SameAs(first));
            Assert.That(duplicate.View.Version,Is.EqualTo(2));Assert.That(duplicate.View.Deployment.Complete,Is.False);
            Assert.That(h.UssrSession.ReadView().Version,Is.EqualTo(3));Assert.That(h.UssrSession.ReadView().Deployment.Complete,Is.True);
        }
        [Test] public async Task Changed_payload_or_type_cannot_reuse_command_id_in_either_direction()
        {
            var h=Host();var s=h.UssrSession;var v=s.ReadView();var cmd=Command(v,"used",A("poland",6));await s.SubmitDeploymentAsync(cmd);
            Assert.That((await s.SubmitDeploymentAsync(Command(v,"used",A("austria",6)))).Error,Is.EqualTo(RuleError.CommandIdConflict));
            Assert.That((await s.SubmitAsync(new CommandEnvelope("used",v.GameId,v.Version,v.ContextId,new[]{C("poland")}))).Error,Is.EqualTo(RuleError.CommandIdConflict));
            Assert.That((await s.SubmitEventAsync(new EventCommandEnvelope("used",v.GameId,v.Version,EventCommandKind.Begin,"event"))).Error,Is.EqualTo(RuleError.CommandIdConflict));
            h=Host();s=h.UssrSession;v=s.ReadView();await s.SubmitAsync(new CommandEnvelope("ops",v.GameId,v.Version,v.ContextId,new[]{C("poland")}));
            Assert.That((await s.SubmitDeploymentAsync(Command(v,"ops",A("poland",6)))).Error,Is.EqualTo(RuleError.CommandIdConflict));
            await s.SubmitEventAsync(new EventCommandEnvelope("event",v.GameId,v.Version,EventCommandKind.Begin,"event"));
            Assert.That((await s.SubmitDeploymentAsync(Command(v,"event",A("poland",6)))).Error,Is.EqualTo(RuleError.CommandIdConflict));
        }
        [Test] public async Task Same_command_id_is_scoped_to_fixed_identity_and_rejected_receipt_is_historical()
        {
            var h=Host();var v=h.UssrSession.ReadView();var cmd=Command(v,"shared-id",A("poland",6));
            var wrong=await h.UsSession.SubmitDeploymentAsync(cmd);Assert.That(wrong.Error,Is.EqualTo(RuleError.WrongActor));
            var accepted=await h.UssrSession.SubmitDeploymentAsync(cmd);Assert.That(accepted.Accepted,Is.True);
            Assert.That(await h.UsSession.SubmitDeploymentAsync(cmd),Is.SameAs(wrong));Assert.That(wrong.View.Version,Is.EqualTo(v.Version));
            Assert.That(h.UsSession.ReadView().Version,Is.EqualTo(v.Version+1));
        }
        [TestCase("game",RuleError.WrongGame)] [TestCase("version",RuleError.StaleVersion)]
        [TestCase("context",RuleError.StaleContext)] [TestCase("decision",RuleError.StaleDecision)]
        [TestCase("outside",RuleError.OutsideSetupRegion)] [TestCase("duplicate",RuleError.DuplicateCountry)]
        [TestCase("total",RuleError.IncorrectAllocationTotal)]
        public async Task Invalid_request_does_not_change_state_and_retries_preserve_rejection(string kind,RuleError expected)
        {
            var h=Host();var s=h.UssrSession;var v=s.ReadView();var allocations=new[]{A("poland",6)};
            if(kind=="outside")allocations=new[]{A("france",6)};if(kind=="duplicate")allocations=new[]{A("poland",3),A("poland",3)};if(kind=="total")allocations=new[]{A("poland",5)};
            var cmd=new DeploymentCommandEnvelope("bad",kind=="game"?"foreign":v.GameId,kind=="version"?v.Version-1:v.Version,
                kind=="context"?"foreign":v.Deployment.ExecutionId,kind=="decision"?"foreign":v.Deployment.DecisionId,allocations);
            var r=await s.SubmitDeploymentAsync(cmd);Assert.That(r.Error,Is.EqualTo(expected));Assert.That(r.Changes,Is.Empty);Assert.That(s.ReadView().Version,Is.EqualTo(v.Version));
            Assert.That(await s.SubmitDeploymentAsync(cmd),Is.SameAs(r));
        }
        [TestCase("null")] [TestCase("id")] [TestCase("long-id")] [TestCase("game")] [TestCase("version")]
        [TestCase("context")] [TestCase("long-context")] [TestCase("decision")] [TestCase("long-decision")]
        [TestCase("empty")] [TestCase("too-many")]
        public async Task Malformed_envelopes_are_bounded_and_do_not_reserve_command_id(string kind)
        {
            var h=Host();var s=h.UssrSession;var v=s.ReadView();
            var a=kind=="empty"?Array.Empty<InfluenceAllocation>():kind=="too-many"?Enumerable.Repeat(A("poland",1),100000):new[]{A("poland",6)};
            var cmd=kind=="null"?null:new DeploymentCommandEnvelope(kind=="id"?"":kind=="long-id"?new string('x',129):"test",kind=="game"?"":v.GameId,
                kind=="version"?-1:v.Version,kind=="context"?null:kind=="long-context"?new string('x',129):v.Deployment.ExecutionId,
                kind=="decision"?null:kind=="long-decision"?new string('x',257):v.Deployment.DecisionId,a);
            if(kind=="too-many")Assert.That(cmd.Allocations.Count,Is.EqualTo(129));
            var r=await s.SubmitDeploymentAsync(cmd);Assert.That(r.Error,Is.EqualTo(RuleError.InvalidCommand));
            Assert.That((await s.SubmitDeploymentAsync(Command(v,"test",A("poland",6)))).Accepted,Is.True);
        }
        [Test] public async Task Cancelled_submission_does_not_consume_identity_or_modify_state()
        {
            var h=Host();var s=h.UssrSession;var v=s.ReadView();var c=Command(v,"cancel",A("poland",6));
            using(var cancel=new CancellationTokenSource())
            {
                cancel.Cancel();try { await s.SubmitDeploymentAsync(c,cancel.Token);Assert.Fail("Expected cancellation."); }catch(OperationCanceledException){}
            }
            Assert.That(s.ReadView().Version,Is.EqualTo(v.Version));Assert.That((await s.SubmitDeploymentAsync(c)).Accepted,Is.True);
        }
        [Test] public async Task Concurrent_duplicates_commit_once_and_input_arrays_are_copied()
        {
            var h=Host();var s=h.UssrSession;var v=s.ReadView();var allocations=new[]{A("poland",6)};var c=Command(v,"race",allocations);allocations[0]=A("france",6);
            var results=await Task.WhenAll(Task.Run(()=>s.SubmitDeploymentAsync(c)),Task.Run(()=>s.SubmitDeploymentAsync(c)));
            Assert.That(results[0].Accepted,Is.True);Assert.That(results[0],Is.SameAs(results[1]));Assert.That(s.ReadView().Version,Is.EqualTo(v.Version+1));
            Assert.Throws<NotSupportedException>(()=>((IList<InfluenceAllocation>)c.Allocations).Clear());
        }
        [Test] public void Queries_reject_wrong_identity_version_and_absent_setup_with_empty_targets()
        {
            var h=Host();var v=h.UssrSession.ReadView();
            var queries=new[]{h.UsSession.QueryDeploymentTargets(v.Deployment.ExecutionId,v.Version),h.UssrSession.QueryDeploymentTargets(v.Deployment.ExecutionId,v.Version-1),h.UssrSession.QueryDeploymentTargets("unknown",v.Version)};
            Assert.That(queries.Select(q=>q.Error),Is.EqualTo(new[]{RuleError.WrongActor,RuleError.StaleVersion,RuleError.StaleContext}));
            foreach(var q in queries){Assert.That(q.Targets,Is.Empty);Assert.That(q.RequiredAmount,Is.Zero);}
            var ordinary=new LocalGameHost(InfluenceFixture.Create(),false);var ov=ordinary.UsSession.ReadView();Assert.That(ov.Deployment,Is.Null);
            Assert.That(ordinary.UsSession.QueryDeploymentTargets("none",ov.Version).Error,Is.EqualTo(RuleError.SetupNotActive));
        }
    }
}
