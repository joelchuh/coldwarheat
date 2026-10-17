using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ColdWarStruggle.Core;
using Newtonsoft.Json.Linq;
namespace ColdWarStruggle.Infrastructure
{
    public sealed partial class DemoCatalog
    {
        public DemoCatalog WithInfluenceRelocation(string parameters,string schema,string cardReferences)
        {
            if(PrintedCards==null || !Scenarios.Any(s=>s.PracticeDefinition!=null))throw new ArgumentException("Configure playable events before influence relocation.");
            var effect=InfluenceRelocationDefinitionLoader.Load(parameters,schema,CardEffectReferenceIndex.Load(cardReferences));
            var definitions=new Dictionary<ActionPracticeDefinition,ActionPracticeDefinition>();
            var scenes=Scenarios.Select(s=>
            {
                var d=s.PracticeDefinition;if(d==null)return s;
                if(d.InfluenceEvents==null || d.InfluenceEvents.Effects.ContainsKey(new CardId(effect.CardId)))throw new ArgumentException("Relocation effect requires an existing catalog without duplicate registration.");
                if(!definitions.TryGetValue(d,out var replacement))
                {
                    var effects=new PracticeInfluenceCatalog(Map,PrintedCards,d.InfluenceEvents.Effects.Values.Append(effect));
                    replacement=new ActionPracticeDefinition(d.Cards,d.Ops,d.Space,d.Calendar,d.Risk,d.Scoring,effects,d.DefconVpEvents,d.TurnOpsEvents,d.Realignment,d.China,
                        d.TurnEndEnabled,d.NextTurnEnabled,d.HeadlineParameters,d.HeadlineDefectors,d.FinalScoring,blockade:d.Blockade,fiveYearPlan:d.FiveYearPlan,wars:d.Wars,spaceEvent:d.SpaceEvent,olympic:d.Olympic);
                    definitions.Add(d,replacement);
                }
                return new DemoScenario(s.Id,s.Title,s.Actor,s.SelectedCountry,s.Ops,s.Countries,s.Hands,s.EventDefinition,s.Turn,s.DeploymentDefinition,s.InitialCards,replacement,s.StartupChoices,s.TurnEndSetup);
            }).ToArray();
            return new DemoCatalog(Map,costs,scenes,Names.ToDictionary(x=>x.Key,x=>x.Value,StringComparer.Ordinal),PrintedCards);
        }
        public DemoCatalog WithRelocationScenarios(string fixture)
        {
            var data=Object(ContractJson.Parse(fixture),"schema_version dataset_id description scope source_scenario_id turn_number initial_vp random_seed random_stream scenarios");
            if(Int(data,"schema_version")!=1 || Text(data,"dataset_id")!="demo.relocation_scenarios" || Text(data,"scope")!="fixture")throw new ArgumentException("Relocation lessons must be explicit fixtures.");
            Text(data,"description");var source=Scenarios.SingleOrDefault(s=>s.Id==Text(data,"source_scenario_id"));var d=source?.PracticeDefinition;var setup=source?.TurnEndSetup;
            if(setup==null || d?.InfluenceEvents==null || !d.InfluenceEvents.Effects.ContainsKey(new CardId("card.de_stalinization")) || d.InfluenceEvents.Effects[new CardId("card.de_stalinization")] is not RelocateOwnInfluenceEffect || d.FinalScoring==null || d.HeadlineParameters!=null || !d.NextTurnEnabled)throw new ArgumentException("Expected final-scoring source with Relocation event and no headline preparation.");
            var number=Int(data,"turn_number");var vp=Int(data,"initial_vp");var seed=Int(data,"random_seed");var stream=Int(data,"random_stream");
            if(number!=2 || vp<=-(long)d.Risk.Victory.AutomaticVpThreshold || vp>=d.Risk.Victory.AutomaticVpThreshold || seed<0 || stream<0)throw new ArgumentException("Invalid lesson turn, VP or seed.");
            var turn=d.Calendar.ForTurn(number);var random=Pcg32Random.FromSeed((ulong)seed,(ulong)stream).Capture();
            var rows=Array(data,"scenarios");if(rows.Count!=2)throw new ArgumentException("Expected two bounded Relocation lessons.");
            var ids=new HashSet<string>(Scenarios.Select(s=>s.Id),StringComparer.Ordinal);var scenes=new List<DemoScenario>();
            foreach(var token in rows)
            {
                var row=Object(token,"id title actor card selected_country ordinary_actions_to_prepare us_hand ussr_hand influence");
                var id=Text(row,"id");var title=Text(row,"title");var actor=Side(Text(row,"actor"));var card=new CardId(Text(row,"card"));var selected=new CountryId(Text(row,"selected_country"));
                var prepared=Int(row,"ordinary_actions_to_prepare");
                if(id.Length>128 || id!=id.Trim() || !ids.Add(id) || title.Length>160 || !card.Equals(new CardId("card.de_stalinization")) || !Map.Contains(selected)
                    || prepared!=(actor==PlayerSide.US?1:0))throw new ArgumentException("Invalid lesson identity, card, actor preparation, target.");
                CardId[] Hand(string key)=>Array(row,key).Select(t=>new CardId(t.Type==JTokenType.String?(string)t:throw new ArgumentException("Expected card ID."))).ToArray();
                var us=Hand("us_hand");var su=Hand("ussr_hand");var used=new HashSet<CardId>(us.Concat(su));
                if(us.Length==0 || su.Length==0 || us.Length>turn.Parameters.HandTarget || su.Length>turn.Parameters.HandTarget
                    || !(actor==PlayerSide.US?us:su).Contains(card) || used.Count!=us.Length+su.Length
                    || used.Any(c=>!d.Cards.Contains(c) || d.Cards.GetCard(c).Kind==CardKind.China || d.Cards.GetCard(c).Era>turn.Era))throw new ArgumentException("Invalid, duplicate or unavailable lesson hand.");
                var board=Map.Countries.ToDictionary(c=>c.Id,c=>new CountryState(0,0));var countries=new HashSet<CountryId>();
                foreach(var influence in Array(row,"influence"))
                {
                    var value=Object(influence,"country_id us ussr");var country=new CountryId(Text(value,"country_id"));var a=Int(value,"us");var b=Int(value,"ussr");
                    if(!Map.Contains(country) || !countries.Add(country) || a<0 || b<0 || (long)a+b>int.MaxValue)throw new ArgumentException("Invalid or repeated lesson influence.");
                    board[country]=new CountryState(a,b);
                }
                var hands=new Dictionary<PlayerSide,IReadOnlyList<CardId>>{{PlayerSide.US,us},{PlayerSide.USSR,su}};
                var cards=new CardZones(d.Cards,d.Cards.Cards.Where(c=>c.Kind!=CardKind.China && c.Era<=turn.Era && !used.Contains(c.Id)).Select(c=>c.Id),hands,
                    d.Cards.Cards.Where(c=>c.Kind!=CardKind.China && c.Era>turn.Era).Select(c=>c.Id),System.Array.Empty<CardId>(),System.Array.Empty<CardId>(),System.Array.Empty<CardId>(),setup.Cards.China,
                    new[]{GameEra.Early,GameEra.Mid,GameEra.Late}.Where(e=>e<=turn.Era));
                var fresh=new DemoTurnEndSetup(number,vp,cards,random,ordinaryActionsToPrepare:prepared);
                scenes.Add(new DemoScenario(id,title,actor,selected,0,new ReadOnlyDictionary<CountryId,CountryState>(board),null,turn:turn,practice:d,turnEndSetup:fresh));
            }
            var result=new DemoCatalog(Map,costs,Scenarios.Concat(scenes),Names.ToDictionary(x=>x.Key,x=>x.Value,StringComparer.Ordinal),PrintedCards);
            foreach(var scene in scenes)
            {
                var host=result.CreateHost(scene.Id);var view=host.UssrSession.ReadView();
                if(view.Practice.Actor!=scene.Actor || view.Event!=null || view.Practice.CompletedActions!=scene.TurnEndSetup.OrdinaryActionsToPrepare)throw new ArgumentException("Unsafe lesson preparation.");
            }
            return result;
        }
    }
}
