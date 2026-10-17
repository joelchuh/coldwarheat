using System;
using System.Linq;
using ColdWarStruggle.Core;
namespace ColdWarStruggle.Infrastructure
{
    public sealed partial class DemoCatalog
    {
        public DemoCatalog WithPracticeNextTurn()
        {
            if(PrintedCards==null || Scenarios.Count(s=>s.PracticeDefinition?.TurnEndEnabled==true)!=1
                || Scenarios.Any(s=>s.PracticeDefinition?.NextTurnEnabled==true || s.StartupChoices!=null))
                throw new ArgumentException("Configure next turn once after turn end and before quick start.");
            var items=Scenarios.Select(s=>{
                if(s.PracticeDefinition==null)return s;var d=s.PracticeDefinition;
                var definition=new ActionPracticeDefinition(d.Cards,d.Ops,d.Space,d.Calendar,d.Risk,d.Scoring,d.InfluenceEvents,d.DefconVpEvents,d.TurnOpsEvents,d.Realignment,d.China,true,true,blockade:d.Blockade,fiveYearPlan:d.FiveYearPlan,wars:d.Wars,spaceEvent:d.SpaceEvent,olympic:d.Olympic,japanPact:d.JapanPact);
                return new DemoScenario(s.Id,s.Title,s.Actor,s.SelectedCountry,s.Ops,s.Countries,s.Hands,s.EventDefinition,s.Turn,s.DeploymentDefinition,s.InitialCards,definition,s.StartupChoices);
            });
            var result=new DemoCatalog(Map,costs,items,Names.ToDictionary(x=>x.Key,x=>x.Value,StringComparer.Ordinal),PrintedCards);
            result.CreateHost(result.Scenarios.Single(s=>s.PracticeDefinition!=null).Id);return result;
        }
    }
}
