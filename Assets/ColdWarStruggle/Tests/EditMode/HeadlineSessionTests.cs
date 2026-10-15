using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ColdWarStruggle.Core;
using ColdWarStruggle.Application;
using Newtonsoft.Json;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class HeadlineSessionTests
    {
        internal static HeadlineCommand Command(IGameSession session,string card,string id="headline")
        {var v=session.ReadView();return new HeadlineCommand(id,v.GameId,v.Version,HeadlineFlowTests.Id(card));}
        internal static HeadlineCommandResult Submit(IGameSession session,string card,string id="headline")=>session.SubmitHeadlineAsync(Command(session,card,id)).GetAwaiter().GetResult();
        internal static LocalGameHost Host(string us="fidel",string su="romanian_abdication",bool observer=true)=>new LocalGameHost(HeadlineFlowTests.Ready(us,su),observer);
        [Test] public void Both_fixed_sessions_select_their_own_card_and_finish_in_shared_state()
        {
            var h=Host();Assert.That(h.UsSession.Side,Is.EqualTo(PlayerSide.US));Assert.That(h.UssrSession.Side,Is.EqualTo(PlayerSide.USSR));
            var us=Submit(h.UsSession,"fidel");Assert.That(us.Accepted,Is.True);Assert.That(us.View.Viewer,Is.EqualTo(PlayerSide.US));Assert.That(us.Steps,Is.Empty);
            var su=Submit(h.UssrSession,"romanian_abdication");Assert.That(su.Accepted,Is.True);Assert.That(su.View.Headline.Status,Is.EqualTo(PublicHeadlineStatus.Completed));
            Assert.That(su.View.Headline.CompletedCount,Is.EqualTo(2));Assert.That(su.View.Headline.Selection,Is.Null);Assert.That(su.View.Headline.Order,Has.Count.EqualTo(2));
            Assert.That(su.View.Practice.CompletedActions,Is.Zero);Assert.That(su.View.Practice.Actor,Is.EqualTo(PlayerSide.USSR));Assert.That(h.UsSession.ReadView().Version,Is.EqualTo(su.View.Version));
        }
        [Test] public void First_locked_card_is_private_in_every_opponent_response_until_reveal()
        {
            var h=Host();var us=Submit(h.UsSession,"fidel");var other=h.UssrSession.ReadView();
            Assert.That(us.View.Headline.Selection.OwnCard,Is.EqualTo(HeadlineFlowTests.Id("fidel")));Assert.That(other.Headline.Selection.OpponentCard,Is.Null);
            Assert.That(other.Headline.Selection.OpponentLocked,Is.True);Assert.That(other.Headline.Order,Is.Empty);Assert.That(other.Headline.CurrentPlayedBy,Is.Null);
            Assert.That(JsonConvert.SerializeObject(other),Does.Not.Contain("card.fidel"));
            var bad=Submit(h.UssrSession,"fidel","wrong");Assert.That(bad.Error,Is.EqualTo(RuleError.CardNotOwned));Assert.That(JsonConvert.SerializeObject(bad),Does.Not.Contain("card.fidel"));
            var reveal=Submit(h.UssrSession,"romanian_abdication","ok");Assert.That(JsonConvert.SerializeObject(reveal.View.Headline),Does.Contain("card.fidel"));
        }
        [Test] public void Observer_is_opt_in_and_reserved_dictionary_is_readonly()
        {
            var h=Host(observer:false);Assert.Throws<InvalidOperationException>(()=>h.ReadLocalTestSnapshot());
            h=Host();Submit(h.UssrSession,"romanian_abdication");var view=h.ReadLocalTestSnapshot();
            Assert.That(view.ReservedHeadlines[PlayerSide.USSR],Is.EqualTo(HeadlineFlowTests.Id("romanian_abdication")));
            Assert.Throws<NotSupportedException>(()=>((IDictionary<PlayerSide,CardId>)view.ReservedHeadlines).Clear());
            Assert.That(JsonConvert.SerializeObject(h.UsSession.ReadView()),Does.Not.Contain("card.romanian_abdication"));
        }
        [Test] public void Repeating_same_payload_returns_same_historical_receipt_without_state_rollback()
        {
            var h=Host();var c=Command(h.UsSession,"fidel");var first=h.UsSession.SubmitHeadlineAsync(c).Result;Submit(h.UssrSession,"romanian_abdication");var current=h.UsSession.ReadView().Version;
            var replay=h.UsSession.SubmitHeadlineAsync(c).Result;Assert.That(replay,Is.SameAs(first));Assert.That(replay.View.Version,Is.LessThan(current));Assert.That(h.UsSession.ReadView().Version,Is.EqualTo(current));
        }
        [Test] public void Changed_payload_same_id_conflicts_and_receipt_namespace_is_shared_both_directions()
        {
            var h=Host();var c=Command(h.UsSession,"fidel","x");Assert.That(h.UsSession.SubmitHeadlineAsync(c).Result.Accepted,Is.True);
            Assert.That(h.UsSession.SubmitHeadlineAsync(new HeadlineCommand("x",c.GameId,c.ExpectedVersion,HeadlineFlowTests.Id("nasser"))).Result.Error,Is.EqualTo(RuleError.CommandIdConflict));
            Assert.That(h.UsSession.SubmitCardActionAsync(new CardActionCommand("x",c.GameId,c.ExpectedVersion,CardActionKind.Event,c.Card)).Result.Error,Is.EqualTo(RuleError.CommandIdConflict));
            h=Host();c=Command(h.UsSession,"fidel","x");var rejected=h.UsSession.SubmitCardActionAsync(new CardActionCommand("x",c.GameId,c.ExpectedVersion,CardActionKind.Event,c.Card)).Result;Assert.That(rejected.Error,Is.EqualTo(RuleError.ActionNotReady));
            Assert.That(h.UsSession.SubmitHeadlineAsync(c).Result.Error,Is.EqualTo(RuleError.CommandIdConflict));
        }
        [Test] public void Same_command_id_is_independent_between_authenticated_players()
        {var h=Host();Assert.That(Submit(h.UsSession,"fidel","shared").Accepted,Is.True);Assert.That(Submit(h.UssrSession,"romanian_abdication","shared").Accepted,Is.True);}
        [Test] public async Task Same_version_competition_commits_once_and_loser_needs_new_command_id()
        {
            var h=Host();var a=Command(h.UsSession,"fidel","us");var b=Command(h.UssrSession,"romanian_abdication","su");
            var results=await Task.WhenAll(Task.Run(()=>h.UsSession.SubmitHeadlineAsync(a)),Task.Run(()=>h.UssrSession.SubmitHeadlineAsync(b)));
            Assert.That(results.Count(x=>x.Accepted),Is.EqualTo(1));Assert.That(results.Count(x=>x.Error==RuleError.StaleVersion),Is.EqualTo(1));
            var failed=results[0].Accepted?1:0;var session=failed==0?h.UsSession:h.UssrSession;var old=failed==0?a:b;
            Assert.That(await session.SubmitHeadlineAsync(old),Is.SameAs(results[failed]));var now=session.ReadView();
            Assert.That((await session.SubmitHeadlineAsync(new HeadlineCommand(old.CommandId,now.GameId,now.Version,old.Card))).Error,Is.EqualTo(RuleError.CommandIdConflict));
            Assert.That((await session.SubmitHeadlineAsync(new HeadlineCommand("fresh",now.GameId,now.Version,old.Card))).Accepted,Is.True);
        }
        [TestCase("wrong-game")][TestCase("negative-version")][TestCase("empty-id")][TestCase("long-id")][TestCase("default-card")][TestCase("null")]
        public void Invalid_requests_preserve_authority_and_do_not_reveal_secrets(string kind)
        {
            var h=Host();var v=h.UsSession.ReadView();var c=new HeadlineCommand(kind=="empty-id"?"":kind=="long-id"?new string('x',129):"invalid",kind=="wrong-game"?"elsewhere":v.GameId,kind=="negative-version"?-1:v.Version,kind=="default-card"?default:HeadlineFlowTests.Id("fidel"));
            var r=h.UsSession.SubmitHeadlineAsync(kind=="null"?null:c).Result;Assert.That(r.Error,Is.EqualTo(kind=="wrong-game"?RuleError.WrongGame:RuleError.InvalidCommand));
            Assert.That(r.Steps,Is.Empty);Assert.That(h.UsSession.ReadView().Version,Is.EqualTo(v.Version));Assert.That(h.ReadLocalTestSnapshot().ReservedHeadlines,Is.Empty);
        }
        [Test] public void Canceled_command_is_not_committed_or_cached()
        {
            var h=Host();var c=Command(h.UsSession,"fidel");using var token=new CancellationTokenSource();token.Cancel();
            Assert.That(h.UsSession.SubmitHeadlineAsync(c,token.Token).IsCanceled,Is.True);Assert.That(h.UsSession.ReadView().Version,Is.EqualTo(c.ExpectedVersion));Assert.That(h.UsSession.SubmitHeadlineAsync(c).Result.Accepted,Is.True);
        }
        [Test] public void Unsupported_cards_have_no_candidates_and_remain_owned()
        {
            var h=Host("blockade");var v=h.UsSession.ReadView();Assert.That(v.Headline.Selection.CanChoose,Is.False);Assert.That(v.Headline.Selection.Candidates,Is.Empty);
            var r=Submit(h.UsSession,"blockade");Assert.That(r.Error,Is.EqualTo(RuleError.UnsupportedCardAction));Assert.That(r.View.OwnHand,Does.Contain("card.blockade"));
        }
        [Test] public void Public_steps_contain_bounded_effect_summaries_without_raw_state_or_other_hands()
        {
            var h=Host("middle_east_scoring","nasser");Submit(h.UsSession,"middle_east_scoring");var r=Submit(h.UssrSession,"nasser");
            Assert.That(r.Steps,Has.Count.EqualTo(2));Assert.That(r.Steps[0].Card,Is.EqualTo(HeadlineFlowTests.Id("nasser")));Assert.That(r.Steps[0].Event.Changes,Is.Not.Empty);
            Assert.That(r.Steps[1].Scoring,Is.Not.Null);Assert.That(r.Steps.All(x=>!x.Terminal&&!x.WaitingForChoice),Is.True);
            var json=JsonConvert.SerializeObject(r.Steps);foreach(var forbidden in new[]{"NextState","OwnHand","DrawPile","RandomState","SupportedCards","Definition","\"View\""})Assert.That(json,Does.Not.Contain(forbidden));
            Assert.Throws<NotSupportedException>(()=>((IList<PublicHeadlineStep>)r.Steps).Clear());Assert.Throws<NotSupportedException>(()=>((IList<HeadlineOrderEntry>)r.View.Headline.Order).Clear());
        }
        [Test] public void Waiting_projection_separates_played_by_and_event_owner()
        {
            var h=Host("decolonization","nasser");Submit(h.UsSession,"decolonization");var r=Submit(h.UssrSession,"nasser");
            Assert.That(r.View.Headline.Status,Is.EqualTo(PublicHeadlineStatus.Resolving));Assert.That(r.View.Headline.CurrentPlayedBy,Is.EqualTo(PlayerSide.US));Assert.That(r.View.Headline.WaitingOwner,Is.EqualTo(PlayerSide.USSR));
            Assert.That(r.Steps.Single().WaitingForChoice,Is.True);Assert.That(r.View.Headline.CompletedCount,Is.Zero);Assert.That(r.View.Headline.Order,Has.Count.EqualTo(2));
        }
        [Test] public void Ordinary_disabled_demo_has_no_headline_projection_or_submission()
        {
            var h=new LocalGameHost(HeadlineFlowTests.Start(HeadlineFlowTests.Prepared(enabled:false)),true);Assert.That(h.UsSession.ReadView().Headline,Is.Null);
            Assert.That(Submit(h.UsSession,"fidel").Error,Is.EqualTo(RuleError.UnsupportedCardAction));
        }
    }
}
