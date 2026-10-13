using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ColdWarStruggle.Presentation;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
namespace ColdWarStruggle.Tests
{
    public class WorldMapDataTests
    {
        public static string Layout=>File.ReadAllText(Path.Combine(DefinitionTests.Root,"Data/Presentation/world-layout.json"));
        public static string Land=>File.ReadAllText(Path.Combine(DefinitionTests.Root,"Data/Presentation/world-land.json"));
        public static WorldMapAssets Assets()=>WorldMapAssets.Load(Layout,Land,DefinitionTests.World());
        [Test] public void Display_assets_can_only_be_created_by_validated_loader()
        { Assert.That(typeof(WorldMapAssets).GetConstructors(),Is.Empty); }
        [Test] public void Canonical_assets_cover_every_rule_country_and_two_decorative_powers()
        {
            var map=DefinitionTests.World();var assets=Assets();Assert.That(assets.Anchors.Count,Is.EqualTo(86));
            foreach(var c in map.Countries)Assert.That(assets.Anchors.ContainsKey(c.Id.Value),Is.True);
            Assert.That(assets.Anchors.ContainsKey("superpower.us"),Is.True);Assert.That(assets.Anchors.ContainsKey("superpower.ussr"),Is.True);
            Assert.That(assets.LandRings.Count,Is.EqualTo(128));Assert.That(assets.LandRings.Sum(r=>r.Count),Is.EqualTo(5143));
            Assert.That(assets.Anchors["country.france"].x,Is.GreaterThan(.5f));
            Assert.Throws<NotSupportedException>(()=>((IDictionary<string,Vector2>)assets.Anchors).Clear());
            Assert.Throws<NotSupportedException>(()=>((IList<IReadOnlyList<Vector2>>)assets.LandRings).Clear());
            Assert.Throws<NotSupportedException>(()=>((IList<Vector2>)assets.LandRings[0]).Clear());
        }
        [TestCase("missing")] [TestCase("unknown")] [TestCase("duplicate")] [TestCase("power")]
        [TestCase("out-of-range")] [TestCase("text-number")] [TestCase("nan")] [TestCase("third-axis")]
        [TestCase("shape")] [TestCase("routes")] [TestCase("3d")] [TestCase("dataset")]
        [TestCase("unknown-field")] [TestCase("version")] [TestCase("schema-type")]
        [TestCase("release")] [TestCase("content-version")] [TestCase("empty-description")]
        public void Invalid_or_unsupported_layout_is_rejected(string mutation)
        {
            var j=JObject.Parse(Layout);var nodes=(JArray)j["nodes"];var node=(JObject)nodes[0];
            switch(mutation)
            {
                case "missing":nodes.RemoveAt(0);break;
                case "unknown":node["node_id"]="country.unknown";break;
                case "duplicate":nodes[1]=node.DeepClone();break;
                case "power":nodes.RemoveAt(nodes.Count-1);break;
                case "out-of-range":node["anchor"][0]=1.01;break;
                case "text-number":node["anchor"][0]="0.5";break;
                case "nan":node["anchor"][0]=double.NaN;break;
                case "third-axis":((JArray)node["anchor"]).Add(0);break;
                case "shape":node["pick_shape_key"]="polygon";break;
                case "routes":((JArray)j["connection_routes"]).Add(new JObject());break;
                case "3d":j["layout_kind"]="Mesh3D";break;
                case "dataset":j["map_dataset_id"]="map.other";break;
                case "unknown-field":node["rule_adjacent"]="country.france";break;
                case "version":j["schema_version"]=2;break;
                case "schema-type":j["schema_version"]=1.0;break;
                case "release":j["scope"]="release-candidate";break;
                case "content-version":j["content_version"]="later";break;
                case "empty-description":j["description"]=" ";break;
            }
            Assert.That(()=>WorldMapAssets.Load(j.ToString(),Land,DefinitionTests.World()),Throws.InstanceOf<ArgumentException>());
        }
        [TestCase("open-ring")] [TestCase("short-ring")] [TestCase("empty")]
        [TestCase("out-of-range")] [TestCase("unknown-field")] [TestCase("projection")] [TestCase("too-many-points")]
        public void Corrupt_land_is_rejected_before_rendering(string mutation)
        {
            var j=JObject.Parse(Land);var rings=(JArray)j["rings"];
            switch(mutation)
            {
                case "open-ring":rings[0][0]=new JArray(.5,.5);break;
                case "short-ring":rings[0]=new JArray(new JArray(0,0));break;
                case "empty":rings.Clear();break;
                case "out-of-range":rings[0][1][0]=-1;break;
                case "unknown-field":j["countries"]=new JArray();break;
                case "projection":j["projection"]="mercator";break;
                case "too-many-points":rings[0]=new JArray(Enumerable.Range(0,20001).Select(i=>new JArray(0,0)));break;
            }
            Assert.That(()=>WorldMapAssets.Load(Layout,j.ToString(),DefinitionTests.World()),Throws.InstanceOf<ArgumentException>());
        }
        [TestCase("duplicate-field")] [TestCase("comments")] [TestCase("trailing")]
        [TestCase("oversize")] [TestCase("depth")] [TestCase("null")]
        public void Untrusted_json_boundaries_are_closed(string mutation)
        {
            string s=Layout;
            switch(mutation)
            {
                case "duplicate-field":s=s.Replace("\"schema_version\": 1", "\"schema_version\": 1, \"schema_version\": 1");break;
                case "comments":s="/* unknown */"+s;break;
                case "trailing":s+=" {}";break;
                case "oversize":s=new string(' ',524289)+s;break;
                case "depth":s=new string('[',40)+new string(']',40);break;
                case "null":s=null;break;
            }
            Assert.That(()=>WorldMapAssets.Load(s,Land,DefinitionTests.World()),Throws.InstanceOf<ArgumentException>());
        }
    }
}
