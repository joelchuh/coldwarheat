using System;
using System.Collections.Generic;
using System.Linq;
using ColdWarStruggle.Core;
using Newtonsoft.Json.Linq;
namespace ColdWarStruggle.Infrastructure
{
    public static class InitialDealDefinitionLoader
    {
        // Consumes initial card configuration only, never publishes an executable RulePack.
        public static InitialDealDefinition LoadSubset(string setupJson,string setupSchema,string turnFlowJson,CardCatalog catalog)
        {
            if(catalog==null)throw new ArgumentNullException(nameof(catalog));
            var root=ContractJson.Parse(setupJson);
            var schema=ContractJson.Parse(setupSchema) as JObject;
            if(schema==null || (string)schema["$id"]!="urn:coldwarheat:contract:setup:1")throw new ArgumentException("Unsupported setup contract.");
            ContractJson.Validate(root,schema);
            if(!JToken.DeepEquals(root["schema_version"],new JValue(1)) || (string)root["dataset_id"]!="setup.deluxe2015.standard"
                || (string)root["content_version"]!="0.1.0" || (string)root["ruleset_id"]!="ruleset.deluxe2015"
                || (string)root["scope"]!="release-candidate" || (string)root["cards_dataset_id"]!="cards.deluxe2015.definitions"
                || (string)root["initial_card_era"]!="Early")throw new ArgumentException("Unsupported initial deal profile.");
            var sources=new HashSet<string>(StringComparer.Ordinal);
            foreach(var source in root["source_catalog"])
                if(string.IsNullOrWhiteSpace((string)source["id"]) || !sources.Add((string)source["id"]))throw new ArgumentException("Invalid setup sources.");
            void CheckSources(JToken refs)
            {
                foreach(var source in refs)
                    if((string)source["verification"]!="Verified" || !sources.Contains((string)source["source_id"]) || string.IsNullOrWhiteSpace((string)source["locator"]))
                        throw new ArgumentException("Unverified initial deal source.");
            }
            CheckSources(root["source_refs"]);CheckSources(root["china"]["source_refs"]);
            foreach(var field in new[]{"turn_number","hand_target"})
            {
                var reference=root["initial_values"][field];
                var pointer=field=="turn_number"?"/era_bands/0/first_turn":"/era_bands/0/hand_target";
                if((string)reference["dataset_id"]!="rules.deluxe2015.turn-flow" || (string)reference["json_pointer"]!=pointer)
                    throw new ArgumentException("Unsupported initial deal parameter reference.");
            }
            var calendar=TurnCalendarLoader.Load(turnFlowJson);
            var first=ContractJson.Parse(turnFlowJson)["era_bands"][0];
            if((string)first["id"]!="early" || !JToken.DeepEquals(first["first_turn"],new JValue(1)))
                throw new ArgumentException("Initial reference no longer points to the first era.");
            var row=root["china"];
            var owner=(string)row["owner"] switch {"USSR"=>PlayerSide.USSR,"US"=>PlayerSide.US,_=>throw new ArgumentException("Unknown China owner.")};
            var face=(string)row["face"] switch {"FaceUp"=>CardFace.FaceUp,"FaceDown"=>CardFace.FaceDown,_=>throw new ArgumentException("Unknown China face.")};
            var china=new ChinaCardState(new CardId((string)row["card_id"]),owner,face);
            if(catalog.Cards.Count(c=>c.Kind==CardKind.China)!=1 || catalog.GetCard(china.Id).Kind!=CardKind.China)
                throw new ArgumentException("Missing China definition.");
            var definition=new InitialDealDefinition(calendar.ForTurn(1),china);
            if(catalog.GetEraCards(definition.Turn.Era).Count<2L*definition.HandTarget)throw new ArgumentException("Insufficient initial era cards.");
            return definition;
        }
    }
}
