using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ColdWarStruggle.Application;
using ColdWarStruggle.Core;
using ColdWarStruggle.Infrastructure;
using ColdWarStruggle.Presentation;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public sealed class InfluenceRelocationDemoTests
    {
        static CountryId C(string n)=>InfluenceRelocationPlayTests.C(n);
        internal static string Fixture()=>PlayInfluenceEventTests.Read("Data/Demo/relocation-scenarios.json");
        internal static DemoCatalog Bind(DemoCatalog c)=>c.WithInfluenceRelocation(InfluenceRelocationDefinitionTests.Data(),InfluenceRelocationDefinitionTests.Schema(),PlayInfluenceEventTests.Read("Data/Cards/deluxe-2015/cards.json"));
        internal static DemoCatalog Catalog()=>Bind(OlympicDemoTests.Catalog()).WithRelocationScenarios(Fixture());
        static DemoController Controller(bool us=false)
        {var c=Catalog();var s=c.Scenarios.Single(x=>x.Id=="demo.practice.relocation_"+(us?"us":"ussr"));var v=new DemoController(c.CreateHost);v.LoadScenario(s.Id,s.Actor,s.SelectedCountry);return v;}
        static async Task<DemoController> Waiting(bool us=false)
        {var c=Controller(us);c.SelectCard(InfluenceRelocationPlayTests.Card);Assert.That((await c.PlayCardAsync(CardActionKind.Event)).Accepted,Is.True);c.SelectSide(PlayerSide.USSR);return c;}
        static void Fill(DemoController c)
        {c.SelectRelocationPhase(false);Assert.That(c.ChangeRelocation(C("poland"),1),Is.True);Assert.That(c.ChangeRelocation(C("poland"),1),Is.True);c.SelectRelocationPhase(true);Assert.That(c.ChangeRelocation(C("india"),1),Is.True);Assert.That(c.ChangeRelocation(C("india"),1),Is.True);}
        [Test] public void Registration_retains_all_prior_definitions_and_rejects_duplicate()
        {
            var before=OlympicDemoTests.Catalog();var after=Bind(before);Assert.That(after.Scenarios.Count,Is.EqualTo(before.Scenarios.Count));
            foreach(var scene in before.Scenarios.Where(s=>s.PracticeDefinition!=null))
            {var a=scene.PracticeDefinition;var b=after.Scenarios.Single(s=>s.Id==scene.Id).PracticeDefinition;
             Assert.That(b.Olympic,Is.SameAs(a.Olympic));Assert.That(b.SpaceEvent,Is.SameAs(a.SpaceEvent));Assert.That(b.Wars,Is.SameAs(a.Wars));Assert.That(b.FiveYearPlan,Is.SameAs(a.FiveYearPlan));Assert.That(b.Blockade,Is.SameAs(a.Blockade));Assert.That(b.HeadlineParameters,Is.SameAs(a.HeadlineParameters));Assert.That(b.FinalScoring,Is.SameAs(a.FinalScoring));Assert.That(b.Risk,Is.SameAs(a.Risk));Assert.That(b.InfluenceEvents.Effects.Count,Is.EqualTo(a.InfluenceEvents.Effects.Count+1));}
            Assert.Throws<ArgumentException>(()=>Bind(after));Assert.Throws<ArgumentException>(()=>Bind(PlayableDemoTests.Catalog()));
        }
        [Test] public void Two_explicit_fixtures_start_with_card_owned_and_no_pending_event()
        {
            var c=Catalog();Assert.That(c.Scenarios.Count,Is.EqualTo(37));
            foreach(var row in JObject.Parse(Fixture())["scenarios"])
            {var scene=c.Scenarios.Single(s=>s.Id==(string)row["id"]);var h=c.CreateHost(scene.Id);var s=scene.Actor==PlayerSide.US?h.UsSession:h.UssrSession;var v=s.ReadView();
             Assert.That(v.Practice.Actor,Is.EqualTo(scene.Actor));Assert.That(v.Practice.CompletedActions,Is.EqualTo((int)row["ordinary_actions_to_prepare"]));Assert.That(v.OwnHand,Has.Member(InfluenceRelocationPlayTests.Card.Value));Assert.That(v.Event,Is.Null);Assert.That(v.Cards.ResolvingCount,Is.Zero);Assert.That(v.Countries.Single(x=>x.Definition.Id.Equals(C("poland"))).USSR,Is.EqualTo(4));}
        }
        [TestCase("scope")][TestCase("extra")][TestCase("source")][TestCase("turn")][TestCase("count")][TestCase("card")][TestCase("duplicate-hand")][TestCase("late")][TestCase("duplicate-country")][TestCase("negative")][TestCase("unsafe-prep")][TestCase("missing")]
        public void Invalid_fixture_does_not_replace_source(string defect)
        {
            var c=Bind(OlympicDemoTests.Catalog());var d=JObject.Parse(Fixture());var row=d["scenarios"][0];
            switch(defect)
            {case "scope":d["scope"]="release";break;case "extra":row["max_total"]=99;break;case "source":d["source_scenario_id"]="missing";break;case "turn":d["turn_number"]=1;break;case "count":((JArray)d["scenarios"]).RemoveAt(0);break;case "card":row["card"]="card.fidel";break;case "duplicate-hand":row["ussr_hand"][0]=row["us_hand"][0].DeepClone();break;case "late":row["us_hand"][0]="card.wargames";break;case "duplicate-country":((JArray)row["influence"]).Add(row["influence"][0].DeepClone());break;case "negative":row["influence"][0]["ussr"]=-1;break;case "unsafe-prep":row["ordinary_actions_to_prepare"]=99;break;case "missing":((JObject)row).Remove("actor");break;}
            Assert.That(()=>c.WithRelocationScenarios(d.ToString()),Throws.InstanceOf<ArgumentException>());Assert.That(c.Scenarios.Count,Is.EqualTo(35));
        }
        [Test] public async Task Draft_is_local_bounded_readonly_and_balanced()
        {
            var c=await Waiting();var before=c.ReadView();Assert.That(c.CanConfirmEvent,Is.True);Assert.That(c.RelocationEditingDestinations,Is.False);
            Assert.That(c.ChangeRelocation(C("india"),1),Is.False);Assert.That(c.ChangeRelocation(C("poland"),1),Is.True);Assert.That(c.CanConfirmEvent,Is.False);
            c.SelectRelocationPhase(true);Assert.That(c.ChangeRelocation(C("poland"),1),Is.False);Assert.That(c.ChangeRelocation(C("cuba"),1),Is.False);Assert.That(c.ChangeRelocation(C("india"),1),Is.True);Assert.That(c.ChangeRelocation(C("angola"),1),Is.False);
            Assert.That(c.CanConfirmEvent,Is.True);Assert.That(c.RelocationSourceDraft.Single().Amount,Is.EqualTo(1));Assert.That(c.RelocationDestinationDraft.Single().Amount,Is.EqualTo(1));
            Assert.Throws<NotSupportedException>(()=>((IList<InfluenceAllocation>)c.RelocationSourceDraft).Clear());Assert.Throws<NotSupportedException>(()=>((IList<InfluenceAllocation>)c.RelocationDestinationDraft).Clear());
            Assert.That(c.ReadView().Version,Is.EqualTo(before.Version));Assert.That(c.ReadView().Countries.Select(x=>x.USSR),Is.EqualTo(before.Countries.Select(x=>x.USSR)));
            c.SelectRelocationPhase(false);Assert.That(c.ChangeRelocation(C("poland"),-1),Is.True);Assert.That(c.CanConfirmEvent,Is.False);c.SelectRelocationPhase(true);Assert.That(c.ChangeRelocation(C("india"),-1),Is.True);Assert.That(c.CanConfirmEvent,Is.True);
        }
        [TestCase(0)][TestCase(2)][TestCase(-2)][TestCase(int.MaxValue)][TestCase(int.MinValue)]
        public async Task Only_single_point_edits_are_accepted(int delta)
        {var c=await Waiting();Assert.That(c.ChangeRelocation(C("poland"),delta),Is.False);Assert.That(c.RelocationSourceDraft,Is.Empty);}
        [Test] public async Task Full_budget_and_destination_cap_are_read_from_query()
        {
            var c=await Waiting();for(int i=0;i<4;i++)Assert.That(c.ChangeRelocation(C("poland"),1),Is.True);Assert.That(c.ChangeRelocation(C("romania"),1),Is.False);
            c.SelectRelocationPhase(true);for(int i=0;i<2;i++)Assert.That(c.ChangeRelocation(C("india"),1),Is.True);Assert.That(c.ChangeRelocation(C("india"),1),Is.False);
            for(int i=0;i<2;i++)Assert.That(c.ChangeRelocation(C("angola"),1),Is.True);Assert.That(c.CanConfirmEvent,Is.True);
        }
        [TestCase(false)][TestCase(true)] public async Task Confirm_and_retry_migrate_once_for_either_root_actor(bool us)
        {
            var c=await Waiting(us);Fill(c);var r=await c.ConfirmEventAsync();Assert.That(r.Accepted,Is.True,r.Error.ToString());Assert.That(r.Changes,Has.Count.EqualTo(2));var v=c.ReadView();
            Assert.That(v.Countries.Single(x=>x.Definition.Id.Equals(C("poland"))).USSR,Is.EqualTo(2));Assert.That(v.Countries.Single(x=>x.Definition.Id.Equals(C("india"))).USSR,Is.EqualTo(2));Assert.That(v.Cards.RemovedCount,Is.EqualTo(1));Assert.That(v.Event,Is.Null);
            Assert.That(c.EventDraft,Is.Empty);Assert.That(c.RelocationDestinationDraft,Is.Empty);Assert.That(c.CanConfirmEvent,Is.False);Assert.That(await c.RetryEventAsync(),Is.SameAs(r));Assert.That(c.ReadView().Version,Is.EqualTo(v.Version));
        }
        [Test] public async Task Explicit_zero_completes_and_failed_unbalanced_choice_keeps_draft()
        {
            var c=await Waiting();c.ChangeRelocation(C("poland"),1);var rejected=await c.ConfirmEventAsync();Assert.That(rejected.Error,Is.EqualTo(RuleError.InvalidSelection));Assert.That(c.RelocationSourceDraft.Single().Amount,Is.EqualTo(1));
            c.ClearEventDraft();Assert.That(c.RelocationEditingDestinations,Is.False);Assert.That(c.CanConfirmEvent,Is.True);var done=await c.ConfirmEventAsync();Assert.That(done.Accepted,Is.True);Assert.That(done.Changes,Is.Empty);Assert.That(c.ReadView().Cards.RemovedCount,Is.EqualTo(1));
        }
        [Test] public async Task Side_reset_and_external_completion_invalidate_both_drafts()
        {
            var c=await Waiting();Fill(c);c.SelectSide(PlayerSide.USSR);Assert.That(c.RelocationDestinationDraft,Has.Count.EqualTo(1));c.SelectSide(PlayerSide.US);Assert.That(c.EventDraft,Is.Empty);Assert.That(c.ChangeRelocation(C("poland"),1),Is.False);
            c.SelectSide(PlayerSide.USSR);Fill(c);c.LoadScenario("demo.practice.relocation_ussr",PlayerSide.USSR,C("poland"));Assert.That(c.EventDraft,Is.Empty);Assert.That(c.RelocationEditingDestinations,Is.False);
            var catalog=Catalog();var host=catalog.CreateHost("demo.practice.relocation_ussr");c=new DemoController(_=>host);c.LoadScenario("x",PlayerSide.USSR,C("poland"));c.SelectCard(InfluenceRelocationPlayTests.Card);await c.PlayCardAsync(CardActionKind.Event);Fill(c);var v=c.ReadView();
            var command=EventCommandEnvelope.Relocate("external",v.GameId,v.Version,v.Event.ExecutionId,v.Event.DecisionId,c.RelocationSourceDraft,c.RelocationDestinationDraft);
            Assert.That((await host.UssrSession.SubmitEventAsync(command)).Accepted,Is.True);Assert.That(c.EventDraft,Is.Empty);Assert.That(c.RelocationSourceDraft,Is.Empty);Assert.That(c.RelocationDestinationDraft,Is.Empty);
        }
        [Test] public async Task Map_switch_preserves_both_drafts_and_phase_changes_targets()
        {
            var c=await Waiting();var catalog=Catalog();using var p=new MapPresenter(c,catalog.Names);var tiles=DiagnosticMapView.Create(DiagnosticMapKind.Tiles);p.Attach(tiles);
            Assert.That(tiles.Frame.Countries.Single(x=>x.Id.Equals(C("poland"))).Allowed,Is.True);Assert.That(tiles.Frame.Countries.Single(x=>x.Id.Equals(C("india"))).Allowed,Is.False);
            c.ChangeRelocation(C("poland"),1);c.SelectRelocationPhase(true);p.Refresh();Assert.That(tiles.Frame.Countries.Single(x=>x.Id.Equals(C("india"))).Allowed,Is.True);Assert.That(tiles.Frame.Countries.Single(x=>x.Id.Equals(C("poland"))).Allowed,Is.False);
            tiles.TrySelect(C("india"));Assert.That(c.SelectedCountry,Is.EqualTo(C("india")));Assert.That(c.RelocationDestinationDraft,Is.Empty);c.ChangeRelocation(C("india"),1);
            var table=DiagnosticMapView.Create(DiagnosticMapKind.Table);p.Attach(table);Assert.That(table.Frame.Countries.Single(x=>x.Id.Equals(C("india"))).Drafted,Is.True);Assert.That(table.Frame.Countries.Single(x=>x.Id.Equals(C("poland"))).Drafted,Is.True);Assert.That(c.CanConfirmEvent,Is.True);
        }
    }
}
