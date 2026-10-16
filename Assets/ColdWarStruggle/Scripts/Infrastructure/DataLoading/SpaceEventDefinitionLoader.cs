using System;
using System.Collections.Generic;
using System.Linq;
using ColdWarStruggle.Core;
using Newtonsoft.Json.Linq;
namespace ColdWarStruggle.Infrastructure
{
    public static class SpaceEventDefinitionLoader
    {
        public static SpaceEventDefinition Load(string json,string schema,CardCatalog cards,SpaceRaceDefinition track,IReadOnlyCollection<string> knownEffectIds)
        {
            if(cards==null || track==null || knownEffectIds==null)throw new ArgumentNullException();
            if(knownEffectIds.Count==0 || knownEffectIds.Any(string.IsNullOrWhiteSpace))throw new ArgumentException("Known space effect reference required.");
            try
            {
                var root=ContractJson.Parse(json);var contract=ContractJson.Parse(schema) as JObject;
                if(contract==null || (string)contract["$id"]!="urn:coldwarheat:contract:space-events-batch1:1")throw new ArgumentException("Unsupported space event contract.");
                ContractJson.Validate(root,contract);
                if((int)root["schema_version"]!=1 || (string)root["dataset_id"]!="effects.deluxe2015.space_events_batch1" || (string)root["content_version"]!="0.1.0" || (string)root["ruleset_id"]!="ruleset.deluxe2015" || (string)root["scope"]!="release-candidate")throw new ArgumentException("Unsupported space event profile.");
                var urls=new Dictionary<string,string>(StringComparer.Ordinal){{"C-DELUXE-EN","https://www.gmtgames.com/nnts/TS_Cards_Deluxe.pdf"},{"R2015","https://www.gmtgames.com/nnts/TS_Rules-2015.pdf"},{"F2010","https://www.gmtgames.com/nnts/FAQv5.pdf"},{"PROJECT-SS1",null}};
                var editions=new Dictionary<string,string>(StringComparer.Ordinal){{"C-DELUXE-EN","English Deluxe card images"},{"R2015","Deluxe rules 2015"},{"F2010","FAQ v5 2010, compatible Deluxe clarification only"},{"PROJECT-SS1","Engineering contracts and unresolved edges, 2026-10-06"}};
                var sources=new HashSet<string>(StringComparer.Ordinal);
                foreach(var s in root["source_catalog"])
                    if(!sources.Add((string)s["id"]) || !urls.TryGetValue((string)s["id"],out var url) || (string)s["url"]!=url || (string)s["edition"]!=editions[(string)s["id"]])throw new ArgumentException("Unknown or mismatched space event source.");
                if(sources.Count!=urls.Count)throw new ArgumentException("Incomplete space event sources.");
                var p=root["parameters"]["captured_nazi_scientist"];
                if((string)p["card_id"]!="card.captured_nazi_scientist" || (string)p["effect_id"]!="effect.captured_nazi_scientist" || !knownEffectIds.Contains((string)p["effect_id"])
                    || (string)p["template"]!="EventSpaceAdvance" || (string)p["beneficiary"]!="EffectController" || (int)p["advance_steps"]!=1 || (int)p["ordinary_attempt_consumption"]!=0
                    || (bool)p["requires_ordinary_ops_qualification"] || (string)p["random_roll"]!="None" || (string)p["track_dataset_id"]!="rules.deluxe2015.space-race"
                    || (string)p["reward_policy"]!="NormalReachedSpaceFirstOrSecond" || (string)p["ability_policy"]!="QueryAuthoritativeTrackPositions" || (string)p["terminal_policy"]!="RequiresRuling")throw new ArgumentException("Unsupported space event policy.");
                var refs=new HashSet<string>(StringComparer.Ordinal);
                foreach(var r in p["source_refs"])
                    if(!sources.Contains((string)r["source_id"]) || !refs.Add((string)r["source_id"]) || (string)r["verification"]!="Verified")throw new ArgumentException("Invalid space event evidence.");
                if(refs.Count!=sources.Count)throw new ArgumentException("Incomplete space event evidence.");
                return new SpaceEventDefinition(cards,track,new CardId((string)p["card_id"]),(string)p["effect_id"],(int)p["advance_steps"]);
            }
            catch(OverflowException e){throw new ArgumentException("Space event parameter out of range.",e);}
        }
    }
}
