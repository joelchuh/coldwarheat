using System;
using System.Linq;
using System.Threading.Tasks;
using ColdWarStruggle.Core;
using ColdWarStruggle.Presentation;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class DeploymentMapTests
    {
        sealed class Probe : IMapView
        {
            public Action<MapIntent> Captured;
            public MapFrame Frame;
            public event Action<MapIntent> Intent { add {Captured=value;} remove {} }
            public void Initialize(MapViewContext context) {Frame=null;}
            public bool Present(MapFrame frame) {Frame=frame;return true;}
            public void FocusOn(CountryId country) {}
            public void Clear() {Frame=null;}
            public void Dispose() {}
            public MapIntent Click(string country)=>new MapIntent(Frame.ContextId,Frame.Version,Frame.Sequence,Frame.InteractionKey,DeploymentDemoTests.Id(country));
        }
        [Test] public void Map_queries_deployment_targets_and_click_only_selects_country()
        {
            var c=DeploymentDemoTests.Controller();using var p=new MapPresenter(c,DeploymentDemoTests.Catalog().Names);var view=new Probe();p.Attach(view);
            Assert.That(view.Frame.InteractionKey,Does.StartWith("deployment"));
            Assert.That(view.Frame.Countries.Where(x=>x.Allowed).Select(x=>x.Id),Is.EquivalentTo(c.DeploymentTargets().Targets));
            Assert.That(view.Frame.Countries.Any(x=>x.Allowed),Is.True);view.Captured(view.Click("finland"));
            Assert.That(c.SelectedCountry,Is.EqualTo(DeploymentDemoTests.Id("finland")));Assert.That(c.DeploymentDraft,Is.Empty);Assert.That(c.ReadView().Version,Is.EqualTo(1));
        }
        [Test] public void Map_replacement_keeps_draft_and_old_input_is_ignored_after_local_edit()
        {
            var c=DeploymentDemoTests.Controller();using var p=new MapPresenter(c,DeploymentDemoTests.Catalog().Names);var first=new Probe();p.Attach(first);
            var stale=first.Click("finland");DeploymentDemoTests.Add(c,"poland",2);first.Captured(stale);Assert.That(c.SelectedCountry,Is.EqualTo(DeploymentDemoTests.Id("poland")));
            var second=new Probe();p.Attach(second);Assert.That(c.DeploymentDraft.Single().Amount,Is.EqualTo(2));
            Assert.That(second.Frame.Countries.Single(x=>x.Id.Equals(DeploymentDemoTests.Id("poland"))).Drafted,Is.True);
            first.Captured(stale);Assert.That(c.SelectedCountry,Is.EqualTo(DeploymentDemoTests.Id("poland")));
            var pending=second.Click("finland");c.ClearDeploymentDraft();second.Captured(pending);Assert.That(c.SelectedCountry,Is.EqualTo(DeploymentDemoTests.Id("poland")));
        }
        [Test] public async Task Stage_and_identity_changes_retire_inputs_and_completed_map_has_no_targets()
        {
            var c=DeploymentDemoTests.Controller();using var p=new MapPresenter(c,DeploymentDemoTests.Catalog().Names);var view=new Probe();p.Attach(view);
            DeploymentDemoTests.Add(c,"poland",6);p.Refresh();var stale=view.Click("finland");await c.ConfirmDeploymentAsync();view.Captured(stale);
            Assert.That(c.SelectedCountry,Is.EqualTo(DeploymentDemoTests.Id("poland")));c.SelectSide(PlayerSide.US);p.Refresh();
            Assert.That(view.Frame.Countries.Single(x=>x.Id.Equals(DeploymentDemoTests.Id("canada"))).Allowed,Is.True);
            var prior=view.Click("canada");c.SelectSide(PlayerSide.USSR);view.Captured(prior);Assert.That(c.SelectedCountry,Is.EqualTo(DeploymentDemoTests.Id("poland")));
            p.Refresh();Assert.That(view.Frame.Countries.Any(x=>x.Allowed),Is.False);c.SelectSide(PlayerSide.US);DeploymentDemoTests.Add(c,"canada",7);await c.ConfirmDeploymentAsync();p.Refresh();
            Assert.That(view.Frame.Countries.Any(x=>x.Allowed || x.Drafted),Is.False);
        }
    }
}
