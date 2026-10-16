using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ColdWarStruggle.Core;
using ColdWarStruggle.Application;
using ColdWarStruggle.Presentation;
using ColdWarStruggle.Editor;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class FinalScoringPresentationTests
    {
        static DemoController Controller(GameState s)
        {var h=new LocalGameHost(s,true);var c=new DemoController(_=>h);c.LoadScenario("test",PlayerSide.US,new CountryId("country.iran"));return c;}
        [TestCase(-2,"苏联获胜")][TestCase(-1,"平局")][TestCase(0,"美国获胜")]
        public async Task Frame_and_status_distinguish_all_terminal_results(int vp,string expected)
        {
            var held=new CardId("card.duck_and_cover");var c=Controller(PlayTurnOpsEventTests.Give(FinalScoringPlayTests.Assessed(vp),PlayerSide.US,held.Value));Assert.That(c.CanSelectHandCard(held),Is.True);Assert.That(c.FinalScoringPrompt,Does.Contain("最终计分"));Assert.That(c.TurnEndMessage,Does.Not.Contain("最终计分尚未接入"));
            var r=await c.CompleteFinalScoringAsync();Assert.That(r.Accepted,Is.True);var f=FinalScoringPanelFrame.Create(c.ReadView().Practice.FinalScoring);
            Assert.That(f.Outcome,Does.Contain(expected));Assert.That(c.PracticePhaseText,Does.Contain(expected));Assert.That(f.Equation,Does.Contain((vp+1).ToString()));
            Assert.That(f.Rows.Count,Is.EqualTo(6));Assert.That(f.Rows.Select(x=>x.Label),Is.EquivalentTo(new[]{"欧洲","亚洲","中东","非洲","中美洲","南美洲"}));
            Assert.That(f.Rows.All(x=>x.Net=="0"),Is.True);Assert.That(f.ChinaLine,Does.Contain("美国").And.Contain("+1"));Assert.That(c.TurnEndMessage,Does.Not.Contain("尚未终局").And.Not.Contain("可进入"));
            Assert.That(await c.RetryCardAsync(),Is.SameAs(r));var version=c.ReadView().Version;Assert.That(c.CanSelectHandCard(held),Is.False,"Terminal hand must not invite another play.");
            c.SelectSide(PlayerSide.USSR);Assert.That(c.LastCardResult,Is.Null);Assert.That(FinalScoringPanelFrame.Create(c.ReadView().Practice.FinalScoring).Outcome,Is.EqualTo(f.Outcome));Assert.That(c.ReadView().Version,Is.EqualTo(version));
            Assert.Throws<NotSupportedException>(()=>((IList<FinalScoringRow>)f.Rows).Clear());
        }
        [TestCase(PlayerSide.US)][TestCase(PlayerSide.USSR)] public async Task Europe_frame_uses_control_label_and_keeps_before_vp(PlayerSide winner)
        {
            var s=FinalScoringPlayTests.Ready(vp:7);var board=s.Countries.ToDictionary(x=>x.Key,x=>x.Value);
            RegionScoringTests.Control(s.Map,board,winner,s.Map.GetCountriesInRegion("region.europe").Where(x=>x.Battleground).Select(x=>x.Id.Value).ToArray());
            s=NextTurnPlayTests.Apply(FinalScoringPlayTests.Complete(PlayScoringTests.Replace(s,board)),CardActionKind.AssessTurnEnd);var c=Controller(s);await c.CompleteFinalScoringAsync();
            var f=FinalScoringPanelFrame.Create(c.ReadView().Practice.FinalScoring);Assert.That(f.Outcome,Does.Contain("欧洲控制"));Assert.That(f.Equation,Does.Contain("保留").And.Contain("7"));Assert.That(f.Rows.Single(x=>x.RegionId=="region.europe").Net,Does.Contain("直接获胜"));
        }
        [Test] public void Prompt_does_not_offer_scoring_before_audit_or_in_legacy_scene()
        {
            var c=Controller(FinalScoringPlayTests.Complete(FinalScoringPlayTests.Ready()));Assert.That(c.FinalScoringPrompt,Does.Contain("审核"));
            c=Controller(NextTurnPlayTests.Assessed());Assert.That(c.FinalScoringPrompt,Is.Null);
        }
        [Test] public void Frame_requires_report(){Assert.Throws<ArgumentNullException>(()=>FinalScoringPanelFrame.Create(null));}
        [Test] public void Build_payload_copies_canonical_final_inputs_and_exposes_separate_build_entry()
        {
            DemoBuild.SynchronizeData();var dir=Path.Combine(UnityEngine.Application.streamingAssetsPath,"ColdWarStruggle");
            foreach(var pair in new[]{("Data/Demo/final-scoring-scenario.json","final-scoring-scenario.json"),("Data/Rulesets/deluxe-2015/scoring-card-parameters.json","scoring-card-parameters.json"),("Docs/Contracts/scoring-card-parameters.schema.json","scoring-card-parameters.schema.json")})
                Assert.That(File.ReadAllBytes(Path.Combine(dir,pair.Item2)),Is.EqualTo(File.ReadAllBytes(Path.Combine(DefinitionTests.Root,pair.Item1))));
            Assert.That(typeof(DemoBuild).GetMethod("BuildFinalScoringPlaytest"),Is.Not.Null);
        }
        [Test] public void Existing_default_headline_and_explicit_final_selection_are_supported()
        {var ids=FinalScoringDemoEntryTests.Catalog().Scenarios.Select(x=>x.Id).ToArray();Assert.That(ids[DemoLaunchOptions.ScenarioIndex(Array.Empty<string>(),ids)],Is.EqualTo(HeadlineDemoCatalogTests.Id));Assert.That(ids[DemoLaunchOptions.ScenarioIndex(new[]{"--demo-scenario="+FinalScoringDemoEntryTests.Id},ids)],Is.EqualTo(FinalScoringDemoEntryTests.Id));Assert.That(DemoLaunchOptions.ShowHands(Array.Empty<string>()),Is.False);}
    }
}
