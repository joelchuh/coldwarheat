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
        public DemoCatalog WithVietnamRevolts(string parameters,string schema)
        {
            if(PrintedCards==null || !Scenarios.Any(s=>s.PracticeDefinition!=null))throw new ArgumentException("Configure playable events before Vietnam.");
            var effect=VietnamRevoltsDefinitionLoader.Load(parameters,schema,Map,PrintedCards);
            var definitions=new Dictionary<ActionPracticeDefinition,ActionPracticeDefinition>();
            var scenes=Scenarios.Select(s=>
            {
                var d=s.PracticeDefinition;if(d==null)return s;
                if(d.TurnOpsEvents==null || d.TurnOpsEvents.Effects.ContainsKey(effect.Card))throw new ArgumentException("Vietnam effect requires an existing catalog without duplicate registration.");
                if(!definitions.TryGetValue(d,out var replacement))
                {
                    var effects=d.TurnOpsEvents.WithVietnam(effect);
                    replacement=new ActionPracticeDefinition(d.Cards,d.Ops,d.Space,d.Calendar,d.Risk,d.Scoring,d.InfluenceEvents,d.DefconVpEvents,effects,d.Realignment,d.China,
                        d.TurnEndEnabled,d.NextTurnEnabled,d.HeadlineParameters,d.HeadlineDefectors,d.FinalScoring,blockade:d.Blockade,fiveYearPlan:d.FiveYearPlan,wars:d.Wars,spaceEvent:d.SpaceEvent,olympic:d.Olympic,japanPact:d.JapanPact);
                    definitions.Add(d,replacement);
                }
                return new DemoScenario(s.Id,s.Title,s.Actor,s.SelectedCountry,s.Ops,s.Countries,s.Hands,s.EventDefinition,s.Turn,s.DeploymentDefinition,s.InitialCards,replacement,s.StartupChoices,s.TurnEndSetup);
            }).ToArray();
            var names=Names.ToDictionary(x=>x.Key,x=>x.Value,StringComparer.Ordinal);
            if(!names.ContainsKey(effect.RegionalBonus.Region))names.Add(effect.RegionalBonus.Region,effect.RegionalBonus.Region);
            return new DemoCatalog(Map,costs,scenes,names,PrintedCards);
        }
        public DemoCatalog WithVietnamScenario(string fixture)
        {
            var data=Object(ContractJson.Parse(fixture),"schema_version dataset_id description scope source_scenario_id turn_number initial_vp random_seed random_stream china_owner china_face scenarios");
            if(Int(data,"schema_version")!=1 || Text(data,"dataset_id")!="demo.vietnam_scenario" || Text(data,"scope")!="fixture")throw new ArgumentException("Vietnam lessons must be explicit fixtures.");
            Text(data,"description");var source=Scenarios.SingleOrDefault(s=>s.Id==Text(data,"source_scenario_id"));var d=source?.PracticeDefinition;var setup=source?.TurnEndSetup;
            if(setup==null || d?.TurnOpsEvents==null || !d.TurnOpsEvents.Effects.ContainsKey(new CardId("card.vietnam_revolts")) || d.TurnOpsEvents.Effects[new CardId("card.vietnam_revolts")].RegionalBonus==null || d.FinalScoring==null || d.HeadlineParameters!=null || !d.NextTurnEnabled)throw new ArgumentException("Expected final-scoring source with Vietnam event and no headline preparation.");
            var number=Int(data,"turn_number");var vp=Int(data,"initial_vp");var seed=Int(data,"random_seed");var stream=Int(data,"random_stream");
            if(number!=2 || vp<=-(long)d.Risk.Victory.AutomaticVpThreshold || vp>=d.Risk.Victory.AutomaticVpThreshold || seed<0 || stream<0)throw new ArgumentException("Invalid lesson turn, VP or seed.");
            var chinaOwner=Side(Text(data,"china_owner"));var chinaFace=Text(data,"china_face");
            if(d.China==null || chinaOwner!=PlayerSide.USSR || chinaFace!="FaceUp")throw new ArgumentException("Vietnam lesson requires explicit Soviet face-up China.");
            var china=new ChinaCardState(d.China.Card,chinaOwner,CardFace.FaceUp);
            var turn=d.Calendar.ForTurn(number);var random=Pcg32Random.FromSeed((ulong)seed,(ulong)stream).Capture();
            var rows=Array(data,"scenarios");if(rows.Count!=1)throw new ArgumentException("Expected one bounded Vietnam lessons.");
            var ids=new HashSet<string>(Scenarios.Select(s=>s.Id),StringComparer.Ordinal);var scenes=new List<DemoScenario>();
            foreach(var token in rows)
            {
                var row=Object(token,"id title actor card selected_country ordinary_actions_to_prepare us_hand ussr_hand influence");
                var id=Text(row,"id");var title=Text(row,"title");var actor=Side(Text(row,"actor"));var card=new CardId(Text(row,"card"));var selected=new CountryId(Text(row,"selected_country"));
                var prepared=Int(row,"ordinary_actions_to_prepare");
                if(id.Length>128 || id!=id.Trim() || !ids.Add(id) || title.Length>160 || !card.Equals(new CardId("card.vietnam_revolts")) || !Map.Contains(selected)
                    || actor!=PlayerSide.USSR || prepared!=0)throw new ArgumentException("Invalid lesson identity, card, actor preparation, target.");
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
                    d.Cards.Cards.Where(c=>c.Kind!=CardKind.China && c.Era>turn.Era).Select(c=>c.Id),System.Array.Empty<CardId>(),System.Array.Empty<CardId>(),System.Array.Empty<CardId>(),china,
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
