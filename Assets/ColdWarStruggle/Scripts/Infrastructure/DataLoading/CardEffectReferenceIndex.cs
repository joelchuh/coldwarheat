using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace ColdWarStruggle.Infrastructure
{
    // Reference existence only; this is not a runtime card registry or complete rule-pack validator.
    public static class CardEffectReferenceIndex
    {
        public static IReadOnlyCollection<string> Load(string json)
        {
            var root=ContractJson.Parse(json) as JObject;
            if(root==null || !JToken.DeepEquals(root["schema_version"],new JValue(2))
                || Text(root["dataset_id"])!="cards.deluxe2015.definitions" || Text(root["content_version"])!="0.45.0"
                || Text(root["ruleset_id"])!="ruleset.deluxe2015" || Text(root["scope"])!="release-candidate")
                throw new ArgumentException("Unsupported card reference profile.");
            if(root["cards"] is not JArray cards || cards.Count==0 || cards.Count>256)
                throw new ArgumentException("Invalid reference list.");
            var cardIds=new HashSet<string>(StringComparer.Ordinal);var effects=new HashSet<string>(StringComparer.Ordinal);
            foreach(var token in cards)
            {
                if(token is not JObject row)throw new ArgumentException("Invalid card reference.");
                var id=Id(row["id"],"card");if(!cardIds.Add(id))throw new ArgumentException("Duplicate card reference.");
                var kind=Text(row["kind"]);var binding=row["effect_binding"];
                if(kind=="China")
                {
                    if(id!="card.china" || binding?.Type!=JTokenType.Null)throw new ArgumentException("Invalid China reference.");
                    continue;
                }
                if(kind!="Event" && kind!="Scoring" || binding is not JObject effect)
                    throw new ArgumentException("Missing or invalid effect reference.");
                if(!effects.Add(Id(effect["effect_id"],"effect")))throw new ArgumentException("Duplicate effect reference.");
            }
            if(effects.Count==0)throw new ArgumentException("No effect references.");
            return Array.AsReadOnly(effects.OrderBy(id=>id,StringComparer.Ordinal).ToArray());
        }
        static string Text(JToken token)=>token?.Type==JTokenType.String?(string)token:throw new ArgumentException("Invalid reference string.");
        static string Id(JToken token,string prefix)
        {
            var id=Text(token);
            if(id.Length>128 || !Regex.IsMatch(id,"^"+prefix+@"\.[a-z][a-z0-9_]*(\.[a-z0-9_]+)*$",RegexOptions.CultureInvariant,TimeSpan.FromSeconds(1)))
                throw new ArgumentException("Invalid stable reference ID.");
            return id;
        }
    }
}
