using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using ColdWarStruggle.Core;
namespace ColdWarStruggle.Infrastructure
{
    public static class MapDefinitionLoader
    {
        public static MapDefinition Load(string json, string schema, string approvedProfile)
        {
            try { return LoadChecked(json,schema,approvedProfile); }
            catch(OverflowException e) { throw new ArgumentException("Numeric value out of range.",e); }
        }
        static MapDefinition LoadChecked(string json,string schema,string approvedProfile)
        {
            var root=ContractJson.Parse(json);var contract=ContractJson.Parse(schema) as JObject;
            if(contract==null || (string)contract["$id"]!="urn:coldwarheat:contract:map:1") throw new ArgumentException("Unsupported map contract.");
            ContractJson.Validate(root,contract);
            if((int)root["schema_version"]!=1 || (string)root["content_version"]!="0.1.0" || (string)root["dataset_id"]!="map.deluxe2015.world"
                || (string)root["ruleset_id"]!="ruleset.deluxe2015" || (string)root["scope"]!="release-candidate") throw new ArgumentException("Unsupported map profile.");
            var sourceIds=new HashSet<string>();
            foreach(var s in root["source_catalog"]) if(!sourceIds.Add((string)s["id"])) throw new ArgumentException("Duplicate source.");
            foreach(var property in new[]{"regions","countries","superpowers","country_edges","superpower_edges"})
                foreach(var item in root[property]) foreach(var source in item["source_refs"])
                    if(!sourceIds.Contains((string)source["source_id"]) || (string)source["verification"]!="Verified") throw new ArgumentException("Unverified source reference.");
            var regions=new Dictionary<string,JToken>();
            foreach(var region in root["regions"])
                if(regions.ContainsKey((string)region["id"])) throw new ArgumentException("Duplicate region.");
                else regions.Add((string)region["id"],region);
            foreach(var region in regions.Values)
            {
                var parent=(string)region["parent_id"];
                if((string)region["kind"]=="ScoringRegion") { if(parent!=null) throw new ArgumentException("Scoring region has parent."); }
                else if(parent==null || !regions.ContainsKey(parent) || (string)regions[parent]["kind"]!="ScoringRegion") throw new ArgumentException("Invalid subregion parent.");
            }
            var countries=new List<CountryDefinition>();
            foreach(var c in root["countries"])
            {
                var primary=(string)c["primary_region_id"];var subs=c["subregion_ids"].Values<string>().ToArray();
                if(!regions.ContainsKey(primary) || (string)regions[primary]["kind"]!="ScoringRegion") throw new ArgumentException("Invalid primary region.");
                if(subs.Any(s=>!regions.ContainsKey(s) || (string)regions[s]["kind"]!="Subregion" || (string)regions[s]["parent_id"]!=primary)) throw new ArgumentException("Invalid subregion.");
                countries.Add(new CountryDefinition(new CountryId((string)c["id"]),(int)c["stability"],primary,(bool)c["base_battleground"],subs));
            }
            if(countries.Count!=84) throw new ArgumentException("Incomplete world map.");
            var powers=new Dictionary<PlayerSide,IReadOnlyList<CountryId>>();
            foreach(var s in root["superpowers"])
            {
                var side=(string)s["side"]=="US"?PlayerSide.US:PlayerSide.USSR;var id=side==PlayerSide.US?"superpower.us":"superpower.ussr";
                if((string)s["id"]!=id || powers.ContainsKey(side)) throw new ArgumentException("Invalid superpower.");
                powers.Add(side,root["superpower_edges"].Where(e=>(string)e["superpower_id"]==id).Select(e=>new CountryId((string)e["country_id"])).ToArray());
            }
            var edges=root["country_edges"].Select(e=>new KeyValuePair<CountryId,CountryId>(new CountryId((string)e["a"]),new CountryId((string)e["b"])));
            var definition = new MapDefinition(countries,edges,powers);
            MapProfile.Validate(root, approvedProfile);
            return definition;
        }
    }
}
