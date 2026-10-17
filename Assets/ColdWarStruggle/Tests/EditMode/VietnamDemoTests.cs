using System;
using System.Linq;
using System.Threading.Tasks;
using ColdWarStruggle.Core;
using ColdWarStruggle.Infrastructure;
using ColdWarStruggle.Presentation;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public sealed class VietnamDemoTests
    {
        internal static string Fixture()=>PlayInfluenceEventTests.Read("Data/Demo/vietnam-scenario.json");
        internal static DemoCatalog Bind(DemoCatalog c)=>c.WithVietnamRevolts(TurnOpsEffectTests.Data(),TurnOpsEffectTests.Schema());
        internal static DemoCatalog Catalog()=>Bind(ComeconDemoTests.Catalog()).WithVietnamScenario(Fixture());
        [Test] public void Registration_preserves_previous_modules_and_rejects_duplicate_registration()
        {
            var before=ComeconDemoTests.Catalog();var after=Bind(before);Assert.That(after.Scenarios.Count,Is.EqualTo(before.Scenarios.Count));
            foreach(var scene in before.Scenarios.Where(s=>s.PracticeDefinition!=null))
            {var a=scene.PracticeDefinition;var b=after.Scenarios.Single(s=>s.Id==scene.Id).PracticeDefinition;Assert.That(b.InfluenceEvents,Is.SameAs(a.InfluenceEvents));Assert.That(b.Olympic,Is.SameAs(a.Olympic));Assert.That(b.SpaceEvent,Is.SameAs(a.SpaceEvent));Assert.That(b.Wars,Is.SameAs(a.Wars));Assert.That(b.FiveYearPlan,Is.SameAs(a.FiveYearPlan));Assert.That(b.Blockade,Is.SameAs(a.Blockade));Assert.That(b.HeadlineParameters,Is.SameAs(a.HeadlineParameters));Assert.That(b.FinalScoring,Is.SameAs(a.FinalScoring));Assert.That(b.China,Is.SameAs(a.China));Assert.That(b.Risk,Is.SameAs(a.Risk));Assert.That(b.NextTurnEnabled,Is.EqualTo(a.NextTurnEnabled));Assert.That(b.TurnOpsEvents.Effects.Count,Is.EqualTo(a.TurnOpsEvents.Effects.Count+1));}
            Assert.Throws<ArgumentException>(()=>Bind(after));Assert.Throws<ArgumentException>(()=>Bind(PlayableDemoTests.Catalog()));
        }
        [Test] public async Task Fixture_plays_Vietnam_then_next_Soviet_China_OPS_has_six_with_both_conditions()
        {
            var data=Catalog();Assert.That(data.Scenarios.Count,Is.EqualTo(39));var scene=data.Scenarios.Single(s=>s.Id=="demo.practice.vietnam");var c=new DemoController(data.CreateHost);c.LoadScenario(scene.Id,scene.Actor,scene.SelectedCountry);Assert.That(c.ReadView().Turn.Number,Is.EqualTo(2));Assert.That(c.ReadView().Event,Is.Null);
            c.SelectCard(VietnamPlayTests.Card);var first=await c.PlayCardAsync(CardActionKind.Event);Assert.That(first.Accepted,Is.True,first.Error.ToString());Assert.That(first.Event.Changes.Single().After,Is.EqualTo(2));Assert.That(first.ActivatedOpsEffect.Region,Is.EqualTo("region.southeast_asia"));
            c.SelectSide(PlayerSide.US);c.SelectCard(new CardId("card.captured_nazi_scientist"));Assert.That((await c.PlayCardAsync(CardActionKind.Influence)).Accepted,Is.True);Assert.That((await c.FinishInfluenceAsync()).Accepted,Is.True);
            c.SelectSide(PlayerSide.USSR);c.SelectCard(ChinaCardPlayTests.China);var ops=await c.PlayCardAsync(CardActionKind.Influence);Assert.That(ops.Accepted,Is.True,ops.Error.ToString());Assert.That(ops.View.RemainingOps,Is.EqualTo(6));
        }
        [TestCase("scope")][TestCase("turn")][TestCase("source")][TestCase("card")][TestCase("actor")][TestCase("duplicate")][TestCase("influence")][TestCase("unknown")]
        public void Bad_fixtures_reject_without_partial_catalog_changes(string fault)
        {
            var catalog=Bind(ComeconDemoTests.Catalog());var data=JObject.Parse(Fixture());var row=(JObject)data["scenarios"][0];
            if(fault=="scope")data["scope"]="official";if(fault=="turn")data["turn_number"]=3;if(fault=="source")data["source_scenario_id"]="missing";if(fault=="card")row["card"]="card.fidel";if(fault=="actor")row["actor"]="US";
            if(fault=="duplicate")row["us_hand"][0]="card.vietnam_revolts";if(fault=="influence")row["influence"][0]["ussr"]=-1;if(fault=="unknown")row["ignored"]=true;
            Assert.Throws<ArgumentException>(()=>catalog.WithVietnamScenario(data.ToString()));Assert.That(catalog.Scenarios.Count,Is.EqualTo(38));
        }
    }
}
