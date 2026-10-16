using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ColdWarStruggle.Core;
namespace ColdWarStruggle.Infrastructure
{
    public sealed partial class DemoCatalog
    {
        public DemoCatalog WithBlockade(string parameters,string schema,string cardReferences)
        {
            if(PrintedCards==null || !Scenarios.Any(s=>s.PracticeDefinition!=null))throw new ArgumentException("Configure playable events before blockade.");
            var known=CardEffectReferenceIndex.Load(cardReferences);var replacements=new Dictionary<ActionPracticeDefinition,ActionPracticeDefinition>();
            var scenes=Scenarios.Select(s=>
            {
                var d=s.PracticeDefinition;if(d==null)return s;if(d.Blockade!=null)throw new ArgumentException("Blockade already registered.");
                if(!replacements.TryGetValue(d,out var replacement))
                {
                    var blockade=BlockadeDefinitionLoader.Load(parameters,schema,Map,d.Cards,d.Ops,known);
                    replacement=new ActionPracticeDefinition(d.Cards,d.Ops,d.Space,d.Calendar,d.Risk,d.Scoring,d.InfluenceEvents,d.DefconVpEvents,d.TurnOpsEvents,d.Realignment,d.China,
                        d.TurnEndEnabled,d.NextTurnEnabled,d.HeadlineParameters,d.HeadlineDefectors,d.FinalScoring,blockade);
                    replacements.Add(d,replacement);
                }
                return new DemoScenario(s.Id,s.Title,s.Actor,s.SelectedCountry,s.Ops,s.Countries,s.Hands,s.EventDefinition,s.Turn,s.DeploymentDefinition,s.InitialCards,replacement,s.StartupChoices,s.TurnEndSetup);
            }).ToArray();
            return new DemoCatalog(Map,costs,scenes,Names.ToDictionary(x=>x.Key,x=>x.Value,StringComparer.Ordinal),PrintedCards);
        }
        public DemoCatalog WithBlockadeScenario(string fixture)
        {
            var data=Object(ContractJson.Parse(fixture),"schema_version dataset_id description scope scenario");
            if(Int(data,"schema_version")!=1 || Text(data,"dataset_id")!="demo.blockade_scenario" || Text(data,"scope")!="fixture")throw new ArgumentException("Blockade demo must be an explicit fixture.");
            var row=Object(data["scenario"],"id title source_scenario_id turn_number event_card us_hand fallback_influence");
            var id=Text(row,"id");var title=Text(row,"title");
            if(id.Length>128 || id!=id.Trim() || title.Length>160 || Scenarios.Any(s=>s.Id==id))throw new ArgumentException("Invalid or repeated blockade fixture identity.");
            var source=Scenarios.SingleOrDefault(s=>s.Id==Text(row,"source_scenario_id"));var d=source?.PracticeDefinition;var setup=source?.TurnEndSetup;
            var eventCard=new CardId(Text(row,"event_card"));
            if(setup==null || d?.Blockade==null || d.FinalScoring==null || d.HeadlineParameters!=null || !d.NextTurnEnabled || !eventCard.Equals(d.Blockade.Card.Id))
                throw new ArgumentException("Expected a configured blockade and final-scoring source.");
            var number=Int(row,"turn_number");if(number<2 || number>d.Calendar.LastTurn)throw new ArgumentException("Invalid blockade fixture turn.");
            var turn=d.Calendar.ForTurn(number);var us=Array(row,"us_hand").Select(v=>new CardId(v.Type==Newtonsoft.Json.Linq.JTokenType.String?(string)v:throw new ArgumentException("Expected a card ID string."))).ToArray();var used=new HashSet<CardId>(us);
            if(us.Length==0 || used.Count!=us.Length || !used.Add(eventCard) || used.Any(c=>!d.Cards.Contains(c) || d.Cards.GetCard(c).Kind==CardKind.China || d.Cards.GetCard(c).Era>turn.Era))
                throw new ArgumentException("Invalid blockade fixture hand.");
            var influence=Object(row["fallback_influence"],"us ussr");var usInfluence=Int(influence,"us");var suInfluence=Int(influence,"ussr");
            if(usInfluence<0 || suInfluence<0)throw new ArgumentException("Negative fixture influence.");
            var countries=source.Countries.ToDictionary(x=>x.Key,x=>x.Value);countries[d.Blockade.FallbackCountry]=new CountryState(usInfluence,suInfluence);
            var hands=new Dictionary<PlayerSide,IReadOnlyList<CardId>>{{PlayerSide.USSR,new[]{eventCard}},{PlayerSide.US,us}};
            var cards=new CardZones(d.Cards,d.Cards.Cards.Where(c=>c.Kind!=CardKind.China && c.Era<=turn.Era && !used.Contains(c.Id)).Select(c=>c.Id),hands,
                d.Cards.Cards.Where(c=>c.Kind!=CardKind.China && c.Era>turn.Era).Select(c=>c.Id),System.Array.Empty<CardId>(),System.Array.Empty<CardId>(),System.Array.Empty<CardId>(),setup.Cards.China,
                new[]{GameEra.Early,GameEra.Mid,GameEra.Late}.Where(e=>e<=turn.Era));
            if(HandDiscardRules.QualifyingCards(cards,d.Blockade.Owner,d.Ops,d.Blockade.MinimumEffectiveOps,System.Array.Empty<OpsFlatModifier>()).Count==0)
                throw new ArgumentException("This lesson must offer a qualifying discard choice.");
            var fresh=new DemoTurnEndSetup(number,setup.InitialVp,cards,setup.Random,ordinaryActionsToPrepare:0);
            var scene=new DemoScenario(id,title,PlayerSide.USSR,d.Blockade.FallbackCountry,0,new ReadOnlyDictionary<CountryId,CountryState>(countries),null,turn:turn,practice:d,turnEndSetup:fresh);
            var result=new DemoCatalog(Map,costs,Scenarios.Concat(new[]{scene}),Names.ToDictionary(x=>x.Key,x=>x.Value,StringComparer.Ordinal),PrintedCards);
            result.CreateHost(id);return result;
        }
    }
}
