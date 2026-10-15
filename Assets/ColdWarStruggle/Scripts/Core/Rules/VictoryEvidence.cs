using System;
namespace ColdWarStruggle.Core
{
    public abstract class VictoryEvidence
    {
        public string Id { get; }
        public string SourceRuleId { get; }
        public VictoryCheckpoint Checkpoint { get; }
        private protected VictoryEvidence(string id,string source,VictoryCheckpoint checkpoint) { Id=VictoryGuard.Text(id);SourceRuleId=VictoryGuard.Text(source);Checkpoint=checkpoint; }
    }
    public sealed class ClosedVpBatchEvidence : VictoryEvidence
    {
        public int BeforeSignedVp { get; }
        public int UsAward { get; }
        public int UssrAward { get; }
        public ClosedVpBatchEvidence(string id,string source,int before,int us,int ussr):base(id,source,VictoryCheckpoint.VpBatchClosed)
        {
            if(us<0 || ussr<0)throw new ArgumentException("Awards must be nonnegative; represent both sides separately.");
            BeforeSignedVp=before;UsAward=us;UssrAward=ussr;
        }
    }
    public sealed class MilitaryAssessedEvidence : VictoryEvidence
    {
        public int BeforeSignedVp { get; }
        public MilitaryTurnEndAssessment Assessment { get; }
        public MilitaryAssessedEvidence(string id,string source,int before,MilitaryTurnEndAssessment assessment):base(id,source,VictoryCheckpoint.MilitaryAssessed)
        { BeforeSignedVp=before;Assessment=assessment??throw new ArgumentNullException(nameof(assessment)); }
    }
    public sealed class PendingMilitaryVictory
    {
        public VictoryDefinition Definition { get; }
        public string CheckpointId { get; }
        public MilitaryTurnEndAssessment Assessment { get; }
        public int SignedVp { get; }
        public PlayerSide? Candidate { get; }
        internal PendingMilitaryVictory(VictoryDefinition definition,MilitaryAssessedEvidence evidence,int vp,PlayerSide? candidate)
        { Definition=definition;CheckpointId=evidence.Id;Assessment=evidence.Assessment;SignedVp=vp;Candidate=candidate; }
    }
    public sealed class HeldScoringAuditedEvidence : VictoryEvidence
    {
        public PendingMilitaryVictory Pending { get; }
        public bool UsHolds { get; }
        public bool UssrHolds { get; }
        public HeldScoringAuditedEvidence(string id,string source,PendingMilitaryVictory pending,bool us,bool ussr):base(id,source,VictoryCheckpoint.HeldScoringAudited)
        { Pending=pending??throw new ArgumentNullException(nameof(pending));UsHolds=us;UssrHolds=ussr; }
    }
    public sealed class NuclearWarEvidence : VictoryEvidence
    {
        public int SignedVp { get; }
        public DefconChangePlan Change { get; }
        public NuclearWarEvidence(string id,string source,int vp,DefconChangePlan change):base(id,source,VictoryCheckpoint.NuclearWarReached)
        {
            if(change==null || !change.NuclearWar || !change.Changed || !change.PhasingPlayer.HasValue)
                throw new ArgumentException("Nuclear evidence requires a responsible degradation plan.");
            SignedVp=vp;Change=change;
        }
    }
    public sealed class ScoringResolvedEvidence : VictoryEvidence
    {
        public int BeforeSignedVp { get; }
        public RegionScoringResult Result { get; }
        public ScoringResolvedEvidence(string id,string source,int before,RegionScoringResult result):base(id,source,VictoryCheckpoint.ScoringResolved)
        { BeforeSignedVp=before;Result=result??throw new ArgumentNullException(nameof(result)); }
    }

    public sealed class FinalScoringCompletedEvidence : VictoryEvidence
    {
        public int BeforeSignedVp { get; }
        public FinalScoringResult Result { get; }
        public FinalScoringCompletedEvidence(string id,string source,int before,FinalScoringResult result):base(id,source,VictoryCheckpoint.FinalScoringCompleted)
        { BeforeSignedVp=before;Result=result??throw new ArgumentNullException(nameof(result)); }
    }

}
