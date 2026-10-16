using System;
using System.Collections.Generic;
using System.Linq;
using ColdWarStruggle.Core;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class WarEventServiceTests
    {
        static CountryId C(string n)=>WarCalculationTests.C(n);
        static CardId Id(string n)=>WarCalculationTests.Id(n);
        sealed class Calls { public int Forks; public int Draws; }
        sealed class FixedRandom : IRandomProvider
        {
            readonly int roll;readonly Calls calls;readonly string defect;ulong count;
            internal FixedRandom(int roll=6,Calls calls=null,string defect=null){this.roll=roll;this.calls=calls??new Calls();this.defect=defect;}
            public RandomSnapshot Capture()=>new RandomSnapshot(RandomSnapshot.SupportedAlgorithm,1+count,defect=="stream" && count>0?3UL:1UL,count);
            public IRandomProvider Fork()
            {
                calls.Forks++;if(defect=="same")return this;if(defect=="null")return null;
                var next=new FixedRandom(roll,calls,defect){count=count};if(defect=="mismatch")next.count++;return next;
            }
            public int NextInt(int min,int max)
            {
                Assert.That(min,Is.Zero);Assert.That(max,Is.EqualTo(6));calls.Draws++;if(defect!="noAdvance")count++;
                if(defect=="throw")throw new InvalidOperationException("Injected war die failure.");return defect=="range"?max:roll-1;
            }
        }
        static WarEventState State(IRandomProvider random,int level=5,int vp=0)
        {
            var r=PlayCoupTests.Prepared().Practice.Definition.Risk;var catalog=EarlyWarDefinitionTests.Fixture(r.Defcon.Map);var board=RegionScoringTests.Empty(catalog.Map);
            foreach(var n in new[]{"south_korea","israel","india","pakistan"})board[C(n)]=new CountryState(3,2);
            return new WarEventState(catalog,board,MilitaryOperationsTests.State(),new DefconState(r.Defcon,level),r.Victory,vp,random.Capture());
        }
        static WarEventState Copy(WarEventState s,IReadOnlyDictionary<CountryId,CountryState> board=null,MilitaryOperationsState military=null,VictoryDefinition victory=null,int? vp=null)
            =>new WarEventState(s.Catalog,board??s.Countries,military??s.Military,s.Defcon,victory??s.Victory,vp??s.VpForUS,s.Random);
        static WarEventPlan Plan(WarEventState s,IRandomProvider random,string card="korean_war",string target="south_korea",PlayerSide controller=PlayerSide.US,string id="attempt",WarInterferencePolicy policy=WarInterferencePolicy.NoActiveInterference)
            =>WarEventService.Plan(s,Id(card),controller,C(target),id,policy,random);

        [TestCase("korean_war","south_korea",PlayerSide.US,6)][TestCase("korean_war","south_korea",PlayerSide.USSR,1)]
        [TestCase("arab_israeli_war","israel",PlayerSide.US,6)][TestCase("arab_israeli_war","israel",PlayerSide.USSR,1)]
        [TestCase("indo_pakistani_war","india",PlayerSide.US,6)][TestCase("indo_pakistani_war","pakistan",PlayerSide.USSR,6)]
        public void Single_fork_commits_fixed_military_and_success_capture_VP_as_one_result(string card,string target,PlayerSide controller,int roll)
        {
            var calls=new Calls();var random=new FixedRandom(roll,calls);var s=State(random);var r=Plan(s,random,card,target,controller);var owner=card=="indo_pakistani_war"?controller:PlayerSide.USSR;
            Assert.That(r.Before,Is.SameAs(s));Assert.That(r.Blocked,Is.False);Assert.That(r.Calculation.Roll,Is.EqualTo(roll));Assert.That(r.Credit.Kind,Is.EqualTo(MilitaryCreditKind.WarEvent));Assert.That(r.Credit.Basis,Is.EqualTo(MilitaryCreditBasis.VerifiedCardParameter));Assert.That(r.Credit.Amount,Is.EqualTo(2));Assert.That(r.Credit.Beneficiary,Is.EqualTo(owner));
            Assert.That(r.After.Military.UsEarned,Is.EqualTo(owner==PlayerSide.US?2:0));Assert.That(r.After.Military.UssrEarned,Is.EqualTo(owner==PlayerSide.USSR?2:0));Assert.That(r.After.VpForUS,Is.EqualTo(roll<4?0:owner==PlayerSide.US?2:-2));
            Assert.That(r.After.Countries[C(target)].US,Is.EqualTo(r.Calculation.After.US));Assert.That(r.After.Countries[C(target)].USSR,Is.EqualTo(r.Calculation.After.USSR));Assert.That(r.After.Defcon,Is.SameAs(s.Defcon));
            Assert.That(calls.Forks,Is.EqualTo(1));Assert.That(calls.Draws,Is.EqualTo(1));Assert.That(r.After.Random.DrawCount,Is.EqualTo(s.Random.DrawCount+1));Pcg32RandomTests.Same(s.Random,random.Capture());Assert.That(s.Military.AppliedCredits,Is.Empty);Assert.That(s.Countries[C(target)].US,Is.EqualTo(3));
        }
        [Test] public void Zero_influence_war_at_DEFCON_two_still_awards_and_does_not_degrade()
        {
            var random=new FixedRandom();var s=State(random,2);s=Copy(s,RegionScoringTests.Empty(s.Catalog.Map));var r=Plan(s,random);
            Assert.That(r.Calculation.Captured,Is.Zero);Assert.That(r.Calculation.Success,Is.True);Assert.That(r.After.VpForUS,Is.EqualTo(-2));Assert.That(r.After.Military.UssrEarned,Is.EqualTo(2));Assert.That(r.After.Defcon.Level,Is.EqualTo(2));
        }
        [TestCase(PlayerSide.US,19,21)][TestCase(PlayerSide.USSR,-19,-21)]
        public void Automatic_victory_includes_whole_country_and_military_result(PlayerSide side,int before,int after)
        {
            var random=new FixedRandom();var s=State(random,vp:before);var r=Plan(s,random,"indo_pakistani_war","india",side);
            Assert.That(r.Decision.Outcome,Is.EqualTo(VictoryOutcome.Finished));Assert.That(r.Decision.Winner,Is.EqualTo(side));Assert.That(r.After.VpForUS,Is.EqualTo(after));Assert.That(r.After.Countries[C("india")].For(side),Is.EqualTo(5));Assert.That(r.After.Military.AppliedCredits.Count,Is.EqualTo(1));
        }
        [Test] public void Military_earned_is_not_clamped_to_printed_track()
        {
            var random=new FixedRandom(1);var s=Copy(State(random),military:MilitaryOperationsTests.Earned(0,5));var r=Plan(s,random);Assert.That(r.After.Military.UssrEarned,Is.EqualTo(7));Assert.That(r.After.VpForUS,Is.Zero);
        }
        [Test] public void Verified_Camp_David_blocks_only_Arab_Israeli_with_no_fork_or_rewards()
        {
            var calls=new Calls();var random=new FixedRandom(calls:calls,defect:"throw");var s=State(random);var r=Plan(s,random,"arab_israeli_war","israel",policy:WarInterferencePolicy.CampDavidPreventsArabIsraeli);
            Assert.That(r.Blocked,Is.True);Assert.That(r.After,Is.SameAs(s));Assert.That(r.Calculation,Is.Null);Assert.That(r.Credit,Is.Null);Assert.That(r.Decision.Outcome,Is.EqualTo(VictoryOutcome.Continue));Assert.That(calls.Forks,Is.Zero);Assert.That(calls.Draws,Is.Zero);
            Assert.That(()=>Plan(s,random,policy:WarInterferencePolicy.CampDavidPreventsArabIsraeli),Throws.InstanceOf<ArgumentException>());Assert.That(calls.Forks,Is.Zero);
        }
        [TestCase("target")][TestCase("card")][TestCase("controller")][TestCase("policy")][TestCase("id")][TestCase("terminal-vp")][TestCase("nuclear")]
        public void Invalid_or_terminal_context_is_rejected_before_fork(string defect)
        {
            var calls=new Calls();var random=new FixedRandom(calls:calls);var s=State(random,defect=="nuclear"?1:5,defect=="terminal-vp"?20:0);
            Assert.That(()=>Plan(s,random,defect=="card"?"brush_war":"korean_war",defect=="target"?"france":"south_korea",defect=="controller"?(PlayerSide)99:PlayerSide.US,defect=="id"?" ":"attempt",defect=="policy"?(WarInterferencePolicy)99:WarInterferencePolicy.NoActiveInterference),Throws.InstanceOf<ArgumentException>());
            Assert.That(calls.Forks,Is.Zero);Assert.That(calls.Draws,Is.Zero);Pcg32RandomTests.Same(s.Random,random.Capture());
        }
        [TestCase("military")][TestCase("capture")][TestCase("vp")]
        public void Overflow_is_rejected_before_random_even_when_actual_roll_would_fail(string defect)
        {
            var calls=new Calls();var random=new FixedRandom(1,calls);var s=State(random);
            if(defect=="military")s=Copy(s,military:MilitaryOperationsTests.Earned(0,int.MaxValue));
            if(defect=="capture"){var board=s.Countries.ToDictionary(x=>x.Key,x=>x.Value);board[C("south_korea")]=new CountryState(1,int.MaxValue);s=Copy(s,board);}
            if(defect=="vp")s=Copy(s,victory:new VictoryDefinition(0,int.MaxValue,s.Victory.HeldScoringRows),vp:int.MaxValue-1);
            Assert.That(()=>Plan(s,random,defect=="vp"?"indo_pakistani_war":"korean_war",defect=="vp"?"india":"south_korea"),Throws.TypeOf<OverflowException>());Assert.That(calls.Forks,Is.Zero);Assert.That(calls.Draws,Is.Zero);
        }
        [TestCase("same")][TestCase("null")][TestCase("mismatch")][TestCase("noAdvance")][TestCase("stream")][TestCase("range")][TestCase("throw")]
        public void Bad_random_provider_cannot_return_a_partial_plan(string defect)
        {
            var calls=new Calls();var random=new FixedRandom(calls:calls,defect:defect);var s=State(random);
            Assert.That(()=>Plan(s,random),Throws.InstanceOf<InvalidOperationException>());Assert.That(s.Military.AppliedCredits,Is.Empty);Assert.That(s.VpForUS,Is.Zero);Assert.That(s.Countries[C("south_korea")].US,Is.EqualTo(3));Pcg32RandomTests.Same(s.Random,random.Capture());
        }
        [Test] public void Wrong_random_cursor_and_already_assessed_or_credited_state_fail_before_draw()
        {
            var calls=new Calls();var random=new FixedRandom(calls:calls);var s=State(random);
            Assert.That(()=>Plan(s,Pcg32Random.FromSeed(42,54)),Throws.InstanceOf<InvalidOperationException>());
            var credit=new MilitaryCredit("war/attempt",s.Military.Turn.Number,PlayerSide.USSR,MilitaryCreditKind.WarEvent,MilitaryCreditBasis.VerifiedCardParameter,2,"effect.korean_war");
            var credited=Copy(s,military:MilitaryOperationsService.PlanCredit(s.Military,credit).After);Assert.That(()=>Plan(credited,random),Throws.InstanceOf<InvalidOperationException>());
            var assessed=Copy(s,military:MilitaryOperationsService.AssessTurnEnd(s.Military,"assess",s.Defcon).After);Assert.That(()=>Plan(assessed,random),Throws.InstanceOf<InvalidOperationException>());Assert.That(calls.Forks,Is.Zero);Assert.That(calls.Draws,Is.Zero);
        }
        [Test] public void PCG_reconstruction_is_deterministic_and_original_stream_is_unchanged()
        {
            var random=Pcg32Random.FromSeed(42,54);var s=State(random);var a=Plan(s,random);var b=Plan(s,new Pcg32Random(s.Random));
            Assert.That(a.Calculation.Roll,Is.EqualTo(b.Calculation.Roll));Assert.That(a.After.VpForUS,Is.EqualTo(b.After.VpForUS));Pcg32RandomTests.Same(a.After.Random,b.After.Random);Pcg32RandomTests.Same(s.Random,random.Capture());
            Assert.That(()=>Plan(a.After,new Pcg32Random(a.After.Random)),Throws.InstanceOf<InvalidOperationException>());
        }
        [Test] public void State_copies_board_and_rejects_incomplete_or_different_map()
        {
            var s=State(new FixedRandom());var board=s.Countries.ToDictionary(x=>x.Key,x=>x.Value);var copy=Copy(s,board);board[C("south_korea")]=new CountryState(99,99);Assert.That(copy.Countries[C("south_korea")].US,Is.EqualTo(3));
            board.Remove(C("france"));Assert.That(()=>Copy(s,board),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>new WarEventState(EarlyWarDefinitionTests.Fixture(),s.Countries,s.Military,s.Defcon,s.Victory,0,s.Random),Throws.InstanceOf<ArgumentException>());
        }
    }
}
