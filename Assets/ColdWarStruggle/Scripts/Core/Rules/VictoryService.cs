using System;
using System.Linq;
namespace ColdWarStruggle.Core
{
    // A pure decision, not a committed GameResult or an instruction to add VP repeatedly.
    public sealed class VictoryDecision
    {
        public VictoryOutcome Outcome { get; }
        public VictoryCheckpoint Checkpoint { get; }
        public string CheckpointId { get; }
        public string SourceRuleId { get; }
        public int SignedVp { get; }
        public PlayerSide? Winner { get; }
        public PendingMilitaryVictory PendingMilitary { get; }
        public string HeldScoringRowId { get; }
        public string Authority { get; }
        internal VictoryDecision(VictoryEvidence evidence,int vp,VictoryOutcome outcome,PlayerSide? winner=null,
            PendingMilitaryVictory pending=null,HeldScoringRow row=null)
        {
            Checkpoint=evidence.Checkpoint;CheckpointId=evidence.Id;SourceRuleId=evidence.SourceRuleId;
            SignedVp=vp;Outcome=outcome;Winner=winner;PendingMilitary=pending;HeldScoringRowId=row?.Id;Authority=row?.Authority;
        }
    }
    public static class VictoryService
    {
        // Evidence is produced by trusted completed rule steps. The host owns ordering, deduplication and terminal absorption.
        public static VictoryDecision Evaluate(VictoryDefinition definition,VictoryEvidence evidence)
        {
            if(definition==null || evidence==null)throw new ArgumentNullException();
            switch(evidence)
            {
                case ClosedVpBatchEvidence batch:
                    RequireLiveScore(definition,batch.BeforeSignedVp);
                    int vp=checked((int)((long)batch.BeforeSignedVp+batch.UsAward-batch.UssrAward));
                    var winner=Candidate(definition,vp);
                    return new VictoryDecision(batch,vp,winner.HasValue?VictoryOutcome.Finished:VictoryOutcome.Continue,winner);
                case MilitaryAssessedEvidence military:
                    RequireLiveScore(definition,military.BeforeSignedVp);
                    int militaryVp=checked(military.BeforeSignedVp+military.Assessment.NetVpForUS);
                    var pending=new PendingMilitaryVictory(definition,military,militaryVp,Candidate(definition,militaryVp));
                    return new VictoryDecision(military,militaryVp,VictoryOutcome.Deferred,pending:pending);
                case HeldScoringAuditedEvidence audit:
                    if(!ReferenceEquals(audit.Pending.Definition,definition))throw new ArgumentException("Pending military decision belongs to another definition.");
                    var row=definition.HeldScoringRows.Single(r=>r.UsHolds==audit.UsHolds && r.UssrHolds==audit.UssrHolds
                        && r.MilitaryCandidate==audit.Pending.Candidate);
                    return new VictoryDecision(audit,audit.Pending.SignedVp,row.Outcome,row.Winner,row:row);
                case NuclearWarEvidence nuclear:
                    return new VictoryDecision(nuclear,nuclear.SignedVp,VictoryOutcome.Finished,
                        nuclear.Change.PhasingPlayer==PlayerSide.US?PlayerSide.USSR:PlayerSide.US);
                default:throw new ArgumentException("Unsupported victory checkpoint.");
            }
        }
        static PlayerSide? Candidate(VictoryDefinition d,int vp)
            =>vp>=d.AutomaticVpThreshold?PlayerSide.US:vp<=-(long)d.AutomaticVpThreshold?PlayerSide.USSR:(PlayerSide?)null;
        static void RequireLiveScore(VictoryDefinition d,int vp)
        {
            if(Candidate(d,vp).HasValue)throw new ArgumentException("An ordinary or military batch cannot start with an already terminal score.");
        }
    }
}
