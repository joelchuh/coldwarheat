using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ColdWarStruggle.Core;
using ColdWarStruggle.Presentation;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class DiagnosticMapTests
    {
        [Test] public void Launch_option_is_a_closed_whitelist_with_world_default()
        {
            Assert.That(DemoLaunchOptions.MapKind(new[]{"game.exe"}),Is.EqualTo(DiagnosticMapKind.World));
            Assert.That(DemoLaunchOptions.MapKind(new[]{"--demo-map=tiles"}),Is.EqualTo(DiagnosticMapKind.Tiles));
            Assert.That(DemoLaunchOptions.MapKind(new[]{"--demo-map=table"}),Is.EqualTo(DiagnosticMapKind.Table));
            Assert.Throws<ArgumentException>(()=>DemoLaunchOptions.MapKind(new[]{"--demo-map=table","--demo-map=table"}));
            Assert.Throws<ArgumentException>(()=>DemoLaunchOptions.MapKind(new[]{"--demo-map="}));
            Assert.Throws<ArgumentException>(()=>DemoLaunchOptions.MapKind(new[]{"--demo-map=Some.Class"}));
        }
        [TestCase(DiagnosticMapKind.Tiles)] [TestCase(DiagnosticMapKind.Table)]
        public async Task Both_real_adapters_select_same_country_and_produce_same_rule_result(DiagnosticMapKind kind)
        {
            var data=DemoTests.Catalog();var c=new DemoController(data.CreateHost);var s=data.Scenarios[0];c.LoadScenario(s.Id,s.Actor,s.SelectedCountry);
            using var p=new MapPresenter(c,data.Names);var v=DiagnosticMapView.Create(kind);p.Attach(v);
            Assert.That(v.Kind,Is.EqualTo(kind));Assert.That(v.VisibleCountries("region.europe"),Has.Member(MapViewTests.France));Assert.That(v.VisibleCountries("region.europe"),Has.No.Member(MapViewTests.Angola));
            Assert.That(v.TrySelect(MapViewTests.France),Is.True);var result=await c.SubmitAsync(3);Assert.That(result.Accepted,Is.True);
            p.Refresh();Assert.That(v.Frame.Countries.Single(x=>x.Id.Equals(MapViewTests.France)).US,Is.EqualTo(3));Assert.That(c.ReadView().RemainingOps,Is.Zero);Assert.That(c.ReadView().Version,Is.EqualTo(1));
        }
        [Test] public void Invalid_renderer_kind_and_retired_renderer_are_unusable()
        {
            Assert.Throws<ArgumentOutOfRangeException>(()=>DiagnosticMapView.Create((DiagnosticMapKind)50));
            var data=DemoTests.Catalog();var c=new DemoController(data.CreateHost);var s=data.Scenarios[0];c.LoadScenario(s.Id,s.Actor,s.SelectedCountry);using var p=new MapPresenter(c,data.Names);
            var a=DiagnosticMapView.Create(DiagnosticMapKind.Tiles);p.Attach(a);var b=DiagnosticMapView.Create(DiagnosticMapKind.Table);p.Attach(b);
            Assert.That(a.TrySelect(MapViewTests.Angola),Is.False);Assert.That(a.VisibleCountries("region.europe"),Is.Empty);Assert.That(c.SelectedCountry,Is.EqualTo(MapViewTests.France));
        }
    }
}
