using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ColdWarStruggle.Core;
using ColdWarStruggle.Application;
using ColdWarStruggle.Infrastructure;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class FinalScoringDemoEntryTests
    {
        internal const string Id="demo.practice.final_scoring";
        internal static string Read(string p)=>File.ReadAllText(Path.Combine(DefinitionTests.Root,p));
        internal static string Data()=>Read("Data/Demo/final-scoring-scenario.json");
        internal static string RegionData()=>Read("Data/Rulesets/deluxe-2015/region-scoring.json");
        internal static string RegionSchema()=>Read("Docs/Contracts/region-scoring-rules.schema.json");
        internal static DemoCatalog Add(DemoCatalog before,string fixture=null,string region=null)=>before.WithFinalScoringScenario(fixture??Data(),region??RegionData(),RegionSchema(),FinalScoringLoaderTests.ChinaJson,FinalScoringLoaderTests.ChinaSchema);
        internal static DemoCatalog Catalog()=>Add(HeadlineDemoCatalogTests.Catalog());
        [Test] public void Adds_one_fixture_and_preserves_legacy_scenes_and_headline_capabilities()
        {
            var before=HeadlineDemoCatalogTests.Catalog();var after=Add(before);Assert.That(after.Scenarios.Count,Is.EqualTo(16));
            foreach(var old in before.Scenarios.Where(x=>x.Id!=HeadlineDemoCatalogTests.Id))Assert.That(after.Scenarios.Single(x=>x.Id==old.Id),Is.SameAs(old));
            var source=before.Scenarios.Single(x=>x.Id==HeadlineDemoCatalogTests.Id);var normal=after.Scenarios.Single(x=>x.Id==HeadlineDemoCatalogTests.Id);
            var d=normal.PracticeDefinition;var previous=source.PracticeDefinition;
            Assert.That(d.FinalScoring,Is.Not.Null);Assert.That(d.FinalScoring.RegionDefinition,Is.SameAs(previous.Scoring));Assert.That(d.Scoring,Is.SameAs(previous.Scoring));
            Assert.That(d.HeadlineParameters,Is.SameAs(previous.HeadlineParameters));Assert.That(d.HeadlineDefectors,Is.SameAs(previous.HeadlineDefectors));
            Assert.That(d.InfluenceEvents,Is.SameAs(previous.InfluenceEvents));Assert.That(d.DefconVpEvents,Is.SameAs(previous.DefconVpEvents));Assert.That(d.TurnOpsEvents,Is.SameAs(previous.TurnOpsEvents));Assert.That(d.China,Is.SameAs(previous.China));
            Assert.That(normal.InitialCards,Is.SameAs(source.InitialCards));Assert.That(normal.Countries,Is.SameAs(source.Countries));
            Assert.That(after.CreateHost(normal.Id).UsSession.ReadView().Practice.Stage,Is.EqualTo(ActionPracticeStage.CollectingHeadline));
            var fixture=after.Scenarios.Single(x=>x.Id==Id);Assert.That(fixture.PracticeDefinition.FinalScoring,Is.SameAs(d.FinalScoring));
            Assert.That(source.PracticeDefinition.FinalScoring,Is.Null);
        }
        [Test] public async Task Fixture_runs_fourteen_real_actions_then_requires_audit_before_final()
        {
            var catalog=Catalog();var h=catalog.CreateHost(Id);var s=h.UsSession;var before=s.ReadView();
            Assert.That(before.Turn.Number,Is.EqualTo(10));Assert.That(before.Practice.CompletedActions,Is.EqualTo(14));Assert.That(before.Practice.Stage,Is.EqualTo(ActionPracticeStage.Complete));
            Assert.That(before.Version,Is.EqualTo(29));Assert.That(before.EventSequence,Is.EqualTo(29));Assert.That(before.OwnHand,Is.Empty);Assert.That(h.UssrSession.ReadView().OwnHand,Is.Empty);
            Assert.That(before.Practice.TurnEnd,Is.Null);Assert.That(before.Practice.TurnEndAvailable,Is.True);Assert.That(before.Practice.FinalScoringAvailable,Is.False);Assert.That(before.Cards.China.Face,Is.EqualTo(CardFace.FaceDown));
            var audit=await s.SubmitCardActionAsync(new CardActionCommand("audit",before.GameId,before.Version,CardActionKind.AssessTurnEnd));Assert.That(audit.Accepted,Is.True);
            Assert.That(audit.View.Practice.FinalScoringAvailable,Is.True);Assert.That(audit.View.Cards.China.Face,Is.EqualTo(CardFace.FaceUp));Assert.That(audit.View.Practice.NextTurnAvailable,Is.False);
            var r=await s.SubmitCardActionAsync(new CardActionCommand("final",before.GameId,audit.View.Version,CardActionKind.FinalScoring));Assert.That(r.Accepted,Is.True);Assert.That(r.View.Practice.FinalScoring.Regions.Count,Is.EqualTo(6));Assert.That(r.View.Practice.EndReason,Is.EqualTo(VictoryCheckpoint.FinalScoringCompleted));
        }
        [TestCase("dataset")][TestCase("scope")][TestCase("version")][TestCase("turn")][TestCase("terminal-vp")][TestCase("source")][TestCase("duplicate-id")][TestCase("extra")]
        [TestCase("unknown-country")][TestCase("duplicate-country")][TestCase("negative-influence")][TestCase("china-owner")][TestCase("china-face")]
        [TestCase("short-hand")][TestCase("scoring-hand")][TestCase("opponent-hand")][TestCase("duplicate-card")][TestCase("unknown-card")]
        public void Invalid_fixture_is_rejected_without_replacing_source(string kind)
        {
            var data=JObject.Parse(Data());var row=(JObject)data["scenario"];
            switch(kind){case "dataset":data["dataset_id"]="other";break;case "scope":data["scope"]="official";break;case "version":data["schema_version"]=2;break;
            case "turn":row["turn_number"]=9;break;case "terminal-vp":row["initial_vp"]=20;break;case "source":row["source_scenario_id"]=QuickStartDemoTests.Id;break;case "duplicate-id":row["id"]=HeadlineDemoCatalogTests.Id;break;case "extra":row["forced_score"]=5;break;
            case "unknown-country":row["influence"][0]["country_id"]="country.unknown";break;case "duplicate-country":((JArray)row["influence"]).Add(row["influence"][0].DeepClone());break;case "negative-influence":row["influence"][0]["us"]=-1;break;
            case "china-owner":row["china_owner"]="unknown";break;case "china-face":row["china_face"]="unknown";break;
            case "short-hand":((JArray)row["hands"]["US"]).RemoveAt(0);break;case "scoring-hand":row["hands"]["US"][0]="card.europe_scoring";break;
            case "opponent-hand":row["hands"]["US"][0]="card.fidel";break;case "duplicate-card":row["hands"]["US"][0]=row["hands"]["US"][1].DeepClone();break;case "unknown-card":row["hands"]["US"][0]="card.unknown";break;}
            var before=HeadlineDemoCatalogTests.Catalog();Assert.Throws<ArgumentException>(()=>Add(before,data.ToString()));Assert.That(before.Scenarios.Count,Is.EqualTo(15));
        }
        [Test] public void Repeated_decoration_is_rejected(){Assert.Throws<ArgumentException>(()=>Add(Catalog()));}
        [Test] public void Bound_loader_reuses_equivalent_shared_definition_and_checks_differences()
        {
            var c=HeadlineDemoCatalogTests.Catalog();var d=c.Scenarios.Single(x=>x.Id==HeadlineDemoCatalogTests.Id).PracticeDefinition;
            var loaded=FinalScoringDefinitionLoader.LoadBound(RegionData(),RegionSchema(),FinalScoringLoaderTests.ChinaJson,FinalScoringLoaderTests.ChinaSchema,d.Scoring,d.Cards);
            Assert.That(loaded.RegionDefinition,Is.SameAs(d.Scoring));Assert.That(loaded.ChinaHolderVp,Is.EqualTo(1));
            var other=RegionScoringTests.Definition(c.Map,bg:7);Assert.Throws<ArgumentException>(()=>FinalScoringDefinitionLoader.LoadBound(RegionData(),RegionSchema(),FinalScoringLoaderTests.ChinaJson,FinalScoringLoaderTests.ChinaSchema,other,d.Cards));
        }
    }
}
