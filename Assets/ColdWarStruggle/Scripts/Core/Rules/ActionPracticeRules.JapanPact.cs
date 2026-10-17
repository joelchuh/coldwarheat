namespace ColdWarStruggle.Core
{
    public static partial class ActionPracticeRules
    {
        static RuleError PrepareJapanPactEvent(GameState state,PlayerSide controller,string resolution,
            out GameState candidate,out JapanPactActivation activation,out InfluenceEventTransition grant)
        {
            candidate=state;activation=null;grant=null;var definition=state.Practice.Definition.JapanPact;
            if(definition==null || state.JapanPact==null)return RuleError.InvalidDefinition;
            if(state.InfluenceEvent!=null)return RuleError.EventInProgress;
            grant=InfluenceEventFlow.Begin(state.WithInfluenceEvent(new InfluenceEventExecution(resolution,definition.ImmediateInfluence,state.Practice.Actor,InfluenceEventPolicy.NoActiveInterference)),state.Practice.Actor);
            if(!grant.Accepted)return grant.Error;
            if(grant.NextState.InfluenceEvent.Stage!=InfluenceEventStage.Resolved)return RuleError.InvalidDefinition;
            candidate=grant.NextState.WithInfluenceEvent(null);
            activation=state.JapanPact.Activate(candidate,controller,resolution);
            return !activation.Accepted?activation.Error:activation.AlreadyApplied?RuleError.StaleContext:RuleError.None;
        }
        static CardActionTransition PlanJapanPactEvent(GameState state,PlayerSide side)
        {
            var error=PrepareJapanPactEvent(state,side,EventResolutionId(state),out var candidate,out var activation,out var grant);
            if(error!=RuleError.None)return new CardActionTransition(error);
            var p=state.Practice;var d=p.Definition.JapanPact;var printed=p.Definition.Cards.GetCard(d.Card);
            var done=Commit(candidate,Next(p,p.Space,p.VpForUS),Move(candidate,d.Card,true,false,printed.RemoveAfterResolvedEvent),candidate.RandomState,japanPact:activation.After);
            return new CardActionTransition(RuleError.None,done.NextState,eventTransition:grant,activatedJapanPact:activation.Instance);
        }
        static CardActionTransition PlanOpponentJapanPact(GameState state)
        {
            var p=state.Practice;var q=p.OpponentOps;
            var error=PrepareJapanPactEvent(state,q.EventSide,EventResolutionId(state),out var candidate,out var activation,out var grant);
            if(error!=RuleError.None)return new CardActionTransition(error);
            var changed=Commit(candidate,candidate.Practice,candidate.Cards,candidate.RandomState,japanPact:activation.After);
            var done=AdvanceOpponentStep(changed.NextState,CardResolutionStep.Event);
            return done.Accepted?new CardActionTransition(RuleError.None,done.NextState,eventTransition:grant,activatedJapanPact:activation.Instance):done;
        }
        static CardActionTransition PlanFiveYearJapanPact(GameState state,FiveYearPlanExecution execution)
        {
            var error=PrepareJapanPactEvent(state,PlayerSide.US,execution.ChildResolutionId,out var candidate,out var activation,out var grant);
            if(error!=RuleError.None)return new CardActionTransition(error);
            var changed=Commit(candidate,candidate.Practice,candidate.Cards,candidate.RandomState,japanPact:activation.After);
            var done=FinishFiveYearPlan(changed.NextState);
            return done.Accepted?new CardActionTransition(RuleError.None,done.NextState,eventTransition:grant,activatedJapanPact:activation.Instance,fiveYearPlanResult:done.FiveYearPlanResult):done;
        }
        static RuleError CheckJapanPactTarget(GameState state,PlayerSide actor,CountryId target,TreatyAction action)
        {
            if(state.Practice.Definition.JapanPact==null)return RuleError.None;
            return JapanPactRules.Evaluate(state,state.JapanPact,action,actor,target).Error;
        }
    }
}
