using System;
using System.Collections.Generic;
using System.Linq;
using ColdWarStruggle.Infrastructure;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public sealed class PlayerPayloadManifestTests
    {
        internal static JObject Sample()=>new JObject{
            ["schema_version"]=1,["dataset_id"]="workflow.player_payloads",["description"]="fixture",
            ["entries"]=new JArray(new JObject{["source"]="Data/value.json",["destination"]="value.json"})};
        [Test] public void Formal_manifest_preserves_renamed_localization_and_complete_unique_sources()
        {
            var m=PlayerPayloadManifest.Load(PlayInfluenceEventTests.Read("Tools/Configuration/player-payloads.json"));
            Assert.That(m.Entries.Single(x=>x.Destination=="names.json").Source,Is.EqualTo("Data/Localization/en-US.catalog-names.json"));
            Assert.That(m.Entries.Single(x=>x.Destination=="pcg32-notice.md").Source,Is.EqualTo("Docs/References/ThirdParty/pcg32.md"));
            Assert.That(m.Entries.Select(x=>x.Destination).Distinct(StringComparer.OrdinalIgnoreCase).Count(),Is.EqualTo(m.Entries.Count));
            Assert.That(m.Entries.Any(x=>x.Destination=="world.json"),Is.True);
        }
        [Test] public void Returned_mapping_is_immutable()
        {
            var entries=PlayerPayloadManifest.Load(Sample().ToString()).Entries;
            Assert.Throws<NotSupportedException>(()=>((IList<PlayerPayloadEntry>)entries).Clear());
        }
        [TestCase("version")][TestCase("float-version")][TestCase("dataset")][TestCase("unknown")]
        [TestCase("missing")][TestCase("empty")][TestCase("too-many")][TestCase("null-row")]
        [TestCase("row-extra")][TestCase("row-missing")][TestCase("source-number")][TestCase("description-empty")]
        [TestCase("description-long")][TestCase("source-duplicate")][TestCase("destination-duplicate")]
        public void Invalid_closed_contract_is_rejected(string fault)
        {
            var j=Sample();var rows=(JArray)j["entries"];var row=(JObject)rows[0];
            switch(fault)
            {
                case "version":j["schema_version"]=2;break;
                case "float-version":j["schema_version"]=1.0;break;
                case "dataset":j["dataset_id"]="other";break;
                case "unknown":j["command"]="run";break;
                case "missing":j.Remove("description");break;
                case "empty":rows.Clear();break;
                case "too-many":for(int i=0;i<256;i++)rows.Add(row.DeepClone());break;
                case "null-row":rows[0]=null;break;
                case "row-extra":row["extra"]=true;break;
                case "row-missing":row.Remove("destination");break;
                case "source-number":row["source"]=1;break;
                case "description-empty":j["description"]=" ";break;
                case "description-long":j["description"]=new string('x',513);break;
                case "source-duplicate":rows.Add(new JObject{["source"]="Data/VALUE.json",["destination"]="second.json"});break;
                case "destination-duplicate":rows.Add(new JObject{["source"]="Data/second.json",["destination"]="VALUE.JSON"});break;
            }
            Assert.Throws<ArgumentException>(()=>PlayerPayloadManifest.Load(j.ToString()));
        }
        [TestCase("../outside.json")][TestCase("/outside.json")][TestCase("C:/outside.json")]
        [TestCase("Data//x.json")][TestCase("Data/./x.json")][TestCase("Data/../x.json")]
        [TestCase("Data/x.json.")][TestCase("Data/x.json ")][TestCase("Data/CON.json")]
        [TestCase("Data/a:b.json")][TestCase("Data/aux.json")][TestCase("Data/COM1.json")]
        [TestCase("Tools/secret.json")][TestCase("Docs/secret.json")][TestCase("Data\\x.json")]
        public void Unsafe_source_paths_are_rejected(string path)
        {var j=Sample();j["entries"][0]["source"]=path;Assert.Throws<ArgumentException>(()=>PlayerPayloadManifest.Load(j.ToString()));}
        [TestCase("sub/value.json")][TestCase("..")] [TestCase("value.json.")]
        [TestCase("CON.txt")][TestCase("LPT9.txt")][TestCase("x:y.json")][TestCase(" value.json")]
        public void Destination_is_safe_flat_filename(string path)
        {var j=Sample();j["entries"][0]["destination"]=path;Assert.Throws<ArgumentException>(()=>PlayerPayloadManifest.Load(j.ToString()));}
        [TestCase("trailing-object")][TestCase("trailing-array")][TestCase("single-quotes")]
        [TestCase("unquoted-property")][TestCase("leading-zero")][TestCase("hex")]
        public void Nonstandard_json_dialects_cannot_disagree_with_the_python_consumer(string dialect)
        {
            var text=Sample().ToString(Newtonsoft.Json.Formatting.None);
            switch(dialect)
            {
                case "trailing-object":text=text.Substring(0,text.Length-1)+",}";break;
                case "trailing-array":text=text.Replace("}]","},]");break;
                case "single-quotes":text=text.Replace('"','\'');break;
                case "unquoted-property":text=text.Replace("\"schema_version\"","schema_version");break;
                case "leading-zero":text=text.Replace("\"schema_version\":1","\"schema_version\":01");break;
                case "hex":text=text.Replace("\"schema_version\":1","\"schema_version\":0x1");break;
            }
            Assert.Throws<ArgumentException>(()=>PlayerPayloadManifest.Load(text));
        }
        [Test] public void Quoted_description_punctuation_and_escapes_remain_valid_json()
        {
            var sample=Sample();sample["description"]="quoted \"name\" \\ path,} ,] ' 中文\nnext";
            Assert.That(PlayerPayloadManifest.Load(sample.ToString()).Entries.Count,Is.EqualTo(1));
        }
        [Test] public void Duplicate_keys_comments_trailing_roots_and_size_limit_are_rejected()
        {
            var text=Sample().ToString(Newtonsoft.Json.Formatting.None);
            Assert.Throws<ArgumentException>(()=>PlayerPayloadManifest.Load(text.Replace("\"schema_version\":1","\"schema_version\":1,\"schema_version\":1")));
            Assert.Throws<ArgumentException>(()=>PlayerPayloadManifest.Load("/*comment*/"+text));
            Assert.Throws<ArgumentException>(()=>PlayerPayloadManifest.Load(text+"{}"));
            Assert.Throws<ArgumentException>(()=>PlayerPayloadManifest.Load(new string(' ',1048577)));
        }
    }
}
