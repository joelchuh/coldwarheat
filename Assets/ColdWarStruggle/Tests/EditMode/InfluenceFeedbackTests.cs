using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ColdWarStruggle.Application;
using ColdWarStruggle.Core;
using ColdWarStruggle.Presentation;
using NUnit.Framework;
using UnityEngine;
namespace ColdWarStruggle.Tests
{
    public sealed class InfluenceFeedbackTests
    {
        static CountryId C(string n)=>ComeconPlayTests.C(n);
        static CardId Card(string n)=>new CardId("card."+n);
        sealed class Fixture:IDisposable
        {
            readonly GameObject go;
            public DemoController Controller {get;}
            public DemoView View {get;}
            public LocalGameHost Host {get;private set;}
            public GameState Initial {get;}
            public Fixture(GameState state,PlayerSide side=PlayerSide.USSR,bool missingName=false)
            {
                Initial=state;Controller=new DemoController(_=>Host=new LocalGameHost(state,true));go=new GameObject("influence-feedback");View=go.AddComponent<DemoView>();
                var names=HeadlineDemoCatalogTests.Catalog().Names.ToDictionary(x=>x.Key,x=>x.Value,StringComparer.Ordinal);
                foreach(var key in new[]{"poland","india","romania","austria","finland","egypt","cuba","yugoslavia"})names["country."+key]="Display:"+key;
                if(missingName)names.Remove("country.cuba");var alternate=names.ToDictionary(x=>x.Key,x=>"Other:"+x.Value,StringComparer.Ordinal);
                View.Initialize(Controller,names,new[]{new DemoScenarioOption("fixture","fixture",side,C("poland"))},printedCards:state.Cards.Catalog,alternateNames:alternate);
            }
            public async Task Play(string card){Controller.SelectCard(Card(card));var r=await Controller.PlayCardAsync(CardActionKind.Event);Assert.That(r.Accepted,Is.True,r.Error.ToString());}
            public void Dispose()=>UnityEngine.Object.DestroyImmediate(go);
        }
        static void SamePosition(PlayerViewSnapshot a,PlayerViewSnapshot b)
        {
            Assert.That(b.GameId,Is.EqualTo(a.GameId));Assert.That(b.Version,Is.EqualTo(a.Version));Assert.That(b.EventSequence,Is.EqualTo(a.EventSequence));Assert.That(b.Countries.Select(c=>(c.Definition.Id,c.US,c.USSR)),Is.EqualTo(a.Countries.Select(c=>(c.Definition.Id,c.US,c.USSR))));Assert.That(b.Practice.CompletedActions,Is.EqualTo(a.Practice.CompletedActions));
        }
        static async Task CompleteComecon(Fixture f)
        {await f.Play("comecon");foreach(var c in ComeconRulesTests.Targets())Assert.That(f.Controller.ToggleEventTarget(c),Is.True);Assert.That((await f.Controller.ConfirmEventAsync()).Accepted,Is.True);}
        static GameState Immediate(string card,int us,int su,string country)
        {var s=PlayInfluenceEventTests.Ready("card."+card);var b=s.Countries.ToDictionary(x=>x.Key,x=>x.Value);b[C(country)]=new CountryState(us,su);return PlayInfluenceEventTests.Copy(s,b);}
        [Test] public async Task Immediate_add_then_remove_shows_receipt_order_both_sides_and_signed_changes()
        {
            using var f=new Fixture(Immediate("nasser",3,0,"egypt"));await f.Play("nasser");var text=f.View.ReadPracticeFeedback();
            const string add="Display:egypt · 苏联 USSR 0 → 2 (+2)",remove="Display:egypt · 美国 US 3 → 1 (-2)";
            Assert.That(text,Does.Contain(add).And.Contain(remove));Assert.That(text.IndexOf(add,StringComparison.Ordinal),Is.LessThan(text.IndexOf(remove,StringComparison.Ordinal)));
            var before=f.Controller.ReadView();Assert.That(f.View.TryOpenFeedback(),Is.True);Assert.That(f.View.ReadExpandedFeedback(),Is.EqualTo(text));f.View.CloseFeedback();SamePosition(before,f.Controller.ReadView());
        }
        [TestCase(false)][TestCase(true)] public async Task Completed_choice_has_exact_four_receipt_rows_even_if_US_played_card(bool us)
        {
            using var f=new Fixture(ComeconPlayTests.Ready(us),us?PlayerSide.US:PlayerSide.USSR);await f.Play("comecon");f.Controller.SelectSide(PlayerSide.USSR);foreach(var c in ComeconRulesTests.Targets())f.Controller.ToggleEventTarget(c);Assert.That((await f.Controller.ConfirmEventAsync()).Accepted,Is.True);
            var text=f.View.ReadPracticeFeedback();Assert.That(text,Does.Contain("Display:poland · 苏联 USSR 4 → 5 (+1)").And.Contain("Display:romania · 苏联 USSR 2 → 3 (+1)"));Assert.That(text.Split(new[]{" → "},StringSplitOptions.None).Length-1,Is.EqualTo(4));Assert.That(f.View.ReadEventFeedback(),Does.Contain("Display:finland · 苏联 USSR 1 → 2 (+1)"));
        }
        [Test] public async Task Relocation_reports_removal_then_addition_without_netting()
        {
            using var f=new Fixture(InfluenceRelocationPlayTests.Ready());await f.Play("de_stalinization");var c=f.Controller;c.ChangeRelocation(C("poland"),1);c.SelectRelocationPhase(true);c.ChangeRelocation(C("india"),1);Assert.That((await c.ConfirmEventAsync()).Accepted,Is.True);
            var text=f.View.ReadPracticeFeedback();const string from="Display:poland · 苏联 USSR 4 → 3 (-1)",to="Display:india · 苏联 USSR 0 → 1 (+1)";Assert.That(text,Does.Contain(from).And.Contain(to));Assert.That(text.IndexOf(from,StringComparison.Ordinal),Is.LessThan(text.IndexOf(to,StringComparison.Ordinal)));
        }
        [TestCase(false)][TestCase(true)] public async Task Zero_change_completion_is_explicit_for_automatic_and_chosen_events(bool choice)
        {
            using var f=new Fixture(choice?InfluenceRelocationPlayTests.Ready():Immediate("fidel",0,5,"cuba"));await f.Play(choice?"de_stalinization":"fidel");if(choice)Assert.That((await f.Controller.ConfirmEventAsync()).Accepted,Is.True);
            Assert.That(f.View.ReadPracticeFeedback(),Does.Contain("影响力无变化"));Assert.That(f.View.ReadPracticeFeedback(),Does.Not.Contain(" → "));Assert.That(f.Controller.ReadView().Event,Is.Null);
        }
        [Test] public async Task Waiting_empty_receipt_never_claims_completion_and_reader_preserves_draft()
        {
            using var f=new Fixture(ComeconPlayTests.Ready());await f.Play("comecon");f.Controller.ToggleEventTarget(C("poland"));var before=f.Controller.ReadView();Assert.That(f.View.TryOpenFeedback(),Is.True);var text=f.View.ReadExpandedFeedback();Assert.That(text,Does.Not.Contain("影响力无变化").And.Not.Contain(" → "));f.View.CloseFeedback();Assert.That(f.Controller.EventDraft,Is.EqualTo(new[]{C("poland")}));SamePosition(before,f.Controller.ReadView());
        }
        [Test] public async Task Rejected_choice_does_not_borrow_prior_success_details()
        {
            using var f=new Fixture(ComeconPlayTests.Ready());await CompleteComecon(f);var before=f.Controller.ReadView();Assert.That((await f.Controller.ConfirmEventAsync()).Accepted,Is.False);Assert.That(f.View.ReadPracticeFeedback(),Does.Not.Contain(" → "));Assert.That(f.View.ReadEventFeedback(),Does.StartWith("未执行"));SamePosition(before,f.Controller.ReadView());
        }
        [Test] public async Task Successful_replay_preserves_details_without_double_changes()
        {
            using var f=new Fixture(ComeconPlayTests.Ready());await CompleteComecon(f);var before=f.Controller.ReadView();var text=f.View.ReadPracticeFeedback();Assert.That(text,Does.Contain("4 → 5"));Assert.That((await f.Controller.RetryEventAsync()).Accepted,Is.True);Assert.That(f.View.ReadPracticeFeedback(),Is.EqualTo(text));SamePosition(before,f.Controller.ReadView());
        }
        [Test] public async Task Historical_event_replay_does_not_masquerade_as_new_details()
        {
            var s=ComeconPlayTests.Ready();using var f=new Fixture(s);await CompleteComecon(f);var c=f.Controller;c.SelectSide(PlayerSide.US);
            var id=c.ReadView().OwnHand.Select(x=>new CardId(x)).First(x=>s.Cards.Catalog.GetCard(x).Kind==CardKind.Event && s.Cards.Catalog.GetCard(x).Affiliation!=CardAffiliation.USSR);c.SelectCard(id);Assert.That((await c.PlayCardAsync(CardActionKind.Influence)).Accepted,Is.True);c.SelectSide(PlayerSide.USSR);var before=c.ReadView();await c.RetryEventAsync();Assert.That(f.View.ReadPracticeFeedback(),Does.Contain("历史事件结果").And.Not.Contain(" → "));Assert.That(f.View.ReadEventFeedback(),Does.Contain("历史事件结果").And.Not.Contain(" → "));SamePosition(before,c.ReadView());
        }
        [Test] public async Task Identity_and_same_version_reload_clear_current_rows_and_reader()
        {
            using var f=new Fixture(ComeconPlayTests.Ready());await CompleteComecon(f);Assert.That(f.View.TryOpenFeedback(),Is.True);f.Controller.SelectSide(PlayerSide.US);Assert.That(f.View.IsFeedbackReaderOpen,Is.False);Assert.That(f.View.ReadPracticeFeedback(),Does.Not.Contain(" → "));f.Controller.LoadScenario("fixture",PlayerSide.USSR,C("poland"));Assert.That(f.View.ReadPracticeFeedback(),Does.Not.Contain(" → "));Assert.That(f.View.ReadExpandedFeedback(),Is.Null);
        }
        [Test] public async Task Display_names_can_change_without_rewriting_receipts_or_state()
        {
            using var f=new Fixture(ComeconPlayTests.Ready());await CompleteComecon(f);var before=f.Controller.ReadView();Assert.That(f.View.ReadPracticeFeedback(),Does.Contain("Display:poland ·"));Assert.That(f.View.SwitchNameLanguage(),Is.True);Assert.That(f.View.ReadPracticeFeedback(),Does.Contain("Other:Display:poland · 苏联 USSR 4 → 5 (+1)"));SamePosition(before,f.Controller.ReadView());
        }
        [Test] public async Task Missing_display_name_falls_back_to_stable_country_id()
        {
            using var f=new Fixture(Immediate("fidel",0,0,"cuba"),missingName:true);await f.Play("fidel");Assert.That(f.View.ReadPracticeFeedback(),Does.Contain("country.cuba · 苏联 USSR 0 → 3 (+3)"));
        }
        [Test] public async Task Independent_reds_child_retains_parent_summary_and_country_delta_once()
        {
            using var f=new Fixture(FiveYearPlanPlayTests.Ready(actor:PlayerSide.US),PlayerSide.US);await f.Play("five_year_plan");Assert.That(f.Controller.ToggleEventTarget(C("yugoslavia")),Is.True);Assert.That((await f.Controller.ConfirmEventAsync()).Accepted,Is.True);var text=f.View.ReadPracticeFeedback();Assert.That(text,Does.Contain("五年计划").And.Contain("Display:yugoslavia · 美国 US 1 → 3 (+2)"));Assert.That(text.Split(new[]{"1 → 3"},StringSplitOptions.None).Length-1,Is.EqualTo(1));
        }
        [Test] public async Task Two_immediate_headline_events_keep_both_rows_in_execution_order()
        {
            using var f=new Fixture(HeadlineFlowTests.Ready("fidel","romanian_abdication"),PlayerSide.US);var c=f.Controller;c.SelectCard(Card("fidel"));await c.SubmitSelectedHeadlineAsync();Assert.That(f.View.ReadHeadlineFeedback(),Is.Null);c.SelectSide(PlayerSide.USSR);c.SelectCard(Card("romanian_abdication"));Assert.That((await c.SubmitSelectedHeadlineAsync()).Accepted,Is.True);
            var text=f.View.ReadPracticeFeedback();Assert.That(text,Does.Contain("Display:romania · 苏联 USSR 0 → 3 (+3)").And.Contain("Display:cuba · 苏联 USSR 0 → 3 (+3)"));Assert.That(text.IndexOf("Display:cuba",StringComparison.Ordinal),Is.LessThan(text.IndexOf("Display:romania",StringComparison.Ordinal)));Assert.That(c.CurrentHeadlineFeedback.Count,Is.EqualTo(2));Assert.That(f.View.TryOpenFeedback(),Is.True);Assert.That(f.View.ReadExpandedFeedback(),Is.EqualTo(text));
        }
        [Test] public async Task Headline_child_then_regular_effect_keeps_parent_and_no_duplicate_delta()
        {
            var prepared=FiveYearPlanFlowTests.HeadlinePrepared("five_year_plan","fidel","independent_reds");using var f=new Fixture(HeadlineFlowTests.Start(prepared),PlayerSide.US);var c=f.Controller;c.SelectCard(Card("five_year_plan"));await c.SubmitSelectedHeadlineAsync();c.SelectSide(PlayerSide.USSR);c.SelectCard(Card("fidel"));Assert.That((await c.SubmitSelectedHeadlineAsync()).Accepted,Is.True);c.SelectSide(PlayerSide.US);c.ToggleEventTarget(C("yugoslavia"));Assert.That((await c.ConfirmEventAsync()).Accepted,Is.True);
            var text=f.View.ReadPracticeFeedback();Assert.That(text,Does.Contain("五年计划").And.Contain("Display:yugoslavia · 美国 US 1 → 3 (+2)").And.Contain("Display:cuba · 苏联 USSR 0 → 3 (+3)"));Assert.That(text.Split(new[]{"1 → 3"},StringSplitOptions.None).Length-1,Is.EqualTo(1));Assert.That(text.IndexOf("Display:yugoslavia",StringComparison.Ordinal),Is.LessThan(text.IndexOf("Display:cuba",StringComparison.Ordinal)));
        }
    }
}
