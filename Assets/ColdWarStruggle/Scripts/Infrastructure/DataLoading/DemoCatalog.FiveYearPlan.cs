using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ColdWarStruggle.Core;
namespace ColdWarStruggle.Infrastructure
{
    public sealed partial class DemoCatalog
    {
        public DemoCatalog WithFiveYearPlan(string parameters,string schema,string cardReferences)
        {
            if(PrintedCards==null || !Scenarios.Any(s=>s.PracticeDefinition!=null))throw new ArgumentException("Configure playable events before Five Year Plan.");
            var known=CardEffectReferenceIndex.Load(cardReferences);var replacements=new Dictionary<ActionPracticeDefinition,ActionPracticeDefinition>();
            var scenes=Scenarios.Select(s=>
            {
                var d=s.PracticeDefinition;if(d==null)return s;if(d.FiveYearPlan!=null)throw new ArgumentException("Five Year Plan already registered.");
                if(!replacements.TryGetValue(d,out var replacement))
                {
                    var definition=FiveYearPlanDefinitionLoader.Load(parameters,schema,d.Cards,known);
                    replacement=new ActionPracticeDefinition(d.Cards,d.Ops,d.Space,d.Calendar,d.Risk,d.Scoring,d.InfluenceEvents,d.DefconVpEvents,d.TurnOpsEvents,d.Realignment,d.China,
                        d.TurnEndEnabled,d.NextTurnEnabled,d.HeadlineParameters,d.HeadlineDefectors,d.FinalScoring,d.Blockade,definition);
                    replacements.Add(d,replacement);
                }
                return new DemoScenario(s.Id,s.Title,s.Actor,s.SelectedCountry,s.Ops,s.Countries,s.Hands,s.EventDefinition,s.Turn,s.DeploymentDefinition,s.InitialCards,replacement,s.StartupChoices,s.TurnEndSetup);
            }).ToArray();
            return new DemoCatalog(Map,costs,scenes,Names.ToDictionary(x=>x.Key,x=>x.Value,StringComparer.Ordinal),PrintedCards);
        }
        public DemoCatalog WithFiveYearPlanScenario(string fixture)
        {
            var data=Object(ContractJson.Parse(fixture),"schema_version dataset_id description scope scenario");
            if(Int(data,"schema_version")!=1 || Text(data,"dataset_id")!="demo.five_year_plan_scenario" || Text(data,"scope")!="fixture")throw new ArgumentException("Five Year Plan lesson must be an explicit fixture.");
            var row=Object(data["scenario"],"id title source_scenario_id turn_number event_card child_card selected_country us_hand influence");
            var id=Text(row,"id");var title=Text(row,"title");
            if(id.Length>128 || id!=id.Trim() || title.Length>160 || Scenarios.Any(s=>s.Id==id))throw new ArgumentException("Invalid or repeated lesson identity.");
            var source=Scenarios.SingleOrDefault(s=>s.Id==Text(row,"source_scenario_id"));var d=source?.PracticeDefinition;var setup=source?.TurnEndSetup;
            var root=new CardId(Text(row,"event_card"));var child=new CardId(Text(row,"child_card"));
            if(setup==null || d?.FiveYearPlan==null || d.FinalScoring==null || d.HeadlineParameters!=null || !d.NextTurnEnabled || !root.Equals(d.FiveYearPlan.Card.Id)
                || d.InfluenceEvents==null || !d.InfluenceEvents.Effects.TryGetValue(child,out var rawEffect) || rawEffect is not MatchOpponentInfluenceEffect effect)
                throw new ArgumentException("Expected a configured parent and matching US child, with final-scoring source.");
            var number=Int(row,"turn_number");if(number<2 || number>d.Calendar.LastTurn)throw new ArgumentException("Invalid lesson turn.");
            var turn=d.Calendar.ForTurn(number);var selected=new CountryId(Text(row,"selected_country"));
            var us=Array(row,"us_hand").Select(v=>new CardId(v.Type==Newtonsoft.Json.Linq.JTokenType.String?(string)v:throw new ArgumentException("Expected a card ID string."))).ToArray();
            var used=new HashSet<CardId>(us);
            if(us.Length==0 || used.Count!=us.Length || !used.Add(root) || !used.Add(child) || !effect.Countries.Contains(selected)
                || used.Any(c=>!d.Cards.Contains(c) || d.Cards.GetCard(c).Kind==CardKind.China || d.Cards.GetCard(c).Era>turn.Era) || d.Cards.GetCard(child).Affiliation!=CardAffiliation.US)
                throw new ArgumentException("Invalid lesson hands or child target.");
            var influence=Object(row["influence"],"us ussr");var a=Int(influence,"us");var b=Int(influence,"ussr");
            if(a<0 || b<=a)throw new ArgumentException("Lesson must offer a visible matching change.");
            var countries=source.Countries.ToDictionary(x=>x.Key,x=>x.Value);countries[selected]=new CountryState(a,b);
            var hands=new Dictionary<PlayerSide,IReadOnlyList<CardId>>{{PlayerSide.USSR,new[]{root,child}},{PlayerSide.US,us}};
            var cards=new CardZones(d.Cards,d.Cards.Cards.Where(c=>c.Kind!=CardKind.China && c.Era<=turn.Era && !used.Contains(c.Id)).Select(c=>c.Id),hands,
                d.Cards.Cards.Where(c=>c.Kind!=CardKind.China && c.Era>turn.Era).Select(c=>c.Id),System.Array.Empty<CardId>(),System.Array.Empty<CardId>(),System.Array.Empty<CardId>(),setup.Cards.China,
                new[]{GameEra.Early,GameEra.Mid,GameEra.Late}.Where(e=>e<=turn.Era));
            var fresh=new DemoTurnEndSetup(number,setup.InitialVp,cards,setup.Random,ordinaryActionsToPrepare:0);
            var scene=new DemoScenario(id,title,PlayerSide.USSR,selected,0,new ReadOnlyDictionary<CountryId,CountryState>(countries),null,turn:turn,practice:d,turnEndSetup:fresh);
            var result=new DemoCatalog(Map,costs,Scenarios.Concat(new[]{scene}),Names.ToDictionary(x=>x.Key,x=>x.Value,StringComparer.Ordinal),PrintedCards);
            result.CreateHost(id);return result;
        }
    }
}
