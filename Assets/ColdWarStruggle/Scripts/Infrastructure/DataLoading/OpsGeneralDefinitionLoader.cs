using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ColdWarStruggle.Core;
using Newtonsoft.Json.Linq;
namespace ColdWarStruggle.Infrastructure
{
    public static class OpsGeneralDefinitionLoader
    {
        // Only the general OPS policy is assembled; passing the enclosing schema does not activate any card effect.
        public static OpsGeneralDefinition LoadSubset(string json,string schema)
        {
            var root=ContractJson.Parse(json);var contract=ContractJson.Parse(schema) as JObject;
            if (contract==null || (string)contract["$id"]!="urn:coldwarheat:contract:persistent-effects-batch1:1") throw new ArgumentException("Unsupported OPS policy contract.");
            ContractJson.Validate(root,contract);
            if (root is not JObject || Integer(root["schema_version"])!=1 || Text(root["dataset_id"])!="effects.deluxe2015.persistent_batch1"
                || Text(root["content_version"])!="0.1.0" || Text(root["ruleset_id"])!="ruleset.deluxe2015" || Text(root["scope"])!="release-candidate")
                throw new ArgumentException("Unsupported OPS policy profile.");
            var policy=root["ops_policy"];
            if (policy is not JObject || Text(policy["aggregation"])!="FloorAfterRegionalBonus") throw new ArgumentException("Unsupported OPS aggregation policy.");
            Text(policy["description"]);
            var names=Enum.GetNames(typeof(OpsValuePurpose));
            RequireSet(policy["flat_purposes"],names);
            RequireSet(policy["regional_purposes"],new[]{"Influence","Coup","Realignment"});
            var known=new Dictionary<string,string>(StringComparer.Ordinal)
            {
                {"R2015","https://www.gmtgames.com/nnts/TS_Rules-2015.pdf"},
                {"C-DELUXE-EN","https://www.gmtgames.com/nnts/TS_Cards_Deluxe.pdf"}
            };
            var seen=new HashSet<string>(StringComparer.Ordinal);
            if (root["source_catalog"] is not JArray sources || sources.Count!=known.Count) throw new ArgumentException("Invalid OPS source catalog.");
            foreach (var source in sources)
            {
                var id=Text(source["id"]);
                if (!seen.Add(id) || !known.TryGetValue(id,out var url) || Text(source["url"])!=url) throw new ArgumentException("Unknown or duplicate OPS source.");
                Text(source["edition"]);Text(source["description"]);
            }
            var referenced=new HashSet<string>(StringComparer.Ordinal);
            if (policy["source_refs"] is not JArray refs || refs.Count!=known.Count) throw new ArgumentException("Invalid OPS source references.");
            foreach (var reference in refs)
            {
                var id=Text(reference["source_id"]);
                if (!known.ContainsKey(id) || !referenced.Add(id) || Text(reference["verification"])!="Verified") throw new ArgumentException("Unverified or duplicate OPS source reference.");
                Text(reference["locator"]);
            }
            var purposes=((JArray)policy["flat_purposes"]).Select(t=>(OpsValuePurpose)Enum.Parse(typeof(OpsValuePurpose),Text(t))).ToArray();
            return new OpsGeneralDefinition(Integer(policy["minimum"]),Integer(policy["ordinary_maximum"]),purposes);
        }
        static void RequireSet(JToken token,string[] expected)
        {
            if (token is not JArray values || values.Count!=expected.Length || !values.Select(Text).OrderBy(x=>x,StringComparer.Ordinal).SequenceEqual(expected.OrderBy(x=>x,StringComparer.Ordinal)))
                throw new ArgumentException("Incomplete or unknown OPS purposes.");
        }
        static string Text(JToken token)
        {
            if (token?.Type!=JTokenType.String || string.IsNullOrWhiteSpace((string)token)) throw new ArgumentException("Nonempty OPS text required.");
            return (string)token;
        }
        static int Integer(JToken token)
        {
            if (token?.Type!=JTokenType.Integer || !int.TryParse(token.ToString(),NumberStyles.Integer,CultureInfo.InvariantCulture,out var value))
                throw new ArgumentException("OPS integer wrong type or out of range.");
            return value;
        }
    }
}
