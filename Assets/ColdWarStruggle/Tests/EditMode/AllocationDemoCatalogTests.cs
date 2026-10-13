using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ColdWarStruggle.Application;
using ColdWarStruggle.Core;
using ColdWarStruggle.Infrastructure;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace ColdWarStruggle.Tests
{
    public class AllocationDemoCatalogTests
    {
        public static string Fixtures=>File.ReadAllText(Path.Combine(DefinitionTests.Root,"Data/Demo/allocation-scenarios.json"));
        public static string Cards=>File.ReadAllText(Path.Combine(DefinitionTests.Root,"Data/Cards/deluxe-2015/cards.json"));
        public static DemoCatalog Catalog()=>EventDemoCatalogTests.Catalog().WithAllocationScenarios(Fixtures,
            BudgetedRemovalCatalogTests.Parameters,BudgetedRemovalCatalogTests.Schema,Cards);
        static CountryId Id(string n)=>BudgetedRemovalTests.Id(n);
        static InfluenceAllocation A(string n,int amount)=>BudgetedRemovalTests.A(n,amount);

        [Test] public void Reference_index_reads_canonical_effect_ids_but_does_not_enable_handlers()
        {
            IReadOnlyCollection<string> ids=null;Assert.DoesNotThrow(()=>ids=CardEffectReferenceIndex.Load(Cards));
            Assert.That(ids.Count,Is.EqualTo(102));Assert.That(ids,Has.Member("effect.the_iron_lady"));Assert.That(ids,Has.Member("effect.suez_crisis"));
            Assert.That(ids,Has.No.Member("effect.china"));Assert.Throws<NotSupportedException>(()=>((IList<string>)ids).Clear());
            var parameters=BudgetedRemovalCatalogLoader.Load(BudgetedRemovalCatalogTests.Parameters,BudgetedRemovalCatalogTests.Schema,DefinitionTests.World(),ids);
            Assert.That(parameters.ContainsKey("card.suez_crisis"),Is.True);Assert.That(parameters.ContainsKey("card.the_iron_lady"),Is.False);
        }
        [TestCase("version")] [TestCase("ruleset")] [TestCase("scope")] [TestCase("dataset")]
        [TestCase("duplicateCard")] [TestCase("duplicateEffect")] [TestCase("missingBinding")]
        [TestCase("badCardId")] [TestCase("badEffectId")] [TestCase("missingId")] [TestCase("bindingType")]
        [TestCase("empty")] [TestCase("unknownKind")] [TestCase("falseChina")]
        public void Invalid_reference_index_is_rejected_before_loading_event_parameters(string kind)
        {
            var root=JObject.Parse(Cards);var cards=(JArray)root["cards"];var first=(JObject)cards[0];
            switch(kind)
            {
                case "version":root["content_version"]="99.0.0";break;
                case "ruleset":root["ruleset_id"]="ruleset.unknown";break;
                case "scope":root["scope"]="fixture";break;
                case "dataset":root["dataset_id"]="other";break;
                case "duplicateCard":cards[1]["id"]=first["id"].DeepClone();break;
                case "duplicateEffect":cards[1]["effect_binding"]["effect_id"]=first["effect_binding"]["effect_id"].DeepClone();break;
                case "missingBinding":first["effect_binding"]=null;break;
                case "badCardId":first["id"]="invalid";break;
                case "badEffectId":first["effect_binding"]["effect_id"]="effect.";break;
                case "missingId":first.Remove("id");break;
                case "bindingType":first["effect_binding"]="anything";break;
                case "empty":cards.Clear();break;
                case "unknownKind":first["kind"]="Other";break;
                case "falseChina":first["kind"]="China";first["effect_binding"]=null;break;
            }
            Assert.Catch<ArgumentException>(()=>CardEffectReferenceIndex.Load(root.ToString()));
        }

        [Test] public async Task Two_new_fixtures_complete_through_the_real_quantity_sessions()
        {
            DemoCatalog catalog=null;Assert.DoesNotThrow(()=>catalog=Catalog());Assert.That(catalog.Scenarios.Count,Is.EqualTo(9));
            foreach(var name in new[]{"socialist_governments","suez_crisis"})
            {
                var h=catalog.CreateHost("demo.event."+name);var started=await h.UsSession.SubmitEventAsync(EventSessionTests.Begin(h.UsSession));
                Assert.That(started.Accepted,Is.True);var s=h.UssrSession;var v=s.ReadView();var query=s.QueryEventTargets(v.Event.ExecutionId,v.Version);
                Assert.That(query.Kind,Is.EqualTo(InfluenceDecisionKind.Allocation));Assert.That(query.RequiredAmount,Is.EqualTo(3));
                var amounts=name=="socialist_governments"?new[]{A("austria",2),A("france",1)}:new[]{A("france",2),A("uk",1)};
                var result=await s.SubmitEventAsync(EventCommandEnvelope.Allocate("fixture-choice",v.GameId,v.Version,v.Event.ExecutionId,v.Event.DecisionId,amounts));
                Assert.That(result.Accepted,Is.True);Assert.That(result.View.Event.Stage,Is.EqualTo(InfluenceEventStage.Resolved));Assert.That(result.View.RemainingOps,Is.Zero);
                if(name=="suez_crisis")Assert.That(result.View.Countries.Single(c=>c.Definition.Id.Equals(Id("france"))).US,Is.EqualTo(1));
                else Assert.That(result.View.Countries.Single(c=>c.Definition.Id.Equals(Id("finland"))).US,Is.EqualTo(2));
                Assert.That(catalog.CreateHost("demo.event."+name).UsSession.ReadView().GameId,Is.Not.EqualTo(v.GameId));
            }
        }
        [TestCase("nonFixture")] [TestCase("unknownCard")] [TestCase("duplicate")] [TestCase("ops")]
        [TestCase("extraField")] [TestCase("negativeInfluence")] [TestCase("unknownCountry")]
        public void Allocation_fixtures_do_not_override_rules_or_publish_invalid_scenarios(string kind)
        {
            var root=JObject.Parse(Fixtures);var row=root["scenarios"][0];
            switch(kind)
            {
                case "nonFixture":root["scope"]="official";break;
                case "unknownCard":row["event_card"]="card.east_european_unrest";break;
                case "duplicate":row["id"]="demo.event.fidel";break;
                case "ops":row["ops"]=4;break;
                case "extraField":row["required_amount"]=99;break;
                case "negativeInfluence":row["influence"][0]["us"]=-1;break;
                case "unknownCountry":row["selected_country"]="country.imaginary";break;
            }
            Assert.Catch<ArgumentException>(()=>EventDemoCatalogTests.Catalog().WithAllocationScenarios(root.ToString(),
                BudgetedRemovalCatalogTests.Parameters,BudgetedRemovalCatalogTests.Schema,Cards));
        }
        [Test] public void Build_payload_contains_exact_allocation_sources_and_can_load_all_nine_lessons()
        {
            ColdWarStruggle.Editor.DemoBuild.SynchronizeData();
            var path=Path.Combine(UnityEngine.Application.streamingAssetsPath,"ColdWarStruggle");
            var files=new Dictionary<string,string> {
                ["allocation-scenarios.json"]=Fixtures,
                ["early-influence-batch2.parameters.json"]=BudgetedRemovalCatalogTests.Parameters,
                ["early-influence-batch2.schema.json"]=BudgetedRemovalCatalogTests.Schema,
                ["cards.json"]=Cards
            };
            foreach(var pair in files)
            {
                Assert.That(File.Exists(Path.Combine(path,pair.Key)),Is.True,"Missing runtime payload "+pair.Key);
                Assert.That(File.ReadAllText(Path.Combine(path,pair.Key)),Is.EqualTo(pair.Value));
            }
            string Read(string name)=>File.ReadAllText(Path.Combine(path,name));
            var catalog=DemoCatalog.Load(Read("world.json"),Read("map.schema.json"),Read("influence-scenarios.json"),Read("influence-placement.json"),Read("names.json"),Read("world.profile.json"))
                .WithEventScenarios(Read("event-scenarios.json"),Read("early-influence-batch1.parameters.json"),Read("early-influence-parameters.schema.json"))
                .WithAllocationScenarios(Read("allocation-scenarios.json"),Read("early-influence-batch2.parameters.json"),Read("early-influence-batch2.schema.json"),Read("cards.json"));
            Assert.That(catalog.Scenarios.Count,Is.EqualTo(9));
        }
    }
}
