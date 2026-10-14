using System;
using ColdWarStruggle.Core;
using Newtonsoft.Json.Linq;
namespace ColdWarStruggle.Infrastructure
{
    public static class ChinaCardDefinitionLoader
    {
        public static ChinaCardDefinition LoadSubset(string json,string schema,CardCatalog cards,MapDefinition map)
        {
            // Reuse the enclosing profile, policy and source verification; no other event is activated.
            OpsGeneralDefinitionLoader.LoadSubset(json,schema);
            var root=ContractJson.Parse(json);var source=root["source_bonuses"]?["china"];
            if(source is not JObject || (string)source["card_id"]!="card.china" || (string)source["region_id"]!="region.asia"
                || string.IsNullOrWhiteSpace((string)source["description"]) || source["source_refs"] is not JArray refs || refs.Count!=1
                || (string)refs[0]["source_id"]!="R2015" || (string)refs[0]["verification"]!="Verified" || string.IsNullOrWhiteSpace((string)refs[0]["locator"]))throw new ArgumentException("Unsupported or unverified China source.");
            return new ChinaCardDefinition(cards,map,new CardId((string)source["card_id"]),new OpsRegionalBonus("source.china",(string)source["region_id"],(int)source["delta"]));
        }
    }
}
