using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ColdWarStruggle.Application;
using ColdWarStruggle.Core;
using ColdWarStruggle.Presentation;
using NUnit.Framework;

namespace ColdWarStruggle.Tests
{
    public class AllocationDemoControllerTests
    {
        static CountryId Id(string name)=>BudgetedRemovalTests.Id(name);
        public static DemoController Controller(string card="suez_crisis")
        {
            var catalog=AllocationDemoCatalogTests.Catalog();var c=new DemoController(catalog.CreateHost);
            var scenario=catalog.Scenarios.Single(s=>s.Id=="demo.event."+card);
            c.LoadScenario(scenario.Id,scenario.Actor,scenario.SelectedCountry);return c;
        }
        static async Task<DemoController> Waiting(string card="suez_crisis")
        { var c=Controller(card);Assert.That((await c.BeginEventAsync()).Accepted,Is.True);c.SelectSide(PlayerSide.USSR);return c; }
        static void FillSuez(DemoController c)
        {
            Assert.That(c.ChangeEventAllocation(Id("france"),1),Is.True);Assert.That(c.ChangeEventAllocation(Id("france"),1),Is.True);
            Assert.That(c.ChangeEventAllocation(Id("uk"),1),Is.True);
        }

        [Test] public async Task Quantity_draft_changes_no_authoritative_influence_and_respects_each_capacity()
        {
            var c=await Waiting();c.EventTargets();var before=c.ReadView();var revision=c.InteractionRevision;
            Assert.That(c.CanChangeEventAllocation(Id("france"),1),Is.True);
            Assert.That(c.ChangeEventAllocation(Id("france"),1),Is.True);Assert.That(c.ChangeEventAllocation(Id("france"),1),Is.True);
            Assert.That(c.CanChangeEventAllocation(Id("france"),1),Is.False);Assert.That(c.ChangeEventAllocation(Id("france"),1),Is.False);
            Assert.That(c.ChangeEventAllocation(Id("israel"),1),Is.False);Assert.That(c.ChangeEventAllocation(Id("imaginary"),1),Is.False);
            Assert.That(c.ReadView().Version,Is.EqualTo(before.Version));Assert.That(c.ReadView().EventSequence,Is.EqualTo(before.EventSequence));
            Assert.That(c.ReadView().Countries.Single(x=>x.Definition.Id.Equals(Id("france"))).US,Is.EqualTo(3));
            Assert.That(c.EventDraft,Is.EqualTo(new[]{Id("france")}));Assert.That(c.AllocationDraft.Single().Amount,Is.EqualTo(2));
            Assert.That(c.InteractionRevision,Is.EqualTo(revision+2));Assert.That(c.CanConfirmEvent,Is.False);
            Assert.Throws<NotSupportedException>(()=>((IList<InfluenceAllocation>)c.AllocationDraft).Clear());
        }

        [Test] public async Task Total_budget_stops_extra_points_even_when_a_country_has_room()
        {
            var c=await Waiting("socialist_governments");
            c.ChangeEventAllocation(Id("austria"),1);c.ChangeEventAllocation(Id("austria"),1);c.ChangeEventAllocation(Id("finland"),1);
            Assert.That(c.CanConfirmEvent,Is.True);Assert.That(c.CanChangeEventAllocation(Id("finland"),1),Is.False);
            Assert.That(c.ChangeEventAllocation(Id("finland"),1),Is.False);Assert.That(c.ChangeEventAllocation(Id("france"),1),Is.False);
            Assert.That(c.AllocationDraft.Sum(a=>a.Amount),Is.EqualTo(3));
        }

        [TestCase(0)] [TestCase(2)] [TestCase(-2)] [TestCase(int.MinValue)] [TestCase(int.MaxValue)]
        public async Task Only_single_step_edits_are_allowed(int delta)
        {
            var c=await Waiting();c.ChangeEventAllocation(Id("france"),1);var revision=c.InteractionRevision;
            Assert.That(c.CanChangeEventAllocation(Id("france"),delta),Is.False);Assert.That(c.ChangeEventAllocation(Id("france"),delta),Is.False);
            Assert.That(c.AllocationDraft.Single().Amount,Is.EqualTo(1));Assert.That(c.InteractionRevision,Is.EqualTo(revision));
        }

