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
    public class DeploymentDemoTests
    {
        public const string ScenarioId="demo.setup.deployment";
        public static string Fixtures=>File.ReadAllText(Path.Combine(DefinitionTests.Root,"Data/Demo/deployment-scenario.json"));
        public static DemoCatalog Catalog(string fixtures=null)=>EraDemoTests.Catalog().WithDeploymentScenario(fixtures??Fixtures,SetupDeploymentTests.Source(),SetupDeploymentTests.Schema());
        public static CountryId Id(string n)=>new CountryId("country."+n);
        public static DemoController Controller()
        { var data=Catalog();var scenario=data.Scenarios.Last();var c=new DemoController(data.CreateHost);c.LoadScenario(scenario.Id,scenario.Actor,scenario.SelectedCountry);return c; }
        public static void Add(DemoController c,string country,int amount)
        { for(int i=0;i<amount;i++)Assert.That(c.ChangeDeploymentAllocation(Id(country),1),Is.True); }
        [Test] public void Catalog_adds_one_lesson_without_mutating_existing_eleven_or_faking_a_turn()
        {
            var before=EraDemoTests.Catalog();var after=before.WithDeploymentScenario(Fixtures,SetupDeploymentTests.Source(),SetupDeploymentTests.Schema());
            Assert.That(before.Scenarios.Count,Is.EqualTo(11));Assert.That(after.Scenarios.Count,Is.EqualTo(12));
            Assert.That(after.Scenarios.Take(11),Is.EqualTo(before.Scenarios));
            var s=after.Scenarios.Last();Assert.That(s.Actor,Is.EqualTo(PlayerSide.USSR));Assert.That(s.Ops,Is.Zero);
            var v=after.CreateHost(s.Id).UssrSession.ReadView();Assert.That(v.Deployment.Owner,Is.EqualTo(PlayerSide.USSR));
            Assert.That(v.Version,Is.EqualTo(1));Assert.That(v.Countries.Sum(x=>x.USSR),Is.EqualTo(9));Assert.That(v.Countries.Sum(x=>x.US),Is.Zero);
            Assert.That(v.Turn,Is.Null);Assert.That(v.Event,Is.Null);Assert.That(v.RemainingOps,Is.Zero);Assert.That(v.OwnHand,Has.Count.EqualTo(1));
        }
        [TestCase("non-fixture")] [TestCase("dataset")] [TestCase("version")] [TestCase("extra-amount")] [TestCase("extra-actor")]
        [TestCase("duplicate")] [TestCase("unknown-country")] [TestCase("outside-region")] [TestCase("missing-hand")]
        [TestCase("non-string-hand")] [TestCase("blank-hand")] [TestCase("too-many-hands")] [TestCase("missing-title")]
        public void Fixture_cannot_override_rules_or_publish_invalid_input(string kind)
        {
            var root=JObject.Parse(Fixtures);var row=(JObject)root["scenario"];
            switch(kind)
            {
                case "non-fixture":root["scope"]="official";break;
                case "dataset":root["dataset_id"]="unknown";break;
                case "version":root["schema_version"]=2;break;
                case "extra-amount":row["free_amount"]=99;break;
                case "extra-actor":row["actor"]="US";break;
                case "duplicate":row["id"]=EraDemoTests.Catalog().Scenarios[0].Id;break;
                case "unknown-country":row["selected_country"]="country.unknown";break;
                case "outside-region":row["selected_country"]="country.canada";break;
                case "missing-hand":((JObject)row["hands"]).Remove("US");break;
                case "non-string-hand":row["hands"]["US"]=new JArray(4);break;
                case "blank-hand":row["hands"]["US"]=new JArray(" ");break;
                case "too-many-hands":row["hands"]["US"]=new JArray(Enumerable.Repeat("fixture",21));break;
                case "missing-title":row.Remove("title");break;
            }
            Assert.That(()=>Catalog(root.ToString()),Throws.InstanceOf<ArgumentException>());
        }
        [Test] public void Readding_deployment_scenario_is_rejected()
        { Assert.That(()=>Catalog().WithDeploymentScenario(Fixtures,SetupDeploymentTests.Source(),SetupDeploymentTests.Schema()),Throws.InstanceOf<ArgumentException>()); }
        [Test] public async Task Controller_completes_both_sides_atomically_and_retry_does_not_reapply()
        {
            var c=Controller();var original=c.ReadView();var q=c.DeploymentTargets();Assert.That(c.DeploymentTargets(),Is.SameAs(q));
            Assert.That(q.RequiredAmount,Is.EqualTo(6));Add(c,"poland",6);Assert.That(c.CanConfirmDeployment,Is.True);
            Assert.That(c.ReadView().Version,Is.EqualTo(original.Version));Assert.That(c.ReadView().Countries.Sum(x=>x.USSR),Is.EqualTo(9));
            var result=await c.ConfirmDeploymentAsync();Assert.That(result.Accepted,Is.True);Assert.That(c.DeploymentDraft,Is.Empty);
            Assert.That(c.ReadView().Deployment.Owner,Is.EqualTo(PlayerSide.US));Assert.That(c.ReadView().Countries.Sum(x=>x.US),Is.EqualTo(18));
            Assert.That(c.DeploymentTargets(),Is.Not.SameAs(q));Assert.That(await c.RetryDeploymentAsync(),Is.SameAs(result));Assert.That(c.ReadView().Version,Is.EqualTo(2));
            c.SelectSide(PlayerSide.US);Add(c,"canada",7);var done=await c.ConfirmDeploymentAsync();Assert.That(done.Accepted,Is.True);
            var final=c.ReadView();Assert.That(final.Deployment.Complete,Is.True);Assert.That(final.Deployment.Owner,Is.Null);Assert.That(final.Version,Is.EqualTo(3));
            Assert.That(final.Countries.Sum(x=>x.US),Is.EqualTo(25));Assert.That(final.Countries.Sum(x=>x.USSR),Is.EqualTo(15));
            Assert.That(c.CanConfirmDeployment,Is.False);Assert.That(c.DeploymentTargets().Targets,Is.Empty);Assert.That(await c.RetryDeploymentAsync(),Is.SameAs(done));
            Assert.That(c.ReadView().Version,Is.EqualTo(3));Assert.That(c.ReadView().Turn,Is.Null);
        }
        [Test] public void Draft_has_budget_bounds_no_stability_cap_and_is_sorted_readonly_snapshot()
        {
            var c=Controller();Assert.That(c.CanConfirmDeployment,Is.False);Assert.That(c.ChangeDeploymentAllocation(Id("poland"),-1),Is.False);
            Assert.That(c.ChangeDeploymentAllocation(Id("canada"),1),Is.False);Assert.That(c.ChangeDeploymentAllocation(Id("unknown"),1),Is.False);
            Assert.That(c.ChangeDeploymentAllocation(Id("poland"),2),Is.False);Assert.That(c.ChangeDeploymentAllocation(Id("poland"),0),Is.False);
            Add(c,"poland",5);Add(c,"austria",1);var draft=c.DeploymentDraft;
            Assert.That(draft.Select(x=>x.Country),Is.EqualTo(new[]{Id("austria"),Id("poland")}));Assert.That(c.ChangeDeploymentAllocation(Id("finland"),1),Is.False);
            Assert.That(c.ChangeDeploymentAllocation(Id("poland"),-1),Is.True);Assert.That(c.CanConfirmDeployment,Is.False);Assert.That(draft.Sum(x=>x.Amount),Is.EqualTo(6));
            c.ClearDeploymentDraft();Assert.That(c.DeploymentDraft,Is.Empty);Assert.That(c.ReadView().Version,Is.EqualTo(1));
        }
        [Test] public async Task Incomplete_confirmation_cannot_change_authoritative_state()
        {
            var c=Controller();Add(c,"poland",1);var result=await c.ConfirmDeploymentAsync();Assert.That(result.Error,Is.EqualTo(RuleError.IncorrectAllocationTotal));
            Assert.That(c.ReadView().Version,Is.EqualTo(1));Assert.That(c.DeploymentDraft.Single().Amount,Is.EqualTo(1));
            Assert.That(await c.RetryDeploymentAsync(),Is.SameAs(result));
        }
        [Test] public void Switching_identity_clears_draft_and_wrong_owner_can_only_inspect()
        {
            var c=Controller();Add(c,"poland",2);c.SelectCountry(Id("finland"));Assert.That(c.DeploymentDraft,Has.Count.EqualTo(1));
            c.SelectSide(PlayerSide.US);Assert.That(c.DeploymentDraft,Is.Empty);Assert.That(c.CanConfirmDeployment,Is.False);
            Assert.That(c.DeploymentTargets().Targets,Is.Empty);Assert.That(c.ChangeDeploymentAllocation(Id("poland"),1),Is.False);
            c.SelectSide(PlayerSide.USSR);Assert.That(c.DeploymentDraft,Is.Empty);Assert.That(c.ReadView().Version,Is.EqualTo(1));
        }
        [Test] public async Task Reset_changes_game_and_clears_receipt_and_draft()
        {
            var c=Controller();Add(c,"poland",6);await c.ConfirmDeploymentAsync();var game=c.ReadView().GameId;
            c.LoadScenario(ScenarioId,PlayerSide.USSR,Id("poland"));Assert.That(c.ReadView().GameId,Is.Not.EqualTo(game));Assert.That(c.ReadView().Version,Is.EqualTo(1));
            Assert.That(c.LastDeploymentResult,Is.Null);Assert.That(c.CanRetryDeployment,Is.False);Assert.That(c.DeploymentDraft,Is.Empty);
            Assert.ThrowsAsync<InvalidOperationException>(async()=>await c.RetryDeploymentAsync());
        }
        [Test] public void Ordinary_lesson_has_no_deployment_actions()
        {
            var data=Catalog();var s=data.Scenarios[0];var c=new DemoController(data.CreateHost);c.LoadScenario(s.Id,s.Actor,s.SelectedCountry);
            Assert.That(c.DeploymentTargets().Error,Is.EqualTo(RuleError.SetupNotActive));Assert.That(c.CanConfirmDeployment,Is.False);
            Assert.That(c.ChangeDeploymentAllocation(Id("poland"),1),Is.False);Assert.That(c.DeploymentDraft,Is.Empty);
        }
        [Test] public void Build_payload_uses_same_three_new_sources()
        {
            ColdWarStruggle.Editor.DemoBuild.SynchronizeData();var dir=Path.Combine(UnityEngine.Application.streamingAssetsPath,"ColdWarStruggle");
            foreach(var file in new[]{"deployment-scenario.json","setup.json","setup.schema.json"})Assert.That(File.Exists(Path.Combine(dir,file)),Is.True,file);
            string Read(string file)=>File.ReadAllText(Path.Combine(dir,file));
            Assert.That(Read("deployment-scenario.json"),Is.EqualTo(Fixtures));Assert.That(Read("setup.json"),Is.EqualTo(SetupDeploymentTests.Source()));Assert.That(Read("setup.schema.json"),Is.EqualTo(SetupDeploymentTests.Schema()));
            Assert.That(EraDemoTests.Catalog().WithDeploymentScenario(Read("deployment-scenario.json"),Read("setup.json"),Read("setup.schema.json")).Scenarios.Count,Is.EqualTo(12));
        }
    }
}
