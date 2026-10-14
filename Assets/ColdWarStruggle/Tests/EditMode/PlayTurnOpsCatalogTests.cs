using System;
using System.Linq;
using System.Threading.Tasks;
using ColdWarStruggle.Core;
using ColdWarStruggle.Application;
using ColdWarStruggle.Infrastructure;
using ColdWarStruggle.Presentation;
using Newtonsoft.Json;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class PlayTurnOpsCatalogTests
    {
        static DemoCatalog Before()=>PlayDefconVpCatalogTests.Add(PlayInfluenceEventCatalogTests.Add(PlayScoringCatalogTests.Catalog()));
        internal static DemoCatalog Add(DemoCatalog c)=>c.WithPracticeTurnOpsEvents(TurnOpsEffectTests.Data(),TurnOpsEffectTests.Schema());
        internal static DemoCatalog Catalog()=>Add(Before()).WithQuickStartScenario(QuickStartDemoTests.Data());
        [Test] public void Catalog_preserves_existing_rules_deal_manual_setup_and_reuses_definition_for_quick_start()
        {
            var before=Before();var first=before.Scenarios.Single(s=>s.PracticeDefinition!=null);var after=Add(before);var manual=after.Scenarios.Single(s=>s.PracticeDefinition!=null);var d=manual.PracticeDefinition;
            Assert.That(d.InfluenceEvents,Is.SameAs(first.PracticeDefinition.InfluenceEvents));Assert.That(d.DefconVpEvents,Is.SameAs(first.PracticeDefinition.DefconVpEvents));Assert.That(d.Risk,Is.SameAs(first.PracticeDefinition.Risk));Assert.That(d.Ops,Is.SameAs(first.PracticeDefinition.Ops));Assert.That(d.TurnOpsEvents.Effects.Count,Is.EqualTo(2));Assert.That(manual.InitialCards,Is.SameAs(first.InitialCards));Assert.That(after.CreateHost(manual.Id).UssrSession.ReadView().Deployment.Complete,Is.False);
            var q=after.WithQuickStartScenario(QuickStartDemoTests.Data());Assert.That(q.Scenarios.Last().PracticeDefinition,Is.SameAs(d));Assert.That(q.Scenarios.Count,Is.EqualTo(before.Scenarios.Count+1));
        }
        [Test] public void Catalog_rejects_repeated_and_wrong_order_assembly()
        {Assert.Throws<ArgumentException>(()=>Add(PlayScoringCatalogTests.Catalog()));var c=Add(Before());Assert.Throws<ArgumentException>(()=>Add(c));Assert.Throws<ArgumentException>(()=>Add(Before().WithQuickStartScenario(QuickStartDemoTests.Data())));}
        [Test] public async Task Actual_default_route_exposes_containment_then_modified_US_ops_without_opponent_hand_leak()
        {
            var c=Catalog();var h=c.CreateHost(c.Scenarios.Last().Id);var su=h.UssrSession;var us=h.UsSession;var v=su.ReadView();Assert.That(v.Practice.OpsEffects,Is.Empty);
            var duck=await su.SubmitCardActionAsync(new CardActionCommand("duck",v.GameId,v.Version,CardActionKind.Event,new CardId("card.duck_and_cover")));Assert.That(duck.Accepted,Is.True);v=us.ReadView();
            var r=await us.SubmitCardActionAsync(new CardActionCommand("contain",v.GameId,v.Version,CardActionKind.Event,new CardId("card.containment")));Assert.That(r.Accepted,Is.True);Assert.That(r.ActivatedOpsEffect.Card.Value,Is.EqualTo("card.containment"));Assert.That(r.ActivatedOpsEffect.AffectedSide,Is.EqualTo(PlayerSide.US));Assert.That(r.ActivatedOpsEffect.Delta,Is.EqualTo(1));Assert.That(r.View.Cards.RemovedCount,Is.EqualTo(1));Assert.That(su.ReadView().Practice.OpsEffects.Count,Is.EqualTo(1));
            var option=us.QueryCardActions().Cards.Single(x=>x.Card.Value=="card.cia_created");Assert.That(option.PrintedOps,Is.EqualTo(1));Assert.That(option.CurrentOps,Is.EqualTo(2));Assert.That(option.FlatModifierTotal,Is.EqualTo(1));Assert.That(option.Influence,Is.EqualTo(RuleError.WrongActor));
            var scoring=us.QueryCardActions().Cards.Single(x=>x.Card.Value=="card.europe_scoring");Assert.That(scoring.PrintedOps,Is.Null);Assert.That(scoring.CurrentOps,Is.Null);
            Assert.That(su.QueryCardActions().Cards.Select(x=>x.Card.Value),Is.EquivalentTo(su.ReadView().OwnHand));var json=JsonConvert.SerializeObject(r.ActivatedOpsEffect);Assert.That(json,Does.Not.Contain("Definition").And.Not.Contain("Random").And.Not.Contain("Resolution").And.Not.Contain("Hand"));
            Assert.That(r.View.Practice.OpsEffects.Single().ExpiryBoundary,Is.EqualTo("ExpireAndReset"));Assert.That(r.View.Practice.OpsEffects.Single().Turn,Is.EqualTo(1));
        }
        [Test] public async Task Controller_event_feedback_and_public_query_follow_actor_switch_and_idempotent_retry()
        {
            var h=new LocalGameHost(PlayTurnOpsEventTests.Ready("card.red_scare_purge"),true);var c=new DemoController(_=>h);c.LoadScenario("fixture",PlayerSide.USSR,new CountryId("country.iran"));c.SelectCard(new CardId("card.red_scare_purge"));var r=await c.PlayCardAsync(CardActionKind.Event);Assert.That(r.Accepted,Is.True);Assert.That(c.LastCardResult.ActivatedOpsEffect.Delta,Is.EqualTo(-1));Assert.That(await c.RetryCardAsync(),Is.SameAs(r));c.SelectSide(PlayerSide.US);var option=c.CardOptions().Cards.Single(x=>x.Card.Value=="card.defectors");Assert.That(option.CurrentOps,Is.EqualTo(1));Assert.That(option.Space,Is.EqualTo(RuleError.SpaceAttemptUnavailable));Assert.That(c.ReadView().Practice.OpsEffects.Count,Is.EqualTo(1));
        }
        [Test] public void Complete_single_turn_does_not_implicitly_expire_effect_or_open_next_turn()
        {
            var s=PlayTurnOpsEventTests.Play(PlayTurnOpsEventTests.Ready(),"card.containment").NextState;var effects=s.Practice.OpsEffects;
            while(s.Practice.Stage!=ActionPracticeStage.Complete)
            {
                s=PlayTurnOpsEventTests.Give(s,s.Practice.Actor,"card.olympic_games");var p=ActionPracticeRules.Plan(s,s.Practice.Actor,CardActionKind.Influence,"ops",new CardId("card.olympic_games"));Assert.That(p.Accepted,Is.True);s=p.NextState;
                var target=new CountryId(s.Practice.Actor==PlayerSide.US?"country.france":"country.east_germany");var place=InfluenceRules.PlanPlacement(s,s.Operations,new[]{target});Assert.That(place.Accepted,Is.True);s=place.NextState;Assert.That(s.Practice.OpsEffects,Is.SameAs(effects));
                var done=ActionPracticeRules.Plan(s,s.Practice.Actor,CardActionKind.FinishInfluence,"finish");Assert.That(done.Accepted,Is.True);s=done.NextState;
            }
            Assert.That(s.Turn.Number,Is.EqualTo(1));Assert.That(s.Practice.OpsEffects,Is.SameAs(effects));Assert.That(s.Practice.CompletedActions,Is.EqualTo(12));Assert.That(s.Ended,Is.False);
        }
        [Test] public void Both_flat_effects_on_US_cancel_before_lower_bound_in_actual_followup_action()
        {
            var s=PlayTurnOpsEventTests.Play(PlayTurnOpsEventTests.Ready(),"card.containment").NextState;
            s=PlayTurnOpsEventTests.Give(s,PlayerSide.US,"card.olympic_games");s=ActionPracticeRules.Plan(s,PlayerSide.US,CardActionKind.Influence,"ops",new CardId("card.olympic_games")).NextState;s=ActionPracticeRules.Plan(s,PlayerSide.US,CardActionKind.FinishInfluence,"finish").NextState;
            s=PlayTurnOpsEventTests.Give(s,PlayerSide.USSR,"card.red_scare_purge");s=PlayTurnOpsEventTests.Play(s,"card.red_scare_purge").NextState;s=PlayTurnOpsEventTests.Give(s,PlayerSide.US,"card.cia_created");var p=ActionPracticeRules.Plan(s,PlayerSide.US,CardActionKind.Influence,"ops",new CardId("card.cia_created"));Assert.That(p.Accepted,Is.True);Assert.That(p.NextState.Operations.Budget,Is.EqualTo(1));Assert.That(p.NextState.Practice.OpsEffects.Instances.Count,Is.EqualTo(2));
        }
    }
}
