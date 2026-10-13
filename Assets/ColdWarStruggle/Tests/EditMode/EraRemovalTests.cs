using System;
using System.Collections.Generic;
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
    public class EraRemovalTests
    {
        static CountryId Id(string name)=>BudgetedRemovalTests.Id(name);
        static string Read(string path)=>File.ReadAllText(Path.Combine(UnityEngine.Application.dataPath,"../"+path));
        public static string Parameters()=>Read("Data/Cards/deluxe-2015/early-influence-batch2.parameters.json");
        public static string Schema()=>Read("Docs/Contracts/early-influence-batch2.schema.json");
        public static IReadOnlyCollection<string> References()=>CardEffectReferenceIndex.Load(Read("Data/Cards/deluxe-2015/cards.json"));
        public static EraCountryRemovalEffect Effect()=>EraRemovalCatalogLoader.Load(Parameters(),Schema(),DefinitionTests.World(),References());
        static GameState State(int? turn,params (string country,int us,int ussr)[] values)
        {
            var s=BudgetedRemovalTests.State(values);
            return new GameState(s.Map,s.Countries,s.Operations,turn:turn.HasValue?TurnCalendarTests.Calendar().ForTurn(turn.Value):null);
        }
        static InfluenceEventQuery Query(GameState state,EraCountryRemovalEffect effect=null)
            =>InfluenceEventRules.Query(state,effect??Effect(),InfluenceEventPolicy.NoActiveInterference);
        static InfluenceEventPlan Plan(GameState state,params string[] selected)
            =>InfluenceEventRules.Plan(state,Effect(),new InfluenceEventSelection(PlayerSide.US,state.Version,selected.Select(Id)),InfluenceEventPolicy.NoActiveInterference);

        [TestCase(1,1)] [TestCase(3,1)] [TestCase(4,1)] [TestCase(7,1)] [TestCase(8,2)] [TestCase(10,2)]
        public void Actual_turn_controls_removal_not_printed_early_card_era(int turn,int amount)
        {
            var state=State(turn,("poland",4,4),("east_germany",3,3),("romania",9,2),("france",0,5));
            var query=Query(state);Assert.That(query.Kind,Is.EqualTo(InfluenceDecisionKind.CountrySet));Assert.That(query.Owner,Is.EqualTo(PlayerSide.US));
            Assert.That(query.RequiredTargets,Is.EqualTo(3));Assert.That(query.RequiredAmount,Is.Zero);
            var result=Plan(state,"romania","poland","east_germany");Assert.That(result.Resolved,Is.True);
            Assert.That(result.NextState.GetInfluence(Id("poland"),PlayerSide.USSR),Is.EqualTo(4-amount));
            Assert.That(result.NextState.GetInfluence(Id("romania"),PlayerSide.USSR),Is.EqualTo(2-amount));
            Assert.That(result.NextState.GetInfluence(Id("romania"),PlayerSide.US),Is.EqualTo(9));
            Assert.That(result.NextState.GetInfluence(Id("france"),PlayerSide.USSR),Is.EqualTo(5));
            Assert.That(result.NextState.Turn,Is.SameAs(state.Turn));Assert.That(state.Turn.Number,Is.EqualTo(turn));
            Assert.That(result.NextState.Operations,Is.SameAs(state.Operations));Assert.That(result.NextState.SpentOps,Is.Zero);
            Assert.That(result.Changes.Select(c=>c.Country),Is.EqualTo(new[]{Id("east_germany"),Id("poland"),Id("romania")}));
            Assert.That(result.ResolutionSequence,Is.EqualTo(4));Assert.That(state.Version,Is.Zero);
        }
        [Test] public void Missing_turn_is_explicit_error_even_with_zero_candidates()
        {
            var state=State(null);Assert.That(state.Turn,Is.Null);var query=Query(state);
            Assert.That(query.Error,Is.EqualTo(InfluenceEventError.MissingTurnContext));Assert.That(query.Status,Is.EqualTo(InfluenceEventStatus.Rejected));
            var plan=InfluenceEventRules.Plan(state,Effect(),null,InfluenceEventPolicy.NoActiveInterference);
            Assert.That(plan.Resolved,Is.False);Assert.That(plan.NextState,Is.Null);
        }
        [Test] public void Shortage_uses_all_positive_candidates_and_clamps_each_country_independently()
        {
            var state=State(8,("austria",9,1),("finland",0,5),("poland",3,0));var query=Query(state);
            Assert.That(query.LegalCountries,Is.EqualTo(new[]{Id("austria"),Id("finland")}));Assert.That(query.RequiredTargets,Is.EqualTo(2));
            var result=Plan(state,"finland","austria");Assert.That(result.Resolved,Is.True);
            Assert.That(result.NextState.GetInfluence(Id("austria"),PlayerSide.USSR),Is.Zero);
            Assert.That(result.NextState.GetInfluence(Id("finland"),PlayerSide.USSR),Is.EqualTo(3));
        }
        [Test] public void Player_may_choose_three_low_amount_targets_despite_richer_alternatives()
        {
            var state=State(9,("austria",0,1),("finland",0,1),("poland",0,1),("romania",0,9));
            var result=Plan(state,"poland","finland","austria");Assert.That(result.Resolved,Is.True);
            Assert.That(result.Changes.Sum(x=>x.Before-x.After),Is.EqualTo(3));Assert.That(result.NextState.GetInfluence(Id("romania"),PlayerSide.USSR),Is.EqualTo(9));
        }
        [Test] public void No_positive_candidate_completes_without_an_unanswerable_decision()
        {
            var state=State(8,("france",0,7));Assert.That(Query(state).Status,Is.EqualTo(InfluenceEventStatus.Ready));
            var plan=InfluenceEventRules.Plan(state,Effect(),null,InfluenceEventPolicy.NoActiveInterference);
            Assert.That(plan.Status,Is.EqualTo(InfluenceEventStatus.ResolvedNoChange));Assert.That(plan.Changes,Is.Empty);
            Assert.That(plan.NextState.Version,Is.EqualTo(1));Assert.That(plan.NextState.EventSequence,Is.EqualTo(1));Assert.That(plan.NextState.Turn,Is.SameAs(state.Turn));
        }
        [TestCase("duplicate")] [TestCase("too-few")] [TestCase("outside")] [TestCase("zero")]
        [TestCase("wrong-side")] [TestCase("stale")] [TestCase("amount")]
        public void Illegal_selection_never_partially_removes_influence(string kind)
        {
            var state=State(8,("poland",0,3),("romania",0,3),("hungary",0,3));var targets=new[]{Id("poland"),Id("romania"),Id("hungary")};
            if(kind=="duplicate")targets[1]=targets[0];if(kind=="too-few")targets=targets.Take(2).ToArray();
            if(kind=="outside")targets[0]=Id("france");if(kind=="zero")targets[0]=Id("finland");
            var selection=kind=="amount"?InfluenceEventSelection.Allocate(PlayerSide.US,state.Version,new[]{new InfluenceAllocation(Id("poland"),6)})
                :new InfluenceEventSelection(kind=="wrong-side"?PlayerSide.USSR:PlayerSide.US,kind=="stale"?99:state.Version,targets);
            var result=InfluenceEventRules.Plan(state,Effect(),selection,InfluenceEventPolicy.NoActiveInterference);
            Assert.That(result.Resolved,Is.False);Assert.That(result.NextState,Is.Null);Assert.That(state.GetInfluence(Id("poland"),PlayerSide.USSR),Is.EqualTo(3));
        }
        [Test] public void Definition_copies_amounts_and_requires_each_supported_era()
        {
            var amounts=new Dictionary<GameEra,int>{{GameEra.Early,1},{GameEra.Mid,1},{GameEra.Late,2}};
            var effect=new EraCountryRemovalEffect("card.test","effect.test",PlayerSide.US,"region.eastern_europe",3,amounts);
            amounts[GameEra.Late]=99;Assert.That(effect.AmountByEra[GameEra.Late],Is.EqualTo(2));
            Assert.Throws<NotSupportedException>(()=>((IDictionary<GameEra,int>)effect.AmountByEra).Clear());
            amounts.Remove(GameEra.Mid);Assert.That(()=>new EraCountryRemovalEffect("card.test","effect.test",PlayerSide.US,"region.eastern_europe",3,amounts),Throws.ArgumentException);
        }
        [TestCase("source")] [TestCase("unknown-effect")] [TestCase("region")]
        [TestCase("era-source")] [TestCase("era-pointer")] [TestCase("identity")]
        public void Actual_parameter_loader_checks_its_own_sources_and_references(string kind)
        {
            var root=JObject.Parse(Parameters());var p=root["parameters"]["east_european_unrest"];var refs=References();
            if(kind=="source")p["source_refs"][0]["source_id"]="unknown";
            if(kind=="unknown-effect")refs=refs.Where(x=>x!="effect.east_european_unrest").ToArray();
            if(kind=="region")p["target_region_id"]="region.missing";
            if(kind=="era-source")p["era_source"]["dataset_id"]="rules.other";
            if(kind=="era-pointer")p["era_source"]["json_pointer"]="/headline";
            if(kind=="identity")p["card_id"]="card.suez_crisis";
            Assert.That(()=>EraRemovalCatalogLoader.Load(root.ToString(),Schema(),DefinitionTests.World(),refs),Throws.InstanceOf<ArgumentException>());
        }
        [Test] public async Task Fixed_sessions_preserve_turn_through_waiting_resolution_and_retry()
        {
            var s=State(8,("austria",0,1),("finland",0,2));var execution=new InfluenceEventExecution("era-event",Effect(),PlayerSide.USSR,InfluenceEventPolicy.NoActiveInterference);
            var host=new LocalGameHost(new GameState(s.Map,s.Countries,s.Operations,"era-game",influenceEvent:execution,turn:s.Turn),true);
            var soviet=host.UssrSession;var american=host.UsSession;
            var begin=await soviet.SubmitEventAsync(EventSessionTests.Begin(soviet));Assert.That(begin.Accepted,Is.True);
            var v=american.ReadView();Assert.That(v.Turn,Is.SameAs(s.Turn));Assert.That(v.Turn.Era,Is.EqualTo(GameEra.Late));
            Assert.That(soviet.QueryEventTargets(v.Event.ExecutionId,v.Version).Error,Is.EqualTo(RuleError.WrongActor));
            var cmd=new EventCommandEnvelope("era-select",v.GameId,v.Version,EventCommandKind.Resolve,v.Event.ExecutionId,v.Event.DecisionId,new[]{Id("finland"),Id("austria")});
            var result=await american.SubmitEventAsync(cmd);Assert.That(result.Accepted,Is.True);Assert.That(result.View.Turn,Is.SameAs(s.Turn));
            Assert.That(await american.SubmitEventAsync(cmd),Is.SameAs(result));Assert.That(american.ReadView().Version,Is.EqualTo(2));
        }
        [Test] public async Task Missing_turn_is_a_named_session_error_and_does_not_start_event()
        {
            var s=State(null);var execution=new InfluenceEventExecution("missing-turn",Effect(),PlayerSide.USSR,InfluenceEventPolicy.NoActiveInterference);
            var h=new LocalGameHost(new GameState(s.Map,s.Countries,s.Operations,influenceEvent:execution),true);
            var result=await h.UssrSession.SubmitEventAsync(EventSessionTests.Begin(h.UssrSession));
            Assert.That(result.Error,Is.EqualTo(RuleError.MissingTurnContext));Assert.That(result.View.Version,Is.Zero);
            Assert.That(result.View.Event.Stage,Is.EqualTo(InfluenceEventStage.Ready));Assert.That(result.View.Turn,Is.Null);
        }
        [Test] public async Task Ordinary_placement_preserves_turn_and_hand_projection()
        {
            var s=State(4);var countries=s.Countries.ToDictionary(p=>p.Key,p=>p.Value);
            var ops=OperationsContext.FromRoundStart("turn-ops",PlayerSide.US,1,s.Map,countries,new InfluenceCosts(1,2));
            var hands=new Dictionary<PlayerSide,IReadOnlyList<string>>{{PlayerSide.US,new[]{"private.us"}},{PlayerSide.USSR,new[]{"private.ussr"}}};
            var h=new LocalGameHost(new GameState(s.Map,countries,ops,hands:hands,turn:s.Turn),true);var v=h.UsSession.ReadView();
            var target=h.UsSession.QueryInfluenceTargets(v.ContextId,v.Version).Targets.First();
            var result=await h.UsSession.SubmitAsync(new CommandEnvelope("ops",v.GameId,v.Version,v.ContextId,new[]{target}));
            Assert.That(result.Accepted,Is.True);Assert.That(result.View.Turn,Is.SameAs(s.Turn));
            Assert.That(result.View.OwnHand,Is.EqualTo(new[]{"private.us"}));Assert.That(result.View.OpponentHandCount,Is.EqualTo(1));
            Assert.That(h.UssrSession.ReadView().OwnHand,Is.EqualTo(new[]{"private.ussr"}));
        }
        [TestCase(true)] [TestCase(false)] public void Version_or_sequence_overflow_is_atomic(bool version)
        {
            var s=State(8,("poland",0,1));var state=new GameState(s.Map,s.Countries,s.Operations,
                version:version?long.MaxValue:0,eventSequence:version?0:long.MaxValue,turn:s.Turn);
            var result=Plan(state,"poland");Assert.That(result.Error,Is.EqualTo(InfluenceEventError.NumericOverflow));
            Assert.That(result.NextState,Is.Null);Assert.That(state.GetInfluence(Id("poland"),PlayerSide.USSR),Is.EqualTo(1));
        }
        [TestCase(InfluenceEventPolicy.Unknown,InfluenceEventError.InterferenceUnknown)]
        [TestCase(InfluenceEventPolicy.Forbidden,InfluenceEventError.Forbidden)]
        public void Existing_interference_gate_still_precedes_era_effect(InfluenceEventPolicy policy,InfluenceEventError expected)
        { Assert.That(InfluenceEventRules.Query(State(8),Effect(),policy).Error,Is.EqualTo(expected)); }
        [Test] public void Unknown_region_and_ended_game_do_not_run_event()
        {
            var s=State(8);var original=Effect();var bad=new EraCountryRemovalEffect(original.CardId,original.EffectId,original.Beneficiary,"region.missing",3,original.AmountByEra);
            Assert.That(Query(s,bad).Error,Is.EqualTo(InfluenceEventError.InvalidDefinition));
            var ended=new GameState(s.Map,s.Countries,s.Operations,ended:true,turn:s.Turn);
            Assert.That(Query(ended).Error,Is.EqualTo(InfluenceEventError.GameAlreadyEnded));
        }
        [TestCase("null")] [TestCase("unknown-era")] [TestCase("zero-amount")]
        [TestCase("empty-region")] [TestCase("zero-count")] [TestCase("excess-count")]
        public void Definition_rejects_unusable_values(string kind)
        {
            var amounts=new Dictionary<GameEra,int>{{GameEra.Early,1},{GameEra.Mid,1},{GameEra.Late,2}};
            if(kind=="unknown-era"){amounts.Remove(GameEra.Mid);amounts.Add((GameEra)99,1);}if(kind=="zero-amount")amounts[GameEra.Mid]=0;
            Assert.That(()=>new EraCountryRemovalEffect("card.test","effect.test",PlayerSide.US,kind=="empty-region"?"":"region.eastern_europe",
                kind=="zero-count"?0:kind=="excess-count"?129:3,kind=="null"?null:amounts),Throws.InstanceOf<ArgumentException>());
        }
    }
}
