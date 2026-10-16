using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
namespace ColdWarStruggle.Infrastructure
{
    internal static class HandEventContract
    {
        internal static JObject Load(string json,string schema,string branch)
        {
            if(branch!="blockade" && branch!="five_year_plan")throw new ArgumentException("Unsupported hand-event branch.");
            var root=ContractJson.Parse(json);var contract=ContractJson.Parse(schema) as JObject;
            if(contract==null || (string)contract["$id"]!="urn:coldwarheat:contract:hand-events-batch1:1")throw new ArgumentException("Unsupported hand-event contract.");
            ContractJson.Validate(root,contract);
            if((int)root["schema_version"]!=1 || (string)root["dataset_id"]!="effects.deluxe2015.hand_events_batch1" || (string)root["content_version"]!="0.1.0"
                || (string)root["ruleset_id"]!="ruleset.deluxe2015" || (string)root["scope"]!="release-candidate")throw new ArgumentException("Unsupported hand-event profile.");
            var expected=new Dictionary<string,string>(StringComparer.Ordinal)
            {
                {"C-DELUXE-EN","https://www.gmtgames.com/nnts/TS_Cards_Deluxe.pdf"},
                {"R2015","https://www.gmtgames.com/nnts/TS_Rules-2015.pdf"},
                {"F2010","https://www.gmtgames.com/nnts/FAQv5.pdf"},
                {"PROJECT-HC1",null}
            };
            var sources=new HashSet<string>(StringComparer.Ordinal);
            foreach(var source in root["source_catalog"])
            {
                var id=(string)source["id"];
                if(!sources.Add(id) || !expected.TryGetValue(id,out var url) || (string)source["url"]!=url)throw new ArgumentException("Unknown, duplicate or mismatched hand-event source.");
            }
            if(sources.Count!=expected.Count)throw new ArgumentException("Incomplete hand-event sources.");
            // Validate the enclosing document, but assemble only the requested implemented branch.
            var p=(JObject)root["parameters"][branch];var refs=new HashSet<string>(StringComparer.Ordinal);
            foreach(var reference in p["source_refs"])
            {
                var id=(string)reference["source_id"];
                if(!sources.Contains(id) || !refs.Add(id) || (string)reference["verification"]!="Verified")throw new ArgumentException("Invalid hand-event evidence.");
            }
            if(refs.Count!=sources.Count)throw new ArgumentException("Incomplete hand-event evidence.");
            return p;
        }
    }
}
