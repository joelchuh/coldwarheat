using System;
using System.Collections.Generic;
using System.Linq;
using ColdWarStruggle.Core;
using Newtonsoft.Json.Linq;
namespace ColdWarStruggle.Infrastructure
{
    public static class EventFactDefinitionLoader
    {
        public static InfluenceEventFactDefinition Load(string json,string schema,MapDefinition map,CardCatalog cards,IEnumerable<InfluenceEffectDefinition> effects)
        {
            if(map==null || cards==null || effects==null)throw new ArgumentNullException();
            var root=ContractJson.Parse(json);var contract=ContractJson.Parse(schema) as JObject;
            if(contract==null || (string)contract["$id"]!="urn:coldwarheat:contract:treaty-protection:1")throw new ArgumentException("Unsupported event fact contract.");
            ContractJson.Validate(root,contract);
            if(root is not JObject || root["schema_version"]?.Type!=JTokenType.Integer || (int)root["schema_version"]!=1
                || (string)root["dataset_id"]!="effects.deluxe2015.treaty_protection" || (string)root["content_version"]!="0.1.0"
                || (string)root["ruleset_id"]!="ruleset.deluxe2015" || (string)root["scope"]!="release-candidate")throw new ArgumentException("Unsupported event fact profile.");
            var sources=new Dictionary<string,string>(StringComparer.Ordinal)
            {
                {"C-DELUXE-EN","https://www.gmtgames.com/nnts/TS_Cards_Deluxe.pdf"},
                {"R2015","https://www.gmtgames.com/nnts/TS_Rules-2015.pdf"},
                {"F2010","https://www.gmtgames.com/nnts/FAQv5.pdf"}
            };
            if(root["source_catalog"] is not JArray catalog || catalog.Count!=sources.Count)throw new ArgumentException("Incomplete event fact source catalog.");
            var seen=new HashSet<string>(StringComparer.Ordinal);
            foreach(var source in catalog)
            {
                var id=(string)source["id"];
                if(string.IsNullOrWhiteSpace(id) || !seen.Add(id) || !sources.TryGetValue(id,out var url) || (string)source["url"]!=url
                    || string.IsNullOrWhiteSpace((string)source["edition"]) || string.IsNullOrWhiteSpace((string)source["description"]))throw new ArgumentException("Invalid event fact source.");
            }
            var policy=root["event_fact_policy"];
            // Data uses ResolvedChanged; the existing influence engine calls the same result ResolvedChange.
            if(!ExactSet(policy["qualifying_outcomes"],"ResolvedChanged","ResolvedNoChange")
                || !ExactSet(policy["nonqualifying_outcomes"],"NotTriggered","BlockedPrerequisite","Suppressed","Cancelled")
                || (string)policy["unknown_history"]!="RequiresRuling" || policy["source_refs"] is not JArray refs || refs.Count!=1
                || (string)refs[0]["source_id"]!="R2015" || (string)refs[0]["verification"]!="Verified"
                || string.IsNullOrWhiteSpace((string)refs[0]["locator"]))throw new ArgumentException("Unsupported event fact semantics.");
            return new InfluenceEventFactDefinition(map,cards,effects,(string)root["ruleset_id"],(string)root["content_version"],CanonicalJsonDigest.Compute(root));
        }
        static bool ExactSet(JToken value,params string[] expected)=>value is JArray array && array.Count==expected.Length
            && array.Select(x=>(string)x).OrderBy(x=>x,StringComparer.Ordinal).SequenceEqual(expected.OrderBy(x=>x,StringComparer.Ordinal));
    }
}
