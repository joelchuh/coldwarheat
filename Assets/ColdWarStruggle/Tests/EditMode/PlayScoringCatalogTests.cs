using System;
using System.IO;
using System.Linq;
using ColdWarStruggle.Core;
using ColdWarStruggle.Infrastructure;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class PlayScoringCatalogTests
    {
        static string Read(string p)=>File.ReadAllText(Path.Combine(DefinitionTests.Root,p));
        internal static string Data()=>Read("Data/Rulesets/deluxe-2015/region-scoring.json");
        internal static string Schema()=>Read("Docs/Contracts/region-scoring-rules.schema.json");
        internal static DemoCatalog Catalog()=>PlayCoupCatalogTests.Risk(PlayableDemoTests.Catalog()).WithPracticeScoring(Data(),Schema());
        [Test] public void Scoring_binds_the_same_map_and_keeps_all_previous_practice_definitions()
        {
            var before=PlayCoupCatalogTests.Risk(PlayableDemoTests.Catalog());var prior=before.Scenarios.Single(s=>s.PracticeDefinition!=null).PracticeDefinition;
            var after=before.WithPracticeScoring(Data(),Schema());Assert.That(after.Scenarios.Count,Is.EqualTo(13));var p=after.Scenarios.Single(s=>s.PracticeDefinition!=null).PracticeDefinition;
            Assert.That(p.Risk,Is.SameAs(prior.Risk));Assert.That(p.Space,Is.SameAs(prior.Space));Assert.That(p.Ops,Is.SameAs(prior.Ops));Assert.That(p.Cards,Is.SameAs(prior.Cards));Assert.That(p.Calendar,Is.SameAs(prior.Calendar));Assert.That(p.Scoring.Map,Is.SameAs(after.Map));
            var v=after.CreateHost(DealtDemoCatalogTests.ScenarioId).UsSession.ReadView();Assert.That(v.Practice.HasScoring,Is.True);Assert.That(v.Deployment,Is.Not.Null);Assert.That(v.Practice.Defcon,Is.EqualTo(5));
        }
        [Test] public void Risk_setup_is_required_and_repeat_configuration_is_rejected()
        {Assert.Throws<ArgumentException>(()=>PlayableDemoTests.Catalog().WithPracticeScoring(Data(),Schema()));Assert.Throws<ArgumentException>(()=>Catalog().WithPracticeScoring(Data(),Schema()));}
        [Test] public void Invalid_shared_source_is_rejected_before_new_catalog_is_published()
        {Assert.Throws<ArgumentException>(()=>PlayCoupCatalogTests.Risk(PlayableDemoTests.Catalog()).WithPracticeScoring(Data().Replace("https://www.gmtgames.com/nnts/TS_Rules-2015.pdf","https://invalid.example/rules.pdf"),Schema()));}
        [Test] public void Definition_refuses_missing_risk_or_different_map()
        {
            var s=PlayScoringTests.Ready();var d=s.Practice.Definition;
            Assert.Throws<ArgumentException>(()=>new ActionPracticeDefinition(d.Cards,d.Ops,d.Space,d.Calendar,null,d.Scoring));
            Assert.Throws<ArgumentException>(()=>new ActionPracticeDefinition(d.Cards,d.Ops,d.Space,d.Calendar,d.Risk,RegionScoringTests.Definition(DefinitionTests.World())));
        }
    }
}
