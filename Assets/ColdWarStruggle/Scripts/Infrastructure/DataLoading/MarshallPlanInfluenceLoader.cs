using System;
using System.Collections.Generic;
using ColdWarStruggle.Core;
using Newtonsoft.Json.Linq;
namespace ColdWarStruggle.Infrastructure
{
    // Influence foundation only. Do not register as a full card event without atomic successful-event facts.
    public static class MarshallPlanInfluenceLoader
    {
        public static AddToDistinctCountriesEffect Load(string json,string schema,MapDefinition map,CardCatalog cards)
        {
            if(map==null || cards==null)throw new ArgumentNullException();
            try
            {
                var root=ContractJson.Parse(json);var contract=ContractJson.Parse(schema) as JObject;
                if(contract==null || (string)contract["$id"]!="urn:coldwarheat:contract:alliance-prerequisites:1")throw new ArgumentException("Unsupported Marshall contract.");
                ContractJson.Validate(root,contract);
                if((int)root["schema_version"]!=1 || (string)root["dataset_id"]!="effects.deluxe2015.alliance_prerequisites"
                    || (string)root["content_version"]!="0.1.0" || (string)root["ruleset_id"]!="ruleset.deluxe2015" || (string)root["scope"]!="release-candidate")
                    throw new ArgumentException("Unsupported Marshall profile.");
                var expectedSources=new Dictionary<string,string>(StringComparer.Ordinal)
                {
                    {"C-DELUXE-EN","https://www.gmtgames.com/nnts/TS_Cards_Deluxe.pdf"},
                    {"R2015","https://www.gmtgames.com/nnts/TS_Rules-2015.pdf"},{"PROJECT-AP1",null}
                };
                var seen=new HashSet<string>(StringComparer.Ordinal);
                foreach(var source in root["source_catalog"])
                {
                    var id=(string)source["id"];
                    if(!seen.Add(id) || !expectedSources.TryGetValue(id,out var url) || (string)source["url"]!=url
                        || string.IsNullOrWhiteSpace((string)source["edition"]) || string.IsNullOrWhiteSpace((string)source["description"]))
                        throw new ArgumentException("Invalid Marshall source.");
                }
                if(seen.Count!=expectedSources.Count)throw new ArgumentException("Incomplete Marshall sources.");
                var completion=root["completion_policy"];
                if((string)completion["template"]!="RecordCommittedEventFact"
                    || (string)completion["fact_policy_ref"]["dataset_id"]!="effects.deluxe2015.treaty_protection"
                    || (string)completion["fact_policy_ref"]["json_pointer"]!="/event_fact_policy")throw new ArgumentException("Unknown Marshall completion policy.");
                var p=root["parameters"]["marshall_plan"];
                if((string)p["card_id"]!="card.marshall_plan" || (string)p["effect_id"]!="effect.marshall_plan"
                    || (string)p["template"]!="AddToDistinctNonOpponentControlledCountries" || (string)p["beneficiary"]!="US"
                    || (string)p["target_region_id"]!="region.western_europe" || (string)p["target_control"]!="NotControlledByOpponent"
                    || (string)p["shortage_policy"]!="SelectAllAvailable" || (string)p["zero_candidates_outcome"]!="ResolvedNoChange"
                    || map.GetCountriesInRegion((string)p["target_region_id"]).Count==0)throw new ArgumentException("Unsupported Marshall influence semantics.");
                seen.Clear();
                foreach(var reference in p["source_refs"])
                {
                    var id=(string)reference["source_id"];
                    if(!expectedSources.ContainsKey(id) || !seen.Add(id) || (string)reference["verification"]!="Verified"
                        || string.IsNullOrWhiteSpace((string)reference["locator"]))throw new ArgumentException("Invalid Marshall source reference.");
                }
                if(seen.Count!=expectedSources.Count)throw new ArgumentException("Incomplete Marshall source references.");
                var printed=cards.GetCard(new CardId((string)p["card_id"]));var binding=printed.Effect;
                if(printed.Kind!=CardKind.Event || printed.Affiliation!=CardAffiliation.US || printed.Era!=GameEra.Early
                    || !printed.RemoveAfterResolvedEvent || !printed.DisplayPersistentReminder || binding==null
                    || binding.EffectId!=(string)p["effect_id"] || binding.ContractVersion!=(string)root["content_version"]
                    || binding.Kind!=CardParameterBindingKind.Bound || binding.Parameters.Count!=1
                    || binding.Parameters[0].DatasetId!=(string)root["dataset_id"] || binding.Parameters[0].JsonPointer!="/parameters/marshall_plan")
                    throw new ArgumentException("Invalid Marshall printed binding.");
                return new AddToDistinctCountriesEffect((string)p["card_id"],(string)p["effect_id"],PlayerSide.US,new[]{(string)p["target_region_id"]},
                    (int)p["target_count"],(int)p["amount_per_country"],InfluenceControlFilter.NotOpponentControlled,InfluenceCandidateShortagePolicy.SelectAllAvailable);
            }
            catch(OverflowException error){throw new ArgumentException("Marshall parameter out of range.",error);}
        }
    }
}
