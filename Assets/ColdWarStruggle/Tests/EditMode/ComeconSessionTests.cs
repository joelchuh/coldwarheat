using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ColdWarStruggle.Application;
using ColdWarStruggle.Core;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public sealed class ComeconSessionTests
    {
        static async Task<LocalGameHost> Waiting(bool us=false)
        {
            var h=new LocalGameHost(ComeconPlayTests.Ready(us),true);var s=us?h.UsSession:h.UssrSession;var v=s.ReadView();
            Assert.That((await s.SubmitCardActionAsync(new CardActionCommand("start",v.GameId,v.Version,CardActionKind.Event,ComeconPlayTests.Card))).Accepted,Is.True);return h;
        }
        static EventCommandEnvelope Choice(IGameSession s)
        {var v=s.ReadView();return new EventCommandEnvelope("choose",v.GameId,v.Version,EventCommandKind.Resolve,v.Event.ExecutionId,v.Event.DecisionId,ComeconRulesTests.Targets());}
        [TestCase(false)][TestCase(true)] public async Task Only_fixed_USSR_identity_can_choose_even_for_US_card(bool us)
        {
            var h=await Waiting(us);var v=h.UssrSession.ReadView();Assert.That(h.UssrSession.QueryEventTargets(v.Event.ExecutionId,v.Version).RequiredTargets,Is.EqualTo(4));Assert.That(h.UsSession.QueryEventTargets(v.Event.ExecutionId,v.Version).Error,Is.EqualTo(RuleError.WrongActor));
            Assert.That((await h.UsSession.SubmitEventAsync(Choice(h.UsSession))).Error,Is.EqualTo(RuleError.WrongActor));Assert.That(h.UssrSession.ReadView().Version,Is.EqualTo(v.Version));var r=await h.UssrSession.SubmitEventAsync(Choice(h.UssrSession));Assert.That(r.Accepted,Is.True,r.Error.ToString());Assert.That(r.Changes.Count,Is.EqualTo(4));Assert.That(h.UssrSession.ReadView().Practice.CompletedActions,Is.EqualTo(v.Practice.CompletedActions+1));Assert.That(h.UssrSession.ReadView().Event,Is.Null);
        }
        [Test] public async Task Replay_returns_original_receipt_and_changed_targets_conflict()
        {
            var h=await Waiting();var s=h.UssrSession;var c=Choice(s);var r=await s.SubmitEventAsync(c);Assert.That(r.Accepted,Is.True);var v=s.ReadView();Assert.That(await s.SubmitEventAsync(c),Is.SameAs(r));var targets=ComeconRulesTests.Targets();targets[0]=ComeconPlayTests.C("bulgaria");var changed=new EventCommandEnvelope(c.CommandId,c.GameId,c.ExpectedVersion,c.Kind,c.ExecutionId,c.DecisionId,targets);
            Assert.That((await s.SubmitEventAsync(changed)).Error,Is.EqualTo(RuleError.CommandIdConflict));Assert.That(s.ReadView().Version,Is.EqualTo(v.Version));Assert.That(s.ReadView().Cards.RemovedCount,Is.EqualTo(1));
        }
        [TestCase("version")][TestCase("execution")][TestCase("decision")][TestCase("duplicate")][TestCase("enemy")][TestCase("empty")][TestCase("oversize")]
        public async Task Invalid_request_preserves_pending_state(string fault)
        {
            var h=await Waiting();var s=h.UssrSession;var v=s.ReadView();var version=v.Version;var execution=v.Event.ExecutionId;var decision=v.Event.DecisionId;IEnumerable<CountryId> targets=ComeconRulesTests.Targets();
            switch(fault){case "version":version--;break;case "execution":execution+="bad";break;case "decision":decision+="bad";break;case "duplicate":targets=Enumerable.Repeat(ComeconPlayTests.C("poland"),4);break;case "enemy":targets=targets.Take(3).Append(ComeconPlayTests.C("hungary"));break;case "empty":targets=Array.Empty<CountryId>();break;case "oversize":targets=Enumerable.Repeat(ComeconPlayTests.C("poland"),129);break;}
            var r=await s.SubmitEventAsync(new EventCommandEnvelope("bad",v.GameId,version,EventCommandKind.Resolve,execution,decision,targets));Assert.That(r.Accepted,Is.False);Assert.That(s.ReadView().Version,Is.EqualTo(v.Version));Assert.That(s.ReadView().Event.DecisionId,Is.EqualTo(v.Event.DecisionId));Assert.That(s.ReadView().Countries.Select(c=>c.USSR),Is.EqualTo(v.Countries.Select(c=>c.USSR)));Assert.That(s.ReadView().Cards.ResolvingCount,Is.EqualTo(1));
        }
        [Test] public async Task Host_does_not_publish_partial_changes_on_finalization_overflow()
        {
            var ready=PlayInfluenceEventTests.Copy(ComeconPlayTests.Ready(),version:long.MaxValue-3);var h=new LocalGameHost(ComeconPlayTests.Start(ready).NextState,true);var s=h.UssrSession;var v=s.ReadView();var r=await s.SubmitEventAsync(Choice(s));Assert.That(r.Error,Is.EqualTo(RuleError.NumericOverflow));Assert.That(s.ReadView().Version,Is.EqualTo(v.Version));Assert.That(s.ReadView().Countries.Select(c=>c.USSR),Is.EqualTo(v.Countries.Select(c=>c.USSR)));Assert.That(s.ReadView().Cards.RemovedCount,Is.Zero);
        }
    }
}
