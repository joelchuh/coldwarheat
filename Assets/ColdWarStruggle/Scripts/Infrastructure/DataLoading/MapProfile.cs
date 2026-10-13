using System;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace ColdWarStruggle.Infrastructure
{
    // The separately reviewed profile is trusted input. Never derive it from a candidate map at runtime.
    internal static class MapProfile
    {
        public static void Validate(JToken map, string profileJson)
        {
            var profile = ContractJson.Parse(profileJson) as JObject;
            var keys = new[] { "schema_version", "dataset_id", "description", "map_dataset_id", "map_content_version", "canonicalization", "sha256", "reviewed_source_commit" };
            if (profile == null || profile.Properties().Count() != keys.Length || keys.Any(k => profile[k] == null)
                || profile["schema_version"].Type != JTokenType.Integer || (int)profile["schema_version"] != 1
                || keys.Where(k => k != "schema_version").Any(k => profile[k].Type != JTokenType.String || string.IsNullOrWhiteSpace((string)profile[k]))
                || (string)profile["dataset_id"] != "map.deluxe2015.approved_profile"
                || (string)profile["map_dataset_id"] != (string)map["dataset_id"]
                || (string)profile["map_content_version"] != (string)map["content_version"]
                || (string)profile["canonicalization"] != "sorted-object-keys-json-utf8-v1")
                throw new ArgumentException("Unsupported approved map profile.");
            if (!string.Equals(CanonicalJsonDigest.Compute(map), (string)profile["sha256"], StringComparison.Ordinal))
                throw new ArgumentException("Map differs from the independently approved profile.");
        }
    }
}
