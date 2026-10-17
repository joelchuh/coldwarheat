using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ColdWarStruggle.Core;
using ColdWarStruggle.Application;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public sealed class InfluenceRelocationSessionTests
    {
        static InfluenceAllocation A(string n,int amount)=>InfluenceRelocationPlayTests.A(n,amount);
        static EventCommandEnvelope Choice(IGameSession s,string id="move",IEnumerable<InfluenceAllocation> from=null,IEnumerable<InfluenceAllocation> to=null)
        {var v=s.ReadView();return EventCommandEnvelope.Relocate(id,v.GameId,v.Version,v.Event.ExecutionId,v.Event.DecisionId,from??new[]{A("poland",2)},to??new[]{A("india",2)});}
        static async Task<LocalGameHost> Waiting(bool us=false)
        {
            var h=new LocalGameHost(InfluenceRelocationPlayTests.Ready(us),true);var player=us?h.UsSession:h.UssrSession;var v=player.ReadView();
            var r=await player.SubmitCardActionAsync(new CardActionCommand("start",v.GameId,v.Version,CardActionKind.Event,InfluenceRelocationPlayTests.Card));Assert.That(r.Accepted,Is.True,r.Error.ToString());return h;
        }
        [TestCase(false)][TestCase(true)] public async Task Fixed_USSR_session_only_can_answer_even_when_US_plays(bool us)
        {
            var h=await Waiting(us);var su=h.UssrSession;var other=h.UsSession;var v=su.ReadView();
            var q=su.QueryEventTargets(v.Event.ExecutionId,v.Version);Assert.That(q.Error,Is.EqualTo(RuleError.None));Assert.That(q.Kind,Is.EqualTo(InfluenceDecisionKind.Relocation));Assert.That(q.MaximumAmount,Is.EqualTo(4));Assert.That(q.SourceCapacities,Is.Not.Empty);Assert.That(q.DestinationCapacities,Is.Not.Empty);
            Assert.Throws<NotSupportedException>(()=>((IList<InfluenceAllocation>)q.SourceCapacities).Clear());Assert.Throws<NotSupportedException>(()=>((IList<InfluenceAllocation>)q.DestinationCapacities).Clear());
            var hidden=other.QueryEventTargets(v.Event.ExecutionId,v.Version);Assert.That(hidden.Error,Is.EqualTo(RuleError.WrongActor));Assert.That(hidden.SourceCapacities,Is.Empty);Assert.That(hidden.DestinationCapacities,Is.Empty);
            Assert.That((await other.SubmitEventAsync(Choice(other))).Error,Is.EqualTo(RuleError.WrongActor));Assert.That(su.ReadView().Version,Is.EqualTo(v.Version));
            var result=await su.SubmitEventAsync(Choice(su));Assert.That(result.Accepted,Is.True,result.Error.ToString());Assert.That(su.ReadView().Event,Is.Null);Assert.That(su.ReadView().Practice.CompletedActions,Is.EqualTo(v.Practice.CompletedActions+1));Assert.That(result.Changes,Has.Count.EqualTo(2));
        }
        [TestCase(false)][TestCase(true)] public async Task Same_command_returns_same_receipt_and_changed_either_list_conflicts(bool sourceChanged)
        {
            var h=await Waiting();var s=h.UssrSession;var command=Choice(s);var done=await s.SubmitEventAsync(command);Assert.That(done.Accepted,Is.True);var current=s.ReadView();
            Assert.That(await s.SubmitEventAsync(command),Is.SameAs(done));
            var changed=EventCommandEnvelope.Relocate(command.CommandId,command.GameId,command.ExpectedVersion,command.ExecutionId,command.DecisionId,sourceChanged?new[]{A("romania",2)}:command.Sources,sourceChanged?command.Destinations:new[]{A("angola",2)});
            Assert.That((await s.SubmitEventAsync(changed)).Error,Is.EqualTo(RuleError.CommandIdConflict));Assert.That(s.ReadView().Version,Is.EqualTo(current.Version));Assert.That(s.ReadView().Cards.RemovedCount,Is.EqualTo(current.Cards.RemovedCount));
        }
        [Test] public async Task Both_explicit_empty_arrays_are_valid_zero_relocation()
        {var h=await Waiting();var s=h.UssrSession;var command=Choice(s,from:Array.Empty<InfluenceAllocation>(),to:Array.Empty<InfluenceAllocation>());var done=await s.SubmitEventAsync(command);Assert.That(done.Accepted,Is.True);Assert.That(done.Changes,Is.Empty);Assert.That(s.ReadView().Cards.RemovedCount,Is.EqualTo(1));Assert.That(s.ReadView().Event,Is.Null);}
        [TestCase("game")][TestCase("version")][TestCase("execution")][TestCase("decision")][TestCase("null-source")][TestCase("null-destination")]
        [TestCase("source-limit")][TestCase("destination-limit")][TestCase("unbalanced")][TestCase("legacy-shape")]
        public async Task Invalid_requests_keep_the_pending_state_exactly(string fault)
        {
            var h=await Waiting();var s=h.UssrSession;var v=s.ReadView();var game=v.GameId;var version=v.Version;var execution=v.Event.ExecutionId;var decision=v.Event.DecisionId;
            IEnumerable<InfluenceAllocation> from=new[]{A("poland",2)},to=new[]{A("india",2)};
            switch(fault)
            {case "game":game+="other";break;case "version":version--;break;case "execution":execution+="other";break;case "decision":decision+="other";break;case "null-source":from=null;break;case "null-destination":to=null;break;case "source-limit":from=Enumerable.Repeat(A("poland",1),129);break;case "destination-limit":to=Enumerable.Repeat(A("india",1),129);break;case "unbalanced":to=new[]{A("india",1)};break;}
            var command=fault=="legacy-shape"?new EventCommandEnvelope("bad",game,version,EventCommandKind.ResolveRelocation,execution,decision):EventCommandEnvelope.Relocate("bad",game,version,execution,decision,from,to);
            var r=await s.SubmitEventAsync(command);Assert.That(r.Accepted,Is.False,fault);var current=s.ReadView();Assert.That(current.Version,Is.EqualTo(v.Version));Assert.That(current.Event.DecisionId,Is.EqualTo(v.Event.DecisionId));Assert.That(current.Cards.ResolvingCount,Is.EqualTo(v.Cards.ResolvingCount));Assert.That(current.Practice.CompletedActions,Is.EqualTo(v.Practice.CompletedActions));Assert.That(current.Countries.Select(c=>c.USSR),Is.EqualTo(v.Countries.Select(c=>c.USSR)));
        }
        [Test] public async Task Envelope_copies_both_caller_lists_before_submission()
        {
            var h=await Waiting();var s=h.UssrSession;var from=new[]{A("poland",2)};var to=new[]{A("india",2)};var c=Choice(s,from:from,to:to);from[0]=A("romania",99);to[0]=A("cuba",99);
            Assert.Throws<NotSupportedException>(()=>((IList<InfluenceAllocation>)c.Sources).Clear());Assert.Throws<NotSupportedException>(()=>((IList<InfluenceAllocation>)c.Destinations).Clear());Assert.That((await s.SubmitEventAsync(c)).Accepted,Is.True);
        }
        [Test] public async Task Failed_finalization_is_not_published_by_the_host()
        {
            var ready=PlayInfluenceEventTests.Copy(InfluenceRelocationPlayTests.Ready(),version:long.MaxValue-3);var waiting=InfluenceRelocationPlayTests.Start(ready).NextState;var host=new LocalGameHost(waiting,true);var session=host.UssrSession;var before=session.ReadView();
            var result=await session.SubmitEventAsync(Choice(session));Assert.That(result.Error,Is.EqualTo(RuleError.NumericOverflow));Assert.That(session.ReadView().Version,Is.EqualTo(before.Version));Assert.That(session.ReadView().Countries.Select(c=>c.USSR),Is.EqualTo(before.Countries.Select(c=>c.USSR)));Assert.That(session.ReadView().Cards.ResolvingCount,Is.EqualTo(1));Assert.That(session.ReadView().Cards.RemovedCount,Is.Zero);
        }
    }
}
