using System;
using System.Linq;
using ColdWarStruggle.Core;
namespace ColdWarStruggle.Infrastructure
{
    public sealed partial class DemoCatalog
    {
        public DemoCatalog WithPracticeInfluenceEvents(string first,string firstSchema,string second,string secondSchema,string cards)
        {
            if(PrintedCards==null || Scenarios.Count(s=>s.PracticeDefinition?.Scoring!=null)!=1 || Scenarios.Any(s=>s.PracticeDefinition?.InfluenceEvents!=null || s.StartupChoices!=null))
                throw new ArgumentException("Configure the manual scoring lesson before events, then append quick start.");
            var references=CardEffectReferenceIndex.Load(cards);
            var definitions=EarlyInfluenceCatalogLoader.Load(first,firstSchema,Map).Values
                .Concat(BudgetedRemovalCatalogLoader.Load(second,secondSchema,Map,references).Values)
                .Append(EraRemovalCatalogLoader.Load(second,secondSchema,Map,references));
            var effects=new PracticeInfluenceCatalog(Map,PrintedCards,definitions);
            var items=Scenarios.Select(s=>
            {
                if(s.PracticeDefinition==null)return s;
                var d=s.PracticeDefinition;var definition=new ActionPracticeDefinition(d.Cards,d.Ops,d.Space,d.Calendar,d.Risk,d.Scoring,effects,d.DefconVpEvents,d.TurnOpsEvents,d.Realignment,blockade:d.Blockade);
                return new DemoScenario(s.Id,s.Title,s.Actor,s.SelectedCountry,s.Ops,s.Countries,s.Hands,s.EventDefinition,s.Turn,s.DeploymentDefinition,s.InitialCards,definition,s.StartupChoices);
            });
            var result=new DemoCatalog(Map,costs,items,Names.ToDictionary(x=>x.Key,x=>x.Value,StringComparer.Ordinal),PrintedCards);
            result.CreateHost(result.Scenarios.Single(s=>s.PracticeDefinition!=null).Id);return result;
        }
    }
}
