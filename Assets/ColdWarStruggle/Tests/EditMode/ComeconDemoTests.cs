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
    public sealed class ComeconDemoTests
    {
        internal static string Fixture()=>PlayInfluenceEventTests.Read("Data/Demo/comecon-scenario.json");
        internal static DemoCatalog Bind(DemoCatalog c)=>c.WithComecon(ComeconDefinitionTests.Data(),ComeconDefinitionTests.Schema(),PlayInfluenceEventTests.Read("Data/Cards/deluxe-2015/cards.json"));
        internal static DemoCatalog Catalog()=>Bind(InfluenceRelocationDemoTests.Catalog()).WithComeconScenario(Fixture());
        static async Task<DemoController> Waiting()
        {
            var data=Catalog();var scene=data.Scenarios.Single(s=>s.Id=="demo.practice.comecon");var c=new DemoController(data.CreateHost);c.LoadScenario(scene.Id,scene.Actor,scene.SelectedCountry);c.SelectCard(ComeconPlayTests.Card);Assert.That((await c.PlayCardAsync(CardActionKind.Event)).Accepted,Is.True);return c;
        }
        [Test] public void Registration_preserves_all_previous_features_and_rejects_duplicates()
        {
            var before=InfluenceRelocationDemoTests.Catalog();var after=Bind(before);Assert.That(after.Scenarios.Count,Is.EqualTo(before.Scenarios.Count));
            foreach(var scene in before.Scenarios.Where(s=>s.PracticeDefinition!=null))
            {
                var a=scene.PracticeDefinition;var b=after.Scenarios.Single(s=>s.Id==scene.Id).PracticeDefinition;
                Assert.That(b.Olympic,Is.SameAs(a.Olympic));Assert.That(b.SpaceEvent,Is.SameAs(a.SpaceEvent));Assert.That(b.Wars,Is.SameAs(a.Wars));Assert.That(b.FiveYearPlan,Is.SameAs(a.FiveYearPlan));Assert.That(b.Blockade,Is.SameAs(a.Blockade));Assert.That(b.HeadlineParameters,Is.SameAs(a.HeadlineParameters));Assert.That(b.FinalScoring,Is.SameAs(a.FinalScoring));Assert.That(b.China,Is.SameAs(a.China));Assert.That(b.Risk,Is.SameAs(a.Risk));Assert.That(b.NextTurnEnabled,Is.EqualTo(a.NextTurnEnabled));Assert.That(b.InfluenceEvents.Effects.Count,Is.EqualTo(a.InfluenceEvents.Effects.Count+1));
                foreach(var e in a.InfluenceEvents.Effects)Assert.That(b.InfluenceEvents.Effects[e.Key],Is.SameAs(e.Value));
            }
            Assert.Throws<ArgumentException>(()=>Bind(after));Assert.Throws<ArgumentException>(()=>Bind(PlayableDemoTests.Catalog()));
        }
        [Test] public void Fixture_is_explicit_turn_two_Soviet_card_and_preserves_source()
        {
            var c=Catalog();Assert.That(c.Scenarios.Count,Is.EqualTo(38));var scene=c.Scenarios.Single(s=>s.Id=="demo.practice.comecon");var v=c.CreateHost(scene.Id).UssrSession.ReadView();
            Assert.That(v.Practice.Actor,Is.EqualTo(PlayerSide.USSR));Assert.That(v.Practice.CompletedActions,Is.Zero);Assert.That(v.OwnHand,Has.Member(ComeconPlayTests.Card.Value));Assert.That(v.Event,Is.Null);Assert.That(v.Cards.ResolvingCount,Is.Zero);Assert.That(v.Countries.Single(x=>x.Definition.Id.Equals(ComeconPlayTests.C("hungary"))).US,Is.EqualTo(4));
        }
        [TestCase("scope")][TestCase("extra")][TestCase("source")][TestCase("turn")][TestCase("count")][TestCase("card")][TestCase("actor")][TestCase("duplicate-hand")][TestCase("late")][TestCase("duplicate-country")][TestCase("negative")][TestCase("unsafe-prep")][TestCase("missing")]
        public void Invalid_fixture_does_not_create_a_host(string fault)
        {
            var c=Bind(InfluenceRelocationDemoTests.Catalog());var data=JObject.Parse(Fixture());var row=data["scenarios"][0];
            switch(fault){case "scope":data["scope"]="release";break;case "extra":row["target_count"]=99;break;case "source":data["source_scenario_id"]="missing";break;case "turn":data["turn_number"]=1;break;case "count":((JArray)data["scenarios"]).Add(row.DeepClone());break;case "card":row["card"]="card.fidel";break;case "actor":row["actor"]="US";break;case "duplicate-hand":row["ussr_hand"][1]=row["us_hand"][0].DeepClone();break;case "late":row["us_hand"][0]="card.wargames";break;case "duplicate-country":((JArray)row["influence"]).Add(row["influence"][0].DeepClone());break;case "negative":row["influence"][0]["ussr"]=-1;break;case "unsafe-prep":row["ordinary_actions_to_prepare"]=1;break;case "missing":((JObject)row).Remove("actor");break;}
            Assert.That(()=>c.WithComeconScenario(data.ToString()),Throws.InstanceOf<ArgumentException>());Assert.That(c.Scenarios.Count,Is.EqualTo(37));
        }
        [Test] public async Task Map_draft_uses_legal_four_targets_and_survives_view_switch()
        {
            var c=await Waiting();var data=Catalog();using var p=new MapPresenter(c,data.Names);var tiles=DiagnosticMapView.Create(DiagnosticMapKind.Tiles);p.Attach(tiles);
            Assert.That(tiles.Frame.Countries.Single(x=>x.Id.Equals(ComeconPlayTests.C("hungary"))).Allowed,Is.False);Assert.That(tiles.Frame.Countries.Single(x=>x.Id.Equals(ComeconPlayTests.C("finland"))).Allowed,Is.True);Assert.That(c.ToggleEventTarget(ComeconPlayTests.C("hungary")),Is.False);
            var before=c.ReadView();foreach(var country in ComeconRulesTests.Targets().Take(3)){Assert.That(tiles.TrySelect(country),Is.True);Assert.That(c.CanConfirmEvent,Is.False);}
            var table=DiagnosticMapView.Create(DiagnosticMapKind.Table);p.Attach(table);Assert.That(table.Frame.Countries.Count(x=>x.Drafted),Is.EqualTo(3));Assert.That(table.TrySelect(ComeconRulesTests.Targets()[3]),Is.True);Assert.That(c.CanConfirmEvent,Is.True);Assert.That(c.ToggleEventTarget(ComeconPlayTests.C("bulgaria")),Is.False);Assert.That(c.ReadView().Version,Is.EqualTo(before.Version));
            var result=await c.ConfirmEventAsync();Assert.That(result.Accepted,Is.True,result.Error.ToString());var after=c.ReadView();Assert.That(after.Event,Is.Null);Assert.That(after.Cards.RemovedCount,Is.EqualTo(1));Assert.That(after.Practice.CompletedActions,Is.EqualTo(1));Assert.That(c.EventDraft,Is.Empty);
            var replay=await c.RetryEventAsync();Assert.That(replay,Is.SameAs(result));Assert.That(c.ReadView().Version,Is.EqualTo(after.Version));
        }
        [Test] public async Task Identity_change_and_reset_clear_local_selection()
        {
            var c=await Waiting();Assert.That(c.ToggleEventTarget(ComeconPlayTests.C("poland")),Is.True);c.SelectSide(PlayerSide.US);Assert.That(c.EventDraft,Is.Empty);Assert.That(c.CanConfirmEvent,Is.False);c.SelectSide(PlayerSide.USSR);Assert.That(c.EventDraft,Is.Empty);Assert.That(c.ToggleEventTarget(ComeconPlayTests.C("poland")),Is.True);c.LoadScenario("demo.practice.comecon",PlayerSide.USSR,ComeconPlayTests.C("poland"));Assert.That(c.EventDraft,Is.Empty);Assert.That(c.ReadView().Event,Is.Null);
        }
    }
}
