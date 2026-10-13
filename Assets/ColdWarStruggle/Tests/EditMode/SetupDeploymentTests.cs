using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ColdWarStruggle.Core;
using ColdWarStruggle.Infrastructure;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace ColdWarStruggle.Tests
{
    public class SetupDeploymentTests
    {
        public static string Source()=>File.ReadAllText(Path.Combine(DefinitionTests.Root,"Data/Rulesets/deluxe-2015/setup.json"));
        public static string Schema()=>File.ReadAllText(Path.Combine(DefinitionTests.Root,"Docs/Contracts/setup.schema.json"));
        public static SetupDeploymentDefinition Definition(MapDefinition map=null)=>SetupDeploymentLoader.LoadSubset(Source(),Schema(),map??DefinitionTests.World());
        static CountryId C(string id)=>new CountryId("country."+id);
        static InfluenceAllocation A(string id,int amount)=>new InfluenceAllocation(C(id),amount);
        public static GameState Empty(MapDefinition map,long version=0,long sequence=0,bool ended=false)
        {
            var countries=map.Countries.ToDictionary(c=>c.Id,c=>new CountryState(0,0));
            var context=OperationsContext.FromRoundStart("setup-no-ops",PlayerSide.USSR,0,map,countries,new InfluenceCosts(1,2));
            return new GameState(map,countries,context,"setup-fixture",version,eventSequence:sequence,ended:ended,
                hands:new Dictionary<PlayerSide,IReadOnlyList<string>>{[PlayerSide.US]=new[]{"fixture.us.secret"},[PlayerSide.USSR]=new[]{"fixture.ussr.secret"}},
                turn:TurnCalendarTests.Calendar().ForTurn(1));
        }
        public static GameState Started()
        { var d=Definition();return InitialDeploymentFlow.Begin(Empty(d.Map),d,"deployment").NextState; }
        static DeploymentTransition Submit(GameState s,PlayerSide actor,params InfluenceAllocation[] allocations)
            =>InitialDeploymentFlow.Resolve(s,actor,s.Deployment.DecisionId,allocations);

        [Test] public void Real_data_has_exact_official_fixed_values_and_shared_subregions()
        {
            var d=Definition();Assert.That(d.Stages.Select(s=>s.Side),Is.EqualTo(new[]{PlayerSide.USSR,PlayerSide.US}));
            Assert.That(d.Stages[0].FixedInfluence.Select(a=>a.Country.Value+":"+a.Amount).OrderBy(x=>x),Is.EqualTo(new[]{"country.east_germany:3","country.finland:1","country.iraq:1","country.north_korea:3","country.syria:1"}));
            Assert.That(d.Stages[1].FixedInfluence.Select(a=>a.Country.Value+":"+a.Amount).OrderBy(x=>x),Is.EqualTo(new[]{"country.australia:4","country.canada:2","country.iran:1","country.israel:1","country.japan:1","country.panama:1","country.philippines:1","country.south_africa:1","country.south_korea:1","country.uk:5"}));
            Assert.That(d.Stages.Select(s=>s.FreeAmount),Is.EqualTo(new[]{6,7}));
            Assert.That(d.Stages.Select(s=>s.Region),Is.EqualTo(new[]{"region.eastern_europe","region.western_europe"}));
        }
        [Test] public void Begin_commits_only_soviet_fixed_influence_and_preserves_prepared_context()
        {
            var d=Definition();var original=Empty(d.Map);var t=InitialDeploymentFlow.Begin(original,d,"deployment");var s=t.NextState;
            Assert.That(t.Accepted,Is.True);Assert.That(original.Countries.Values.Sum(c=>c.USSR),Is.Zero);
            Assert.That(s.Countries.Values.Sum(c=>c.USSR),Is.EqualTo(9));Assert.That(s.Countries.Values.Sum(c=>c.US),Is.Zero);
            Assert.That(s.Version,Is.EqualTo(1));Assert.That(t.Changes.Count,Is.EqualTo(5));Assert.That(s.EventSequence,Is.EqualTo(6));
            Assert.That(t.DecisionRequestedSequence,Is.EqualTo(6));Assert.That(t.CompletionSequence,Is.Null);
            Assert.That(s.Deployment.Owner,Is.EqualTo(PlayerSide.USSR));Assert.That(s.Deployment.DecisionSourceVersion,Is.EqualTo(s.Version));
            Assert.That(s.GetHand(PlayerSide.US),Is.EqualTo(original.GetHand(PlayerSide.US)));Assert.That(s.Turn,Is.SameAs(original.Turn));
            Assert.That(s.Operations,Is.SameAs(original.Operations));Assert.That(s.SpentOps,Is.Zero);Assert.That(s.GameId,Is.EqualTo(original.GameId));
        }
        [Test] public void Two_full_allocations_advance_once_and_never_enter_turn_or_repeat_fixed_points()
        {
            var s=Started();var soviet=Submit(s,PlayerSide.USSR,A("poland",6));var u=soviet.NextState;
            Assert.That(soviet.Accepted,Is.True);Assert.That(u.Version,Is.EqualTo(2));Assert.That(u.Deployment.Owner,Is.EqualTo(PlayerSide.US));
            Assert.That(u.GetInfluence(C("poland"),PlayerSide.USSR),Is.EqualTo(6));Assert.That(u.Countries.Values.Sum(c=>c.US),Is.EqualTo(18));
            Assert.That(u.EventSequence,Is.EqualTo(18));Assert.That(soviet.Changes.Count,Is.EqualTo(11));
            var us=Submit(u,PlayerSide.US,A("canada",7));var done=us.NextState;
            Assert.That(done.Countries.Values.Sum(c=>c.USSR),Is.EqualTo(15));Assert.That(done.Countries.Values.Sum(c=>c.US),Is.EqualTo(25));
            Assert.That(done.GetInfluence(C("canada"),PlayerSide.US),Is.EqualTo(9));Assert.That(done.Version,Is.EqualTo(3));
            Assert.That(done.EventSequence,Is.EqualTo(20));Assert.That(us.CompletionSequence,Is.EqualTo(20));Assert.That(us.DecisionRequestedSequence,Is.Null);
            Assert.That(done.Deployment.Complete,Is.True);Assert.That(done.Deployment.Owner,Is.Null);Assert.That(done.Deployment.DecisionId,Is.Null);
            Assert.That(done.Turn,Is.SameAs(s.Turn));Assert.That(Submit(done,PlayerSide.US,A("canada",7)).Error,Is.EqualTo(RuleError.SetupComplete));
            Assert.That(InitialDeploymentFlow.Begin(done,done.Deployment.Definition,"another").Error,Is.EqualTo(RuleError.SetupInProgress));
        }
        [Test] public void Dual_subregions_and_enemy_control_do_not_add_cost_or_stability_cap()
        {
            var s=Started();var q=InitialDeploymentFlow.QueryDecision(s,PlayerSide.USSR);
            Assert.That(q.Targets,Does.Contain(C("austria")));Assert.That(q.Targets,Does.Contain(C("finland")));Assert.That(q.Targets,Has.No.Member(C("canada")));
            var u=Submit(s,PlayerSide.USSR,A("austria",6)).NextState;
            var us=InitialDeploymentFlow.QueryDecision(u,PlayerSide.US);
            Assert.That(us.Targets,Does.Contain(C("canada")));Assert.That(us.Targets,Does.Contain(C("finland")));Assert.That(us.Targets,Does.Contain(C("austria")));
            var done=Submit(u,PlayerSide.US,A("austria",7)).NextState;
            Assert.That(done.GetInfluence(C("austria"),PlayerSide.US),Is.EqualTo(7));Assert.That(done.GetInfluence(C("austria"),PlayerSide.USSR),Is.EqualTo(6));
            Assert.That(done.SpentOps,Is.Zero);
        }
        [TestCase("null",RuleError.InvalidCommand)] [TestCase("empty",RuleError.InvalidCommand)]
        [TestCase("many",RuleError.InvalidCommand)] [TestCase("duplicate",RuleError.DuplicateCountry)]
        [TestCase("zero",RuleError.InvalidAllocationAmount)] [TestCase("negative",RuleError.InvalidAllocationAmount)]
        [TestCase("unknown",RuleError.UnknownCountry)] [TestCase("outside",RuleError.OutsideSetupRegion)]
        [TestCase("short",RuleError.IncorrectAllocationTotal)] [TestCase("long",RuleError.IncorrectAllocationTotal)]
        [TestCase("sum-overflow",RuleError.IncorrectAllocationTotal)]
        public void Invalid_allocations_return_no_partial_state(string kind,RuleError expected)
        {
            var s=Started();IReadOnlyList<InfluenceAllocation> a=new[]{A("poland",6)};
            switch(kind)
            {
                case "null":a=null;break;case "empty":a=Array.Empty<InfluenceAllocation>();break;
                case "many":a=Enumerable.Repeat(A("poland",1),129).ToArray();break;
                case "duplicate":a=new[]{A("poland",3),A("poland",3)};break;
                case "zero":a=new[]{A("poland",0)};break;case "negative":a=new[]{A("poland",-1)};break;
                case "unknown":a=new[]{A("missing",6)};break;case "outside":a=new[]{A("france",6)};break;
                case "short":a=new[]{A("poland",5)};break;case "long":a=new[]{A("poland",7)};break;
                case "sum-overflow":a=new[]{A("poland",int.MaxValue),A("austria",int.MaxValue)};break;
            }
            var t=InitialDeploymentFlow.Resolve(s,PlayerSide.USSR,s.Deployment.DecisionId,a);
            Assert.That(t.Error,Is.EqualTo(expected));Assert.That(t.NextState,Is.Null);Assert.That(t.Changes,Is.Empty);
            Assert.That(s.Version,Is.EqualTo(1));Assert.That(s.GetInfluence(C("poland"),PlayerSide.USSR),Is.Zero);
        }
        [Test] public void Identity_decision_and_query_checks_do_not_leak_drafts_or_allow_stale_selection()
        {
            var s=Started();Assert.That(Submit(s,PlayerSide.US,A("poland",6)).Error,Is.EqualTo(RuleError.WrongActor));
            Assert.That(InitialDeploymentFlow.Resolve(s,PlayerSide.USSR,"old",new[]{A("poland",6)}).Error,Is.EqualTo(RuleError.StaleDecision));
            var q=InitialDeploymentFlow.QueryDecision(s,PlayerSide.US);Assert.That(q.Error,Is.EqualTo(RuleError.WrongActor));Assert.That(q.Targets,Is.Empty);Assert.That(q.RequiredAmount,Is.Zero);
            var valid=InitialDeploymentFlow.QueryDecision(s,PlayerSide.USSR);Assert.That(valid.RequiredAmount,Is.EqualTo(6));
            Assert.That(valid.Targets,Is.Ordered.Using<CountryId>(Comparer<CountryId>.Create((a,b)=>StringComparer.Ordinal.Compare(a.Value,b.Value))));
            Assert.Throws<NotSupportedException>(()=>((IList<CountryId>)valid.Targets).Clear());
        }
        [TestCase("ended",RuleError.GameAlreadyEnded)] [TestCase("dirty",RuleError.InvalidSetupState)]
        [TestCase("ops",RuleError.InvalidSetupState)] [TestCase("foreign-map",RuleError.InvalidDefinition)]
        [TestCase("version-overflow",RuleError.NumericOverflow)] [TestCase("sequence-overflow",RuleError.NumericOverflow)]
        public void Begin_requires_clean_prepared_state_and_is_atomic(string kind,RuleError expected)
        {
            var d=Definition();var s=Empty(d.Map,kind=="version-overflow"?long.MaxValue:0,kind=="sequence-overflow"?long.MaxValue:0,kind=="ended");
            if(kind=="dirty" || kind=="ops")
            {
                var countries=s.Countries.ToDictionary(x=>x.Key,x=>x.Value);if(kind=="dirty")countries[C("poland")]=new CountryState(1,0);
                var context=kind=="ops"?OperationsContext.FromRoundStart("ops",PlayerSide.US,1,d.Map,countries,new InfluenceCosts(1,2)):s.Operations;
                s=new GameState(d.Map,countries,context);
            }
            if(kind=="foreign-map")d=Definition();
            var t=InitialDeploymentFlow.Begin(s,d,"start");Assert.That(t.Error,Is.EqualTo(expected));Assert.That(t.NextState,Is.Null);Assert.That(t.Changes,Is.Empty);
            Assert.That(s.GetInfluence(C("syria"),PlayerSide.USSR),Is.Zero);
        }
        [Test] public void Overflow_during_allocation_preserves_entire_input_and_stage()
        {
            var map=DefinitionTests.World();var d=new SetupDeploymentDefinition(map,new[]{
                new SetupDeploymentStage(PlayerSide.USSR,new[]{A("poland",int.MaxValue)},"region.eastern_europe",6),
                new SetupDeploymentStage(PlayerSide.US,new[]{A("uk",5)},"region.western_europe",7)});
            var s=InitialDeploymentFlow.Begin(Empty(map),d,"overflow").NextState;
            var t=Submit(s,PlayerSide.USSR,A("austria",1),A("poland",5));Assert.That(t.Error,Is.EqualTo(RuleError.NumericOverflow));
            Assert.That(t.NextState,Is.Null);Assert.That(t.Changes,Is.Empty);Assert.That(s.GetInfluence(C("austria"),PlayerSide.USSR),Is.Zero);Assert.That(s.Deployment.Owner,Is.EqualTo(PlayerSide.USSR));
        }
        [Test] public void Other_rule_paths_are_blocked_during_and_after_deployment()
        {
            var started=Started();var second=Submit(started,PlayerSide.USSR,A("poland",6)).NextState;var done=Submit(second,PlayerSide.US,A("canada",7)).NextState;
            foreach(var s in new[]{started,second,done})
            {
                Assert.That(InfluenceRules.PlanPlacement(s,s.Operations,new[]{C("poland")}).Error,Is.EqualTo(RuleError.SetupInProgress));
                var effect=new ClearThenControlEffect("card.fidel","effect.fidel",PlayerSide.USSR,C("cuba"));
                Assert.That(InfluenceEventRules.Plan(s,effect,null,InfluenceEventPolicy.NoActiveInterference).Error,Is.EqualTo(InfluenceEventError.SetupInProgress));
            }
        }
        [TestCase("profile")] [TestCase("version")] [TestCase("unknown-field")] [TestCase("unknown-country")]
        [TestCase("duplicate-country")] [TestCase("wrong-order")] [TestCase("wrong-region")]
        [TestCase("zero")] [TestCase("decimal")] [TestCase("overflow")] [TestCase("unverified")] [TestCase("unknown-source")]
        [TestCase("nonzero-default")] [TestCase("wrong-map")]
        public void Data_loader_rejects_invalid_deployment_subset(string kind)
        {
            var r=JObject.Parse(Source());var stage=r["deployment_stages"][0];
            switch(kind)
            {
                case "profile":r["dataset_id"]="unknown";break;case "version":r["content_version"]="9.0.0";break;
                case "unknown-field":r["extra_points"]=2;break;case "unknown-country":stage["fixed_influence"][0]["country_id"]="country.missing";break;
                case "duplicate-country":stage["fixed_influence"][1]["country_id"]=stage["fixed_influence"][0]["country_id"].DeepClone();break;
                case "wrong-order":stage["side"]="US";break;case "wrong-region":stage["free_allocation"]["region_id"]="region.africa";break;
                case "zero":stage["free_allocation"]["amount"]=0;break;case "decimal":stage["free_allocation"]["amount"]=6.5;break;
                case "overflow":stage["free_allocation"]["amount"]=long.MaxValue;break;
                case "unverified":stage["source_refs"][0]["verification"]="Pending";break;
                case "unknown-source":stage["source_refs"][0]["source_id"]="Unknown";break;
                case "nonzero-default":r["default_country_influence_per_side"]=1;break;case "wrong-map":r["map_dataset_id"]="map.other";break;
            }
            Assert.That(()=>SetupDeploymentLoader.LoadSubset(r.ToString(),Schema(),DefinitionTests.World()),Throws.InstanceOf<ArgumentException>());
        }
        [Test] public void Definition_copies_input_collections_and_rejects_ambiguous_stages()
        {
            var d=Definition();var a=d.Stages[0].FixedInfluence.ToArray();var stage=new SetupDeploymentStage(PlayerSide.USSR,a,"region.eastern_europe",6);a[0]=A("poland",99);
            Assert.That(stage.FixedInfluence.Any(x=>x.Amount==99),Is.False);Assert.Throws<NotSupportedException>(()=>((IList<InfluenceAllocation>)stage.FixedInfluence).Clear());
            var stages=new[]{stage,d.Stages[1]};var copy=new SetupDeploymentDefinition(d.Map,stages);stages[0]=null;Assert.That(copy.Stages[0],Is.SameAs(stage));
            Assert.Throws<NotSupportedException>(()=>((IList<SetupDeploymentStage>)copy.Stages).Clear());
            Assert.That(()=>new SetupDeploymentDefinition(d.Map,new[]{d.Stages[1],stage}),Throws.ArgumentException);
            Assert.That(()=>new SetupDeploymentStage(PlayerSide.USSR,new[]{A("poland",1),A("poland",1)},"region.eastern_europe",6),Throws.ArgumentException);
        }
    }
}
