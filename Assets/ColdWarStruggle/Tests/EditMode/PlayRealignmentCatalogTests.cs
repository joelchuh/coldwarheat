using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using ColdWarStruggle.Core;
using ColdWarStruggle.Application;
using ColdWarStruggle.Infrastructure;
using ColdWarStruggle.Presentation;
using Newtonsoft.Json;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class PlayRealignmentCatalogTests
    {
        static DemoCatalog Before()=>PlayTurnOpsCatalogTests.Add(PlayDefconVpCatalogTests.Add(PlayInfluenceEventCatalogTests.Add(PlayScoringCatalogTests.Catalog())));
        internal static DemoCatalog Add(DemoCatalog c)=>c.WithPracticeRealignment(RealignmentTests.Data());
        internal static DemoCatalog Catalog()=>Add(Before()).WithQuickStartScenario(QuickStartDemoTests.Data());
        [Test] public void Catalog_keeps_twelve_events_and_existing_rules_and_real_deal()
        {
            var before=Before();var first=before.Scenarios.Single(s=>s.PracticeDefinition!=null);var after=Add(before);var manual=after.Scenarios.Single(s=>s.PracticeDefinition!=null);var d=manual.PracticeDefinition;Assert.That(d.TurnOpsEvents,Is.SameAs(first.PracticeDefinition.TurnOpsEvents));Assert.That(d.InfluenceEvents,Is.SameAs(first.PracticeDefinition.InfluenceEvents));Assert.That(d.DefconVpEvents,Is.SameAs(first.PracticeDefinition.DefconVpEvents));Assert.That(d.Risk,Is.SameAs(first.PracticeDefinition.Risk));Assert.That(d.Realignment.OpsCostPerRoll,Is.EqualTo(1));Assert.That(manual.InitialCards,Is.SameAs(first.InitialCards));Assert.That(after.CreateHost(manual.Id).UssrSession.ReadView().Deployment.Complete,Is.False);Assert.That(after.WithQuickStartScenario(QuickStartDemoTests.Data()).Scenarios.Last().PracticeDefinition,Is.SameAs(d));
        }
        [Test] public void Catalog_rejects_wrong_order_duplicate_or_after_quick_start()
        {Assert.Throws<ArgumentException>(()=>Add(PlayScoringCatalogTests.Catalog()));Assert.Throws<ArgumentException>(()=>Add(Add(Before())));Assert.Throws<ArgumentException>(()=>Add(Before().WithQuickStartScenario(QuickStartDemoTests.Data())));}
        [Test] public async Task Actual_default_hand_can_start_roll_then_finish_with_next_actor_and_discard()
        {
            var c=Catalog();var h=c.CreateHost(c.Scenarios.Last().Id);var session=h.UssrSession;var v=session.ReadView();var id=new CardId("card.olympic_games");Assert.That(session.QueryCardActions().Cards.Single(x=>x.Card.Equals(id)).Realignment,Is.EqualTo(RuleError.None));
            var begin=await session.SubmitCardActionAsync(new CardActionCommand("start",v.GameId,v.Version,CardActionKind.Realignment,id));Assert.That(begin.Accepted,Is.True);v=begin.View;Assert.That(v.RemainingOps,Is.EqualTo(2));var q=session.QueryRealignmentTargets(v.ContextId,v.Version);Assert.That(q.Error,Is.EqualTo(RuleError.None));Assert.That(q.Targets,Does.Contain(RealignmentTests.C("iran")));Assert.That(q.Targets,Has.No.Member(RealignmentTests.C("poland")));
            var roll=await session.SubmitCardActionAsync(new CardActionCommand("roll",v.GameId,v.Version,CardActionKind.RollRealignment,target:RealignmentTests.C("iran")));Assert.That(roll.Accepted,Is.True);Assert.That(roll.Realignment,Is.Not.Null);Assert.That(roll.View.RemainingOps,Is.EqualTo(1));Assert.That(roll.View.Practice.Defcon,Is.EqualTo(5));Assert.That(roll.View.Practice.UssrMilitary,Is.Zero);v=roll.View;
            var end=await session.SubmitCardActionAsync(new CardActionCommand("end",v.GameId,v.Version,CardActionKind.FinishRealignment));Assert.That(end.Accepted,Is.True);Assert.That(end.View.Practice.Actor,Is.EqualTo(PlayerSide.US));Assert.That(end.View.Cards.DiscardCount,Is.EqualTo(1));Assert.That(end.View.Cards.ResolvingCount,Is.Zero);Assert.That(end.View.Practice.CompletedActions,Is.EqualTo(1));
        }
        [Test] public async Task Public_result_has_named_dice_and_modifier_detail_but_no_private_random_or_hand()
        {
            var h=new LocalGameHost(PlayRealignmentTests.Started(),true);var session=h.UssrSession;var v=session.ReadView();var cmd=new CardActionCommand("roll",v.GameId,v.Version,CardActionKind.RollRealignment,target:RealignmentTests.C("angola"));var r=await session.SubmitCardActionAsync(cmd);Assert.That(r.Accepted,Is.True);Assert.That(r.Realignment.UsRoll,Is.EqualTo(4));Assert.That(r.Realignment.UssrRoll,Is.EqualTo(4));Assert.That(r.Realignment.US.Total,Is.Zero);Assert.That(r.Realignment.USSR.Total,Is.Zero);Assert.That(r.Realignment.UsTotal,Is.EqualTo(4));Assert.That(r.Realignment.Removed,Is.Zero);Assert.That(r.Realignment.RemovedSide,Is.Null);Assert.That(await session.SubmitCardActionAsync(cmd),Is.SameAs(r));var json=JsonConvert.SerializeObject(r.Realignment);Assert.That(json,Does.Not.Contain("Random").And.Not.Contain("Seed").And.Not.Contain("Definition").And.Not.Contain("Hand"));
        }
        [Test] public async Task Target_query_rejects_stale_context_side_and_exhausted_budget_without_hidden_targets()
        {
            var h=new LocalGameHost(PlayRealignmentTests.Started(),true);var s=h.UssrSession;var v=s.ReadView();Assert.That(s.QueryRealignmentTargets("old",v.Version).Error,Is.EqualTo(RuleError.StaleContext));Assert.That(s.QueryRealignmentTargets(v.ContextId,v.Version-1).Error,Is.EqualTo(RuleError.StaleVersion));var wrong=h.UsSession.QueryRealignmentTargets(v.ContextId,v.Version);Assert.That(wrong.Error,Is.EqualTo(RuleError.WrongActor));Assert.That(wrong.Targets,Is.Empty);
            for(int i=0;i<2;i++){v=s.ReadView();Assert.That((await s.SubmitCardActionAsync(new CardActionCommand("roll"+i,v.GameId,v.Version,CardActionKind.RollRealignment,target:RealignmentTests.C("angola")))).Accepted,Is.True);}v=s.ReadView();var q=s.QueryRealignmentTargets(v.ContextId,v.Version);Assert.That(q.Error,Is.EqualTo(RuleError.InsufficientBudget));Assert.That(q.Targets,Is.Empty);Assert.That(s.QueryInfluenceTargets(v.ContextId,v.Version).Error,Is.EqualTo(RuleError.ModeMismatch));
        }
        [Test] public async Task Controller_and_replaceable_map_follow_realignment_stage_without_rules_in_view()
        {
            var h=new LocalGameHost(PlayRealignmentTests.Ready(),true);var c=new DemoController(_=>h);c.LoadScenario("fixture",PlayerSide.USSR,RealignmentTests.C("iran"));c.SelectCard(new CardId("card.olympic_games"));Assert.That((await c.PlayCardAsync(CardActionKind.Realignment)).Accepted,Is.True);Assert.That(c.SelectedCard,Is.Null);
            using var presenter=new MapPresenter(c,new Dictionary<string,string>());presenter.Attach(DrawableMapView.CreateView(DiagnosticMapKind.Tiles,null));var frame=presenter.Refresh();Assert.That(frame.Countries.Single(x=>x.Id.Equals(RealignmentTests.C("iran"))).Allowed,Is.True);Assert.That(frame.Countries.Single(x=>x.Id.Equals(RealignmentTests.C("poland"))).Allowed,Is.False);
            var result=await c.RollRealignmentAsync();Assert.That(result.Accepted,Is.True);Assert.That(c.LastCardResult.Realignment,Is.Not.Null);Assert.That(await c.RetryCardAsync(),Is.SameAs(result));Assert.That(c.ReadView().RemainingOps,Is.EqualTo(1));presenter.Attach(DrawableMapView.CreateView(DiagnosticMapKind.Table,null));Assert.That(presenter.Refresh().Version,Is.EqualTo(c.ReadView().Version));Assert.That((await c.FinishRealignmentAsync()).Accepted,Is.True);Assert.That(c.ReadView().Practice.CompletedActions,Is.EqualTo(1));
        }
    }
}
