using System;
using ColdWarStruggle.Core;
using Newtonsoft.Json.Linq;
namespace ColdWarStruggle.Infrastructure
{
    public static class DefectorsHeadlineLoader
    {
        // Only selected-headline semantics are executable; ordinary VP and nested sources are not enabled.
        public static DefectorsHeadlineDefinition Load(string json,string schema,CardCatalog catalog)
        {
            var root=ContractJson.Parse(json);var contract=ContractJson.Parse(schema) as JObject;
            if(contract==null || (string)contract["$id"]!="urn:coldwarheat:contract:defectors:1")throw new ArgumentException("Unsupported Defectors contract.");
            ContractJson.Validate(root,contract);
            if((int)root["schema_version"]!=1 || (string)root["dataset_id"]!="effects.deluxe2015.defectors" || (string)root["ruleset_id"]!="ruleset.deluxe2015"
                || (string)root["content_version"]!="0.1.0" || (string)root["scope"]!="verified-subset")throw new ArgumentException("Unsupported Defectors profile.");
            var p=root["parameters"]["defectors"];
            if((string)p["effect_id"]!="effect.defectors" || (string)p["template"]!="SelectedHeadlineCancellationOrSovietCardUseVp"
                || (string)p["headline_strategy"]!="USSelectedDefectorsBeforeOrdinaryPrintedOpsOrder"
                || (string)p["us_selected_target"]!="PendingUSSRSelectedHeadlineIncludingScoring" || (string)p["ussr_selected"]!="CancelSelfAndNoVp"
                || (string)p["cancelled_disposition"]!="DiscardPileRegardlessOfPrintedStar" || p["cancelled_success_fact"]?.Type!=JTokenType.Boolean
                || (bool)p["cancelled_success_fact"])throw new ArgumentException("Unsupported selected-headline cancellation semantics.");
            return new DefectorsHeadlineDefinition(catalog,new CardId((string)p["card_id"]));
        }
    }
}
