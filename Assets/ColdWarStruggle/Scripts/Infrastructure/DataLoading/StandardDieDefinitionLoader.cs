using System;
using System.Collections.Generic;
using System.Linq;
using ColdWarStruggle.Core;
using Newtonsoft.Json.Linq;
namespace ColdWarStruggle.Infrastructure
{
    public static class StandardDieDefinitionLoader
    {
        public static StandardDieDefinition Load(string json,string schema)
        {
            var root=RuleParameterContract.Load(json,schema,"standard-dice");
            RuleParameterContract.Profile(root,"rules.deluxe2015.standard-dice");
            RuleParameterContract.Sources(root,new[]{
                ("R2015","https://www.gmtgames.com/nnts/TS_Rules-2015.pdf","Deluxe rules 2015"),
                ("PROJECT-TR1",(string)null,"Engineering scope/verified-source boundaries, 2026-10-06")});
            var d=root.SelectToken("definitions.standard_d6");
            RuleParameterContract.Require(d?["id"],"die.standard_d6");RuleParameterContract.Require(d?["kind"],"UniformIntegerFaces");
            RuleParameterContract.Require(d?["minimum"],1);RuleParameterContract.Require(d?["maximum"],6);
            return new StandardDieDefinition((string)d["id"],(int)d["minimum"],(int)d["maximum"]);
        }
    }
    // Strict typed profile checks supplement the supplied document schema; no coercion or executable values.
    internal static class RuleParameterContract
    {
        internal static JObject Load(string json,string schema,string name)
        {
            var root=ContractJson.Parse(json) as JObject;var contract=ContractJson.Parse(schema) as JObject;
            if(root==null || contract==null)throw new ArgumentException("Object parameter document and schema required.");
            Require(contract["$id"],"urn:coldwarheat:contract:"+name+":1");ContractJson.Validate(root,contract);return root;
        }
        internal static void Profile(JObject root,string dataset)
        {
            Require(root["schema_version"],1);Require(root["dataset_id"],dataset);Require(root["content_version"],"0.1.0");
            Require(root["ruleset_id"],"ruleset.deluxe2015");Require(root["scope"],"verified-subset");
        }
        internal static void Require(JToken actual,object expected)
        {
            var value=expected==null?JValue.CreateNull():JToken.FromObject(expected);
            if(actual==null || actual.Type!=value.Type || !JToken.DeepEquals(actual,value))throw new ArgumentException("Unsupported typed parameter: "+(actual?.Path??"missing"));
        }
        internal static HashSet<string> Sources(JObject root,(string id,string url,string edition)[] expected)
        {
            if(!(root["source_catalog"] is JArray sources) || sources.Count!=expected.Length)throw new ArgumentException("Incomplete sources.");
            var seen=new HashSet<string>(StringComparer.Ordinal);
            foreach(var source in sources)
            {
                if(!(source is JObject s) || s["id"]?.Type!=JTokenType.String || !seen.Add((string)s["id"]))throw new ArgumentException("Invalid or duplicate source.");
                var id=(string)s["id"];var match=Array.FindIndex(expected,e=>e.id==id);
                if(match<0)throw new ArgumentException("Unknown parameter source.");
                Require(s["url"],expected[match].url);Require(s["edition"],expected[match].edition);
            }
            return seen;
        }
        internal static void Reference(JToken token,string dataset,string pointer)
        {Require(token?["dataset_id"],dataset);Require(token?["json_pointer"],pointer);}
    }
}
