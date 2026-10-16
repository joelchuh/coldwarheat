using System;
using System.Collections.Generic;
using System.Linq;
using ColdWarStruggle.Core;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public sealed class OlympicContinuationTests
    {
        static readonly int[] Faces={1,3,2,4,4,5};
        static OlympicResolution Tie(long revision=7)
        {
            var r=new OlympicResolutionTests.ScriptRandom(Faces);
            return OlympicResolutionTests.Respond(OlympicResolutionTests.Begin(r,revision:revision),r);
        }
        static IRandomProvider RandomAt(RandomSnapshot s)=>new OlympicResolutionTests.ScriptRandom(Faces,s.DrawCount);
        [Test] public void One_pair_slices_and_one_larger_slice_produce_the_same_complete_history()
        {
            var start=Tie();var two=OlympicContinuationRunner.PlanSlice(start,3,RandomAt).ToArray();
            var one=OlympicContinuationRunner.PlanSlice(start,1,RandomAt).Single();var final=OlympicContinuationRunner.PlanSlice(one,1,RandomAt).Single();
            Assert.That(two.Length,Is.EqualTo(2));Assert.That(two[0].Stage,Is.EqualTo(OlympicResolutionStage.Rolling));Assert.That(two[1].Stage,Is.EqualTo(OlympicResolutionStage.Completed));
            Assert.That(final.Current.VpForUS,Is.EqualTo(2));Assert.That(final.LastPair.RoundIndex,Is.EqualTo(3));Assert.That(final.Revision,Is.EqualTo(10));
            Assert.That(final.Current.VpForUS,Is.EqualTo(two[1].Current.VpForUS));Pcg32RandomTests.Same(final.Current.Random,two[1].Current.Random);
            Assert.That(final.LastPair.Plan.Calculation.UsRoll,Is.EqualTo(4));Assert.That(final.LastPair.Plan.Calculation.UssrRoll,Is.EqualTo(5));
            Assert.That(final.LastPair.Previous.Plan.Calculation.UsRoll,Is.EqualTo(2));Assert.That(final.LastPair.Previous.Plan.Calculation.UssrRoll,Is.EqualTo(4));
        }
        [Test] public void Factory_is_lazy_uses_the_latest_cursor_and_stops_at_victory()
        {
            var start=Tie();var seen=new List<ulong>();
            var enumerable=OlympicContinuationRunner.PlanSlice(start,100,s=>{seen.Add(s.DrawCount);return RandomAt(s);});
            Assert.That(seen,Is.Empty);
            using(var e=enumerable.GetEnumerator())
            {
                Assert.That(seen,Is.Empty);Assert.That(e.MoveNext(),Is.True);Assert.That(seen,Is.EqualTo(new ulong[]{2}));
                var committed=e.Current;Assert.That(committed.Current.Random.DrawCount,Is.EqualTo(4));Assert.That(committed.LastPair.Previous,Is.SameAs(start.LastPair));
                Assert.That(e.MoveNext(),Is.True);Assert.That(seen,Is.EqualTo(new ulong[]{2,4}));Assert.That(e.Current.Current.Random.DrawCount,Is.EqualTo(6));
                Assert.That(e.MoveNext(),Is.False);Assert.That(seen.Count,Is.EqualTo(2));
            }
            Assert.That(start.Current.Random.DrawCount,Is.EqualTo(2));
        }
        [Test] public void Disposing_after_one_result_never_draws_the_remaining_pair()
        {
            var count=0;OlympicResolution last;
            using(var e=OlympicContinuationRunner.PlanSlice(Tie(),3,s=>{count++;return RandomAt(s);}).GetEnumerator())
            {Assert.That(e.MoveNext(),Is.True);last=e.Current;}
            Assert.That(count,Is.EqualTo(1));Assert.That(last.Stage,Is.EqualTo(OlympicResolutionStage.Rolling));Assert.That(last.Current.Random.DrawCount,Is.EqualTo(4));
            var final=OlympicContinuationRunner.PlanSlice(last,1,RandomAt).Single();Assert.That(final.Current.VpForUS,Is.EqualTo(2));
        }
        [TestCase(0)][TestCase(-1)][TestCase(int.MinValue)]
        public void Nonpositive_work_budget_is_rejected_without_calling_factory(int budget)
        {
            var count=0;Assert.Throws<ArgumentOutOfRangeException>(()=>OlympicContinuationRunner.PlanSlice(Tie(),budget,s=>{count++;return RandomAt(s);}));Assert.That(count,Is.Zero);
        }
        [TestCase("waiting")][TestCase("ops")][TestCase("completed")]
        public void Only_internal_rolling_states_can_start_a_slice(string stage)
        {
            var r=new OlympicResolutionTests.ScriptRandom(new[]{6,1});var s=OlympicResolutionTests.Begin(r);
            if(stage=="ops")s=OlympicResolutionTests.Respond(s,null,OlympicBranch.Boycott);if(stage=="completed")s=OlympicResolutionTests.Respond(s,r);
            Assert.Throws<InvalidOperationException>(()=>OlympicContinuationRunner.PlanSlice(s,1,RandomAt));
        }
        [Test] public void Later_pair_failure_preserves_the_already_returned_tie()
        {
            var start=Tie();var count=0;
            using(var e=OlympicContinuationRunner.PlanSlice(start,3,s=>{count++;return new OlympicResolutionTests.ScriptRandom(Faces,s.DrawCount,failAt:6);}).GetEnumerator())
            {
                Assert.That(e.MoveNext(),Is.True);var committed=e.Current;Assert.That(committed.Current.Random.DrawCount,Is.EqualTo(4));
                Assert.Throws<InvalidOperationException>(()=>e.MoveNext());Assert.That(count,Is.EqualTo(2));
                Assert.That(committed.Stage,Is.EqualTo(OlympicResolutionStage.Rolling));Assert.That(committed.LastPair.RoundIndex,Is.EqualTo(2));Assert.That(committed.Current.VpForUS,Is.Zero);
                var retry=OlympicContinuationRunner.PlanSlice(committed,1,RandomAt).Single();Assert.That(retry.LastPair.RoundIndex,Is.EqualTo(3));Assert.That(retry.Current.VpForUS,Is.EqualTo(2));
            }
            Assert.That(start.Current.Random.DrawCount,Is.EqualTo(2));
        }
        [TestCase("null")][TestCase("mismatched")][TestCase("throws")]
        public void Bad_factory_is_not_swallowed_or_turned_into_a_fallback_result(string fault)
        {
            var start=Tie();var calls=0;
            Func<RandomSnapshot,IRandomProvider> factory=s=>{calls++;if(fault=="throws")throw new InvalidOperationException("Factory failure.");return fault=="null"?null:new OlympicResolutionTests.ScriptRandom(Faces);};
            using(var e=OlympicContinuationRunner.PlanSlice(start,1,factory).GetEnumerator())
            {Assert.That(()=>e.MoveNext(),fault=="null"?Throws.InstanceOf<ArgumentNullException>():Throws.InstanceOf<InvalidOperationException>());}
            Assert.That(calls,Is.EqualTo(1));Assert.That(start.LastPair.RoundIndex,Is.EqualTo(1));Assert.That(start.Current.Random.DrawCount,Is.EqualTo(2));
        }
        [Test] public void Revision_overflow_is_detected_before_invoking_factory()
        {
            var start=Tie(long.MaxValue-1);var calls=0;
            using(var e=OlympicContinuationRunner.PlanSlice(start,1,s=>{calls++;return RandomAt(s);}).GetEnumerator())
            {Assert.Throws<OverflowException>(()=>e.MoveNext());}
            Assert.That(calls,Is.Zero);Assert.That(start.Revision,Is.EqualTo(long.MaxValue));
        }
        [Test] public void Slice_budget_never_becomes_a_limit_on_total_ties()
        {
            var faces=Enumerable.Range(0,40).SelectMany(i=>new[]{1,3}).Concat(new[]{6,1}).ToArray();
            var r=new OlympicResolutionTests.ScriptRandom(faces);var current=OlympicResolutionTests.Respond(OlympicResolutionTests.Begin(r),r);
            var slices=0;
            while(current.Stage==OlympicResolutionStage.Rolling)
            {
                var slice=OlympicContinuationRunner.PlanSlice(current,3,s=>new OlympicResolutionTests.ScriptRandom(faces,s.DrawCount)).ToArray();
                Assert.That(slice.Length,Is.InRange(1,3));current=slice.Last();slices++;
            }
            Assert.That(slices,Is.EqualTo(14));Assert.That(current.LastPair.RoundIndex,Is.EqualTo(41));Assert.That(current.Current.Random.DrawCount,Is.EqualTo(82));Assert.That(current.Current.VpForUS,Is.EqualTo(2));
        }
        [Test] public void Missing_arguments_are_rejected_at_slice_creation()
        {
            Assert.Throws<ArgumentNullException>(()=>OlympicContinuationRunner.PlanSlice(null,1,RandomAt));
            Assert.Throws<ArgumentNullException>(()=>OlympicContinuationRunner.PlanSlice(Tie(),1,null));
        }
    }
}
