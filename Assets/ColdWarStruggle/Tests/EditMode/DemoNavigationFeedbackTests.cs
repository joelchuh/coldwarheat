using System;
using System.Linq;
using System.Threading.Tasks;
using ColdWarStruggle.Core;
using ColdWarStruggle.Infrastructure;
using ColdWarStruggle.Presentation;
using NUnit.Framework;
using UnityEngine;
namespace ColdWarStruggle.Tests
{
    public class DemoNavigationFeedbackTests
    {
        static readonly CountryId Romania=new CountryId("country.romania");
        static readonly CardId Reds=new CardId("card.independent_reds");
        static DemoController Controller(DemoCatalog catalog)
        {var c=new DemoController(catalog.CreateHost);c.LoadScenario(IndependentRedsPlayTests.Scene,PlayerSide.USSR,Romania);return c;}

        [Test] public async Task Replaying_completed_event_keeps_original_receipt_but_displays_it_as_history()
        {
            var c=Controller(IndependentRedsPlayTests.Catalog());c.SelectCard(Reds);
            var original=await c.PlayCardAsync(CardActionKind.Event);
            Assert.That(original.Accepted,Is.True);Assert.That(c.CardResultHistoryMessage,Is.Null);
            c.SelectSide(PlayerSide.US);Assert.That(c.ToggleEventTarget(Romania),Is.True);
            Assert.That((await c.ConfirmEventAsync()).Accepted,Is.True);c.SelectSide(PlayerSide.USSR);
            var before=c.ReadView();Assert.That(await c.RetryCardAsync(),Is.SameAs(original));
            Assert.That(original.Event.Stage,Is.EqualTo(InfluenceEventStage.AwaitingChoice));
            Assert.That(c.CardResultHistoryMessage,Is.Not.Null);Assert.That(c.CardResultHistoryMessage,Does.Contain("历史"));
            Assert.That(c.ReadView().Version,Is.EqualTo(before.Version));Assert.That(c.ReadView().EventSequence,Is.EqualTo(before.EventSequence));
            Assert.That(c.ReadView().Event,Is.Null);Assert.That(c.ReadView().Countries.Single(x=>x.Definition.Id.Equals(Romania)).US,Is.EqualTo(4));
            c.SelectSide(PlayerSide.US);Assert.That(c.CardResultHistoryMessage,Is.Null);
        }

        [Test] public async Task Replaying_current_wait_does_not_hide_pending_choice()
        {
            var c=Controller(IndependentRedsPlayTests.Catalog());c.SelectCard(Reds);
            var original=await c.PlayCardAsync(CardActionKind.Event);Assert.That(await c.RetryCardAsync(),Is.SameAs(original));
            Assert.That(c.CardResultHistoryMessage,Is.Null);Assert.That(c.ReadView().Event.Stage,Is.EqualTo(InfluenceEventStage.AwaitingChoice));
        }

        [Test] public void Browsing_and_cancelling_preserves_authority_selected_card_and_identity()
        {
            var d=IndependentRedsPlayTests.Catalog();var c=Controller(d);var go=new GameObject("picker-test");
            try
            {
                var v=Initialize(go,c,d);c.SelectCard(Reds);var before=c.ReadView();
                v.OpenScenarioPicker();Assert.That(v.IsScenarioPickerOpen,Is.True);v.CancelScenarioPicker();
                Assert.That(v.IsScenarioPickerOpen,Is.False);Assert.That(c.ReadView().GameId,Is.EqualTo(before.GameId));
                Assert.That(c.ReadView().Version,Is.EqualTo(before.Version));Assert.That(c.SelectedCard,Is.EqualTo(Reds));Assert.That(c.SelectedSide,Is.EqualTo(PlayerSide.USSR));
            }
            finally{UnityEngine.Object.DestroyImmediate(go);}
        }

        [TestCase(false)][TestCase(true)] public void Choosing_a_practice_starts_fresh_and_clears_old_commands_even_for_same_scene(bool same)
        {
            var d=IndependentRedsPlayTests.Catalog();var c=Controller(d);var go=new GameObject("picker-test");
            try
            {
                var v=Initialize(go,c,d);c.SelectCard(Reds);var old=c.ReadView().GameId;
                v.OpenScenarioPicker();int target=same?d.Scenarios.Count-1:0;v.SelectScenario(target);
                Assert.That(v.IsScenarioPickerOpen,Is.False);Assert.That(c.ReadView().GameId,Is.Not.EqualTo(old));
                Assert.That(c.SelectedCard,Is.Null);Assert.That(c.CanRetryCard,Is.False);Assert.That(c.SelectedCountry,Is.EqualTo(d.Scenarios[target].SelectedCountry));
                Assert.That(c.SelectedSide,Is.EqualTo(d.Scenarios[target].Actor));Assert.That(c.ReadView().Practice!=null,Is.EqualTo(same));
            }
            finally{UnityEngine.Object.DestroyImmediate(go);}
        }

        [TestCase(-1)][TestCase(22)] public void Invalid_choice_does_not_reset_or_close_current_game(int index)
        {
            var d=IndependentRedsPlayTests.Catalog();var c=Controller(d);var go=new GameObject("picker-test");
            try
            {
                var v=Initialize(go,c,d);var before=c.ReadView();v.OpenScenarioPicker();
                Assert.Throws<ArgumentOutOfRangeException>(()=>v.SelectScenario(index));
                Assert.That(v.IsScenarioPickerOpen,Is.True);Assert.That(c.ReadView().GameId,Is.EqualTo(before.GameId));Assert.That(c.ReadView().Version,Is.EqualTo(before.Version));
            }
            finally{UnityEngine.Object.DestroyImmediate(go);}
        }
        static DemoView Initialize(GameObject go,DemoController controller,DemoCatalog catalog)
        {
            var view=go.AddComponent<DemoView>();view.Initialize(controller,catalog.Names,catalog.Scenarios.Select(s=>new DemoScenarioOption(s.Id,s.Title,s.Actor,s.SelectedCountry)).ToArray(),catalog.Scenarios.Count-1,printedCards:catalog.PrintedCards);return view;
        }
    }
}
