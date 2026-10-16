using System;
using System.Linq;
using System.Collections.Generic;
using ColdWarStruggle.Core;
using Newtonsoft.Json.Linq;
namespace ColdWarStruggle.Infrastructure
{
    public sealed partial class DemoCatalog
    {
        public DemoCatalog WithSpaceDiscardScenarios(string fixture)
        {
            var data=Object(ContractJson.Parse(fixture),"schema_version dataset_id description scope scenarios");
            if(Int(data,"schema_version")!=1 || Text(data,"dataset_id")!="demo.space_end_turn_discard_scenarios" || Text(data,"scope")!="fixture")throw new ArgumentException("Space discard demos must be explicit fixtures.");
            var rows=Array(data,"scenarios");
            if(rows.Count!=2 || Scenarios.Any(s=>s.TurnEndSetup?.InitialTrack!=null))throw new ArgumentException("Expected two new space discard fixtures.");
            var ids=new HashSet<string>(Scenarios.Select(s=>s.Id),StringComparer.Ordinal);var added=new List<DemoScenario>();
            foreach(var raw in rows)
            {
                var row=Object(raw,"id title source_scenario_id turn_number initial_space held_cards");var id=Text(row,"id");var title=Text(row,"title");
                if(id.Length>128 || id!=id.Trim() || title.Length>160 || !ids.Add(id))throw new ArgumentException("Invalid or repeated space fixture ID/title.");
                var source=Scenarios.SingleOrDefault(s=>s.Id==Text(row,"source_scenario_id"));var setup=source?.TurnEndSetup;var d=source?.PracticeDefinition;
                if(setup==null || setup.InitialTrack!=null || setup.HeldCards.Count!=0 || d?.FinalScoring==null || !d.NextTurnEnabled
                    || d.HeadlineParameters!=null || !ReferenceEquals(d.Cards,PrintedCards))throw new ArgumentException("Expected the configured final-scoring fixture as source.");
                var turnNumber=Int(row,"turn_number");
                if(turnNumber<8 || turnNumber>10)throw new ArgumentException("All-era turn-end fixture requires a Late turn.");
                var turn=d.Calendar.ForTurn(turnNumber);if(turn.Era!=GameEra.Late)throw new ArgumentException("Expected Late era.");
                var positions=Object(row["initial_space"],"US USSR");var track=new SpaceTrackState(d.Space,Int(positions,"US"),Int(positions,"USSR"));
                var us=SpaceAbilityRules.Query(track,PlayerSide.US);var su=SpaceAbilityRules.Query(track,PlayerSide.USSR);
                if(us.ActionRoundAllowance.HasValue || su.ActionRoundAllowance.HasValue || !((us.EndTurnDiscardMaximum==1 && su.EndTurnDiscardMaximum==0) || (su.EndTurnDiscardMaximum==1 && us.EndTurnDiscardMaximum==0)))throw new ArgumentException("Expected exactly one ordinary end-turn discard owner and no extra action.");
                var owner=us.EndTurnDiscardMaximum==1?PlayerSide.US:PlayerSide.USSR;
                var rowsBySide=Object(row["held_cards"],"US USSR");var held=new HashSet<CardId>();var hands=new Dictionary<PlayerSide,IReadOnlyList<CardId>>();
                foreach(var side in new[]{PlayerSide.US,PlayerSide.USSR})
                {
                    var list=Array(rowsBySide,side.ToString());if(list.Count>2 || side==owner && list.Count==0)throw new ArgumentException("Expected one or two owner-held cards, at most two for the other side.");
                    var hand=setup.Cards.GetHand(side).ToList();if(hand.Count!=turn.Parameters.NormalActionsPerSide)throw new ArgumentException("Source action count does not match this turn.");
                    foreach(var item in list)
                    {
                        if(item.Type!=JTokenType.String || string.IsNullOrWhiteSpace((string)item))throw new ArgumentException("Invalid held card ID.");
                        var card=new CardId((string)item);
                        if(!d.Cards.Contains(card) || d.Cards.GetCard(card).Kind!=CardKind.Event || !setup.Cards.DrawPile.Contains(card) || !held.Add(card))throw new ArgumentException("Held cards must be distinct ordinary cards from the source draw pile.");
                        hand.Add(card);
                    }
                    hands.Add(side,hand.AsReadOnly());
                }
                var c=setup.Cards;var cards=new CardZones(d.Cards,c.DrawPile.Where(card=>!held.Contains(card)),hands,c.EraReserve,c.DiscardPile,c.RemovedCards,c.ResolvingCards,c.China,c.EnteredEras,c.HeadlineReserved);
                var fresh=new DemoTurnEndSetup(turnNumber,setup.InitialVp,cards,setup.Random,track,held);
                added.Add(new DemoScenario(id,title,PlayerSide.USSR,source.SelectedCountry,0,source.Countries,null,turn:turn,practice:d,turnEndSetup:fresh));
            }
            var result=new DemoCatalog(Map,costs,Scenarios.Concat(added),Names.ToDictionary(x=>x.Key,x=>x.Value,StringComparer.Ordinal),PrintedCards);
            foreach(var scene in added)
                if(!result.CreateHost(scene.Id).UsSession.ReadView().Practice.TurnEndAvailable)throw new ArgumentException("Fixture did not reach an assessable boundary.");
            return result;
        }
    }
}
