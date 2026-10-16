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
        public DemoCatalog WithSpaceEvents(string parameters,string schema,string cardReferences)
        {
            if(PrintedCards==null || !Scenarios.Any(s=>s.PracticeDefinition!=null))throw new ArgumentException("Configure playable risk rules before space events.");
            var known=CardEffectReferenceIndex.Load(cardReferences);var replacements=new Dictionary<ActionPracticeDefinition,ActionPracticeDefinition>();
            var scenes=Scenarios.Select(s=>
            {
                var d=s.PracticeDefinition;if(d==null)return s;if(d.SpaceEvent!=null)throw new ArgumentException("Space event already registered.");
                if(!replacements.TryGetValue(d,out var replacement))
                {
                    var definition=SpaceEventDefinitionLoader.Load(parameters,schema,d.Cards,d.Space,known);
                    replacement=new ActionPracticeDefinition(d.Cards,d.Ops,d.Space,d.Calendar,d.Risk,d.Scoring,d.InfluenceEvents,d.DefconVpEvents,d.TurnOpsEvents,d.Realignment,d.China,
                        d.TurnEndEnabled,d.NextTurnEnabled,d.HeadlineParameters,d.HeadlineDefectors,d.FinalScoring,d.Blockade,d.FiveYearPlan,d.Wars,definition,d.Olympic);
                    replacements.Add(d,replacement);
                }
                return new DemoScenario(s.Id,s.Title,s.Actor,s.SelectedCountry,s.Ops,s.Countries,s.Hands,s.EventDefinition,s.Turn,s.DeploymentDefinition,s.InitialCards,replacement,s.StartupChoices,s.TurnEndSetup);
            }).ToArray();
            return new DemoCatalog(Map,costs,scenes,Names.ToDictionary(x=>x.Key,x=>x.Value,StringComparer.Ordinal),PrintedCards);
        }
        public DemoCatalog WithSpaceEventScenarios(string fixture)
        {
            var data=Object(ContractJson.Parse(fixture),"schema_version dataset_id description scope source_scenario_id turn_number initial_vp random_seed random_stream scenarios");
            if(Int(data,"schema_version")!=1 || Text(data,"dataset_id")!="demo.space_event_scenarios" || Text(data,"scope")!="fixture")throw new ArgumentException("Space event lessons must be explicit fixtures.");
            Text(data,"description");var source=Scenarios.SingleOrDefault(s=>s.Id==Text(data,"source_scenario_id"));var d=source?.PracticeDefinition;var setup=source?.TurnEndSetup;
            if(setup==null || d?.SpaceEvent==null || d.FinalScoring==null || d.HeadlineParameters!=null || !d.NextTurnEnabled)throw new ArgumentException("Expected final-scoring source with a space event and no headline preparation.");
            var number=Int(data,"turn_number");var vp=Int(data,"initial_vp");var seed=Int(data,"random_seed");var stream=Int(data,"random_stream");
            if(number!=2 || vp<=-(long)d.Risk.Victory.AutomaticVpThreshold || vp>=d.Risk.Victory.AutomaticVpThreshold || seed<0 || stream<0)throw new ArgumentException("Invalid lesson turn, VP or seed.");
            var turn=d.Calendar.ForTurn(number);var random=Pcg32Random.FromSeed((ulong)seed,(ulong)stream).Capture();
            var rows=Array(data,"scenarios");if(rows.Count!=4)throw new ArgumentException("Expected four bounded space lessons.");
            var ids=new HashSet<string>(Scenarios.Select(s=>s.Id),StringComparer.Ordinal);var scenes=new List<DemoScenario>();
            foreach(var token in rows)
            {
                var row=Object(token,"id title actor card selected_country us_space ussr_space ordinary_actions_to_prepare us_hand ussr_hand");
                var id=Text(row,"id");var title=Text(row,"title");var actor=Side(Text(row,"actor"));var card=new CardId(Text(row,"card"));var selected=new CountryId(Text(row,"selected_country"));
                var prepared=Int(row,"ordinary_actions_to_prepare");var track=new SpaceTrackState(d.Space,Int(row,"us_space"),Int(row,"ussr_space"));
                if(id.Length>128 || id!=id.Trim() || !ids.Add(id) || title.Length>160 || !card.Equals(d.SpaceEvent.Card.Id) || !Map.Contains(selected)
                    || prepared!=(actor==PlayerSide.US?1:0))throw new ArgumentException("Invalid lesson identity, card, actor preparation or target.");
                CardId[] Hand(string key)=>Array(row,key).Select(t=>new CardId(t.Type==JTokenType.String?(string)t:throw new ArgumentException("Expected card ID."))).ToArray();
                var us=Hand("us_hand");var su=Hand("ussr_hand");var used=new HashSet<CardId>(us.Concat(su));
                if(us.Length==0 || su.Length==0 || us.Length>turn.Parameters.HandTarget || su.Length>turn.Parameters.HandTarget
                    || !(actor==PlayerSide.US?us:su).Contains(card) || used.Count!=us.Length+su.Length
                    || used.Any(c=>!d.Cards.Contains(c) || d.Cards.GetCard(c).Kind==CardKind.China || d.Cards.GetCard(c).Era>turn.Era))throw new ArgumentException("Invalid, duplicate or unavailable lesson hand.");
                var hands=new Dictionary<PlayerSide,IReadOnlyList<CardId>>{{PlayerSide.US,us},{PlayerSide.USSR,su}};
                var cards=new CardZones(d.Cards,d.Cards.Cards.Where(c=>c.Kind!=CardKind.China && c.Era<=turn.Era && !used.Contains(c.Id)).Select(c=>c.Id),hands,
                    d.Cards.Cards.Where(c=>c.Kind!=CardKind.China && c.Era>turn.Era).Select(c=>c.Id),System.Array.Empty<CardId>(),System.Array.Empty<CardId>(),System.Array.Empty<CardId>(),setup.Cards.China,
                    new[]{GameEra.Early,GameEra.Mid,GameEra.Late}.Where(e=>e<=turn.Era));
                var board=new ReadOnlyDictionary<CountryId,CountryState>(Map.Countries.ToDictionary(c=>c.Id,c=>new CountryState(0,0)));
                var fresh=new DemoTurnEndSetup(number,vp,cards,random,track,ordinaryActionsToPrepare:prepared);
                scenes.Add(new DemoScenario(id,title,actor,selected,0,board,null,turn:turn,practice:d,turnEndSetup:fresh));
            }
            var result=new DemoCatalog(Map,costs,Scenarios.Concat(scenes),Names.ToDictionary(x=>x.Key,x=>x.Value,StringComparer.Ordinal),PrintedCards);
            foreach(var scene in scenes)
            {
                var host=result.CreateHost(scene.Id);var view=host.UssrSession.ReadView();
                if(view.Practice.Actor!=scene.Actor)throw new ArgumentException("Lesson preparation changed actor.");
            }
            return result;
        }
    }
}
