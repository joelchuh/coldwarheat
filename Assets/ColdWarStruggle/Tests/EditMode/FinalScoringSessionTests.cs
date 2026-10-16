using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ColdWarStruggle.Core;
using ColdWarStruggle.Application;
using Newtonsoft.Json;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class FinalScoringSessionTests
    {
        [TestCase(-1)] [TestCase(0)] public async Task Host_retries_once_and_projects_only_public_final_report(int vp)
        {
            var h=new LocalGameHost(FinalScoringPlayTests.Assessed(vp),true);var session=h.UsSession;var v=session.ReadView();
            Assert.That(v.Practice.FinalScoringEnabled,Is.True);Assert.That(v.Practice.FinalScoringAvailable,Is.True);Assert.That(v.Practice.FinalScoring,Is.Null);
            var command=new CardActionCommand("final",v.GameId,v.Version,CardActionKind.FinalScoring);var r=await session.SubmitCardActionAsync(command);
            Assert.That(r.Accepted,Is.True,r.Error.ToString());Assert.That(await session.SubmitCardActionAsync(command),Is.SameAs(r));
            var report=r.View.Practice.FinalScoring;Assert.That(report.BeforeVp,Is.EqualTo(vp));Assert.That(report.AfterVp,Is.EqualTo(vp+1));Assert.That(report.IsDraw,Is.EqualTo(vp==-1));
            Assert.That(report.Regions.Count,Is.EqualTo(6));Assert.That(report.ChinaOwner,Is.EqualTo(PlayerSide.US));Assert.That(report.ChinaVpForUS,Is.EqualTo(1));Assert.That(report.EuropeControl,Is.False);
            Assert.Throws<NotSupportedException>(()=>((IList<PublicRegionScore>)report.Regions).Clear());
            var json=JsonConvert.SerializeObject(report);Assert.That(JsonConvert.SerializeObject(h.UssrSession.ReadView().Practice.FinalScoring),Is.EqualTo(json));
            Assert.That(json,Does.Not.Contain("Definition").And.Not.Contain("Random").And.Not.Contain("DrawPile").And.Not.Contain("OwnHand").And.Not.Contain("country."));
            Assert.That(r.View.Practice.FinalScoringAvailable,Is.False);Assert.That(r.View.Practice.NextTurnAvailable,Is.False);Assert.That(r.View.Practice.TurnEnd.AfterVp,Is.EqualTo(vp));
            Assert.That((await session.SubmitCardActionAsync(new CardActionCommand("final",v.GameId,v.Version,CardActionKind.AssessTurnEnd))).Error,Is.EqualTo(RuleError.CommandIdConflict));
            var current=h.UssrSession.ReadView();Assert.That((await h.UssrSession.SubmitCardActionAsync(new CardActionCommand("again",current.GameId,current.Version,CardActionKind.FinalScoring))).Error,Is.EqualTo(RuleError.GameAlreadyEnded));
            Assert.That(current.Version,Is.EqualTo(v.Version+1));
        }
        [Test] public async Task Stale_request_does_not_publish_a_report()
        {
            var h=new LocalGameHost(FinalScoringPlayTests.Assessed(),true);var s=h.UsSession;var v=s.ReadView();
            var r=await s.SubmitCardActionAsync(new CardActionCommand("stale",v.GameId,v.Version-1,CardActionKind.FinalScoring));Assert.That(r.Error,Is.EqualTo(RuleError.StaleVersion));
            Assert.That(s.ReadView().Practice.FinalScoring,Is.Null);Assert.That(s.ReadView().Version,Is.EqualTo(v.Version));
        }
    }
}
