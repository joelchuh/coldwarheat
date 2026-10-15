using System;
using System.IO;
using System.Linq;
using ColdWarStruggle.Core;
using ColdWarStruggle.Infrastructure;
using ColdWarStruggle.Application;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class HeadlineDemoCatalogTests
    {
        internal const string Id="demo.practice.headlines";
        internal static string Data()=>File.ReadAllText(Path.Combine(DefinitionTests.Root,"Data/Demo/headline-scenario.json"));
        internal static DemoCatalog Add(DemoCatalog d,string fixture=null)=>d.WithHeadlineScenario(fixture??Data(),TurnCalendarTests.Source(),DefectorsHeadlineDefinitionTests.Data(),DefectorsHeadlineDefinitionTests.Schema());
        internal static DemoCatalog Catalog()=>Add(NextTurnSessionTests.Catalog());
        [Test] public void New_scene_appends_without_changing_legacy_fourteen_or_capabilities()
        {
            var before=NextTurnSessionTests.Catalog();var after=Add(before);Assert.That(before.Scenarios,Has.Count.EqualTo(14));Assert.That(after.Scenarios,Has.Count.EqualTo(15));
            foreach(var old in before.Scenarios)Assert.That(after.Scenarios.Single(x=>x.Id==old.Id),Is.SameAs(old));
            var source=before.Scenarios.Single(x=>x.Id==QuickStartDemoTests.Id);var scene=after.Scenarios.Single(x=>x.Id==Id);var d=scene.PracticeDefinition;var oldD=source.PracticeDefinition;
            Assert.That(scene.InitialCards,Is.SameAs(source.InitialCards));Assert.That(scene.DeploymentDefinition,Is.SameAs(source.DeploymentDefinition));Assert.That(scene.Countries,Is.SameAs(source.Countries));
            Assert.That(d.Cards,Is.SameAs(oldD.Cards));Assert.That(d.Ops,Is.SameAs(oldD.Ops));Assert.That(d.Space,Is.SameAs(oldD.Space));Assert.That(d.Calendar,Is.SameAs(oldD.Calendar));Assert.That(d.Risk,Is.SameAs(oldD.Risk));Assert.That(d.Scoring,Is.SameAs(oldD.Scoring));
            Assert.That(d.InfluenceEvents,Is.SameAs(oldD.InfluenceEvents));Assert.That(d.DefconVpEvents,Is.SameAs(oldD.DefconVpEvents));Assert.That(d.TurnOpsEvents,Is.SameAs(oldD.TurnOpsEvents));Assert.That(d.Realignment,Is.SameAs(oldD.Realignment));Assert.That(d.China,Is.SameAs(oldD.China));
            Assert.That(d.TurnEndEnabled&&d.NextTurnEnabled,Is.True);Assert.That(d.HeadlineParameters,Is.Not.Null);Assert.That(d.HeadlineDefectors.Catalog,Is.SameAs(d.Cards));Assert.That(oldD.HeadlineParameters,Is.Null);
        }
        [Test] public void Real_hands_and_deployment_match_source_but_only_new_host_collects()
        {
            var d=Catalog();var old=d.CreateHost(QuickStartDemoTests.Id);var h=d.CreateHost(Id);var v=h.UsSession.ReadView();var baseline=old.UsSession.ReadView();
            Assert.That(v.Practice.HeadlineEnabled,Is.True);Assert.That(baseline.Practice.HeadlineEnabled,Is.False);Assert.That(v.Practice.Stage,Is.EqualTo(ActionPracticeStage.CollectingHeadline));Assert.That(baseline.Practice.Stage,Is.EqualTo(ActionPracticeStage.ChooseCard));
            Assert.That(v.Deployment,Is.Null);Assert.That(v.Practice.CompletedActions,Is.Zero);Assert.That(v.OwnHand,Is.EqualTo(baseline.OwnHand));Assert.That(h.UssrSession.ReadView().OwnHand,Is.EqualTo(old.UssrSession.ReadView().OwnHand));
            Assert.That(v.Countries.Select(x=>(x.Definition.Id,x.US,x.USSR)),Is.EqualTo(baseline.Countries.Select(x=>(x.Definition.Id,x.US,x.USSR))));Assert.That(v.Practice.VpForUS,Is.EqualTo(baseline.Practice.VpForUS));Assert.That(v.Practice.Defcon,Is.EqualTo(baseline.Practice.Defcon));
            Assert.That(v.Headline.Selection.Candidates,Is.Not.Empty);Assert.That(h.UssrSession.ReadView().Headline.Selection.Candidates,Is.Not.Empty);
        }
        [Test] public void Actual_scene_can_lock_reveal_resolve_choices_and_reach_first_action()
        {
            var h=Catalog().CreateHost(Id);var us=h.UsSession.ReadView();var su=h.UssrSession.ReadView();var a=us.Headline.Selection.Candidates.First();var b=su.Headline.Selection.Candidates.First();
            var r=h.UsSession.SubmitHeadlineAsync(new HeadlineCommand("us",us.GameId,us.Version,a)).Result;Assert.That(r.Accepted,Is.True);Assert.That(h.UssrSession.ReadView().Headline.Selection.OpponentCard,Is.Null);
            su=h.UssrSession.ReadView();var second=h.UssrSession.SubmitHeadlineAsync(new HeadlineCommand("su",su.GameId,su.Version,b)).Result;Assert.That(second.Accepted,Is.True,second.Error.ToString());
            for(int i=0;i<2 && h.UsSession.ReadView().Event!=null;i++)
            {var owner=h.UsSession.ReadView().Event.Owner==PlayerSide.US?h.UsSession:h.UssrSession;Assert.That(owner.SubmitEventAsync(HeadlineSessionContinuationTests.Choice(owner,"choice-"+i)).Result.Accepted,Is.True);}
            var done=h.UsSession.ReadView();Assert.That(done.Headline.Status,Is.EqualTo(PublicHeadlineStatus.Completed));Assert.That(done.Practice.Actor,Is.EqualTo(PlayerSide.USSR));Assert.That(done.Practice.CompletedActions,Is.Zero);Assert.That(done.Practice.HeadlineEnabled,Is.True);
        }
        [TestCase("version")][TestCase("dataset")][TestCase("scope")][TestCase("unknown-source")][TestCase("manual-source")][TestCase("duplicate-id")][TestCase("empty-id")][TestCase("empty-title")][TestCase("extra-root")][TestCase("extra-scenario")]
        public void Bad_fixture_is_rejected_without_changing_source(string kind)
        {
            var j=JObject.Parse(Data());var s=(JObject)j["scenario"];
            switch(kind){case "version":j["schema_version"]=2;break;case "dataset":j["dataset_id"]="other";break;case "scope":j["scope"]="rule";break;
            case "unknown-source":s["source_scenario_id"]="missing";break;case "manual-source":s["source_scenario_id"]="demo.setup.initial_deal";break;
            case "duplicate-id":s["id"]=QuickStartDemoTests.Id;break;case "empty-id":s["id"]="";break;case "empty-title":s["title"]=" ";break;case "extra-root":j["ops"]=5;break;case "extra-scenario":s["deck"]="forced";break;}
            var d=NextTurnSessionTests.Catalog();Assert.Throws<ArgumentException>(()=>Add(d,j.ToString()));Assert.That(d.Scenarios,Has.Count.EqualTo(14));
        }
        [Test] public void Repeat_configuration_is_rejected(){var d=Catalog();Assert.Throws<ArgumentException>(()=>Add(d));}
        [Test] public void Missing_next_turn_support_is_rejected(){Assert.Throws<ArgumentException>(()=>Add(QuickStartDemoTests.Catalog()));}
        [Test] public void Broken_shared_parameter_profile_is_not_ignored()
        {Assert.Throws<ArgumentException>(()=>NextTurnSessionTests.Catalog().WithHeadlineScenario(Data(),TurnCalendarTests.Source(),DefectorsHeadlineDefinitionTests.Data().Replace("USSelectedDefectorsBeforeOrdinaryPrintedOpsOrder","UnverifiedOrder"),DefectorsHeadlineDefinitionTests.Schema()));}
        [Test] public void Public_flag_is_true_before_collection_and_false_for_legacy()
        {
            Assert.That(new LocalGameHost(DefectorsHeadlineResolutionTests.Prepared(collecting:true),true).UsSession.ReadView().Practice.HeadlineEnabled,Is.True);
            Assert.That(new LocalGameHost(DefectorsHeadlineResolutionTests.Prepared(collecting:false),true).UsSession.ReadView().Practice.HeadlineEnabled,Is.False);
        }
    }
}
