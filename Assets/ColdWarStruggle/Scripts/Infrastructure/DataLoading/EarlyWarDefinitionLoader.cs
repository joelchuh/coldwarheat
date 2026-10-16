using System;
using System.Collections.Generic;
using System.Linq;
using ColdWarStruggle.Core;
using Newtonsoft.Json.Linq;
namespace ColdWarStruggle.Infrastructure
{
    public static class EarlyWarDefinitionLoader
    {
        public static WarEventCatalog Load(string json,string schema,MapDefinition map,CardCatalog cards,IReadOnlyCollection<string> knownEffectIds)
        {
            if(map==null || cards==null || knownEffectIds==null)throw new ArgumentNullException();
            if(knownEffectIds.Count==0 || knownEffectIds.Any(string.IsNullOrWhiteSpace))throw new ArgumentException("Known effect references required.");
            try
            {
                var root=ContractJson.Parse(json);var contract=ContractJson.Parse(schema) as JObject;
                if(contract==null || (string)contract["$id"]!="urn:coldwarheat:contract:war-events-batch1:1")throw new ArgumentException("Unsupported early-war contract.");
                ContractJson.Validate(root,contract);
                if((int)root["schema_version"]!=1 || (string)root["dataset_id"]!="effects.deluxe2015.war_events_batch1" || (string)root["content_version"]!="0.1.0" || (string)root["ruleset_id"]!="ruleset.deluxe2015" || (string)root["scope"]!="release-candidate")throw new ArgumentException("Unsupported early-war profile.");
                var expected=new Dictionary<string,string>(StringComparer.Ordinal){{"C-DELUXE-EN","https://www.gmtgames.com/nnts/TS_Cards_Deluxe.pdf"},{"R2015","https://www.gmtgames.com/nnts/TS_Rules-2015.pdf"},{"F2010","https://www.gmtgames.com/nnts/FAQv5.pdf"},{"PROJECT-WAR1",null}};
                var editions=new Dictionary<string,string>(StringComparer.Ordinal){{"C-DELUXE-EN","English Deluxe card images"},{"R2015","Deluxe rules 2015"},{"F2010","FAQ v5 2010, compatible Deluxe clarifications only"},{"PROJECT-WAR1","WAR1-ENGINEERING-01, 2026-10-05"}};
                var sources=new HashSet<string>(StringComparer.Ordinal);
                foreach(var s in root["source_catalog"])
                    if(!sources.Add((string)s["id"]) || !expected.TryGetValue((string)s["id"],out var url) || (string)s["url"]!=url || (string)s["edition"]!=editions[(string)s["id"]])throw new ArgumentException("Unknown, duplicate or mismatched war source.");
                if(sources.Count!=expected.Count)throw new ArgumentException("Incomplete war sources.");
                var p=root["war_policy"];
                if((int)p["die_sides"]!=6 || (int)p["minimum_winning_result"]!=4 || (int)p["opponent_controlled_neighbor_penalty"]!=1 || !(bool)p["include_direct_superpower_neighbors"]
                    || (string)p["military_credit"]["kind"]!="WarEvent" || (int)p["military_credit"]["amount"]!=2 || (string)p["military_credit"]["on"]!="EveryAllowedResolvedAttempt"
                    || (int)p["victory_vp"]!=2 || (string)p["capture"]!="AllOpponentInfluenceToBeneficiaryPreserveExisting" || (string)p["defcon_policy"]!="NoChangeNoGeographicGate"
                    || (bool)p["ops_modifiers_apply"] || (string)p["result_checkpoint"]!="VpBatchClosedAfterWholeWarResult")throw new ArgumentException("Unsupported war policy semantics.");
                var policy=new WarPolicy((int)p["die_sides"],(int)p["minimum_winning_result"],(int)p["opponent_controlled_neighbor_penalty"],(int)p["military_credit"]["amount"],(int)p["victory_vp"]);
                var effects=new List<WarEventDefinition>();var branches=new[]{"korean_war","arab_israeli_war","indo_pakistani_war"};
                for(int i=0;i<branches.Length;i++)
                {
                    var name=branches[i];var e=root["parameters"][name];var neutral=i==2;var arab=i==1;
                    if((string)e["card_id"]!="card."+name || (string)e["effect_id"]!="effect."+name || !knownEffectIds.Contains((string)e["effect_id"])
                        || (string)e["template"]!="SingleRollInfluenceCaptureWar" || (string)e["beneficiary"]!=(neutral?"EventController":"USSR")
                        || (string)e["target_policy"]["mode"]!=(neutral?"ChooseOne":"Fixed") || (bool)e["target_policy"]["requires_opponent_influence"]
                        || (bool)e["extra_target_control_penalty"]!=arab || (string)e["war_policy_ref"]["dataset_id"]!="effects.deluxe2015.war_events_batch1" || (string)e["war_policy_ref"]["json_pointer"]!="/war_policy")throw new ArgumentException("Unsupported war branch semantics.");
                    var gate=e["prevention_gate"];
                    if(arab)
                    {
                        if((string)gate["source_card_id"]!="card.camp_david_accords" || (string)gate["event_fact_policy_ref"]["dataset_id"]!="effects.deluxe2015.treaty_protection" || (string)gate["event_fact_policy_ref"]["json_pointer"]!="/event_fact_policy" || (string)gate["on_qualifying_fact"]!="BlockedPrerequisite" || (string)gate["scope"]!="EventOnly")throw new ArgumentException("Unsupported war prevention semantics.");
                    }
                    else if(gate.Type!=JTokenType.Null)throw new ArgumentException("Unexpected war prevention.");
                    var refs=new HashSet<string>(StringComparer.Ordinal);
                    foreach(var source in e["source_refs"])
                        if(!sources.Contains((string)source["source_id"]) || !refs.Add((string)source["source_id"]) || (string)source["verification"]!="Verified")throw new ArgumentException("Invalid war evidence.");
                    if(refs.Count!=sources.Count)throw new ArgumentException("Incomplete war evidence.");
                    effects.Add(new WarEventDefinition(new CardId((string)e["card_id"]),(string)e["effect_id"],(WarEventKind)i,e["target_policy"]["country_ids"].Select(t=>new CountryId((string)t))));
                }
                return new WarEventCatalog(map,cards,policy,effects);
            }
            catch(OverflowException e){throw new ArgumentException("War parameter out of range.",e);}
        }
    }
}
