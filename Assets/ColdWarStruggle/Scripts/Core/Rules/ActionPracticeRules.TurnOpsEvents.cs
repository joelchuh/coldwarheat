namespace ColdWarStruggle.Core
{
    public static partial class ActionPracticeRules
    {
        static CardActionTransition PlanTurnOpsEvent(GameState state,PlayerSide side,TurnOpsEffectDefinition effect)
        {
            var p=state.Practice;
            var activation=p.OpsEffects.Activate(effect,side,"practice.event."+p.CompletedActions,checked(state.EventSequence+1),state.Turn);
            if(!activation.Accepted)return new CardActionTransition(activation.Error);
            // The host owns duplicate command replay; an already-used root cursor must not dispose a second physical card.
            if(activation.AlreadyApplied)return new CardActionTransition(RuleError.StaleContext);
            var next=Next(p,p.Space,p.VpForUS,opsEffects:activation.After);
            var printed=p.Definition.Cards.GetCard(effect.Card);
            var result=Commit(state,next,Move(state,effect.Card,true,false,printed.RemoveAfterResolvedEvent),state.RandomState);
            return new CardActionTransition(RuleError.None,result.NextState,activatedOpsEffect:activation.Instance);
        }
    }
}
