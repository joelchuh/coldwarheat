using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ColdWarStruggle.Core;
using ColdWarStruggle.Application;
using ColdWarStruggle.Presentation;
using NUnit.Framework;
using UnityEngine;
namespace ColdWarStruggle.Tests
{
    public class WarPresentationTests
    {
        static CardId Id(string n)=>WarPlayTests.Id(n);
        static CountryId C(string n)=>WarPlayTests.Country(n);
        sealed class Fixture:IDisposable
        {
            readonly GameObject go;
            public DemoController Controller {get;}
            public DemoView View {get;}
            public Fixture(GameState state,PlayerSide side=PlayerSide.USSR)
            {
                Controller=new DemoController(_=>new LocalGameHost(state,true));go=new GameObject("war-presentation");View=go.AddComponent<DemoView>();
                View.Initialize(Controller,HeadlineDemoCatalogTests.Catalog().Names,new[]{new DemoScenarioOption("fixture","fixture",side,C("india"))},printedCards:state.Cards.Catalog);
            }
            public async Task Play(string name){Controller.SelectCard(Id(name));var r=await Controller.PlayCardAsync(CardActionKind.Event);Assert.That(r.Accepted,Is.True,r.Error.ToString());}
            public async Task Headlines(string us,string su)
            {Controller.SelectCard(Id(us));Assert.That((await Controller.SubmitSelectedHeadlineAsync()).Accepted,Is.True);Controller.SelectSide(PlayerSide.USSR);Controller.SelectCard(Id(su));Assert.That((await Controller.SubmitSelectedHeadlineAsync()).Accepted,Is.True);}
            public void Dispose()=>UnityEngine.Object.DestroyImmediate(go);
        }
        sealed class Probe:IMapView
        {
            Action<MapIntent> handler;
            public Action<MapIntent> Captured;
            public MapFrame Frame;
            public bool Disposed;
            public event Action<MapIntent> Intent {add{handler+=value;Captured=handler;} remove{handler-=value;}}
            public void Initialize(MapViewContext context){}
            public bool Present(MapFrame frame){Frame=frame;return true;}
            public void FocusOn(CountryId id){}
            public void Clear(){Frame=null;}
            public void Dispose(){Disposed=true;}
            public MapIntent Click(CountryId id)=>new MapIntent(Frame.ContextId,Frame.Version,Frame.Sequence,Frame.InteractionKey,id);
            public void Send(CountryId id)=>handler?.Invoke(Click(id));
        }
        [Test] public async Task Controller_requires_live_owner_and_candidate_and_retry_does_not_restore_choice()
        {
            using var f=new Fixture(WarPlayTests.Ready());var c=f.Controller;await f.Play("indo_pakistani_war");Assert.That(c.PracticePhaseText,Does.Contain("战争").And.Contain("苏联"));Assert.That(c.CanConfirmWar,Is.True);
            c.SelectCountry(C("israel"));Assert.That(c.CanConfirmWar,Is.False);Assert.ThrowsAsync<InvalidOperationException>(async()=>{await c.ConfirmWarAsync();});
            c.SelectCountry(C("india"));c.SelectSide(PlayerSide.US);Assert.That(c.CanConfirmWar,Is.False);Assert.ThrowsAsync<InvalidOperationException>(async()=>{await c.ConfirmWarAsync();});c.SelectSide(PlayerSide.USSR);
            var result=await c.ConfirmWarAsync();Assert.That(result.Accepted,Is.True);Assert.That(result.War,Is.Not.Null);Assert.That(c.CanConfirmWar,Is.False);Assert.That(await c.RetryCardAsync(),Is.SameAs(result));Assert.That(c.ReadView().War,Is.Null);Assert.That(c.ReadView().Practice.UssrMilitary,Is.EqualTo(2));
            c.LoadScenario("fixture",PlayerSide.USSR,C("india"));Assert.That(c.LastCardResult,Is.Null);Assert.That(c.CurrentWarHeadline,Is.Null);Assert.That(c.CanConfirmWar,Is.False);
        }
        [Test] public async Task Map_highlights_two_targets_for_owner_and_replaced_view_cannot_submit_old_intent()
        {
            using var f=new Fixture(WarPlayTests.Ready());await f.Play("indo_pakistani_war");var c=f.Controller;using var p=new MapPresenter(c,new Dictionary<string,string>());var a=new Probe();p.Attach(a);
            Assert.That(a.Frame.Countries.Where(x=>x.Allowed).Select(x=>x.Id),Is.EquivalentTo(new[]{C("india"),C("pakistan")}));var before=c.ReadView();var old=a.Click(C("pakistan"));var callback=a.Captured;
            var b=new Probe();p.Attach(b);callback(old);Assert.That(a.Disposed,Is.True);Assert.That(c.SelectedCountry,Is.EqualTo(C("india")));b.Send(C("pakistan"));Assert.That(c.SelectedCountry,Is.EqualTo(C("pakistan")));Assert.That(c.ReadView().Version,Is.EqualTo(before.Version));
            c.SelectSide(PlayerSide.US);p.Refresh();Assert.That(b.Frame.Countries.Where(x=>x.Allowed),Is.Empty);c.SelectSide(PlayerSide.USSR);p.Refresh();Assert.That(c.CanConfirmWar,Is.True);var done=await c.ConfirmWarAsync();Assert.That(done.War.Target,Is.EqualTo(C("pakistan")));p.Refresh();Assert.That(c.ReadView().War,Is.Null);
        }
        [TestCase("korean_war",true)][TestCase("arab_israeli_war",false)] public async Task Actual_feedback_helper_shows_war_math_and_awards(string name,bool win)
        {
            using var f=new Fixture(WarPlayTests.Ready(name,win:win));await f.Play(name);var text=f.View.ReadPracticeFeedback();Assert.That(text,Does.Contain("骰").And.Contain("修正").And.Contain("军事 +2").And.Contain("VP").And.Contain(win?"胜利":"失败"));
        }
        [Test] public async Task Coup_then_war_actual_feedback_retains_both_outcomes()
        {
            using var f=new Fixture(WarPlayTests.Ready("korean_war",PlayerSide.US),PlayerSide.US);var c=f.Controller;c.SelectCard(Id("korean_war"));Assert.That((await c.PlayCardAsync(CardActionKind.OpponentOpsFirst)).Accepted,Is.True);c.SelectCountry(C("south_korea"));Assert.That((await c.ChooseOpponentOperationsAsync(OperationsMode.Coup)).Accepted,Is.True);
            Assert.That(f.View.ReadPracticeFeedback(),Does.Contain("政变骰").And.Contain("战争").And.Contain("VP"));
        }
        [Test] public async Task Instant_headline_feedback_survives_identity_change_and_is_hidden_after_later_action()
        {
            using var f=new Fixture(WarContinuationTests.Headlines("korean_war","fidel"),PlayerSide.US);await f.Headlines("korean_war","fidel");var c=f.Controller;Assert.That(c.CurrentWarHeadline,Is.Not.Null);var text=f.View.ReadHeadlineWarFeedback();Assert.That(text,Does.Contain("头条").And.Contain("骰").And.Contain("军事 +2"));Assert.That(f.View.ReadPracticeFeedback(),Is.EqualTo(text));
            c.SelectSide(PlayerSide.US);Assert.That(f.View.ReadHeadlineWarFeedback(),Is.EqualTo(text));c.SelectSide(PlayerSide.USSR);Assert.That((await c.SkipEmptyHandAsync()).Accepted,Is.True);Assert.That(c.CurrentWarHeadline,Is.Null);await c.RetryHeadlineAsync();Assert.That(f.View.ReadHeadlineWarFeedback(),Is.Null);
        }
        [Test] public async Task War_headline_feedback_stays_visible_while_next_event_waits_for_country_choice()
        {
            using var f=new Fixture(WarContinuationTests.Headlines("korean_war","truman_doctrine"),PlayerSide.US);await f.Headlines("korean_war","truman_doctrine");Assert.That(f.Controller.ReadView().Event,Is.Not.Null);
            Assert.That(f.View.ReadEventFeedback(),Does.Contain("头条").And.Contain("战争").And.Contain("军事 +2"));
        }
        [Test] public async Task Completed_war_headline_feedback_is_not_overwritten_by_old_pending_retry()
        {
            using var f=new Fixture(WarContinuationTests.Headlines("indo_pakistani_war","fidel"),PlayerSide.US);await f.Headlines("indo_pakistani_war","fidel");var c=f.Controller;Assert.That(c.ReadView().War,Is.Not.Null);c.SelectSide(PlayerSide.US);var done=await c.ConfirmWarAsync();Assert.That(done.Accepted,Is.True);var text=f.View.ReadHeadlineWarFeedback();Assert.That(text,Does.Contain("胜利").And.Contain("VP"));Assert.That(f.View.ReadPracticeFeedback(),Is.EqualTo(text));
            c.SelectSide(PlayerSide.USSR);await c.RetryHeadlineAsync();Assert.That(f.View.ReadHeadlineWarFeedback(),Is.EqualTo(text));Assert.That(c.ReadView().War,Is.Null);
        }
    }
}
