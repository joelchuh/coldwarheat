using System;
using System.Collections.ObjectModel;
using System.Linq;
using ColdWarStruggle.Core;
using Newtonsoft.Json.Linq;
namespace ColdWarStruggle.Infrastructure
{
    public sealed partial class DemoCatalog
    {
        public DemoCatalog WithInitialDealScenario(string fixtures,string setup,string schema,string turnFlow,CardCatalog cards,string names)
        {
            if(PrintedCards!=null && !ReferenceEquals(PrintedCards,cards))throw new ArgumentException("Demo cannot mix card catalogs.");
            var deployment=SetupDeploymentLoader.LoadSubset(setup,schema,Map);
            var deal=InitialDealDefinitionLoader.LoadSubset(setup,schema,turnFlow,cards);
            var data=Object(ContractJson.Parse(fixtures),"schema_version dataset_id description scope scenario");
            if(Int(data,"schema_version")!=1 || Text(data,"dataset_id")!="demo.initial_deal_scenario" || Text(data,"scope")!="fixture")
                throw new ArgumentException("Initial deal lesson must be an explicit fixture.");
            var row=Object(data["scenario"],"id title selected_country random_state");
            var id=Text(row,"id");var title=Text(row,"title");var selected=new CountryId(Text(row,"selected_country"));
            if(Scenarios.Any(s=>s.Id==id) || !Map.GetCountriesInRegion(deployment.Stages[0].Region).Any(c=>c.Id.Equals(selected)))
                throw new ArgumentException("Duplicate scenario or invalid initial deployment target.");
            var snapshot=RandomSnapshotJson.Deserialize(row["random_state"].ToString());
            var initial=InitialDealService.Create(cards,deal,new Pcg32Random(snapshot));
            var labels=Object(ContractJson.Parse(names),"schema_version dataset_id content_version locale description entries");
            if(Int(labels,"schema_version")!=1 || labels["entries"] is not JObject entries)throw new ArgumentException("Invalid card names.");
            var dictionary=Names.ToDictionary(p=>p.Key,p=>p.Value,StringComparer.Ordinal);
            foreach(var card in cards.Cards)dictionary[card.Id.Value]=Text(entries,card.NameKey);
            var countries=Map.Countries.ToDictionary(c=>c.Id,c=>new CountryState(0,0));
            var scenario=new DemoScenario(id,title,deployment.Stages[0].Side,selected,0,
                new ReadOnlyDictionary<CountryId,CountryState>(countries),null,turn:deal.Turn,deployment:deployment,initialCards:initial);
            var result=new DemoCatalog(Map,costs,Scenarios.Concat(new[]{scenario}),dictionary,cards);
            result.CreateHost(id);
            return result;
        }
    }
}
