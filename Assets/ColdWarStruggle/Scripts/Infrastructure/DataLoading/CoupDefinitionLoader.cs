using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ColdWarStruggle.Core;
using Newtonsoft.Json.Linq;
namespace ColdWarStruggle.Infrastructure
{
    public static class CoupDefinitionLoader
    {
        // Fixed trusted subset profile; field/source checks are not full RulePack authentication.
        public static CoupDefinition LoadSubset(string json)
        {
            var root=Fields(ContractJson.Parse(json),"schema_version","content_version","id","status","source","die","stability_multiplier");
            if(Integer(root["schema_version"])!=1 || Text(root["content_version"])!="0.1.0"
                || Text(root["id"])!="rules.deluxe2015.coup" || Text(root["status"])!="verified-subset")
                throw new ArgumentException("Unsupported coup profile.");
            var source=Fields(root["source"],"id","url","checked_on","sections");
            if(Text(source["id"])!="R2015" || Text(source["url"])!="https://www.gmtgames.com/nnts/TS_Rules-2015.pdf"
                || !DateTime.TryParseExact(Text(source["checked_on"]),"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out _))
                throw new ArgumentException("Unsupported coup source.");
            if(source["sections"] is not JArray sections || sections.Count!=2
                || !new HashSet<string>(sections.Select(Text),StringComparer.Ordinal).SetEquals(new[]{"6.3.1","6.3.2"}))
                throw new ArgumentException("Incomplete or unsupported coup source sections.");
            var die=Fields(root["die"],"minimum","maximum");
            return new CoupDefinition(Integer(die["minimum"]),Integer(die["maximum"]),Integer(root["stability_multiplier"]));
        }
        static JObject Fields(JToken token,params string[] expected)
        {
            if(token is not JObject obj || obj.Count!=expected.Length || obj.Properties().Any(p=>!expected.Contains(p.Name)))
                throw new ArgumentException("Unexpected coup fields.");
            return obj;
        }
        static string Text(JToken token)
        {
            if(token?.Type!=JTokenType.String || string.IsNullOrWhiteSpace((string)token))throw new ArgumentException("Nonempty text required.");
            return (string)token;
        }
        static int Integer(JToken token)
        {
            if(token?.Type!=JTokenType.Integer || !int.TryParse(token.ToString(),NumberStyles.Integer,CultureInfo.InvariantCulture,out var value))
                throw new ArgumentException("Coup integer out of range or wrong type.");
            return value;
        }
    }
}
