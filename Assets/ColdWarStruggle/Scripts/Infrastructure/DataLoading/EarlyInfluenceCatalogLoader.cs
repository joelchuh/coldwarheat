using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ColdWarStruggle.Core;
using Newtonsoft.Json.Linq;

namespace ColdWarStruggle.Infrastructure
{
    public static class EarlyInfluenceCatalogLoader
    {
        public static IReadOnlyDictionary<string, InfluenceEffectDefinition> Load(string json, string schema, MapDefinition map)
        {
            if(map==null)throw new ArgumentNullException(nameof(map));
            try { return LoadChecked(json,schema,map); }
            catch(OverflowException error) { throw new ArgumentException("Effect parameter out of range.",error); }
        }
        static IReadOnlyDictionary<string, InfluenceEffectDefinition> LoadChecked(string json,string schema,MapDefinition map)
        {
            var root=ContractJson.Parse(json);var contract=ContractJson.Parse(schema) as JObject;
            if(contract==null || (string)contract["$id"]!="urn:coldwarheat:contract:early-influence-parameters:1")throw new ArgumentException("Unsupported effect contract.");
            ContractJson.Validate(root,contract);
            if((int)root["schema_version"]!=1 || (string)root["dataset_id"]!="effects.deluxe2015.early_influence_batch1"
                || (string)root["content_version"]!="0.1.0" || (string)root["ruleset_id"]!="ruleset.deluxe2015" || (string)root["scope"]!="release-candidate")
                throw new ArgumentException("Unsupported effect profile.");
            var sourceIds=new HashSet<string>(StringComparer.Ordinal);
            foreach(var source in root["source_catalog"])
                if(!sourceIds.Add((string)source["id"]))throw new ArgumentException("Duplicate source.");
            var definitions=new Dictionary<string,InfluenceEffectDefinition>(StringComparer.Ordinal);
            var effects=new HashSet<string>(StringComparer.Ordinal);
            foreach(var property in ((JObject)root["parameters"]).Properties())
            {
                var p=property.Value;var card=(string)p["card_id"];var effect=(string)p["effect_id"];
                if(card!="card."+property.Name || effect!="effect."+property.Name || !effects.Add(effect))throw new ArgumentException("Effect identity mismatch.");
                foreach(var source in p["source_refs"])
                    if((string)source["verification"]!="Verified" || !sourceIds.Contains((string)source["source_id"]))throw new ArgumentException("Unverified effect source.");
                var beneficiary=(string)p["beneficiary"]=="US"?PlayerSide.US:PlayerSide.USSR;
                InfluenceEffectDefinition definition;
                switch((string)p["template"])
                {
                    case "ClearOpponentThenEnsureControl":
                        definition=new ClearThenControlEffect(card,effect,beneficiary,Country(p,map));break;
                    case "AddThenRemoveFraction":
                        if((string)p["removal_rounding"]!="Ceiling")throw new ArgumentException("Unsupported rounding.");
                        definition=new AddThenRemoveFractionEffect(card,effect,beneficiary,Country(p,map),(int)p["add_amount"],(int)p["removal_divisor"]);break;
                    case "RemoveOpponentFromUncontrolledCountry":
                        if((string)p["target_control"]!="NeitherSide" || (int)p["target_count"]!=1)throw new ArgumentException("Unsupported uncontrolled selection.");
                        var region=(string)p["target_region_id"];Region(region,map);
                        definition=new RemoveFromUncontrolledEffect(card,effect,beneficiary,region);break;
                    case "AddToDistinctCountries":
                        var regions=p["target_region_ids"].Values<string>().ToArray();foreach(var id in regions)Region(id,map);
                        definition=new AddToDistinctCountriesEffect(card,effect,beneficiary,regions,(int)p["target_count"],(int)p["amount_per_country"]);break;
                    default:throw new ArgumentException("Unknown influence effect template.");
                }
                definitions.Add(card,definition);
            }
            return new ReadOnlyDictionary<string,InfluenceEffectDefinition>(definitions);
        }
        static CountryId Country(JToken parameters,MapDefinition map)
        {
            var id=new CountryId((string)parameters["target_country_id"]);
            if(!map.Contains(id))throw new ArgumentException("Unknown effect country.");
            return id;
        }
        static void Region(string region,MapDefinition map)
        { if(map.GetCountriesInRegion(region).Count==0)throw new ArgumentException("Unknown effect region."); }
    }
}
