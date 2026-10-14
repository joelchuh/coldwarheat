using System;
using System.IO;
using System.Linq;
using ColdWarStruggle.Core;
using ColdWarStruggle.Infrastructure;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class PlayCoupCatalogTests
    {
        static string Read(string id)=>File.ReadAllText(Path.Combine(DefinitionTests.Root,"Data/Rulesets/deluxe-2015/"+id+".json"));
        internal static DemoCatalog Risk(DemoCatalog c)=>c.WithPracticeRisk(Read("coup"),Read("defcon"),Read("military-operations"),Read("victory"));
        [Test] public void Risk_is_bound_to_actual_map_only_on_the_one_real_deal_scenario()
        {
            var c=Risk(PlayableDemoTests.Catalog());Assert.That(c.Scenarios.Count,Is.EqualTo(13));
            foreach(var s in c.Scenarios){var v=c.CreateHost(s.Id).UsSession.ReadView();Assert.That(v.Practice?.Defcon.HasValue==true,Is.EqualTo(s.InitialCards!=null));}
            var d=c.Scenarios.Single(s=>s.InitialCards!=null).PracticeDefinition;Assert.That(d.Risk.Defcon.Map,Is.SameAs(c.Map));
            var view=c.CreateHost(DealtDemoCatalogTests.ScenarioId).UsSession.ReadView();Assert.That(view.Practice.Defcon,Is.EqualTo(5));Assert.That(view.Practice.UsMilitary,Is.Zero);Assert.That(view.Practice.Winner,Is.Null);
        }
        [Test] public void Missing_action_setup_and_repeated_risk_configuration_are_rejected()
        {Assert.Throws<ArgumentException>(()=>Risk(DealtDemoCatalogTests.Catalog()));Assert.Throws<ArgumentException>(()=>Risk(Risk(PlayableDemoTests.Catalog())));}
        [Test] public void Bad_shared_data_is_refused_before_creating_a_host()
        {Assert.Catch(()=>PlayableDemoTests.Catalog().WithPracticeRisk("{}",Read("defcon"),Read("military-operations"),Read("victory")));}
        [TestCase(true)] [TestCase(false)] public void Version_or_sequence_overflow_has_no_partial_coup(bool version)
        {
            var s=PlayCoupTests.Ready();var bad=new GameState(s.Map,s.Countries,s.Operations,s.GameId,version?long.MaxValue:s.Version,s.SpentOps,version?s.EventSequence:long.MaxValue,turn:s.Turn,cards:s.Cards,randomState:s.RandomState,practice:s.Practice);
            var p=PlayCoupTests.Coup(bad,"iran");Assert.That(p.Error,Is.EqualTo(RuleError.NumericOverflow));Assert.That(p.NextState,Is.Null);Assert.That(bad.Practice.Risk.CoupReceipts,Is.Empty);Pcg32RandomTests.Same(bad.RandomState,s.RandomState);
        }
    }
}
