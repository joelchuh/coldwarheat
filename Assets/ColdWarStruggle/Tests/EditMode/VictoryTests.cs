using System;
using System.Collections.Generic;
using System.Linq;
using ColdWarStruggle.Core;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class VictoryTests
    {
        public static PlayerSide? Side(string side)=>side==null?(PlayerSide?)null:side=="US"?PlayerSide.US:PlayerSide.USSR;
        public static List<HeldScoringRow> Rows()
        {
            var rows=new List<HeldScoringRow>();int n=0;
            foreach(bool us in new[]{false,true})foreach(bool ussr in new[]{false,true})foreach(var c in new PlayerSide?[]{null,PlayerSide.US,PlayerSide.USSR})
            {
                PlayerSide? winner=us && ussr?(c==PlayerSide.US?PlayerSide.USSR:PlayerSide.US):us?PlayerSide.USSR:ussr?PlayerSide.US:c;
                rows.Add(new HeldScoringRow("fixture."+(++n),us,ussr,c,winner.HasValue?VictoryOutcome.Finished:VictoryOutcome.Continue,winner,"test.fixture","adopted"));
            }
            return rows;
        }
        public static VictoryDefinition Definition()=>new VictoryDefinition(0,20,Rows());
        public static MilitaryTurnEndAssessment Assessment(int us=4,int ussr=4)
            =>MilitaryOperationsService.AssessTurnEnd(MilitaryOperationsTests.Earned(us,ussr),"assessment",new DefconState(DefconTests.Definition(),4)).Assessment;
        public static PendingMilitaryVictory Pending(VictoryDefinition d,string side=null)
            =>VictoryService.Evaluate(d,new MilitaryAssessedEvidence("mil","R2015",side=="US"?19:side=="USSR"?-19:0,
                side=="US"?Assessment(2,1):side=="USSR"?Assessment(1,2):Assessment())).PendingMilitary;

        [TestCase(false,false,null,null)] [TestCase(false,false,"US","US")] [TestCase(false,false,"USSR","USSR")]
        [TestCase(true,false,null,"USSR")] [TestCase(true,false,"US","USSR")] [TestCase(true,false,"USSR","USSR")]
        [TestCase(false,true,null,"US")] [TestCase(false,true,"US","US")] [TestCase(false,true,"USSR","US")]
        [TestCase(true,true,null,"US")] [TestCase(true,true,"US","USSR")] [TestCase(true,true,"USSR","US")]
        public void Whole_matrix_is_evaluated_including_user_CF003(bool us,bool ussr,string candidate,string winner)
        {
            var d=Definition();var pending=Pending(d,candidate);
            var r=VictoryService.Evaluate(d,new HeldScoringAuditedEvidence("audit","held",pending,us,ussr));
            Assert.That(r.Winner,Is.EqualTo(Side(winner)));
            Assert.That(r.Outcome,Is.EqualTo(winner==null?VictoryOutcome.Continue:VictoryOutcome.Finished));
            Assert.That(r.SignedVp,Is.EqualTo(candidate=="US"?20:candidate=="USSR"?-20:0));
            Assert.That(r.PendingMilitary,Is.Null);Assert.That(r.HeldScoringRowId,Is.Not.Empty);
            Assert.That(r.Checkpoint,Is.EqualTo(VictoryCheckpoint.HeldScoringAudited));Assert.That(r.CheckpointId,Is.EqualTo("audit"));
        }
        [TestCase(19,2,3,18,null)] [TestCase(-19,0,1,-20,"USSR")] [TestCase(19,4,0,23,"US")]
        [TestCase(-19,3,2,-18,null)] [TestCase(0,0,0,0,null)] [TestCase(19,int.MaxValue,int.MaxValue,19,null)]
        public void Only_complete_net_batch_checks_threshold_without_clamping(int before,int us,int ussr,int after,string winner)
        {
            var d=Definition();var e=new ClosedVpBatchEvidence("batch","event",before,us,ussr);var r=VictoryService.Evaluate(d,e);
            Assert.That(r.SignedVp,Is.EqualTo(after));Assert.That(r.Winner,Is.EqualTo(Side(winner)));
            Assert.That(r.Outcome,Is.EqualTo(winner==null?VictoryOutcome.Continue:VictoryOutcome.Finished));Assert.That(r.PendingMilitary,Is.Null);
            Assert.That(e.BeforeSignedVp,Is.EqualTo(before));Assert.That(r.SourceRuleId,Is.EqualTo("event"));
        }
        [TestCase(null)] [TestCase("US")] [TestCase("USSR")]
        public void Every_military_assessment_is_deferred_until_private_audit(string candidate)
        {
            var d=Definition();var before=candidate=="US"?19:candidate=="USSR"?-19:0;
            var a=candidate=="US"?Assessment(2,1):candidate=="USSR"?Assessment(1,2):Assessment();
            var r=VictoryService.Evaluate(d,new MilitaryAssessedEvidence("mil","military",before,a));
            Assert.That(r.Outcome,Is.EqualTo(VictoryOutcome.Deferred));Assert.That(r.Winner,Is.Null);
            Assert.That(r.PendingMilitary.Candidate,Is.EqualTo(Side(candidate)));Assert.That(r.PendingMilitary.Definition,Is.SameAs(d));
            Assert.That(r.PendingMilitary.Assessment,Is.SameAs(a));Assert.That(r.PendingMilitary.CheckpointId,Is.EqualTo("mil"));
        }
        [TestCase(PlayerSide.US,PlayerSide.USSR)] [TestCase(PlayerSide.USSR,PlayerSide.US)]
        public void Nuclear_war_loser_comes_from_phasing_player(PlayerSide phasing,PlayerSide winner)
        {
            var change=DefconService.PlanChange(new DefconState(DefconTests.Definition(),2),DefconChangeKind.DegradeOne,phasing);
            var r=VictoryService.Evaluate(Definition(),new NuclearWarEvidence("nuclear","coup",3,change));
            Assert.That(r.Winner,Is.EqualTo(winner));Assert.That(r.Outcome,Is.EqualTo(VictoryOutcome.Finished));Assert.That(r.SignedVp,Is.EqualTo(3));
        }
        [Test] public void Rows_are_copied_readonly_and_changed_shared_outcome_is_used()
        {
            var rows=Rows();var old=rows.Single(x=>x.UsHolds && x.UssrHolds && x.MilitaryCandidate==null);
            rows.Remove(old);rows.Add(new HeldScoringRow(old.Id,true,true,null,VictoryOutcome.Finished,PlayerSide.USSR,"synthetic","adopted"));
            var d=new VictoryDefinition(0,20,rows);rows.Clear();
            var r=VictoryService.Evaluate(d,new HeldScoringAuditedEvidence("audit","held",Pending(d),true,true));
            Assert.That(r.Winner,Is.EqualTo(PlayerSide.USSR));Assert.That(r.Authority,Is.EqualTo("synthetic"));
            Assert.That(d.HeldScoringRows.Count,Is.EqualTo(12));Assert.Throws<NotSupportedException>(()=>((IList<HeldScoringRow>)d.HeldScoringRows).Clear());
        }
        [Test] public void Pending_row_is_unresolved_never_a_draw_or_guessed_winner()
        {
            var rows=Rows();var old=rows.Single(x=>!x.UsHolds && !x.UssrHolds && x.MilitaryCandidate==null);
            rows.Remove(old);rows.Add(new HeldScoringRow(old.Id,false,false,null,VictoryOutcome.UnresolvedAdjudication,null,"pending-review","pending"));
            var d=new VictoryDefinition(0,20,rows);var r=VictoryService.Evaluate(d,new HeldScoringAuditedEvidence("audit","held",Pending(d),false,false));
            Assert.That(r.Outcome,Is.EqualTo(VictoryOutcome.UnresolvedAdjudication));Assert.That(r.Winner,Is.Null);
        }
        [Test] public void Shared_threshold_and_initial_score_are_not_recreated_constants()
        {
            var d=new VictoryDefinition(1,7,Rows());Assert.That(d.InitialSignedVp,Is.EqualTo(1));
            Assert.That(VictoryService.Evaluate(d,new ClosedVpBatchEvidence("b","s",6,1,0)).Winner,Is.EqualTo(PlayerSide.US));
        }
        [TestCase("threshold")] [TestCase("initial")] [TestCase("null-rows")] [TestCase("missing-row")]
        [TestCase("extra-row")] [TestCase("duplicate-condition")] [TestCase("duplicate-id")] [TestCase("null-row")]
        public void Definition_rejects_invalid_or_incomplete_matrix(string mutation)
        {
            var rows=Rows();int initial=0,threshold=20;
            if(mutation=="threshold")threshold=0;if(mutation=="initial")initial=20;if(mutation=="null-rows")rows=null;
            if(mutation=="missing-row")rows.RemoveAt(0);if(mutation=="extra-row")rows.Add(rows[0]);
            if(mutation=="duplicate-condition")rows[1]=new HeldScoringRow("unique",false,false,null,VictoryOutcome.Continue,null,"test","adopted");
            if(mutation=="duplicate-id"){var row=rows[1];rows[1]=new HeldScoringRow(rows[0].Id,row.UsHolds,row.UssrHolds,row.MilitaryCandidate,row.Outcome,row.Winner,row.Authority,row.AdoptionStatus);}
            if(mutation=="null-row")rows[0]=null;
            Assert.That(()=>new VictoryDefinition(initial,threshold,rows),Throws.InstanceOf<ArgumentException>());
        }
        [TestCase("id")] [TestCase("authority")] [TestCase("candidate")] [TestCase("winner")] [TestCase("deferred")]
        [TestCase("finished-no-winner")] [TestCase("continue-winner")] [TestCase("pending-winner")] [TestCase("adoption")] [TestCase("unknown-outcome")]
        public void Rows_reject_bad_types_and_unresolved_winners(string mutation)
        {
            var outcome=mutation=="deferred"?VictoryOutcome.Deferred:mutation=="unknown-outcome"?(VictoryOutcome)999:mutation=="finished-no-winner"?VictoryOutcome.Finished:mutation=="pending-winner"?VictoryOutcome.UnresolvedAdjudication:VictoryOutcome.Continue;
            PlayerSide? winner=mutation=="winner"?(PlayerSide)999:mutation=="continue-winner" || mutation=="pending-winner"?PlayerSide.US:(PlayerSide?)null;
            Assert.That(()=>new HeldScoringRow(mutation=="id"?"":"row",false,false,mutation=="candidate"?(PlayerSide)999:(PlayerSide?)null,outcome,winner,
                mutation=="authority"?"":"test",mutation=="adoption"?"unknown":mutation=="pending-winner"?"pending":"adopted"),Throws.InstanceOf<ArgumentException>());
        }
        [Test] public void Missing_wrong_or_non_nuclear_evidence_is_rejected()
        {
            var d=Definition();
            Assert.That(()=>VictoryService.Evaluate(d,null),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>VictoryService.Evaluate(null,new ClosedVpBatchEvidence("b","s",0,0,0)),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>new ClosedVpBatchEvidence("","s",0,0,0),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>new ClosedVpBatchEvidence("b"," ",0,0,0),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>new ClosedVpBatchEvidence(new string('x',257),"s",0,0,0),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>new ClosedVpBatchEvidence("b","s",0,-1,0),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>new ClosedVpBatchEvidence("b","s",0,0,-1),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>new MilitaryAssessedEvidence("m","s",0,null),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>new HeldScoringAuditedEvidence("h","s",null,false,false),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>new NuclearWarEvidence("n","s",0,null),Throws.InstanceOf<ArgumentException>());
            var nonNuclear=DefconService.PlanChange(new DefconState(DefconTests.Definition(),3),DefconChangeKind.DegradeOne,PlayerSide.US);
            Assert.That(()=>new NuclearWarEvidence("n","s",0,nonNuclear),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>VictoryService.Evaluate(Definition(),new HeldScoringAuditedEvidence("h","s",Pending(d),false,false)),Throws.InstanceOf<ArgumentException>());
        }
        [TestCase(20)] [TestCase(-20)] [TestCase(int.MinValue)] [TestCase(int.MaxValue)]
        public void Already_terminal_prebatch_scores_cannot_be_repaired_by_new_awards(int before)
        {
            var d=Definition();
            Assert.That(()=>VictoryService.Evaluate(d,new ClosedVpBatchEvidence("b","s",before,0,0)),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>VictoryService.Evaluate(d,new MilitaryAssessedEvidence("m","s",before,Assessment())),Throws.InstanceOf<ArgumentException>());
        }
        [Test] public void Integer_overflow_fails_without_changing_evidence()
        {
            var e=new ClosedVpBatchEvidence("b","s",19,int.MaxValue,0);
            Assert.Throws<OverflowException>(()=>VictoryService.Evaluate(Definition(),e));
            Assert.That(e.BeforeSignedVp,Is.EqualTo(19));
        }
    }
}
