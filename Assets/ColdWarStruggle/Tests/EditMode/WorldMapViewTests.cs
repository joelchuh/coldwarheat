using System;
using System.Linq;
using System.Threading.Tasks;
using ColdWarStruggle.Core;
using ColdWarStruggle.Presentation;
using NUnit.Framework;
using UnityEngine;
namespace ColdWarStruggle.Tests
{
    public class WorldMapViewTests
    {
        static readonly Rect Area=new Rect(0,0,614,400);
        [Test] public void Default_zoom_hover_candidate_is_drawn_above_overlapping_selection()
        {
            var c=Controller(out var names);c.SelectCountry(new CountryId("country.israel"));var assets=WorldMapDataTests.Assets();using var p=new MapPresenter(c,names);var v=new WorldMapView(assets);p.Attach(v);
            var jordan=new CountryId("country.jordan");var point=v.Viewport.Project(assets.Anchors[jordan.Value],Area);
            Assert.That(Vector2.Distance(point,v.Viewport.Project(assets.Anchors["country.israel"],Area)),Is.LessThan(8));
            Assert.That(v.HitTest(point,Area),Is.EqualTo(jordan));var visible=v.VisibleMarkers(Area,point);
            Assert.That(visible.Last().Id,Is.EqualTo(jordan),"The actual candidate must be the topmost painted marker");
            Assert.That(v.SelectAt(point,Area),Is.True);Assert.That(c.SelectedCountry,Is.EqualTo(jordan));
        }
        [Test] public void Marker_outside_clipped_viewport_is_not_drawn_or_picked_near_its_edge()
        {
            var c=Controller(out var names);var assets=WorldMapDataTests.Assets();using var p=new MapPresenter(c,names);var v=new WorldMapView(assets);p.Attach(v);
            var australia=new CountryId("country.australia");var anchor=assets.Anchors[australia.Value];
            v.Viewport.Focus(anchor+new Vector2((Area.width/2+2)/(Area.width*12),0),12,Area);
            var offscreen=v.Viewport.Project(anchor,Area);Assert.That(offscreen.x,Is.LessThan(0));var pointer=new Vector2(1,offscreen.y);
            Assert.That(v.VisibleMarkers(Area,pointer).Any(m=>m.Id.Equals(australia)),Is.False);Assert.That(v.HitTest(pointer,Area),Is.Not.EqualTo(australia));
        }
        [TestCase(DiagnosticMapKind.Tiles,false,DiagnosticMapKind.Table)]
        [TestCase(DiagnosticMapKind.Table,false,DiagnosticMapKind.Tiles)]
        [TestCase(DiagnosticMapKind.Tiles,true,DiagnosticMapKind.Table)]
        [TestCase(DiagnosticMapKind.Table,true,DiagnosticMapKind.World)]
        [TestCase(DiagnosticMapKind.World,true,DiagnosticMapKind.Tiles)]
        public void View_cycle_only_offers_installed_renderers(DiagnosticMapKind current,bool available,DiagnosticMapKind expected)
        { Assert.That(DrawableMapView.NextKind(current,available),Is.EqualTo(expected)); }
        [Test] public void Region_buttons_can_refocus_the_same_region_after_reset()
        {
            var c=Controller(out var names);using var p=new MapPresenter(c,names);var v=new WorldMapView(WorldMapDataTests.Assets());p.Attach(v);
            Assert.That(v.RequestRegionFocus("region.middle_east"),Is.True);v.PrepareViewport(Area);var center=v.Viewport.Center;var zoom=v.Viewport.Zoom;Assert.That(zoom,Is.GreaterThan(1));
            v.Viewport.Reset();Assert.That(v.RequestRegionFocus("region.middle_east"),Is.True);v.PrepareViewport(Area);
            Assert.That(v.Viewport.Center,Is.EqualTo(center));Assert.That(v.Viewport.Zoom,Is.EqualTo(zoom));
            v.Viewport.Reset();v.PrepareViewport(Area);Assert.That(v.Viewport.Zoom,Is.EqualTo(1),"Old queued request must not repeat");
            Assert.That(v.RequestRegionFocus("region.unknown"),Is.False);
            Assert.That(v.RequestCountryFocus(MapViewTests.France),Is.True);v.PrepareViewport(Area);Assert.That(v.Viewport.Zoom,Is.EqualTo(8));
            Assert.That(v.RequestCountryFocus(new CountryId("superpower.us")),Is.False);
            v.Dispose();Assert.That(v.RequestRegionFocus("region.middle_east"),Is.False);Assert.That(v.RequestCountryFocus(MapViewTests.France),Is.False);
        }
        [Test] public void World_can_be_selected_by_launch_option_and_is_the_default()
        {
            Assert.That(DemoLaunchOptions.MapKind(new[]{"demo.exe"}),Is.EqualTo(DiagnosticMapKind.World));
            Assert.That(DemoLaunchOptions.MapKind(new[]{"--demo-map=world"}),Is.EqualTo(DiagnosticMapKind.World));
            using var view=DrawableMapView.CreateView(DiagnosticMapKind.World,WorldMapDataTests.Assets());Assert.That(view,Is.TypeOf<WorldMapView>());
        }
        static DemoController Controller(out System.Collections.Generic.IReadOnlyDictionary<string,string> names)
        { var d=EraDemoTests.Catalog();var c=new DemoController(d.CreateHost);var s=d.Scenarios[0];c.LoadScenario(s.Id,s.Actor,s.SelectedCountry);names=d.Names;return c; }
        [Test] public async Task Actual_screen_pick_uses_the_same_controller_and_rule_command()
        {
            var c=Controller(out var names);var assets=WorldMapDataTests.Assets();using var p=new MapPresenter(c,names);var v=new WorldMapView(assets);p.Attach(v);
            v.Viewport.Focus(assets.Anchors["country.france"],8,Area);
            var screen=v.Viewport.Project(assets.Anchors["country.france"],Area);
            Assert.That(v.HitTest(screen,Area),Is.EqualTo(MapViewTests.France));Assert.That(v.SelectAt(screen,Area),Is.True);
            var result=await c.SubmitAsync(3);Assert.That(result.Accepted,Is.True);p.Refresh();
            Assert.That(v.Frame.Countries.Single(x=>x.Id.Equals(MapViewTests.France)).US,Is.EqualTo(3));Assert.That(c.ReadView().RemainingOps,Is.Zero);
        }
        [Test] public void Every_country_can_be_picked_when_focused_even_in_dense_regions()
        {
            var c=Controller(out var names);var assets=WorldMapDataTests.Assets();using var p=new MapPresenter(c,names);var v=new WorldMapView(assets);p.Attach(v);
            foreach(var n in v.Context.Nodes)
            {
                var a=assets.Anchors[n.Id.Value];v.Viewport.Focus(a,12,Area);
                Assert.That(v.HitTest(v.Viewport.Project(a,Area),Area),Is.EqualTo(n.Id),n.Id.Value);
            }
        }
        [Test] public void Ocean_powers_outside_and_retired_views_do_not_select()
        {
            var c=Controller(out var names);var assets=WorldMapDataTests.Assets();using var p=new MapPresenter(c,names);var v=new WorldMapView(assets);p.Attach(v);
            Assert.That(v.HitTest(new Vector2(-1,200),Area),Is.Null);Assert.That(v.HitTest(new Vector2(200,-1),Area),Is.Null);
            v.Viewport.Focus(assets.Anchors["superpower.us"],12,Area);
            Assert.That(v.HitTest(v.Viewport.Project(assets.Anchors["superpower.us"],Area),Area),Is.Null);
            v.Viewport.Focus(new Vector2(.15f,.25f),12,Area);Assert.That(v.HitTest(Area.center,Area),Is.Null);
            p.Attach(new CountryTilesMapView());Assert.That(v.HitTest(Area.center,Area),Is.Null);Assert.That(v.SelectAt(Area.center,Area),Is.False);
        }
        [Test] public async Task World_tile_switch_preserves_event_draft_and_authoritative_state()
        {
            var d=EraDemoTests.Catalog();var s=d.Scenarios.Single(x=>x.Id=="demo.event.suez_crisis");var c=new DemoController(d.CreateHost);c.LoadScenario(s.Id,s.Actor,s.SelectedCountry);
            await c.BeginEventAsync();c.SelectSide(PlayerSide.USSR);var before=c.ReadView();using var p=new MapPresenter(c,d.Names);
            var assets=WorldMapDataTests.Assets();var v=new WorldMapView(assets);p.Attach(v);v.Viewport.Focus(assets.Anchors["country.france"],8,Area);
            v.SelectAt(v.Viewport.Project(assets.Anchors["country.france"],Area),Area);Assert.That(c.ChangeEventAllocation(MapViewTests.France,1),Is.True);
            var oldContext=v.Context.Id;p.Attach(new CountryTableMapView());var next=new WorldMapView(assets);p.Attach(next);
            Assert.That(c.AllocationDraft.Single().Amount,Is.EqualTo(1));Assert.That(next.Context.Id,Is.Not.EqualTo(oldContext));
            Assert.That(c.ReadView().GameId,Is.EqualTo(before.GameId));Assert.That(c.ReadView().Version,Is.EqualTo(before.Version));
        }
        [Test] public void Incompatible_country_context_retires_world_adapter()
        {
            var v=new WorldMapView(WorldMapDataTests.Assets());
            Assert.That(()=>v.Initialize(new MapViewContext("small",MapViewTests.Nodes())),Throws.InstanceOf<ArgumentException>());
            Assert.That(v.IsDisposed,Is.True);Assert.That(v.SelectAt(Area.center,Area),Is.False);
            Assert.Throws<ArgumentNullException>(()=>new WorldMapView(null));
        }
    }
}
