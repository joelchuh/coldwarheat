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
    public sealed class JapanPactDemoTests
    {
        internal static string Fixture()=>PlayInfluenceEventTests.Read("Data/Demo/japan-pact-scenario.json");
        internal static DemoCatalog Bind(DemoCatalog c)=>c.WithJapanPact(JapanPactFoundationTests.Data(),JapanPactFoundationTests.Schema());
        internal static DemoCatalog Catalog()=>Bind(VietnamDemoTests.Catalog()).WithJapanPactScenario(Fixture());
        [Test] public void Registration_preserves_previous_modules_and_requires_explicit_known_inactive_state()
        {
            var before=VietnamDemoTests.Catalog();var after=Bind(before);Assert.That(after.Scenarios.Count,Is.EqualTo(before.Scenarios.Count));
            foreach(var scene in before.Scenarios.Where(s=>s.PracticeDefinition!=null))
            {var a=scene.PracticeDefinition;var b=after.Scenarios.Single(s=>s.Id==scene.Id).PracticeDefinition;Assert.That(b.InfluenceEvents,Is.SameAs(a.InfluenceEvents));Assert.That(b.Olympic,Is.SameAs(a.Olympic));Assert.That(b.SpaceEvent,Is.SameAs(a.SpaceEvent));Assert.That(b.Wars,Is.SameAs(a.Wars));Assert.That(b.FiveYearPlan,Is.SameAs(a.FiveYearPlan));Assert.That(b.Blockade,Is.SameAs(a.Blockade));Assert.That(b.TurnOpsEvents,Is.SameAs(a.TurnOpsEvents));Assert.That(b.HeadlineParameters,Is.SameAs(a.HeadlineParameters));Assert.That(b.FinalScoring,Is.SameAs(a.FinalScoring));Assert.That(b.China,Is.SameAs(a.China));Assert.That(b.Risk,Is.SameAs(a.Risk));Assert.That(b.JapanPact,Is.Not.Null);Assert.That(after.CreateHost(scene.Id).UsSession.ReadView().JapanPact,Is.Null);}
            Assert.Throws<ArgumentException>(()=>Bind(after));Assert.Throws<ArgumentException>(()=>Bind(PlayableDemoTests.Catalog()));
        }
        [Test] public async Task Fixture_real_event_then_protected_coup_and_adjustment_and_other_target()
        {
            var data=Catalog();Assert.That(data.Scenarios.Count,Is.EqualTo(40));var scene=data.Scenarios.Single(s=>s.Id=="demo.practice.japan_pact");var c=new DemoController(data.CreateHost);c.LoadScenario(scene.Id,scene.Actor,scene.SelectedCountry);
            c.SelectCard(JapanPactPlayTests.Card);var first=await c.PlayCardAsync(CardActionKind.Event);Assert.That(first.Accepted,Is.True,first.Error.ToString());Assert.That(first.Event.Changes.Single().After,Is.EqualTo(7));Assert.That(first.View.JapanPact,Is.Not.Null);
            c.SelectSide(PlayerSide.US);c.SelectCard(new CardId("card.captured_nazi_scientist"));Assert.That((await c.PlayCardAsync(CardActionKind.Influence)).Accepted,Is.True);Assert.That((await c.FinishInfluenceAsync()).Accepted,Is.True);
            c.SelectSide(PlayerSide.USSR);c.SelectCard(OlympicPlayTests.Card);c.SelectCoupUse();Assert.That(c.LegalTargets().Targets,Has.No.Member(JapanPactPlayTests.C("japan")));Assert.That(c.SelectedCoupQuote.Error,Is.EqualTo(RuleError.EventForbidden));var before=c.ReadView();Assert.That((await c.PlayCardAsync(CardActionKind.Coup)).Error,Is.EqualTo(RuleError.EventForbidden));Assert.That(c.ReadView().Version,Is.EqualTo(before.Version));
            c.SelectCountry(JapanPactPlayTests.C("angola"));Assert.That(c.LegalTargets().Targets,Has.Member(JapanPactPlayTests.C("angola")));Assert.That((await c.PlayCardAsync(CardActionKind.Realignment)).Accepted,Is.True);Assert.That(c.LegalTargets().Targets,Has.No.Member(JapanPactPlayTests.C("japan")));Assert.That(c.ReadView().JapanPact,Is.Not.Null);
        }
        [TestCase("scope")][TestCase("turn")][TestCase("source")][TestCase("card")][TestCase("actor")][TestCase("duplicate")][TestCase("influence")][TestCase("unknown")][TestCase("china")]
        public void Bad_fixture_rejects_without_partial_catalog(string fault)
        {
            var c=Bind(VietnamDemoTests.Catalog());var d=JObject.Parse(Fixture());var row=(JObject)d["scenarios"][0];
            if(fault=="scope")d["scope"]="official";if(fault=="turn")d["turn_number"]=3;if(fault=="source")d["source_scenario_id"]="missing";if(fault=="card")row["card"]="card.fidel";if(fault=="actor")row["actor"]="US";
            if(fault=="duplicate")row["us_hand"][0]="card.us_japan_mutual_defense_pact";if(fault=="influence")row["influence"][0]["ussr"]=-1;if(fault=="unknown")row["ignored"]=true;if(fault=="china")d["china_face"]="FaceDown";
            Assert.Throws<ArgumentException>(()=>c.WithJapanPactScenario(d.ToString()));Assert.That(c.Scenarios.Count,Is.EqualTo(39));
        }
        [Test] public void Runtime_payload_manifest_contains_exact_pact_contract_and_fixture()
        {
            var m=PlayerPayloadManifest.Load(PlayInfluenceEventTests.Read("Tools/Configuration/player-payloads.json"));
            foreach(var path in new[]{"Data/Cards/deluxe-2015/treaty-protection.parameters.json","Docs/Contracts/treaty-protection.schema.json","Data/Demo/japan-pact-scenario.json"})Assert.That(m.Entries.SingleOrDefault(e=>e.Source==path)?.Destination,Is.EqualTo(System.IO.Path.GetFileName(path)));
        }
    }
}
