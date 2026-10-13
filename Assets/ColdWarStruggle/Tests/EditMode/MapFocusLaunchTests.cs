using System;
using ColdWarStruggle.Presentation;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class MapFocusLaunchTests
    {
        [TestCase("selected",true)] [TestCase("overview",false)]
        public void World_focus_choices_are_display_only_whitelist(string choice,bool expected)
        { Assert.That(DemoLaunchOptions.FocusSelected(new[]{"--demo-focus="+choice},DiagnosticMapKind.World),Is.EqualTo(expected)); }
        [TestCase(DiagnosticMapKind.World)] [TestCase(DiagnosticMapKind.Tiles)] [TestCase(DiagnosticMapKind.Table)]
        public void No_explicit_focus_preserves_existing_default(DiagnosticMapKind kind)
        {Assert.That(DemoLaunchOptions.FocusSelected(new[]{"game.exe"},kind),Is.False);}
        [TestCase("")] [TestCase("france")] [TestCase("../external.json")] [TestCase("Selected")]
        public void Unknown_focus_cannot_become_a_resource_or_country_override(string value)
        {Assert.That(()=>DemoLaunchOptions.FocusSelected(new[]{"--demo-focus="+value},DiagnosticMapKind.World),Throws.InstanceOf<ArgumentException>());}
        [TestCase(DiagnosticMapKind.Tiles)] [TestCase(DiagnosticMapKind.Table)] [TestCase((DiagnosticMapKind)999)]
        public void Focus_parameter_requires_world_renderer(DiagnosticMapKind kind)
        {Assert.That(()=>DemoLaunchOptions.FocusSelected(new[]{"--demo-focus=selected"},kind),Throws.InstanceOf<ArgumentException>());}
        [Test] public void Duplicate_and_null_arguments_are_rejected()
        {
            Assert.That(()=>DemoLaunchOptions.FocusSelected(new[]{"--demo-focus=selected","--demo-focus=overview"},DiagnosticMapKind.World),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>DemoLaunchOptions.FocusSelected(null,DiagnosticMapKind.World),Throws.InstanceOf<ArgumentException>());
        }
    }
}
