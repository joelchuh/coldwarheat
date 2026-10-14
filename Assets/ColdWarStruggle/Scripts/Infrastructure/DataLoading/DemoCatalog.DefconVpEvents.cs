using System;
using System.Linq;
using ColdWarStruggle.Core;
namespace ColdWarStruggle.Infrastructure
{
    public sealed partial class DemoCatalog
    {
        public DemoCatalog WithPracticeDefconVpEvents(string parameters,string schema)
        {
            if(PrintedCards==null || Scenarios.Count(s=>s.PracticeDefinition?.InfluenceEvents!=null)!=1
                || Scenarios.Any(s=>s.PracticeDefinition?.DefconVpEvents!=null || s.StartupChoices!=null))throw new ArgumentException("Configure the manual influence-event lesson once before quick start.");
            var effects=new PracticeDefconVpCatalog(PrintedCards,DefconVpEventLoader.LoadImmediateSubset(parameters,schema));
            var items=Scenarios.Select(s=> {
                if(s.PracticeDefinition==null)return s;
                var d=s.PracticeDefinition;var definition=new ActionPracticeDefinition(d.Cards,d.Ops,d.Space,d.Calendar,d.Risk,d.Scoring,d.InfluenceEvents,effects);
                return new DemoScenario(s.Id,s.Title,s.Actor,s.SelectedCountry,s.Ops,s.Countries,s.Hands,s.EventDefinition,s.Turn,s.DeploymentDefinition,s.InitialCards,definition,s.StartupChoices);
            });
            var result=new DemoCatalog(Map,costs,items,Names.ToDictionary(x=>x.Key,x=>x.Value,StringComparer.Ordinal),PrintedCards);
            result.CreateHost(result.Scenarios.Single(s=>s.PracticeDefinition!=null).Id);return result;
        }
    }
}
