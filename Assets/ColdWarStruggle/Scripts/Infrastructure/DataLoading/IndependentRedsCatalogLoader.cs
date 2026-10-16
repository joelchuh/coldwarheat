using System;
using System.Collections.Generic;
using System.Linq;
using ColdWarStruggle.Core;
using Newtonsoft.Json.Linq;
namespace ColdWarStruggle.Infrastructure
{
    public static class IndependentRedsCatalogLoader
    {
        public static MatchOpponentInfluenceEffect Load(string json,string schema,MapDefinition map,IReadOnlyCollection<string> knownEffectIds)
        {
            if(map==null || knownEffectIds==null)throw new ArgumentNullException();
            if(knownEffectIds.Count==0 || knownEffectIds.Any(string.IsNullOrWhiteSpace))throw new ArgumentException("Known effect references are required.");
            try
            {
                var root=ContractJson.Parse(json);var contract=ContractJson.Parse(schema) as JObject;
                if(contract==null || (string)contract["$id"]!="urn:coldwarheat:contract:early-influence-batch3:1")throw new ArgumentException("Unsupported matching contract.");
                ContractJson.Validate(root,contract);
                if((int)root["schema_version"]!=1 || (string)root["dataset_id"]!="effects.deluxe2015.early_influence_batch3"
                    || (string)root["content_version"]!="0.1.0" || (string)root["ruleset_id"]!="ruleset.deluxe2015" || (string)root["scope"]!="release-candidate")
                    throw new ArgumentException("Unsupported matching profile.");
                var sources=new HashSet<string>(StringComparer.Ordinal);
                foreach(var source in root["source_catalog"])
                    if(!sources.Add((string)source["id"]))throw new ArgumentException("Duplicate source.");
                // This reader deliberately binds only the implemented matching effect in the shared document.
                var p=root["parameters"]["independent_reds"];var card=(string)p["card_id"];var effect=(string)p["effect_id"];
                if(card!="card.independent_reds" || effect!="effect.independent_reds" || !knownEffectIds.Contains(effect))throw new ArgumentException("Invalid matching identity or reference.");
                if((string)p["template"]!="MatchOpponentByAdding" || (string)p["beneficiary"]!="US" || (int)p["target_count"]!=1
                    || (string)p["selection_policy"]!="AnyListedCountry")throw new ArgumentException("Unsupported matching semantics.");
                foreach(var source in p["source_refs"])
                    if((string)source["verification"]!="Verified" || !sources.Contains((string)source["source_id"]))throw new ArgumentException("Unverified matching source.");
                var countries=p["target_country_ids"].Values<string>().Select(id=>new CountryId(id)).ToArray();
                if(countries.Any(c=>!map.Contains(c)))throw new ArgumentException("Unknown matching country.");
                return new MatchOpponentInfluenceEffect(card,effect,PlayerSide.US,countries);
            }
            catch(OverflowException error) { throw new ArgumentException("Matching parameter out of range.",error); }
        }
    }
}
