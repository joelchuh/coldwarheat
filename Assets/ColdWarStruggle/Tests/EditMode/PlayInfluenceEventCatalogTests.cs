using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using ColdWarStruggle.Core;
using ColdWarStruggle.Infrastructure;
using ColdWarStruggle.Application;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class PlayInfluenceEventCatalogTests
    {
        static string Read(string p)=>PlayInfluenceEventTests.Read(p);
        internal static DemoCatalog Add(DemoCatalog c,string first=null)=>c.WithPracticeInfluenceEvents(first??Read("Data/Cards/deluxe-2015/early-influence-batch1.parameters.json"),Read("Docs/Contracts/early-influence-parameters.schema.json"),Read("Data/Cards/deluxe-2015/early-influence-batch2.parameters.json"),Read("Docs/Contracts/early-influence-batch2.schema.json"),Read("Data/Cards/deluxe-2015/cards.json"));
        internal static DemoCatalog Catalog()=>Add(PlayScoringCatalogTests.Catalog()).WithQuickStartScenario(QuickStartDemoTests.Data());
        [Test] public void One_catalog_binds_eight_effects_and_preserves_risk_scoring_deal_and_manual_lesson()
        {
            var before=PlayScoringCatalogTests.Catalog();var source=before.Scenarios.Single(x=>x.PracticeDefinition!=null);var after=Add(before);var configured=after.Scenarios.Single(x=>x.PracticeDefinition!=null);var d=configured.PracticeDefinition;
            Assert.That(d.InfluenceEvents.Effects.Count,Is.EqualTo(8));Assert.That(d.InfluenceEvents.Map,Is.SameAs(after.Map));Assert.That(d.InfluenceEvents.Cards,Is.SameAs(after.PrintedCards));Assert.That(d.Risk,Is.SameAs(source.PracticeDefinition.Risk));Assert.That(d.Scoring,Is.SameAs(source.PracticeDefinition.Scoring));Assert.That(d.Ops,Is.SameAs(source.PracticeDefinition.Ops));Assert.That(d.Space,Is.SameAs(source.PracticeDefinition.Space));Assert.That(d.Calendar,Is.SameAs(source.PracticeDefinition.Calendar));Assert.That(configured.InitialCards,Is.SameAs(source.InitialCards));Assert.That(after.CreateHost(source.Id).UssrSession.ReadView().Deployment,Is.Not.Null);
            Assert.That(source.PracticeDefinition.InfluenceEvents,Is.Null);Assert.That(after.Scenarios.Count,Is.EqualTo(13));Assert.That(after.WithQuickStartScenario(QuickStartDemoTests.Data()).Scenarios.Last().PracticeDefinition,Is.SameAs(d));
        }
        [Test] public void Wrong_order_repeat_and_changed_printed_affiliation_are_rejected()
        {
            Assert.Throws<ArgumentException>(()=>Add(PlayableDemoTests.Catalog()));Assert.Throws<ArgumentException>(()=>Add(Catalog()));Assert.Throws<ArgumentException>(()=>Add(Add(PlayScoringCatalogTests.Catalog())));
            var first=Read("Data/Cards/deluxe-2015/early-influence-batch1.parameters.json");Assert.Throws<ArgumentException>(()=>Add(PlayScoringCatalogTests.Catalog(),first.Replace("\"beneficiary\": \"USSR\"","\"beneficiary\": \"US\"")));
        }
        [Test] public async Task Actual_quick_start_suez_allocation_finishes_then_us_can_use_nasser_event()
        {
            var d=Catalog();var h=d.CreateHost(QuickStartDemoTests.Id);var s=h.UssrSession;var v=s.ReadView();Assert.That(s.QueryCardActions().Cards.Where(c=>c.Event==RuleError.None).Select(c=>c.Card.Value),Is.EquivalentTo(new[]{"card.suez_crisis","card.socialist_governments"}));
            var started=await s.SubmitCardActionAsync(new CardActionCommand("suez",v.GameId,v.Version,CardActionKind.Event,new CardId("card.suez_crisis")));Assert.That(started.Accepted,Is.True);v=s.ReadView();var q=s.QueryEventTargets(v.Event.ExecutionId,v.Version);Assert.That(q.RequiredAmount,Is.EqualTo(3));
            var remaining=q.RequiredAmount;var allocations=new List<InfluenceAllocation>();foreach(var cap in q.Capacities){var n=Math.Min(cap.Amount,remaining);if(n>0)allocations.Add(new InfluenceAllocation(cap.Country,n));remaining-=n;}
            Assert.That((await s.SubmitEventAsync(EventCommandEnvelope.Allocate("resolve",v.GameId,v.Version,v.Event.ExecutionId,v.Event.DecisionId,allocations))).Accepted,Is.True);var us=h.UsSession;v=us.ReadView();Assert.That(v.Practice.Actor,Is.EqualTo(PlayerSide.US));Assert.That(v.Cards.RemovedCount,Is.EqualTo(1));Assert.That(us.QueryCardActions().Cards.Single(c=>c.Card.Value=="card.nasser").Event,Is.EqualTo(RuleError.None));
            var done=await us.SubmitCardActionAsync(new CardActionCommand("nasser",v.GameId,v.Version,CardActionKind.Event,new CardId("card.nasser")));Assert.That(done.Accepted,Is.True);Assert.That(done.View.Event,Is.Null);Assert.That(done.View.Practice.CompletedActions,Is.EqualTo(2));Assert.That(done.View.Cards.RemovedCount,Is.EqualTo(2));Assert.That(done.View.Countries.Single(c=>c.Definition.Id.Value=="country.egypt").USSR,Is.EqualTo(2));
        }
    }
}
