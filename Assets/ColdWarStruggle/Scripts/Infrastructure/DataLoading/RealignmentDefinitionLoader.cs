using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ColdWarStruggle.Core;
using Newtonsoft.Json.Linq;
namespace ColdWarStruggle.Infrastructure
{
    public static class RealignmentDefinitionLoader
    {
        // Fixed trusted subset profile; strict fields and sources are not RulePack authentication.
        public static RealignmentDefinition LoadSubset(string json)
        {
            var root=Fields(ContractJson.Parse(json),"schema_version","content_version","id","status","description","source","die","ops_cost_per_roll","modifiers");
            if(Integer(root["schema_version"])!=1 || Text(root["content_version"])!="0.1.0" || Text(root["id"])!="rules.deluxe2015.realignment" || Text(root["status"])!="verified-subset")throw new ArgumentException("Unsupported realignment profile.");
            Text(root["description"]);
            var source=Fields(root["source"],"id","url","checked_on","sections");
            if(Text(source["id"])!="R2015" || Text(source["url"])!="https://www.gmtgames.com/nnts/TS_Rules-2015.pdf" || !DateTime.TryParseExact(Text(source["checked_on"]),"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out _))throw new ArgumentException("Unsupported realignment source.");
            if(source["sections"] is not JArray sections || sections.Count!=6 || !new HashSet<string>(sections.Select(Text),StringComparer.Ordinal).SetEquals(new[]{"2.1.5","6.0","6.2.1","6.2.2","6.2.3","8.1.5"}))throw new ArgumentException("Incomplete realignment source sections.");
            var die=Fields(root["die"],"minimum","maximum");var mods=Fields(root["modifiers"],"per_adjacent_controlled_country","more_local_influence","adjacent_own_superpower");
            return new RealignmentDefinition(Integer(die["minimum"]),Integer(die["maximum"]),Integer(root["ops_cost_per_roll"]),Integer(mods["per_adjacent_controlled_country"]),Integer(mods["more_local_influence"]),Integer(mods["adjacent_own_superpower"]));
        }
        static JObject Fields(JToken token,params string[] expected)
        {
            if(token is not JObject obj || obj.Count!=expected.Length || obj.Properties().Any(p=>!expected.Contains(p.Name)))throw new ArgumentException("Unexpected realignment fields.");
            return obj;
        }
        static string Text(JToken token)
        {
            if(token?.Type!=JTokenType.String || string.IsNullOrWhiteSpace((string)token))throw new ArgumentException("Nonempty realignment text required.");
            return (string)token;
        }
        static int Integer(JToken token)
        {
            if(token?.Type!=JTokenType.Integer || !int.TryParse(token.ToString(),NumberStyles.Integer,CultureInfo.InvariantCulture,out var value))throw new ArgumentException("Realignment integer out of range or wrong type.");
            return value;
        }
    }
}
