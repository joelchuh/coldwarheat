using System;
using System.Linq;
using System.Threading.Tasks;
using ColdWarStruggle.Core;
using ColdWarStruggle.Application;
using ColdWarStruggle.Infrastructure;
using ColdWarStruggle.Presentation;
using Newtonsoft.Json;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class TurnEndSessionTests
    {
        internal static DemoCatalog Before()=>ChinaCardSessionTests.Add(ChinaCardSessionTests.Before());
        internal static DemoCatalog Catalog()=>Before().WithPracticeTurnEnd().WithQuickStartScenario(QuickStartDemoTests.Data());
        [Test] public async Task Assessment_is_atomic_idempotent_and_public_across_fixed_identities()
        {
            var h=new LocalGameHost(TurnEndPlayTests.Ready(true,true,19,5,4),true);var session=h.UsSession;var v=session.ReadView();Assert.That(v.Practice.TurnEndAvailable,Is.True);Assert.That(v.Practice.TurnEnd,Is.Null);
            var command=new CardActionCommand("audit",v.GameId,v.Version,CardActionKind.AssessTurnEnd);var r=await session.SubmitCardActionAsync(command);Assert.That(r.Accepted,Is.True);Assert.That(await session.SubmitCardActionAsync(command),Is.SameAs(r));Assert.That(r.View.Practice.Winner,Is.EqualTo(PlayerSide.USSR));
            var report=r.View.Practice.TurnEnd;Assert.That(report,Is.Not.Null);Assert.That(report.MilitaryCandidate,Is.EqualTo(PlayerSide.US));Assert.That(report.Winner,Is.EqualTo(PlayerSide.USSR));Assert.That(report.BeforeVp,Is.EqualTo(19));Assert.That(report.AfterVp,Is.EqualTo(20));Assert.That(report.VpDelta,Is.EqualTo(1));Assert.That(report.RuleRowId,Is.Not.Empty);Assert.That(report.UsHoldsScoring,Is.True);Assert.That(report.UssrHoldsScoring,Is.True);
            Assert.That(JsonConvert.SerializeObject(h.UssrSession.ReadView().Practice.TurnEnd),Is.EqualTo(JsonConvert.SerializeObject(report)));Assert.That(JsonConvert.SerializeObject(report),Does.Not.Contain("card.").And.Not.Contain("Random").And.Not.Contain("DrawPile").And.Not.Contain("OwnHand"));
            var conflict=await session.SubmitCardActionAsync(new CardActionCommand("audit",v.GameId,v.Version,CardActionKind.StartPractice));Assert.That(conflict.Error,Is.EqualTo(RuleError.CommandIdConflict));
            var next=h.UssrSession.ReadView();Assert.That((await h.UssrSession.SubmitCardActionAsync(new CardActionCommand("other",next.GameId,next.Version,CardActionKind.AssessTurnEnd))).Error,Is.EqualTo(RuleError.GameAlreadyEnded));Assert.That(next.Version,Is.EqualTo(r.View.Version));
        }
        [Test] public async Task Continue_report_survives_identity_switch_and_new_command_cannot_reapply()
        {
            var h=new LocalGameHost(TurnEndPlayTests.Ready(effect:true),true);var c=new DemoController(_=>h);c.LoadScenario("end",PlayerSide.US,ChinaCardPlayTests.C("iran"));var r=await c.AssessTurnEndAsync();Assert.That(r.Accepted,Is.True);var text=c.TurnEndMessage;Assert.That(text,Does.Contain("下一回合").And.Contain("1").And.Contain("中国牌"));var n=c.ReadView();Assert.That(n.Practice.TurnEndAvailable,Is.False);Assert.That(n.Practice.TurnEnd.ExpiredOpsEffects,Is.EqualTo(1));Assert.That(n.Practice.TurnEnd.ChinaFlipped,Is.True);Assert.That(await c.RetryCardAsync(),Is.SameAs(r));
            c.SelectSide(PlayerSide.USSR);Assert.That(c.LastCardResult,Is.Null);Assert.That(c.TurnEndMessage,Is.EqualTo(text));Assert.That((await c.AssessTurnEndAsync()).Error,Is.EqualTo(RuleError.ActionSliceComplete));Assert.That(c.ReadView().Version,Is.EqualTo(n.Version));Assert.That(c.ReadView().Practice.TurnEnd.AfterVp,Is.EqualTo(n.Practice.VpForUS));
        }
        [Test] public async Task Early_and_stale_assessment_leave_private_hands_and_state_unchanged()
        {
            var h=new LocalGameHost(TurnEndPlayTests.Ready(completed:11),true);var session=h.UsSession;var v=session.ReadView();Assert.That(v.Practice.TurnEndAvailable,Is.False);Assert.That((await session.SubmitCardActionAsync(new CardActionCommand("early",v.GameId,v.Version,CardActionKind.AssessTurnEnd))).Error,Is.EqualTo(RuleError.ActionNotReady));Assert.That((await session.SubmitCardActionAsync(new CardActionCommand("stale",v.GameId,v.Version-1,CardActionKind.AssessTurnEnd))).Error,Is.EqualTo(RuleError.StaleVersion));Assert.That(session.ReadView().Version,Is.EqualTo(v.Version));Assert.That(session.ReadView().OwnHand,Is.EqualTo(v.OwnHand));
        }
        [Test] public void Catalog_preserves_definitions_and_rejects_wrong_assembly_order()
        {var before=Before();var old=before.Scenarios.Single(x=>x.PracticeDefinition!=null);var after=before.WithPracticeTurnEnd();var d=after.Scenarios.Single(x=>x.PracticeDefinition!=null);Assert.That(d.PracticeDefinition.TurnEndEnabled,Is.True);Assert.That(d.PracticeDefinition.China,Is.SameAs(old.PracticeDefinition.China));Assert.That(d.InitialCards,Is.SameAs(old.InitialCards));Assert.Throws<ArgumentException>(()=>after.WithPracticeTurnEnd());Assert.Throws<ArgumentException>(()=>ChinaCardSessionTests.Before().WithPracticeTurnEnd());Assert.Throws<ArgumentException>(()=>ChinaCardSessionTests.Catalog().WithPracticeTurnEnd());}
        internal static readonly (string card,CardActionKind kind)[] DefaultRoute={
            ("china",CardActionKind.Influence),("containment",CardActionKind.Event),
            ("olympic_games",CardActionKind.Influence),("cia_created",CardActionKind.Influence),
            ("blockade",CardActionKind.Influence),("europe_scoring",CardActionKind.Scoring),
            ("socialist_governments",CardActionKind.Influence),("red_scare_purge",CardActionKind.Influence),
            ("suez_crisis",CardActionKind.Influence),("nuclear_test_ban",CardActionKind.Influence),
            ("five_year_plan",CardActionKind.Space),("marshall_plan",CardActionKind.Influence)};
        [Test] public async Task Default_actual_deal_reaches_turn_end_through_twelve_real_actions()
        {
            var catalog=Catalog();var h=catalog.CreateHost(catalog.Scenarios.Last().Id);var c=new DemoController(_=>h);c.LoadScenario("route",PlayerSide.USSR,ChinaCardPlayTests.C("iran"));
            foreach(var use in DefaultRoute)
            {
                var view=c.ReadView();c.SelectSide(view.Practice.Actor);c.SelectCard(new CardId("card."+use.card));var r=await c.PlayCardAsync(use.kind);Assert.That(r.Accepted,Is.True,use.card+":"+r.Error);
                if(use.kind==CardActionKind.Influence)Assert.That((await c.FinishInfluenceAsync()).Accepted,Is.True);
            }
            var before=c.ReadView();Assert.That(before.Practice.CompletedActions,Is.EqualTo(12));Assert.That(before.Practice.TurnEndAvailable,Is.True);Assert.That(before.Practice.OpsEffects.Count,Is.EqualTo(1));Assert.That(before.Practice.UssrAttempts,Is.EqualTo(1));Assert.That(before.Cards.China.Face,Is.EqualTo(CardFace.FaceDown));var result=await c.AssessTurnEndAsync();Assert.That(result.Accepted,Is.True);
            var after=c.ReadView();Assert.That(after.Practice.Winner,Is.Null);Assert.That(after.Practice.TurnEnd.UsShortfall,Is.EqualTo(5));Assert.That(after.Practice.TurnEnd.UssrShortfall,Is.EqualTo(5));Assert.That(after.Practice.VpForUS,Is.EqualTo(before.Practice.VpForUS));Assert.That(after.Practice.UssrAttempts,Is.EqualTo(before.Practice.UssrAttempts));Assert.That(after.Cards.China.Owner,Is.EqualTo(PlayerSide.US));Assert.That(after.Cards.China.Face,Is.EqualTo(CardFace.FaceUp));Assert.That(after.Practice.OpsEffects,Is.Empty);
        }
    }
}
