namespace ColdWarStruggle.Core
{
    public static partial class ActionPracticeRules
    {
        // Called only after real-hand Event eligibility and command validation.
        static CardActionTransition PlanDefconVpEvent(GameState state,PlayerSide side,DefconVpEffectDefinition effect)
        {
            var p=state.Practice;var plan=DefconVpEventService.Plan(effect,p.Risk.Defcon,p.Risk.Definition.Victory,p.VpForUS,side,side,"practice.event."+p.CompletedActions,DefconVpEventPolicy.NoActiveInterference);
            var risk=p.Risk.After(plan);var terminal=risk.Result?.Outcome==VictoryOutcome.Finished;
            var printed=p.Definition.Cards.GetCard(effect.Card);var next=AfterImmediate(p,effect.Card,p.Space,plan.Decision.SignedVp,risk);
            var done=Commit(state,next,Move(state,effect.Card,true,terminal,!terminal && printed.RemoveAfterResolvedEvent),state.RandomState);
            return new CardActionTransition(RuleError.None,done.NextState,defconVpEvent:plan);
        }
    }
}
