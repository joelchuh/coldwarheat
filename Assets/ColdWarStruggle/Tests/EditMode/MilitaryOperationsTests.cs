using System;
using System.Collections.Generic;
using ColdWarStruggle.Core;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class MilitaryOperationsTests
    {
        public static Dictionary<MilitaryCreditKind,MilitaryCreditBasis> Sources()=>new Dictionary<MilitaryCreditKind,MilitaryCreditBasis>
        {
            [MilitaryCreditKind.OperationsCoup]=MilitaryCreditBasis.EffectiveMilitaryOperationsCredit,
            [MilitaryCreditKind.WarEvent]=MilitaryCreditBasis.VerifiedCardParameter,
            [MilitaryCreditKind.DirectEventFixedCredit]=MilitaryCreditBasis.VerifiedCardParameter,
            [MilitaryCreditKind.FreeCoup]=MilitaryCreditBasis.None,[MilitaryCreditKind.Realignment]=MilitaryCreditBasis.None,
            [MilitaryCreditKind.Influence]=MilitaryCreditBasis.None,[MilitaryCreditKind.Space]=MilitaryCreditBasis.None
        };
        public static MilitaryOperationsDefinition Definition()=>new MilitaryOperationsDefinition(0,0,1,Sources());
        public static MilitaryOperationsState State(MilitaryOperationsDefinition d=null)=>MilitaryOperationsState.Create(d??Definition(),TurnCalendarTests.Calendar().ForTurn(1));
        public static MilitaryCredit Credit(string id,int amount,PlayerSide side=PlayerSide.US,int turn=1,MilitaryCreditKind kind=MilitaryCreditKind.OperationsCoup,
            MilitaryCreditBasis basis=MilitaryCreditBasis.EffectiveMilitaryOperationsCredit,string rule="rule.coup")
            =>new MilitaryCredit(id,turn,side,kind,basis,amount,rule);
        public static MilitaryOperationsState Earned(int us,int ussr,MilitaryOperationsDefinition definition=null)
        {
            var s=State(definition);
            if(us>0)s=MilitaryOperationsService.PlanCredit(s,Credit("credit.us",us)).After;
            if(ussr>0)s=MilitaryOperationsService.PlanCredit(s,Credit("credit.ussr",ussr,PlayerSide.USSR)).After;
            return s;
        }
        [TestCase(MilitaryCreditKind.OperationsCoup,MilitaryCreditBasis.EffectiveMilitaryOperationsCredit)]
        [TestCase(MilitaryCreditKind.WarEvent,MilitaryCreditBasis.VerifiedCardParameter)]
        [TestCase(MilitaryCreditKind.DirectEventFixedCredit,MilitaryCreditBasis.VerifiedCardParameter)]
        public void Only_verified_amount_and_beneficiary_are_used(MilitaryCreditKind kind,MilitaryCreditBasis basis)
        {
            var before=State();var p=MilitaryOperationsService.PlanCredit(before,Credit("award",7,PlayerSide.USSR,1,kind,basis));
            Assert.That(p.After.UssrEarned,Is.EqualTo(7));Assert.That(p.After.UsEarned,Is.Zero);Assert.That(before.UssrEarned,Is.Zero);
            Assert.That(p.AmountApplied,Is.EqualTo(7));Assert.That(p.AlreadyApplied,Is.False);Assert.That(p.After.Turn,Is.SameAs(before.Turn));
        }
        [TestCase(MilitaryCreditKind.FreeCoup)] [TestCase(MilitaryCreditKind.Realignment)]
        [TestCase(MilitaryCreditKind.Influence)] [TestCase(MilitaryCreditKind.Space)]
        public void Noncredit_sources_cannot_award_military_points(MilitaryCreditKind kind)
        {
            var s=State();var p=MilitaryOperationsService.PlanCredit(s,Credit("none",0,PlayerSide.US,1,kind,MilitaryCreditBasis.None));
            Assert.That(p.After.UsEarned,Is.Zero);Assert.That(p.After.UssrEarned,Is.Zero);Assert.That(p.AmountApplied,Is.Zero);
            Assert.That(()=>Credit("bad",1,PlayerSide.US,1,kind,MilitaryCreditBasis.None),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>MilitaryOperationsService.PlanCredit(s,Credit("wrong",1,PlayerSide.US,1,kind)),Throws.InstanceOf<ArgumentException>());
        }
        [Test] public void Same_credit_facts_are_idempotent_even_after_assessment()
        {
            var first=MilitaryOperationsService.PlanCredit(State(),Credit("stable",2));
            var retry=MilitaryOperationsService.PlanCredit(first.After,Credit("stable",2));
            Assert.That(retry.After,Is.SameAs(first.After));Assert.That(retry.AmountApplied,Is.Zero);Assert.That(retry.AlreadyApplied,Is.True);
            var end=MilitaryOperationsService.AssessTurnEnd(first.After,"assess",new DefconState(DefconTests.Definition(),4));
            var late=MilitaryOperationsService.PlanCredit(end.After,Credit("stable",2));
            Assert.That(late.After,Is.SameAs(end.After));Assert.That(late.AmountApplied,Is.Zero);Assert.That(late.After.UsEarned,Is.Zero);
        }
        [TestCase("amount")] [TestCase("side")] [TestCase("kind")] [TestCase("basis")] [TestCase("source")] [TestCase("turn")]
        public void Conflicting_credit_id_or_wrong_turn_is_rejected_without_mutation(string mutation)
        {
            var s=MilitaryOperationsService.PlanCredit(State(),Credit("same",2)).After;
            var changed=Credit("same",mutation=="amount"?3:2,mutation=="side"?PlayerSide.USSR:PlayerSide.US,mutation=="turn"?2:1,
                mutation=="kind"?MilitaryCreditKind.WarEvent:MilitaryCreditKind.OperationsCoup,
                mutation=="basis"?MilitaryCreditBasis.VerifiedCardParameter:MilitaryCreditBasis.EffectiveMilitaryOperationsCredit,
                mutation=="source"?"rule.other":"rule.coup");
            Assert.That(()=>MilitaryOperationsService.PlanCredit(s,changed),Throws.InstanceOf<ArgumentException>());
            Assert.That(s.UsEarned,Is.EqualTo(2));Assert.That(s.AppliedCredits.Count,Is.EqualTo(1));
        }
        [TestCase(MilitaryCreditKind.WarEvent,MilitaryCreditKind.DirectEventFixedCredit)]
        [TestCase(MilitaryCreditKind.DirectEventFixedCredit,MilitaryCreditKind.WarEvent)]
        public void Same_id_different_kind_is_rejected_even_when_both_sources_are_valid(MilitaryCreditKind firstKind,MilitaryCreditKind secondKind)
        {
            var first=Credit("same.valid-source",2,kind:firstKind,basis:MilitaryCreditBasis.VerifiedCardParameter,rule:"rule.fixture");
            var conflicting=Credit("same.valid-source",2,kind:secondKind,basis:MilitaryCreditBasis.VerifiedCardParameter,rule:"rule.fixture");
            // Each request is valid in a fresh state: rejection below must not be the source/basis gate.
            Assert.That(MilitaryOperationsService.PlanCredit(State(),conflicting).AmountApplied,Is.EqualTo(2));
            var state=MilitaryOperationsService.PlanCredit(State(),first).After;
            Assert.Throws<ArgumentException>(()=>MilitaryOperationsService.PlanCredit(state,conflicting));
            Assert.That(state.UsEarned,Is.EqualTo(2));Assert.That(state.AppliedCredits.Count,Is.EqualTo(1));
            Assert.That(state.AppliedCredits[first.Id],Is.SameAs(first));
        }
        [TestCase("id")] [TestCase("long-id")] [TestCase("turn")] [TestCase("side")]
        [TestCase("kind")] [TestCase("basis")] [TestCase("amount")] [TestCase("source")] [TestCase("long-source")]
        public void Invalid_credit_facts_are_rejected(string mutation)
            =>Assert.That(()=>new MilitaryCredit(mutation=="id"?" ":mutation=="long-id"?new string('x',257):"c",
                mutation=="turn"?0:1,mutation=="side"?(PlayerSide)99:PlayerSide.US,
                mutation=="kind"?(MilitaryCreditKind)99:MilitaryCreditKind.OperationsCoup,
                mutation=="basis"?(MilitaryCreditBasis)99:MilitaryCreditBasis.EffectiveMilitaryOperationsCredit,
                mutation=="amount"?-1:1,mutation=="source"?"":mutation=="long-source"?new string('x',257):"rule.coup"),Throws.InstanceOf<ArgumentException>());
        [Test] public void State_and_definition_are_readonly_and_reject_missing_context()
        {
            var sources=Sources();var d=new MilitaryOperationsDefinition(0,0,1,sources);sources.Clear();
            Assert.That(d.CreditSources.Count,Is.EqualTo(7));
            Assert.Throws<NotSupportedException>(()=>((IDictionary<MilitaryCreditKind,MilitaryCreditBasis>)d.CreditSources).Clear());
            var s=MilitaryOperationsService.PlanCredit(State(d),Credit("c",1)).After;
            Assert.Throws<NotSupportedException>(()=>((IDictionary<string,MilitaryCredit>)s.AppliedCredits).Clear());
            Assert.Throws<ArgumentNullException>(()=>MilitaryOperationsState.Create(null,s.Turn));
            Assert.Throws<ArgumentNullException>(()=>MilitaryOperationsState.Create(d,null));
            Assert.Throws<ArgumentNullException>(()=>MilitaryOperationsService.PlanCredit(null,Credit("c",1)));
            Assert.Throws<ArgumentNullException>(()=>MilitaryOperationsService.PlanCredit(s,null));
        }
        [TestCase("initial")] [TestCase("reset")] [TestCase("penalty")] [TestCase("null-sources")]
        [TestCase("missing-kind")] [TestCase("unknown-kind")] [TestCase("unknown-basis")]
        public void Invalid_definitions_fail(string mutation)
        {
            var sources=Sources();
            if(mutation=="missing-kind")sources.Remove(MilitaryCreditKind.Space);
            if(mutation=="unknown-kind")sources[(MilitaryCreditKind)99]=MilitaryCreditBasis.None;
            if(mutation=="unknown-basis")sources[MilitaryCreditKind.Space]=(MilitaryCreditBasis)99;
            Assert.That(()=>new MilitaryOperationsDefinition(mutation=="initial"?-1:0,mutation=="reset"?-1:0,mutation=="penalty"?0:1,
                mutation=="null-sources"?null:sources),Throws.InstanceOf<ArgumentException>());
        }
        [TestCase(4,2,1,1)] [TestCase(4,2,2,0)] [TestCase(2,2,2,0)]
        [TestCase(5,7,6,0)] [TestCase(3,0,3,-3)] [TestCase(3,3,0,3)]
        public void Assessment_uses_one_current_snapshot_nets_both_sides_and_resets(int level,int us,int ussr,int delta)
        {
            var s=Earned(us,ussr);var d=new DefconState(DefconTests.Definition(),level);
            var plan=MilitaryOperationsService.AssessTurnEnd(s,"turn1.assessment",d);
            Assert.That(plan.VpDeltaToApply,Is.EqualTo(delta));Assert.That(plan.Assessment.NetVpForUS,Is.EqualTo(delta));
            Assert.That(plan.Assessment.UsShortfall,Is.EqualTo(Math.Max(0,level-us)));Assert.That(plan.Assessment.UssrShortfall,Is.EqualTo(Math.Max(0,level-ussr)));
            Assert.That(plan.Assessment.UsEarned,Is.EqualTo(us));Assert.That(plan.Assessment.UssrEarned,Is.EqualTo(ussr));
            Assert.That(plan.Assessment.Defcon,Is.SameAs(d));Assert.That(plan.After.Assessment,Is.SameAs(plan.Assessment));
            Assert.That(plan.After.UsEarned,Is.Zero);Assert.That(plan.After.UssrEarned,Is.Zero);
            Assert.That(s.UsEarned,Is.EqualTo(us));Assert.That(s.Assessment,Is.Null);Assert.That(plan.AlreadyApplied,Is.False);
        }
        [Test] public void Retrying_assessment_does_not_apply_net_again_or_reassess_zeroed_totals()
        {
            var d=new DefconState(DefconTests.Definition(),4);var first=MilitaryOperationsService.AssessTurnEnd(Earned(2,1),"stable",d);
            var retry=MilitaryOperationsService.AssessTurnEnd(first.After,"stable",new DefconState(d.Definition,4));
            Assert.That(retry.VpDeltaToApply,Is.Zero);Assert.That(retry.AlreadyApplied,Is.True);Assert.That(retry.After,Is.SameAs(first.After));
            Assert.That(retry.Assessment,Is.SameAs(first.Assessment));Assert.That(retry.Assessment.NetVpForUS,Is.EqualTo(1));
            Assert.That(()=>MilitaryOperationsService.AssessTurnEnd(first.After,"new",d),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>MilitaryOperationsService.AssessTurnEnd(first.After,"stable",new DefconState(d.Definition,3)),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>MilitaryOperationsService.AssessTurnEnd(first.After,"stable",new DefconState(DefconTests.Definition(),4)),Throws.InstanceOf<ArgumentException>());
            Assert.Throws<InvalidOperationException>(()=>MilitaryOperationsService.PlanCredit(first.After,Credit("later",1)));
        }
        [Test] public void Overflow_does_not_publish_partial_credit_or_assessment()
        {
            var full=Earned(int.MaxValue,0);
            Assert.Throws<OverflowException>(()=>MilitaryOperationsService.PlanCredit(full,Credit("overflow",1)));
            Assert.That(full.UsEarned,Is.EqualTo(int.MaxValue));Assert.That(full.AppliedCredits.Count,Is.EqualTo(1));
            var s=Earned(5,0,new MilitaryOperationsDefinition(0,0,int.MaxValue,Sources()));
            Assert.Throws<OverflowException>(()=>MilitaryOperationsService.AssessTurnEnd(s,"overflow",new DefconState(DefconTests.Definition(),5)));
            Assert.That(s.Assessment,Is.Null);Assert.That(s.UsEarned,Is.EqualTo(5));
        }
        [Test] public void Definition_values_drive_initial_reset_and_penalty()
        {
            var d=new MilitaryOperationsDefinition(1,2,2,Sources());var s=MilitaryOperationsService.PlanCredit(State(d),Credit("plus",3)).After;
            Assert.That(s.UsEarned,Is.EqualTo(4));Assert.That(s.UssrEarned,Is.EqualTo(1));
            var p=MilitaryOperationsService.AssessTurnEnd(s,"assess",new DefconState(DefconTests.Definition(),5));
            Assert.That(p.VpDeltaToApply,Is.EqualTo(6));Assert.That(p.After.UsEarned,Is.EqualTo(2));Assert.That(p.After.UssrEarned,Is.EqualTo(2));
        }
        [Test] public void Invalid_or_nuclear_assessment_inputs_are_rejected()
        {
            var s=State();var d=new DefconState(DefconTests.Definition(),5);
            Assert.Throws<ArgumentNullException>(()=>MilitaryOperationsService.AssessTurnEnd(null,"a",d));
            Assert.Throws<ArgumentNullException>(()=>MilitaryOperationsService.AssessTurnEnd(s,"a",null));
            Assert.That(()=>MilitaryOperationsService.AssessTurnEnd(s," ",d),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>MilitaryOperationsService.AssessTurnEnd(s,new string('a',257),d),Throws.InstanceOf<ArgumentException>());
            Assert.Throws<InvalidOperationException>(()=>MilitaryOperationsService.AssessTurnEnd(s,"a",new DefconState(d.Definition,1)));
            Assert.That(s.Assessment,Is.Null);
        }
    }
}
