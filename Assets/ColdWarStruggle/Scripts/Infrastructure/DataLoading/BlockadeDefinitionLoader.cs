using System;
using System.Collections.Generic;
using System.Linq;
using ColdWarStruggle.Core;
using Newtonsoft.Json.Linq;
namespace ColdWarStruggle.Infrastructure
{
    public static class BlockadeDefinitionLoader
    {
        public static BlockadeDefinition Load(string json,string schema,MapDefinition map,CardCatalog cards,OpsGeneralDefinition ops,IReadOnlyCollection<string> knownEffectIds)
        {
            if(map==null || cards==null || ops==null || knownEffectIds==null)throw new ArgumentNullException();
            if(knownEffectIds.Count==0 || knownEffectIds.Any(string.IsNullOrWhiteSpace))throw new ArgumentException("Known effect references required.");
            try
            {
                var root=ContractJson.Parse(json);var contract=ContractJson.Parse(schema) as JObject;
                if(contract==null || (string)contract["$id"]!="urn:coldwarheat:contract:hand-events-batch1:1")throw new ArgumentException("Unsupported hand-event contract.");
                ContractJson.Validate(root,contract);
                if((int)root["schema_version"]!=1 || (string)root["dataset_id"]!="effects.deluxe2015.hand_events_batch1" || (string)root["content_version"]!="0.1.0"
                    || (string)root["ruleset_id"]!="ruleset.deluxe2015" || (string)root["scope"]!="release-candidate")throw new ArgumentException("Unsupported hand-event profile.");
                var expected=new Dictionary<string,string>(StringComparer.Ordinal)
                {
                    {"C-DELUXE-EN","https://www.gmtgames.com/nnts/TS_Cards_Deluxe.pdf"},
                    {"R2015","https://www.gmtgames.com/nnts/TS_Rules-2015.pdf"},
                    {"F2010","https://www.gmtgames.com/nnts/FAQv5.pdf"},
                    {"PROJECT-HC1",null}
                };
                var sources=new HashSet<string>(StringComparer.Ordinal);
                foreach(var source in root["source_catalog"])
                {
                    var id=(string)source["id"];
                    if(!sources.Add(id) || !expected.TryGetValue(id,out var url) || (string)source["url"]!=url)throw new ArgumentException("Unknown, duplicate or mismatched hand-event source.");
                }
                if(sources.Count!=expected.Count)throw new ArgumentException("Incomplete hand-event sources.");
                // Validate the enclosing document, but assemble only this implemented effect.
                var p=root["parameters"]["blockade"];var effect=(string)p["effect_id"];
                if((string)p["card_id"]!="card.blockade" || effect!="effect.blockade" || !knownEffectIds.Contains(effect)
                    || (string)p["template"]!="DiscardOrRemoveAllInfluence" || (string)p["decision_owner"]!="US" || (string)p["hand_owner"]!="US"
                    || (string)p["ops_purpose"]!="OpsThreshold" || (string)p["ops_policy_ref"]["dataset_id"]!="effects.deluxe2015.persistent_batch1"
                    || (string)p["ops_policy_ref"]["json_pointer"]!="/ops_policy" || (string)p["source_zone"]!="Hand" || (string)p["allowed_kind"]!="Event"
                    || !(bool)p["allow_decline"] || (string)p["discard_destination"]!="DiscardPile" || (string)p["discard_event_policy"]!="DoNotTrigger"
                    || (string)p["fallback_influence"]["side"]!="US" || (string)p["fallback_influence"]["operation"]!="RemoveAll"
                    || (string)p["candidate_visibility"]!="DecisionOwnerOnly")throw new ArgumentException("Unsupported blockade semantics.");
                var refs=new HashSet<string>(StringComparer.Ordinal);
                foreach(var reference in p["source_refs"])
                {
                    var id=(string)reference["source_id"];
                    if(!sources.Contains(id) || !refs.Add(id) || (string)reference["verification"]!="Verified")throw new ArgumentException("Invalid blockade evidence.");
                }
                if(refs.Count!=sources.Count)throw new ArgumentException("Incomplete blockade evidence.");
                return new BlockadeDefinition(map,cards,ops,new CardId((string)p["card_id"]),effect,PlayerSide.US,(int)p["minimum_effective_ops"],new CountryId((string)p["fallback_influence"]["country_id"]));
            }
            catch(OverflowException e){throw new ArgumentException("Blockade parameter out of range.",e);}
        }
    }
}
