using System;
using System.Collections.Generic;
using System.Linq;
namespace ColdWarStruggle.Core
{
    public sealed class DefconVpEventPlan
    {
        public DefconVpEffectDefinition Effect { get; }
        public VictoryDefinition VictoryDefinition { get; }
        public DefconState Before { get; }
        public DefconState After { get; }
        public int VpBefore { get; }
        public DefconChangePlan DefconChange { get; }
        public int? Award { get; }
        public PlayerSide AwardSide { get; }
        public VictoryDecision Decision { get; }
        public IReadOnlyList<DefconVpStep> Steps { get; }
        internal DefconVpEventPlan(DefconVpEffectDefinition effect,VictoryDefinition victory,DefconState before,int vp,DefconChangePlan change,int? award,PlayerSide owner,VictoryDecision decision,IEnumerable<DefconVpStep> steps)
        { Effect=effect;VictoryDefinition=victory;Before=before;After=change?.After??before;VpBefore=vp;DefconChange=change;Award=award;AwardSide=owner;Decision=decision;Steps=Array.AsReadOnly(steps.ToArray()); }
    }
    public static class DefconVpEventService
    {
        public static DefconVpEventPlan Plan(DefconVpEffectDefinition effect,DefconState defcon,VictoryDefinition victory,int vp,PlayerSide controller,PlayerSide phasing,string resolutionId,DefconVpEventPolicy policy)
        {
            if(effect==null || defcon==null || victory==null)throw new ArgumentNullException();
            if((controller!=PlayerSide.US && controller!=PlayerSide.USSR) || (phasing!=PlayerSide.US && phasing!=PlayerSide.USSR)
                || policy!=DefconVpEventPolicy.NoActiveInterference || defcon.Level==defcon.Definition.NuclearLevel)throw new ArgumentException("Expected live, known event context.");
            // Validate an already-live score even when the first actual step will cause nuclear war.
            var decision=VictoryService.Evaluate(victory,new ClosedVpBatchEvidence(resolutionId,effect.EffectId,vp,0,0));
            var duck=effect.Kind==DefconVpEventKind.DegradeThenUsAward;var owner=duck?PlayerSide.US:controller;
            var steps=new List<DefconVpStep>();DefconChangePlan change=null;int? award=null;
            DefconChangePlan Change()
            {
                var target=(int)Math.Max(defcon.Definition.NuclearLevel,Math.Min(defcon.Definition.MaximumLevel,(long)defcon.Level+effect.DefconDelta));
                var result=DefconService.PlanChange(defcon,DefconChangeKind.SetLevel,phasing,target);steps.Add(DefconVpStep.DefconChanged);return result;
            }
            DefconVpEventPlan Result()=>new DefconVpEventPlan(effect,victory,defcon,vp,change,award,owner,decision,steps);
            if(duck)
            {
                change=Change();
                if(change.NuclearWar)
                { decision=VictoryService.Evaluate(victory,new NuclearWarEvidence(resolutionId,effect.EffectId,vp,change));steps.Add(DefconVpStep.NuclearWarReached);return Result(); }
            }
            award=duck?checked(effect.VpConstant-change.After.Level):checked(defcon.Level-effect.VpConstant);
            decision=VictoryService.Evaluate(victory,new ClosedVpBatchEvidence(resolutionId,effect.EffectId,vp,owner==PlayerSide.US?award.Value:0,owner==PlayerSide.USSR?award.Value:0));
            steps.Add(DefconVpStep.VpBatchClosed);
            if(!duck && decision.Outcome!=VictoryOutcome.Finished)change=Change();
            return Result();
        }
    }
}
