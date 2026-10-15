using System;
using System.Linq;
using System.Threading.Tasks;
using ColdWarStruggle.Core;
using ColdWarStruggle.Application;
using ColdWarStruggle.Presentation;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class HeadlinePlaytestEntryTests
    {
        static DemoController Controller(LocalGameHost host,PlayerSide side=PlayerSide.US)
        {var c=new DemoController(_=>host);c.LoadScenario("test",side,new CountryId("country.iran"));return c;}
        [Test] public void Default_launch_prefers_headline_scene_when_present()
        {Assert.That(DemoLaunchOptions.ScenarioIndex(Array.Empty<string>(),new[]{"demo.practice.quick_start",HeadlineDemoCatalogTests.Id,"demo.setup.initial_deal"}),Is.EqualTo(1));}
        [Test] public void Explicit_legacy_selection_remains_available()
        {Assert.That(DemoLaunchOptions.ScenarioIndex(new[]{"--demo-scenario=demo.practice.quick_start"},new[]{HeadlineDemoCatalogTests.Id,"demo.practice.quick_start"}),Is.EqualTo(1));}
        [TestCase(PlayerSide.US)][TestCase(PlayerSide.USSR)] public void Collection_allows_current_candidates_independent_of_ordinary_actor(PlayerSide side)
        {
            var c=Controller(HeadlinePanelFrameTests.Host(),side);var v=c.ReadView();var id=v.Headline.Selection.Candidates.Single();Assert.That(c.CanSelectHandCard(id),Is.True);Assert.That(c.CanSelectHandCard(side==PlayerSide.US?HeadlineFlowTests.Id("fidel"):HeadlineFlowTests.Id("defectors")),Is.False);
            Assert.That(c.PracticePhaseText,Does.Contain("头条选择"));Assert.That(c.PracticePhaseText,Does.Not.Contain("行动 1"));
        }
        [Test] public async Task Lock_disables_further_hand_selection()
        {var c=Controller(HeadlinePanelFrameTests.Host());c.SelectCard(HeadlineFlowTests.Id("defectors"));await c.SubmitSelectedHeadlineAsync();Assert.That(c.CanSelectHandCard(HeadlineFlowTests.Id("defectors")),Is.False);}
        [Test] public void Unsupported_headline_card_stays_readonly()
        {var c=Controller(HeadlinePanelFrameTests.Host("blockade"));Assert.That(c.CanSelectHandCard(HeadlineFlowTests.Id("blockade")),Is.False);}
        [TestCase(false)][TestCase(true)] public void Phase_text_distinguishes_waiting_from_terminal(bool terminal)
        {
            var h=HeadlinePanelFrameTests.Host(terminal?"duck_and_cover":"decolonization","defectors",terminal?2:5);HeadlineSessionTests.Submit(h.UsSession,terminal?"duck_and_cover":"decolonization");HeadlineSessionTests.Submit(h.UssrSession,"defectors");
            var c=Controller(h);Assert.That(c.PracticePhaseText,Does.Contain(terminal?"终局":"头条结算"));Assert.That(c.PracticePhaseText,Does.Not.Contain("行动 1"));
        }
        [Test] public void Resolved_headline_returns_to_ordinary_action_label()
        {
            var h=HeadlinePanelFrameTests.Host();HeadlineSessionTests.Submit(h.UsSession,"defectors");HeadlineSessionTests.Submit(h.UssrSession,"fidel");Assert.That(Controller(h).PracticePhaseText,Does.Contain("行动 1"));
        }
        [TestCase(false)][TestCase(true)] public void Next_turn_text_matches_actual_headline_configuration(bool enabled)
        {
            GameState s;
            if(enabled){s=HeadlineFlowTests.Start(HeadlineFlowTests.Prepared(earlyOnly:true));s=HeadlineFlowTests.Choose(s,PlayerSide.US,"fidel");s=HeadlineFlowTests.Choose(s,PlayerSide.USSR,"romanian_abdication");s=NextTurnPlayTests.Assessed(s);}
            else s=NextTurnPlayTests.Assessed();
            var c=Controller(new LocalGameHost(s,true));Assert.That(c.ReadView().Practice.NextTurnAvailable,Is.True);Assert.That(c.NextTurnButtonText,Does.Contain(enabled?"进入头条":"跳过头条"));Assert.That(c.TurnEndMessage,Does.Contain(enabled?"进入头条":"跳过头条"));
        }
    }
}
