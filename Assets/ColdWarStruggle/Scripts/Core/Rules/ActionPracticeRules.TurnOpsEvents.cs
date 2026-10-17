namespace ColdWarStruggle.Core
{
    public static partial class ActionPracticeRules
    {
        // All states here are private candidates; the host publishes only the completed parent transition.
        static RuleError PrepareTurnOpsEvent(GameState state,PlayerSide controller,TurnOpsEffectDefinition effect,string resolution,
            out GameState candidate,out TurnOpsActivation activation,out InfluenceEventTransition grant)
        {
            candidate=state;activation=null;grant=null;
            if(effect.ImmediateInfluence!=null)
            {
                if(state.InfluenceEvent!=null)return RuleError.EventInProgress;
                var prepared=state.WithInfluenceEvent(new InfluenceEventExecution(resolution,effect.ImmediateInfluence,state.Practice.Actor,InfluenceEventPolicy.NoActiveInterference));
                grant=InfluenceEventFlow.Begin(prepared,state.Practice.Actor);
                if(!grant.Accepted)return grant.Error;
                if(grant.NextState.InfluenceEvent.Stage!=InfluenceEventStage.Resolved)return RuleError.InvalidDefinition;
                candidate=grant.NextState.WithInfluenceEvent(null);
            }
            activation=state.Practice.OpsEffects.Activate(effect,controller,resolution,checked(candidate.EventSequence+1),state.Turn);
            return !activation.Accepted?activation.Error:activation.AlreadyApplied?RuleError.StaleContext:RuleError.None;
        }
        static CardActionTransition PlanTurnOpsEvent(GameState state,PlayerSide side,TurnOpsEffectDefinition effect)
        {
            var p=state.Practice;
            var error=PrepareTurnOpsEvent(state,side,effect,EventResolutionId(state),out var candidate,out var activation,out var grant);
            if(error!=RuleError.None)return new CardActionTransition(error);
            var next=Next(p,p.Space,p.VpForUS,opsEffects:activation.After);
            var printed=p.Definition.Cards.GetCard(effect.Card);
            var result=Commit(candidate,next,Move(candidate,effect.Card,true,false,printed.RemoveAfterResolvedEvent),candidate.RandomState);
            return new CardActionTransition(RuleError.None,result.NextState,eventTransition:grant,activatedOpsEffect:activation.Instance);
        }
    }
}
