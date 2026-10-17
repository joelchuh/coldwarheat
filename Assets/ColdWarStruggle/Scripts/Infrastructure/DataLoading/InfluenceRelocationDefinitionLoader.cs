using System;
using System.Collections.Generic;
using System.Linq;
using ColdWarStruggle.Core;
using Newtonsoft.Json.Linq;
namespace ColdWarStruggle.Infrastructure
{
    public static class InfluenceRelocationDefinitionLoader
    {
        public static RelocateOwnInfluenceEffect Load(string json,string schema,IReadOnlyCollection<string> knownEffectIds)
        {
            if(knownEffectIds==null)throw new ArgumentNullException(nameof(knownEffectIds));
            if(knownEffectIds.Count==0 || knownEffectIds.Any(string.IsNullOrWhiteSpace))throw new ArgumentException("Known effect references are required.");
            try
            {
                var root=ContractJson.Parse(json);var contract=ContractJson.Parse(schema) as JObject;
                if(contract==null || (string)contract["$id"]!="urn:coldwarheat:contract:influence-relocation:1")throw new ArgumentException("Unsupported relocation contract.");
                ContractJson.Validate(root,contract);
                if(root["schema_version"].Type!=JTokenType.Integer || (int)root["schema_version"]!=1
                    || (string)root["dataset_id"]!="effects.deluxe2015.influence_relocation" || (string)root["content_version"]!="0.1.0"
                    || (string)root["ruleset_id"]!="ruleset.deluxe2015" || (string)root["scope"]!="release-candidate")throw new ArgumentException("Unsupported relocation profile.");
                var sources=new HashSet<string>(StringComparer.Ordinal);
                foreach(var row in root["source_catalog"])
                    if(string.IsNullOrWhiteSpace((string)row["id"]) || !sources.Add((string)row["id"]))throw new ArgumentException("Invalid relocation source.");
                var p=root["parameters"]["de_stalinization"];var references=new HashSet<string>(StringComparer.Ordinal);
                foreach(var row in p["source_refs"])
                    if((string)row["verification"]!="Verified" || !sources.Contains((string)row["source_id"]) || !references.Add((string)row["source_id"]))throw new ArgumentException("Invalid relocation source reference.");
                var card=(string)p["card_id"];var effect=(string)p["effect_id"];var from=p["source_policy"];var to=p["destination_policy"];
                if(card!="card.de_stalinization" || effect!="effect.de_stalinization" || !knownEffectIds.Contains(effect)
                    || (string)p["template"]!="RelocateOwnInfluence" || (string)p["beneficiary"]!="USSR" || (string)p["selection_owner"]!="USSR"
                    || (string)from["query"]!="CountriesWithOwnInfluence" || (string)from["side"]!="USSR" || (bool)from["allow_any_region"]!=true
                    || (string)to["query"]!="CountriesNotControlledBySide" || (string)to["excluded_controller"]!="US"
                    || (bool)to["uses_ops_adjacency"] || (bool)to["uses_enemy_control_cost"]
                    || (string)p["total_policy"]!="DestinationSumEqualsSourceSum" || (string)p["overlap_policy"]!="RequiresRuling"
                    || (string)p["commit_policy"]!="AtomicCompleteAllocation" || (int)p["minimum_total"]!=0
                    || p["maximum_total"].Type!=JTokenType.Integer || to["max_added_per_country"].Type!=JTokenType.Integer)
                    throw new ArgumentException("Unsupported relocation semantics.");
                return new RelocateOwnInfluenceEffect(card,effect,PlayerSide.USSR,(int)p["maximum_total"],(int)to["max_added_per_country"]);
            }
            catch(OverflowException error){throw new ArgumentException("Relocation parameter out of range.",error);}
        }
    }
}
