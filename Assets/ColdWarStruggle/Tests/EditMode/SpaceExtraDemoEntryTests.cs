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
    public class SpaceExtraDemoEntryTests
    {
        internal const string EarlyId="demo.practice.space_extra.early",FinalId="demo.practice.space_extra.final";
        internal static string Data()=>FinalScoringDemoEntryTests.Read("Data/Demo/space-extra-action-scenarios.json");
        internal static DemoCatalog Catalog()=>SpaceDiscardDemoEntryTests.Catalog().WithSpaceExtraScenarios(Data());
        [Test] public void Appending_two_explicit_fixtures_keeps_all_eighteen_existing_scenes()
        {
            var old=SpaceDiscardDemoEntryTests.Catalog();var next=old.WithSpaceExtraScenarios(Data());Assert.That(next.Scenarios.Count,Is.EqualTo(20));
            foreach(var s in old.Scenarios)Assert.That(next.Scenarios.Single(n=>n.Id==s.Id),Is.SameAs(s));
            Assert.That(next.CreateHost(SpaceDiscardDemoEntryTests.NextId).UsSession.ReadView().Practice.TurnEndAvailable,Is.True);
        }
        [TestCase(EarlyId,PlayerSide.US,2,6)] [TestCase(FinalId,PlayerSide.USSR,10,7)]
        public async Task Fixtures_reach_real_optional_boundary_and_retain_legal_era_cards(string id,PlayerSide owner,int turn,int normal)
        {
            var c=Catalog();var scene=c.Scenarios.Single(s=>s.Id==id);Assert.That(scene.Title,Does.Contain("人工局面"));
            Assert.That(scene.Countries,Is.SameAs(c.Scenarios.Single(s=>s.Id==FinalScoringDemoEntryTests.Id).Countries));
            var h=c.CreateHost(id);var session=owner==PlayerSide.US?h.UsSession:h.UssrSession;var v=session.ReadView();
            Assert.That(v.Turn.Number,Is.EqualTo(turn));Assert.That(v.Version,Is.EqualTo(1+4*normal));Assert.That(v.Practice.CompletedActions,Is.EqualTo(2*normal));
            Assert.That(v.Practice.Actor,Is.EqualTo(owner));Assert.That(v.Practice.Round,Is.EqualTo(normal+1));Assert.That(v.Practice.RoundLimit,Is.EqualTo(8));
            Assert.That(v.Practice.IsExtraAction,Is.True);Assert.That(v.Practice.CanDeclineExtraActions,Is.True);Assert.That(v.Practice.TurnEndAvailable,Is.False);
            Assert.That(v.OwnHand,Has.Count.EqualTo(2));Assert.That(v.OpponentHandCount,Is.Zero);Assert.That(v.Practice.UsAttempts+v.Practice.UssrAttempts,Is.Zero);
            foreach(var card in v.OwnHand)Assert.That(c.PrintedCards.GetCard(new CardId(card)).Era<=v.Turn.Era,Is.True);
            async Task<PlayerViewSnapshot> Send(CardActionKind kind,CardId? card=null)
            { var now=session.ReadView();var result=await session.SubmitCardActionAsync(new CardActionCommand("fixture-test-"+now.Version,now.GameId,now.Version,kind,card));Assert.That(result.Accepted,Is.True,result.Error.ToString());return result.View; }
            if(turn==2)
            {
                foreach(var card in v.OwnHand){await Send(CardActionKind.Influence,new CardId(card));await Send(CardActionKind.FinishInfluence);}
                v=session.ReadView();Assert.That(v.Practice.CompletedActions,Is.EqualTo(14));Assert.That(v.Practice.Stage,Is.EqualTo(ActionPracticeStage.Complete));
                await Send(CardActionKind.AssessTurnEnd);v=await Send(CardActionKind.BeginNextTurn);Assert.That(v.Turn.Number,Is.EqualTo(3));Assert.That(v.OwnHand,Has.Count.EqualTo(8));Assert.That(v.Practice.CompletedActions,Is.Zero);
            }
            else
            {
                await Send(CardActionKind.DeclineExtraActions);await Send(CardActionKind.AssessTurnEnd);await Send(CardActionKind.KeepHeldCards);
                v=await Send(CardActionKind.FinalScoring);Assert.That(v.Practice.FinalScoring,Is.Not.Null);Assert.That(v.Practice.CompletedActions,Is.EqualTo(14));
            }
        }
        [TestCase("dataset")] [TestCase("scope")] [TestCase("count")] [TestCase("unknown-field")] [TestCase("duplicate-id")] [TestCase("source")]
        [TestCase("turn-one")] [TestCase("equal-track")] [TestCase("no-ability")] [TestCase("scoring")] [TestCase("future-card")] [TestCase("duplicate-card")] [TestCase("empty-owner")]
        public void Invalid_fixture_inputs_fail_before_creating_playable_state(string fault)
        {
            var d=JObject.Parse(Data());var row=(JObject)d["scenarios"][0];
            switch(fault)
            {
                case "dataset":d["dataset_id"]="bad";break;case "scope":d["scope"]="production";break;case "count":((JArray)d["scenarios"]).RemoveAt(1);break;
                case "unknown-field":row["pretend_actions"]=6;break;case "duplicate-id":row["id"]=SpaceDiscardDemoEntryTests.NextId;break;case "source":row["source_scenario_id"]=SpaceDiscardDemoEntryTests.NextId;break;
                case "turn-one":row["turn_number"]=1;break;case "equal-track":row["initial_space"]["USSR"]=8;break;case "no-ability":row["initial_space"]["US"]=7;break;
                case "scoring":row["held_cards"]["US"][0]="card.asia_scoring";break;case "future-card":row["held_cards"]["US"][0]="card.wargames";break;
                case "duplicate-card":row["held_cards"]["US"][1]=row["held_cards"]["US"][0].DeepClone();break;case "empty-owner":row["held_cards"]["US"]=new JArray();break;
            }
            Assert.Throws<ArgumentException>(()=>SpaceDiscardDemoEntryTests.Catalog().WithSpaceExtraScenarios(d.ToString()));
        }
        [Test] public void Same_dataset_cannot_be_appended_twice()=>Assert.Throws<ArgumentException>(()=>Catalog().WithSpaceExtraScenarios(Data()));
    }
}
