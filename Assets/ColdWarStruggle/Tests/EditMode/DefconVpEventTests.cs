using System;
using System.Linq;
using ColdWarStruggle.Core;
using ColdWarStruggle.Infrastructure;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class DefconVpEventTests
    {
        internal static string Data()=>PlayInfluenceEventTests.Read("Data/Cards/deluxe-2015/defcon-vp-events.parameters.json");
        internal static string Schema()=>PlayInfluenceEventTests.Read("Docs/Contracts/defcon-vp-events.schema.json");
        // Read card numbers from the authoritative parameters; helper is deliberately independent of the new loader.
        internal static DefconVpEffectDefinition Effect(bool duck)
        {
            var p=JObject.Parse(Data())["parameters"][duck?"duck_and_cover":"nuclear_test_ban"];
            return new DefconVpEffectDefinition(new CardId((string)p["card_id"]),(string)p["effect_id"],duck?DefconVpEventKind.DegradeThenUsAward:DefconVpEventKind.ControllerAwardThenImprove,(int)p["defcon_change"],(int)p["vp_constant"]);
        }
        internal static DefconVpEventPlan Plan(bool duck,int level,int vp=0,PlayerSide controller=PlayerSide.USSR,PlayerSide phasing=PlayerSide.USSR)
        {
            var r=PlayCoupTests.Prepared().Practice.Definition.Risk;
            return DefconVpEventService.Plan(Effect(duck),new DefconState(r.Defcon,level),r.Victory,vp,controller,phasing,"event.0",DefconVpEventPolicy.NoActiveInterference);
        }
        [TestCase(5,4,1)][TestCase(4,3,2)][TestCase(3,2,3)]
        public void Duck_degrades_then_awards_US_even_when_USSR_controls_event(int level,int after,int amount)
        {var p=Plan(true,level);Assert.That(p.Before.Level,Is.EqualTo(level));Assert.That(p.After.Level,Is.EqualTo(after));Assert.That(p.Award,Is.EqualTo(amount));Assert.That(p.AwardSide,Is.EqualTo(PlayerSide.US));Assert.That(p.Decision.SignedVp,Is.EqualTo(amount));Assert.That(p.Steps,Is.EqualTo(new[]{DefconVpStep.DefconChanged,DefconVpStep.VpBatchClosed}));Assert.That(p.Decision.Outcome,Is.EqualTo(VictoryOutcome.Continue));}
        [TestCase(PlayerSide.US,PlayerSide.USSR)][TestCase(PlayerSide.USSR,PlayerSide.US)]
        public void Nuclear_duck_stops_before_VP_and_loses_by_root_phasing(PlayerSide phasing,PlayerSide winner)
        {var p=Plan(true,2,19,phasing==PlayerSide.US?PlayerSide.USSR:PlayerSide.US,phasing);Assert.That(p.Award,Is.Null);Assert.That(p.After.Level,Is.EqualTo(1));Assert.That(p.Decision.SignedVp,Is.EqualTo(19));Assert.That(p.Decision.Winner,Is.EqualTo(winner));Assert.That(p.Steps,Is.EqualTo(new[]{DefconVpStep.DefconChanged,DefconVpStep.NuclearWarReached}));}
        [TestCase(2,0,4)][TestCase(3,1,5)][TestCase(4,2,5)][TestCase(5,3,5)]
        public void Test_ban_awards_from_old_level_before_clamped_improvement(int level,int amount,int after)
        {foreach(var side in new[]{PlayerSide.US,PlayerSide.USSR}){var p=Plan(false,level,0,side);Assert.That(p.Award,Is.EqualTo(amount));Assert.That(p.AwardSide,Is.EqualTo(side));Assert.That(p.After.Level,Is.EqualTo(after));Assert.That(p.Decision.SignedVp,Is.EqualTo(side==PlayerSide.US?amount:-amount));Assert.That(p.Steps,Is.EqualTo(new[]{DefconVpStep.VpBatchClosed,DefconVpStep.DefconChanged}));}}
        [TestCase(PlayerSide.US,19,20)][TestCase(PlayerSide.USSR,-19,-20)]
        public void VP_victory_test_ban_does_not_improve_defcon(PlayerSide controller,int before,int after)
        {var p=Plan(false,3,before,controller);Assert.That(p.Decision.SignedVp,Is.EqualTo(after));Assert.That(p.Decision.Winner,Is.EqualTo(controller));Assert.That(p.After,Is.SameAs(p.Before));Assert.That(p.DefconChange,Is.Null);Assert.That(p.Steps,Is.EqualTo(new[]{DefconVpStep.VpBatchClosed}));}
        [Test] public void Duck_can_win_after_degradation_and_preserves_step_order()
        {var p=Plan(true,3,18);Assert.That(p.After.Level,Is.EqualTo(2));Assert.That(p.Decision.SignedVp,Is.EqualTo(21));Assert.That(p.Decision.Winner,Is.EqualTo(PlayerSide.US));Assert.That(p.Steps.Count,Is.EqualTo(2));}
        [Test] public void Invalid_context_terminal_score_and_terminal_defcon_are_rejected()
        {
            var r=PlayCoupTests.Prepared().Practice.Definition.Risk;var d=new DefconState(r.Defcon,5);
            Assert.Throws<ArgumentException>(()=>DefconVpEventService.Plan(Effect(true),d,r.Victory,0,(PlayerSide)99,PlayerSide.US,"id",DefconVpEventPolicy.NoActiveInterference));
            Assert.Throws<ArgumentException>(()=>DefconVpEventService.Plan(Effect(true),d,r.Victory,0,PlayerSide.US,(PlayerSide)99,"id",DefconVpEventPolicy.NoActiveInterference));
            Assert.Throws<ArgumentException>(()=>DefconVpEventService.Plan(Effect(true),d,r.Victory,0,PlayerSide.US,PlayerSide.US,"id",(DefconVpEventPolicy)99));
            Assert.Throws<ArgumentException>(()=>Plan(true,2,20));Assert.Throws<ArgumentException>(()=>Plan(false,1));
        }
        [Test] public void Formula_numeric_overflow_and_negative_awards_do_not_return_a_plan()
        {
            var r=PlayCoupTests.Prepared().Practice.Definition.Risk;var v=new VictoryDefinition(0,int.MaxValue,r.Victory.HeldScoringRows);var d=new DefconState(r.Defcon,5);
            var e=new DefconVpEffectDefinition(new CardId("card.duck_and_cover"),"effect.duck_and_cover",DefconVpEventKind.DegradeThenUsAward,-1,int.MaxValue);
            Assert.Throws<OverflowException>(()=>DefconVpEventService.Plan(e,d,v,10,PlayerSide.US,PlayerSide.US,"id",DefconVpEventPolicy.NoActiveInterference));
            e=new DefconVpEffectDefinition(e.Card,e.EffectId,e.Kind,-1,0);Assert.Throws<ArgumentException>(()=>DefconVpEventService.Plan(e,d,r.Victory,0,PlayerSide.US,PlayerSide.US,"id",DefconVpEventPolicy.NoActiveInterference));Assert.That(d.Level,Is.EqualTo(5));
        }
        [Test] public void Loader_returns_only_two_immediate_effects_and_binds_printed_cards()
        {var d=DefconVpEventLoader.LoadImmediateSubset(Data(),Schema());Assert.That(d.Count,Is.EqualTo(2));var cards=PlayCoupTests.Prepared().Practice.Definition.Cards;var c=new PracticeDefconVpCatalog(cards,d);Assert.That(c.Effects.Keys.Select(x=>x.Value),Is.EquivalentTo(new[]{"card.duck_and_cover","card.nuclear_test_ban"}));foreach(var duck in new[]{true,false}){var e=Effect(duck);Assert.That(c.Effects[e.Card].DefconDelta,Is.EqualTo(e.DefconDelta));Assert.That(c.Effects[e.Card].VpConstant,Is.EqualTo(e.VpConstant));}}
        [Test] public void Loader_rejects_bad_order_formula_source_contract_or_unimplemented_parameter()
        {
            foreach(var edit in new Action<JObject>[] {r=>r["parameters"]["duck_and_cover"]["steps"][0]="ImproveDefcon",r=>r["parameters"]["nuclear_test_ban"]["vp_formula"]="eval",r=>r["parameters"]["duck_and_cover"]["source_refs"][0]["source_id"]="missing",r=>r["parameters"]["how_i_learned_to_stop_worrying"]["military_credit_amount"]=6}){var j=JObject.Parse(Data());edit(j);Assert.Throws<ArgumentException>(()=>DefconVpEventLoader.LoadImmediateSubset(j.ToString(),Schema()));}
            Assert.Throws<ArgumentException>(()=>DefconVpEventLoader.LoadImmediateSubset(Data(),Schema().Replace("contract:defcon-vp-events:1","contract:other:1")));
        }
        [Test] public void Definitions_and_catalog_reject_bad_binding_duplicates_and_mutable_inputs()
        {
            var cards=PlayCoupTests.Prepared().Practice.Definition.Cards;var e=Effect(true);var list=new[]{e};var c=new PracticeDefconVpCatalog(cards,list);list[0]=Effect(false);Assert.That(c.Effects[e.Card],Is.SameAs(e));
            Assert.Throws<ArgumentException>(()=>new PracticeDefconVpCatalog(cards,new[]{e,e}));Assert.Throws<ArgumentException>(()=>new PracticeDefconVpCatalog(cards,new[]{new DefconVpEffectDefinition(e.Card,"effect.wrong",e.Kind,e.DefconDelta,e.VpConstant)}));
            Assert.Throws<ArgumentException>(()=>new DefconVpEffectDefinition(e.Card,e.EffectId,(DefconVpEventKind)99,-1,5));Assert.Throws<ArgumentException>(()=>new DefconVpEffectDefinition(e.Card,e.EffectId,e.Kind,1,5));
        }
    }
}
