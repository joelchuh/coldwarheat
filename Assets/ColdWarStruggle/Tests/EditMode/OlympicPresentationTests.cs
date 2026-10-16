using System;
using System.Threading.Tasks;
using ColdWarStruggle.Core;
using ColdWarStruggle.Application;
using ColdWarStruggle.Presentation;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public sealed class OlympicPresentationTests
    {
        static DemoController Controller(GameState s,PlayerSide side)
        {var c=new DemoController(_=>new LocalGameHost(s,true));c.LoadScenario("fixture",side,new CountryId("country.angola"));return c;}
        [Test] public async Task Side_switch_controls_only_response_identity_and_internal_pump_keeps_original_retry()
        {
            var c=Controller(OlympicPlayTests.Ready(PlayerSide.US,0,5,1,3,6,1),PlayerSide.US);c.SelectCard(OlympicPlayTests.Card);Assert.That((await c.PlayCardAsync(CardActionKind.Event)).Accepted,Is.True);
            Assert.That((await c.RespondOlympicAsync(true)).Accepted,Is.False);c.SelectSide(PlayerSide.USSR);var tie=await c.RespondOlympicAsync(true);Assert.That(tie.Accepted,Is.True);
            var done=c.AdvanceInternalOlympic();Assert.That(done.Olympic.LastPair.Round,Is.EqualTo(2));var current=c.ReadView();Assert.That(await c.RetryCardAsync(),Is.SameAs(tie));Assert.That(c.ReadView().Version,Is.EqualTo(current.Version));Assert.That(c.CardResultHistoryMessage,Is.Not.Null);
        }
        [TestCase(OperationsMode.Influence)][TestCase(OperationsMode.Realignment)]
        public async Task Controller_selects_event_operations_without_a_hand_selection(OperationsMode mode)
        {
            var c=Controller(OlympicOperationsTests.Pending(),PlayerSide.USSR);Assert.That(c.SelectedCard,Is.Null);var r=await c.ChooseOlympicOperationsAsync(mode);Assert.That(r.Accepted,Is.True);Assert.That(c.ReadView().RemainingOps,Is.EqualTo(4));Assert.That(c.ReadView().Olympic.Mode,Is.EqualTo(mode));
        }
        [Test] public async Task Controller_coup_and_decline_keep_live_public_result()
        {
            var c=Controller(OlympicOperationsTests.Pending(level:3),PlayerSide.USSR);c.SelectCoupUse();Assert.That(c.SelectedCoupQuote.Strength,Is.EqualTo(4));var r=await c.ChooseOlympicOperationsAsync(OperationsMode.Coup);Assert.That(r.Olympic.Terminal,Is.True);Assert.That(r.Olympic.DefconAfter,Is.EqualTo(1));
            c=Controller(OlympicOperationsTests.Pending(),PlayerSide.USSR);Assert.That((await c.DeclineOlympicOperationsAsync()).Olympic.Outcome,Is.EqualTo(OlympicPracticeOutcome.OperationsDeclined));
        }
    }
}
