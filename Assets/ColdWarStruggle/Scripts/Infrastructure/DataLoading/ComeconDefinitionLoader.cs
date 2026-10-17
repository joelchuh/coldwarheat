using System;
using System.Collections.Generic;
using System.Linq;
using ColdWarStruggle.Core;
using Newtonsoft.Json.Linq;
namespace ColdWarStruggle.Infrastructure
{
    public static class ComeconDefinitionLoader
    {
        public static AddToDistinctCountriesEffect Load(string json,string schema,MapDefinition map,IReadOnlyCollection<string> knownEffectIds)
        {
            if(map==null || knownEffectIds==null)throw new ArgumentNullException();
            if(knownEffectIds.Count==0 || knownEffectIds.Any(string.IsNullOrWhiteSpace))throw new ArgumentException("Known effect references are required.");
            try
            {
                var root=ContractJson.Parse(json);var contract=ContractJson.Parse(schema) as JObject;
                if(contract==null || (string)contract["$id"]!="urn:coldwarheat:contract:early-influence-batch3:1")throw new ArgumentException("Unsupported Comecon contract.");
                ContractJson.Validate(root,contract);
                if((int)root["schema_version"]!=1 || (string)root["dataset_id"]!="effects.deluxe2015.early_influence_batch3"
                    || (string)root["content_version"]!="0.1.0" || (string)root["ruleset_id"]!="ruleset.deluxe2015" || (string)root["scope"]!="release-candidate")
                    throw new ArgumentException("Unsupported Comecon profile.");
                var sources=new HashSet<string>(StringComparer.Ordinal);
                foreach(var source in root["source_catalog"])
                    if(!sources.Add((string)source["id"]))throw new ArgumentException("Duplicate source.");
                // The shared document also contains Independent Reds; its reader remains independent.
                var p=root["parameters"]["comecon"];var card=(string)p["card_id"];var effect=(string)p["effect_id"];var region=(string)p["target_region_id"];
                if(card!="card.comecon" || effect!="effect.comecon" || !knownEffectIds.Contains(effect))throw new ArgumentException("Invalid Comecon identity or reference.");
                if((string)p["template"]!="AddToDistinctNonOpponentControlledCountries" || (string)p["beneficiary"]!="USSR"
                    || (string)p["target_control"]!="NotControlledByOpponent" || (string)p["shortage_policy"]!="RequiresRuling"
                    || region!="region.eastern_europe" || map.GetCountriesInRegion(region).Count==0)throw new ArgumentException("Unsupported Comecon semantics or map region.");
                var references=new HashSet<string>(StringComparer.Ordinal);
                foreach(var source in p["source_refs"])
                {
                    var id=(string)source["source_id"];var locator=(string)source["locator"];
                    if((string)source["verification"]!="Verified" || !sources.Contains(id) || !references.Add(id+"\n"+locator))throw new ArgumentException("Unverified or duplicate Comecon source.");
                }
                return new AddToDistinctCountriesEffect(card,effect,PlayerSide.USSR,new[]{region},(int)p["target_count"],(int)p["amount_per_country"],InfluenceControlFilter.NotOpponentControlled);
            }
            catch(OverflowException error) { throw new ArgumentException("Comecon parameter out of range.",error); }
        }
    }
}
