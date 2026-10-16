using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ColdWarStruggle.Core;
using ColdWarStruggle.Application;
using ColdWarStruggle.Presentation;
using Newtonsoft.Json;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class BlockadeSessionTests
    {
        static CardId Id(string n)=>BlockadePlayTests.Id(n);
        static CardActionCommand Command(IGameSession s,CardActionKind kind,CardId? card=null,string id="command")
        {var v=s.ReadView();return new CardActionCommand(id,v.GameId,v.Version,kind,card);}
        static async Task<LocalGameHost> Pending()
        {var h=new LocalGameHost(BlockadePlayTests.Ready(),true);Assert.That((await h.UssrSession.SubmitCardActionAsync(Command(h.UssrSession,CardActionKind.Event,Id("blockade"),"root"))).Accepted,Is.True);return h;}
        [Test] public async Task Only_US_sees_current_candidates_and_public_waiting_receipt_has_no_hand_leak()
        {
            var h=await Pending();var us=h.UsSession.ReadView();var su=h.UssrSession.ReadView();
            Assert.That(us.Blockade,Is.Not.Null);Assert.That(su.Blockade,Is.Not.Null);Assert.That(us.Blockade.Owner,Is.EqualTo(PlayerSide.US));Assert.That(us.Blockade.CanChoose,Is.True);Assert.That(us.Blockade.MinimumEffectiveOps,Is.EqualTo(3));
            Assert.That(us.Blockade.Candidates.Single().Card,Is.EqualTo(Id("marshall_plan")));Assert.That(us.Blockade.Candidates.Single().EffectiveOps,Is.EqualTo(4));Assert.That(su.Blockade.CanChoose,Is.False);Assert.That(su.Blockade.Candidates,Is.Empty);
            Assert.That(JsonConvert.SerializeObject(su),Does.Not.Contain("card.marshall_plan"));Assert.That(su.Cards.DiscardPile,Is.Empty);
            Assert.Throws<NotSupportedException>(()=>((IList<BlockadeDiscardOption>)us.Blockade.Candidates).Clear());
        }
        [TestCase(true)][TestCase(false)] public async Task Accepted_response_is_public_and_retries_preserve_original_receipts_and_current_state(bool discard)
        {
            var h=new LocalGameHost(BlockadePlayTests.Ready(),true);var root=Command(h.UssrSession,CardActionKind.Event,Id("blockade"),"root");var waiting=await h.UssrSession.SubmitCardActionAsync(root);Assert.That(waiting.Accepted,Is.True);Assert.That(waiting.Blockade,Is.Not.Null);Assert.That(waiting.Blockade.Outcome,Is.EqualTo(BlockadeOutcome.AwaitingChoice));
            var cmd=Command(h.UsSession,discard?CardActionKind.DiscardForBlockade:CardActionKind.DeclineBlockade,discard?Id("marshall_plan"):(CardId?)null,"answer");var result=await h.UsSession.SubmitCardActionAsync(cmd);Assert.That(result.Accepted,Is.True);Assert.That(result.Blockade,Is.Not.Null);
            var live=h.UssrSession.ReadView();Assert.That(live.Blockade,Is.Null);Assert.That(live.Practice.CompletedActions,Is.EqualTo(1));Assert.That(live.Cards.DiscardPile.Contains(Id("marshall_plan")),Is.EqualTo(discard));
            Assert.That(result.Blockade.Outcome,Is.EqualTo(discard?BlockadeOutcome.Discarded:BlockadeOutcome.InfluenceRemoved));Assert.That(result.Blockade.DiscardedCard,Is.EqualTo(discard?Id("marshall_plan"):(CardId?)null));
            Assert.That(live.Countries.Single(c=>c.Definition.Id.Equals(BlockadePlayTests.West)).US,Is.EqualTo(discard?5:0));
            Assert.That(await h.UsSession.SubmitCardActionAsync(cmd),Is.SameAs(result));Assert.That(await h.UssrSession.SubmitCardActionAsync(root),Is.SameAs(waiting));Assert.That(waiting.View.Cards.DiscardPile,Is.Empty);Assert.That(waiting.View.Blockade.Candidates,Is.Empty);
            Assert.That(h.UssrSession.ReadView().Version,Is.EqualTo(live.Version));Assert.That(h.UssrSession.ReadView().EventSequence,Is.EqualTo(live.EventSequence));Assert.That(waiting.View.Version,Is.LessThan(live.Version));
        }
        [TestCase("owner",RuleError.WrongActor)][TestCase("low",RuleError.InvalidSelection)][TestCase("score",RuleError.InvalidSelection)][TestCase("china",RuleError.InvalidSelection)][TestCase("other",RuleError.InvalidSelection)]
        public async Task Rejected_selection_never_changes_influence_or_reveals_opponent_candidates(string kind,RuleError error)
        {
            var h=await Pending();var s=kind=="owner"?h.UssrSession:h.UsSession;var before=s.ReadView();
            var card=Id(kind=="low"?"independent_reds":kind=="score"?"asia_scoring":kind=="china"?"china":kind=="other"?"olympic_games":"marshall_plan");
            var r=await s.SubmitCardActionAsync(Command(s,CardActionKind.DiscardForBlockade,card,"bad"));Assert.That(r.Error,Is.EqualTo(error));Assert.That(r.View.Blockade,Is.Not.Null);Assert.That(r.View.Version,Is.EqualTo(before.Version));Assert.That(r.View.EventSequence,Is.EqualTo(before.EventSequence));Assert.That(r.View.Cards.DiscardPile,Is.Empty);Assert.That(r.View.Countries.Single(c=>c.Definition.Id.Equals(BlockadePlayTests.West)).US,Is.EqualTo(5));
            if(kind=="owner"){Assert.That(r.View.Blockade.Candidates,Is.Empty);Assert.That(JsonConvert.SerializeObject(r),Does.Not.Contain("card.marshall_plan"));}
        }
        [TestCase(true,RuleError.WrongGame)][TestCase(false,RuleError.StaleVersion)] public async Task Envelope_rejects_other_game_or_old_version(bool otherGame,RuleError expected)
        {
            var h=await Pending();var v=h.UsSession.ReadView();var cmd=new CardActionCommand("bad-envelope",otherGame?"another-game":v.GameId,otherGame?v.Version:v.Version-1,CardActionKind.DiscardForBlockade,Id("marshall_plan"));
            var r=await h.UsSession.SubmitCardActionAsync(cmd);Assert.That(r.Error,Is.EqualTo(expected));Assert.That(r.View.Blockade,Is.Not.Null);Assert.That(h.UsSession.ReadView().Version,Is.EqualTo(v.Version));Assert.That(h.UsSession.ReadView().Cards.DiscardPile,Is.Empty);
        }
        [Test] public async Task Conflicting_command_id_cannot_apply_decline_after_accepted_discard()
        {
            var h=await Pending();var cmd=Command(h.UsSession,CardActionKind.DiscardForBlockade,Id("marshall_plan"),"same-id");var done=await h.UsSession.SubmitCardActionAsync(cmd);Assert.That(done.Accepted,Is.True);
            var conflict=await h.UsSession.SubmitCardActionAsync(new CardActionCommand(cmd.CommandId,cmd.GameId,cmd.ExpectedVersion,CardActionKind.DeclineBlockade));Assert.That(conflict.Error,Is.EqualTo(RuleError.CommandIdConflict));Assert.That(conflict.View.Cards.DiscardPile,Has.Member(Id("marshall_plan")));Assert.That(conflict.View.Version,Is.EqualTo(done.View.Version));Assert.That(conflict.View.Countries.Single(c=>c.Definition.Id.Equals(BlockadePlayTests.West)).US,Is.EqualTo(5));
        }
        static LocalGameHost HeadlineHost(long version=0)
        {
            var s=HeadlineFlowTests.Prepared("blockade","truman_doctrine");var d=BlockadePlayTests.Bind(s.Practice.Definition);
            s=new GameState(s.Map,s.Countries,s.Operations,s.GameId,s.Version,s.SpentOps,s.EventSequence,turn:s.Turn,deployment:s.Deployment,cards:s.Cards,randomState:s.RandomState,practice:ActionPracticeState.Create(d));
            s=PlayTurnOpsEventTests.Give(s,PlayerSide.US,"card.marshall_plan");var board=s.Countries.ToDictionary(x=>x.Key,x=>x.Value);board[BlockadePlayTests.West]=new CountryState(5,2);s=PlayInfluenceEventTests.Copy(s,board,version:version);
            return new LocalGameHost(HeadlineFlowTests.Start(s),true);
        }
        static HeadlineCommandResult ChooseHeadlines(LocalGameHost h)
        {Assert.That(HeadlineSessionTests.Submit(h.UsSession,"blockade").Accepted,Is.True);var r=HeadlineSessionTests.Submit(h.UssrSession,"truman_doctrine");Assert.That(r.Accepted,Is.True,r.Error.ToString());return r;}
        [Test] public async Task Headline_receipts_report_waiting_then_completed_Blockade_and_following_choice()
        {
            var h=HeadlineHost();var selected=ChooseHeadlines(h);Assert.That(selected.Steps.Single().WaitingForChoice,Is.True);Assert.That(selected.Steps.Single().Blockade,Is.Not.Null);Assert.That(selected.View.Blockade.Candidates,Is.Empty);
            var done=await h.UsSession.SubmitCardActionAsync(Command(h.UsSession,CardActionKind.DiscardForBlockade,Id("marshall_plan")));
            Assert.That(done.Accepted,Is.True);Assert.That(done.HeadlineSteps.Count,Is.EqualTo(2));Assert.That(done.HeadlineSteps[0].Blockade.Outcome,Is.EqualTo(BlockadeOutcome.Discarded));Assert.That(done.HeadlineSteps[0].WaitingForChoice,Is.False);Assert.That(done.HeadlineSteps[1].WaitingForChoice,Is.True);Assert.That(done.View.Event,Is.Not.Null);Assert.That(done.View.Practice.CompletedActions,Is.Zero);
        }
        [Test] public async Task Following_headline_overflow_rolls_back_the_accepted_discard_draft()
        {
            var low=HeadlineHost();ChooseHeadlines(low);Assert.That(low.UssrSession.ReadView().Headline.WaitingOwner,Is.EqualTo(PlayerSide.US));var high=HeadlineHost(long.MaxValue-2-low.UsSession.ReadView().Version);ChooseHeadlines(high);var before=high.UsSession.ReadView();Assert.That(before.Version,Is.EqualTo(long.MaxValue-2));
            var result=await high.UsSession.SubmitCardActionAsync(Command(high.UsSession,CardActionKind.DiscardForBlockade,Id("marshall_plan")));
            Assert.That(result.Error,Is.EqualTo(RuleError.NumericOverflow));Assert.That(result.Blockade,Is.Null);Assert.That(result.HeadlineSteps,Is.Empty);Assert.That(result.View.Version,Is.EqualTo(before.Version));Assert.That(result.View.EventSequence,Is.EqualTo(before.EventSequence));Assert.That(result.View.Blockade,Is.Not.Null);Assert.That(result.View.OwnHand,Has.Member("card.marshall_plan"));Assert.That(result.View.Cards.DiscardPile,Is.Empty);
        }
        [Test] public async Task Controller_clears_selection_across_identities_and_resets_but_can_retry_an_accepted_response()
        {
            var c=new DemoController(_=>new LocalGameHost(BlockadePlayTests.Ready(),true));c.LoadScenario("fixture",PlayerSide.USSR,BlockadePlayTests.West);c.SelectCard(Id("blockade"));Assert.That((await c.PlayCardAsync(CardActionKind.Event)).Accepted,Is.True);
            c.SelectSide(PlayerSide.US);c.SelectCard(Id("marshall_plan"));Assert.That(c.CanDiscardForBlockade,Is.True);Assert.That(c.CanDeclineBlockade,Is.True);c.SelectSide(PlayerSide.USSR);Assert.That(c.SelectedCard,Is.Null);Assert.That(c.CanDiscardForBlockade,Is.False);Assert.That(c.ReadView().Blockade.Candidates,Is.Empty);
            Assert.ThrowsAsync<InvalidOperationException>(async()=>{await c.DiscardForBlockadeAsync(Id("marshall_plan"));});
            c.SelectSide(PlayerSide.US);c.SelectCard(Id("marshall_plan"));var done=await c.DiscardForBlockadeAsync(Id("marshall_plan"));Assert.That(done.Accepted,Is.True);Assert.That(c.SelectedCard,Is.Null);Assert.That(c.ReadView().Blockade,Is.Null);Assert.That(await c.RetryCardAsync(),Is.SameAs(done));
            c.LoadScenario("fixture",PlayerSide.USSR,BlockadePlayTests.West);Assert.That(c.CanRetryCard,Is.False);Assert.That(c.LastCardResult,Is.Null);Assert.That(c.CardResultHistoryMessage,Is.Null);
        }
        [Test] public async Task Controller_can_decline_without_selecting_a_qualified_card()
        {
            var c=new DemoController(_=>new LocalGameHost(BlockadePlayTests.Ready(),true));c.LoadScenario("fixture",PlayerSide.USSR,BlockadePlayTests.West);c.SelectCard(Id("blockade"));await c.PlayCardAsync(CardActionKind.Event);c.SelectSide(PlayerSide.US);c.SelectCard(Id("independent_reds"));Assert.That(c.CanDiscardForBlockade,Is.False);Assert.That(c.CanDeclineBlockade,Is.True);
            var done=await c.DeclineBlockadeAsync();Assert.That(done.Accepted,Is.True);Assert.That(done.Blockade.Outcome,Is.EqualTo(BlockadeOutcome.InfluenceRemoved));Assert.That(c.ReadView().Countries.Single(x=>x.Definition.Id.Equals(BlockadePlayTests.West)).US,Is.Zero);
        }
    }
}
