using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
namespace ColdWarStruggle.Core
{
    // Trusted rule evidence only; never a client-selectable command option.
    public enum WarInterferencePolicy { NoActiveInterference, CampDavidPreventsArabIsraeli }
    public sealed class WarEventState
    {
        public WarEventCatalog Catalog { get; }
        public IReadOnlyDictionary<CountryId,CountryState> Countries { get; }
        public MilitaryOperationsState Military { get; }
        public DefconState Defcon { get; }
        public VictoryDefinition Victory { get; }
        public int VpForUS { get; }
        public RandomSnapshot Random { get; }
        public WarEventState(WarEventCatalog catalog,IReadOnlyDictionary<CountryId,CountryState> countries,MilitaryOperationsState military,DefconState defcon,VictoryDefinition victory,int vp,RandomSnapshot random)
        {
            if(catalog==null || countries==null || military==null || defcon==null || victory==null || random==null)throw new ArgumentNullException();
            if(!ReferenceEquals(catalog.Map,defcon.Definition.Map))throw new ArgumentException("War and DEFCON must use the same rule map.");
            WarEventRules.RequireBoard(catalog,countries);
            Catalog=catalog;Countries=new ReadOnlyDictionary<CountryId,CountryState>(countries.ToDictionary(p=>p.Key,p=>p.Value));Military=military;Defcon=defcon;Victory=victory;VpForUS=vp;Random=random;
        }
    }
    public sealed class WarEventPlan
    {
        public WarEventState Before { get; }
        public WarEventState After { get; }
        public WarCalculation Calculation { get; }
        public MilitaryCredit Credit { get; }
        public VictoryDecision Decision { get; }
        public bool Blocked { get; }
        internal WarEventPlan(WarEventState before,WarEventState after,WarCalculation calculation,MilitaryCredit credit,VictoryDecision decision,bool blocked)
        {Before=before;After=after;Calculation=calculation;Credit=credit;Decision=decision;Blocked=blocked;}
    }
    public static class WarEventService
    {
        // The authenticated parent owns action legality, deduplication and the atomic commit of After.
        public static WarEventPlan Plan(WarEventState state,CardId card,PlayerSide controller,CountryId target,string resolutionId,WarInterferencePolicy policy,IRandomProvider random)
        {
            if(state==null || random==null)throw new ArgumentNullException();
            MilitaryCredit.RequireIdentifier(resolutionId);var creditId="war/"+resolutionId;MilitaryCredit.RequireIdentifier(creditId);
            var catalog=state.Catalog;var effect=WarEventRules.Effect(catalog,card);
            if((controller!=PlayerSide.US && controller!=PlayerSide.USSR) || !effect.Targets.Contains(target)
                || !Enum.IsDefined(typeof(WarInterferencePolicy),policy) || state.Defcon.Level==state.Defcon.Definition.NuclearLevel
                || (policy==WarInterferencePolicy.CampDavidPreventsArabIsraeli && effect.Kind!=WarEventKind.ArabIsraeli))throw new ArgumentException("Invalid or unsupported war context.");
            var live=VictoryService.Evaluate(state.Victory,new ClosedVpBatchEvidence(resolutionId,effect.EffectId,state.VpForUS,0,0));
            if(state.Military.Assessment!=null || state.Military.AppliedCredits.ContainsKey(creditId))throw new InvalidOperationException("Assessed military or existing war credit; parent receipt required.");
            if(!Same(state.Random,random.Capture()))throw new InvalidOperationException("Random stream does not match war state.");
            if(policy==WarInterferencePolicy.CampDavidPreventsArabIsraeli)return new WarEventPlan(state,state,null,null,live,true);

            // Validate every possible reward before the one draw, including a winning capture that might overflow.
            var maximum=WarEventRules.Calculate(catalog,card,state.Countries,controller,target,catalog.Policy.DieSides);var owner=maximum.Beneficiary;
            var credit=new MilitaryCredit(creditId,state.Military.Turn.Number,owner,MilitaryCreditKind.WarEvent,MilitaryCreditBasis.VerifiedCardParameter,catalog.Policy.MilitaryCredit,effect.EffectId);
            var military=MilitaryOperationsService.PlanCredit(state.Military,credit);
            if(military.AlreadyApplied)throw new InvalidOperationException("Existing war credit without parent receipt.");
            var maximumAward=maximum.Success?catalog.Policy.VictoryVp:0;
            VictoryService.Evaluate(state.Victory,new ClosedVpBatchEvidence(resolutionId,effect.EffectId,state.VpForUS,owner==PlayerSide.US?maximumAward:0,owner==PlayerSide.USSR?maximumAward:0));
            var fork=random.Fork();
            if(fork==null || ReferenceEquals(fork,random) || !Same(state.Random,fork.Capture()))throw new InvalidOperationException("Independent matching random fork required.");
            var sample=fork.NextInt(0,catalog.Policy.DieSides);
            if(sample<0 || sample>=catalog.Policy.DieSides)throw new InvalidOperationException("War die sample out of range.");
            var afterRandom=fork.Capture();
            if(afterRandom==null || afterRandom.AlgorithmId!=state.Random.AlgorithmId || afterRandom.Increment!=state.Random.Increment || afterRandom.DrawCount<=state.Random.DrawCount)throw new InvalidOperationException("War random stream did not advance correctly.");
            var calculation=WarEventRules.Calculate(catalog,card,state.Countries,controller,target,checked(sample+1));
            var award=calculation.Success?catalog.Policy.VictoryVp:0;
            var decision=VictoryService.Evaluate(state.Victory,new ClosedVpBatchEvidence(resolutionId,effect.EffectId,state.VpForUS,owner==PlayerSide.US?award:0,owner==PlayerSide.USSR?award:0));
            var countries=state.Countries.ToDictionary(p=>p.Key,p=>p.Value);countries[target]=calculation.After;
            var after=new WarEventState(catalog,countries,military.After,state.Defcon,state.Victory,decision.SignedVp,afterRandom);
            return new WarEventPlan(state,after,calculation,credit,decision,false);
        }
        static bool Same(RandomSnapshot a,RandomSnapshot b)=>a!=null && b!=null && a.AlgorithmId==b.AlgorithmId && a.State==b.State && a.Increment==b.Increment && a.DrawCount==b.DrawCount;
    }
}
