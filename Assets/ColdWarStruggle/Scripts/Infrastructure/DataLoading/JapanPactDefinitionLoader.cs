using System;
using System.Collections.Generic;
using System.Linq;
using ColdWarStruggle.Core;
using Newtonsoft.Json.Linq;
namespace ColdWarStruggle.Infrastructure
{
    public static class JapanPactDefinitionLoader
    {
        public static JapanPactDefinition Load(string json,string schema,MapDefinition map,CardCatalog cards)
        {
            if(map==null || cards==null)throw new ArgumentNullException();
            var root=ContractJson.Parse(json);var contract=ContractJson.Parse(schema) as JObject;
            if(contract==null || (string)contract["$id"]!="urn:coldwarheat:contract:treaty-protection:1")throw new ArgumentException("Unsupported treaty contract.");
            ContractJson.Validate(root,contract);
            if(root is not JObject || root["schema_version"]?.Type!=JTokenType.Integer || (int)root["schema_version"]!=1
                || (string)root["dataset_id"]!="effects.deluxe2015.treaty_protection" || (string)root["content_version"]!="0.1.0"
                || (string)root["ruleset_id"]!="ruleset.deluxe2015" || (string)root["scope"]!="release-candidate")throw new ArgumentException("Unsupported treaty profile.");
            var sources=new Dictionary<string,string>(StringComparer.Ordinal)
            {
                {"C-DELUXE-EN","https://www.gmtgames.com/nnts/TS_Cards_Deluxe.pdf"},
                {"R2015","https://www.gmtgames.com/nnts/TS_Rules-2015.pdf"},
                {"F2010","https://www.gmtgames.com/nnts/FAQv5.pdf"}
            };
            if(root["source_catalog"] is not JArray catalog || catalog.Count!=sources.Count)throw new ArgumentException("Incomplete treaty source catalog.");
            var seen=new HashSet<string>(StringComparer.Ordinal);
            foreach(var s in catalog)
            {
                var id=(string)s["id"];
                if(string.IsNullOrWhiteSpace(id) || !seen.Add(id) || !sources.TryGetValue(id,out var url) || (string)s["url"]!=url
                    || string.IsNullOrWhiteSpace((string)s["edition"]) || string.IsNullOrWhiteSpace((string)s["description"]))throw new ArgumentException("Invalid treaty source.");
            }
            var p=root["parameters"]["us_japan_mutual_defense_pact"];
            if((string)p["template"]!="ActivateTreatyProtection" || (string)p["duration"]!="UntilCancelledOrGameEnd" || p["expiry_boundary"]?.Type!=JTokenType.Null
                || (string)p["activation_gate"]?["kind"]!="None" || p["activation_gate"]?["card_ids"] is not JArray gate || gate.Count!=0
                || p["immediate_influence"] is not JObject grant || (string)grant["template"]!="AddToControl" || (string)grant["side"]!="US"
                || (string)p["target_scope"]?["kind"]!="SpecificCountry" || (string)p["target_scope"]?["country_id"]!=(string)grant["country_id"]
                || p["country_exceptions"] is not JArray exceptions || exceptions.Count!=0)throw new ArgumentException("Unsupported Japan pact semantics.");
            if(p["prohibitions"] is not JArray prohibitions || prohibitions.Count!=2
                || !prohibitions.Select(x=>(string)x["action"]).OrderBy(x=>x,StringComparer.Ordinal).SequenceEqual(new[]{"Coup","Realignment"})
                || prohibitions.Any(x=>(string)x["actor"]!="USSR" || x["source_card_id"]?.Type!=JTokenType.Null))throw new ArgumentException("Unsupported Japan pact prohibitions.");
            if(p["source_refs"] is not JArray refs || refs.Count!=2)throw new ArgumentException("Incomplete Japan source references.");
            seen.Clear();
            foreach(var reference in refs)
            {
                var id=(string)reference["source_id"];
                if((id!="R2015" && id!="C-DELUXE-EN") || !seen.Add(id) || (string)reference["verification"]!="Verified"
                    || string.IsNullOrWhiteSpace((string)reference["locator"]))throw new ArgumentException("Invalid Japan source reference.");
            }
            return new JapanPactDefinition(map,cards,new AddToControlInfluenceEffect((string)p["card_id"],(string)p["effect_id"],PlayerSide.US,new CountryId((string)grant["country_id"])),
                (string)root["ruleset_id"],(string)root["content_version"],CanonicalJsonDigest.Compute(root));
        }
    }
}
