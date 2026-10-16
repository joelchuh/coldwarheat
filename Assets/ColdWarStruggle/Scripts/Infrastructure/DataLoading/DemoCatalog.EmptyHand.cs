using System;
using System.Linq;
using System.Collections.Generic;
using ColdWarStruggle.Core;
namespace ColdWarStruggle.Infrastructure
{
    public sealed partial class DemoCatalog
    {
        public DemoCatalog WithEmptyHandScenario(string fixture)
        {
            var data=Object(ContractJson.Parse(fixture),"schema_version dataset_id description scope scenario");
            if(Int(data,"schema_version")!=1 || Text(data,"dataset_id")!="demo.empty_hand_scenario" || Text(data,"scope")!="fixture")throw new ArgumentException("Empty-hand demo must be an explicit fixture.");
            var row=Object(data["scenario"],"id title source_scenario_id turn_number remaining_card china_owner china_face");var id=Text(row,"id");var title=Text(row,"title");
            if(id.Length>128 || id!=id.Trim() || title.Length>160 || Scenarios.Any(s=>s.Id==id || s.TurnEndSetup?.OrdinaryActionsToPrepare!=null))throw new ArgumentException("Expected one new empty-hand fixture.");
            var source=Scenarios.SingleOrDefault(s=>s.Id==Text(row,"source_scenario_id"));var setup=source?.TurnEndSetup;var d=source?.PracticeDefinition;
            if(setup==null || setup.InitialTrack!=null || setup.HeldCards.Count!=0 || setup.OrdinaryActionsToPrepare.HasValue || d?.FinalScoring==null
                || !d.NextTurnEnabled || d.HeadlineParameters!=null || !ReferenceEquals(d.Cards,PrintedCards))throw new ArgumentException("Expected configured final-scoring source.");
            var number=Int(row,"turn_number");if(number<2 || number>d.Calendar.LastTurn)throw new ArgumentException("Expected post-setup calendar turn.");
            var turn=d.Calendar.ForTurn(number);var normal=turn.Parameters.NormalActionsPerSide;
            var remaining=new CardId(Text(row,"remaining_card"));
            if(!d.Cards.Contains(remaining) || !setup.Cards.DrawPile.Contains(remaining))throw new ArgumentException("Remaining card must be available in source draw pile.");
            var printed=d.Cards.GetCard(remaining);
            if(printed.Kind!=CardKind.Event || printed.Era>turn.Era || printed.Affiliation==CardAffiliation.USSR)throw new ArgumentException("Remaining card must support ordinary US operations in this era.");
            var face=Text(row,"china_face")=="FaceUp"?CardFace.FaceUp:Text(row,"china_face")=="FaceDown"?CardFace.FaceDown:throw new ArgumentException("Invalid China face.");
            var china=new ChinaCardState(setup.Cards.China.Id,Side(Text(row,"china_owner")),face);
            if(china.Owner!=PlayerSide.USSR || china.Face!=CardFace.FaceUp)throw new ArgumentException("This lesson must offer voluntary China use to the empty-handed USSR.");
            var used=new HashSet<CardId>();var hands=new Dictionary<PlayerSide,IReadOnlyList<CardId>>();
            foreach(var side in new[]{PlayerSide.USSR,PlayerSide.US})
            {
                var hand=setup.Cards.GetHand(side).Take(normal-1).ToList();
                if(hand.Count!=normal-1 || hand.Any(c=>d.Cards.GetCard(c).Era>turn.Era))throw new ArgumentException("Source preparation cards do not match turn.");
                if(side==PlayerSide.US)hand.Add(remaining);
                foreach(var card in hand)if(!used.Add(card))throw new ArgumentException("Repeated physical card.");
                hands.Add(side,hand.AsReadOnly());
            }
            var cards=new CardZones(d.Cards,d.Cards.Cards.Where(c=>c.Kind!=CardKind.China && c.Era<=turn.Era && !used.Contains(c.Id)).Select(c=>c.Id),hands,
                d.Cards.Cards.Where(c=>c.Kind!=CardKind.China && c.Era>turn.Era).Select(c=>c.Id),System.Array.Empty<CardId>(),System.Array.Empty<CardId>(),System.Array.Empty<CardId>(),china,
                new[]{GameEra.Early,GameEra.Mid,GameEra.Late}.Where(e=>e<=turn.Era));
            var fresh=new DemoTurnEndSetup(number,setup.InitialVp,cards,setup.Random,ordinaryActionsToPrepare:(normal-1)*2);
            var scene=new DemoScenario(id,title,PlayerSide.USSR,source.SelectedCountry,0,source.Countries,null,turn:turn,practice:d,turnEndSetup:fresh);
            var result=new DemoCatalog(Map,costs,Scenarios.Concat(new[]{scene}),Names.ToDictionary(x=>x.Key,x=>x.Value,StringComparer.Ordinal),PrintedCards);
            var host=result.CreateHost(id);var view=host.UssrSession.ReadView();
            if(!view.Practice.CanSkipEmptyHand || host.UssrSession.QueryCardActions().China?.Influence!=RuleError.None)throw new ArgumentException("Fixture did not offer both skip and China.");
            return result;
        }
    }
}
