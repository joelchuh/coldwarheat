using System;
using ColdWarStruggle.Presentation;
using NUnit.Framework;

namespace ColdWarStruggle.Tests
{
    public class DemoLaunchTests
    {
        static readonly string[] Ids={"demo.influence","demo.event"};
        [Test] public void Default_launch_keeps_first_lesson_and_capture_can_select_an_existing_lesson()
        {
            Assert.That(DemoLaunchOptions.ScenarioIndex(new[]{"game.exe","--capture-demo"},Ids),Is.Zero);
            Assert.That(DemoLaunchOptions.ScenarioIndex(new[]{"game.exe","--capture-demo","--demo-scenario=demo.event"},Ids),Is.EqualTo(1));
        }
        [TestCase("--demo-scenario=")] [TestCase("--demo-scenario=unknown")]
        public void Unknown_lesson_is_rejected_instead_of_silently_capturing_the_wrong_screen(string argument)
            =>Assert.Throws<ArgumentException>(()=>DemoLaunchOptions.ScenarioIndex(new[]{argument},Ids));
        [Test] public void Duplicate_selection_is_ambiguous_and_rejected()
            =>Assert.Throws<ArgumentException>(()=>DemoLaunchOptions.ScenarioIndex(new[]{"--demo-scenario=demo.event","--demo-scenario=demo.influence"},Ids));
        [Test] public void Capture_success_requires_an_initialized_view_without_loading_errors()
        {
            var go=new UnityEngine.GameObject("capture-test");
            try
            {
                var view=go.AddComponent<DemoView>();Assert.That(view.IsReady,Is.False);
                var catalog=DemoTests.Catalog();var s=catalog.Scenarios[0];
                view.Initialize(new DemoController(catalog.CreateHost),catalog.Names,new[]{new DemoScenarioOption(s.Id,s.Title,s.Actor,s.SelectedCountry)});
                Assert.That(view.IsReady,Is.True);
                view.ShowError("Invalid selected lesson");Assert.That(view.IsReady,Is.False);
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }
    }
}
