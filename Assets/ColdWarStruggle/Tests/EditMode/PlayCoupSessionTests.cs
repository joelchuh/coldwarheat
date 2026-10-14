using System;
using System.Linq;
using System.Threading.Tasks;
using ColdWarStruggle.Core;
using ColdWarStruggle.Application;
using ColdWarStruggle.Presentation;
using Newtonsoft.Json;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class PlayCoupSessionTests
    {
        static CountryId C(string id)=>new CountryId("country."+id);
        static CardActionCommand Command(IGameSession s,string target,string id="coup")
        {var v=s.ReadView();var card=s.QueryCardActions().Cards.First(c=>c.Coup==RuleError.None).Card;return new CardActionCommand(id,v.GameId,v.Version,CardActionKind.Coup,card,C(target));}
        [Test] public async Task Query_is_side_and_version_checked_and_receipt_retry_keeps_one_credit()
        {
            var h=new LocalGameHost(PlayCoupTests.Ready(),true);var s=h.UssrSession;var cmd=Command(s,"iran");
            Assert.That(s.QueryCoupTargets(cmd.Card.Value,cmd.ExpectedVersion).Targets,Does.Contain(C("iran")));
            Assert.That(s.QueryCoupTargets(cmd.Card.Value,cmd.ExpectedVersion-1).Error,Is.EqualTo(RuleError.StaleVersion));
            Assert.That(h.UsSession.QueryCoupTargets(cmd.Card.Value,cmd.ExpectedVersion).Error,Is.EqualTo(RuleError.WrongActor));
            var result=await s.SubmitCardActionAsync(cmd);Assert.That(result.Accepted,Is.True);Assert.That(result.Coup,Is.Not.Null);Assert.That(result.View.Practice.Defcon,Is.EqualTo(4));
            Assert.That(result.View.Practice.UssrMilitary,Is.EqualTo(result.Coup.MilitaryCredit));var v=s.ReadView().Version;
            Assert.That(await s.SubmitCardActionAsync(cmd),Is.SameAs(result));Assert.That(s.ReadView().Version,Is.EqualTo(v));
            Assert.That((await s.SubmitCardActionAsync(new CardActionCommand(cmd.CommandId,cmd.GameId,cmd.ExpectedVersion,cmd.Kind,cmd.Card,C("australia")))).Error,Is.EqualTo(RuleError.CommandIdConflict));
        }
        [Test] public async Task Missing_target_is_rejected_and_illegal_target_query_is_read_only()
        {
            var h=new LocalGameHost(PlayCoupTests.Ready(),true);var s=h.UssrSession;var cmd=Command(s,"iran");var v=s.ReadView();
            Assert.That(s.QueryCoupTargets(cmd.Card.Value,v.Version).Targets,Has.No.Member(C("poland")));
            Assert.That((await s.SubmitCardActionAsync(new CardActionCommand("missing",v.GameId,v.Version,cmd.Kind,cmd.Card))).Error,Is.EqualTo(RuleError.InvalidCommand));
            Assert.That(s.ReadView().Version,Is.EqualTo(v.Version));Assert.That(s.ReadView().OwnHand,Is.EqualTo(v.OwnHand));
        }
        [Test] public async Task Public_result_has_no_rng_history_or_other_hand()
        {
            var h=new LocalGameHost(PlayCoupTests.Ready(),false);var s=h.UssrSession;var result=await s.SubmitCardActionAsync(Command(s,"iran"));Assert.That(result.Accepted,Is.True);
            var json=JsonConvert.SerializeObject(result);foreach(var forbidden in new[]{"RandomBefore","RandomAfter","Receipts","DrawCount","AlgorithmId","AppliedCredits"})Assert.That(json,Does.Not.Contain(forbidden));
            foreach(var card in h.UsSession.ReadView().OwnHand)Assert.That(json,Does.Not.Contain(card));
            Assert.That(result.Coup.Target,Is.EqualTo(C("iran")));Assert.That(result.Coup.Roll,Is.InRange(1,6));
        }
        [Test] public async Task Nuclear_absorbing_state_keeps_old_receipt_and_rejects_new_command()
        {
            var h=new LocalGameHost(PlayCoupTests.Ready(),true);CardActionCommand old=null;CardActionResult first=null;
            foreach(var target in new[]{"iran","iraq","south_africa","nigeria"})
            {var s=h.UsSession.ReadView().Practice.Actor==PlayerSide.US?h.UsSession:h.UssrSession;var cmd=Command(s,target,target);var result=await s.SubmitCardActionAsync(cmd);Assert.That(result.Accepted,Is.True);if(old==null){old=cmd;first=result;}}
            Assert.That(h.UsSession.ReadView().Practice.Winner,Is.EqualTo(PlayerSide.USSR));Assert.That(await h.UssrSession.SubmitCardActionAsync(old),Is.SameAs(first));
            var v=h.UsSession.ReadView();Assert.That((await h.UsSession.SubmitCardActionAsync(new CardActionCommand("after",v.GameId,v.Version,CardActionKind.Coup,new CardId(v.OwnHand[0]),C("nigeria")))).Error,Is.EqualTo(RuleError.GameAlreadyEnded));
        }
        static DemoController Controller()
        {var c=new DemoController(id=>new LocalGameHost(PlayCoupTests.Ready(),true));c.LoadScenario("risk",PlayerSide.USSR,C("iran"));return c;}
        [Test] public async Task Coup_selection_is_local_intent_then_single_atomic_confirm()
        {
            var c=Controller();var v=c.ReadView();var card=c.CardOptions().Cards.First(x=>x.Coup==RuleError.None).Card;c.SelectCard(card);var revision=c.InteractionRevision;
            c.SelectCoupUse();Assert.That(c.SelectingCoup,Is.True);Assert.That(c.InteractionRevision,Is.GreaterThan(revision));Assert.That(c.ReadView().Version,Is.EqualTo(v.Version));
            Assert.That(c.LegalTargets().Targets,Does.Contain(C("iran")));Assert.That(c.LegalTargets().Targets,Has.No.Member(C("poland")));
            Assert.That((await c.PlayCardAsync(CardActionKind.Coup)).Accepted,Is.True);Assert.That(c.SelectedCard,Is.Null);Assert.That(c.SelectingCoup,Is.False);Assert.That(c.LastCardResult.Coup,Is.Not.Null);
            var version=c.ReadView().Version;await c.RetryCardAsync();Assert.That(c.ReadView().Version,Is.EqualTo(version));
        }
        [Test] public void Switching_card_identity_or_scenario_clears_coup_target_mode()
        {
            var c=Controller();var cards=c.CardOptions().Cards.Where(x=>x.Coup==RuleError.None).ToArray();c.SelectCard(cards[0].Card);c.SelectCoupUse();c.SelectCard(cards[1].Card);Assert.That(c.SelectingCoup,Is.False);
            c.SelectCoupUse();c.SelectSide(PlayerSide.US);Assert.That(c.SelectingCoup,Is.False);Assert.That(c.LegalTargets().Targets,Is.Empty);
            c.LoadScenario("again",PlayerSide.USSR,C("iran"));Assert.That(c.SelectingCoup,Is.False);
        }
    }
}