        [Test] public async Task Decrement_and_clear_are_local_and_remove_zero_amount_entries()
        {
            var c=await Waiting();c.ChangeEventAllocation(Id("france"),1);Assert.That(c.ChangeEventAllocation(Id("france"),-1),Is.True);
            Assert.That(c.AllocationDraft,Is.Empty);Assert.That(c.EventDraft,Is.Empty);Assert.That(c.ChangeEventAllocation(Id("france"),-1),Is.False);
            FillSuez(c);var revision=c.InteractionRevision;c.ClearEventDraft();
            Assert.That(c.AllocationDraft,Is.Empty);Assert.That(c.EventDraft,Is.Empty);Assert.That(c.CanConfirmEvent,Is.False);
            Assert.That(c.InteractionRevision,Is.EqualTo(revision+1));Assert.That(c.ReadView().Version,Is.EqualTo(1));
        }

        [Test] public async Task Confirm_submits_quantity_command_and_retry_does_not_remove_again()
        {
            var c=await Waiting();FillSuez(c);Assert.That(c.CanConfirmEvent,Is.True);
            var done=await c.ConfirmEventAsync();Assert.That(done.Accepted,Is.True);Assert.That(done.View.Version,Is.EqualTo(2));
            Assert.That(done.View.Countries.Single(x=>x.Definition.Id.Equals(Id("france"))).US,Is.EqualTo(1));
            Assert.That(done.View.Countries.Single(x=>x.Definition.Id.Equals(Id("uk"))).US,Is.Zero);
            Assert.That(c.AllocationDraft,Is.Empty);Assert.That(c.EventDraft,Is.Empty);Assert.That(c.CanConfirmEvent,Is.False);
            Assert.That(c.CanRetryEvent,Is.True);Assert.That(await c.RetryEventAsync(),Is.SameAs(done));Assert.That(c.ReadView().Version,Is.EqualTo(2));
            Assert.That(c.ChangeEventAllocation(Id("france"),1),Is.False);
        }

        [Test] public async Task Incomplete_confirmation_preserves_editable_draft_and_correction_uses_new_id()
        {
            var c=await Waiting();c.ChangeEventAllocation(Id("france"),1);
            var rejected=await c.ConfirmEventAsync();Assert.That(rejected.Error,Is.EqualTo(RuleError.InvalidSelection));
            Assert.That(c.AllocationDraft.Single().Amount,Is.EqualTo(1));Assert.That(c.ReadView().Version,Is.EqualTo(1));
            c.ChangeEventAllocation(Id("france"),1);c.ChangeEventAllocation(Id("uk"),1);
            Assert.That((await c.ConfirmEventAsync()).Accepted,Is.True);
        }

        [Test] public async Task Side_changes_and_reset_clear_quantities_but_same_side_keeps_them()
        {
            var c=Controller();Assert.That(c.ChangeEventAllocation(Id("france"),1),Is.False);
            await c.BeginEventAsync();Assert.That(c.ChangeEventAllocation(Id("france"),1),Is.False);
            c.SelectSide(PlayerSide.USSR);FillSuez(c);c.SelectSide(PlayerSide.USSR);Assert.That(c.AllocationDraft.Sum(a=>a.Amount),Is.EqualTo(3));
            c.SelectSide(PlayerSide.US);Assert.That(c.AllocationDraft,Is.Empty);Assert.That(c.CanConfirmEvent,Is.False);
            c.SelectSide(PlayerSide.USSR);FillSuez(c);var old=c.ReadView().GameId;
            c.LoadScenario("demo.event.suez_crisis",PlayerSide.US,Id("france"));
            Assert.That(c.AllocationDraft,Is.Empty);Assert.That(c.CanRetryEvent,Is.False);Assert.That(c.ReadView().GameId,Is.Not.EqualTo(old));
        }

