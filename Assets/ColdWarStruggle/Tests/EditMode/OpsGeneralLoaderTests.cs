using System;
using System.IO;
using System.Linq;
using ColdWarStruggle.Core;
using ColdWarStruggle.Infrastructure;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public sealed class OpsGeneralLoaderTests
    {
        static string Source()=>File.ReadAllText(Path.Combine(DefinitionTests.Root,"Data/Cards/deluxe-2015/persistent-effects-batch1.parameters.json"));
        static string Schema()=>File.ReadAllText(Path.Combine(DefinitionTests.Root,"Docs/Contracts/persistent-effects-batch1.schema.json"));
        [Test]
        public void Actual_policy_produces_general_quote_and_space_request_without_copying_facts()
        {
            var definition=OpsGeneralDefinitionLoader.LoadSubset(Source(),Schema());var card=OpsGeneralTests.Card(2);
            Assert.That(definition.Minimum,Is.EqualTo(1));Assert.That(definition.OrdinaryMaximum,Is.EqualTo(4));Assert.That(definition.FlatPurposes.Count,Is.EqualTo(5));
            var quote=OpsGeneralRules.QuoteCard(definition,card,PlayerSide.USSR,OpsValuePurpose.Space,new[]{new OpsFlatModifier("plus",PlayerSide.USSR,1)});
            var request=SpaceAttemptRequest.FromQuote("attempt","action",1,quote);
            Assert.That(request.Card,Is.SameAs(card));Assert.That(request.Actor,Is.EqualTo(PlayerSide.USSR));Assert.That(request.EffectiveOps,Is.EqualTo(3));
            var state=SpaceAttemptTests.State(4,4);
            Assert.That(SpaceAttemptRules.ValidateAttempt(state,request),Is.EqualTo(SpaceAttemptEligibility.Allowed));
            var random=Pcg32Random.FromSeed(42,54);var count=random.Capture().DrawCount;
            var plan=SpaceAttemptRules.PlanAttempt(state,request,random);
            Assert.That(plan.After.UssrAttempts,Is.EqualTo(1));Assert.That(plan.After.UsAttempts,Is.Zero);Assert.That(random.Capture().DrawCount,Is.EqualTo(count));
            var low=SpaceAttemptRequest.FromQuote("low","action",1,OpsGeneralRules.QuoteCard(definition,card,PlayerSide.USSR,OpsValuePurpose.Space,Array.Empty<OpsFlatModifier>()));
            Assert.That(SpaceAttemptRules.ValidateAttempt(state,low),Is.EqualTo(SpaceAttemptEligibility.InsufficientOps));
        }
        [Test]
        public void Loader_reads_bounds_from_data_instead_of_another_constant_table()
        {
            var j=JObject.Parse(Source());j["ops_policy"]["minimum"]=2;j["ops_policy"]["ordinary_maximum"]=6;
            var definition=OpsGeneralDefinitionLoader.LoadSubset(j.ToString(),Schema());
            Assert.That(OpsGeneralRules.QuoteCard(definition,OpsGeneralTests.Card(4),PlayerSide.US,OpsValuePurpose.Space,new[]{new OpsFlatModifier("plus",PlayerSide.US,5)}).GeneralValue,Is.EqualTo(6));
            Assert.That(OpsGeneralRules.QuoteCard(definition,OpsGeneralTests.Card(1),PlayerSide.US,OpsValuePurpose.Space,Array.Empty<OpsFlatModifier>()).GeneralValue,Is.EqualTo(2));
        }
        [Test]
        public void Source_and_purpose_order_are_not_business_facts()
        {
            var j=JObject.Parse(Source());j["source_catalog"]=new JArray(((JArray)j["source_catalog"]).Reverse().Select(x=>x.DeepClone()));
            j["ops_policy"]["flat_purposes"]=new JArray(((JArray)j["ops_policy"]["flat_purposes"]).Reverse().Select(x=>x.DeepClone()));
            Assert.That(OpsGeneralDefinitionLoader.LoadSubset(j.ToString(),Schema()).FlatPurposes.Count,Is.EqualTo(5));
        }
        [TestCase("schema")] [TestCase("version")] [TestCase("dataset")] [TestCase("ruleset")] [TestCase("scope")]
        [TestCase("min")] [TestCase("bounds")] [TestCase("float")] [TestCase("string")] [TestCase("overflow")] [TestCase("policy")]
        [TestCase("missingPurpose")] [TestCase("duplicatePurpose")] [TestCase("unknownPurpose")] [TestCase("missingRegionalPurpose")]
        [TestCase("sourceId")] [TestCase("sourceUrl")] [TestCase("duplicateSource")] [TestCase("sourceMissing")]
        [TestCase("pendingRef")] [TestCase("unknownRef")] [TestCase("duplicateRef")] [TestCase("extra")] [TestCase("missing")]
        public void Broken_policy_and_unverified_sources_rejected(string defect)
        {
            var j=JObject.Parse(Source());var p=(JObject)j["ops_policy"];var sources=(JArray)j["source_catalog"];var refs=(JArray)p["source_refs"];
            switch(defect)
            {
                case "schema":j["schema_version"]=2;break;case "version":j["content_version"]="9.0.0";break;case "dataset":j["dataset_id"]="other";break;
                case "ruleset":j["ruleset_id"]="other";break;case "scope":j["scope"]="pending";break;case "min":p["minimum"]=0;break;case "bounds":p["minimum"]=5;break;
                case "float":p["minimum"]=1.0;break;case "string":p["ordinary_maximum"]="4";break;case "overflow":p["ordinary_maximum"]=long.MaxValue;break;
                case "policy":p["aggregation"]="ClipEach";break;case "missingPurpose":((JArray)p["flat_purposes"]).RemoveAt(0);break;
                case "duplicatePurpose":p["flat_purposes"][0]=p["flat_purposes"][1].DeepClone();break;case "unknownPurpose":p["flat_purposes"][0]="Headline";break;
                case "missingRegionalPurpose":((JArray)p["regional_purposes"]).RemoveAt(0);break;
                case "sourceId":sources[0]["id"]="other";break;case "sourceUrl":sources[0]["url"]="https://example.org";break;
                case "duplicateSource":sources[1]=sources[0].DeepClone();break;case "sourceMissing":sources.RemoveAt(0);break;
                case "pendingRef":refs[0]["verification"]="Pending";break;case "unknownRef":refs[0]["source_id"]="other";break;
                case "duplicateRef":refs[1]=refs[0].DeepClone();break;case "extra":p["fallback"]=1;break;case "missing":p.Remove("minimum");break;
            }
            Assert.That(()=>OpsGeneralDefinitionLoader.LoadSubset(j.ToString(),Schema()),Throws.InstanceOf<ArgumentException>());
        }
        [Test]
        public void Duplicate_json_invalid_roots_and_wrong_contract_rejected()
        {
            var duplicate=Source().Replace("\"minimum\": 1","\"minimum\": 1, \"minimum\": 1");Assert.That(duplicate,Is.Not.EqualTo(Source()));
            foreach(var json in new[]{duplicate,null,"[]","{}",Source()+" {}",new string(' ',2000001)})
                Assert.That(()=>OpsGeneralDefinitionLoader.LoadSubset(json,Schema()),Throws.InstanceOf<ArgumentException>());
            foreach(var schema in new[]{null,"[]","{}",Schema().Replace("urn:coldwarheat:contract:persistent-effects-batch1:1","other")})
                Assert.That(()=>OpsGeneralDefinitionLoader.LoadSubset(Source(),schema),Throws.InstanceOf<ArgumentException>());
        }
        [Test]
        public void Factory_does_not_turn_nonspace_or_as_if_quote_into_physical_space_card()
        {
            var definition=OpsGeneralTests.Definition();var empty=Array.Empty<OpsFlatModifier>();
            Assert.Throws<ArgumentNullException>(()=>SpaceAttemptRequest.FromQuote("a","s",1,null));
            foreach(var purpose in new[]{OpsValuePurpose.Influence,OpsValuePurpose.Coup,OpsValuePurpose.Realignment,OpsValuePurpose.OpsThreshold})
            {
                var quote=OpsGeneralRules.QuoteCard(definition,OpsGeneralTests.Card(),PlayerSide.US,purpose,empty);
                Assert.Throws<ArgumentException>(()=>SpaceAttemptRequest.FromQuote("a","s",1,quote));
            }
            var asIf=OpsGeneralRules.QuoteAsIfCard(definition,"event",3,PlayerSide.US,OpsValuePurpose.Coup,empty);
            Assert.Throws<ArgumentException>(()=>SpaceAttemptRequest.FromQuote("a","s",1,asIf));
        }
        [Test]
        public void Factory_still_checks_request_id_source_and_turn()
        {
            var quote=OpsGeneralRules.QuoteCard(OpsGeneralTests.Definition(),OpsGeneralTests.Card(),PlayerSide.US,OpsValuePurpose.Space,Array.Empty<OpsFlatModifier>());
            Assert.That(()=>SpaceAttemptRequest.FromQuote(" ","s",1,quote),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>SpaceAttemptRequest.FromQuote("a"," ",1,quote),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>SpaceAttemptRequest.FromQuote("a","s",0,quote),Throws.InstanceOf<ArgumentException>());
        }
    }
}
