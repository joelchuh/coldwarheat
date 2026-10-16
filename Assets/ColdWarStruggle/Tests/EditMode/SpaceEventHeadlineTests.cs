using System.Linq;
using ColdWarStruggle.Core;
using ColdWarStruggle.Application;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class SpaceEventHeadlineTests
    {
        internal const string Scientist="captured_nazi_scientist";
        internal static GameState Ready(string us=Scientist,string su="fidel",int usPosition=0,int suPosition=0,int vp=0,bool collecting=true)
        {
            var s=DefectorsHeadlineResolutionTests.Prepared(us,su,collecting:collecting);var d=SpaceEventPlayTests.Bind(s.Practice.Definition);
            var p=ActionPracticeState.CreateAtTurn(d,1,vp,new SpaceTrackState(d.Space,usPosition,suPosition));
            s=new GameState(s.Map,s.Countries,s.Operations,s.GameId,s.Version,s.SpentOps,s.EventSequence,turn:s.Turn,deployment:s.Deployment,cards:s.Cards,randomState:s.RandomState,practice:p);
            return HeadlineFlowTests.Start(s);
        }
        [TestCase(PlayerSide.US)][TestCase(PlayerSide.USSR)] public void Neutral_headline_benefits_actual_player_without_spending_root_or_rng(PlayerSide side)
        {
            var us=side==PlayerSide.US?Scientist:"fidel";var su=side==PlayerSide.USSR?Scientist:"fidel";var s=Ready(us,su);var r=WarContinuationTests.SubmitBoth(s,us,su);
            var result=r.Steps.Single(step=>step.Card.Equals(SpaceEventPlayTests.Card)).Result.SpaceEventResult;Assert.That(result,Is.Not.Null);Assert.That(result.Receipt.Request.Beneficiary,Is.EqualTo(side));Assert.That(result.Receipt.Request.SourceActionId,Does.Contain("headline"));
            Assert.That(r.NextState.Practice.Headline.IsComplete,Is.True);Assert.That(r.NextState.Practice.CompletedActions,Is.Zero);Assert.That(r.NextState.Practice.Space.Track.PositionFor(side),Is.EqualTo(1));Pcg32RandomTests.Same(s.RandomState,r.NextState.RandomState);
        }
        [Test] public void Defectors_cancels_before_advancement_and_discards_instead_of_removing()
        {
            var s=Ready("defectors",Scientist);var r=WarContinuationTests.SubmitBoth(s,"defectors",Scientist);Assert.That(r.NextState.Practice.Space.EventReceipts,Is.Empty);Assert.That(r.NextState.Practice.Space.Track.USSR,Is.Zero);Assert.That(r.NextState.Cards.DiscardPile,Has.Member(SpaceEventPlayTests.Card));Assert.That(r.NextState.Cards.RemovedCards,Has.No.Member(SpaceEventPlayTests.Card));Pcg32RandomTests.Same(s.RandomState,r.NextState.RandomState);
        }
        [Test] public void Terminal_holder_cannot_select_unknown_headline_but_opponent_candidates_stay_private()
        {
            var s=Ready(usPosition:8);Assert.That(s.Practice.HeadlineSelection.SupportedCards,Has.No.Member(SpaceEventPlayTests.Card));
            var r=HeadlineFlowRules.Submit(s,PlayerSide.US,SpaceEventPlayTests.Card);Assert.That(r.Accepted,Is.False);Assert.That(r.NextState,Is.Null);Assert.That(s.Cards.GetHand(PlayerSide.US),Has.Member(SpaceEventPlayTests.Card));
            var view=new LocalGameHost(s,true).UssrSession.ReadView();Assert.That(view.Headline.Selection.OpponentCard,Is.Null);
        }
        [Test] public void Foreign_terminal_position_does_not_block_holder_and_new_headline_ability_does_not_reopen_selection()
        {
            var s=Ready(usPosition:3,suPosition:0);var r=WarContinuationTests.SubmitBoth(s,Scientist,"fidel");Assert.That(r.NextState.Practice.Headline.IsComplete,Is.True);Assert.That(r.NextState.Practice.HeadlineSelection,Is.Null);Assert.That(SpaceAbilityRules.Query(r.NextState.Practice.Space.Track,PlayerSide.US).OpponentRevealsHeadlineFirst,Is.True);
            var other=Ready(suPosition:8);Assert.That(other.Practice.HeadlineSelection.SupportedCards,Has.Member(SpaceEventPlayTests.Card));
        }
    }
}
