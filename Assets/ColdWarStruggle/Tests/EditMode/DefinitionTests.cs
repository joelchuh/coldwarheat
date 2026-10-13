using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ColdWarStruggle.Core;
using ColdWarStruggle.Infrastructure;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
namespace ColdWarStruggle.Tests
{
    public class DefinitionTests
    {
        public static string Root => Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, ".."));
        public static string MapJson => File.ReadAllText(Path.Combine(Root, "Data/Maps/deluxe-2015/world.json"));
        public static string Schema => File.ReadAllText(Path.Combine(Root, "Docs/Contracts/map.schema.json"));
        public static string Profile => File.ReadAllText(Path.Combine(Root, "Data/Maps/deluxe-2015/world.profile.json"));
        public static MapDefinition World() => MapDefinitionLoader.Load(MapJson, Schema, Profile);
        [TestCase(2,0,2,PlayerSide.US)] [TestCase(0,2,2,PlayerSide.USSR)]
        [TestCase(int.MaxValue,0,5,PlayerSide.US)]
        public void Control_requires_margin(int us, int ussr, int stability, PlayerSide expected)
            => Assert.That(ControlRules.GetController(us, ussr, stability), Is.EqualTo(expected));
        [Test] public void Equal_influence_is_uncontrolled() => Assert.That(ControlRules.GetController(2,2,2), Is.Null);
        [TestCase(-1,0,2)] [TestCase(0,-1,2)] [TestCase(0,0,0)]
        public void Invalid_control_inputs_rejected(int us, int ussr, int stability)
            => Assert.Throws<ArgumentOutOfRangeException>(() => ControlRules.GetController(us,ussr,stability));
        [Test] public void World_has_complete_immutable_bidirectional_topology()
        {
            var map = World(); Assert.That(map.Countries.Count, Is.EqualTo(84));
            foreach (var c in map.Countries)
                foreach (var neighbor in map.GetCountryNeighbors(c.Id))
                    Assert.That(map.GetCountryNeighbors(neighbor), Does.Contain(c.Id));
            Assert.That(map.GetSuperpowerNeighbors(PlayerSide.US), Does.Contain(new CountryId("country.canada")));
            Assert.Throws<NotSupportedException>(() => ((IList<CountryDefinition>)map.Countries).Clear());
            Assert.Throws<ArgumentException>(() => map.GetCountry(new CountryId("unknown")));
            Assert.Throws<ArgumentOutOfRangeException>(() => map.GetSuperpowerNeighbors((PlayerSide)99));
        }
        [TestCase("version")] [TestCase("duplicateCountry")] [TestCase("missing")]
        [TestCase("unknownField")] [TestCase("badReference")] [TestCase("duplicateEdge")]
        [TestCase("selfEdge")] [TestCase("coverage")] [TestCase("pending")]
        [TestCase("regionCycle")] [TestCase("fixture")] [TestCase("floatStability")]
        [TestCase("missingTopology")] [TestCase("replacedCountry")]
        public void Corrupt_world_rejected(string mutation)
        {
            var d = JObject.Parse(MapJson); var countries=(JArray)d["countries"];var edges=(JArray)d["country_edges"];
            switch(mutation)
            {
                case "version": d["schema_version"]=2;break;
                case "duplicateCountry": countries.Add(countries[0].DeepClone());break;
                case "missing": ((JObject)countries[0]).Remove("stability");break;
                case "unknownField": countries[0]["extra"]=true;break;
                case "badReference": edges[0]["b"]="country.imaginary";break;
                case "duplicateEdge": edges.Add(edges[0].DeepClone());break;
                case "selfEdge": edges[0]["b"]=edges[0]["a"];break;
                case "coverage": countries.RemoveAt(0);break;
                case "pending": countries[0]["source_refs"][0]["verification"]="Pending";break;
                case "regionCycle": d["regions"][0]["parent_id"]="region.europe";break;
                case "fixture": d["scope"]="fixture";break;
                case "floatStability": countries[0]["stability"]=2.5;break;
                case "missingTopology": edges.Clear();((JArray)d["superpower_edges"]).Clear();break;
                case "replacedCountry": d=JObject.Parse(d.ToString().Replace("country.canada","country.fake_canada"));break;
            }
            Assert.Throws<ArgumentException>(() => MapDefinitionLoader.Load(d.ToString(),Schema,Profile));
        }
        [Test] public void Duplicate_json_fields_and_trailing_documents_rejected()
        {
            Assert.Throws<ArgumentException>(()=>MapDefinitionLoader.Load(MapJson.Replace("\"schema_version\": 1", "\"schema_version\": 1, \"schema_version\": 1"),Schema,Profile));
            Assert.Throws<ArgumentException>(()=>MapDefinitionLoader.Load(MapJson+" {}",Schema,Profile));
        }
        [Test] public void Approved_profile_is_required_and_formatting_does_not_change_approval()
        {
            Assert.That(MapDefinitionLoader.Load(JObject.Parse(MapJson).ToString(Newtonsoft.Json.Formatting.None),Schema,Profile).Countries.Count,Is.EqualTo(84));
            Assert.Throws<ArgumentException>(()=>MapDefinitionLoader.Load(MapJson,Schema,"{}"));
            var profile=JObject.Parse(Profile); profile["sha256"]=new string('0',64);
            Assert.Throws<ArgumentException>(()=>MapDefinitionLoader.Load(MapJson,Schema,profile.ToString()));
        }
    }
}
