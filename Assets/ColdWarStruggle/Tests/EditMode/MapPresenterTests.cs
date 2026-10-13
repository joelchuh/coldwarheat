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
    public class MapPresenterTests
    {
        static readonly IReadOnlyDictionary<string,string> Names=new Dictionary<string,string>();
        static DemoController Ordinary()
        { var data=DemoTests.Catalog();var c=new DemoController(data.CreateHost);var s=data.Scenarios[0];c.LoadScenario(s.Id,s.Actor,s.SelectedCountry);return c; }
        static DemoController Event()
        { var c=new DemoController(_=>new LocalGameHost(InfluenceEventFlowTests.State(gameId:Guid.NewGuid().ToString("N")),true));c.LoadScenario("fixture",PlayerSide.US,MapViewTests.Angola);return c; }
        sealed class Probe : IMapView
        {
            Action<MapIntent> handler;
            public Action<MapIntent> Captured;
            public MapViewContext Context;
            public MapFrame Frame;
            public bool Disposed;
            public bool FailPresentation;
            public event Action<MapIntent> Intent { add {handler+=value;Captured=handler;} remove {handler-=value;} }
            public void Initialize(MapViewContext context) {Context=context;Frame=null;}
            public bool Present(MapFrame frame) {if(FailPresentation)throw new InvalidOperationException("Render failed.");Frame=frame;return true;}
            public void FocusOn(CountryId id) {}
            public void Clear() {Frame=null;}
            public void Dispose() {Disposed=true;}
            public MapIntent Click(CountryId id)=>new MapIntent(Frame.ContextId,Frame.Version,Frame.Sequence,Frame.InteractionKey,id);
            public void Send(CountryId id)=>handler?.Invoke(Click(id));
        }
        [Test] public void Swap_preserves_game_state_and_only_exposes_public_map_fields()
        {
            var c=Ordinary();var before=c.ReadView();using var p=new MapPresenter(c,Names);var a=new Probe();p.Attach(a);var old=a.Context.Id;var b=new Probe();p.Attach(b);
            Assert.That(a.Disposed,Is.True);Assert.That(b.Context.Id,Is.Not.EqualTo(old));Assert.That(c.ReadView().GameId,Is.EqualTo(before.GameId));Assert.That(c.ReadView().Version,Is.EqualTo(before.Version));Assert.That(c.ReadView().RemainingOps,Is.EqualTo(before.RemainingOps));
            Assert.That(b.Frame.Countries.Count,Is.EqualTo(84));Assert.That(b.Context.Nodes.Select(x=>x.Id),Is.EquivalentTo(before.Countries.Select(x=>x.Definition.Id)));
            Assert.That(typeof(MapFrame).GetProperties().Any(x=>x.Name.Contains("Hand")),Is.False);
        }
        [Test] public void Ordinary_pick_only_changes_selection_and_retired_delegate_is_ignored()
        {
            var c=Ordinary();using var p=new MapPresenter(c,Names);var a=new Probe();p.Attach(a);var old=a.Click(MapViewTests.Angola);var callback=a.Captured;
            var b=new Probe();p.Attach(b);callback(old);Assert.That(c.SelectedCountry,Is.EqualTo(MapViewTests.France));
            b.Send(MapViewTests.Angola);Assert.That(c.SelectedCountry,Is.EqualTo(MapViewTests.Angola));Assert.That(c.ReadView().Version,Is.Zero);
        }
        [Test] public void Side_switch_roundtrip_rejects_old_input_even_before_refresh()
        {
            var c=Ordinary();using var p=new MapPresenter(c,Names);var v=new Probe();p.Attach(v);var old=v.Click(MapViewTests.Angola);var context=v.Context.Id;
            c.SelectSide(PlayerSide.USSR);c.SelectSide(PlayerSide.US);v.Captured(old);Assert.That(c.SelectedCountry,Is.EqualTo(MapViewTests.France));
            p.Refresh();Assert.That(v.Context.Id,Is.Not.EqualTo(context));
        }
        [Test] public async Task State_advance_reset_wrong_sequence_and_unknown_node_cannot_use_old_frame()
        {
            var c=Ordinary();using var p=new MapPresenter(c,Names);var v=new Probe();p.Attach(v);var old=v.Click(MapViewTests.Angola);
            await c.SubmitAsync(1);v.Captured(old);Assert.That(c.SelectedCountry,Is.EqualTo(MapViewTests.France));
            p.Refresh();v.Captured(new MapIntent(v.Frame.ContextId,v.Frame.Version,v.Frame.Sequence,"different-mode",MapViewTests.Angola));
            v.Captured(new MapIntent(v.Frame.ContextId,v.Frame.Version,v.Frame.Sequence-1,v.Frame.InteractionKey,MapViewTests.Angola));v.Send(new CountryId("unknown"));Assert.That(c.SelectedCountry,Is.EqualTo(MapViewTests.France));
            var game=c.ReadView().GameId;var saved=v.Click(MapViewTests.Angola);c.LoadScenario(DemoTests.Catalog().Scenarios[0].Id,PlayerSide.US,MapViewTests.France);v.Captured(saved);Assert.That(c.ReadView().GameId,Is.Not.EqualTo(game));Assert.That(c.SelectedCountry,Is.EqualTo(MapViewTests.France));
        }
        [Test] public async Task Event_pick_edits_draft_only_and_same_version_stale_frame_cannot_toggle_twice()
        {
            var c=Event();await c.BeginEventAsync();c.SelectSide(PlayerSide.USSR);using var p=new MapPresenter(c,Names);var v=new Probe();p.Attach(v);var old=v.Click(MapViewTests.Angola);
            v.Captured(old);Assert.That(c.EventDraft,Is.EquivalentTo(new[]{MapViewTests.Angola}));Assert.That(c.ReadView().Version,Is.EqualTo(1));
            Assert.That(v.Frame.Countries.Single(x=>x.Id.Equals(MapViewTests.Angola)).Drafted,Is.True);
            v.Captured(old);Assert.That(c.EventDraft.Count,Is.EqualTo(1));
            var another=new Probe();p.Attach(another);Assert.That(c.EventDraft.Count,Is.EqualTo(1));Assert.That(another.Frame.Countries.Single(x=>x.Id.Equals(MapViewTests.Angola)).Drafted,Is.True);
        }
        [Test] public async Task Wrong_owner_map_can_inspect_but_has_no_event_targets_or_draft_changes()
        {
            var c=Event();await c.BeginEventAsync();using var p=new MapPresenter(c,Names);var v=new Probe();p.Attach(v);
            Assert.That(v.Frame.Countries.Any(x=>x.Allowed),Is.False);v.Send(InfluenceEventFlowTests.Id("kenya"));Assert.That(c.EventDraft,Is.Empty);Assert.That(c.ReadView().Version,Is.EqualTo(1));
        }
        [Test] public void Presenter_dispose_removes_input_and_rejects_further_use()
        {
            var c=Ordinary();var p=new MapPresenter(c,Names);var v=new Probe();p.Attach(v);var old=v.Click(MapViewTests.Angola);p.Dispose();p.Dispose();v.Captured(old);
            Assert.That(c.SelectedCountry,Is.EqualTo(MapViewTests.France));Assert.That(v.Disposed,Is.True);Assert.Throws<ObjectDisposedException>(()=>p.Refresh());Assert.Throws<ObjectDisposedException>(()=>p.Attach(new Probe()));
        }
        [Test] public void Later_presentation_exception_retires_view_and_old_input()
        {
            var c=Ordinary();using var p=new MapPresenter(c,Names);var v=new Probe();p.Attach(v);var old=v.Click(MapViewTests.Angola);v.FailPresentation=true;
            Assert.Throws<InvalidOperationException>(()=>p.Refresh());v.Captured(old);Assert.That(c.SelectedCountry,Is.EqualTo(MapViewTests.France));Assert.That(v.Disposed,Is.True);
        }
        [Test] public async Task External_draft_clear_invalidates_input_before_next_refresh()
        {
            var c=Event();await c.BeginEventAsync();c.SelectSide(PlayerSide.USSR);using var p=new MapPresenter(c,Names);var v=new Probe();p.Attach(v);v.Send(MapViewTests.Angola);
            var old=v.Click(InfluenceEventFlowTests.Id("kenya"));c.ClearEventDraft();v.Captured(old);
            Assert.That(c.EventDraft,Is.Empty);Assert.That(c.SelectedCountry,Is.EqualTo(MapViewTests.Angola));Assert.That(c.ReadView().Version,Is.EqualTo(1));
        }
    }
}
