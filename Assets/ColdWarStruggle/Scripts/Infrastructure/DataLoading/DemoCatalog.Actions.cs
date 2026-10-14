using System;
using System.Linq;
using ColdWarStruggle.Core;
namespace ColdWarStruggle.Infrastructure
{
    public sealed partial class DemoCatalog
    {
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
