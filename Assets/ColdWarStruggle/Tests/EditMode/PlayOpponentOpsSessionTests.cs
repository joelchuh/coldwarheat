using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using ColdWarStruggle.Core;
using ColdWarStruggle.Application;
using ColdWarStruggle.Presentation;
using Newtonsoft.Json;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class PlayOpponentOpsSessionTests
    {
        static CountryId C(string s)=>PlayOpponentOpsTests.C(s);
        static async Task<CardActionResult> Send(IGameSession session,string id,CardActionKind kind,CardId? card=null,CountryId? target=null)
        {var v=session.ReadView();return await session.SubmitCardActionAsync(new CardActionCommand(id,v.GameId,v.Version,kind,card,target));}
        [Test] public async Task Pending_public_options_and_coup_targets_require_fixed_actor_current_version_and_actual_resolving_card()
        {
            var h=new LocalGameHost(PlayOpponentOpsTests.Ready(),true);var ussr=h.UssrSession;var card=new CardId("card.duck_and_cover");var before=ussr.ReadView();var o=ussr.QueryCardActions().Cards.Single(x=>x.Card.Equals(card));Assert.That(o.OpponentEventFirst,Is.EqualTo(RuleError.None));Assert.That(o.OpponentOpsFirst,Is.EqualTo(RuleError.None));var a=await Send(ussr,"start",CardActionKind.OpponentEventFirst,card);Assert.That(a.Accepted,Is.True);var v=a.View;var options=ussr.QueryCardActions();Assert.That(options.Version,Is.EqualTo(v.Version));Assert.That(options.PendingOperations.Card,Is.EqualTo(card));Assert.That(options.PendingOperations.Influence,Is.EqualTo(RuleError.None));Assert.That(options.PendingOperations.Coup,Is.EqualTo(RuleError.None));Assert.That(options.PendingOperations.Realignment,Is.EqualTo(RuleError.None));Assert.That(options.PendingOperations.CurrentOps,Is.EqualTo(3));Assert.That(h.UsSession.QueryCardActions().PendingOperations.Coup,Is.EqualTo(RuleError.WrongActor));
            Assert.That(ussr.QueryCoupTargets(card,before.Version).Error,Is.EqualTo(RuleError.StaleVersion));Assert.That(h.UsSession.QueryCoupTargets(card,v.Version).Error,Is.EqualTo(RuleError.WrongActor));Assert.That(ussr.QueryCoupTargets(new CardId("card.olympic_games"),v.Version).Error,Is.EqualTo(RuleError.CardNotOwned));var targets=ussr.QueryCoupTargets(card,v.Version);Assert.That(targets.Error,Is.EqualTo(RuleError.None));Assert.That(targets.Targets,Does.Contain(C("angola")));Assert.That(targets.Targets,Has.No.Member(C("france")));Assert.That(JsonConvert.SerializeObject(options.PendingOperations),Does.Not.Contain("Random").And.Not.Contain("Definition").And.Not.Contain("Hand"));
            Assert.That((await Send(ussr,"bypass",CardActionKind.Coup,card,C("angola"))).Error,Is.EqualTo(RuleError.ActionNotReady));Assert.That((await Send(ussr,"choose",CardActionKind.UseOpponentRealignment)).Accepted,Is.True);Assert.That(ussr.QueryCardActions().PendingOperations,Is.Null);Assert.That(ussr.ReadView().Practice.OpponentOps.Mode,Is.EqualTo(OperationsMode.Realignment));
        }
        [Test] public async Task Combined_coup_and_immediate_event_receipt_replays_without_reroll_and_conflicting_payload_is_rejected()
        {
            var h=new LocalGameHost(PlayOpponentOpsTests.Ready(level:3),true);var session=h.UssrSession;Assert.That((await Send(session,"begin",CardActionKind.OpponentOpsFirst,new CardId("card.duck_and_cover"))).Accepted,Is.True);var v=session.ReadView();var command=new CardActionCommand("coup",v.GameId,v.Version,CardActionKind.UseOpponentCoup,target:C("angola"));var r=await session.SubmitCardActionAsync(command);Assert.That(r.Accepted,Is.True);Assert.That(r.Coup,Is.Not.Null);Assert.That(r.DefconVpEvent,Is.Not.Null);Assert.That(r.Coup.NuclearWar,Is.False);Assert.That(r.DefconVpEvent.Terminal,Is.True);Assert.That(r.View.Practice.Winner,Is.EqualTo(PlayerSide.US));Assert.That(await session.SubmitCardActionAsync(command),Is.SameAs(r));Assert.That((await session.SubmitCardActionAsync(new CardActionCommand("coup",v.GameId,v.Version,CardActionKind.UseOpponentCoup,target:C("iran")))).Error,Is.EqualTo(RuleError.CommandIdConflict));Assert.That((await session.SubmitCardActionAsync(new CardActionCommand("old",v.GameId,v.Version,CardActionKind.UseOpponentCoup,target:C("angola")))).Error,Is.EqualTo(RuleError.StaleVersion));Assert.That(session.ReadView().Version,Is.EqualTo(r.View.Version));
        }
        [Test] public async Task Coup_receipt_and_waiting_opponent_choice_are_both_returned_without_root_switch()
        {
            var s=PlayOpponentInfluenceTests.ReadyUS("card.suez_crisis");var b=s.Countries.ToDictionary(x=>x.Key,x=>x.Value);b[C("angola")]=new CountryState(3,3);var h=new LocalGameHost(PlayScoringTests.Replace(s,b),true);Assert.That((await Send(h.UsSession,"begin",CardActionKind.OpponentOpsFirst,new CardId("card.suez_crisis"))).Accepted,Is.True);var r=await Send(h.UsSession,"coup",CardActionKind.UseOpponentCoup,target:C("angola"));Assert.That(r.Accepted,Is.True);Assert.That(r.Coup,Is.Not.Null);Assert.That(r.Event.Stage,Is.EqualTo(InfluenceEventStage.AwaitingChoice));Assert.That(r.View.Practice.Actor,Is.EqualTo(PlayerSide.US));Assert.That(r.View.Event.Owner,Is.EqualTo(PlayerSide.USSR));Assert.That(r.View.Practice.CompletedActions,Is.EqualTo(1));Assert.That(r.View.Practice.OpponentOps.Mode,Is.EqualTo(OperationsMode.Coup));Assert.That(r.View.Cards.ResolvingCount,Is.EqualTo(1));
            var q=h.UssrSession.QueryEventTargets(r.View.Event.ExecutionId,r.View.Version);var left=q.RequiredAmount;var allocations=new List<InfluenceAllocation>();foreach(var cap in q.Capacities){var n=Math.Min(left,cap.Amount);if(n>0)allocations.Add(new InfluenceAllocation(cap.Country,n));left-=n;}Assert.That(left,Is.Zero);var done=await h.UssrSession.SubmitEventAsync(EventCommandEnvelope.Allocate("choice",r.View.GameId,r.View.Version,r.View.Event.ExecutionId,r.View.Event.DecisionId,allocations));Assert.That(done.Accepted,Is.True);Assert.That(done.View.Practice.CompletedActions,Is.EqualTo(2));Assert.That(done.View.Cards.ResolvingCount,Is.Zero);Assert.That(done.View.Cards.RemovedCount,Is.EqualTo(1));Assert.That(done.View.Practice.UsMilitary,Is.EqualTo(3));
        }
        [TestCase(false)] [TestCase(true)] public async Task Event_panel_keeps_public_coup_summary_across_owner_switch_then_hides_after_resolution(bool switchOwner)
        {
            var s=PlayOpponentInfluenceTests.ReadyUS("card.suez_crisis");var board=s.Countries.ToDictionary(x=>x.Key,x=>x.Value);board[C("angola")]=new CountryState(3,3);
            var h=new LocalGameHost(PlayScoringTests.Replace(s,board),true);var c=new DemoController(_=>h);c.LoadScenario("test",PlayerSide.US,C("angola"));c.SelectCard(new CardId("card.suez_crisis"));
            Assert.That((await c.PlayCardAsync(CardActionKind.OpponentOpsFirst)).Accepted,Is.True);c.SelectCoupUse();var coup=await c.ChooseOpponentOperationsAsync(OperationsMode.Coup);
            Assert.That(coup.Accepted,Is.True);Assert.That(c.ReadView().Event.Stage,Is.EqualTo(InfluenceEventStage.AwaitingChoice));
            if(switchOwner){c.SelectSide(PlayerSide.USSR);Assert.That(c.LastCardResult,Is.Null);}
            var summary=c.EventPanelCoupMessage;Assert.That(summary,Is.Not.Null);Assert.That(summary,Does.Contain($"政变骰 {coup.Coup.Roll}").And.Contain($"OPS {coup.Coup.Strength}").And.Contain($"军事 +{coup.Coup.MilitaryCredit}"));
            var v=h.UssrSession.ReadView();var q=h.UssrSession.QueryEventTargets(v.Event.ExecutionId,v.Version);var left=q.RequiredAmount;var allocations=new List<InfluenceAllocation>();
            foreach(var cap in q.Capacities){var n=Math.Min(left,cap.Amount);if(n>0)allocations.Add(new InfluenceAllocation(cap.Country,n));left-=n;}
            Assert.That((await h.UssrSession.SubmitEventAsync(EventCommandEnvelope.Allocate("finish",v.GameId,v.Version,v.Event.ExecutionId,v.Event.DecisionId,allocations))).Accepted,Is.True);
            Assert.That(c.EventPanelCoupMessage,Is.Null);c.LoadScenario("test",PlayerSide.US,C("angola"));Assert.That(c.EventPanelCoupMessage,Is.Null);
        }
        [Test] public async Task Controller_can_preview_pending_coup_without_hand_selection_then_change_mode_and_complete()
        {
            var h=new LocalGameHost(PlayOpponentOpsTests.Ready(),true);var c=new DemoController(_=>h);c.LoadScenario("test",PlayerSide.USSR,C("angola"));c.SelectCard(new CardId("card.duck_and_cover"));Assert.That((await c.PlayCardAsync(CardActionKind.OpponentEventFirst)).Accepted,Is.True);Assert.That(c.SelectedCard,Is.Null);c.SelectCoupUse();Assert.That(c.SelectingCoup,Is.True);Assert.That(c.SelectedCard,Is.Null);Assert.That(c.LegalTargets().Error,Is.EqualTo(RuleError.None));Assert.That(c.LegalTargets().Targets,Does.Contain(C("angola")));Assert.That(c.SelectingCoup,Is.True);c.SelectSide(PlayerSide.US);Assert.That(c.SelectingCoup,Is.False);Assert.That((await c.ChooseOpponentOperationsAsync(OperationsMode.Realignment)).Error,Is.EqualTo(RuleError.WrongActor));c.SelectSide(PlayerSide.USSR);c.SelectCoupUse();Assert.That((await c.ChooseOpponentOperationsAsync(OperationsMode.Realignment)).Accepted,Is.True);Assert.That(c.SelectingCoup,Is.False);Assert.That((await c.RollRealignmentAsync()).Accepted,Is.True);Assert.That((await c.FinishRealignmentAsync()).Accepted,Is.True);Assert.That(c.ReadView().Practice.CompletedActions,Is.EqualTo(1));
        }
        [Test] public async Task Controller_real_default_hand_order_then_coup_routes_actual_active_card()
        {
            var catalog=PlayRealignmentCatalogTests.Catalog();var h=catalog.CreateHost(catalog.Scenarios.Last().Id);var c=new DemoController(_=>h);c.LoadScenario("quick",PlayerSide.USSR,C("iran"));c.SelectCard(new CardId("card.duck_and_cover"));Assert.That((await c.PlayCardAsync(CardActionKind.OpponentOpsFirst)).Accepted,Is.True);c.SelectCoupUse();Assert.That(c.LegalTargets().Error,Is.EqualTo(RuleError.None));Assert.That(c.LegalTargets().Targets,Does.Contain(C("iran")));var r=await c.ChooseOpponentOperationsAsync(OperationsMode.Coup);Assert.That(r.Accepted,Is.True);Assert.That(r.Coup.Target,Is.EqualTo(C("iran")));Assert.That(r.DefconVpEvent,Is.Not.Null);Assert.That(r.View.Practice.CompletedActions,Is.EqualTo(1));Assert.That(await c.RetryCardAsync(),Is.SameAs(r));
        }
        [Test] public async Task Failed_coup_followup_receipt_leaves_host_at_pending_mode_without_public_partial_result()
        {
            var s=PlayOpponentOpsTests.Start(PlayOpponentOpsTests.Ready("card.containment"),false).NextState;s=PlayScoringTests.Replace(s,version:long.MaxValue-2);var h=new LocalGameHost(s,true);var v=h.UssrSession.ReadView();var cmd=new CardActionCommand("overflow",v.GameId,v.Version,CardActionKind.UseOpponentCoup,target:C("angola"));var r=await h.UssrSession.SubmitCardActionAsync(cmd);Assert.That(r.Error,Is.EqualTo(RuleError.NumericOverflow));Assert.That(r.Coup,Is.Null);Assert.That(r.ActivatedOpsEffect,Is.Null);Assert.That(await h.UssrSession.SubmitCardActionAsync(cmd),Is.SameAs(r));Assert.That(h.UssrSession.ReadView().Version,Is.EqualTo(v.Version));Assert.That(h.UssrSession.ReadView().Practice.UssrMilitary,Is.Zero);Assert.That(h.UssrSession.ReadView().Practice.OpponentOps.Mode,Is.Null);
        }

        [Test] public async Task No_legal_coup_targets_does_not_trap_player_before_mode_confirmation()
        {
            var s=PlayOpponentOpsTests.Ready();s=PlayScoringTests.Replace(s,RegionScoringTests.Empty(s.Map));var h=new LocalGameHost(s,true);var c=new DemoController(_=>h);c.LoadScenario("empty",PlayerSide.USSR,C("iran"));c.SelectCard(new CardId("card.duck_and_cover"));Assert.That((await c.PlayCardAsync(CardActionKind.OpponentEventFirst)).Accepted,Is.True);c.SelectCoupUse();var q=c.LegalTargets();Assert.That(q.Error,Is.EqualTo(RuleError.None));Assert.That(q.Targets,Is.Empty);Assert.That((await c.ChooseOpponentOperationsAsync(OperationsMode.Coup)).Error,Is.EqualTo(RuleError.IllegalTarget));Assert.That(c.ReadView().Practice.Stage,Is.EqualTo(ActionPracticeStage.ChoosingOperations));Assert.That((await c.ChooseOpponentOperationsAsync(OperationsMode.Influence)).Accepted,Is.True);Assert.That((await c.FinishInfluenceAsync()).Accepted,Is.True);Assert.That(c.ReadView().Practice.CompletedActions,Is.EqualTo(1));
        }
    }
}
