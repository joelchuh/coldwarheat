using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Newtonsoft.Json.Linq;
using ColdWarStruggle.Core;
namespace ColdWarStruggle.Infrastructure
{
    public sealed partial class DemoCatalog
    {
        public DemoCatalog WithDeploymentScenario(string fixtures,string setup,string schema)
        {
            var definition=SetupDeploymentLoader.LoadSubset(setup,schema,Map);
            var data=Object(ContractJson.Parse(fixtures),"schema_version dataset_id description scope scenario");
            if(Int(data,"schema_version")!=1 || Text(data,"dataset_id")!="demo.deployment_scenario" || Text(data,"scope")!="fixture")
                throw new ArgumentException("Deployment lesson must be an explicit fixture.");
            var row=Object(data["scenario"],"id title selected_country hands");
            var id=Text(row,"id");var title=Text(row,"title");var selected=new CountryId(Text(row,"selected_country"));
            if(Scenarios.Any(s=>s.Id==id) || !Map.GetCountriesInRegion(definition.Stages[0].Region).Any(c=>c.Id.Equals(selected)))
                throw new ArgumentException("Duplicate scenario or invalid initial deployment target.");
            var handData=Object(row["hands"],"US USSR");var hands=new Dictionary<PlayerSide,IReadOnlyList<string>>();
            foreach(var side in new[]{PlayerSide.US,PlayerSide.USSR})
            {
                var list=Array(handData,side.ToString());
                if(list.Count>20 || list.Any(t=>t.Type!=JTokenType.String || string.IsNullOrWhiteSpace((string)t)))throw new ArgumentException("Invalid demo hands.");
                hands.Add(side,System.Array.AsReadOnly(list.Values<string>().ToArray()));
            }
            var countries=Map.Countries.ToDictionary(c=>c.Id,c=>new CountryState(0,0));
            var scenario=new DemoScenario(id,title,definition.Stages[0].Side,selected,0,
                new ReadOnlyDictionary<CountryId,CountryState>(countries),new ReadOnlyDictionary<PlayerSide,IReadOnlyList<string>>(hands),deployment:definition);
            var result=new DemoCatalog(Map,costs,Scenarios.Concat(new[]{scenario}),Names.ToDictionary(p=>p.Key,p=>p.Value,StringComparer.Ordinal),PrintedCards);
            // Validate the complete trusted start before publishing the extended catalog.
            result.CreateHost(id);
            return result;
        }
    }
}
