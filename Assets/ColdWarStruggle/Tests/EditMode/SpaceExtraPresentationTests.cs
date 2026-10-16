using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ColdWarStruggle.Core;
using ColdWarStruggle.Application;
using ColdWarStruggle.Presentation;
using ColdWarStruggle.Editor;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class SpaceExtraPresentationTests
    {
        [TestCase(PlayerSide.US)] [TestCase(PlayerSide.USSR)]
        public async Task Controller_displays_optional_phase_and_declines_via_idempotent_command(PlayerSide side)
        {
            var host=new LocalGameHost(SpaceExtraActionPlayTests.Ready(side,2),true);var c=new DemoController(_=>host);c.LoadScenario("extra",side,ChinaCardPlayTests.C("iran"));
            Assert.That(c.PracticePhaseText,Does.Contain("太空额外").And.Contain("7/8"));Assert.That(c.CanDeclineExtraActions,Is.True);
            var v=c.ReadView();var done=await c.DeclineExtraActionsAsync();Assert.That(done.Accepted,Is.True);Assert.That(c.CanDeclineExtraActions,Is.False);
            Assert.That(c.ReadView().Version,Is.EqualTo(v.Version+1));Assert.That(await c.RetryCardAsync(),Is.SameAs(done));Assert.That(c.ReadView().Version,Is.EqualTo(v.Version+1));
            Assert.That(c.PracticePhaseText,Does.Contain("行动结束"));c.SelectSide(side==PlayerSide.US?PlayerSide.USSR:PlayerSide.US);Assert.That(c.CanRetryCard,Is.False);Assert.That(c.LastCardResult,Is.Null);
        }
        [Test] public async Task In_progress_extra_card_cannot_be_declined_from_controller()
        {
            var state=PlayTurnOpsEventTests.Give(SpaceExtraActionPlayTests.Ready(),PlayerSide.US,"card.olympic_games");var h=new LocalGameHost(state,true);var c=new DemoController(_=>h);c.LoadScenario("extra",PlayerSide.US,ChinaCardPlayTests.C("iran"));
            c.SelectCard(new CardId("card.olympic_games"));await c.PlayCardAsync(CardActionKind.Influence);Assert.That(c.CanDeclineExtraActions,Is.False);
            var before=c.ReadView();var rejected=await c.DeclineExtraActionsAsync();Assert.That(rejected.Error,Is.EqualTo(RuleError.ActionNotReady));Assert.That(c.ReadView().Version,Is.EqualTo(before.Version));
        }
        [Test] public void Extra_fixture_has_identical_editor_player_payload_and_independent_build_entry()
        {
            DemoBuild.SynchronizeData();var target=Path.Combine(UnityEngine.Application.streamingAssetsPath,"ColdWarStruggle","space-extra-action-scenarios.json");
            Assert.That(File.Exists(target),Is.True);Assert.That(File.ReadAllBytes(target),Is.EqualTo(File.ReadAllBytes(Path.Combine(DefinitionTests.Root,"Data/Demo/space-extra-action-scenarios.json"))));
            Assert.That(typeof(DemoBuild).GetMethod("BuildSpaceExtraPlaytest"),Is.Not.Null);
            var ids=SpaceExtraDemoEntryTests.Catalog().Scenarios.Select(s=>s.Id).ToArray();Assert.That(ids[DemoLaunchOptions.ScenarioIndex(Array.Empty<string>(),ids)],Is.EqualTo(HeadlineDemoCatalogTests.Id));
            Assert.That(ids[DemoLaunchOptions.ScenarioIndex(new[]{"--demo-scenario="+SpaceExtraDemoEntryTests.EarlyId},ids)],Is.EqualTo(SpaceExtraDemoEntryTests.EarlyId));Assert.That(DemoLaunchOptions.ShowHands(Array.Empty<string>()),Is.False);
        }
    }
}
