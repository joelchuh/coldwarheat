using System;
using System.Linq;
using System.Collections.Generic;
using ColdWarStruggle.Core;
using Newtonsoft.Json.Linq;
namespace ColdWarStruggle.Infrastructure
{
    public sealed partial class DemoCatalog
    {
        public DemoCatalog WithSpaceExtraScenarios(string fixture)
        {
            var data=Object(ContractJson.Parse(fixture),"schema_version dataset_id description scope scenarios");
            if(Int(data,"schema_version")!=1 || Text(data,"dataset_id")!="demo.space_extra_action_scenarios" || Text(data,"scope")!="fixture")throw new ArgumentException("Extra action demos must be explicit fixtures.");
            var rows=Array(data,"scenarios");
            if(rows.Count!=2 || Scenarios.Any(s=>s.TurnEndSetup?.InitialTrack!=null &&
                (SpaceAbilityRules.Query(s.TurnEndSetup.InitialTrack,PlayerSide.US).ActionRoundAllowance.HasValue || SpaceAbilityRules.Query(s.TurnEndSetup.InitialTrack,PlayerSide.USSR).ActionRoundAllowance.HasValue)))throw new ArgumentException("Expected two new extra action fixtures.");
            var ids=new HashSet<string>(Scenarios.Select(s=>s.Id),StringComparer.Ordinal);var added=new List<DemoScenario>();
            foreach(var raw in rows)
            {
                var row=Object(raw,"id title source_scenario_id turn_number initial_space held_cards");var id=Text(row,"id");var title=Text(row,"title");
                if(id.Length>128 || id!=id.Trim() || title.Length>160 || !ids.Add(id))throw new ArgumentException("Invalid or repeated extra fixture ID/title.");
                var source=Scenarios.SingleOrDefault(s=>s.Id==Text(row,"source_scenario_id"));var setup=source?.TurnEndSetup;var d=source?.PracticeDefinition;
                if(setup==null || setup.InitialTrack!=null || setup.HeldCards.Count!=0 || d?.FinalScoring==null || !d.NextTurnEnabled
                    || d.HeadlineParameters!=null || !ReferenceEquals(d.Cards,PrintedCards))throw new ArgumentException("Expected final-scoring fixture as source.");
                var turnNumber=Int(row,"turn_number");if(turnNumber<2 || turnNumber>d.Calendar.LastTurn)throw new ArgumentException("Fixture requires a post-setup calendar turn.");
                var turn=d.Calendar.ForTurn(turnNumber);var normal=turn.Parameters.NormalActionsPerSide;
                var positions=Object(row["initial_space"],"US USSR");var track=new SpaceTrackState(d.Space,Int(positions,"US"),Int(positions,"USSR"));
                var us=SpaceAbilityRules.Query(track,PlayerSide.US).ActionRoundAllowance;var su=SpaceAbilityRules.Query(track,PlayerSide.USSR).ActionRoundAllowance;
                if(!((us>normal && !su.HasValue) || (su>normal && !us.HasValue)))throw new ArgumentException("Expected exactly one exclusive extra action owner.");
                var owner=us.HasValue?PlayerSide.US:PlayerSide.USSR;
                var rowsBySide=Object(row["held_cards"],"US USSR");var held=new HashSet<CardId>();var used=new HashSet<CardId>();var hands=new Dictionary<PlayerSide,IReadOnlyList<CardId>>();
                foreach(var side in new[]{PlayerSide.US,PlayerSide.USSR})
                {
                    var list=Array(rowsBySide,side.ToString());if(list.Count>2 || side==owner && list.Count==0)throw new ArgumentException("Expected one or two owner-held cards and at most two other cards.");
                    var hand=setup.Cards.GetHand(side).Take(normal).ToList();
                    if(hand.Count!=normal || hand.Any(card=>d.Cards.GetCard(card).Era>turn.Era))throw new ArgumentException("Source ordinary cards do not match this era.");
                    foreach(var item in list)
                    {
                        if(item.Type!=JTokenType.String || string.IsNullOrWhiteSpace((string)item))throw new ArgumentException("Invalid held card ID.");
                        var card=new CardId((string)item);
                        if(!d.Cards.Contains(card) || d.Cards.GetCard(card).Kind!=CardKind.Event || d.Cards.GetCard(card).Era>turn.Era
                            || !setup.Cards.DrawPile.Contains(card) || !held.Add(card))throw new ArgumentException("Held cards must be distinct era-eligible ordinary cards from the source draw pile.");
                        hand.Add(card);
                    }
                    foreach(var card in hand)if(!used.Add(card))throw new ArgumentException("Repeated physical card.");
                    hands.Add(side,hand.AsReadOnly());
                }
                var cards=new CardZones(d.Cards,d.Cards.Cards.Where(c=>c.Kind!=CardKind.China && c.Era<=turn.Era && !used.Contains(c.Id)).Select(c=>c.Id),hands,
                    d.Cards.Cards.Where(c=>c.Kind!=CardKind.China && c.Era>turn.Era).Select(c=>c.Id),System.Array.Empty<CardId>(),System.Array.Empty<CardId>(),System.Array.Empty<CardId>(),setup.Cards.China,
                    new[]{GameEra.Early,GameEra.Mid,GameEra.Late}.Where(e=>e<=turn.Era));
                var fresh=new DemoTurnEndSetup(turnNumber,setup.InitialVp,cards,setup.Random,track,held);
                added.Add(new DemoScenario(id,title,owner,source.SelectedCountry,0,source.Countries,null,turn:turn,practice:d,turnEndSetup:fresh));
            }
            var result=new DemoCatalog(Map,costs,Scenarios.Concat(added),Names.ToDictionary(x=>x.Key,x=>x.Value,StringComparer.Ordinal),PrintedCards);
            foreach(var scene in added)
            {
                var h=result.CreateHost(scene.Id);var v=(scene.Actor==PlayerSide.US?h.UsSession:h.UssrSession).ReadView();
                if(!v.Practice.IsExtraAction || !v.Practice.CanDeclineExtraActions || v.Practice.TurnEndAvailable)throw new ArgumentException("Fixture did not reach the optional action boundary.");
            }
            return result;
        }
    }
}
