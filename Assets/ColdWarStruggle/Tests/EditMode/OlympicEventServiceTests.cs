using System;
using ColdWarStruggle.Core;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public sealed class OlympicEventServiceTests
    {
        sealed class Calls { public int Forks;public int Draws; }
        sealed class ScriptRandom : IRandomProvider
        {
            readonly int[] rolls;readonly Calls calls;readonly string fault;readonly ulong badAt;ulong count;
            internal ScriptRandom(int[] rolls,Calls calls=null,string fault=null,ulong cursor=0,ulong badAt=2)
            {this.rolls=(int[])rolls.Clone();this.calls=calls??new Calls();this.fault=fault;count=cursor;this.badAt=badAt;}
            public RandomSnapshot Capture()=>fault=="nullCapture"&&count==badAt?null:new RandomSnapshot(RandomSnapshot.SupportedAlgorithm,1+count,fault=="stream"&&count==badAt?3UL:1UL,count);
            public IRandomProvider Fork()
            {
                calls.Forks++;if(fault=="sameFork")return this;if(fault=="nullFork")return null;
                return new ScriptRandom(rolls,calls,fault,fault=="wrongFork"?count+1:count,badAt);
            }
            public int NextInt(int min,int max)
            {
                Assert.That(min,Is.Zero);Assert.That(max,Is.EqualTo(6));calls.Draws++;var next=count+1;var raw=rolls[(int)count];
                if(!(fault=="noAdvance"&&next==badAt))count=next;
                if(fault=="throw"&&next==badAt)throw new InvalidOperationException("Injected pair failure.");
                return fault=="range"&&next==badAt?max:raw-1;
            }
        }
        static OlympicEventState State(IRandomProvider random,int level=5,int vp=0,VictoryDefinition victory=null)
        {
            var risk=PlayCoupTests.Prepared().Practice.Definition.Risk;
            return new OlympicEventState(OlympicDefinitionTests.Load(),new DefconState(risk.Defcon,level),victory??risk.Victory,vp,random.Capture());
        }
        static OlympicPairPlan Pair(OlympicEventState state,IRandomProvider random,PlayerSide sponsor=PlayerSide.US,string id="pair",OlympicInterferencePolicy policy=OlympicInterferencePolicy.NoActiveInterference)
            =>OlympicEventService.PlanPair(state,sponsor,id,policy,random);
        static OlympicBoycottPlan Boycott(OlympicEventState state,PlayerSide sponsor=PlayerSide.US,PlayerSide root=PlayerSide.US,string id="boycott",OlympicInterferencePolicy policy=OlympicInterferencePolicy.NoActiveInterference)
            =>OlympicEventService.PlanBoycott(state,sponsor,root,id,policy);
        [TestCase(PlayerSide.US,6,6,PlayerSide.US,2)][TestCase(PlayerSide.US,1,6,PlayerSide.USSR,-2)]
        [TestCase(PlayerSide.USSR,6,1,PlayerSide.US,2)][TestCase(PlayerSide.USSR,6,6,PlayerSide.USSR,-2)]
        public void Both_dice_and_the_actual_winners_VP_commit_as_one_pure_plan(PlayerSide sponsor,int us,int su,PlayerSide winner,int vp)
        {
            var calls=new Calls();var random=new ScriptRandom(new[]{us,su},calls);var s=State(random);var p=Pair(s,random,sponsor);
            Assert.That(p.Before,Is.SameAs(s));Assert.That(p.Calculation.UsRoll,Is.EqualTo(us));Assert.That(p.Calculation.UssrRoll,Is.EqualTo(su));
            Assert.That(p.Calculation.Winner,Is.EqualTo(winner));Assert.That(p.Calculation.AwardVp,Is.EqualTo(2));Assert.That(p.After.VpForUS,Is.EqualTo(vp));Assert.That(p.Decision.Outcome,Is.EqualTo(VictoryOutcome.Continue));
            Assert.That(p.After.Defcon,Is.SameAs(s.Defcon));Assert.That(p.After.Definition,Is.SameAs(s.Definition));Assert.That(p.After.Victory,Is.SameAs(s.Victory));Assert.That(p.RequiresAnotherPair,Is.False);
            Assert.That(p.After.Random.DrawCount,Is.EqualTo(2));Assert.That(calls.Forks,Is.EqualTo(1));Assert.That(calls.Draws,Is.EqualTo(2));Pcg32RandomTests.Same(s.Random,random.Capture());Assert.That(s.VpForUS,Is.Zero);
        }
        [TestCase(PlayerSide.US,19,6,1,21,PlayerSide.US)][TestCase(PlayerSide.USSR,-19,1,6,-21,PlayerSide.USSR)]
        [TestCase(PlayerSide.US,-19,1,6,-21,PlayerSide.USSR)][TestCase(PlayerSide.USSR,19,6,1,21,PlayerSide.US)]
        public void Either_competitor_can_trigger_automatic_victory(PlayerSide sponsor,int before,int us,int su,int after,PlayerSide winner)
        {
            var random=new ScriptRandom(new[]{us,su});var p=Pair(State(random,vp:before),random,sponsor);
            Assert.That(p.Decision.Outcome,Is.EqualTo(VictoryOutcome.Finished));Assert.That(p.Decision.Winner,Is.EqualTo(winner));Assert.That(p.After.VpForUS,Is.EqualTo(after));Assert.That(p.After.Random.DrawCount,Is.EqualTo(2));
        }
        [Test] public void Ties_yield_one_complete_pair_and_continue_from_committed_cursor_without_a_rule_cap()
        {
            var rolls=new[]{1,3,2,4,4,5};var r=new ScriptRandom(rolls);var state=State(r,vp:3);
            for(int i=0;i<3;i++)
            {
                var p=Pair(state,new ScriptRandom(rolls,cursor:state.Random.DrawCount),id:"pair-"+i);
                Assert.That(p.RequiresAnotherPair,Is.EqualTo(i<2));Assert.That(p.After.VpForUS,Is.EqualTo(i<2?3:5));Assert.That(p.After.Random.DrawCount,Is.EqualTo((i+1)*2));state=p.After;
            }
            Assert.That(r.Capture().DrawCount,Is.Zero);
        }
        [TestCase("sameFork")][TestCase("nullFork")][TestCase("wrongFork")]
        [TestCase("throw")][TestCase("range")][TestCase("noAdvance")][TestCase("stream")][TestCase("nullCapture")]
        public void Bad_second_sample_or_fork_cannot_publish_half_a_pair(string fault)
        {
            var calls=new Calls();var random=new ScriptRandom(new[]{6,1},calls,fault);var s=State(random);
            Assert.That(()=>Pair(s,random),Throws.InstanceOf<InvalidOperationException>());
            Assert.That(s.VpForUS,Is.Zero);Assert.That(s.Defcon.Level,Is.EqualTo(5));Pcg32RandomTests.Same(s.Random,random.Capture());
            Assert.That(calls.Draws,Is.EqualTo(fault.EndsWith("Fork")?0:2));
        }
        [Test] public void First_sample_cursor_must_advance_even_if_second_would_advance()
        {
            var calls=new Calls();var random=new ScriptRandom(new[]{6,1},calls,"noAdvance",badAt:1);var s=State(random);
            Assert.That(()=>Pair(s,random),Throws.InstanceOf<InvalidOperationException>());Assert.That(calls.Draws,Is.EqualTo(1));Pcg32RandomTests.Same(s.Random,random.Capture());
        }
        [Test] public void Later_pair_failure_keeps_earlier_tie_and_its_cursor()
        {
            var rolls=new[]{1,3,6,1};var initial=new ScriptRandom(rolls);var first=Pair(State(initial),initial);
            var later=new ScriptRandom(rolls,fault:"throw",cursor:2,badAt:4);
            Assert.That(()=>Pair(first.After,later,id:"next"),Throws.InstanceOf<InvalidOperationException>());
            Assert.That(first.RequiresAnotherPair,Is.True);Assert.That(first.After.Random.DrawCount,Is.EqualTo(2));Assert.That(first.After.VpForUS,Is.Zero);Pcg32RandomTests.Same(first.After.Random,later.Capture());
        }
        [TestCase("sponsor")][TestCase("id")][TestCase("policy")][TestCase("nuclear")][TestCase("vp")][TestCase("random")]
        public void Invalid_or_terminal_context_fails_before_random_fork(string fault)
        {
            var calls=new Calls();var random=new ScriptRandom(new[]{6,1},calls);var s=State(random,fault=="nuclear"?1:5,fault=="vp"?20:0);
            var supplied=fault=="random"?(IRandomProvider)Pcg32Random.FromSeed(42,54):random;
            Assert.That(()=>Pair(s,supplied,fault=="sponsor"?(PlayerSide)99:PlayerSide.US,fault=="id"?" ":"pair",fault=="policy"?(OlympicInterferencePolicy)99:OlympicInterferencePolicy.NoActiveInterference),fault=="random"?Throws.InstanceOf<InvalidOperationException>():Throws.InstanceOf<ArgumentException>());
            Assert.That(calls.Draws,Is.Zero);Assert.That(calls.Forks,Is.Zero);Pcg32RandomTests.Same(s.Random,random.Capture());
        }
        [Test] public void Potential_winner_overflow_is_preflighted_even_when_actual_pair_would_tie()
        {
            var calls=new Calls();var random=new ScriptRandom(new[]{1,3},calls);var normal=State(random);var d=new VictoryDefinition(0,int.MaxValue,normal.Victory.HeldScoringRows);
            var s=State(random,vp:int.MaxValue-1,victory:d);Assert.Throws<OverflowException>(()=>Pair(s,random));Assert.That(calls.Forks,Is.Zero);Assert.That(calls.Draws,Is.Zero);
        }
        [Test] public void Real_PCG_reconstruction_repeats_pair_and_keeps_original_stream()
        {
            var random=Pcg32Random.FromSeed(42,54);var s=State(random);var a=Pair(s,random);var b=Pair(s,new Pcg32Random(s.Random));
            Assert.That(a.Calculation.UsRoll,Is.EqualTo(b.Calculation.UsRoll));Assert.That(a.Calculation.UssrRoll,Is.EqualTo(b.Calculation.UssrRoll));Assert.That(a.After.VpForUS,Is.EqualTo(b.After.VpForUS));Pcg32RandomTests.Same(a.After.Random,b.After.Random);Pcg32RandomTests.Same(s.Random,random.Capture());
        }
        [TestCase(PlayerSide.US,PlayerSide.US)][TestCase(PlayerSide.US,PlayerSide.USSR)]
        [TestCase(PlayerSide.USSR,PlayerSide.US)][TestCase(PlayerSide.USSR,PlayerSide.USSR)]
        public void Boycott_nuclear_loss_belongs_to_root_and_offers_no_operations(PlayerSide sponsor,PlayerSide root)
        {
            var random=Pcg32Random.FromSeed(42,54);var s=State(random,2,3);var p=Boycott(s,sponsor,root);
            Assert.That(p.Change.After.Level,Is.EqualTo(1));Assert.That(p.Change.PhasingPlayer,Is.EqualTo(root));Assert.That(p.Decision.Outcome,Is.EqualTo(VictoryOutcome.Finished));Assert.That(p.Decision.Winner,Is.EqualTo(root==PlayerSide.US?PlayerSide.USSR:PlayerSide.US));
            Assert.That(p.Operations,Is.Null);Assert.That(p.After.VpForUS,Is.EqualTo(3));Assert.That(p.After.Defcon,Is.SameAs(p.Change.After));Assert.That(p.After.Random,Is.SameAs(s.Random));Assert.That(s.Defcon.Level,Is.EqualTo(2));Pcg32RandomTests.Same(s.Random,random.Capture());
        }
        [TestCase(PlayerSide.US,3)][TestCase(PlayerSide.US,4)][TestCase(PlayerSide.US,5)]
        [TestCase(PlayerSide.USSR,3)][TestCase(PlayerSide.USSR,4)][TestCase(PlayerSide.USSR,5)]
        public void Surviving_boycott_lowers_DEFCON_then_proposes_sponsors_event_budget(PlayerSide sponsor,int level)
        {
            var random=Pcg32Random.FromSeed(42,54);var s=State(random,level,-4);var p=Boycott(s,sponsor);
            Assert.That(p.Before,Is.SameAs(s));Assert.That(p.After.Defcon.Level,Is.EqualTo(level-1));Assert.That(p.Decision.Outcome,Is.EqualTo(VictoryOutcome.Continue));Assert.That(p.Operations.Actor,Is.EqualTo(sponsor));Assert.That(p.Operations.BaseOps,Is.EqualTo(4));Assert.That(p.Operations.BaseOps,Is.Not.EqualTo(s.Definition.Card.PrintedOps));Assert.That(p.After.VpForUS,Is.EqualTo(-4));Assert.That(p.After.Random,Is.SameAs(s.Random));
        }
        [TestCase("root")][TestCase("sponsor")][TestCase("id")][TestCase("policy")][TestCase("nuclear")][TestCase("vp")]
        public void Invalid_boycott_context_has_no_transition(string fault)
        {
            var random=Pcg32Random.FromSeed(42,54);var s=State(random,fault=="nuclear"?1:5,fault=="vp"?-20:0);
            Assert.That(()=>Boycott(s,fault=="sponsor"?(PlayerSide)99:PlayerSide.US,fault=="root"?(PlayerSide)99:PlayerSide.US,fault=="id"?" ":"b",fault=="policy"?(OlympicInterferencePolicy)99:OlympicInterferencePolicy.NoActiveInterference),Throws.InstanceOf<ArgumentException>());Pcg32RandomTests.Same(s.Random,random.Capture());
        }
        [Test] public void Missing_state_inputs_are_rejected()
        {
            var r=Pcg32Random.FromSeed(42,54);var s=State(r);
            Assert.Throws<ArgumentNullException>(()=>Pair(null,r));Assert.Throws<ArgumentNullException>(()=>Pair(s,null));Assert.Throws<ArgumentNullException>(()=>Boycott(null));
            Assert.Throws<ArgumentNullException>(()=>new OlympicEventState(null,s.Defcon,s.Victory,0,s.Random));Assert.Throws<ArgumentNullException>(()=>new OlympicEventState(s.Definition,null,s.Victory,0,s.Random));
            Assert.Throws<ArgumentNullException>(()=>new OlympicEventState(s.Definition,s.Defcon,null,0,s.Random));Assert.Throws<ArgumentNullException>(()=>new OlympicEventState(s.Definition,s.Defcon,s.Victory,0,null));
        }
    }
}
