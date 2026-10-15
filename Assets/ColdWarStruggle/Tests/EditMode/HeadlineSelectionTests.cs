using System;
using System.Collections.Generic;
using System.Linq;
using ColdWarStruggle.Core;
using ColdWarStruggle.Application;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class HeadlineSelectionTests
    {
        static CardId Id(string n)=>CardZonesTests.Id(n);
        static PlayerSide Other(PlayerSide side)=>side==PlayerSide.US?PlayerSide.USSR:PlayerSide.US;
        static CardId HandCard(PlayerSide side)=>Id(side==PlayerSide.US?"c":"b");
        static SpaceTrackState Track(int us=0,int su=0)=>new SpaceTrackState(new SpaceRaceDefinition(0,1,1,6,SpaceAbilityTests.Steps(),SpaceAbilityTests.Abilities()),us,su);
        static HeadlineSelectionState Begin(int us=0,int su=0,IEnumerable<CardId> supported=null)=>HeadlineSelectionState.Begin(CardZonesTests.Catalog(),CardZonesTests.Zones(),Track(us,su),supported??new[]{Id("b"),Id("c")});
        static HeadlineSelectionState Select(HeadlineSelectionState s,PlayerSide side)
        {var result=HeadlineSelectionRules.PlanSelection(s,side,HandCard(side));Assert.That(result.Accepted,Is.True,result.Error.ToString());return result.State;}
        [TestCase(PlayerSide.US)][TestCase(PlayerSide.USSR)]
        public void Simultaneous_choices_lock_without_exposing_first_card_and_reveal_together(PlayerSide first)
        {
            var initial=Begin();var second=Other(first);var chosen=Select(initial,first);
            var own=HeadlineSelectionView.Create(chosen,first);var opponent=HeadlineSelectionView.Create(chosen,second);
            Assert.That(own.OwnCard,Is.EqualTo(HandCard(first)));Assert.That(own.OwnLocked,Is.True);Assert.That(own.CanChoose,Is.False);
            Assert.That(opponent.OpponentCard,Is.Null);Assert.That(opponent.OpponentLocked,Is.True);Assert.That(opponent.CanChoose,Is.True);Assert.That(opponent.Candidates,Is.EqualTo(new[]{HandCard(second)}));
            Assert.That(chosen.Ready,Is.False);Assert.That(chosen.Cards.GetHand(first),Is.Empty);Assert.That(initial.Cards.GetHand(first),Is.EqualTo(new[]{HandCard(first)}));Assert.That(initial.Cards.HeadlineReserved,Is.Empty);
            var complete=Select(chosen,second);Assert.That(complete.Ready,Is.True);
            foreach(var side in new[]{PlayerSide.US,PlayerSide.USSR})
            {var v=HeadlineSelectionView.Create(complete,side);Assert.That(v.Ready,Is.True);Assert.That(v.OwnCard,Is.EqualTo(HandCard(side)));Assert.That(v.OpponentCard,Is.EqualTo(HandCard(Other(side))));Assert.That(v.Candidates,Is.Empty);Assert.That(v.CanChoose,Is.False);}
            Assert.That(complete.Cards.DrawPile,Is.EqualTo(initial.Cards.DrawPile));Assert.That(complete.Cards.China,Is.SameAs(initial.Cards.China));Assert.That(complete.Cards.ResolvingCards,Is.Empty);
        }
        [TestCase(PlayerSide.US)][TestCase(PlayerSide.USSR)]
        public void Space_owner_must_wait_then_can_see_opponents_choice(PlayerSide owner)
        {
            var initial=owner==PlayerSide.US?Begin(4,0):Begin(0,4);var first=Other(owner);
            Assert.That(initial.ChoosesSecond,Is.EqualTo(owner));Assert.That(HeadlineSelectionView.Create(initial,owner).CanChoose,Is.False);
            var early=HeadlineSelectionRules.PlanSelection(initial,owner,HandCard(owner));Assert.That(early.Error,Is.EqualTo(RuleError.ActionNotReady));Assert.That(early.State,Is.SameAs(initial));
            var chosen=Select(initial,first);var v=HeadlineSelectionView.Create(chosen,owner);
            Assert.That(v.OpponentCard,Is.EqualTo(HandCard(first)));Assert.That(v.CanChoose,Is.True);Assert.That(v.Ready,Is.False);
            Assert.That(Select(chosen,owner).Ready,Is.True);
        }
        [TestCase(0,0)][TestCase(4,4)][TestCase(5,4)][TestCase(4,8)]
        public void Shared_space_milestone_restores_secret_simultaneous_selection(int us,int su)
        {var s=Begin(us,su);Assert.That(s.ChoosesSecond,Is.Null);foreach(var side in new[]{PlayerSide.US,PlayerSide.USSR})Assert.That(HeadlineSelectionRules.CanChoose(s,side),Is.True);}
        [Test] public void Supplied_space_definition_controls_policy_without_hardcoded_position()
        {
            var definition=new SpaceRaceDefinition(3,1,1,6,new[]{new SpaceStepDefinition("before",4,1,1,6,0,0),new SpaceStepDefinition("custom",5,1,1,6,0,0,"look")},new[]{new SpaceAbilityDefinition("look",SpaceAbilityKind.HeadlineSelectionPolicy,headlinePolicy:"OpponentRevealsFirst")});
            var s=HeadlineSelectionState.Begin(CardZonesTests.Catalog(),CardZonesTests.Zones(),new SpaceTrackState(definition,4,5),new[]{Id("b"),Id("c")});
            Assert.That(s.ChoosesSecond,Is.EqualTo(PlayerSide.USSR));
        }
        [Test] public void Already_locked_or_completed_choices_cannot_be_replaced_or_reopened()
        {
            var one=Select(Begin(),PlayerSide.US);var repeat=HeadlineSelectionRules.PlanSelection(one,PlayerSide.US,Id("a"));
            Assert.That(repeat.Error,Is.EqualTo(RuleError.ActionNotReady));Assert.That(repeat.State,Is.SameAs(one));
            var done=Select(one,PlayerSide.USSR);Assert.That(HeadlineSelectionRules.PlanSelection(done,PlayerSide.USSR,Id("b")).State,Is.SameAs(done));
            Assert.Throws<ArgumentException>(()=>HeadlineSelectionState.Begin(one.Catalog,one.Cards,Track(),one.SupportedCards));
        }
        [TestCase("not-owned",RuleError.CardNotOwned)][TestCase("unsupported",RuleError.UnsupportedCardAction)]
        [TestCase("china",RuleError.UnsupportedCardAction)][TestCase("unknown",RuleError.InvalidCommand)]
        [TestCase("default",RuleError.InvalidCommand)][TestCase("invalid-side",RuleError.InvalidCommand)]
        public void Invalid_submission_returns_stable_error_and_preserves_authority(string mutation,RuleError expected)
        {
            var s=mutation=="unsupported"?Begin(supported:new[]{Id("b")}):Begin();var side=PlayerSide.US;var card=Id("c");
            if(mutation=="not-owned")card=Id("b");else if(mutation=="china")card=Id("china");else if(mutation=="unknown")card=Id("unknown");else if(mutation=="default")card=default;else if(mutation=="invalid-side")side=(PlayerSide)99;
            var result=HeadlineSelectionRules.PlanSelection(s,side,card);Assert.That(result.Accepted,Is.False);Assert.That(result.Error,Is.EqualTo(expected));Assert.That(result.State,Is.SameAs(s));Assert.That(s.Cards.HeadlineReserved,Is.Empty);
        }
        [Test] public void Scoring_card_is_a_valid_selection_without_spending_an_ordinary_action()
        {
            var game=PlayScoringTests.Ready("card.asia_scoring");var id=new CardId("card.asia_scoring");
            var initial=HeadlineSelectionState.Begin(game.Practice.Definition.Cards,game.Cards,game.Practice.Space.Track,new[]{id});
            var result=HeadlineSelectionRules.PlanSelection(initial,PlayerSide.USSR,id);
            Assert.That(result.Accepted,Is.True);Assert.That(result.State.Cards.GetHeadline(PlayerSide.USSR),Is.EqualTo(id));Assert.That(game.Practice.CompletedActions,Is.Zero);
            Assert.That(game.Cards.GetHand(PlayerSide.USSR),Does.Contain(id));Assert.That(result.State.Cards.ResolvingCards,Is.Empty);
        }
        [Test] public void Empty_coverage_yields_no_choices_without_discarding_or_auto_completing()
        {
            var s=Begin(supported:Array.Empty<CardId>());foreach(var side in new[]{PlayerSide.US,PlayerSide.USSR})
            {var v=HeadlineSelectionView.Create(s,side);Assert.That(v.CanChoose,Is.False);Assert.That(v.Candidates,Is.Empty);Assert.That(v.Ready,Is.False);Assert.That(s.Cards.GetHand(side),Has.Count.EqualTo(1));}
            Assert.That(s.Cards.DiscardPile,Is.Empty);
        }
        [Test] public void Supported_ids_and_candidate_projections_are_immutable_copies()
        {
            var ids=new List<CardId>{Id("b"),Id("c")};var s=Begin(supported:ids);ids.Clear();var v=HeadlineSelectionView.Create(s,PlayerSide.US);
            Assert.That(v.Candidates,Is.EqualTo(new[]{Id("c")}));Assert.Throws<NotSupportedException>(()=>((IList<CardId>)s.SupportedCards).Clear());Assert.Throws<NotSupportedException>(()=>((IList<CardId>)v.Candidates).Clear());
            Assert.Throws<ArgumentOutOfRangeException>(()=>HeadlineSelectionView.Create(s,(PlayerSide)99));Assert.Throws<ArgumentOutOfRangeException>(()=>HeadlineSelectionRules.CanChoose(s,(PlayerSide)99));
        }
        [TestCase("catalog")][TestCase("zones")][TestCase("track")][TestCase("supported")]
        [TestCase("unknown")][TestCase("duplicate")][TestCase("china")][TestCase("mismatched-catalog")][TestCase("resolving")]
        public void Invalid_begin_inputs_are_rejected(string mutation)
        {
            var catalog=CardZonesTests.Catalog();var zones=CardZonesTests.Zones();var track=Track();IEnumerable<CardId> ids=new[]{Id("b"),Id("c")};
            if(mutation=="catalog")catalog=null;else if(mutation=="zones")zones=null;else if(mutation=="track")track=null;else if(mutation=="supported")ids=null;
            else if(mutation=="unknown")ids=new[]{Id("unknown")};else if(mutation=="duplicate")ids=new[]{Id("b"),Id("b")};else if(mutation=="china")ids=new[]{Id("china")};
            else if(mutation=="mismatched-catalog")catalog=new CardCatalog(catalog.Cards.Where(c=>!c.Id.Equals(Id("a"))));
            else if(mutation=="resolving"){var h=CardZonesTests.Hands();h[PlayerSide.USSR]=Array.Empty<CardId>();zones=CardZonesTests.Zones(hands:h,resolving:new[]{Id("b")});}
            Assert.That(()=>HeadlineSelectionState.Begin(catalog,zones,track,ids),Throws.InstanceOf<ArgumentException>());
        }
    }
}
