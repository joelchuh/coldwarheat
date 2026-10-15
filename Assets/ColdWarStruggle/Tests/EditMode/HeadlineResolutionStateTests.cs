using System;
using System.Collections.Generic;
using System.Linq;
using ColdWarStruggle.Core;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class HeadlineResolutionStateTests
    {
        static CardId Id(string n)=>CardZonesTests.Id(n);
        internal static CardZones Zones(int count=2,bool busy=false)
        {
            var z=CardZonesTests.Zones();var h=CardZonesTests.Hands();
            var reserved=new Dictionary<PlayerSide,CardId>();
            if(count>0){reserved[PlayerSide.US]=Id("c");h[PlayerSide.US]=Array.Empty<CardId>();}
            if(count>1){reserved[PlayerSide.USSR]=Id("b");h[PlayerSide.USSR]=Array.Empty<CardId>();}
            return new CardZones(CardZonesTests.Catalog(),z.DrawPile.Where(c=>!busy || !c.Equals(Id("a"))),h,z.EraReserve,z.DiscardPile,z.RemovedCards,
                busy?new[]{Id("a")}:Array.Empty<CardId>(),z.China,z.EnteredEras,reserved);
        }
        static HeadlineResolutionState Create(int turn=1)=>HeadlineResolutionState.CreateOrdinary(new HeadlineOrderParameters(0,PlayerSide.US),CardZonesTests.Catalog(),Zones(),TurnCalendarTests.Calendar().ForTurn(turn));
        [Test] public void Queue_starts_with_frozen_printed_order_and_no_consumed_root()
        {
            var cards=Zones();var turn=TurnCalendarTests.Calendar().ForTurn(1);
            var q=HeadlineResolutionState.CreateOrdinary(new HeadlineOrderParameters(0,PlayerSide.US),CardZonesTests.Catalog(),cards,turn);
            Assert.That(q.Turn,Is.SameAs(turn));Assert.That(q.CompletedCount,Is.Zero);Assert.That(q.IsComplete,Is.False);
            Assert.That(q.Order.Select(e=>e.Card),Is.EqualTo(new[]{Id("c"),Id("b")}));Assert.That(q.Current,Is.SameAs(q.Order[0]));
            Assert.That(q.Current.PlayedBy,Is.EqualTo(PlayerSide.US));Assert.That(q.Current.Value,Is.EqualTo(2));
            Assert.That(cards.HeadlineReserved,Has.Count.EqualTo(2));Assert.That(cards.ResolvingCards,Is.Empty);
        }
        [Test] public void Completing_exact_current_root_advances_once_and_keeps_prior_snapshot()
        {
            var first=Create();var second=first.AdvanceAfterResolution(Id("c"),PlayerSide.US);
            Assert.That(first.CompletedCount,Is.Zero);Assert.That(second.CompletedCount,Is.EqualTo(1));Assert.That(second.Current.Card,Is.EqualTo(Id("b")));
            Assert.That(second.Current.PlayedBy,Is.EqualTo(PlayerSide.USSR));Assert.That(second.Order,Is.SameAs(first.Order));Assert.That(second.Turn,Is.SameAs(first.Turn));
            var done=second.AdvanceAfterResolution(Id("b"),PlayerSide.USSR);
            Assert.That(done.IsComplete,Is.True);Assert.That(done.CompletedCount,Is.EqualTo(2));Assert.That(done.Current,Is.Null);Assert.That(done.CurrentResolutionId,Is.Null);
            Assert.That(second.CompletedCount,Is.EqualTo(1));Assert.That(done.Order,Has.Count.EqualTo(2));
        }
        [Test] public void Queue_is_readonly_and_alternative_tie_policy_is_preserved()
        {
            var q=HeadlineResolutionState.CreateOrdinary(new HeadlineOrderParameters(0,PlayerSide.USSR),CardZonesTests.Catalog(),Zones(),TurnCalendarTests.Calendar().ForTurn(1));
            Assert.That(q.Current.PlayedBy,Is.EqualTo(PlayerSide.USSR));Assert.Throws<NotSupportedException>(()=>((IList<HeadlineOrderEntry>)q.Order).Clear());
        }
        [Test] public void Root_identity_is_distinct_for_each_side_and_turn_and_stable_during_retries()
        {
            var first=Create();var second=first.AdvanceAfterResolution(Id("c"),PlayerSide.US);var later=Create(2);
            Assert.That(first.CurrentResolutionId,Is.EqualTo(Create().CurrentResolutionId));
            Assert.That(new[]{first.CurrentResolutionId,second.CurrentResolutionId,later.CurrentResolutionId}.Distinct().Count(),Is.EqualTo(3));
            Assert.That(first.CurrentResolutionId,Does.Not.StartWith("practice.event."));
        }
        [TestCase("wrong-card")][TestCase("wrong-side")][TestCase("unknown-side")][TestCase("default-card")][TestCase("already-complete")][TestCase("repeat-first")]
        public void Wrong_or_repeated_completion_does_not_advance_cursor(string mutation)
        {
            var q=Create();var id=Id("c");var side=PlayerSide.US;
            switch(mutation)
            {
                case "wrong-card":id=Id("b");break;
                case "wrong-side":side=PlayerSide.USSR;break;
                case "unknown-side":side=(PlayerSide)99;break;
                case "default-card":id=default;break;
                case "already-complete":q=q.AdvanceAfterResolution(Id("c"),PlayerSide.US).AdvanceAfterResolution(Id("b"),PlayerSide.USSR);break;
                case "repeat-first":q=q.AdvanceAfterResolution(Id("c"),PlayerSide.US);break;
            }
            var count=q.CompletedCount;Assert.That(()=>q.AdvanceAfterResolution(id,side),Throws.InstanceOf<ArgumentException>());Assert.That(q.CompletedCount,Is.EqualTo(count));
        }
        [TestCase("parameters")][TestCase("catalog")][TestCase("cards")][TestCase("turn")][TestCase("empty")][TestCase("one")][TestCase("busy")][TestCase("different-catalog")]
        public void Incomplete_or_invalid_authority_input_cannot_create_an_execution_queue(string mutation)
        {
            var p=new HeadlineOrderParameters(0,PlayerSide.US);var catalog=CardZonesTests.Catalog();var cards=Zones();var turn=TurnCalendarTests.Calendar().ForTurn(1);
            switch(mutation)
            {
                case "parameters":p=null;break;case "catalog":catalog=null;break;case "cards":cards=null;break;case "turn":turn=null;break;
                case "empty":cards=Zones(0);break;case "one":cards=Zones(1);break;case "busy":cards=Zones(busy:true);break;
                case "different-catalog":catalog=new CardCatalog(catalog.Cards.Where(c=>!c.Id.Equals(Id("a"))));break;
            }
            Assert.That(()=>HeadlineResolutionState.CreateOrdinary(p,catalog,cards,turn),Throws.InstanceOf<ArgumentException>());
        }
    }
}
