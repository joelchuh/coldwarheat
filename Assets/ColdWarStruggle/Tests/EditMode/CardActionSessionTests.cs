using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ColdWarStruggle.Core;
using ColdWarStruggle.Application;
using Newtonsoft.Json;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class CardActionSessionTests
    {
        static CardActionCommand Command(IGameSession s,CardActionKind kind,CardId? card=null,string id="card-command")
        {var v=s.ReadView();return new CardActionCommand(id,v.GameId,v.Version,kind,card);}
        [Test] public async Task Space_retry_returns_original_receipt_without_reapplying_after_actor_changes()
        {
            var host=new LocalGameHost(ActionPracticeTests.Ready(),true);var session=host.UssrSession;
            var card=session.QueryCardActions().Cards.First(c=>c.Space==RuleError.None).Card;var cmd=Command(session,CardActionKind.Space,card);
            var result=await session.SubmitCardActionAsync(cmd);var v=session.ReadView();var retry=await session.SubmitCardActionAsync(cmd);
            Assert.That(result.Accepted,Is.True);Assert.That(retry,Is.SameAs(result));Assert.That(session.ReadView().Version,Is.EqualTo(v.Version));Assert.That(session.ReadView().Practice.UssrAttempts,Is.EqualTo(1));
            Assert.That(result.Space,Is.Not.Null);Assert.That(result.View.OwnHand,Has.No.Member(card.Value));
        }
        [Test] public async Task Finish_influence_discards_and_cross_type_id_conflicts()
        {
            var host=new LocalGameHost(ActionPracticeTests.Ready(),true);var s=host.UssrSession;var id=s.QueryCardActions().Cards.First(c=>c.Influence==RuleError.None).Card;
            Assert.That((await s.SubmitCardActionAsync(Command(s,CardActionKind.Influence,id,"begin"))).Accepted,Is.True);
            var v=s.ReadView();var placed=await s.SubmitAsync(new CommandEnvelope("place",v.GameId,v.Version,v.ContextId,new[]{new CountryId("country.poland")}));Assert.That(placed.Accepted,Is.True);
            Assert.That(s.ReadView().Practice.ActiveCard,Is.EqualTo(id));var end=Command(s,CardActionKind.FinishInfluence,card:null);
            Assert.That((await s.SubmitCardActionAsync(end)).Accepted,Is.True);Assert.That(s.ReadView().Cards.DiscardCount,Is.EqualTo(1));
            v=s.ReadView();Assert.That((await s.SubmitAsync(new CommandEnvelope(end.CommandId,v.GameId,v.Version,v.ContextId,new[]{new CountryId("country.poland")}))).Error,Is.EqualTo(RuleError.CommandIdConflict));
        }
        [Test] public async Task Conflicting_kind_under_same_id_rejects_without_second_draw()
        {
            var h=new LocalGameHost(ActionPracticeTests.Ready(),true);var s=h.UssrSession;var id=s.QueryCardActions().Cards.First(c=>c.Space==RuleError.None).Card;
            var cmd=Command(s,CardActionKind.Space,id);await s.SubmitCardActionAsync(cmd);
            Assert.That((await s.SubmitCardActionAsync(new CardActionCommand(cmd.CommandId,cmd.GameId,cmd.ExpectedVersion,CardActionKind.Influence,id))).Error,Is.EqualTo(RuleError.CommandIdConflict));
            Assert.That(s.ReadView().Practice.CompletedActions,Is.EqualTo(1));
        }
        [TestCase("wrong-game",RuleError.WrongGame)] [TestCase("stale",RuleError.StaleVersion)] [TestCase("wrong-actor",RuleError.WrongActor)]
        public async Task Envelope_checks_do_not_consume_any_card(string defect,RuleError error)
        {
            var h=new LocalGameHost(ActionPracticeTests.Ready(),true);var s=defect=="wrong-actor"?h.UsSession:h.UssrSession;var v=s.ReadView();
            var result=await s.SubmitCardActionAsync(new CardActionCommand("x",defect=="wrong-game"?"other":v.GameId,defect=="stale"?v.Version-1:v.Version,CardActionKind.Space,new CardId(v.OwnHand[0])));
            Assert.That(result.Error,Is.EqualTo(error));Assert.That(s.ReadView().Version,Is.EqualTo(v.Version));Assert.That(s.ReadView().OwnHand,Is.EqualTo(v.OwnHand));
        }
        [Test] public async Task Cancelled_command_has_no_receipt_or_effect()
        {
            var h=new LocalGameHost(ActionPracticeTests.Ready(),true);var s=h.UssrSession;var id=s.QueryCardActions().Cards.First(c=>c.Space==RuleError.None).Card;var cmd=Command(s,CardActionKind.Space,id);
            var cancellation=new CancellationToken(true);var pending=s.SubmitCardActionAsync(cmd,cancellation);Assert.That(pending.IsCanceled,Is.True);
            Assert.That((await s.SubmitCardActionAsync(cmd)).Accepted,Is.True);Assert.That(s.ReadView().Practice.CompletedActions,Is.EqualTo(1));
        }
        [Test] public async Task Normal_view_and_receipt_do_not_serialize_rng_or_other_hand()
        {
            var h=new LocalGameHost(ActionPracticeTests.Ready(),false);var s=h.UssrSession;var card=s.QueryCardActions().Cards.First(c=>c.Space==RuleError.None).Card;
            var result=await s.SubmitCardActionAsync(Command(s,CardActionKind.Space,card));var json=JsonConvert.SerializeObject(result);
            foreach(var forbidden in new[]{"RandomBefore","RandomAfter","AlgorithmId","DrawCount","Receipts","DrawPile\"","EraReserve\""})Assert.That(json,Does.Not.Contain(forbidden));
            foreach(var other in h.UsSession.ReadView().OwnHand)Assert.That(json,Does.Not.Contain(other));
            Assert.Throws<InvalidOperationException>(()=>h.ReadLocalTestSnapshot());
        }
        [Test] public async Task Rejected_new_id_after_success_does_not_replay_consumed_card()
        {
            var h=new LocalGameHost(ActionPracticeTests.Ready(),true);var s=h.UssrSession;var card=s.QueryCardActions().Cards.First(c=>c.Space==RuleError.None).Card;
            Assert.That((await s.SubmitCardActionAsync(Command(s,CardActionKind.Space,card,"one"))).Accepted,Is.True);
            Assert.That((await s.SubmitCardActionAsync(Command(s,CardActionKind.Space,card,"two"))).Error,Is.EqualTo(RuleError.WrongActor));
        }
        [Test] public async Task Start_uses_same_authority_as_completed_deployment()
        {
            var h=new LocalGameHost(ActionPracticeTests.Prepared(),true);var s=h.UssrSession;
            Assert.That((await s.SubmitCardActionAsync(Command(s,CardActionKind.StartPractice))).Accepted,Is.True);
            Assert.That(s.ReadView().Deployment,Is.Null);Assert.That(h.UsSession.ReadView().Practice.Stage,Is.EqualTo(ActionPracticeStage.ChooseCard));
        }
    }
}
