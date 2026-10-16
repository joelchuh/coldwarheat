using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ColdWarStruggle.Core;
using ColdWarStruggle.Application;
using ColdWarStruggle.Infrastructure;
using ColdWarStruggle.Presentation;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class IndependentRedsPlayTests
    {
        internal const string Scene="demo.practice.independent_reds";
        static readonly CardId Reds=new CardId("card.independent_reds");
        static CountryId C(string n)=>new CountryId("country."+n);
        internal static string Fixture()=>PlayInfluenceEventTests.Read("Data/Demo/independent-reds-scenario.json");
        internal static DemoCatalog Bind(DemoCatalog c)=>c.WithIndependentReds(IndependentRedsCatalogTests.Data(),IndependentRedsCatalogTests.Schema(),PlayInfluenceEventTests.Read("Data/Cards/deluxe-2015/cards.json"));
        internal static DemoCatalog Catalog()=>Bind(EmptyHandDemoTests.Catalog()).WithIndependentRedsScenario(Fixture());
        static ActionPracticeDefinition Definition(ActionPracticeDefinition d,MapDefinition map)
        {
            var effect=IndependentRedsCatalogLoader.Load(IndependentRedsCatalogTests.Data(),IndependentRedsCatalogTests.Schema(),map,BudgetedRemovalCatalogTests.KnownEffects);
            var effects=new PracticeInfluenceCatalog(map,d.Cards,d.InfluenceEvents.Effects.Values.Append(effect));
            return new ActionPracticeDefinition(d.Cards,d.Ops,d.Space,d.Calendar,d.Risk,d.Scoring,effects,d.DefconVpEvents,d.TurnOpsEvents,d.Realignment,d.China,d.TurnEndEnabled,d.NextTurnEnabled,d.HeadlineParameters,d.HeadlineDefectors,d.FinalScoring);
        }
        static GameState Ready()
        {
            return PlayScoringTests.Ready(Reds.Value,configure:d=>Definition(new ActionPracticeDefinition(d.Cards,d.Ops,d.Space,d.Calendar,d.Risk,d.Scoring,PlayInfluenceEventTests.Effects(d.Risk.Defcon.Map,d.Cards)),d.Risk.Defcon.Map));
        }
        static EventCommandEnvelope Choose(IGameSession s,string target,string id="choose")
        {var v=s.ReadView();return new EventCommandEnvelope(id,v.GameId,v.Version,EventCommandKind.Resolve,v.Event.ExecutionId,v.Event.DecisionId,new[]{C(target)});}
        static CardActionCommand Action(IGameSession s,CardActionKind kind,CardId? card=null,string id="play")
        {var v=s.ReadView();return new CardActionCommand(id,v.GameId,v.Version,kind,card);}
        [TestCase(false)][TestCase(true)] public async Task Direct_event_keeps_US_choice_even_when_USSR_plays_and_zero_change_still_removes_star(bool zero)
        {
            var s=Ready();var board=s.Countries.ToDictionary(x=>x.Key,x=>x.Value);board[C("romania")]=new CountryState(zero?6:1,4);s=PlayScoringTests.Replace(s,board);var h=new LocalGameHost(s,true);
            var play=Action(h.UssrSession,CardActionKind.Event,Reds);var started=await h.UssrSession.SubmitCardActionAsync(play);Assert.That(started.Accepted,Is.True);Assert.That(started.View.Event.Owner,Is.EqualTo(PlayerSide.US));Assert.That(started.View.Practice.CompletedActions,Is.Zero);
            var wrong=await h.UssrSession.SubmitEventAsync(Choose(h.UssrSession,"romania","wrong"));Assert.That(wrong.Accepted,Is.False);Assert.That(h.UsSession.ReadView().Version,Is.EqualTo(started.View.Version));
            var choose=Choose(h.UsSession,"romania");var done=await h.UsSession.SubmitEventAsync(choose);Assert.That(done.Accepted,Is.True);var v=h.UsSession.ReadView();Assert.That(v.Event,Is.Null);Assert.That(v.Practice.CompletedActions,Is.EqualTo(1));Assert.That(v.Practice.Actor,Is.EqualTo(PlayerSide.US));
            Assert.That(v.Countries.Single(c=>c.Definition.Id.Equals(C("romania"))).US,Is.EqualTo(zero?6:4));Assert.That(v.Cards.RemovedCount,Is.EqualTo(1));Assert.That(v.Cards.ResolvingCount,Is.Zero);
            Assert.That(await h.UsSession.SubmitEventAsync(choose),Is.SameAs(done));Assert.That(await h.UssrSession.SubmitCardActionAsync(play),Is.SameAs(started));Assert.That(h.UsSession.ReadView().Version,Is.EqualTo(v.Version));
        }
        [TestCase(true)][TestCase(false)] public void Opponent_event_and_OPS_finish_only_one_root_in_either_order(bool eventFirst)
        {
            var s=Ready();var begin=PlayOpponentInfluenceTests.Start(s,Reds.Value,eventFirst);Assert.That(begin.Accepted,Is.True,begin.Error.ToString());
            var waiting=eventFirst?begin.NextState:PlayOpponentInfluenceTests.Finish(begin.NextState).NextState;
            Assert.That(waiting.InfluenceEvent.Owner,Is.EqualTo(PlayerSide.US));Assert.That(waiting.Practice.Actor,Is.EqualTo(PlayerSide.USSR));Assert.That(waiting.Practice.CompletedActions,Is.Zero);
            var raw=InfluenceEventFlow.Resolve(waiting,PlayerSide.US,waiting.InfluenceEvent.DecisionId,new[]{C("romania")});var result=ActionPracticeRules.CompleteEventAction(raw);Assert.That(result.Accepted,Is.True);var next=result.NextState;
            if(eventFirst){Assert.That(next.Practice.CompletedActions,Is.Zero);Assert.That(next.Operations.Budget,Is.EqualTo(2));next=PlayOpponentInfluenceTests.Finish(next).NextState;}
            Assert.That(next.Practice.CompletedActions,Is.EqualTo(1));Assert.That(next.Cards.RemovedCards,Has.Member(Reds));Assert.That(next.Cards.ResolvingCards,Is.Empty);Pcg32RandomTests.Same(s.RandomState,next.RandomState);
        }
        [Test] public async Task Headline_uses_same_choice_then_resumes_other_headline_without_consuming_action_slot()
        {
            var s=HeadlineFlowTests.Prepared("independent_reds","fidel");var def=Definition(s.Practice.Definition,s.Map);
            var state=new GameState(s.Map,s.Countries,s.Operations,s.GameId,s.Version,s.SpentOps,s.EventSequence,turn:s.Turn,deployment:s.Deployment,cards:s.Cards,randomState:s.RandomState,practice:ActionPracticeState.Create(def));
            var h=new LocalGameHost(HeadlineFlowTests.Start(state),true);Assert.That(HeadlineSessionTests.Submit(h.UsSession,"independent_reds").Accepted,Is.True);Assert.That(HeadlineSessionTests.Submit(h.UssrSession,"fidel").Accepted,Is.True);
            var v=h.UsSession.ReadView();Assert.That(v.Event.Owner,Is.EqualTo(PlayerSide.US));Assert.That(v.Practice.CompletedActions,Is.Zero);
            Assert.That((await h.UsSession.SubmitEventAsync(Choose(h.UsSession,"yugoslavia"))).Accepted,Is.True);v=h.UsSession.ReadView();Assert.That(v.Headline.Status,Is.EqualTo(PublicHeadlineStatus.Completed));Assert.That(v.Practice.Actor,Is.EqualTo(PlayerSide.USSR));Assert.That(v.Practice.CompletedActions,Is.Zero);Assert.That(v.Cards.RemovedCount,Is.EqualTo(2));
        }
        [Test] public void Binding_preserves_previous_scenarios_and_all_services_and_rejects_duplicate_registration()
        {
            var old=EmptyHandDemoTests.Catalog();var c=Bind(old);Assert.That(c.Scenarios.Count,Is.EqualTo(old.Scenarios.Count));
            foreach(var source in old.Scenarios)
            {
                var target=c.Scenarios.Single(s=>s.Id==source.Id);Assert.That(target.Countries,Is.SameAs(source.Countries));Assert.That(target.InitialCards,Is.SameAs(source.InitialCards));
                if(source.PracticeDefinition==null){Assert.That(target,Is.SameAs(source));continue;}
                var a=source.PracticeDefinition;var b=target.PracticeDefinition;Assert.That(b.InfluenceEvents.Effects.Count,Is.EqualTo(a.InfluenceEvents.Effects.Count+1));Assert.That(a.InfluenceEvents.Effects.ContainsKey(Reds),Is.False);
                Assert.That(b.FinalScoring,Is.SameAs(a.FinalScoring));Assert.That(b.HeadlineParameters,Is.SameAs(a.HeadlineParameters));Assert.That(b.HeadlineDefectors,Is.SameAs(a.HeadlineDefectors));Assert.That(b.China,Is.SameAs(a.China));Assert.That(b.TurnOpsEvents,Is.SameAs(a.TurnOpsEvents));Assert.That(b.Risk,Is.SameAs(a.Risk));
            }
            Assert.That(c.CreateHost(EmptyHandDemoTests.Id).UssrSession.ReadView().Practice.CanSkipEmptyHand,Is.True);
            Assert.Throws<ArgumentException>(()=>Bind(c));
        }
        [Test] public void Fixture_has_real_partitioned_early_cards_and_known_matching_targets()
        {
            var c=Catalog();Assert.That(c.Scenarios.Count,Is.EqualTo(22));var h=c.CreateHost(Scene);var su=h.UssrSession.ReadView();Assert.That(su.OwnHand,Is.EqualTo(new[]{Reds.Value}));Assert.That(h.UsSession.ReadView().OwnHand,Is.EqualTo(new[]{"card.olympic_games"}));Assert.That(su.Practice.CompletedActions,Is.Zero);Assert.That(su.Version,Is.EqualTo(1));Assert.That(su.Cards.DrawPileCount,Is.EqualTo(33));
            Assert.That(h.UssrSession.QueryCardActions().Cards.Single().Event,Is.EqualTo(RuleError.None));Assert.That(su.Countries.Single(c=>c.Definition.Id.Equals(C("romania"))).USSR,Is.EqualTo(4));Assert.That(c.Scenarios.Last().Title,Does.Contain("人工局面"));
        }
        [TestCase("duplicate")][TestCase("scope")][TestCase("future-turn")][TestCase("wrong-event")][TestCase("same-card")][TestCase("unknown-country")][TestCase("repeated-country")][TestCase("negative")]
        public void Malformed_fixture_is_rejected_without_changing_source_catalog(string kind)
        {
            var c=Bind(EmptyHandDemoTests.Catalog());var data=JObject.Parse(Fixture());var row=data["scenario"];
            switch(kind){case "duplicate":row["id"]=EmptyHandDemoTests.Id;break;case "scope":data["scope"]="production";break;case "future-turn":row["turn_number"]=99;break;case "wrong-event":row["event_card"]="card.comecon";break;case "same-card":row["other_card"]=Reds.Value;break;case "unknown-country":row["influence"][0]["country_id"]="country.imaginary";break;case "repeated-country":row["influence"][0]["country_id"]="country.romania";break;case "negative":row["influence"][0]["us"]=-1;break;}
            Assert.Throws<ArgumentException>(()=>c.WithIndependentRedsScenario(data.ToString()));Assert.That(c.Scenarios.Count,Is.EqualTo(21));
        }
        [Test] public async Task Controller_uses_existing_country_choice_and_readonly_privacy_boundary()
        {
            var c=Catalog();var controller=new DemoController(c.CreateHost);controller.LoadScenario(Scene,PlayerSide.USSR,C("romania"));controller.SelectCard(Reds);
            Assert.That((await controller.PlayCardAsync(CardActionKind.Event)).Accepted,Is.True);Assert.That(controller.EventTargets().Error,Is.EqualTo(RuleError.WrongActor));controller.SelectSide(PlayerSide.US);
            Assert.That(controller.EventTargets().Targets,Has.Count.EqualTo(5));Assert.That(controller.ToggleEventTarget(C("romania")),Is.True);Assert.That(controller.CanConfirmEvent,Is.True);Assert.That((await controller.ConfirmEventAsync()).Accepted,Is.True);
            Assert.That(controller.ReadView().Countries.Single(c=>c.Definition.Id.Equals(C("romania"))).US,Is.EqualTo(4));Assert.That(controller.ReadView().Practice.CompletedActions,Is.EqualTo(1));Assert.That(controller.EventDraft,Is.Empty);
        }
    }
}
