using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ColdWarStruggle.Core;
using Newtonsoft.Json.Linq;

namespace ColdWarStruggle.Infrastructure
{
    public static class BudgetedRemovalCatalogLoader
    {
        public static IReadOnlyDictionary<string, InfluenceEffectDefinition> Load(string json, string schema,
            MapDefinition map, IReadOnlyCollection<string> knownEffectIds)
        {
            if (map == null || knownEffectIds == null) throw new ArgumentNullException();
            if (knownEffectIds.Count == 0 || knownEffectIds.Any(string.IsNullOrWhiteSpace))
                throw new ArgumentException("Known effect references are required.");
            try { return LoadChecked(json, schema, map, new HashSet<string>(knownEffectIds, StringComparer.Ordinal)); }
            catch (OverflowException error) { throw new ArgumentException("Effect parameter out of range.", error); }
        }

        static IReadOnlyDictionary<string, InfluenceEffectDefinition> LoadChecked(string json, string schema,
            MapDefinition map, HashSet<string> knownEffects)
        {
            var root = ContractJson.Parse(json); var contract = ContractJson.Parse(schema) as JObject;
            if (contract == null || (string)contract["$id"] != "urn:coldwarheat:contract:early-influence-batch2:1")
                throw new ArgumentException("Unsupported removal contract.");
            ContractJson.Validate(root, contract);
            if ((int)root["schema_version"] != 1 || (string)root["dataset_id"] != "effects.deluxe2015.early_influence_batch2"
                || (string)root["content_version"] != "0.1.0" || (string)root["ruleset_id"] != "ruleset.deluxe2015"
                || (string)root["scope"] != "release-candidate") throw new ArgumentException("Unsupported removal profile.");
            var sourceIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var source in root["source_catalog"])
                if (!sourceIds.Add((string)source["id"])) throw new ArgumentException("Duplicate source.");
            var definitions = new Dictionary<string, InfluenceEffectDefinition>(StringComparer.Ordinal);
            // Explicit subset: the era-dependent third parameter is schema-checked but is not a runnable definition.
            foreach (var name in new[] { "socialist_governments", "suez_crisis" })
            {
                var p = root["parameters"][name]; var card = (string)p["card_id"]; var effect = (string)p["effect_id"];
                if (card != "card." + name || effect != "effect." + name || !knownEffects.Contains(effect))
                    throw new ArgumentException("Removal identity mismatch or unknown effect reference.");
                if ((string)p["template"] != "BudgetedRemoval" || (string)p["shortage_policy"] != "RemoveMaximumAvailable")
                    throw new ArgumentException("Unsupported removal semantics.");
                foreach (var source in p["source_refs"])
                    if ((string)source["verification"] != "Verified" || !sourceIds.Contains((string)source["source_id"]))
                        throw new ArgumentException("Unverified removal source.");
                var blocker = (string)p["blocked_by_effect_id"];
                if (blocker != null && !knownEffects.Contains(blocker)) throw new ArgumentException("Unknown blocking effect reference.");
                var scope = p["target_scope"]; string region = null; CountryId[] countries = null;
                switch ((string)scope["kind"])
                {
                    case "Region":
                        region = (string)scope["region_id"];
                        if (map.GetCountriesInRegion(region).Count == 0) throw new ArgumentException("Unknown removal region.");
                        break;
                    case "CountrySet":
                        countries = scope["country_ids"].Values<string>().Select(id => new CountryId(id)).ToArray();
                        if (countries.Any(c => !map.Contains(c))) throw new ArgumentException("Unknown removal country.");
                        break;
                    default: throw new ArgumentException("Unsupported removal target scope.");
                }
                var beneficiary = (string)p["beneficiary"] == "US" ? PlayerSide.US : PlayerSide.USSR;
                definitions.Add(card, new BudgetedRemovalEffect(card, effect, beneficiary, (int)p["total_amount"],
                    (int)p["per_country_cap"], region, countries, blocker));
            }
            return new ReadOnlyDictionary<string, InfluenceEffectDefinition>(definitions);
        }
    }
}
