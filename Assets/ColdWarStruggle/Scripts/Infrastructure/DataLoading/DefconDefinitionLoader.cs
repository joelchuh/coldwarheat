using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ColdWarStruggle.Core;
using Newtonsoft.Json.Linq;
namespace ColdWarStruggle.Infrastructure
{
    public static class DefconDefinitionLoader
    {
        // Consumes the base track/geographic subset only; no effect grants or full RulePack.
        public static DefconDefinition LoadSubset(string json,MapDefinition map)
        {
            if(map==null)throw new ArgumentNullException(nameof(map));
            var root=Fields(ContractJson.Parse(json),"schema_version","id","status","scope","source","initial_level",
                "maximum_level","nuclear_level","turn_start_improvement","battleground_coup_degradation",
                "restricted_actions","blocked_regions_by_level","region_binding_status","content_version");
            if(Integer(root["schema_version"])!=1 || Text(root["id"])!="rules.deluxe2015.defcon"
                || Text(root["content_version"])!="0.2.0" || Text(root["status"])!="verified-subset"
                || Text(root["region_binding_status"])!="verified-definitions-runtime-pending")
                throw new ArgumentException("Unsupported DEFCON profile.");
            Text(root["scope"]);
            var source=Fields(root["source"],"id","url","checked_on","sections");
            if(Text(source["id"])!="R2015" || Text(source["url"])!="https://www.gmtgames.com/nnts/TS_Rules-2015.pdf"
                || !DateTime.TryParseExact(Text(source["checked_on"]),"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out _))
                throw new ArgumentException("Unsupported DEFCON source.");
            var sections=Strings(source["sections"],3);
            if(sections.Length!=3 || !new HashSet<string>(sections,StringComparer.Ordinal).SetEquals(new[]{"4.5A","6.3.4–5","8.1"}))
                throw new ArgumentException("Unsupported DEFCON source sections.");
            var actions=Strings(root["restricted_actions"],2).Select(action=>action switch
            {
                "Coup"=>DefconActionKind.Coup,"Realignment"=>DefconActionKind.Realignment,
                _=>throw new ArgumentException("Unsupported restricted action.")
            }).ToArray();
            if(root["blocked_regions_by_level"] is not JObject levels || levels.Count>64)
                throw new ArgumentException("Invalid DEFCON region table.");
            var knownRegionCount=map.Countries.SelectMany(c=>c.Subregions.Concat(new[]{c.Region})).Distinct(StringComparer.Ordinal).Count();
            var regions=new Dictionary<int,IReadOnlyList<string>>();
            foreach(var p in levels.Properties())
            {
                if(!int.TryParse(p.Name,NumberStyles.None,CultureInfo.InvariantCulture,out var level)
                    || p.Name!=level.ToString(CultureInfo.InvariantCulture))
                    throw new ArgumentException("Noncanonical DEFCON level.");
                regions.Add(level,Array.AsReadOnly(Strings(p.Value,knownRegionCount)));
            }
            return new DefconDefinition(map,Integer(root["initial_level"]),Integer(root["maximum_level"]),Integer(root["nuclear_level"]),
                Integer(root["turn_start_improvement"]),Integer(root["battleground_coup_degradation"]),actions,regions);
        }
        static JObject Fields(JToken token,params string[] expected)
        {
            if(token is not JObject obj || obj.Count!=expected.Length || obj.Properties().Any(p=>!expected.Contains(p.Name)))
                throw new ArgumentException("Unexpected DEFCON fields.");
            return obj;
        }
        static string Text(JToken token)
        {
            if(token?.Type!=JTokenType.String || string.IsNullOrWhiteSpace((string)token))
                throw new ArgumentException("Nonempty text required.");
            return (string)token;
        }
        static int Integer(JToken token)
        {
            if(token?.Type!=JTokenType.Integer || !int.TryParse(token.ToString(),NumberStyles.Integer,CultureInfo.InvariantCulture,out var number))
                throw new ArgumentException("DEFCON integer out of range or wrong type.");
            return number;
        }
        static string[] Strings(JToken token,int maximum)
        {
            if(token is not JArray array || array.Count>maximum)throw new ArgumentException("Invalid DEFCON list.");
            return array.Select(Text).ToArray();
        }
    }
}
