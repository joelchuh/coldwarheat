using System;
using System.Linq;
namespace ColdWarStruggle.Core
{
    public sealed class MilitaryCreditPlan
    {
        public MilitaryOperationsState Before { get; }
        public MilitaryOperationsState After { get; }
        public MilitaryCredit Credit { get; }
        public bool AlreadyApplied { get; }
        public int AmountApplied => AlreadyApplied?0:Credit.Amount;
        internal MilitaryCreditPlan(MilitaryOperationsState before,MilitaryOperationsState after,MilitaryCredit credit,bool already)
        { Before=before;After=after;Credit=credit;AlreadyApplied=already; }
    }
    public sealed class MilitaryAssessmentPlan
    {
        public MilitaryOperationsState Before { get; }
        public MilitaryOperationsState After { get; }
        public MilitaryTurnEndAssessment Assessment { get; }
        public bool AlreadyApplied { get; }
        public int VpDeltaToApply => AlreadyApplied?0:Assessment.NetVpForUS;
        internal MilitaryAssessmentPlan(MilitaryOperationsState before,MilitaryOperationsState after,MilitaryTurnEndAssessment assessment,bool already)
        { Before=before;After=after;Assessment=assessment;AlreadyApplied=already; }
    }
    public static class MilitaryOperationsService
    {
        // Credits are produced by trusted rules, not by client-declared rewards.
        public static MilitaryCreditPlan PlanCredit(MilitaryOperationsState state,MilitaryCredit credit)
        {
            if(state==null || credit==null)throw new ArgumentNullException();
            if(credit.TurnNumber!=state.Turn.Number || state.Definition.CreditSources[credit.Kind]!=credit.Basis)
                throw new ArgumentException("Military credit context or source mismatch.");
            if(state.AppliedCredits.TryGetValue(credit.Id,out var prior))
            {
                if(!prior.SameFacts(credit))throw new ArgumentException("Conflicting military credit ID.");
                return new MilitaryCreditPlan(state,state,prior,true);
            }
            if(state.Assessment!=null)throw new InvalidOperationException("Military operations have already been assessed.");
            if(state.AppliedCredits.Count>=4096)throw new InvalidOperationException("Military credit record limit reached.");
            int us=state.UsEarned,ussr=state.UssrEarned;
            if(credit.Beneficiary==PlayerSide.US)us=checked(us+credit.Amount);else ussr=checked(ussr+credit.Amount);
            var credits=state.AppliedCredits.ToDictionary(p=>p.Key,p=>p.Value,StringComparer.Ordinal);
            credits.Add(credit.Id,credit);
            var after=new MilitaryOperationsState(state.Definition,state.Turn,us,ussr,credits,null);
            return new MilitaryCreditPlan(state,after,credit,false);
        }
        // Commit the returned state, net VP delta and surrounding step cursor together.
        public static MilitaryAssessmentPlan AssessTurnEnd(MilitaryOperationsState state,string assessmentId,DefconState defcon)
        {
            if(state==null || defcon==null)throw new ArgumentNullException();
            MilitaryCredit.RequireIdentifier(assessmentId);
            if(defcon.Level==defcon.Definition.NuclearLevel)throw new InvalidOperationException("No turn-end assessment after nuclear war.");
            if(state.Assessment!=null)
            {
                var prior=state.Assessment;
                if(prior.Id!=assessmentId || prior.Defcon.Level!=defcon.Level || !ReferenceEquals(prior.Defcon.Definition,defcon.Definition))
                    throw new ArgumentException("Conflicting military assessment input.");
                return new MilitaryAssessmentPlan(state,state,prior,true);
            }
            int usShortfall=Math.Max(0,defcon.Level-state.UsEarned),ussrShortfall=Math.Max(0,defcon.Level-state.UssrEarned);
            int net=checked((int)(((long)ussrShortfall-usShortfall)*state.Definition.PenaltyVpPerShortfall));
            var assessment=new MilitaryTurnEndAssessment(assessmentId,state,defcon,usShortfall,ussrShortfall,net);
            var after=new MilitaryOperationsState(state.Definition,state.Turn,state.Definition.ResetEarned,state.Definition.ResetEarned,state.AppliedCredits,assessment);
            return new MilitaryAssessmentPlan(state,after,assessment,false);
        }
    }
}
