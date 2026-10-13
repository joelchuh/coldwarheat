using System;
using System.Linq;
using ColdWarStruggle.Core;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class Pcg32RandomTests
    {
        public static void Same(RandomSnapshot a,RandomSnapshot b)
        { Assert.That(b.AlgorithmId,Is.EqualTo(a.AlgorithmId));Assert.That(b.State,Is.EqualTo(a.State));Assert.That(b.Increment,Is.EqualTo(a.Increment));Assert.That(b.DrawCount,Is.EqualTo(a.DrawCount)); }
        [Test] public void Seed_42_stream_54_matches_author_published_sequence()
        {
            // pcg-random.org/using-pcg-c-basic.html and imneme/pcg-c-basic pcg32-demo.c.
            var rng=Pcg32Random.FromSeed(42,54);Assert.That(rng.Capture().DrawCount,Is.Zero);
            var expected=new uint[]{0xa15c02b7,0x7b47f409,0xba1d3330,0x83d2f293,0xbfa4784b,0xcbed606e};
            Assert.That(Enumerable.Range(0,6).Select(_=>rng.NextUInt32()).ToArray(),Is.EqualTo(expected));Assert.That(rng.Capture().DrawCount,Is.EqualTo(6));
        }
        [Test] public void Snapshot_restore_continues_the_current_sequence_instead_of_reseeding()
        {
            var rng=Pcg32Random.FromSeed(45,0);for(int i=0;i<37;i++)rng.NextInt(-100,100);
            var captured=rng.Capture();var restored=new Pcg32Random(captured);
            for(int i=0;i<64;i++)Assert.That(restored.NextInt(-7,23),Is.EqualTo(rng.NextInt(-7,23)));
            Same(rng.Capture(),restored.Capture());Assert.That(captured.DrawCount,Is.EqualTo(37));
        }
        [Test] public void Fork_is_independent_and_speculative_failure_does_not_consume_source()
        {
            var rng=Pcg32Random.FromSeed(12,34);var before=rng.Capture();var child=rng.Fork();
            for(int i=0;i<9;i++)child.NextInt(0,100);Same(before,rng.Capture());
            Assert.That(child.Capture().DrawCount,Is.GreaterThan(before.DrawCount));
            var child2=rng.Fork();Assert.That(child2.NextInt(0,100),Is.EqualTo(rng.NextInt(0,100)));Same(rng.Capture(),child2.Capture());
        }
        [TestCase(0,1)] [TestCase(-10,-9)] [TestCase(1,7)] [TestCase(-100,100)] [TestCase(int.MinValue,int.MaxValue)] [TestCase(int.MinValue,1)]
        public void Bounded_values_fit_the_requested_interval(int min,int max)
        {
            var rng=Pcg32Random.FromSeed(42,54);for(int i=0;i<256;i++)Assert.That(rng.NextInt(min,max),Is.InRange(min,max-1));
            if((long)max-min==1)Assert.That(rng.Capture().DrawCount,Is.EqualTo(256));
        }
        [TestCase(0,0)] [TestCase(2,1)] [TestCase(int.MaxValue,int.MinValue)]
        public void Invalid_interval_is_rejected_without_consuming_random_state(int min,int max)
        {
            var rng=Pcg32Random.FromSeed(42,54);var before=rng.Capture();Assert.That(()=>rng.NextInt(min,max),Throws.InstanceOf<ArgumentException>());Same(before,rng.Capture());
        }
        [Test] public void Rejection_sampling_records_rejected_raw_outputs()
        {
            var rng=new Pcg32Random(new RandomSnapshot(RandomSnapshot.SupportedAlgorithm,0,1,0));
            // First two outputs are zero; third is 3837872008. Offset + modulo gives -457095289.
            var value=rng.NextInt(int.MinValue,1);Assert.That(value,Is.EqualTo(-457095289));Assert.That(rng.Capture().DrawCount,Is.EqualTo(3));
        }
        [Test] public void Overflow_during_rejection_sampling_rolls_back_all_local_advances()
        {
            var rng=new Pcg32Random(new RandomSnapshot(RandomSnapshot.SupportedAlgorithm,0,1,ulong.MaxValue-1));var before=rng.Capture();
            Assert.Throws<OverflowException>(()=>rng.NextInt(int.MinValue,1));Same(before,rng.Capture());
            rng=new Pcg32Random(new RandomSnapshot(RandomSnapshot.SupportedAlgorithm,5,1,ulong.MaxValue));before=rng.Capture();
            Assert.Throws<OverflowException>(()=>rng.NextUInt32());Same(before,rng.Capture());
        }
        [Test] public void Unknown_algorithm_even_increment_null_snapshot_or_out_of_range_stream_are_rejected()
        {
            Assert.That(()=>new RandomSnapshot("future",0,1,0),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>new RandomSnapshot(RandomSnapshot.SupportedAlgorithm,0,2,0),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>new Pcg32Random(null),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>Pcg32Random.FromSeed(0,1UL<<63),Throws.InstanceOf<ArgumentException>());
            Assert.DoesNotThrow(()=>Pcg32Random.FromSeed(ulong.MaxValue,(1UL<<63)-1));
        }
    }
}
