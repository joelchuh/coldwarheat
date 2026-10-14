using System;
using System.Linq;
using ColdWarStruggle.Core;
namespace ColdWarStruggle.Infrastructure
{
    public sealed partial class DemoCatalog
    {
        public DemoCatalog WithPracticeScoring(string scoring,string schema)
        {
            if(Scenarios.Count(s=>s.PracticeDefinition?.Risk!=null)!=1 || Scenarios.Any(s=>s.PracticeDefinition?.Scoring!=null))throw new ArgumentException("Configure the risk action lesson once before adding scoring.");
            var scoringDefinition=RegionScoringDefinitionLoader.LoadSubset(scoring,schema,Map);
            var items=Scenarios.Select(s=>
            {
                if(s.PracticeDefinition==null)return s;
                var d=s.PracticeDefinition;var definition=new ActionPracticeDefinition(d.Cards,d.Ops,d.Space,d.Calendar,d.Risk,scoringDefinition,d.InfluenceEvents,d.DefconVpEvents,d.TurnOpsEvents);
                return new DemoScenario(s.Id,s.Title,s.Actor,s.SelectedCountry,s.Ops,s.Countries,s.Hands,s.EventDefinition,s.Turn,s.DeploymentDefinition,s.InitialCards,definition,s.StartupChoices);
            });
            var result=new DemoCatalog(Map,costs,items,Names.ToDictionary(x=>x.Key,x=>x.Value,StringComparer.Ordinal),PrintedCards);
            result.CreateHost(result.Scenarios.Single(s=>s.PracticeDefinition!=null).Id);return result;
        }
        public DemoCatalog WithPracticeRisk(string coup,string defcon,string military,string victory)
        {
            if(Scenarios.Count(s=>s.PracticeDefinition!=null)!=1 || Scenarios.Any(s=>s.PracticeDefinition?.Risk!=null))throw new ArgumentException("Configure the real action lesson once before adding risk rules.");
            var risk=new PracticeRiskDefinition(CoupDefinitionLoader.LoadSubset(coup),DefconDefinitionLoader.LoadSubset(defcon,Map),MilitaryOperationsLoader.LoadSubset(military),VictoryDefinitionLoader.LoadSubset(victory));
            var items=Scenarios.Select(s=>
            {
                if(s.PracticeDefinition==null)return s;
                var d=s.PracticeDefinition;var definition=new ActionPracticeDefinition(d.Cards,d.Ops,d.Space,d.Calendar,risk,d.Scoring,d.InfluenceEvents,d.DefconVpEvents,d.TurnOpsEvents);
                return new DemoScenario(s.Id,s.Title,s.Actor,s.SelectedCountry,s.Ops,s.Countries,s.Hands,s.EventDefinition,s.Turn,s.DeploymentDefinition,s.InitialCards,definition,s.StartupChoices);
            });
            var result=new DemoCatalog(Map,costs,items,Names.ToDictionary(x=>x.Key,x=>x.Value,StringComparer.Ordinal),PrintedCards);
            result.CreateHost(result.Scenarios.Single(s=>s.PracticeDefinition!=null).Id);return result;
        }
        public DemoCatalog WithActionPractice(string ops,string opsSchema,string space,string calendar)
        {
            if(PrintedCards==null || Scenarios.Count(s=>s.InitialCards!=null)!=1 || Scenarios.Any(s=>s.PracticeDefinition!=null))throw new ArgumentException("Exactly one unconfigured real-deal lesson is required.");
            var definition=new ActionPracticeDefinition(PrintedCards,OpsGeneralDefinitionLoader.LoadSubset(ops,opsSchema),SpaceRaceDefinitionLoader.LoadSubset(space),TurnCalendarLoader.Load(calendar));
            var items=Scenarios.Select(s=>s.InitialCards==null?s:new DemoScenario(s.Id,s.Title,s.Actor,s.SelectedCountry,s.Ops,s.Countries,s.Hands,s.EventDefinition,s.Turn,s.DeploymentDefinition,s.InitialCards,definition));
            var result=new DemoCatalog(Map,costs,items,Names.ToDictionary(x=>x.Key,x=>x.Value,StringComparer.Ordinal),PrintedCards);
            result.CreateHost(result.Scenarios.Single(s=>s.PracticeDefinition!=null).Id);return result;
        }
    }
}
