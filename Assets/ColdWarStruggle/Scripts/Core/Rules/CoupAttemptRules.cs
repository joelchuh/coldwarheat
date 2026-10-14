using System;
using System.Linq;
namespace ColdWarStruggle.Core
{
    public sealed class CoupAttemptPlan
    {
        public CoupAttemptState Before { get; }
        public CoupAttemptState After { get; }
        public CoupAttemptReceipt Receipt { get; }
        public bool AlreadyApplied { get; }
        internal CoupAttemptPlan(CoupAttemptState before,CoupAttemptState after,CoupAttemptReceipt receipt,bool alreadyApplied)
        { Before=before;After=after;Receipt=receipt;AlreadyApplied=alreadyApplied; }
    }
    // Trusted one-turn aggregate plan. The host must authorize the action and commit After only against current Before.
    public static class CoupAttemptRules
    {
        public static CoupAttemptPlan PlanAttempt(CoupAttemptState state,CoupAttemptRequest request,IRandomProvider random)
        {
            if(state==null || request==null || random==null)throw new ArgumentNullException();
            if(request.GameId!=state.GameId || request.TurnNumber!=state.Military.Turn.Number)throw new ArgumentException("Coup game or turn mismatch.");
            if(state.Receipts.TryGetValue(request.Id,out var prior))
            {
                if(!prior.Request.SameFacts(request))throw new ArgumentException("Conflicting coup request ID.");
                // A retry acknowledges the original receipt while preserving every part of the current aggregate.
                return new CoupAttemptPlan(state,state,prior,true);
            }
            if(request.ExpectedRevision!=state.Revision)throw new ArgumentException("Stale coup revision.");
            if(state.Receipts.Count>=4096)throw new InvalidOperationException("Coup receipt engineering limit reached.");
            var nextRevision=checked(state.Revision+1);
            if(!state.Countries.TryGetValue(request.Target,out var before))throw new ArgumentException("Unknown coup country.");
            var reason=CoupRules.CheckTarget(state.Defcon,request.Target,before,request.Actor,state.ActiveEffectIds);
            if(reason!=CoupTargetReason.Allowed)throw new InvalidOperationException("Coup target rejected: "+reason);
            var credit=new MilitaryCredit("coup/"+request.Id,request.TurnNumber,request.Actor,MilitaryCreditKind.OperationsCoup,
                MilitaryCreditBasis.EffectiveMilitaryOperationsCredit,request.MilitaryCredit,request.SourceRuleId);
            var military=MilitaryOperationsService.PlanCredit(state.Military,credit);
            if(military.AlreadyApplied)throw new InvalidOperationException("Military credit exists without its coup receipt.");
            // Only the independent fork advances. The resulting cursor is committed with all other effects in After.
            if(!Same(state.Random,random.Capture()))throw new InvalidOperationException("Random stream does not match the coup state.");
            var fork=random.Fork();
            if(fork==null || ReferenceEquals(fork,random) || !Same(state.Random,fork.Capture()))throw new InvalidOperationException("Independent random fork required.");
            var definition=state.Definition;
            var sample=fork.NextInt(definition.DieMinimum-1,definition.DieMaximum);
            if(sample<definition.DieMinimum-1 || sample>=definition.DieMaximum)throw new InvalidOperationException("Random die sample out of range.");
            var afterRandom=fork.Capture();
            if(afterRandom==null || afterRandom.AlgorithmId!=state.Random.AlgorithmId || afterRandom.Increment!=state.Random.Increment
                || afterRandom.DrawCount<=state.Random.DrawCount)throw new InvalidOperationException("Random stream did not advance correctly.");
            var calculation=CoupRules.Calculate(definition,state.Defcon,request.Target,before,request.Actor,request.Strength,sample+1,state.ActiveEffectIds);
            var defcon=calculation.RequiresBattlegroundDegradation
                ?DefconService.PlanChange(state.Defcon,DefconChangeKind.BattlegroundCoupDegradation,request.PhasingPlayer):null;
            var receipt=new CoupAttemptReceipt(request,calculation,credit,defcon,state.Random,afterRandom);
            var countries=state.Countries.ToDictionary(p=>p.Key,p=>p.Value);countries[request.Target]=calculation.After;
            var receipts=state.Receipts.ToDictionary(p=>p.Key,p=>p.Value,StringComparer.Ordinal);receipts.Add(request.Id,receipt);
            var after=new CoupAttemptState(state.GameId,nextRevision,definition,defcon?.After??state.Defcon,military.After,countries,afterRandom,state.ActiveEffectIds,receipts);
            return new CoupAttemptPlan(state,after,receipt,false);
        }
        static bool Same(RandomSnapshot a,RandomSnapshot b)=>a!=null && b!=null && a.AlgorithmId==b.AlgorithmId
            && a.State==b.State && a.Increment==b.Increment && a.DrawCount==b.DrawCount;
    }
}
