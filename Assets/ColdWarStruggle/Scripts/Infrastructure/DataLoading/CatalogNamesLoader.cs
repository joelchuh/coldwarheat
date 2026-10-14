using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
namespace ColdWarStruggle.Infrastructure
{
    public static class CatalogNamesLoader
    {
        public static IReadOnlyDictionary<string,string> Load(string json,string expectedLocale,IEnumerable<string> requiredIds)
        {
            string dataset=expectedLocale switch {"zh-CN"=>"localization.zh_cn.catalog_names","en-US"=>"localization.en_us.catalog_names",_=>throw new ArgumentException("Unsupported name locale.")};
            if(requiredIds==null)throw new ArgumentException("Missing requested names.");
            var ids=requiredIds.Take(513).ToArray();
            if(ids.Length==0 || ids.Length>512 || ids.Any(x=>string.IsNullOrWhiteSpace(x)) || ids.Distinct(StringComparer.Ordinal).Count()!=ids.Length)throw new ArgumentException("Invalid requested names.");
            var root=ContractJson.Parse(json) as JObject;
            var keys=new[]{"schema_version","dataset_id","content_version","locale","description","entries"};
            if(root==null || !root.Properties().Select(p=>p.Name).OrderBy(x=>x,StringComparer.Ordinal).SequenceEqual(keys.OrderBy(x=>x,StringComparer.Ordinal))
                || root["schema_version"]?.Type!=JTokenType.Integer || (int)root["schema_version"]!=1
                || Text(root["dataset_id"])!=dataset || Text(root["content_version"])!="0.1.0" || Text(root["locale"])!=expectedLocale
                || string.IsNullOrWhiteSpace(Text(root["description"])) || root["entries"] is not JObject entries || entries.Count==0 || entries.Count>512)
                throw new ArgumentException("Invalid name package.");
            foreach(var item in entries.Properties())
            {
                var value=Text(item.Value);
                if(!Regex.IsMatch(item.Name,@"\A(country|card|region|superpower)\.[a-z0-9_]+\.name\z") || string.IsNullOrWhiteSpace(value) || value.Length>160 || value.Any(char.IsControl))throw new ArgumentException("Invalid display name.");
            }
            var names=new Dictionary<string,string>(StringComparer.Ordinal);
            foreach(var id in ids)
            {
                if(!entries.TryGetValue(id+".name",StringComparison.Ordinal,out var value))throw new ArgumentException("Missing display name: "+id);
                names.Add(id,(string)value);
            }
            return new ReadOnlyDictionary<string,string>(names);
        }
        static string Text(JToken token)=>token?.Type==JTokenType.String?(string)token:throw new ArgumentException("Expected name text.");
    }
}
