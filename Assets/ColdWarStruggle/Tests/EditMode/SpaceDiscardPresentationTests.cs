using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ColdWarStruggle.Core;
using ColdWarStruggle.Application;
using ColdWarStruggle.Presentation;
using ColdWarStruggle.Editor;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class SpaceDiscardPresentationTests
    {
        static DemoController Controller(GameState state,PlayerSide side)
        {var h=new LocalGameHost(state,true);var c=new DemoController(_=>h);c.LoadScenario("space",side,new CountryId("country.iran"));return c;}
        [TestCase(PlayerSide.US,true)] [TestCase(PlayerSide.US,false)] [TestCase(PlayerSide.USSR,true)] [TestCase(PlayerSide.USSR,false)]
        public async Task Controller_uses_own_choice_and_retry_without_an_extra_action(PlayerSide owner,bool discard)
        {
            var c=Controller(SpaceEndTurnDiscardTests.Assessed(owner),owner);var v=c.ReadView();Assert.That(c.PracticePhaseText,Does.Contain("太空弃牌").And.Contain(owner==PlayerSide.US?"美国":"苏联"));
            Assert.That(c.CanDiscardSelectedHeldCard,Is.False);Assert.That(c.CanSelectChinaCard,Is.False);Assert.That(c.CanSelectHandCard(SpaceEndTurnDiscardTests.Held),Is.True);
            c.SelectCard(SpaceEndTurnDiscardTests.Held);Assert.That(c.CanDiscardSelectedHeldCard,Is.True);
            var r=discard?await c.DiscardSelectedHeldCardAsync():await c.KeepHeldCardsAsync();Assert.That(r.Accepted,Is.True,r.Error.ToString());Assert.That(await c.RetryCardAsync(),Is.SameAs(r));
            Assert.That(c.SelectedCard,Is.Null);Assert.That(c.CanDiscardSelectedHeldCard,Is.False);Assert.That(c.CanSelectHandCard(SpaceEndTurnDiscardTests.OtherHeld),Is.False);Assert.That(c.CanSelectChinaCard,Is.False);
            Assert.That(c.ReadView().Version,Is.EqualTo(v.Version+1));Assert.That(c.ReadView().Practice.CompletedActions,Is.EqualTo(14));Assert.That(c.ReadView().Practice.NextTurnAvailable,Is.True);
            Assert.That(c.TurnEndMessage,Does.Contain(discard?"已弃一张":"保留全部"));Assert.That(c.PracticePhaseText,Does.Not.Contain("太空弃牌"));
            c.SelectSide(owner==PlayerSide.US?PlayerSide.USSR:PlayerSide.US);Assert.That(c.LastCardResult,Is.Null);Assert.That(c.CanRetryCard,Is.False);Assert.That(c.ReadView().SpaceDiscard.Candidates,Is.Empty);
        }
        [TestCase(PlayerSide.US)] [TestCase(PlayerSide.USSR)] public async Task Opponent_has_no_candidates_or_buttons_and_switch_clears_selection(PlayerSide owner)
        {
            var other=owner==PlayerSide.US?PlayerSide.USSR:PlayerSide.US;var s=PlayTurnOpsEventTests.Give(SpaceEndTurnDiscardTests.Completed(owner),other,"card.olympic_games");
            s=NextTurnPlayTests.Apply(s,CardActionKind.AssessTurnEnd);var c=Controller(s,owner);c.SelectCard(SpaceEndTurnDiscardTests.Held);c.SelectSide(other);
            Assert.That(c.SelectedCard,Is.Null);Assert.That(c.CanDiscardSelectedHeldCard,Is.False);Assert.That(c.CanSelectHandCard(new CardId("card.olympic_games")),Is.False);Assert.That(c.CanSelectChinaCard,Is.False);
            Assert.That(JsonConvert.SerializeObject(c.ReadView().SpaceDiscard),Does.Not.Contain("card.blockade").And.Not.Contain("card.duck_and_cover"));
            var r=await c.KeepHeldCardsAsync();Assert.That(r.Error,Is.EqualTo(RuleError.WrongActor));Assert.That(r.View.Version,Is.EqualTo(s.Version));
            c.SelectSide(owner);Assert.That(c.SelectedCard,Is.Null);Assert.That(c.CanSelectHandCard(SpaceEndTurnDiscardTests.Held),Is.True);
        }
        [Test] public void Missing_or_stale_selected_card_cannot_submit_a_discard()
        {
            var s=SpaceEndTurnDiscardTests.Assessed();var h=new LocalGameHost(s,true);var c=new DemoController(_=>h);c.LoadScenario("space",PlayerSide.US,new CountryId("country.iran"));
            Assert.ThrowsAsync<InvalidOperationException>(async()=>await c.DiscardSelectedHeldCardAsync());c.SelectCard(SpaceEndTurnDiscardTests.Held);
            var v=h.UsSession.ReadView();h.UsSession.SubmitCardActionAsync(new CardActionCommand("external",v.GameId,v.Version,CardActionKind.KeepHeldCards)).GetAwaiter().GetResult();
            Assert.That(c.CanDiscardSelectedHeldCard,Is.False);Assert.ThrowsAsync<InvalidOperationException>(async()=>await c.DiscardSelectedHeldCardAsync());
        }
        [TestCase(true)] [TestCase(false)] public async Task Final_prompt_waits_for_space_decision_then_accepts_either_choice(bool discard)
        {
            var c=Controller(SpaceEndTurnDiscardTests.Assessed(PlayerSide.USSR,10),PlayerSide.USSR);
            Assert.That(c.FinalScoringPrompt,Does.Contain("太空").And.Contain("选择"));Assert.That(c.ReadView().Practice.FinalScoringAvailable,Is.False);
            c.SelectCard(SpaceEndTurnDiscardTests.Held);if(discard)await c.DiscardSelectedHeldCardAsync();else await c.KeepHeldCardsAsync();
            Assert.That(c.ReadView().Practice.FinalScoringAvailable,Is.True);Assert.That(c.FinalScoringPrompt,Does.Contain("最终计分"));Assert.That((await c.CompleteFinalScoringAsync()).Accepted,Is.True);
            Assert.That(c.CanSelectHandCard(SpaceEndTurnDiscardTests.OtherHeld),Is.False);Assert.That(c.CanSelectChinaCard,Is.False);
        }
        [Test] public void Looking_at_map_does_not_change_pending_candidate_or_version()
        {
            var c=Controller(SpaceEndTurnDiscardTests.Assessed(),PlayerSide.US);c.SelectCard(SpaceEndTurnDiscardTests.Held);var v=c.ReadView();
            c.SelectCountry(new CountryId("country.japan"));Assert.That(c.SelectedCard,Is.EqualTo(SpaceEndTurnDiscardTests.Held));Assert.That(c.CanDiscardSelectedHeldCard,Is.True);Assert.That(c.ReadView().Version,Is.EqualTo(v.Version));Assert.That(c.ReadView().SpaceDiscard.Candidates,Is.EqualTo(v.SpaceDiscard.Candidates));
        }
        [Test] public void Empty_hand_auto_completion_does_not_show_a_decision_prompt()
        {var c=Controller(SpaceEndTurnDiscardTests.Assessed(held:false),PlayerSide.US);Assert.That(c.PracticePhaseText,Does.Not.Contain("太空弃牌"));Assert.That(c.CanDiscardSelectedHeldCard,Is.False);Assert.That(c.TurnEndMessage,Does.Contain("无普通手牌"));}
        [Test] public void Ordinary_china_selection_remains_available_before_turn_end()
        {var c=Controller(ChinaCardPlayTests.SetChina(FinalScoringPlayTests.Ready(),PlayerSide.US,CardFace.FaceUp),PlayerSide.US);Assert.That(c.CanSelectChinaCard,Is.True);}
        [Test] public void Build_payload_adds_canonical_delta_and_has_separate_player_entry()
        {
            DemoBuild.SynchronizeData();var path=Path.Combine(UnityEngine.Application.streamingAssetsPath,"ColdWarStruggle","space-end-turn-discard-scenarios.json");
            Assert.That(File.Exists(path),Is.True);Assert.That(File.ReadAllBytes(path),Is.EqualTo(File.ReadAllBytes(Path.Combine(DefinitionTests.Root,"Data/Demo/space-end-turn-discard-scenarios.json"))));
            Assert.That(typeof(DemoBuild).GetMethod("BuildSpaceDiscardPlaytest"),Is.Not.Null);
        }
        [Test] public void New_entries_can_be_selected_but_default_and_observer_policy_stay_explicit()
        {
            var ids=SpaceDiscardDemoEntryTests.Catalog().Scenarios.Select(s=>s.Id).ToArray();Assert.That(ids[DemoLaunchOptions.ScenarioIndex(Array.Empty<string>(),ids)],Is.EqualTo(HeadlineDemoCatalogTests.Id));
            foreach(var id in new[]{SpaceDiscardDemoEntryTests.NextId,SpaceDiscardDemoEntryTests.FinalId})Assert.That(ids[DemoLaunchOptions.ScenarioIndex(new[]{"--demo-scenario="+id},ids)],Is.EqualTo(id));Assert.That(DemoLaunchOptions.ShowHands(Array.Empty<string>()),Is.False);
        }
    }
}
