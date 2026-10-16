using System;
using System.Collections.Generic;
namespace ColdWarStruggle.Core
{
    public static class OlympicContinuationRunner
    {
        // Trusted host only: commit each yielded snapshot before asking for the next one.
        // This iterator does not commit, retry, serialize, or impose a total reroll limit.
        public static IEnumerable<OlympicResolution> PlanSlice(OlympicResolution state,int maxPairs,Func<RandomSnapshot,IRandomProvider> randomFactory)
        {
            if(state==null || randomFactory==null)throw new ArgumentNullException();
            if(maxPairs<=0)throw new ArgumentOutOfRangeException(nameof(maxPairs));
            if(state.Stage!=OlympicResolutionStage.Rolling)throw new InvalidOperationException("Only an internal Olympic tie can continue.");
            return Iterate(state,maxPairs,randomFactory);
        }
        static IEnumerable<OlympicResolution> Iterate(OlympicResolution current,int maxPairs,Func<RandomSnapshot,IRandomProvider> factory)
        {
            for(var i=0;i<maxPairs && current.Stage==OlympicResolutionStage.Rolling;i++)
            {
                // Preflight even before calling the factory, whose construction may be stateful.
                _=checked(current.Revision+1);_=current.Continuation.NextRoundIndex;
                var next=OlympicResolutionRules.ContinueOne(current,current.Continuation,factory(current.Current.Random));
                yield return next;
                current=next;
            }
        }
    }
}
