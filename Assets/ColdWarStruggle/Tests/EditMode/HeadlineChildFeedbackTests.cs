using System;
using System.Linq;
using System.Threading.Tasks;
using ColdWarStruggle.Core;
using ColdWarStruggle.Application;
using ColdWarStruggle.Presentation;
using NUnit.Framework;
using UnityEngine;
namespace ColdWarStruggle.Tests
{
    public class HeadlineChildFeedbackTests
    {
        static CardId Id(string n)=>FiveYearPlanPlayTests.Id(n);
        static CountryId C(string n)=>FiveYearPlanPlayTests.C(n);
        sealed class Fixture : IDisposable
        {
            readonly GameObject go;
            public DemoController Controller { get; }
            public DemoView View { get; }
            public Fixture(string child="duck_and_cover",int level=5,string us="five_year_plan",string su="fidel",bool allocation=false)
            {
                var state=FiveYearPlanFlowTests.HeadlinePrepared(us,su,child,level);
                if(allocation){var countries=state.Countries.ToDictionary(x=>x.Key,x=>x.Value);countries[C("uk")]=new CountryState(3,0);countries[C("france")]=new CountryState(2,0);state=PlayInfluenceEventTests.Copy(state,countries);}
                Controller=new DemoController(_=>new LocalGameHost(HeadlineFlowTests.Start(state),true));var catalog=HeadlineDemoCatalogTests.Catalog();go=new GameObject("headline-child-feedback");View=go.AddComponent<DemoView>();
                View.Initialize(Controller,catalog.Names,new[]{new DemoScenarioOption("fixture","fixture",PlayerSide.US,C("yugoslavia"))},printedCards:state.Cards.Catalog);
            }
            public async Task Open(string us="five_year_plan",string su="fidel")
            {
                Controller.SelectCard(Id(us));Assert.That((await Controller.SubmitSelectedHeadlineAsync()).Accepted,Is.True);
                Controller.SelectSide(PlayerSide.USSR);Controller.SelectCard(Id(su));var r=await Controller.SubmitSelectedHeadlineAsync();Assert.That(r.Accepted,Is.True,r.Error.ToString());
            }
            public void Dispose()=>UnityEngine.Object.DestroyImmediate(go);
        }
        [TestCase("duck_and_cover","DEFCON 5→4")][TestCase("containment","+1")][TestCase("asia_scoring","不触发")][TestCase(null,"未进行抽样")]
        public async Task Instant_headline_child_renders_result_details_without_choice_screen(string child,string detail)
        {
            using var f=new Fixture(child);await f.Open();var c=f.Controller;Assert.That(c.CurrentFiveYearHeadline,Is.Not.Null);Assert.That(c.CurrentFiveYearHeadline.FiveYearPlan.SelectedCard,Is.EqualTo(child==null?(CardId?)null:Id(child)));
            var text=f.View.ReadHeadlineFiveYearPlanFeedback();Assert.That(text,Does.Contain("头条").And.Contain(detail));
            if(child!=null)Assert.That(text,Does.Contain(HeadlineDemoCatalogTests.Catalog().Names[Id(child).Value]));
            var version=c.ReadView().Version;c.SelectSide(PlayerSide.US);Assert.That(f.View.ReadHeadlineFiveYearPlanFeedback(),Is.EqualTo(text));Assert.That(c.ReadView().Version,Is.EqualTo(version));
        }
        [Test] public async Task Nuclear_child_feedback_is_visible_and_keeps_terminal_track_summary()
        {
            using var f=new Fixture(level:2);await f.Open();Assert.That(f.Controller.ReadView().Practice.Winner,Is.EqualTo(PlayerSide.USSR));Assert.That(f.View.ReadHeadlineFiveYearPlanFeedback(),Does.Contain("终局").And.Contain("DEFCON 2→1"));
        }
        [Test] public async Task Choice_completion_replaces_waiting_summary_and_old_headline_retry_cannot_restore_it()
        {
            using var f=new Fixture("independent_reds");await f.Open();var c=f.Controller;Assert.That(f.View.ReadHeadlineFiveYearPlanFeedback(),Does.Contain("等待美国"));c.SelectSide(PlayerSide.US);Assert.That(c.ToggleEventTarget(C("yugoslavia")),Is.True);Assert.That((await c.ConfirmEventAsync()).Accepted,Is.True);
            var done=f.View.ReadHeadlineFiveYearPlanFeedback();Assert.That(done,Does.Contain("子事件完成"));var version=c.ReadView().Version;c.SelectSide(PlayerSide.USSR);Assert.That((await c.RetryHeadlineAsync()).Accepted,Is.True);Assert.That(c.ReadView().Version,Is.EqualTo(version));Assert.That(f.View.ReadHeadlineFiveYearPlanFeedback(),Is.EqualTo(done));
        }
        [Test] public async Task Completing_prior_headline_choice_exposes_following_instant_child_step()
        {
            using var f=new Fixture("containment",us:"socialist_governments",su:"five_year_plan",allocation:true);await f.Open("socialist_governments","five_year_plan");var c=f.Controller;Assert.That(c.CurrentFiveYearHeadline,Is.Null);
            Assert.That(c.ChangeEventAllocation(C("uk"),1),Is.True);Assert.That(c.ChangeEventAllocation(C("uk"),1),Is.True);Assert.That(c.ChangeEventAllocation(C("france"),1),Is.True);var done=await c.ConfirmEventAsync();Assert.That(done.Accepted,Is.True,done.Error.ToString());Assert.That(done.HeadlineSteps.Last().FiveYearPlan,Is.Not.Null);Assert.That(done.FiveYearPlan,Is.Null);
            Assert.That(c.CurrentFiveYearHeadline,Is.Not.Null);Assert.That(f.View.ReadHeadlineFiveYearPlanFeedback(),Does.Contain("子事件完成").And.Contain("+1"));
        }
        [Test] public async Task Later_action_hides_old_summary_and_retry_does_not_reclassify_it_as_current()
        {
            using var f=new Fixture("containment");await f.Open();var c=f.Controller;Assert.That(c.CurrentFiveYearHeadline,Is.Not.Null);Assert.That((await c.SkipEmptyHandAsync()).Accepted,Is.True);Assert.That(c.CurrentFiveYearHeadline,Is.Null);Assert.That(f.View.ReadHeadlineFiveYearPlanFeedback(),Is.Null);
            await c.RetryHeadlineAsync();Assert.That(c.CurrentFiveYearHeadline,Is.Null);Assert.That(f.View.ReadHeadlineFiveYearPlanFeedback(),Is.Null);
        }
        [Test] public async Task Current_child_completion_is_not_hidden_by_older_parent_card_receipt()
        {
            var state=FiveYearPlanPlayTests.Ready(actor:PlayerSide.US);var controller=new DemoController(_=>new LocalGameHost(state,true));var go=new GameObject("current-child-feedback");
            try
            {
                var view=go.AddComponent<DemoView>();view.Initialize(controller,HeadlineDemoCatalogTests.Catalog().Names,new[]{new DemoScenarioOption("fixture","fixture",PlayerSide.US,C("yugoslavia"))},printedCards:state.Cards.Catalog);
                controller.SelectCard(Id("five_year_plan"));Assert.That((await controller.PlayCardAsync(CardActionKind.Event)).Accepted,Is.True);
                Assert.That(controller.ToggleEventTarget(C("yugoslavia")),Is.True);Assert.That((await controller.ConfirmEventAsync()).Accepted,Is.True);
                Assert.That(controller.LastCardResult.View.Version,Is.LessThan(controller.LastEventResult.View.Version));
                Assert.That(view.ReadPracticeFeedback(),Does.Contain("子事件完成").And.Not.Contain("历史"));
            }
            finally{UnityEngine.Object.DestroyImmediate(go);}
        }
        [Test] public async Task Secret_first_headline_and_reset_have_no_child_feedback()
        {
            using var f=new Fixture();var c=f.Controller;c.SelectCard(Id("five_year_plan"));await c.SubmitSelectedHeadlineAsync();Assert.That(c.CurrentFiveYearHeadline,Is.Null);Assert.That(f.View.ReadHeadlineFiveYearPlanFeedback(),Is.Null);
            c.SelectSide(PlayerSide.USSR);c.SelectCard(Id("fidel"));await c.SubmitSelectedHeadlineAsync();Assert.That(c.CurrentFiveYearHeadline,Is.Not.Null);
            c.LoadScenario("fixture",PlayerSide.US,C("yugoslavia"));Assert.That(c.CurrentFiveYearHeadline,Is.Null);Assert.That(f.View.ReadHeadlineFiveYearPlanFeedback(),Is.Null);
        }
    }
}
