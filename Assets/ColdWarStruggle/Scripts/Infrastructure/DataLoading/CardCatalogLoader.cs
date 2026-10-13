using System;
using System.Collections.Generic;
using System.Linq;
using ColdWarStruggle.Core;
using Newtonsoft.Json.Linq;
namespace ColdWarStruggle.Infrastructure
{
    public static class CardCatalogLoader
    {
        // Printed definitions only. Declared handlers and parameter references do not authorize execution.
        public static CardCatalog LoadPrintedSubset(string json,string schema,string approvedProfile)
        {
            try { return LoadChecked(json,schema,approvedProfile); }
            catch(OverflowException e) { throw new ArgumentException("Card numeric value out of range.",e); }
        }
        static CardCatalog LoadChecked(string json,string schema,string approvedProfile)
        {
            var root=ContractJson.Parse(json) as JObject;var contract=ContractJson.Parse(schema) as JObject;
            if(root==null || contract==null || (string)contract["$id"]!="urn:coldwarheat:contract:cards:2")throw new ArgumentException("Unsupported card contract.");
            ContractJson.Validate(root,contract);
            if((int)root["schema_version"]!=2 || (string)root["dataset_id"]!="cards.deluxe2015.definitions" || (string)root["content_version"]!="0.45.0"
                || (string)root["ruleset_id"]!="ruleset.deluxe2015" || (string)root["scope"]!="release-candidate")throw new ArgumentException("Unsupported printed card profile.");
            var sources=new HashSet<string>(StringComparer.Ordinal);
            foreach(var source in root["source_catalog"])
                if(!sources.Add((string)source["id"]))throw new ArgumentException("Duplicate card source.");
            var contracts=new Dictionary<string,string>(StringComparer.Ordinal);
            foreach(var effect in root["effect_contracts"])
            {
                var id=(string)effect["effect_id"];
                if(contracts.ContainsKey(id))throw new ArgumentException("Duplicate effect declaration.");
                contracts.Add(id,(string)effect["contract_version"]);
                // Effect rulings may still be pending; this loader publishes no executable handlers.
                CheckSources(effect["source_refs"],sources,false);
            }
            var cards=new List<CardDefinition>();var used=new HashSet<string>(StringComparer.Ordinal);
            foreach(var row in root["cards"])
            {
                CheckSources(row["source_refs"],sources,true);
                CardEffectBinding binding=null;
                if(row["effect_binding"].Type!=JTokenType.Null)
                {
                    var raw=row["effect_binding"];var id=(string)raw["effect_id"];var version=(string)raw["contract_version"];
                    if(!contracts.TryGetValue(id,out var declaredVersion) || declaredVersion!=version)throw new ArgumentException("Unknown or mismatched effect declaration.");
                    binding=new CardEffectBinding(id,version,Choice<CardParameterBindingKind>(raw["parameter_binding_status"]),
                        raw["parameter_refs"].Select(p=>new CardParameterReference((string)p["dataset_id"],(string)p["json_pointer"])));
                    used.Add(id);
                }
                cards.Add(new CardDefinition(new CardId((string)row["id"]),(int)row["printed_number"],(string)row["name_key"],(string)row["rules_text_key"],
                    Choice<CardKind>(row["kind"]),(int?)row["printed_ops"],Choice<CardAffiliation>(row["affiliation"]),row["era"].Type==JTokenType.Null?(GameEra?)null:Choice<GameEra>(row["era"]),
                    Choice<CardDeckEntry>(row["deck_entry"]),(bool)row["remove_after_resolved_event"],(bool)row["display_persistent_reminder"],(string)row["scoring_region_id"],binding));
            }
            var catalog=new CardCatalog(cards);
            if(catalog.Cards.Count!=103 || !catalog.Cards.Select(c=>c.PrintedNumber).OrderBy(n=>n).SequenceEqual(Enumerable.Range(1,103))
                || catalog.Cards.Count(c=>c.Kind==CardKind.China)!=1 || used.Count!=contracts.Count)throw new ArgumentException("Incomplete printed card catalog.");
            CardPrintedProfile.Validate(root,approvedProfile);
            return catalog;
        }
        static T Choice<T>(JToken value) where T:struct
        {
            var name=(string)value;
            if(!Enum.TryParse<T>(name,false,out var parsed) || !Enum.IsDefined(typeof(T),parsed) || parsed.ToString()!=name)
                throw new ArgumentException("Unknown card enum.");
            return parsed;
        }
        static void CheckSources(JToken refs,HashSet<string> sources,bool requireVerified)
        {
            foreach(var r in refs)
                if(!sources.Contains((string)r["source_id"]) || string.IsNullOrWhiteSpace((string)r["locator"])
                    || (requireVerified && (string)r["verification"]!="Verified"))throw new ArgumentException("Invalid printed source reference.");
        }
    }
}
