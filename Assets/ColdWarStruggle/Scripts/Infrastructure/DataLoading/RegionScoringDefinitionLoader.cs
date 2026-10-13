using System;
using System.Collections.Generic;
using System.Linq;
using ColdWarStruggle.Core;
using Newtonsoft.Json.Linq;
namespace ColdWarStruggle.Infrastructure
{
    public static class RegionScoringDefinitionLoader
    {
        // Caller supplies the trusted repository contract; source references are checked, not authenticated.
        public static RegionScoringDefinition LoadSubset(string json,string schema,MapDefinition map)
        {
            if(map==null)throw new ArgumentNullException(nameof(map));
            var root=ContractJson.Parse(json) as JObject;var contract=ContractJson.Parse(schema) as JObject;
            if(root==null || contract==null || (string)contract["$id"]!="urn:coldwarheat:contract:region-scoring-rules:1")throw new ArgumentException("Unsupported scoring contract.");
            ContractJson.Validate(root,contract);
            var expectedSources=new Dictionary<string,string>(StringComparer.Ordinal) {
                {"R2015","https://www.gmtgames.com/nnts/TS_Rules-2015.pdf"},
                {"M-DELUXE","https://s3-us-west-2.amazonaws.com/gmtwebsiteassets/nnts/TSMap11.jpg"},
                {"C-DELUXE-EN","https://www.gmtgames.com/nnts/TS_Cards_Deluxe.pdf"} };
            var sources=root["sources"] as JArray;var seen=new HashSet<string>(StringComparer.Ordinal);
            if(sources==null || sources.Count!=expectedSources.Count)throw new ArgumentException("Incomplete scoring source profile.");
            foreach(var source in sources)
            {
                if(!(source is JObject obj) || obj["id"]?.Type!=JTokenType.String || obj["url"]?.Type!=JTokenType.String
                    || !expectedSources.TryGetValue((string)obj["id"],out var url) || url!=(string)obj["url"] || !seen.Add((string)obj["id"]))
                    throw new ArgumentException("Unknown, duplicate or mismatched scoring source.");
            }
            var tables=root["regions"].Select(t=>new RegionScoreTable((string)t["id"],Integer(t["base_vp"]["presence"]),Integer(t["base_vp"]["domination"]),
                t["base_vp"]["control"].Type==JTokenType.Null?(int?)null:Integer(t["base_vp"]["control"]))).ToArray();
            var weighted=root["southeast_asia"];
            return new RegionScoringDefinition(map,Integer(root["bonus_vp"]["controlled_battleground"]),Integer(root["bonus_vp"]["controlled_country_adjacent_to_enemy_superpower"]),
                tables,(string)weighted["id"],(string)weighted["parent_region_id"],weighted["country_vp"].Select(w=>new KeyValuePair<CountryId,int>(new CountryId((string)w["country_id"]),Integer(w["vp"]))));
        }
        static int Integer(JToken token)
        {
            if(token==null || token.Type!=JTokenType.Integer)throw new ArgumentException("Expected integer score.");
            try { return checked((int)token); }
            catch(OverflowException e) { throw new ArgumentException("Score exceeds Int32.",e); }
        }
    }
}
