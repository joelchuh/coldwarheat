using System;
using System.Collections.Generic;
using System.Linq;
using ColdWarStruggle.Core;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class HeadlineCardZonesTests
    {
        static CardId Id(string n)=>CardZonesTests.Id(n);
        static CardZones Reserved(IReadOnlyDictionary<PlayerSide,CardId> selected=null)
        {
            selected??=new Dictionary<PlayerSide,CardId>{{PlayerSide.USSR,Id("b")}};
            var z=CardZonesTests.Zones();var h=CardZonesTests.Hands();
            foreach(var side in new[]{PlayerSide.US,PlayerSide.USSR})
                if(selected.TryGetValue(side,out var id))h[side]=h[side].Where(c=>!c.Equals(id)).ToArray();
            return new CardZones(CardZonesTests.Catalog(),z.DrawPile,h,z.EraReserve,z.DiscardPile,z.RemovedCards,z.ResolvingCards,z.China,z.EnteredEras,selected);
        }
        [Test] public void Existing_zones_default_to_no_headline_reservations()
        {var z=CardZonesTests.Zones();Assert.That(z.HeadlineReserved,Is.Empty);Assert.That(z.GetHeadline(PlayerSide.US),Is.Null);Assert.Throws<ArgumentOutOfRangeException>(()=>z.GetHeadline((PlayerSide)99));}
        [Test] public void Both_reservations_are_unique_entities_outside_their_hands()
        {
            var z=Reserved(new Dictionary<PlayerSide,CardId>{{PlayerSide.US,Id("c")},{PlayerSide.USSR,Id("b")}});
            Assert.That(z.GetHeadline(PlayerSide.US),Is.EqualTo(Id("c")));Assert.That(z.GetHeadline(PlayerSide.USSR),Is.EqualTo(Id("b")));
            Assert.That(z.GetHand(PlayerSide.US).Concat(z.GetHand(PlayerSide.USSR)),Is.Empty);
            Assert.That(z.DrawPile.Concat(z.EraReserve).Concat(z.HeadlineReserved.Values).Append(z.China.Id).Distinct().Count(),Is.EqualTo(CardZonesTests.Catalog().Cards.Count));
        }
        [Test] public void Reservation_inputs_and_outputs_are_defensively_copied()
        {
            var input=new Dictionary<PlayerSide,CardId>{{PlayerSide.USSR,Id("b")}};var z=Reserved(input);input.Clear();
            Assert.That(z.GetHeadline(PlayerSide.USSR),Is.EqualTo(Id("b")));Assert.Throws<NotSupportedException>(()=>((IDictionary<PlayerSide,CardId>)z.HeadlineReserved).Clear());
        }
        [TestCase("duplicate-hand")][TestCase("duplicate-slot")][TestCase("unknown")][TestCase("china")][TestCase("future")][TestCase("invalid-owner")][TestCase("missing")]
        public void Invalid_reserved_partition_is_rejected(string mutation)
        {
            var z=CardZonesTests.Zones();var h=CardZonesTests.Hands();var reserved=new Dictionary<PlayerSide,CardId>{{PlayerSide.USSR,Id("b")}};
            var reserve=z.EraReserve;h[PlayerSide.USSR]=Array.Empty<CardId>();
            switch(mutation)
            {
                case "duplicate-hand":h=CardZonesTests.Hands();break;
                case "duplicate-slot":reserved[PlayerSide.US]=Id("b");break;
                case "unknown":reserved[PlayerSide.US]=Id("unknown");break;
                case "china":reserved[PlayerSide.US]=z.China.Id;break;
                case "future":reserved[PlayerSide.US]=Id("m");reserve=reserve.Where(c=>!c.Equals(Id("m"))).ToArray();break;
                case "invalid-owner":reserved[(PlayerSide)99]=Id("a");break;
                case "missing":reserved.Clear();break;
            }
            Assert.That(()=>new CardZones(CardZonesTests.Catalog(),z.DrawPile,h,reserve,z.DiscardPile,z.RemovedCards,z.ResolvingCards,z.China,z.EnteredEras,reserved),Throws.InstanceOf<ArgumentException>());
        }
        sealed class UnusedRandom:IRandomProvider
        {
            public int Calls;
            public int NextInt(int min,int max){Calls++;throw new AssertionException("No random reads permitted");}
            public RandomSnapshot Capture(){Calls++;throw new AssertionException("No capture permitted");}
            public IRandomProvider Fork(){Calls++;throw new AssertionException("No fork permitted");}
        }
        [Test] public void Turn_deal_rejects_pending_headlines_before_touching_random()
        {
            var z=Reserved();var random=new UnusedRandom();
            Assert.That(()=>TurnDealService.Plan(CardZonesTests.Catalog(),z,new TurnCalendar(new[]{new EraTurnParameters(GameEra.Early,1,3,1,6),new EraTurnParameters(GameEra.Mid,4,7,1,7),new EraTurnParameters(GameEra.Late,8,10,1,7)}).ForTurn(2),random),Throws.InstanceOf<ArgumentException>());
            Assert.That(random.Calls,Is.Zero);Assert.That(z.GetHeadline(PlayerSide.USSR),Is.EqualTo(Id("b")));
        }
        [Test] public void Pure_china_transfer_preserves_reserved_entities()
        {
            var z=Reserved();var d=new ChinaCardDefinition(CardZonesTests.Catalog(),DefinitionTests.World(),Id("china"),new OpsRegionalBonus("china","region.asia",1));
            var moved=ChinaCardRules.TransferOnUse(d,z,PlayerSide.USSR);
            Assert.That(moved.GetHeadline(PlayerSide.USSR),Is.EqualTo(Id("b")));Assert.That(moved.China.Owner,Is.EqualTo(PlayerSide.US));Assert.That(z.China.Owner,Is.EqualTo(PlayerSide.USSR));
        }
        internal static GameState ReserveUsCard(GameState s)
        {
            var z=s.Cards;var selected=z.GetHand(PlayerSide.US).First();var h=new Dictionary<PlayerSide,IReadOnlyList<CardId>>
                {{PlayerSide.US,z.GetHand(PlayerSide.US).Skip(1).ToArray()},{PlayerSide.USSR,z.GetHand(PlayerSide.USSR)}};
            return PlayInfluenceEventTests.Copy(s,cards:new CardZones(s.Practice.Definition.Cards,z.DrawPile,h,z.EraReserve,z.DiscardPile,z.RemovedCards,z.ResolvingCards,z.China,z.EnteredEras,new Dictionary<PlayerSide,CardId>{{PlayerSide.US,selected}}));
        }
        [TestCase("ordinary")][TestCase("end")][TestCase("next")]
        public void Legacy_flow_cannot_skip_pending_headlines(string boundary)
        {
            var s=NextTurnPlayTests.Ready();CardActionKind kind;
            if(boundary=="end"){s=NextTurnPlayTests.Complete(s);kind=CardActionKind.AssessTurnEnd;}
            else if(boundary=="next"){s=NextTurnPlayTests.Assessed(s);kind=CardActionKind.BeginNextTurn;}
            else {s=PlayTurnOpsEventTests.Give(s,PlayerSide.USSR,"card.olympic_games");kind=CardActionKind.Space;}
            s=ReserveUsCard(s);var cards=s.Cards;var before=s.RandomState;
            var plan=ActionPracticeRules.Plan(s,PlayerSide.USSR,kind,"pending-headline",boundary=="ordinary"?new CardId("card.olympic_games"):(CardId?)null);
            Assert.That(plan.Accepted,Is.False);Assert.That(plan.Error,Is.EqualTo(RuleError.ActionNotReady));Assert.That(s.Cards,Is.SameAs(cards));Pcg32RandomTests.Same(before,s.RandomState);
        }
    }
}
