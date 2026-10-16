using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ColdWarStruggle.Core;
using ColdWarStruggle.Application;
using ColdWarStruggle.Infrastructure;
using ColdWarStruggle.Presentation;
using ColdWarStruggle.Editor;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class EmptyHandDemoTests
    {
        internal const string Id="demo.practice.empty_hand";
        internal static string Data()=>FinalScoringDemoEntryTests.Read("Data/Demo/empty-hand-scenario.json");
        internal static DemoCatalog Catalog()=>SpaceExtraDemoEntryTests.Catalog().WithEmptyHandScenario(Data());
        [Test] public void New_fixture_preserves_all_previous_scenarios_and_prepares_real_ordinary_slots()
        {
            var old=SpaceExtraDemoEntryTests.Catalog();var c=old.WithEmptyHandScenario(Data());Assert.That(c.Scenarios.Count,Is.EqualTo(21));foreach(var scene in old.Scenarios)Assert.That(c.Scenarios.Single(x=>x.Id==scene.Id),Is.SameAs(scene));
            var source=c.Scenarios.Single(x=>x.Id==FinalScoringDemoEntryTests.Id);var created=c.Scenarios.Single(x=>x.Id==Id);Assert.That(created.Countries,Is.SameAs(source.Countries));Assert.That(created.Title,Does.Contain("人工局面"));
            var h=c.CreateHost(Id);var su=h.UssrSession.ReadView();var us=h.UsSession.ReadView();Assert.That(su.Version,Is.EqualTo(21));Assert.That(su.Practice.CompletedActions,Is.EqualTo(10));Assert.That(su.Practice.Round,Is.EqualTo(6));Assert.That(su.Practice.CanSkipEmptyHand,Is.True);Assert.That(us.Practice.CanSkipEmptyHand,Is.False);
            Assert.That(su.OwnHand,Is.Empty);Assert.That(su.OpponentHandCount,Is.EqualTo(1));Assert.That(us.OwnHand.Single(),Is.EqualTo("card.olympic_games"));Assert.That(su.Cards.China.Owner,Is.EqualTo(PlayerSide.USSR));Assert.That(su.Cards.China.Face,Is.EqualTo(CardFace.FaceUp));Assert.That(su.Practice.UsSpace+su.Practice.UssrSpace,Is.Zero);
            Assert.That(c.CreateHost(Id).UsSession.ReadView().GameId,Is.Not.EqualTo(su.GameId));
        }
        [TestCase(false)] [TestCase(true)] public async Task Skip_or_voluntary_China_both_reach_audit_and_next_turn(bool china)
        {
            var c=Catalog();var h=c.CreateHost(Id);var controller=new DemoController(_=>h);controller.LoadScenario(Id,PlayerSide.USSR,ChinaCardPlayTests.C("iran"));Assert.That(controller.CanSkipEmptyHand,Is.True);
            if(china){controller.SelectCard(ChinaCardPlayTests.China);Assert.That((await controller.PlayCardAsync(CardActionKind.Influence)).Accepted,Is.True);Assert.That(controller.CanSkipEmptyHand,Is.False);Assert.That((await controller.FinishInfluenceAsync()).Accepted,Is.True);}
            else {var r=await controller.SkipEmptyHandAsync();Assert.That(r.Accepted,Is.True);var version=controller.ReadView().Version;Assert.That(await controller.RetryCardAsync(),Is.SameAs(r));Assert.That(controller.ReadView().Version,Is.EqualTo(version));}
            var after=controller.ReadView();Assert.That(after.Practice.Actor,Is.EqualTo(PlayerSide.US));Assert.That(after.Practice.CompletedActions,Is.EqualTo(11));Assert.That(after.Cards.China.Owner,Is.EqualTo(china?PlayerSide.US:PlayerSide.USSR));
            controller.SelectSide(PlayerSide.US);Assert.That(controller.CanRetryCard,Is.False);controller.SelectCard(new CardId("card.olympic_games"));Assert.That((await controller.PlayCardAsync(CardActionKind.Influence)).Accepted,Is.True);Assert.That((await controller.FinishInfluenceAsync()).Accepted,Is.True);
            Assert.That(controller.ReadView().Practice.TurnEndAvailable,Is.True);Assert.That((await controller.AssessTurnEndAsync()).Accepted,Is.True);Assert.That((await controller.BeginNextTurnAsync()).Accepted,Is.True);Assert.That(controller.ReadView().Turn.Number,Is.EqualTo(3));Assert.That(controller.ReadView().OwnHand,Has.Count.EqualTo(8));Assert.That(controller.ReadView().Practice.CompletedActions,Is.Zero);
        }
        [Test] public async Task Controller_queries_bound_identity_and_cannot_skip_with_cards()
        {
            var h=new LocalGameHost(EmptyHandActionTests.Ready(),true);var c=new DemoController(_=>h);c.LoadScenario("empty",PlayerSide.US,ChinaCardPlayTests.C("iran"));Assert.That(c.CanSkipEmptyHand,Is.False);Assert.That((await c.SkipEmptyHandAsync()).Error,Is.EqualTo(RuleError.WrongActor));
            c.SelectSide(PlayerSide.USSR);Assert.That(c.CanSkipEmptyHand,Is.True);var done=await c.SkipEmptyHandAsync();Assert.That(done.Accepted,Is.True);Assert.That(c.CanSkipEmptyHand,Is.False);Assert.That(c.ReadView().OwnHand,Is.Empty);
        }
        [TestCase("scope")] [TestCase("dataset")] [TestCase("field")] [TestCase("duplicate")] [TestCase("source")] [TestCase("turn")] [TestCase("future")] [TestCase("scoring")] [TestCase("China")] [TestCase("source-card")]
        public void Malformed_or_incompatible_fixture_is_rejected(string fault)
        {
            var d=JObject.Parse(Data());var row=(JObject)d["scenario"];
            switch(fault){case "scope":d["scope"]="production";break;case "dataset":d["dataset_id"]="unknown";break;case "field":row["completed_actions"]=10;break;case "duplicate":row["id"]=SpaceExtraDemoEntryTests.EarlyId;break;case "source":row["source_scenario_id"]=SpaceExtraDemoEntryTests.EarlyId;break;case "turn":row["turn_number"]=1;break;case "future":row["remaining_card"]="card.wargames";break;case "scoring":row["remaining_card"]="card.asia_scoring";break;case "China":row["remaining_card"]="card.china_card";break;case "source-card":row["remaining_card"]="card.fidel";break;}
            Assert.Throws<ArgumentException>(()=>SpaceExtraDemoEntryTests.Catalog().WithEmptyHandScenario(d.ToString()));
        }
        [Test] public void Duplicate_fixture_is_rejected()=>Assert.Throws<ArgumentException>(()=>Catalog().WithEmptyHandScenario(Data()));
        [Test] public void Fixture_payload_and_build_entry_are_available_to_player()
        {
            DemoBuild.SynchronizeData();var target=Path.Combine(UnityEngine.Application.streamingAssetsPath,"ColdWarStruggle","empty-hand-scenario.json");Assert.That(File.Exists(target),Is.True);
            var source=Path.Combine(DefinitionTests.Root,"Data/Demo/empty-hand-scenario.json");Assert.That(File.ReadAllBytes(target),Is.EqualTo(File.ReadAllBytes(source)));
            Assert.That(typeof(DemoBuild).GetMethod("BuildEmptyHandPlaytest"),Is.Not.Null);
        }
    }
}
