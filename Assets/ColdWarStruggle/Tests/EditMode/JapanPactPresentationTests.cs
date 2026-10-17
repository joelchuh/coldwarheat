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
    public sealed class JapanPactPresentationTests
    {
        sealed class Fixture:IDisposable
        {
            readonly GameObject go;public DemoController Controller {get;} public DemoView View {get;}
            public Fixture(GameState state,PlayerSide side)
            {
                Controller=new DemoController(_=>new LocalGameHost(state,true));go=new GameObject("japan-feedback");View=go.AddComponent<DemoView>();
                var names=HeadlineDemoCatalogTests.Catalog().Names.ToDictionary(x=>x.Key,x=>x.Value,StringComparer.Ordinal);names["country.japan"]="日本测试";
                View.Initialize(Controller,names,new[]{new DemoScenarioOption("fixture","fixture",side,JapanPactPlayTests.C("japan"))},printedCards:state.Cards.Catalog);
            }
            public void Dispose()=>UnityEngine.Object.DestroyImmediate(go);
        }
        [TestCase(1)][TestCase(9)] public async Task Activation_feedback_includes_grant_or_zero_change_and_permanent_status(int own)
        {
            using var f=new Fixture(JapanPactPlayTests.Ready(own:own),PlayerSide.USSR);var c=f.Controller;Assert.That(f.View.ReadPermanentEffectsSummary(),Is.Empty);c.SelectCard(JapanPactPlayTests.Card);var r=await c.PlayCardAsync(CardActionKind.Event);Assert.That(r.Accepted,Is.True);
            var text=f.View.ReadPracticeFeedback();Assert.That(text,Does.Contain("美日安保").And.Contain("长期").And.Contain("日本测试").And.Contain("政变").And.Contain("调整"));Assert.That(text,Does.Contain(own==1?"1 → 7 (+6)":"影响力无变化"));Assert.That(f.View.ReadPermanentEffectsSummary(),Does.Contain("美日安保"));
            Assert.That(await c.RetryCardAsync(),Is.SameAs(r));Assert.That(f.View.ReadPracticeFeedback(),Is.EqualTo(text));c.SelectSide(PlayerSide.US);Assert.That(f.View.ReadPermanentEffectsSummary(),Does.Contain("美日安保"));
        }
        [Test] public async Task Ops_first_coup_feedback_keeps_both_coup_and_pact_receipts()
        {
            using var f=new Fixture(JapanPactPlayTests.Ready(),PlayerSide.USSR);var c=f.Controller;c.SelectCard(JapanPactPlayTests.Card);await c.PlayCardAsync(CardActionKind.OpponentOpsFirst);var r=await c.ChooseOpponentOperationsAsync(OperationsMode.Coup);Assert.That(r.Accepted,Is.True,r.Error.ToString());Assert.That(f.View.ReadPracticeFeedback(),Does.Contain("政变").And.Contain("美日安保").And.Contain("日本测试"));
        }
        [Test] public void Protected_coup_preview_uses_authoritative_error_and_retains_public_status()
        {
            var s=PlayTurnOpsEventTests.Give(JapanPactPlayTests.Active(),PlayerSide.USSR,OlympicPlayTests.Card.Value);using var f=new Fixture(s,PlayerSide.USSR);var c=f.Controller;c.SelectCard(OlympicPlayTests.Card);c.SelectCoupUse();var before=c.ReadView();Assert.That(c.SelectedCoupQuote.Error,Is.EqualTo(RuleError.EventForbidden));Assert.That(f.View.ReadOperationsPreview(),Does.Contain("持续效果"));Assert.That(f.View.ReadPermanentEffectsSummary(),Does.Contain("日本测试"));Assert.That(c.ReadView().Version,Is.EqualTo(before.Version));
        }
        [Test] public async Task Headline_public_feedback_keeps_pact_and_grant_once()
        {
            var raw=HeadlineFlowTests.Prepared("us_japan_mutual_defense_pact","nasser");var d=JapanPactPlayTests.Bind(raw.Practice.Definition);
            var s=new GameState(raw.Map,raw.Countries,raw.Operations,raw.GameId,raw.Version,raw.SpentOps,raw.EventSequence,turn:raw.Turn,deployment:raw.Deployment,cards:raw.Cards,randomState:raw.RandomState,practice:ActionPracticeState.Create(d),japanPact:JapanPactState.Create(d.JapanPact,raw.GameId));
            using var f=new Fixture(HeadlineFlowTests.Start(s),PlayerSide.US);var c=f.Controller;c.SelectCard(JapanPactPlayTests.Card);await c.SubmitSelectedHeadlineAsync();c.SelectSide(PlayerSide.USSR);c.SelectCard(new CardId("card.nasser"));Assert.That((await c.SubmitSelectedHeadlineAsync()).Accepted,Is.True);Assert.That(c.CurrentHeadlineFeedback.Any(x=>x.ActivatedJapanPact!=null),Is.True);var text=f.View.ReadPracticeFeedback();Assert.That(text,Does.Contain("美日安保").And.Contain("长期"));Assert.That(text.Split(new[]{"日本测试 · 美国 US "},StringSplitOptions.None).Length-1,Is.EqualTo(1));
        }
    }
}
