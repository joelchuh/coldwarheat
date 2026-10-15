using System.Linq;
using ColdWarStruggle.Core;
using ColdWarStruggle.Application;
using ColdWarStruggle.Presentation;
using NUnit.Framework;
using UnityEngine;
namespace ColdWarStruggle.Tests
{
    public class HeadlineViewPrivacyTests
    {
        [Test] public void Fresh_view_and_identity_switch_keep_first_headline_private_until_explicit_observation()
        {
            var catalog=HeadlineDemoCatalogTests.Catalog();var scene=catalog.Scenarios.Last();var host=catalog.CreateHost(scene.Id);var controller=new DemoController(_=>host);
            var go=new GameObject("headline-privacy-view");
            try
            {
                var view=go.AddComponent<DemoView>();view.Initialize(controller,catalog.Names,new[]{new DemoScenarioOption(scene.Id,scene.Title,scene.Actor,scene.SelectedCountry)},printedCards:catalog.PrintedCards);
                controller.SelectSide(PlayerSide.US);var first=controller.ReadView().Headline.Selection.Candidates.First();controller.SelectCard(first);Assert.That(controller.SubmitSelectedHeadlineAsync().Result.Accepted,Is.True);
                controller.SelectSide(PlayerSide.USSR);var version=controller.ReadView().Version;var frame=view.ReadHeadlineFrame();
                Assert.That(frame.OpponentLocked,Is.True);Assert.That(frame.IsObserver,Is.False);Assert.That(frame.ObserverLines,Is.Empty);Assert.That(frame.Prompt,Does.Not.Contain(catalog.Names[first.Value]));
                view.TestObserverEnabled=true;frame=view.ReadHeadlineFrame();Assert.That(frame.IsObserver,Is.True);Assert.That(frame.ObserverLines.Single(),Does.Contain(catalog.Names[first.Value]));
                view.TestObserverEnabled=false;Assert.That(view.ReadHeadlineFrame().ObserverLines,Is.Empty);Assert.That(controller.ReadView().Version,Is.EqualTo(version));Assert.That(controller.SelectedSide,Is.EqualTo(PlayerSide.USSR));
            }
            finally {Object.DestroyImmediate(go);}
        }
    }
}
