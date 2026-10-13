using System;
using ColdWarStruggle.Core;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
namespace ColdWarStruggle.Infrastructure
{
    public static class SetupDeploymentLoader
    {
        // Validates and consumes deployment only. Other setup references do not become runtime authority here.
        public static SetupDeploymentDefinition LoadSubset(string json,string schema,MapDefinition map)
        {
            if(map==null)throw new ArgumentNullException(nameof(map));
            try { return LoadChecked(json,schema,map); }
            catch(OverflowException e) { throw new ArgumentException("Deployment number out of range.",e); }
        }
        static SetupDeploymentDefinition LoadChecked(string json,string schema,MapDefinition map)
        {
            var root=ContractJson.Parse(json);var contract=ContractJson.Parse(schema) as JObject;
            if(contract==null || (string)contract["$id"]!="urn:coldwarheat:contract:setup:1")throw new ArgumentException("Unsupported setup contract.");
            ContractJson.Validate(root,contract);
            if((int)root["schema_version"]!=1 || (string)root["dataset_id"]!="setup.deluxe2015.standard"
                || (string)root["content_version"]!="0.1.0" || (string)root["ruleset_id"]!="ruleset.deluxe2015"
                || (string)root["scope"]!="release-candidate" || (string)root["map_dataset_id"]!="map.deluxe2015.world"
                || (int)root["default_country_influence_per_side"]!=0)throw new ArgumentException("Unsupported setup deployment profile.");
            var sources=new HashSet<string>(StringComparer.Ordinal);
            foreach(var source in root["source_catalog"])
                if(!sources.Add((string)source["id"]))throw new ArgumentException("Duplicate setup source.");
            void CheckSources(JToken refs)
            { foreach(var source in refs)if((string)source["verification"]!="Verified" || !sources.Contains((string)source["source_id"]))throw new ArgumentException("Unverified setup source."); }
            CheckSources(root["source_refs"]);
            var stages=new List<SetupDeploymentStage>();
            foreach(var stage in root["deployment_stages"])
            {
                CheckSources(stage["source_refs"]);
                var side=(string)stage["side"]=="USSR"?PlayerSide.USSR:PlayerSide.US;
                var region=(string)stage["free_allocation"]["region_id"];
                if(region!=(side==PlayerSide.USSR?"region.eastern_europe":"region.western_europe"))throw new ArgumentException("Unsupported setup region.");
                var fixedInfluence=stage["fixed_influence"].Select(a=>new InfluenceAllocation(new CountryId((string)a["country_id"]),(int)a["amount"]));
                stages.Add(new SetupDeploymentStage(side,fixedInfluence,region,(int)stage["free_allocation"]["amount"]));
            }
            return new SetupDeploymentDefinition(map,stages);
        }
    }
}
