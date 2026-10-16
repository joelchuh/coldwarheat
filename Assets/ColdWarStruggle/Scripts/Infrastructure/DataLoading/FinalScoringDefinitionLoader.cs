using System;
using System.Collections.Generic;
using System.Linq;
using ColdWarStruggle.Core;
using Newtonsoft.Json.Linq;
namespace ColdWarStruggle.Infrastructure
{
    public static class FinalScoringDefinitionLoader
    {
        public static FinalScoringDefinition LoadBound(string regionJson,string regionSchema,string chinaJson,string chinaSchema,RegionScoringDefinition shared,CardCatalog cards)
        {
            if(shared==null)throw new ArgumentNullException(nameof(shared));
            var loaded=LoadSubset(regionJson,regionSchema,chinaJson,chinaSchema,shared.Map,cards);var d=loaded.RegionDefinition;
            if(d.BattlegroundBonus!=shared.BattlegroundBonus || d.EnemyAdjacencyBonus!=shared.EnemyAdjacencyBonus
                || d.WeightedRegionId!=shared.WeightedRegionId || d.WeightedParentId!=shared.WeightedParentId
                || !d.Regions.Select(x=>(x.Id,x.Presence,x.Domination,x.Control)).SequenceEqual(shared.Regions.Select(x=>(x.Id,x.Presence,x.Domination,x.Control)))
                || d.CountryWeights.Count!=shared.CountryWeights.Count || d.CountryWeights.Any(x=>!shared.CountryWeights.TryGetValue(x.Key,out var weight) || weight!=x.Value))
                throw new ArgumentException("Final and ordinary scoring must use equivalent shared parameters.");
            return new FinalScoringDefinition(shared,loaded.RegionIds,loaded.ChinaCardId,loaded.ChinaHolderVp);
        }
        // Schemas are trusted repository contracts. Source references are checked, not authenticated.
        public static FinalScoringDefinition LoadSubset(string regionJson,string regionSchema,string chinaJson,string chinaSchema,MapDefinition map,CardCatalog cards)
        {
            if(map==null || cards==null)throw new ArgumentNullException();
            var root=ContractJson.Parse(chinaJson) as JObject;
            var contract=ContractJson.Parse(chinaSchema) as JObject;
            if(root==null || contract==null || (string)contract["$id"]!="urn:coldwarheat:contract:scoring-card-parameters:2")
                throw new ArgumentException("Unsupported final China reward contract.");
            ContractJson.Validate(root,contract);
            var expectedSources=new Dictionary<string,string>(StringComparer.Ordinal) {
                {"R2015","https://www.gmtgames.com/nnts/TS_Rules-2015.pdf"},
                {"C-DELUXE-EN","https://www.gmtgames.com/nnts/TS_Cards_Deluxe.pdf"} };
            var sources=root["sources"] as JArray;
            var seen=new HashSet<string>(StringComparer.Ordinal);
            if(sources==null || sources.Count!=expectedSources.Count)throw new ArgumentException("Incomplete final scoring source profile.");
            foreach(var source in sources)
            {
                if(!(source is JObject obj) || obj["id"]?.Type!=JTokenType.String || obj["url"]?.Type!=JTokenType.String
                    || !expectedSources.TryGetValue((string)obj["id"],out var url) || url!=(string)obj["url"] || !seen.Add((string)obj["id"]))
                    throw new ArgumentException("Unknown, duplicate or mismatched final scoring source.");
            }
            var card=root["cards"][0];
            var chinaId=new CardId((string)card["id"]);
            if(!cards.Contains(chinaId) || cards.GetCard(chinaId).Kind!=CardKind.China)
                throw new ArgumentException("Final reward must reference the catalog's separate China card.");
            var reward=card["parameters"]["final_holder_vp"];
            if(reward.Type!=JTokenType.Integer)throw new ArgumentException("Expected integer final reward.");
            int chinaVp;
            try { chinaVp=checked((int)reward); }
            catch(OverflowException e) { throw new ArgumentException("Final reward exceeds Int32.",e); }

            var regionDefinition=RegionScoringDefinitionLoader.LoadSubset(regionJson,regionSchema,map);
            var regionRoot=(JObject)ContractJson.Parse(regionJson);
            var ids=regionRoot["regions"].Where(x=>(bool)x["include_in_final"]).Select(x=>(string)x["id"]);
            return new FinalScoringDefinition(regionDefinition,ids,chinaId,chinaVp);
        }
    }
}
