using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ColdWarStruggle.Core;
using ColdWarStruggle.Infrastructure;
using ColdWarStruggle.Presentation;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class DealtDemoCatalogTests
    {
        public const string ScenarioId="demo.setup.initial_deal";
        public static string Fixtures=>File.ReadAllText(Path.Combine(DefinitionTests.Root,"Data/Demo/initial-deal-scenario.json"));
        public static string Names=>File.ReadAllText(Path.Combine(DefinitionTests.Root,"Data/Localization/en-US.catalog-names.json"));
        public static DemoCatalog Catalog(string fixtures=null,string names=null)=>DeploymentDemoTests.Catalog().WithInitialDealScenario(fixtures??Fixtures,SetupDeploymentTests.Source(),SetupDeploymentTests.Schema(),TurnCalendarTests.Source(),CardCatalogLoaderTests.Load(),names??Names);
        [Test] public void New_lesson_has_real_hands_while_previous_twelve_remain_unchanged()
        {
            var original=DeploymentDemoTests.Catalog();var printed=CardCatalogLoaderTests.Load();
            var next=original.WithInitialDealScenario(Fixtures,SetupDeploymentTests.Source(),SetupDeploymentTests.Schema(),TurnCalendarTests.Source(),printed,Names);
            Assert.That(next.Scenarios,Has.Count.EqualTo(13));Assert.That(original.Scenarios,Has.Count.EqualTo(12));Assert.That(next.Scenarios.Take(12),Is.EqualTo(original.Scenarios));
            Assert.That(next.PrintedCards,Is.SameAs(printed));var lesson=next.Scenarios.Last();Assert.That(lesson.InitialCards,Is.Not.Null);Assert.That(lesson.Hands,Is.Null);
            var host=next.CreateHost(ScenarioId);var v=host.UssrSession.ReadView();Assert.That(v.OwnHand,Has.Count.EqualTo(8));Assert.That(v.OpponentHandCount,Is.EqualTo(8));
            Assert.That(v.Cards.DrawPileCount,Is.EqualTo(19));Assert.That(v.Turn.Number,Is.EqualTo(1));Assert.That(v.Deployment.Owner,Is.EqualTo(PlayerSide.USSR));Assert.That(v.RemainingOps,Is.Zero);
            Assert.That(v.Countries.Sum(c=>c.USSR),Is.EqualTo(9));Assert.That(v.Countries.Sum(c=>c.US),Is.Zero);
            foreach(var card in printed.Cards)Assert.That(next.Names.ContainsKey(card.Id.Value),Is.True);
            Assert.That(next.CreateHost(DeploymentDemoTests.ScenarioId).UsSession.ReadView().Cards,Is.Null);
        }
        [Test] public void Reset_creates_new_game_but_repeats_the_published_fixture_without_mutating_it()
        {
            var data=Catalog();var a=data.CreateHost(ScenarioId);var b=data.CreateHost(ScenarioId);
            Assert.That(a.UsSession.ReadView().GameId,Is.Not.EqualTo(b.UsSession.ReadView().GameId));
            Assert.That(a.ReadLocalTestSnapshot().USHand,Is.EqualTo(b.ReadLocalTestSnapshot().USHand));Assert.That(a.ReadLocalTestSnapshot().USSRHand,Is.EqualTo(b.ReadLocalTestSnapshot().USSRHand));
            var snapshot=RandomSnapshotJson.Deserialize(JObject.Parse(Fixtures)["scenario"]["random_state"].ToString());Pcg32RandomTests.Same(Pcg32Random.FromSeed(42,54).Capture(),snapshot);
        }
        [Test] public async Task Controller_deploys_without_redealing_when_identity_changes_or_receipt_is_retried()
        {
            var data=Catalog();var controller=new DemoController(data.CreateHost);controller.LoadScenario(ScenarioId,PlayerSide.USSR,new CountryId("country.poland"));
            var before=controller.ReadBoth();DeploymentDemoTests.Add(controller,"poland",6);Assert.That((await controller.ConfirmDeploymentAsync()).Accepted,Is.True);
            controller.SelectSide(PlayerSide.US);DeploymentDemoTests.Add(controller,"canada",7);Assert.That((await controller.ConfirmDeploymentAsync()).Accepted,Is.True);
            controller.SelectSide(PlayerSide.USSR);Assert.That(controller.CanRetryDeployment,Is.False);
            controller.SelectSide(PlayerSide.US);Assert.That((await controller.RetryDeploymentAsync()).Accepted,Is.True);
            var after=controller.ReadBoth();Assert.That(after.USHand,Is.EqualTo(before.USHand));Assert.That(after.USSRHand,Is.EqualTo(before.USSRHand));Assert.That(controller.ReadView().Deployment.Complete,Is.True);
        }
        [TestCase("scope")] [TestCase("version")] [TestCase("dataset")] [TestCase("duplicate")] [TestCase("country")] [TestCase("region")]
        [TestCase("hands")] [TestCase("amount")] [TestCase("actor")] [TestCase("random-version")] [TestCase("random-extra")] [TestCase("random-even")] [TestCase("random-exhausted")]
        [TestCase("missing-card-name")] [TestCase("blank-card-name")]
        public void Invalid_fixture_rules_overrides_and_card_names_are_rejected(string defect)
        {
            var root=JObject.Parse(Fixtures);var row=root["scenario"];var labels=JObject.Parse(Names);
            switch(defect)
            {
                case "scope":root["scope"]="official";break;case "version":root["schema_version"]=2;break;case "dataset":root["dataset_id"]="other";break;
                case "duplicate":row["id"]=DeploymentDemoTests.ScenarioId;break;case "country":row["selected_country"]="country.unknown";break;case "region":row["selected_country"]="country.brazil";break;
                case "hands":row["hands"]=new JObject();break;case "amount":row["amount"]=6;break;case "actor":row["actor"]="US";break;
                case "random-version":row["random_state"]["schema_version"]=2;break;case "random-extra":row["random_state"]["seed"]=42;break;case "random-even":row["random_state"]["increment"]="0000000000000002";break;
                case "random-exhausted":row["random_state"]["draw_count"]="ffffffffffffffff";break;
                case "missing-card-name":((JObject)labels["entries"]).Remove("card.fidel.name");break;case "blank-card-name":labels["entries"]["card.fidel.name"]=" ";break;
            }
            if(defect=="random-exhausted")Assert.Throws<OverflowException>(()=>Catalog(root.ToString(),labels.ToString()));
            else Assert.That(()=>Catalog(root.ToString(),labels.ToString()),Throws.InstanceOf<ArgumentException>());
        }
        [Test] public void Default_launch_prefers_dealt_setup_but_explicit_selection_and_legacy_catalog_still_work()
        {
            var ids=new[]{"old",ScenarioId,"third"};Assert.That(DemoLaunchOptions.ScenarioIndex(Array.Empty<string>(),ids),Is.EqualTo(1));
            Assert.That(DemoLaunchOptions.ScenarioIndex(new[]{"--demo-scenario=old"},ids),Is.Zero);
            Assert.That(DemoLaunchOptions.ScenarioIndex(Array.Empty<string>(),new[]{"old","third"}),Is.Zero);
            Assert.Throws<ArgumentException>(()=>DemoLaunchOptions.ScenarioIndex(new[]{"--demo-scenario=unknown"},ids));
        }
    }
}
