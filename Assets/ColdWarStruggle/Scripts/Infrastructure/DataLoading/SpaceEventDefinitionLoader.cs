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
                if(contract==null || Text(contract["$id"])!="urn:coldwarheat:contract:space-events-batch1:1")throw new ArgumentException("Unsupported space event contract.");
                ContractJson.Validate(root,contract);
                if(Integer(root["schema_version"])!=1 || Text(root["dataset_id"])!="effects.deluxe2015.space_events_batch1" || Text(root["content_version"])!="0.1.0" || Text(root["ruleset_id"])!="ruleset.deluxe2015" || Text(root["scope"])!="release-candidate")throw new ArgumentException("Unsupported space event profile.");
                var urls=new Dictionary<string,string>(StringComparer.Ordinal){{"C-DELUXE-EN","https://www.gmtgames.com/nnts/TS_Cards_Deluxe.pdf"},{"R2015","https://www.gmtgames.com/nnts/TS_Rules-2015.pdf"},{"F2010","https://www.gmtgames.com/nnts/FAQv5.pdf"},{"PROJECT-SS1",null}};
                var editions=new Dictionary<string,string>(StringComparer.Ordinal){{"C-DELUXE-EN","English Deluxe card images"},{"R2015","Deluxe rules 2015"},{"F2010","FAQ v5 2010, compatible Deluxe clarification only"},{"PROJECT-SS1","Engineering contracts and unresolved edges, 2026-10-06"}};
                var sources=new HashSet<string>(StringComparer.Ordinal);
                foreach(var s in root["source_catalog"])
                    if(!sources.Add(Text(s["id"])) || !urls.TryGetValue(Text(s["id"]),out var url) || (s["url"]?.Type==JTokenType.Null?null:Text(s["url"]))!=url || Text(s["edition"])!=editions[Text(s["id"])])throw new ArgumentException("Unknown or mismatched space event source.");
                if(sources.Count!=urls.Count)throw new ArgumentException("Incomplete space event sources.");
                var p=root["parameters"]["captured_nazi_scientist"];
                if(Text(p["card_id"])!="card.captured_nazi_scientist" || Text(p["effect_id"])!="effect.captured_nazi_scientist" || !knownEffectIds.Contains(Text(p["effect_id"]))
                    || Text(p["template"])!="EventSpaceAdvance" || Text(p["beneficiary"])!="EffectController" || Integer(p["advance_steps"])!=1 || Integer(p["ordinary_attempt_consumption"])!=0
                    || Boolean(p["requires_ordinary_ops_qualification"]) || Text(p["random_roll"])!="None" || Text(p["track_dataset_id"])!="rules.deluxe2015.space-race"
                    || Text(p["reward_policy"])!="NormalReachedSpaceFirstOrSecond" || Text(p["ability_policy"])!="QueryAuthoritativeTrackPositions" || Text(p["terminal_policy"])!="RequiresRuling")throw new ArgumentException("Unsupported space event policy.");
                var refs=new HashSet<string>(StringComparer.Ordinal);
                foreach(var r in p["source_refs"])
                    if(!sources.Contains(Text(r["source_id"])) || !refs.Add(Text(r["source_id"])) || Text(r["verification"])!="Verified")throw new ArgumentException("Invalid space event evidence.");
                if(refs.Count!=sources.Count)throw new ArgumentException("Incomplete space event evidence.");
                return new SpaceEventDefinition(cards,track,new CardId(Text(p["card_id"])),Text(p["effect_id"]),Integer(p["advance_steps"]));
            }
            catch(OverflowException e){throw new ArgumentException("Space event parameter out of range.",e);}
        }
        static string Text(JToken value)
        {
            if(value?.Type!=JTokenType.String)throw new ArgumentException("Space event text must be a JSON string.");
            return (string)value;
        }
        static int Integer(JToken value)
        {
            if(value?.Type!=JTokenType.Integer)throw new ArgumentException("Space event integer must be a JSON integer.");
            return (int)value;
        }
        static bool Boolean(JToken value)
        {
            if(value?.Type!=JTokenType.Boolean)throw new ArgumentException("Space event flag must be a JSON boolean.");
            return (bool)value;
        }
    }
}
