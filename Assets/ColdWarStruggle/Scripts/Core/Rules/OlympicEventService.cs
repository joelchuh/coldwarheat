using System;
namespace ColdWarStruggle.Core
{
    // Trusted effect evidence; never a flag chosen by a player command.
    public enum OlympicInterferencePolicy { NoActiveInterference }
    public sealed class OlympicEventState
    {
        public OlympicGamesDefinition Definition { get; }
        public DefconState Defcon { get; }
        public VictoryDefinition Victory { get; }
        public int VpForUS { get; }
        public RandomSnapshot Random { get; }
        public OlympicEventState(OlympicGamesDefinition definition,DefconState defcon,VictoryDefinition victory,int vp,RandomSnapshot random)
        {
            if(definition==null || defcon==null || victory==null || random==null)throw new ArgumentNullException();
            Definition=definition;Defcon=defcon;Victory=victory;VpForUS=vp;Random=random;
        }
    }
    public sealed class OlympicPairPlan
    {
        public OlympicEventState Before { get; }
        public OlympicEventState After { get; }
        public OlympicContestResult Calculation { get; }
        public VictoryDecision Decision { get; }
        public bool RequiresAnotherPair => Calculation.Tie;
        internal OlympicPairPlan(OlympicEventState before,OlympicEventState after,OlympicContestResult calculation,VictoryDecision decision)
        {Before=before;After=after;Calculation=calculation;Decision=decision;}
    }
    // A proposed event budget only. This is not an authorization to execute operations.
    public sealed class OlympicOperationsOffer
    {
        public PlayerSide Actor { get; }
        public int BaseOps { get; }
        internal OlympicOperationsOffer(PlayerSide actor,int baseOps){Actor=actor;BaseOps=baseOps;}
    }
    public sealed class OlympicBoycottPlan
    {
        public OlympicEventState Before { get; }
        public OlympicEventState After { get; }
        public DefconChangePlan Change { get; }
        public VictoryDecision Decision { get; }
        public OlympicOperationsOffer Operations { get; }
        internal OlympicBoycottPlan(OlympicEventState before,OlympicEventState after,DefconChangePlan change,VictoryDecision decision,OlympicOperationsOffer operations)
        {Before=before;After=after;Change=change;Decision=decision;Operations=operations;}
    }
    public static class OlympicEventService
    {
        // One pair only. The parent owns source/choice authorization, receipts, atomic commit and internal tie scheduling.
        public static OlympicPairPlan PlanPair(OlympicEventState state,PlayerSide sponsor,string resolutionId,OlympicInterferencePolicy policy,IRandomProvider random)
        {
            if(random==null)throw new ArgumentNullException(nameof(random));
            var live=Check(state,sponsor,resolutionId,policy);var d=state.Definition;
            if(!Same(state.Random,random.Capture()))throw new InvalidOperationException("Olympic random cursor does not match state.");
            // Both possible winners are checked before any sampling, including integer overflow.
            var usWin=VictoryService.Evaluate(state.Victory,new ClosedVpBatchEvidence(resolutionId,d.EffectId,state.VpForUS,d.WinnerVp,0));
            var suWin=VictoryService.Evaluate(state.Victory,new ClosedVpBatchEvidence(resolutionId,d.EffectId,state.VpForUS,0,d.WinnerVp));
            var fork=random.Fork();
            if(fork==null || ReferenceEquals(fork,random) || !Same(state.Random,fork.Capture()))throw new InvalidOperationException("Independent matching Olympic random fork required.");
            var us=DrawFace(d.Die,fork,state.Random,out var afterUs);
            var su=DrawFace(d.Die,fork,afterUs,out var afterPair);
            var result=OlympicContestRules.Calculate(d,sponsor,us,su);
            var decision=result.Tie?live:result.Winner==PlayerSide.US?usWin:suWin;
            var after=new OlympicEventState(d,state.Defcon,state.Victory,decision.SignedVp,afterPair);
            return new OlympicPairPlan(state,after,result,decision);
        }
        public static OlympicBoycottPlan PlanBoycott(OlympicEventState state,PlayerSide sponsor,PlayerSide rootPhasing,string resolutionId,OlympicInterferencePolicy policy)
        {
            var live=Check(state,sponsor,resolutionId,policy);OlympicContestRules.RequireSide(rootPhasing);var d=state.Definition;
            var change=DefconService.PlanChange(state.Defcon,DefconChangeKind.DegradeOne,rootPhasing);
            var decision=change.NuclearWar?VictoryService.Evaluate(state.Victory,new NuclearWarEvidence(resolutionId,d.EffectId,state.VpForUS,change)):live;
            var offer=change.NuclearWar?null:new OlympicOperationsOffer(sponsor,d.BoycottBaseOps);
            var after=new OlympicEventState(d,change.After,state.Victory,decision.SignedVp,state.Random);
            return new OlympicBoycottPlan(state,after,change,decision,offer);
        }
        static VictoryDecision Check(OlympicEventState state,PlayerSide sponsor,string resolutionId,OlympicInterferencePolicy policy)
        {
            if(state==null)throw new ArgumentNullException(nameof(state));
            OlympicContestRules.RequireSide(sponsor);VictoryGuard.Text(resolutionId);
            if(policy!=OlympicInterferencePolicy.NoActiveInterference || state.Defcon.Level==state.Defcon.Definition.NuclearLevel)throw new ArgumentException("Unknown Olympic interference or terminal DEFCON.");
            return VictoryService.Evaluate(state.Victory,new ClosedVpBatchEvidence(resolutionId,state.Definition.EffectId,state.VpForUS,0,0));
        }
        static int DrawFace(StandardDieDefinition die,IRandomProvider fork,RandomSnapshot before,out RandomSnapshot after)
        {
            var width=checked(die.Maximum-die.Minimum+1);var sample=fork.NextInt(0,width);
            if(sample<0 || sample>=width)throw new InvalidOperationException("Olympic die sample outside shared faces.");
            after=fork.Capture();
            if(after==null || after.AlgorithmId!=before.AlgorithmId || after.Increment!=before.Increment || after.DrawCount<=before.DrawCount)throw new InvalidOperationException("Olympic random stream did not advance correctly.");
            return checked(sample+die.Minimum);
        }
        static bool Same(RandomSnapshot a,RandomSnapshot b)=>a!=null && b!=null && a.AlgorithmId==b.AlgorithmId && a.State==b.State && a.Increment==b.Increment && a.DrawCount==b.DrawCount;
    }
}
