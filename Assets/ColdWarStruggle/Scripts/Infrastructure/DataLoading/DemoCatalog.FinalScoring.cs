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
        public DemoCatalog WithFinalScoringScenario(string fixture,string regionJson,string regionSchema,string chinaJson,string chinaSchema)
        {
            var data=Object(ContractJson.Parse(fixture),"schema_version dataset_id description scope scenario");
            if(Int(data,"schema_version")!=1 || Text(data,"dataset_id")!="demo.final_scoring_scenario" || Text(data,"scope")!="fixture")throw new ArgumentException("Final scoring demo must be an explicit fixture.");
            var row=Object(data["scenario"],"id title source_scenario_id turn_number initial_vp selected_country china_owner china_face influence hands");
            var id=Text(row,"id");var title=Text(row,"title");var sourceId=Text(row,"source_scenario_id");var source=Scenarios.SingleOrDefault(s=>s.Id==sourceId);var d=source?.PracticeDefinition;
            if(id.Length>128 || title.Length>160 || Scenarios.Any(s=>s.Id==id || s.TurnEndSetup!=null || s.PracticeDefinition?.FinalScoring!=null)
                || source?.InitialCards==null || source.StartupChoices==null || d?.Scoring==null || d.HeadlineParameters==null
                || !d.TurnEndEnabled || !d.NextTurnEnabled || !ReferenceEquals(d.Cards,PrintedCards))throw new ArgumentException("Expected configured headline source and a new fixture ID.");
            var final=FinalScoringDefinitionLoader.LoadBound(regionJson,regionSchema,chinaJson,chinaSchema,d.Scoring,d.Cards);
            ActionPracticeDefinition Definition(bool headline)=>new ActionPracticeDefinition(d.Cards,d.Ops,d.Space,d.Calendar,d.Risk,d.Scoring,d.InfluenceEvents,d.DefconVpEvents,d.TurnOpsEvents,d.Realignment,d.China,
                d.TurnEndEnabled,d.NextTurnEnabled,headline?d.HeadlineParameters:null,headline?d.HeadlineDefectors:null,final,blockade:d.Blockade,fiveYearPlan:d.FiveYearPlan,wars:d.Wars);
            var turn=Int(row,"turn_number");var vp=Int(row,"initial_vp");var selected=new CountryId(Text(row,"selected_country"));
            if(turn!=10 || !Map.Contains(selected))throw new ArgumentException("Final fixture requires turn ten and a known selection.");
            var definition=Definition(false);
            // Validate the same starting-state invariants used by the eventual host.
            if(vp<=-(long)d.Risk.Victory.AutomaticVpThreshold || vp>=d.Risk.Victory.AutomaticVpThreshold)throw new ArgumentException("Fixture must start before terminal VP.");
            var turnContext=d.Calendar.ForTurn(turn);var countries=Map.Countries.ToDictionary(c=>c.Id,c=>new CountryState(0,0));var seenCountries=new HashSet<CountryId>();
            var placements=Array(row,"influence");if(placements.Count>Map.Countries.Count)throw new ArgumentException("Too many influence rows.");
            foreach(var raw in placements)
            {
                var item=Object(raw,"country_id us ussr");var cid=new CountryId(Text(item,"country_id"));var us=Int(item,"us");var su=Int(item,"ussr");
                if(!Map.Contains(cid) || !seenCountries.Add(cid) || us<0 || su<0)throw new ArgumentException("Invalid final fixture influence.");
                countries[cid]=new CountryState(us,su);
            }
            var handsData=Object(row["hands"],"US USSR");var hands=new Dictionary<PlayerSide,IReadOnlyList<CardId>>();var used=new HashSet<CardId>();
            foreach(var side in new[]{PlayerSide.US,PlayerSide.USSR})
            {
                var list=Array(handsData,side.ToString());if(list.Count!=turnContext.Parameters.NormalActionsPerSide)throw new ArgumentException("Fixture must contain exactly one ordinary card per action.");
                var ids=new List<CardId>();
                foreach(var item in list)
                {
                    if(item.Type!=JTokenType.String || string.IsNullOrWhiteSpace((string)item))throw new ArgumentException("Invalid fixture card ID.");
                    var card=new CardId((string)item);if(!d.Cards.Contains(card) || !used.Add(card))throw new ArgumentException("Unknown or repeated fixture card.");
                    var printed=d.Cards.GetCard(card);var own=side==PlayerSide.US?CardAffiliation.US:CardAffiliation.USSR;
                    if(printed.Kind!=CardKind.Event || (printed.Affiliation!=own && printed.Affiliation!=CardAffiliation.Neutral))throw new ArgumentException("Fixture needs ordinary non-opponent OPS cards.");
                    ids.Add(card);
                }
                hands.Add(side,ids.AsReadOnly());
            }
            var faceText=Text(row,"china_face");var face=faceText=="FaceUp"?CardFace.FaceUp:faceText=="FaceDown"?CardFace.FaceDown:throw new ArgumentException("Invalid China face.");
            var china=new ChinaCardState(final.ChinaCardId,Side(Text(row,"china_owner")),face);
            var cards=new CardZones(d.Cards,d.Cards.Cards.Where(c=>c.Kind!=CardKind.China && !used.Contains(c.Id)).Select(c=>c.Id),hands,
                System.Array.Empty<CardId>(),System.Array.Empty<CardId>(),System.Array.Empty<CardId>(),System.Array.Empty<CardId>(),china,new[]{GameEra.Early,GameEra.Mid,GameEra.Late});
            var setup=new DemoTurnEndSetup(turn,vp,cards,source.InitialCards.Random);
            var normal=new DemoScenario(source.Id,source.Title,source.Actor,source.SelectedCountry,source.Ops,source.Countries,source.Hands,source.EventDefinition,
                source.Turn,source.DeploymentDefinition,source.InitialCards,Definition(true),source.StartupChoices);
            var scene=new DemoScenario(id,title,PlayerSide.USSR,selected,0,new ReadOnlyDictionary<CountryId,CountryState>(countries),null,turn:turnContext,practice:definition,turnEndSetup:setup);
            var result=new DemoCatalog(Map,costs,Scenarios.Select(s=>ReferenceEquals(s,source)?normal:s).Concat(new[]{scene}),Names.ToDictionary(x=>x.Key,x=>x.Value,StringComparer.Ordinal),PrintedCards);
            if(!result.CreateHost(id).UsSession.ReadView().Practice.TurnEndAvailable)throw new ArgumentException("Fixture did not reach a valid turn-end boundary.");
            return result;
        }
    }
}
