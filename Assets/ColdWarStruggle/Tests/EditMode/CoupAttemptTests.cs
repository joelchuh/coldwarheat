using System;
using System.Collections.Generic;
using System.Linq;
using ColdWarStruggle.Core;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public sealed class CoupAttemptTests
    {
        static readonly CountryId Target=new CountryId("country.test");
        sealed class Calls { public int Forks; public int Draws; }
        sealed class FixedRandom : IRandomProvider
        {
            readonly int roll;readonly Calls calls;readonly string defect;ulong count;
            internal FixedRandom(int roll=4,Calls calls=null,string defect=null) { this.roll=roll;this.calls=calls??new Calls();this.defect=defect; }
            public RandomSnapshot Capture()=>new RandomSnapshot(RandomSnapshot.SupportedAlgorithm,1+count,defect=="stream" && count>0?3UL:1UL,count);
            public IRandomProvider Fork()
            {
                calls.Forks++;
                if(defect=="same")return this;if(defect=="null")return null;
                var copy=new FixedRandom(roll,calls,defect){count=count};if(defect=="mismatch")copy.count++;return copy;
            }
            public int NextInt(int min,int max)
            {
                calls.Draws++;if(defect!="noAdvance")count++;
                if(defect=="throw")throw new InvalidOperationException("Injected draw failure");
                return defect=="range"?max:roll-1;
            }
        }
        static void Same(RandomSnapshot a,RandomSnapshot b)
        { Assert.That(b.AlgorithmId,Is.EqualTo(a.AlgorithmId));Assert.That(b.State,Is.EqualTo(a.State));Assert.That(b.Increment,Is.EqualTo(a.Increment));Assert.That(b.DrawCount,Is.EqualTo(a.DrawCount)); }
        static CoupAttemptState State(IRandomProvider random=null,bool bg=true,int level=5,CountryState? influence=null,MilitaryOperationsState military=null,IEnumerable<string> effects=null)
        {
            var defcon=CoupCalculationTests.State(2,bg,level);
            return CoupAttemptState.Create("g",new CoupDefinition(1,6,2),defcon,military??MilitaryOperationsTests.State(),
                new Dictionary<CountryId,CountryState>{{Target,influence??new CountryState(2,10)}},(random??new FixedRandom()).Capture(),effects??Array.Empty<string>());
        }
        static CoupAttemptRequest Request(string id="a",string game="g",long revision=0,int turn=1,PlayerSide actor=PlayerSide.US,PlayerSide phasing=PlayerSide.USSR,int strength=3,int credit=3,string action="action",string rule="rule.coup",CountryId? target=null)
            =>new CoupAttemptRequest(id,game,action,rule,revision,turn,target??Target,actor,phasing,strength,credit);
        [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)] [TestCase(5)] [TestCase(6)]
        public void Full_plan_records_military_roll_influence_and_battleground_even_on_failure(int roll)
        {
            foreach(var actor in new[]{PlayerSide.US,PlayerSide.USSR})
            {
                var random=new FixedRandom(roll);var state=State(random,influence:new CountryState(2,2));var request=Request(actor:actor,phasing:actor==PlayerSide.US?PlayerSide.USSR:PlayerSide.US);
                var plan=CoupAttemptRules.PlanAttempt(state,request,random);var receipt=plan.Receipt;
                Assert.That(plan.Before,Is.SameAs(state));Assert.That(plan.AlreadyApplied,Is.False);Assert.That(plan.After.Revision,Is.EqualTo(1));
                Assert.That(receipt.Request,Is.SameAs(request));Assert.That(receipt.Calculation.Roll,Is.EqualTo(roll));
                Assert.That(receipt.MilitaryCredit.Beneficiary,Is.EqualTo(actor));Assert.That(receipt.MilitaryCredit.Amount,Is.EqualTo(3));
                Assert.That(receipt.MilitaryCredit.Id,Is.EqualTo("coup/a"));Assert.That(receipt.MilitaryCredit.Kind,Is.EqualTo(MilitaryCreditKind.OperationsCoup));
                Assert.That(actor==PlayerSide.US?plan.After.Military.UsEarned:plan.After.Military.UssrEarned,Is.EqualTo(3));
                Assert.That(plan.After.Defcon.Level,Is.EqualTo(4));Assert.That(receipt.DefconChange.PhasingPlayer,Is.EqualTo(request.PhasingPlayer));
                Assert.That(plan.After.Countries[Target].US,Is.EqualTo(receipt.Calculation.After.US));Assert.That(plan.After.Countries[Target].USSR,Is.EqualTo(receipt.Calculation.After.USSR));
                Assert.That(plan.After.Receipts["a"],Is.SameAs(receipt));Same(receipt.RandomAfter,plan.After.Random);Same(state.Random,random.Capture());
                Assert.That(state.Military.UsEarned,Is.Zero);Assert.That(state.Military.UssrEarned,Is.Zero);Assert.That(state.Defcon.Level,Is.EqualTo(5));Assert.That(state.Receipts.Count,Is.Zero);
            }
        }
        [Test]
        public void Nonbattleground_preserves_track_and_credit_is_separate_from_strength()
        {
            var random=new FixedRandom();var state=State(random,bg:false);
            var p=CoupAttemptRules.PlanAttempt(state,Request(strength:4,credit:2),random);
            Assert.That(p.After.Defcon,Is.SameAs(state.Defcon));Assert.That(p.Receipt.DefconChange,Is.Null);Assert.That(p.Receipt.Calculation.Strength,Is.EqualTo(4));Assert.That(p.After.Military.UsEarned,Is.EqualTo(2));
        }
        [Test]
        public void Failed_battleground_at_two_still_goes_nuclear_and_replay_does_not_reenter_gate()
        {
            var random=new FixedRandom(1);var state=State(random,level:2);var request=Request(strength:1,credit:1);
            var p=CoupAttemptRules.PlanAttempt(state,request,random);Assert.That(p.Receipt.Calculation.Removed,Is.Zero);Assert.That(p.Receipt.DefconChange.NuclearWar,Is.True);Assert.That(p.Receipt.DefconChange.PhasingPlayer,Is.EqualTo(PlayerSide.USSR));
            var calls=new Calls();var poison=new FixedRandom(calls:calls,defect:"throw");
            var replay=CoupAttemptRules.PlanAttempt(p.After,request,poison);Assert.That(replay.AlreadyApplied,Is.True);Assert.That(replay.After,Is.SameAs(p.After));Assert.That(replay.Receipt,Is.SameAs(p.Receipt));Assert.That(calls.Forks,Is.Zero);Assert.That(calls.Draws,Is.Zero);
            Assert.Throws<InvalidOperationException>(()=>CoupAttemptRules.PlanAttempt(p.After,Request("new",revision:1),new Pcg32Random(p.After.Random)));
        }
        [Test]
        public void Late_replay_preserves_current_country_military_revision_and_rng()
        {
            var random=Pcg32Random.FromSeed(42,54);var state=State(random,bg:false);var request=Request();
            var first=CoupAttemptRules.PlanAttempt(state,request,random);
            var second=CoupAttemptRules.PlanAttempt(first.After,Request("b",revision:1),new Pcg32Random(first.After.Random));
            var repeat=CoupAttemptRules.PlanAttempt(second.After,request,new FixedRandom());
            Assert.That(repeat.AlreadyApplied,Is.True);Assert.That(repeat.After,Is.SameAs(second.After));Assert.That(repeat.Receipt,Is.SameAs(first.Receipt));Same(repeat.After.Random,second.After.Random);
            Assert.That(repeat.After.Military.UsEarned,Is.EqualTo(6));Assert.That(repeat.After.Revision,Is.EqualTo(2));
            var again=CoupAttemptRules.PlanAttempt(state,request,Pcg32Random.FromSeed(42,54));Same(first.After.Random,again.After.Random);Assert.That(again.Receipt.Calculation.Roll,Is.EqualTo(first.Receipt.Calculation.Roll));
        }
        [TestCase("actor")] [TestCase("phasing")] [TestCase("strength")] [TestCase("credit")] [TestCase("action")] [TestCase("rule")] [TestCase("target")] [TestCase("revision")]
        public void Duplicate_id_with_changed_facts_is_rejected_without_rng(string defect)
        {
            var state=State();var after=CoupAttemptRules.PlanAttempt(state,Request(),new FixedRandom()).After;
            var r=Request(actor:defect=="actor"?PlayerSide.USSR:PlayerSide.US,phasing:defect=="phasing"?PlayerSide.US:PlayerSide.USSR,
                strength:defect=="strength"?4:3,credit:defect=="credit"?4:3,action:defect=="action"?"other":"action",rule:defect=="rule"?"other":"rule.coup",
                target:defect=="target"?new CountryId("other"):Target,revision:defect=="revision"?1:0);
            var calls=new Calls();Assert.Throws<ArgumentException>(()=>CoupAttemptRules.PlanAttempt(after,r,new FixedRandom(calls:calls)));Assert.That(calls.Forks,Is.Zero);
        }
        [TestCase("game")] [TestCase("turn")] [TestCase("stale")] [TestCase("target")] [TestCase("enemy")] [TestCase("effect")] [TestCase("assessed")] [TestCase("orphan")] [TestCase("militaryOverflow")]
        public void Invalid_new_attempts_fail_before_random_is_touched(string defect)
        {
            var calls=new Calls();var random=new FixedRandom(calls:calls);var military=MilitaryOperationsTests.State();
            if(defect=="assessed")military=MilitaryOperationsService.AssessTurnEnd(military,"end",CoupCalculationTests.State()).After;
            if(defect=="orphan")military=MilitaryOperationsService.PlanCredit(military,MilitaryOperationsTests.Credit("coup/a",3)).After;
            if(defect=="militaryOverflow")military=MilitaryOperationsTests.Earned(int.MaxValue,0);
            var state=State(random,military:military,influence:defect=="enemy"?new CountryState(2,0):(CountryState?)null,effects:defect=="effect"?new[]{"unknown"}:null);
            var request=Request(game:defect=="game"?"other":"g",turn:defect=="turn"?2:1,revision:defect=="stale"?1:0,target:defect=="target"?new CountryId("unknown"):Target);
            Assert.That(()=>CoupAttemptRules.PlanAttempt(state,request,random),Throws.Exception);Assert.That(calls.Forks,Is.Zero);Assert.That(calls.Draws,Is.Zero);Assert.That(state.Receipts.Count,Is.Zero);
        }
        [TestCase("same")] [TestCase("null")] [TestCase("mismatch")] [TestCase("noAdvance")] [TestCase("throw")] [TestCase("range")] [TestCase("stream")]
        public void Broken_random_dependencies_leave_input_state_and_stream_unchanged(string defect)
        {
            var random=new FixedRandom(defect:defect);var state=State(random);var captured=random.Capture();
            Assert.Throws<InvalidOperationException>(()=>CoupAttemptRules.PlanAttempt(state,Request(),random));Same(captured,random.Capture());Assert.That(state.Military.UsEarned,Is.Zero);Assert.That(state.Defcon.Level,Is.EqualTo(5));Assert.That(state.Revision,Is.Zero);
        }
        [Test]
        public void Wrong_external_random_snapshot_is_rejected_before_fork()
        {
            var state=State(Pcg32Random.FromSeed(1,2));var calls=new Calls();Assert.Throws<InvalidOperationException>(()=>CoupAttemptRules.PlanAttempt(state,Request(),new FixedRandom(calls:calls)));Assert.That(calls.Forks,Is.Zero);
        }
        [Test]
        public void Postroll_influence_overflow_does_not_publish_military_rng_or_defcon()
        {
            var random=new FixedRandom(6);var state=State(random,influence:new CountryState(int.MaxValue,1));
            Assert.Throws<OverflowException>(()=>CoupAttemptRules.PlanAttempt(state,Request(strength:4),random));Same(state.Random,random.Capture());Assert.That(state.Military.UsEarned,Is.Zero);Assert.That(state.Receipts.Count,Is.Zero);Assert.That(state.Defcon.Level,Is.EqualTo(5));
        }
        [Test]
        public void State_copies_complete_map_and_effect_context_to_readonly_collections()
        {
            var countries=new Dictionary<CountryId,CountryState>{{Target,new CountryState(0,2)}};var effects=new[]{"known"};
            var s=CoupAttemptState.Create("g",new CoupDefinition(1,6,2),CoupCalculationTests.State(),MilitaryOperationsTests.State(),countries,new FixedRandom().Capture(),effects);
            countries.Clear();effects[0]="changed";Assert.That(s.Countries[Target].USSR,Is.EqualTo(2));Assert.That(s.ActiveEffectIds[0],Is.EqualTo("known"));
            Assert.Throws<NotSupportedException>(()=>((IDictionary<CountryId,CountryState>)s.Countries).Clear());Assert.Throws<NotSupportedException>(()=>((IList<string>)s.ActiveEffectIds).Clear());Assert.Throws<NotSupportedException>(()=>((IDictionary<string,CoupAttemptReceipt>)s.Receipts).Clear());
        }
        [Test]
        public void Invalid_state_context_is_rejected()
        {
            var d=new CoupDefinition(1,6,2);var dc=CoupCalculationTests.State();var m=MilitaryOperationsTests.State();var rng=new FixedRandom().Capture();var c=new Dictionary<CountryId,CountryState>{{Target,new CountryState(0,2)}};
            Assert.Throws<ArgumentNullException>(()=>CoupAttemptState.Create("g",null,dc,m,c,rng,Array.Empty<string>()));
            Assert.Throws<ArgumentNullException>(()=>CoupAttemptState.Create("g",d,dc,m,c,rng,null));
            Assert.Throws<ArgumentException>(()=>CoupAttemptState.Create(" g ",d,dc,m,c,rng,Array.Empty<string>()));
            Assert.Throws<ArgumentException>(()=>CoupAttemptState.Create("g",d,dc,m,new Dictionary<CountryId,CountryState>(),rng,Array.Empty<string>()));
            Assert.Throws<ArgumentException>(()=>CoupAttemptState.Create("g",d,dc,m,new Dictionary<CountryId,CountryState>{{new CountryId("other"),default}},rng,Array.Empty<string>()));
            Assert.Throws<ArgumentException>(()=>CoupAttemptState.Create("g",d,dc,m,c,rng,new[]{"a","a"}));
            Assert.Throws<ArgumentException>(()=>CoupAttemptState.Create("g",d,dc,m,c,rng,Enumerable.Range(0,129).Select(i=>"e"+i)));
        }
        [TestCase("id")] [TestCase("game")] [TestCase("action")] [TestCase("rule")] [TestCase("revision")] [TestCase("turn")] [TestCase("actor")] [TestCase("phasing")] [TestCase("strength")] [TestCase("credit")] [TestCase("country")]
        public void Invalid_request_facts_rejected(string defect)
        {
            Assert.That(()=>Request(id:defect=="id"?new string('x',129):"a",game:defect=="game"?" ":"g",action:defect=="action"?" action":"action",rule:defect=="rule"?"":"rule.coup",
                revision:defect=="revision"?-1:0,turn:defect=="turn"?0:1,actor:defect=="actor"?(PlayerSide)9:PlayerSide.US,phasing:defect=="phasing"?(PlayerSide)9:PlayerSide.USSR,
                strength:defect=="strength"?0:3,credit:defect=="credit"?0:3,target:defect=="country"?default(CountryId):Target),Throws.InstanceOf<ArgumentException>());
        }
        [Test]
        public void Null_plan_inputs_rejected()
        {
            Assert.Throws<ArgumentNullException>(()=>CoupAttemptRules.PlanAttempt(null,Request(),new FixedRandom()));
            Assert.Throws<ArgumentNullException>(()=>CoupAttemptRules.PlanAttempt(State(),null,new FixedRandom()));
            Assert.Throws<ArgumentNullException>(()=>CoupAttemptRules.PlanAttempt(State(),Request(),null));
        }
        [Test]
        public void Receipt_limit_rejects_new_but_retains_safe_replays()
        {
            var random=Pcg32Random.FromSeed(42,54);var state=State(random,bg:false,influence:new CountryState(0,int.MaxValue));var initial=Request("0",revision:0,strength:1,credit:1);
            for(int i=0;i<4096;i++)state=CoupAttemptRules.PlanAttempt(state,i==0?initial:Request(i.ToString(),revision:i,strength:1,credit:1),new Pcg32Random(state.Random)).After;
            var calls=new Calls();Assert.Throws<InvalidOperationException>(()=>CoupAttemptRules.PlanAttempt(state,Request("limit",revision:4096),new FixedRandom(calls:calls)));Assert.That(calls.Forks,Is.Zero);
            Assert.That(CoupAttemptRules.PlanAttempt(state,initial,new FixedRandom()).After,Is.SameAs(state));
        }
    }
}
