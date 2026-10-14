using System;
using System.Collections.Generic;
using System.Linq;
using ColdWarStruggle.Core;
namespace ColdWarStruggle.Infrastructure
{
    // Trusted demo startup choices only; rule amounts/regions remain in SetupDeploymentDefinition.
    internal sealed class DemoDeploymentChoice
    {
        internal PlayerSide Side { get; }
        internal IReadOnlyList<InfluenceAllocation> Allocations { get; }
        internal DemoDeploymentChoice(PlayerSide side,IEnumerable<InfluenceAllocation> allocations)
        { Side=side;Allocations=System.Array.AsReadOnly(allocations.ToArray()); }
    }
    public sealed partial class DemoCatalog
    {
        public DemoCatalog WithQuickStartScenario(string fixture)
        {
            var data=Object(ContractJson.Parse(fixture),"schema_version dataset_id description scope scenario");
            if(Int(data,"schema_version")!=1 || Text(data,"dataset_id")!="demo.quick_start_scenario" || Text(data,"scope")!="fixture")throw new ArgumentException("Quick start must be an explicit fixture.");
            var row=Object(data["scenario"],"id title source_scenario_id selected_country deployments");
            var id=Text(row,"id");var sourceId=Text(row,"source_scenario_id");var selected=new CountryId(Text(row,"selected_country"));
            var source=Scenarios.SingleOrDefault(s=>s.Id==sourceId);
            if(Scenarios.Any(s=>s.Id==id || s.StartupChoices!=null) || !Map.Contains(selected) || source==null || source.StartupChoices!=null
                || source.InitialCards==null || source.DeploymentDefinition==null || source.PracticeDefinition?.Risk==null || source.PracticeDefinition.Scoring==null)
                throw new ArgumentException("Quick start requires one configured manual action lesson and a new scenario ID.");
            var rows=Array(row,"deployments");
            if(rows.Count!=source.DeploymentDefinition.Stages.Count)throw new ArgumentException("Every deployment stage must be specified once, in rule order.");
            var choices=new List<DemoDeploymentChoice>();
            foreach(var raw in rows)
            {
                var step=Object(raw,"side allocations");var side=Side(Text(step,"side"));var allocations=Array(step,"allocations");
                if(side!=source.DeploymentDefinition.Stages[choices.Count].Side || allocations.Count<1 || allocations.Count>128)throw new ArgumentException("Invalid deployment stage.");
                choices.Add(new DemoDeploymentChoice(side,allocations.Select(a=>
                {var item=Object(a,"country_id amount");return new InfluenceAllocation(new CountryId(Text(item,"country_id")),Int(item,"amount"));})));
            }
            var scenario=new DemoScenario(id,Text(row,"title"),source.Actor,selected,source.Ops,source.Countries,source.Hands,source.EventDefinition,
                source.Turn,source.DeploymentDefinition,source.InitialCards,source.PracticeDefinition,choices);
            var result=new DemoCatalog(Map,costs,Scenarios.Concat(new[]{scenario}),Names.ToDictionary(p=>p.Key,p=>p.Value,StringComparer.Ordinal),PrintedCards);
            // Validate using the authoritative flow before the catalog can escape to a caller.
            result.CreateHost(id);return result;
        }
        static GameState ApplyStartup(GameState state,IReadOnlyList<DemoDeploymentChoice> choices)
        {
            foreach(var choice in choices)
            {
                var plan=InitialDeploymentFlow.Resolve(state,choice.Side,state.Deployment.DecisionId,choice.Allocations);
                if(!plan.Accepted)throw new ArgumentException("Invalid quick-start deployment: "+plan.Error);
                state=plan.NextState;
            }
            var started=ActionPracticeRules.Plan(state,state.Practice.Actor,CardActionKind.StartPractice,"quick-start");
            if(!started.Accepted)throw new ArgumentException("Invalid quick-start practice: "+started.Error);
            return started.NextState;
        }
    }
}
