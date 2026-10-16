using System;
using System.Threading.Tasks;
using ColdWarStruggle.Core;
using ColdWarStruggle.Application;
using ColdWarStruggle.Presentation;
using NUnit.Framework;
using UnityEngine;
namespace ColdWarStruggle.Tests
{
    public class SpaceEventPresentationTests
    {
        sealed class Fixture:IDisposable
        {
            readonly GameObject go;public DemoController Controller {get;}public DemoView View {get;}
            public Fixture(GameState state,PlayerSide side=PlayerSide.USSR)
            {
                Controller=new DemoController(_=>new LocalGameHost(state,true));go=new GameObject("space-event-results");View=go.AddComponent<DemoView>();
                View.Initialize(Controller,HeadlineDemoCatalogTests.Catalog().Names,new[]{new DemoScenarioOption("fixture","fixture",side,new CountryId("country.india"))},printedCards:state.Cards.Catalog);
            }
            public async Task Play(){Controller.SelectCard(SpaceEventPlayTests.Card);var r=await Controller.PlayCardAsync(CardActionKind.Event);Assert.That(r.Accepted,Is.True,r.Error.ToString());}
            public async Task Headlines(string us,string su)
            {Controller.SelectCard(WarPlayTests.Id(us));Assert.That((await Controller.SubmitSelectedHeadlineAsync()).Accepted,Is.True);Controller.SelectSide(PlayerSide.USSR);Controller.SelectCard(WarPlayTests.Id(su));Assert.That((await Controller.SubmitSelectedHeadlineAsync()).Accepted,Is.True);}
            public void Dispose()=>UnityEngine.Object.DestroyImmediate(go);
        }
        [TestCase(0,0,"先到",2)][TestCase(1,0,"后到",1)][TestCase(0,1,"先到",0)]
        public async Task Event_feedback_explains_position_reward_and_attempts(int us,int su,string arrival,int reward)
        {
            using var f=new Fixture(SpaceEventPlayTests.Ready(us:us,su:su));await f.Play();var text=f.View.ReadPracticeFeedback();Assert.That(text,Does.Contain("太空事件").And.Contain(su+" → "+(su+1)).And.Contain(arrival).And.Contain("+"+reward+" VP").And.Contain("不消耗普通太空次数"));
            if(su==1)Assert.That(text,Does.Contain("尝试上限 1→2"));
            var before=f.Controller.ReadView();Assert.That(f.View.TryOpenFeedback(),Is.True);Assert.That(f.View.ReadExpandedFeedback(),Is.EqualTo(text));f.View.CloseFeedback();Assert.That(f.View.TryOpenFeedback(),Is.True);await f.Controller.RetryCardAsync();Assert.That(f.Controller.ReadView().Version,Is.EqualTo(before.Version));Assert.That(f.Controller.ReadView().Practice.VpForUS,Is.EqualTo(before.Practice.VpForUS));
        }
        [Test] public async Task Mixed_war_and_space_headline_feedback_preserves_both_and_old_retry_stays_historical()
        {
            var s=SpaceEventHeadlineTests.Ready(SpaceEventHeadlineTests.Scientist,"arab_israeli_war",wars:true);
            using var f=new Fixture(s,PlayerSide.US);await f.Headlines(SpaceEventHeadlineTests.Scientist,"arab_israeli_war");var c=f.Controller;var text=f.View.ReadPracticeFeedback();Assert.That(c.CurrentHeadlineFeedback.Count,Is.EqualTo(2));Assert.That(text,Does.Contain("战争").And.Contain("太空事件").And.Contain("军事 +2"));Assert.That(f.View.TryOpenFeedback(),Is.True);Assert.That(f.View.ReadExpandedFeedback(),Is.EqualTo(text));
            c.SelectSide(PlayerSide.US);Assert.That(f.View.ReadHeadlineFeedback(),Is.EqualTo(text));c.SelectSide(PlayerSide.USSR);Assert.That((await c.SkipEmptyHandAsync()).Accepted,Is.True);Assert.That(f.View.IsFeedbackReaderOpen,Is.False);await c.RetryHeadlineAsync();Assert.That(c.CurrentHeadlineFeedback,Is.Empty);Assert.That(f.View.ReadHeadlineFeedback(),Is.Null);
        }
    }
}
