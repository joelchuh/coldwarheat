using System;
using System.Collections.Generic;
using System.Linq;
using ColdWarStruggle.Core;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public sealed class CoupCalculationTests
    {
        static readonly CountryId Target=new CountryId("country.test");
        static readonly string[] None=Array.Empty<string>();
        public static DefconState State(int stability=2,bool battleground=true,int level=5)
        {
            var map=new MapDefinition(new[]{new CountryDefinition(Target,stability,"region.test",battleground)},
                Array.Empty<KeyValuePair<CountryId,CountryId>>(),new Dictionary<PlayerSide,IReadOnlyList<CountryId>>
                { [PlayerSide.US]=Array.Empty<CountryId>(),[PlayerSide.USSR]=Array.Empty<CountryId>() });
            var regions=new Dictionary<int,IReadOnlyList<string>>();for(int i=2;i<=5;i++)regions[i]=None;
            return new DefconState(new DefconDefinition(map,5,5,1,1,1,new[]{DefconActionKind.Coup,DefconActionKind.Realignment},regions),level);
        }
        static CountryState Influence(PlayerSide actor,int own,int enemy)=>actor==PlayerSide.US?new CountryState(own,enemy):new CountryState(enemy,own);
        [TestCase(1,3,0,0,0)] [TestCase(2,3,1,1,0)] [TestCase(3,3,2,2,0)]
        [TestCase(4,3,3,2,1)] [TestCase(5,3,4,2,2)] [TestCase(6,3,5,2,3)]
        [TestCase(1,1,-2,0,0)]
        public void Die_boundaries_remove_before_adding_symmetrically(int roll,int strength,long margin,int removed,int added)
        {
            foreach(var actor in new[]{PlayerSide.US,PlayerSide.USSR})
            {
                var state=State();var before=Influence(actor,2,2);var definition=new CoupDefinition(1,6,2);
                var result=CoupRules.Calculate(definition,state,Target,before,actor,strength,roll,None);
                Assert.That(result.Definition,Is.SameAs(definition));Assert.That(result.Target,Is.SameAs(state.Definition.Map.GetCountry(Target)));
                Assert.That(result.Actor,Is.EqualTo(actor));Assert.That(result.Before.For(actor),Is.EqualTo(2));
                Assert.That(result.Roll,Is.EqualTo(roll));Assert.That(result.Strength,Is.EqualTo(strength));Assert.That(result.Margin,Is.EqualTo(margin));
                Assert.That(result.Removed,Is.EqualTo(removed));Assert.That(result.Added,Is.EqualTo(added));
                Assert.That(result.After.For(actor),Is.EqualTo(2+added));Assert.That(result.After.For(actor==PlayerSide.US?PlayerSide.USSR:PlayerSide.US),Is.EqualTo(2-removed));
                Assert.That(before.US,Is.EqualTo(2));Assert.That(before.USSR,Is.EqualTo(2));Assert.That(state.Level,Is.EqualTo(5));
            }
        }
        [TestCase(true,1)] [TestCase(true,6)] [TestCase(false,1)] [TestCase(false,6)]
        public void Battleground_intent_does_not_depend_on_success(bool battleground,int roll)
        {
            var result=CoupRules.Calculate(new CoupDefinition(1,6,2),State(3,battleground),Target,new CountryState(0,1),PlayerSide.US,1,roll,None);
            Assert.That(result.RequiresBattlegroundDegradation,Is.EqualTo(battleground));Assert.That(result.Removed,Is.EqualTo(roll==6?1:0));
        }
        [TestCase(PlayerSide.US)] [TestCase(PlayerSide.USSR)]
        public void No_reach_or_own_presence_required_but_enemy_required(PlayerSide actor)
        {
            Assert.That(CoupRules.CheckTarget(State(),Target,Influence(actor,0,1),actor,None),Is.EqualTo(CoupTargetReason.Allowed));
            Assert.That(CoupRules.CheckTarget(State(),Target,Influence(actor,5,0),actor,None),Is.EqualTo(CoupTargetReason.NoOpponentInfluence));
            Assert.Throws<InvalidOperationException>(()=>CoupRules.Calculate(new CoupDefinition(1,6,2),State(),Target,Influence(actor,5,0),actor,1,1,None));
        }
        [TestCase(4,"country.france")] [TestCase(3,"country.thailand")] [TestCase(2,"country.iran")]
        public void Formal_map_geographic_gate_reused(int level,string target)
        {
            var state=new DefconState(DefconTests.Definition(),level);var id=new CountryId(target);
            Assert.That(CoupRules.CheckTarget(state,id,new CountryState(0,1),PlayerSide.US,None),Is.EqualTo(CoupTargetReason.GeographicRestriction));
            Assert.Throws<InvalidOperationException>(()=>CoupRules.Calculate(new CoupDefinition(1,6,2),state,id,new CountryState(0,1),PlayerSide.US,4,6,None));
        }
        [TestCase(0)] [TestCase(1)]
        public void Nuclear_state_is_absorbing_even_with_no_enemy(int enemy)
        {
            Assert.That(CoupRules.CheckTarget(State(level:1),Target,new CountryState(0,enemy),PlayerSide.US,None),Is.EqualTo(CoupTargetReason.NuclearWarReached));
            Assert.Throws<InvalidOperationException>(()=>CoupRules.Calculate(new CoupDefinition(1,6,2),State(level:1),Target,new CountryState(0,enemy),PlayerSide.US,1,1,None));
        }
        [TestCase("cuban-crisis")] [TestCase("nuclear-subs")] [TestCase("unknown")]
        public void Any_unimplemented_effect_blocks_instead_of_silently_assuming_none(string effect)
        {
            Assert.Throws<NotSupportedException>(()=>CoupRules.CheckTarget(State(),Target,new CountryState(0,1),PlayerSide.US,new[]{effect}));
            Assert.Throws<NotSupportedException>(()=>CoupRules.Calculate(new CoupDefinition(1,6,2),State(),Target,new CountryState(0,1),PlayerSide.US,1,1,new[]{effect}));
        }
        [Test]
        public void Synthetic_definition_controls_range_and_stability_coefficient()
        {
            var definition=new CoupDefinition(2,8,3);
            var result=CoupRules.Calculate(definition,State(),Target,new CountryState(0,1),PlayerSide.US,2,8,None);
            Assert.That(result.Margin,Is.EqualTo(4));Assert.That(result.Added,Is.EqualTo(3));
            Assert.Throws<ArgumentOutOfRangeException>(()=>CoupRules.Calculate(definition,State(),Target,new CountryState(0,1),PlayerSide.US,2,1,None));
            Assert.Throws<ArgumentOutOfRangeException>(()=>CoupRules.Calculate(definition,State(),Target,new CountryState(0,1),PlayerSide.US,2,9,None));
        }
        [Test]
        public void Wide_math_does_not_wrap_large_stability_or_positive_margin()
        {
            var result=CoupRules.Calculate(new CoupDefinition(1,int.MaxValue,int.MaxValue),State(int.MaxValue),Target,new CountryState(0,1),PlayerSide.US,int.MaxValue,int.MaxValue,None);
            Assert.That(result.Margin,Is.EqualTo(-4611686009837453315L));Assert.That(result.Added,Is.Zero);Assert.That(result.Removed,Is.Zero);
            var positive=CoupRules.Calculate(new CoupDefinition(1,int.MaxValue,1),State(1),Target,new CountryState(0,int.MaxValue),PlayerSide.US,int.MaxValue,int.MaxValue,None);
            Assert.That(positive.Margin,Is.EqualTo(4294967293L));Assert.That(positive.Removed,Is.EqualTo(int.MaxValue));Assert.That(positive.Added,Is.EqualTo(int.MaxValue-1));
        }
        [TestCase(PlayerSide.US)] [TestCase(PlayerSide.USSR)]
        public void Influence_storage_overflow_rejects_without_mutation(PlayerSide actor)
        {
            var before=Influence(actor,int.MaxValue,1);
            Assert.Throws<OverflowException>(()=>CoupRules.Calculate(new CoupDefinition(1,6,2),State(1),Target,before,actor,4,6,None));
            Assert.That(before.For(actor),Is.EqualTo(int.MaxValue));
            Assert.Throws<OverflowException>(()=>CoupRules.Calculate(new CoupDefinition(1,int.MaxValue,1),State(1),Target,Influence(actor,0,1),actor,int.MaxValue,int.MaxValue,None));
        }
        [TestCase(0,6,2)] [TestCase(1,0,2)] [TestCase(7,6,2)] [TestCase(1,6,0)] [TestCase(1,6,-1)]
        public void Invalid_definitions_rejected(int min,int max,int multiplier)=>Assert.Throws<ArgumentOutOfRangeException>(()=>new CoupDefinition(min,max,multiplier));
        [Test]
        public void Missing_identity_state_or_explicit_effect_context_rejected()
        {
            var state=State();var before=new CountryState(0,1);var definition=new CoupDefinition(1,6,2);
            Assert.Throws<ArgumentNullException>(()=>CoupRules.CheckTarget(null,Target,before,PlayerSide.US,None));
            Assert.Throws<ArgumentNullException>(()=>CoupRules.CheckTarget(state,Target,before,PlayerSide.US,null));
            Assert.Throws<ArgumentOutOfRangeException>(()=>CoupRules.CheckTarget(state,Target,before,(PlayerSide)99,None));
            Assert.Throws<ArgumentException>(()=>CoupRules.CheckTarget(state,new CountryId("unknown"),before,PlayerSide.US,None));
            Assert.Throws<ArgumentNullException>(()=>CoupRules.Calculate(null,state,Target,before,PlayerSide.US,1,1,None));
            Assert.Throws<ArgumentOutOfRangeException>(()=>CoupRules.Calculate(definition,state,Target,before,PlayerSide.US,0,1,None));
            Assert.Throws<ArgumentOutOfRangeException>(()=>CoupRules.Calculate(definition,state,Target,before,PlayerSide.US,1,0,None));
            Assert.Throws<ArgumentOutOfRangeException>(()=>CoupRules.Calculate(definition,state,Target,before,PlayerSide.US,1,7,None));
        }
    }
}
