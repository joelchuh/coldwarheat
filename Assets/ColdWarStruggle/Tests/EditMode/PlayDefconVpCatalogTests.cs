using System;
using System.Linq;
using System.Threading.Tasks;
using ColdWarStruggle.Core;
using ColdWarStruggle.Application;
using ColdWarStruggle.Infrastructure;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class PlayDefconVpCatalogTests
    {
        static DemoCatalog Before()=>PlayInfluenceEventCatalogTests.Add(PlayScoringCatalogTests.Catalog());
        internal static DemoCatalog Add(DemoCatalog c)=>c.WithPracticeDefconVpEvents(DefconVpEventTests.Data(),DefconVpEventTests.Schema());
        internal static DemoCatalog Catalog()=>Add(Before()).WithQuickStartScenario(QuickStartDemoTests.Data());
        [Test] public void Registry_survives_quick_start_without_changing_other_rules_deal_or_manual_setup()
        {
            var before=Before();var after=Add(before);var first=before.Scenarios.Single(s=>s.PracticeDefinition!=null);var manual=after.Scenarios.Single(s=>s.PracticeDefinition!=null);var d=manual.PracticeDefinition;
            Assert.That(d.InfluenceEvents,Is.SameAs(first.PracticeDefinition.InfluenceEvents));Assert.That(d.Risk,Is.SameAs(first.PracticeDefinition.Risk));Assert.That(d.Scoring,Is.SameAs(first.PracticeDefinition.Scoring));Assert.That(d.DefconVpEvents.Effects.Count,Is.EqualTo(2));Assert.That(manual.InitialCards,Is.SameAs(first.InitialCards));Assert.That(after.CreateHost(manual.Id).UssrSession.ReadView().Deployment.Complete,Is.False);
            var q=after.WithQuickStartScenario(QuickStartDemoTests.Data());Assert.That(q.Scenarios.Last().PracticeDefinition,Is.SameAs(d));Assert.That(q.Scenarios.Count,Is.EqualTo(before.Scenarios.Count+1));
        }
        [Test] public void Repeated_wrong_order_and_after_quick_start_configuration_are_rejected()
        {Assert.Throws<ArgumentException>(()=>Add(PlayScoringCatalogTests.Catalog()));var c=Add(Before());Assert.Throws<ArgumentException>(()=>Add(c));Assert.Throws<ArgumentException>(()=>Add(Before().WithQuickStartScenario(QuickStartDemoTests.Data())));}
        [Test] public async Task Actual_default_Duck_then_Test_Ban_uses_old_defcon_and_keeps_eight_influence_events()
        {
            var c=Catalog();var h=c.CreateHost(c.Scenarios.Last().Id);var s=h.UssrSession;var v=s.ReadView();var allowed=s.QueryCardActions().Cards.Where(x=>x.Event==RuleError.None).Select(x=>x.Card.Value);
            Assert.That(allowed,Is.EquivalentTo(new[]{"card.duck_and_cover","card.socialist_governments","card.suez_crisis"}));
            var r=await s.SubmitCardActionAsync(new CardActionCommand("duck",v.GameId,v.Version,CardActionKind.Event,new CardId("card.duck_and_cover")));Assert.That(r.Accepted,Is.True);Assert.That(r.View.Practice.Defcon,Is.EqualTo(4));Assert.That(r.View.Practice.VpForUS,Is.EqualTo(1));
            s=h.UsSession;v=s.ReadView();Assert.That(s.QueryCardActions().Cards.Single(x=>x.Card.Value=="card.nasser").Event,Is.EqualTo(RuleError.None));r=await s.SubmitCardActionAsync(new CardActionCommand("ban",v.GameId,v.Version,CardActionKind.Event,new CardId("card.nuclear_test_ban")));
            Assert.That(r.Accepted,Is.True);Assert.That(r.DefconVpEvent.Award,Is.EqualTo(2));Assert.That(r.View.Practice.VpForUS,Is.EqualTo(3));Assert.That(r.View.Practice.Defcon,Is.EqualTo(5));Assert.That(r.View.Practice.CompletedActions,Is.EqualTo(2));Assert.That(r.View.Cards.DiscardCount,Is.EqualTo(2));Assert.That(r.View.Practice.UsMilitary,Is.Zero);Assert.That(r.View.Practice.UssrMilitary,Is.Zero);
        }
    }
}
