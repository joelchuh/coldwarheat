using System;
using System.Collections.Generic;
using System.Linq;
using ColdWarStruggle.Core;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class CardCatalogTests
    {
        public static CardEffectBinding Effect()=>new CardEffectBinding("effect.fixture","0.1.0",CardParameterBindingKind.NoParameters,Array.Empty<CardParameterReference>());
        public static CardDefinition Event(string id="card.fixture",int number=1)=>new CardDefinition(new CardId(id),number,id+".name",id+".rules",CardKind.Event,2,CardAffiliation.US,GameEra.Early,CardDeckEntry.EraDeck,true,false,null,Effect());
        [TestCase(null)] [TestCase("")] [TestCase("card.")] [TestCase("country.a")] [TestCase("card.UPPER")] [TestCase("card.a/b")] [TestCase("card.a\n")]
        public void Card_id_requires_stable_syntax(string id)=>Assert.That(()=>new CardId(id),Throws.InstanceOf<ArgumentException>());
        [Test] public void Parameter_reference_accepts_existing_hyphenated_dataset_ids()
        {
            var reference=new CardParameterReference("rules.deluxe2015.region-scoring","/regions/1");
            Assert.That(reference.DatasetId,Is.EqualTo("rules.deluxe2015.region-scoring"));
            Assert.That(()=>new CardParameterReference("rules.fixture\n",""),Throws.InstanceOf<ArgumentException>());
        }
        [Test] public void Card_id_has_ordinal_value_identity()
        {
            var a=new CardId("card.a");Assert.That(a,Is.EqualTo(new CardId("card.a")));Assert.That(a.GetHashCode(),Is.EqualTo(new CardId("card.a").GetHashCode()));
            Assert.That(a,Is.Not.EqualTo(new CardId("card.b")));Assert.That(a.ToString(),Is.EqualTo("card.a"));Assert.That(default(CardId).ToString(),Is.Empty);
        }
        [Test] public void Catalog_copies_and_sorts_input_and_rejects_unknown_queries()
        {
            var a=Event("card.a",2);var b=Event("card.b",1);var source=new List<CardDefinition>{b,a};var cat=new CardCatalog(source);source.Clear();
            Assert.That(cat.Cards.Select(c=>c.Id.Value),Is.EqualTo(new[]{"card.a","card.b"}));Assert.That(cat.GetPrintedCard(1),Is.SameAs(b));Assert.That(cat.GetCard(a.Id),Is.SameAs(a));
            Assert.That(cat.Contains(default),Is.False);Assert.That(()=>cat.GetCard(default),Throws.InstanceOf<ArgumentException>());Assert.That(()=>cat.GetPrintedCard(99),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>cat.GetEraCards((GameEra)999),Throws.InstanceOf<ArgumentException>());Assert.That(cat.GetEraCards(GameEra.Early),Has.Count.EqualTo(2));
            Assert.Throws<NotSupportedException>(()=>((IList<CardDefinition>)cat.Cards).Clear());
        }
        [Test] public void Catalog_rejects_duplicate_ids_numbers_null_empty_and_excessive_input()
        {
            var a=Event();Assert.That(()=>new CardCatalog(new[]{a,a}),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>new CardCatalog(new[]{a,Event("card.other",1)}),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>new CardCatalog(new[]{a,(CardDefinition)null}),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>new CardCatalog(Array.Empty<CardDefinition>()),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>new CardCatalog(null),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>new CardCatalog(Enumerable.Range(1,257).Select(i=>Event("card.c"+i,i))),Throws.InstanceOf<ArgumentException>());
        }
        [Test] public void Parameter_binding_copies_input_and_rejects_unresolved_or_malformed_references()
        {
            var p=new CardParameterReference("effects.fixture","/parameters/a~1b");var list=new List<CardParameterReference>{p};
            var e=new CardEffectBinding("effect.a","0.1.0",CardParameterBindingKind.Bound,list);list.Clear();Assert.That(e.Parameters.Single(),Is.SameAs(p));
            Assert.Throws<NotSupportedException>(()=>((IList<CardParameterReference>)e.Parameters).Clear());
            Assert.That(()=>new CardParameterReference("bad id","/x"),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>new CardParameterReference("effects.fixture","/bad~2"),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>new CardEffectBinding("effect.a","0.1.0",CardParameterBindingKind.Bound,Array.Empty<CardParameterReference>()),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>new CardEffectBinding("effect.a","0.1.0",CardParameterBindingKind.NoParameters,new[]{p}),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>new CardEffectBinding("effect.a","0.1.0",CardParameterBindingKind.Bound,new[]{p,p}),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>new CardEffectBinding("effect.a","0.1.0",(CardParameterBindingKind)99,new[]{p}),Throws.InstanceOf<ArgumentException>());
        }
        [TestCase("score-ops")] [TestCase("score-side")] [TestCase("event-no-ops")] [TestCase("event-no-era")]
        [TestCase("event-no-effect")] [TestCase("event-region")] [TestCase("event-special")] [TestCase("bad-number")]
        [TestCase("bad-kind")] [TestCase("bad-side")] [TestCase("bad-era")] [TestCase("bad-id")]
        [TestCase("china-era")] [TestCase("china-effect")] [TestCase("china-remove")] [TestCase("china-id")]
        public void Definitions_reject_invalid_kind_combinations(string kind)
        {
            var id=new CardId("card.a");int number=1;CardKind type=CardKind.Event;int? ops=2;var side=CardAffiliation.US;GameEra? era=GameEra.Early;
            var deck=CardDeckEntry.EraDeck;bool remove=false;string region=null;var effect=Effect();
            if(kind.StartsWith("score")){type=CardKind.Scoring;ops=null;side=CardAffiliation.None;region="region.asia";}
            if(kind.StartsWith("china")){id=new CardId("card.china");type=CardKind.China;ops=4;side=CardAffiliation.None;era=null;deck=CardDeckEntry.SpecialChina;effect=null;}
            switch(kind){case "score-ops":ops=1;break;case "score-side":side=CardAffiliation.US;break;case "event-no-ops":ops=null;break;case "event-no-era":era=null;break;case "event-no-effect":effect=null;break;case "event-region":region="region.asia";break;case "event-special":deck=CardDeckEntry.SpecialChina;break;case "bad-number":number=0;break;case "bad-kind":type=(CardKind)99;break;case "bad-side":side=(CardAffiliation)99;break;case "bad-era":era=(GameEra)99;break;case "bad-id":id=default;break;case "china-era":era=GameEra.Early;break;case "china-effect":effect=Effect();break;case "china-remove":remove=true;break;case "china-id":id=new CardId("card.other");break;}
            Assert.That(()=>new CardDefinition(id,number,"name","rules",type,ops,side,era,deck,remove,false,region,effect),Throws.InstanceOf<ArgumentException>());
        }
    }
}
