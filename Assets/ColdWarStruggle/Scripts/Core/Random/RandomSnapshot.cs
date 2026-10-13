using System;
namespace ColdWarStruggle.Core
{
    public sealed class RandomSnapshot
    {
        public const string SupportedAlgorithm="pcg32-xsh-rr-v1";
        public string AlgorithmId { get; }
        public ulong State { get; }
        public ulong Increment { get; }
        public ulong DrawCount { get; }
        public RandomSnapshot(string algorithm,ulong state,ulong increment,ulong drawCount)
        {
            if(algorithm!=SupportedAlgorithm)throw new ArgumentException("Unsupported random algorithm.",nameof(algorithm));
            if((increment&1UL)==0)throw new ArgumentException("PCG increment must be odd.",nameof(increment));
            AlgorithmId=algorithm;State=state;Increment=increment;DrawCount=drawCount;
        }
    }
}
