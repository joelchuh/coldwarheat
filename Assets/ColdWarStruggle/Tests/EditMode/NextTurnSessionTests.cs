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
    public class NextTurnSessionTests
    {
        internal static DemoCatalog Catalog()=>TurnEndSessionTests.Before().WithPracticeTurnEnd().WithPracticeNextTurn().WithQuickStartScenario(QuickStartDemoTests.Data());
        [Test] public async Task Next_turn_command_is_fixed_identity_idempotent_and_preserves_private_views()
        {
            var h=new LocalGameHost(NextTurnPlayTests.Assessed(),true);var session=h.UsSession;var v=session.ReadView();Assert.That(v.Practice.NextTurnAvailable,Is.True);
            var command=new CardActionCommand("next",v.GameId,v.Version,CardActionKind.BeginNextTurn);var r=await session.SubmitCardActionAsync(command);Assert.That(r.Accepted,Is.True);
            Assert.That(await session.SubmitCardActionAsync(command),Is.SameAs(r));Assert.That(session.ReadView().Version,Is.EqualTo(v.Version+1));Assert.That(r.View.Turn.Number,Is.EqualTo(2));Assert.That(r.View.Practice.NextTurnAvailable,Is.False);
            var opponent=h.UssrSession.ReadView();Assert.That(r.View.OwnHand,Has.Count.EqualTo(8));Assert.That(opponent.OwnHand,Has.Count.EqualTo(8));Assert.That(r.View.OwnHand.Intersect(opponent.OwnHand),Is.Empty);
            var text=JsonConvert.SerializeObject(r.View);foreach(var id in opponent.OwnHand)Assert.That(text,Does.Not.Contain(id));Assert.That(text,Does.Not.Contain("RandomState").And.Not.Contain("\"EraReserve\":"));
            Assert.That((await session.SubmitCardActionAsync(new CardActionCommand("next",v.GameId,v.Version,CardActionKind.AssessTurnEnd))).Error,Is.EqualTo(RuleError.CommandIdConflict));
            Assert.That((await session.SubmitCardActionAsync(new CardActionCommand("stale",v.GameId,v.Version,CardActionKind.BeginNextTurn))).Error,Is.EqualTo(RuleError.StaleVersion));
            Assert.That((await h.UssrSession.SubmitCardActionAsync(new CardActionCommand("other",opponent.GameId,opponent.Version,CardActionKind.BeginNextTurn))).Error,Is.EqualTo(RuleError.ActionNotReady));
        }
        [Test] public async Task Default_deal_twelve_actions_assessment_next_turn_and_new_action_use_actual_host()
        {
            var catalog=Catalog();var h=catalog.CreateHost(catalog.Scenarios.Last().Id);var c=new DemoController(_=>h);c.LoadScenario("route",PlayerSide.USSR,ChinaCardPlayTests.C("iran"));
            foreach(var use in TurnEndSessionTests.DefaultRoute)
            {c.SelectSide(c.ReadView().Practice.Actor);c.SelectCard(new CardId("card."+use.card));Assert.That((await c.PlayCardAsync(use.kind)).Accepted,Is.True,use.card);if(use.kind==CardActionKind.Influence)Assert.That((await c.FinishInfluenceAsync()).Accepted,Is.True);}
            Assert.That((await c.AssessTurnEndAsync()).Accepted,Is.True);var before=c.ReadView();Assert.That(before.Practice.NextTurnAvailable,Is.True);Assert.That(before.Practice.UssrAttempts,Is.EqualTo(1));Assert.That(c.TurnEndMessage,Does.Contain("跳过头条"));
            var result=await c.BeginNextTurnAsync();Assert.That(result.Accepted,Is.True);Assert.That(await c.RetryCardAsync(),Is.SameAs(result));var next=c.ReadView();
            Assert.That(next.Turn.Number,Is.EqualTo(2));Assert.That(next.Practice.UssrAttempts,Is.Zero);Assert.That(next.Practice.UssrSpace,Is.EqualTo(before.Practice.UssrSpace));Assert.That(next.Practice.VpForUS,Is.EqualTo(before.Practice.VpForUS));Assert.That(next.Practice.OpsEffects,Is.Empty);Assert.That(next.Practice.Actor,Is.EqualTo(PlayerSide.USSR));Assert.That(next.Practice.TurnEnd,Is.Null);Assert.That(c.TurnEndMessage,Is.Null);Assert.That(next.Cards.DrawPileCount,Is.EqualTo(8));
            c.SelectSide(PlayerSide.USSR);var card=c.ReadView().OwnHand.Select(id=>catalog.PrintedCards.GetCard(new CardId(id))).First(d=>d.Kind==CardKind.Event && d.Affiliation!=CardAffiliation.US);c.SelectCard(card.Id);
            Assert.That((await c.PlayCardAsync(CardActionKind.Influence)).Accepted,Is.True);Assert.That((await c.FinishInfluenceAsync()).Accepted,Is.True);Assert.That(c.ReadView().Practice.CompletedActions,Is.EqualTo(1));Assert.That(c.ReadView().Practice.Actor,Is.EqualTo(PlayerSide.US));
        }
        [Test] public void Next_turn_catalog_preserves_prior_definitions_and_rejects_wrong_order()
        {
            var before=TurnEndSessionTests.Before().WithPracticeTurnEnd();var old=before.Scenarios.Single(s=>s.PracticeDefinition!=null);var after=before.WithPracticeNextTurn();var updated=after.Scenarios.Single(s=>s.PracticeDefinition!=null);
            Assert.That(updated.PracticeDefinition.NextTurnEnabled,Is.True);Assert.That(updated.PracticeDefinition.Cards,Is.SameAs(old.PracticeDefinition.Cards));Assert.That(updated.InitialCards,Is.SameAs(old.InitialCards));Assert.That(updated.PracticeDefinition.TurnEndEnabled,Is.True);
            Assert.Throws<ArgumentException>(()=>after.WithPracticeNextTurn());Assert.Throws<ArgumentException>(()=>TurnEndSessionTests.Before().WithPracticeNextTurn());Assert.Throws<ArgumentException>(()=>TurnEndSessionTests.Catalog().WithPracticeNextTurn());
        }
    }
}
