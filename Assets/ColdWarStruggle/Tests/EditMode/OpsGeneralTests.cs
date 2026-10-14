using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ColdWarStruggle.Core;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public sealed class OpsGeneralTests
    {
        internal static OpsGeneralDefinition Definition()=>new OpsGeneralDefinition(1,4,(OpsValuePurpose[])Enum.GetValues(typeof(OpsValuePurpose)));
        internal static CardDefinition Card(int ops=2)=>new CardDefinition(new CardId("card.fixture"),1,"name","rules",CardKind.Event,ops,CardAffiliation.US,GameEra.Early,CardDeckEntry.EraDeck,true,false,null,CardCatalogTests.Effect());
        static IEnumerable Values()
        {for(var b=1;b<=4;b++)foreach(var delta in new[]{-10,-1,0,1,10})yield return new TestCaseData(b,delta);}
        [TestCaseSource(nameof(Values))]
        public void Flat_values_are_clamped_after_aggregation_for_every_purpose_and_actor(int basis,int delta)
        {
            var definition=Definition();var card=Card(basis);
            foreach(var actor in new[]{PlayerSide.US,PlayerSide.USSR})foreach(OpsValuePurpose purpose in Enum.GetValues(typeof(OpsValuePurpose)))
            {
                var mods=new[]{new OpsFlatModifier("own",actor,delta),new OpsFlatModifier("other",actor==PlayerSide.US?PlayerSide.USSR:PlayerSide.US,99)};
                var quote=OpsGeneralRules.QuoteCard(definition,card,actor,purpose,mods);
                Assert.That(quote.Definition,Is.SameAs(definition));Assert.That(quote.Card,Is.SameAs(card));Assert.That(quote.AsIfSourceRuleId,Is.Null);
                Assert.That(quote.Actor,Is.EqualTo(actor));Assert.That(quote.Purpose,Is.EqualTo(purpose));Assert.That(quote.BaseValue,Is.EqualTo(basis));
                Assert.That(quote.ModifierTotal,Is.EqualTo(delta));Assert.That(quote.RawValue,Is.EqualTo((long)basis+delta));
                Assert.That(quote.GeneralValue,Is.EqualTo(Math.Max(1,Math.Min(4,basis+delta))));Assert.That(quote.AppliedModifiers.Count,Is.EqualTo(1));
                Assert.That(card.PrintedOps,Is.EqualTo(basis));
            }
        }
        [TestCase(4,1,-1,4)] [TestCase(1,-1,1,1)] [TestCase(2,1,1,4)]
        public void Modifier_order_does_not_clip_intermediate_values(int basis,int a,int b,int expected)
        {
            var mods=new[]{new OpsFlatModifier("a",PlayerSide.US,a),new OpsFlatModifier("b",PlayerSide.US,b)};
            var first=OpsGeneralRules.QuoteCard(Definition(),Card(basis),PlayerSide.US,OpsValuePurpose.Coup,mods);
            var reverse=OpsGeneralRules.QuoteCard(Definition(),Card(basis),PlayerSide.US,OpsValuePurpose.Coup,mods.Reverse());
            Assert.That(first.GeneralValue,Is.EqualTo(expected));Assert.That(reverse.GeneralValue,Is.EqualTo(expected));
        }
        [Test]
        public void Different_owner_modifier_does_not_travel_with_card_and_china_has_no_automatic_regional_bonus()
        {
            var card=new CardDefinition(new CardId("card.china"),6,"name","rules",CardKind.China,4,CardAffiliation.None,null,CardDeckEntry.SpecialChina,false,false,null,null);
            var mods=new[]{new OpsFlatModifier("ussr",PlayerSide.USSR,-1)};
            Assert.That(OpsGeneralRules.QuoteCard(Definition(),card,PlayerSide.US,OpsValuePurpose.Space,mods).GeneralValue,Is.EqualTo(4));
            Assert.That(OpsGeneralRules.QuoteCard(Definition(),card,PlayerSide.USSR,OpsValuePurpose.Space,mods).GeneralValue,Is.EqualTo(3));
        }
        [TestCase(OpsValuePurpose.Influence)] [TestCase(OpsValuePurpose.Coup)] [TestCase(OpsValuePurpose.Realignment)]
        public void Verified_as_if_ops_receive_same_flat_modifiers(OpsValuePurpose purpose)
        {
            var quote=OpsGeneralRules.QuoteAsIfCard(Definition(),"event.fixture",1,PlayerSide.US,purpose,new[]{new OpsFlatModifier("containment",PlayerSide.US,1)});
            Assert.That(quote.Card,Is.Null);Assert.That(quote.AsIfSourceRuleId,Is.EqualTo("event.fixture"));Assert.That(quote.GeneralValue,Is.EqualTo(2));
        }
        [TestCase(OpsValuePurpose.Space)] [TestCase(OpsValuePurpose.OpsThreshold)]
        public void As_if_ops_cannot_be_used_as_a_physical_card(OpsValuePurpose purpose)
        { Assert.That(()=>OpsGeneralRules.QuoteAsIfCard(Definition(),"event",1,PlayerSide.US,purpose,Array.Empty<OpsFlatModifier>()),Throws.InstanceOf<ArgumentException>()); }
        [Test]
        public void Definitions_and_applied_modifiers_defensively_copy_and_support_data_driven_bounds()
        {
            var purposes=new List<OpsValuePurpose>{OpsValuePurpose.Space};var definition=new OpsGeneralDefinition(2,6,purposes);purposes.Clear();
            var mods=new List<OpsFlatModifier>{new OpsFlatModifier("plus",PlayerSide.US,5)};
            var quote=OpsGeneralRules.QuoteCard(definition,Card(4),PlayerSide.US,OpsValuePurpose.Space,mods);mods.Clear();
            Assert.That(quote.GeneralValue,Is.EqualTo(6));Assert.That(quote.AppliedModifiers.Count,Is.EqualTo(1));Assert.That(definition.FlatPurposes.Count,Is.EqualTo(1));
            Assert.That(OpsGeneralRules.QuoteCard(definition,Card(1),PlayerSide.US,OpsValuePurpose.Space,mods).GeneralValue,Is.EqualTo(2));
            Assert.Throws<NotSupportedException>(()=>((IList)definition.FlatPurposes).Clear());Assert.Throws<NotSupportedException>(()=>((IList)quote.AppliedModifiers).Clear());
            Assert.That(()=>OpsGeneralRules.QuoteCard(definition,Card(),PlayerSide.US,OpsValuePurpose.Coup,mods),Throws.InstanceOf<ArgumentException>());
        }
        [TestCase(int.MaxValue,4)] [TestCase(int.MinValue,1)]
        public void Full_bounded_modifier_sum_uses_long_before_clamping(int delta,int expected)
        {
            var mods=Enumerable.Range(0,128).Select(i=>new OpsFlatModifier("m"+i,PlayerSide.US,delta));
            var quote=OpsGeneralRules.QuoteCard(Definition(),Card(4),PlayerSide.US,OpsValuePurpose.Space,mods);
            Assert.That(quote.ModifierTotal,Is.EqualTo((long)delta*128));Assert.That(quote.RawValue,Is.EqualTo((long)delta*128+4));Assert.That(quote.GeneralValue,Is.EqualTo(expected));
        }
        [TestCase("min")] [TestCase("range")] [TestCase("null")] [TestCase("empty")] [TestCase("duplicate")] [TestCase("unknown")]
        public void Invalid_definitions_rejected(string defect)
        {
            IEnumerable<OpsValuePurpose> purposes=defect=="null"?null:defect=="empty"?Array.Empty<OpsValuePurpose>():defect=="duplicate"?new[]{OpsValuePurpose.Space,OpsValuePurpose.Space}:new[]{defect=="unknown"?(OpsValuePurpose)99:OpsValuePurpose.Space};
            Assert.That(()=>new OpsGeneralDefinition(defect=="min"?0:1,defect=="range"?0:4,purposes),Throws.InstanceOf<ArgumentException>());
        }
        [TestCase("id")] [TestCase("long")] [TestCase("side")]
        public void Invalid_modifiers_rejected(string defect)
        { Assert.That(()=>new OpsFlatModifier(defect=="id"?" ":defect=="long"?new string('x',257):"valid",defect=="side"?(PlayerSide)99:PlayerSide.US,1),Throws.InstanceOf<ArgumentException>()); }
        [TestCase("null")] [TestCase("itemNull")] [TestCase("duplicate")] [TestCase("tooMany")]
        public void Invalid_modifier_sets_rejected(string defect)
        {
            var modifier=new OpsFlatModifier("id",PlayerSide.US,1);
            IEnumerable<OpsFlatModifier> mods=defect=="null"?null:defect=="itemNull"?new OpsFlatModifier[]{null}:defect=="duplicate"?new[]{modifier,modifier}:Enumerable.Range(0,129).Select(i=>new OpsFlatModifier("m"+i,PlayerSide.US,1));
            Assert.That(()=>OpsGeneralRules.QuoteCard(Definition(),Card(),PlayerSide.US,OpsValuePurpose.Space,mods),Throws.InstanceOf<ArgumentException>());
        }
        [Test]
        public void Missing_or_invalid_quote_context_is_rejected()
        {
            var empty=Array.Empty<OpsFlatModifier>();var def=Definition();var card=Card();
            Assert.That(()=>OpsGeneralRules.QuoteCard(null,card,PlayerSide.US,OpsValuePurpose.Space,empty),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>OpsGeneralRules.QuoteCard(def,null,PlayerSide.US,OpsValuePurpose.Space,empty),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>OpsGeneralRules.QuoteCard(def,SpaceAttemptTests.Scoring(),PlayerSide.US,OpsValuePurpose.Space,empty),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>OpsGeneralRules.QuoteCard(def,card,(PlayerSide)99,OpsValuePurpose.Space,empty),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>OpsGeneralRules.QuoteCard(def,card,PlayerSide.US,(OpsValuePurpose)99,empty),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>OpsGeneralRules.QuoteAsIfCard(def," ",1,PlayerSide.US,OpsValuePurpose.Coup,empty),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>OpsGeneralRules.QuoteAsIfCard(def,"source",0,PlayerSide.US,OpsValuePurpose.Coup,empty),Throws.InstanceOf<ArgumentException>());
        }
    }
}