        [Test] public async Task External_completion_invalidates_a_draft_before_it_can_be_submitted()
        {
            var catalog=AllocationDemoCatalogTests.Catalog();var h=catalog.CreateHost("demo.event.suez_crisis");var c=new DemoController(_=>h);
            c.LoadScenario("demo.event.suez_crisis",PlayerSide.US,Id("france"));await c.BeginEventAsync();c.SelectSide(PlayerSide.USSR);FillSuez(c);
            var s=h.UssrSession;var v=s.ReadView();var command=EventCommandEnvelope.Allocate("outside",v.GameId,v.Version,v.Event.ExecutionId,v.Event.DecisionId,c.AllocationDraft);
            Assert.That((await s.SubmitEventAsync(command)).Accepted,Is.True);
            Assert.That(c.AllocationDraft,Is.Empty);Assert.That(c.EventDraft,Is.Empty);Assert.That(c.CanConfirmEvent,Is.False);
        }

        [Test] public async Task Map_switch_keeps_quantities_and_map_click_only_changes_selected_country()
        {
            var c=await Waiting();FillSuez(c);var snapshot=c.ReadView();var catalog=AllocationDemoCatalogTests.Catalog();
            using var presenter=new MapPresenter(c,catalog.Names);var tiles=DiagnosticMapView.Create(DiagnosticMapKind.Tiles);presenter.Attach(tiles);
            Assert.That(tiles.Frame.Countries.Single(x=>x.Id.Equals(Id("france"))).Drafted,Is.True);
            tiles.TrySelect(Id("uk"));Assert.That(c.SelectedCountry,Is.EqualTo(Id("uk")));Assert.That(c.AllocationDraft.Sum(a=>a.Amount),Is.EqualTo(3));
            var table=DiagnosticMapView.Create(DiagnosticMapKind.Table);presenter.Attach(table);
            Assert.That(tiles.IsDisposed,Is.True);Assert.That(table.Frame.Countries.Single(x=>x.Id.Equals(Id("uk"))).Drafted,Is.True);
            Assert.That(c.ReadView().Version,Is.EqualTo(snapshot.Version));Assert.That(c.ReadView().GameId,Is.EqualTo(snapshot.GameId));
            Assert.That(c.CanConfirmEvent,Is.True);Assert.That((await c.ConfirmEventAsync()).Accepted,Is.True);
        }

        [Test] public async Task Quantity_edit_invalidates_the_prior_map_frame_before_refresh()
        {
            var c=await Waiting();var catalog=AllocationDemoCatalogTests.Catalog();using var presenter=new MapPresenter(c,catalog.Names);
            var view=DiagnosticMapView.Create(DiagnosticMapKind.Tiles);presenter.Attach(view);
            Assert.That(c.ChangeEventAllocation(Id("france"),1),Is.True);view.TrySelect(Id("uk"));
            Assert.That(c.SelectedCountry,Is.EqualTo(Id("france")));presenter.Refresh();view.TrySelect(Id("uk"));
            Assert.That(c.SelectedCountry,Is.EqualTo(Id("uk")));Assert.That(c.AllocationDraft.Single().Country,Is.EqualTo(Id("france")));
        }

        [Test] public async Task Existing_country_set_event_does_not_accept_quantity_edits()
        {
            var catalog=EventDemoCatalogTests.Catalog();var c=new DemoController(catalog.CreateHost);
            c.LoadScenario("demo.event.decolonization",PlayerSide.US,Id("angola"));await c.BeginEventAsync();c.SelectSide(PlayerSide.USSR);
            Assert.That(c.ChangeEventAllocation(Id("angola"),1),Is.False);Assert.That(c.ToggleEventTarget(Id("angola")),Is.True);
            Assert.That(c.EventDraft,Is.EqualTo(new[]{Id("angola")}));Assert.That(c.AllocationDraft,Is.Empty);
        }
    }
}
