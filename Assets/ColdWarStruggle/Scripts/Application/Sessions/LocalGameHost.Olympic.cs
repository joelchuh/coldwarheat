using System;
using ColdWarStruggle.Core;
namespace ColdWarStruggle.Application
{
    public sealed partial class LocalGameHost
    {
        // Trusted scheduling boundary, deliberately absent from IGameSession.
        public CardActionResult AdvanceInternalOlympic(PlayerSide viewer)
        {
            if(viewer!=PlayerSide.US && viewer!=PlayerSide.USSR)throw new ArgumentOutOfRangeException(nameof(viewer));
            lock(gate)
            {
                var plan=ActionPracticeRules.PlanOlympicContinuation(state);if(!plan.Accepted)return RejectCard(plan.Error,viewer);
                // Construct the public receipt before publishing the immutable authority snapshot.
                var result=new CardActionResult(RuleError.None,new PlayerViewSnapshot(plan.NextState,viewer),olympicResult:plan.OlympicResult);
                state=plan.NextState;return result;
            }
        }
    }
}
