using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ColdWarStruggle.Core;
namespace ColdWarStruggle.Infrastructure
{
    public sealed partial class DemoCatalog
    {
        public DemoCatalog WithIndependentReds(string parameters,string schema,string cardReferences)
        {
            if(PrintedCards==null || !Scenarios.Any(s=>s.PracticeDefinition!=null))throw new ArgumentException("Configure playable events before matching influence.");
            var effect=IndependentRedsCatalogLoader.Load(parameters,schema,Map,CardEffectReferenceIndex.Load(cardReferences));
            var definitions=new Dictionary<ActionPracticeDefinition,ActionPracticeDefinition>();
            var scenes=Scenarios.Select(s=>
            {
                var d=s.PracticeDefinition;if(d==null)return s;
                if(d.InfluenceEvents==null || d.InfluenceEvents.Effects.ContainsKey(new CardId(effect.CardId)))throw new ArgumentException("Matching effect requires an existing catalog without duplicate registration.");
                if(!definitions.TryGetValue(d,out var replacement))
                {
                    var effects=new PracticeInfluenceCatalog(Map,PrintedCards,d.InfluenceEvents.Effects.Values.Append(effect));
                    replacement=new ActionPracticeDefinition(d.Cards,d.Ops,d.Space,d.Calendar,d.Risk,d.Scoring,effects,d.DefconVpEvents,d.TurnOpsEvents,d.Realignment,d.China,
                        d.TurnEndEnabled,d.NextTurnEnabled,d.HeadlineParameters,d.HeadlineDefectors,d.FinalScoring,blockade:d.Blockade,fiveYearPlan:d.FiveYearPlan,wars:d.Wars,spaceEvent:d.SpaceEvent,olympic:d.Olympic,japanPact:d.JapanPact);
                    definitions.Add(d,replacement);
                }
                return new DemoScenario(s.Id,s.Title,s.Actor,s.SelectedCountry,s.Ops,s.Countries,s.Hands,s.EventDefinition,s.Turn,s.DeploymentDefinition,s.InitialCards,replacement,s.StartupChoices,s.TurnEndSetup);
            }).ToArray();
            return new DemoCatalog(Map,costs,scenes,Names.ToDictionary(x=>x.Key,x=>x.Value,StringComparer.Ordinal),PrintedCards);
        }
        public DemoCatalog WithIndependentRedsScenario(string fixture)
        {
            var data=Object(ContractJson.Parse(fixture),"schema_version dataset_id description scope scenario");
            if(Int(data,"schema_version")!=1 || Text(data,"dataset_id")!="demo.independent_reds_scenario" || Text(data,"scope")!="fixture")throw new ArgumentException("Matching demo must be an explicit fixture.");
            var row=Object(data["scenario"],"id title source_scenario_id turn_number selected_country event_card other_card influence");
            var id=Text(row,"id");var title=Text(row,"title");
            if(id.Length>128 || id!=id.Trim() || title.Length>160 || Scenarios.Any(s=>s.Id==id))throw new ArgumentException("Invalid or repeated matching fixture identity.");
            var source=Scenarios.SingleOrDefault(s=>s.Id==Text(row,"source_scenario_id"));var d=source?.PracticeDefinition;var setup=source?.TurnEndSetup;
            var eventCard=new CardId(Text(row,"event_card"));var otherCard=new CardId(Text(row,"other_card"));
            if(setup==null || d?.FinalScoring==null || d.HeadlineParameters!=null || !d.NextTurnEnabled || d.InfluenceEvents==null
                || !d.InfluenceEvents.Effects.TryGetValue(eventCard,out var rawEffect) || rawEffect is not MatchOpponentInfluenceEffect effect)
                throw new ArgumentException("Expected a configured matching event and final-scoring source.");
            var number=Int(row,"turn_number");if(number<2 || number>d.Calendar.LastTurn)throw new ArgumentException("Invalid matching fixture turn.");
            var turn=d.Calendar.ForTurn(number);var selected=new CountryId(Text(row,"selected_country"));
            if(!effect.Countries.Contains(selected) || eventCard.Equals(otherCard) || !d.Cards.Contains(otherCard))throw new ArgumentException("Invalid matching fixture selection or cards.");
            var printed=d.Cards.GetCard(otherCard);
            if(d.Cards.GetCard(eventCard).Era>turn.Era || printed.Era>turn.Era || printed.Kind!=CardKind.Event || printed.Affiliation==CardAffiliation.USSR)
                throw new ArgumentException("Fixture cards must be ordinary cards available in this era.");
            var countries=source.Countries.ToDictionary(x=>x.Key,x=>x.Value);var seen=new HashSet<CountryId>();var rows=Array(row,"influence");
            if(rows.Count!=effect.Countries.Count)throw new ArgumentException("Matching lesson must describe all candidate countries.");
            foreach(var item in rows)
            {
                var values=Object(item,"country_id us ussr");var country=new CountryId(Text(values,"country_id"));var us=Int(values,"us");var su=Int(values,"ussr");
                if(!effect.Countries.Contains(country) || !seen.Add(country) || us<0 || su<0)throw new ArgumentException("Invalid matching influence row.");
                countries[country]=new CountryState(us,su);
            }
            var used=new HashSet<CardId>{eventCard,otherCard};var hands=new Dictionary<PlayerSide,IReadOnlyList<CardId>>{{PlayerSide.USSR,new[]{eventCard}},{PlayerSide.US,new[]{otherCard}}};
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
