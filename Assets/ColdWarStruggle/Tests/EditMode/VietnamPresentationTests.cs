using System;
using System.Linq;
using System.Threading.Tasks;
using ColdWarStruggle.Application;
using ColdWarStruggle.Core;
using ColdWarStruggle.Presentation;
using NUnit.Framework;
using UnityEngine;
namespace ColdWarStruggle.Tests
{
    public sealed class VietnamPresentationTests
    {
        sealed class Fixture:IDisposable
        {
            readonly GameObject go;
            public DemoController Controller {get;}
            public DemoView View {get;}
            public Fixture(GameState state,PlayerSide side)
            {
                Controller=new DemoController(_=>new LocalGameHost(state,true));go=new GameObject("vietnam-feedback");View=go.AddComponent<DemoView>();
                var names=HeadlineDemoCatalogTests.Catalog().Names.ToDictionary(x=>x.Key,x=>x.Value,StringComparer.Ordinal);names["country.vietnam"]="越南测试";names["region.southeast_asia"]="东南亚测试";
                View.Initialize(Controller,names,new[]{new DemoScenarioOption("fixture","fixture",side,VietnamPlayTests.C("thailand"))},printedCards:state.Cards.Catalog);
            }
            public void Dispose()=>UnityEngine.Object.DestroyImmediate(go);
        }
        [Test] public async Task Event_feedback_exposes_condition_and_grant_and_retry_preserves_it()
        {
            using var f=new Fixture(VietnamPlayTests.Ready(),PlayerSide.USSR);var c=f.Controller;c.SelectCard(VietnamPlayTests.Card);var r=await c.PlayCardAsync(CardActionKind.Event);Assert.That(r.Accepted,Is.True);
            Assert.That(r.ActivatedOpsEffect.Region,Is.EqualTo("region.southeast_asia"));Assert.That(c.ReadView().Practice.OpsEffects.Single().Region,Is.EqualTo("region.southeast_asia"));
            var text=f.View.ReadPracticeFeedback();Assert.That(text,Does.Contain("全部目标").And.Contain("东南亚测试").And.Contain("越南测试 · 苏联 USSR 0 → 2 (+2)"));
            var before=c.ReadView();Assert.That(await c.RetryCardAsync(),Is.SameAs(r));Assert.That(f.View.ReadPracticeFeedback(),Is.EqualTo(text));Assert.That(c.ReadView().Version,Is.EqualTo(before.Version));Assert.That(f.View.TryOpenFeedback(),Is.True);Assert.That(f.View.ReadExpandedFeedback(),Is.EqualTo(text));
        }
        [Test] public async Task Coup_then_event_keeps_coup_condition_and_immediate_grant()
        {
            using var f=new Fixture(VietnamPlayTests.Ready(true),PlayerSide.US);var c=f.Controller;c.SelectCard(VietnamPlayTests.Card);Assert.That((await c.PlayCardAsync(CardActionKind.OpponentOpsFirst)).Accepted,Is.True);
            var r=await c.ChooseOpponentOperationsAsync(OperationsMode.Coup);Assert.That(r.Accepted,Is.True,r.Error.ToString());Assert.That(r.Coup,Is.Not.Null);Assert.That(r.Event,Is.Not.Null);Assert.That(r.ActivatedOpsEffect,Is.Not.Null);
            var text=f.View.ReadPracticeFeedback();Assert.That(text,Does.Contain("政变").And.Contain("全部目标").And.Contain("越南测试 · 苏联 USSR 0 → 2 (+2)"));
        }
        [Test] public async Task Mixed_flat_and_regional_headlines_keep_both_effects_and_one_grant()
        {
            var raw=HeadlineFlowTests.Prepared("containment","vietnam_revolts");var d=VietnamPlayTests.Bind(raw.Practice.Definition);
            var prepared=new GameState(raw.Map,raw.Countries,raw.Operations,raw.GameId,raw.Version,raw.SpentOps,raw.EventSequence,turn:raw.Turn,deployment:raw.Deployment,cards:raw.Cards,randomState:raw.RandomState,practice:ActionPracticeState.Create(d));
            using var f=new Fixture(HeadlineFlowTests.Start(prepared),PlayerSide.US);var c=f.Controller;c.SelectCard(new CardId("card.containment"));await c.SubmitSelectedHeadlineAsync();c.SelectSide(PlayerSide.USSR);c.SelectCard(VietnamPlayTests.Card);Assert.That((await c.SubmitSelectedHeadlineAsync()).Accepted,Is.True);
            var text=f.View.ReadPracticeFeedback();Assert.That(c.CurrentHeadlineFeedback.Count,Is.EqualTo(2));Assert.That(text,Does.Contain("全部目标").And.Contain("美国 US").And.Contain("越南测试"));Assert.That(text.Split(new[]{"越南测试 ·"},StringSplitOptions.None).Length-1,Is.EqualTo(1));
        }
        [Test] public void China_preview_combines_both_regions_without_changing_ordinary_quote_or_state()
        {
            using var f=new Fixture(VietnamPlayTests.Activated(),PlayerSide.USSR);var c=f.Controller;c.SelectCard(ChinaCardPlayTests.China);var before=c.ReadView();var option=c.CardOptions().China;
            Assert.That(option.CurrentOps,Is.EqualTo(4));Assert.That(option.RegionalMaximum,Is.EqualTo(6));Assert.That(option.RegionalOps.EligibleRegions,Is.EquivalentTo(new[]{"region.asia","region.southeast_asia"}));
            Assert.That(f.View.ReadOperationsPreview(),Does.Contain("普通 OPS 4"));Assert.That(f.View.ReadRegionalOperationsHint(),Does.Contain("东南亚测试").And.Contain("最多 6 OPS").And.Contain("全部目标"));
            Assert.That(c.ReadView().Version,Is.EqualTo(before.Version));Assert.That(c.ReadView().EventSequence,Is.EqualTo(before.EventSequence));
            c.SelectSide(PlayerSide.US);Assert.That(c.CardOptions().China.RegionalOps.EligibleRegions,Is.EquivalentTo(new[]{"region.asia"}));
        }
        [TestCase(false,"thailand",4)][TestCase(false,"angola",3)][TestCase(true,"thailand",5)][TestCase(true,"angola",4)]
        public async Task Pending_preview_and_selected_coup_agree_with_execution(bool olympic,string target,int expected)
        {
            var card=olympic?OlympicPlayTests.Card:new CardId("card.containment");var s=PlayTurnOpsEventTests.Give(VietnamPlayTests.Activated(),PlayerSide.USSR,card.Value);
            s=olympic?OlympicPlayTests.Respond(OlympicPlayTests.Play(s).NextState,false).NextState:ActionPracticeRules.Plan(s,PlayerSide.USSR,CardActionKind.OpponentEventFirst,"preview",card).NextState;
            using var f=new Fixture(s,PlayerSide.USSR);var c=f.Controller;var options=c.CardOptions().PendingOperations;var ordinary=olympic?4:3;
            Assert.That(options.CurrentOps,Is.EqualTo(ordinary));Assert.That(options.RegionalOps,Is.Not.Null);Assert.That(options.RegionalOps.Total,Is.EqualTo(ordinary+1));
            Assert.That(f.View.ReadOperationsPreview(),Does.Contain("普通 OPS "+ordinary).And.Contain("东南亚测试").And.Contain("最多 "+(ordinary+1)+" OPS"));
            c.SelectCoupUse();c.SelectCountry(VietnamPlayTests.C(target));var before=c.ReadView();var quote=c.SelectedCoupQuote;
            Assert.That(f.View.ReadOperationsPreview(),Does.Contain("政变 OPS "+expected).And.Contain("军事 +"+expected));Assert.That(quote.Strength,Is.EqualTo(expected));Assert.That(c.ReadView().Version,Is.EqualTo(before.Version));
            var r=olympic?await c.ChooseOlympicOperationsAsync(OperationsMode.Coup):await c.ChooseOpponentOperationsAsync(OperationsMode.Coup);Assert.That(r.Accepted,Is.True,r.Error.ToString());Assert.That(r.Coup.Strength,Is.EqualTo(quote.Strength));
        }
    }
}
