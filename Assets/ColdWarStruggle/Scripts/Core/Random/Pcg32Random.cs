using System;
// PCG XSH-RR transition/output adapted from pcg-c-basic, Copyright 2014 Melissa O'Neill.
// SPDX-License-Identifier: Apache-2.0
// https://github.com/imneme/pcg-c-basic/blob/master/pcg_basic.c
// C# adaptation adds snapshots, output counts, independent forks and atomic bounded calls.
namespace ColdWarStruggle.Core
{
    public sealed class Pcg32Random : IRandomProvider
    {
        const ulong Multiplier=6364136223846793005UL;
        ulong state;
        ulong drawCount;
        readonly ulong increment;

        public Pcg32Random(RandomSnapshot snapshot)
        {
            if(snapshot==null)throw new ArgumentNullException(nameof(snapshot));
            state=snapshot.State;increment=snapshot.Increment;drawCount=snapshot.DrawCount;
        }
        public static Pcg32Random FromSeed(ulong seed,ulong stream)
        {
            if(stream>ulong.MaxValue>>1)throw new ArgumentOutOfRangeException(nameof(stream));
            var increment=(stream<<1)|1UL;
            // Two PCG seeding transitions, excluded from game output count.
            var initialized=unchecked((increment+seed)*Multiplier+increment);
            return new Pcg32Random(new RandomSnapshot(RandomSnapshot.SupportedAlgorithm,initialized,increment,0));
        }
        static uint Advance(ref ulong localState,ref ulong localCount,ulong increment)
        {
            if(localCount==ulong.MaxValue)throw new OverflowException("Random output count exhausted.");
            var previous=localState;
            localState=unchecked(previous*Multiplier+increment);
            localCount++;
            var mixed=unchecked((uint)(((previous>>18)^previous)>>27));
            var rotation=(int)(previous>>59);
            return (mixed>>rotation)|(mixed<<((-rotation)&31));
        }
        public uint NextUInt32()=>Advance(ref state,ref drawCount,increment);
        public int NextInt(int minInclusive,int maxExclusive)
        {
            if(minInclusive>=maxExclusive)throw new ArgumentException("Random interval must be nonempty.");
            var bound=(uint)((long)maxExclusive-minInclusive);
            // Reject the incomplete block so every possible remainder has equal weight.
            var rejectedBelow=(uint)(0x100000000UL%bound);
            var localState=state;var localCount=drawCount;
            uint sample;
            do { sample=Advance(ref localState,ref localCount,increment); } while(sample<rejectedBelow);
            var result=(int)((long)minInclusive+sample%bound);
            state=localState;drawCount=localCount;
            return result;
        }
        public RandomSnapshot Capture()=>new RandomSnapshot(RandomSnapshot.SupportedAlgorithm,state,increment,drawCount);
        public IRandomProvider Fork()=>new Pcg32Random(Capture());
    }
}
