using System;
using System.Linq;
using System.Threading.Tasks;
using ColdWarStruggle.Core;
using ColdWarStruggle.Application;
using ColdWarStruggle.Infrastructure;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class SpaceDiscardDemoEntryTests
    {
        internal const string NextId="demo.practice.space_discard.next";
        internal const string FinalId="demo.practice.space_discard.final";
        internal static string Data()=>FinalScoringDemoEntryTests.Read("Data/Demo/space-end-turn-discard-scenarios.json");
        internal static DemoCatalog Catalog()=>FinalScoringDemoEntryTests.Catalog().WithSpaceDiscardScenarios(Data());
        [Test] public void Two_explicit_fixtures_preserve_all_sixteen_existing_objects_and_old_boundary()
        {
            var before=FinalScoringDemoEntryTests.Catalog();var after=before.WithSpaceDiscardScenarios(Data());Assert.That(after.Scenarios.Count,Is.EqualTo(18));
            foreach(var s in before.Scenarios)Assert.That(after.Scenarios.Single(n=>n.Id==s.Id),Is.SameAs(s));
            var old=after.CreateHost(FinalScoringDemoEntryTests.Id).UsSession.ReadView();Assert.That(old.Version,Is.EqualTo(29));Assert.That(old.OwnHand,Is.Empty);Assert.That(old.Practice.VpForUS,Is.EqualTo(2));Assert.That(old.Practice.UsSpace,Is.Zero);Assert.That(old.Practice.UssrSpace,Is.Zero);
            Assert.That(after.CreateHost(HeadlineDemoCatalogTests.Id).UsSession.ReadView().Practice.Stage,Is.EqualTo(ActionPracticeStage.CollectingHeadline));
        }
        [TestCase(NextId,PlayerSide.US,9,true)] [TestCase(FinalId,PlayerSide.USSR,10,false)]
        public async Task Shared_fixture_retains_held_cards_and_runs_real_audit_choice_and_continuation(string id,PlayerSide owner,int turn,bool discard)
        {
            var catalog=Catalog();var scene=catalog.Scenarios.Single(s=>s.Id==id);Assert.That(scene.Title,Does.Contain("人工局面"));
            var source=catalog.Scenarios.Single(s=>s.Id==FinalScoringDemoEntryTests.Id);Assert.That(scene.Countries,Is.SameAs(source.Countries));Assert.That(scene.PracticeDefinition,Is.SameAs(source.PracticeDefinition));
            var h=catalog.CreateHost(id);var session=owner==PlayerSide.US?h.UsSession:h.UssrSession;var other=owner==PlayerSide.US?h.UssrSession:h.UsSession;var v=session.ReadView();
            Assert.That(v.Turn.Number,Is.EqualTo(turn));Assert.That(v.Version,Is.EqualTo(29));Assert.That(v.EventSequence,Is.EqualTo(29));Assert.That(v.Practice.CompletedActions,Is.EqualTo(14));Assert.That(v.Practice.VpForUS,Is.EqualTo(2));
            Assert.That(v.Practice.UsSpace,Is.EqualTo(owner==PlayerSide.US?6:4));Assert.That(v.Practice.UssrSpace,Is.EqualTo(owner==PlayerSide.USSR?6:4));Assert.That(v.Practice.UsAttempts,Is.Zero);Assert.That(v.Practice.UssrAttempts,Is.Zero);
            Assert.That(v.OwnHand,Has.Count.EqualTo(2));Assert.That(other.ReadView().OwnHand,Has.Count.EqualTo(1));Assert.That(v.SpaceDiscard,Is.Null);Assert.That(v.Practice.TurnEndAvailable,Is.True);
            var audit=await session.SubmitCardActionAsync(new CardActionCommand("audit",v.GameId,v.Version,CardActionKind.AssessTurnEnd));Assert.That(audit.Accepted,Is.True);
            Assert.That(audit.View.SpaceDiscard.Status,Is.EqualTo(SpaceEndTurnDiscardStatus.Pending));Assert.That(audit.View.SpaceDiscard.Candidates.Select(c=>c.Value),Is.EquivalentTo(v.OwnHand));Assert.That(other.ReadView().SpaceDiscard.Candidates,Is.Empty);
            var card=audit.View.SpaceDiscard.Candidates[0];var choice=await session.SubmitCardActionAsync(new CardActionCommand("choice",v.GameId,audit.View.Version,discard?CardActionKind.DiscardHeldCard:CardActionKind.KeepHeldCards,discard?card:(CardId?)null));Assert.That(choice.Accepted,Is.True,choice.Error.ToString());
            var end=await session.SubmitCardActionAsync(new CardActionCommand("end",v.GameId,choice.View.Version,turn==9?CardActionKind.BeginNextTurn:CardActionKind.FinalScoring));Assert.That(end.Accepted,Is.True,end.Error.ToString());
            if(turn==9){Assert.That(end.View.Turn.Number,Is.EqualTo(10));Assert.That(end.View.Practice.Stage,Is.EqualTo(ActionPracticeStage.ChooseCard));Assert.That(end.View.SpaceDiscard,Is.Null);Assert.That(end.View.OwnHand,Has.Count.EqualTo(9));}
            else {Assert.That(end.View.Practice.EndReason,Is.EqualTo(VictoryCheckpoint.FinalScoringCompleted));Assert.That(end.View.Practice.FinalScoring.AfterVp,Is.EqualTo(1));Assert.That(end.View.SpaceDiscard.Status,Is.EqualTo(SpaceEndTurnDiscardStatus.Declined));Assert.That(end.View.OwnHand,Is.EqualTo(v.OwnHand));}
        }
        [TestCase("dataset")] [TestCase("scope")] [TestCase("version")] [TestCase("extra")] [TestCase("count")] [TestCase("source")] [TestCase("duplicate-id")] [TestCase("duplicate-row")] [TestCase("early-turn")] [TestCase("unknown-turn")]
        [TestCase("track-low")] [TestCase("track-high")] [TestCase("track-extra")] [TestCase("no-ability")] [TestCase("extra-action")] [TestCase("unknown-card")] [TestCase("scoring")] [TestCase("china")] [TestCase("action-card")] [TestCase("duplicate-card")] [TestCase("cross-hand")] [TestCase("no-owner-cards")] [TestCase("too-many-held")] [TestCase("unknown-side")]
        public void Malformed_or_out_of_scope_fixture_cannot_replace_existing_catalog(string kind)
        {
            var data=JObject.Parse(Data());var rows=(JArray)data["scenarios"];var row=(JObject)rows[0];var us=(JArray)row["held_cards"]["US"];
            switch(kind){case "dataset":data["dataset_id"]="wrong";break;case "scope":data["scope"]="official";break;case "version":data["schema_version"]=2;break;case "extra":row["force_choice"]="discard";break;
            case "count":rows.RemoveAt(0);break;case "source":row["source_scenario_id"]=HeadlineDemoCatalogTests.Id;break;case "duplicate-id":row["id"]=FinalScoringDemoEntryTests.Id;break;case "duplicate-row":rows[1]=row.DeepClone();break;
            case "early-turn":row["turn_number"]=7;break;case "unknown-turn":row["turn_number"]=11;break;case "track-low":row["initial_space"]["US"]=-1;break;case "track-high":row["initial_space"]["US"]=9;break;case "track-extra":row["initial_space"]["award_vp"]=true;break;
            case "no-ability":row["initial_space"]["US"]=4;break;case "extra-action":row["initial_space"]["US"]=8;break;case "unknown-card":us[0]="card.unknown";break;case "scoring":us[0]="card.europe_scoring";break;case "china":us[0]="card.china";break;case "action-card":us[0]="card.fidel";break;
            case "duplicate-card":us[1]=us[0].DeepClone();break;case "cross-hand":row["held_cards"]["USSR"][0]=us[0].DeepClone();break;case "no-owner-cards":us.Clear();break;case "too-many-held":us.Add("card.nuclear_test_ban");break;case "unknown-side":row["held_cards"]["Other"]=new JArray();break;}
            var before=FinalScoringDemoEntryTests.Catalog();Assert.That(()=>before.WithSpaceDiscardScenarios(data.ToString()),Throws.InstanceOf<ArgumentException>());Assert.That(before.Scenarios.Count,Is.EqualTo(16));
        }
        [Test] public void Repeated_decoration_is_rejected(){Assert.Throws<ArgumentException>(()=>Catalog().WithSpaceDiscardScenarios(Data()));}
    }
}
