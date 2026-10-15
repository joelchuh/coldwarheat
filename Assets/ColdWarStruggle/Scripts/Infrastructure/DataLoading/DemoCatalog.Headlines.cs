using System;
using System.Linq;
using ColdWarStruggle.Core;
namespace ColdWarStruggle.Infrastructure
{
    public sealed partial class DemoCatalog
    {
        public DemoCatalog WithHeadlineScenario(string fixture,string turnFlow,string defectors,string defectorsSchema)
        {
            var data=Object(ContractJson.Parse(fixture),"schema_version dataset_id description scope scenario");
            if(Int(data,"schema_version")!=1 || Text(data,"dataset_id")!="demo.headline_scenario" || Text(data,"scope")!="fixture")throw new ArgumentException("Headline demo must be an explicit fixture.");
            var row=Object(data["scenario"],"id title source_scenario_id");var id=Text(row,"id");var sourceId=Text(row,"source_scenario_id");var title=Text(row,"title");
            var source=Scenarios.SingleOrDefault(s=>s.Id==sourceId);var d=source?.PracticeDefinition;
            if(PrintedCards==null || Scenarios.Any(s=>s.Id==id || s.PracticeDefinition?.HeadlineParameters!=null)
                || source?.StartupChoices==null || source.InitialCards==null || source.DeploymentDefinition==null || d==null
                || !d.TurnEndEnabled || !d.NextTurnEnabled || !ReferenceEquals(d.Cards,PrintedCards))
                throw new ArgumentException("Headline demo requires a configured quick start and a new scenario ID.");
            var parameters=HeadlineParametersLoader.Load(turnFlow);var special=DefectorsHeadlineLoader.Load(defectors,defectorsSchema,d.Cards);
            var definition=new ActionPracticeDefinition(d.Cards,d.Ops,d.Space,d.Calendar,d.Risk,d.Scoring,d.InfluenceEvents,d.DefconVpEvents,d.TurnOpsEvents,d.Realignment,d.China,d.TurnEndEnabled,d.NextTurnEnabled,parameters,special);
            var scene=new DemoScenario(id,title,source.Actor,source.SelectedCountry,source.Ops,source.Countries,source.Hands,source.EventDefinition,
                source.Turn,source.DeploymentDefinition,source.InitialCards,definition,source.StartupChoices);
            var result=new DemoCatalog(Map,costs,Scenarios.Concat(new[]{scene}),Names.ToDictionary(x=>x.Key,x=>x.Value,StringComparer.Ordinal),PrintedCards);
            if(result.CreateHost(id).UsSession.ReadView().Practice.Stage!=ActionPracticeStage.CollectingHeadline)
                throw new ArgumentException("Headline scene must enter collection after setup.");
            return result;
        }
    }
}
