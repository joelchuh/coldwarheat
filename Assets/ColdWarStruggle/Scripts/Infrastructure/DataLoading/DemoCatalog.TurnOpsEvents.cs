using System;
using System.Linq;
using ColdWarStruggle.Core;
namespace ColdWarStruggle.Infrastructure
{
    public sealed partial class DemoCatalog
    {
        public DemoCatalog WithPracticeTurnOpsEvents(string parameters,string schema)
        {
            if(PrintedCards==null || Scenarios.Count(s=>s.PracticeDefinition?.DefconVpEvents!=null)!=1
                || Scenarios.Any(s=>s.PracticeDefinition?.TurnOpsEvents!=null || s.StartupChoices!=null))throw new ArgumentException("Configure manual DEFCON events once before quick start.");
            var effects=new PracticeTurnOpsCatalog(PrintedCards,TurnOpsEffectLoader.LoadFlatSubset(parameters,schema));
            var items=Scenarios.Select(s=> {
                if(s.PracticeDefinition==null)return s;
                var d=s.PracticeDefinition;var definition=new ActionPracticeDefinition(d.Cards,d.Ops,d.Space,d.Calendar,d.Risk,d.Scoring,d.InfluenceEvents,d.DefconVpEvents,effects,d.Realignment,blockade:d.Blockade);
                return new DemoScenario(s.Id,s.Title,s.Actor,s.SelectedCountry,s.Ops,s.Countries,s.Hands,s.EventDefinition,s.Turn,s.DeploymentDefinition,s.InitialCards,definition,s.StartupChoices);
            });
            var result=new DemoCatalog(Map,costs,items,Names.ToDictionary(x=>x.Key,x=>x.Value,StringComparer.Ordinal),PrintedCards);
            result.CreateHost(result.Scenarios.Single(s=>s.PracticeDefinition!=null).Id);return result;
        }
    }
}
