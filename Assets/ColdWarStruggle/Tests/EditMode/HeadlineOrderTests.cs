using System;
using System.Collections.Generic;
using System.Linq;
using ColdWarStruggle.Core;
using ColdWarStruggle.Infrastructure;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class HeadlineOrderTests
    {
        static CardId Id(string n)=>CardZonesTests.Id(n);
        // Synthetic cards isolate ordering; canonical printed data is checked separately below.
        static CardCatalog Catalog()
        {
            CardDefinition Event(string n,int number,int ops,CardAffiliation side)=>new CardDefinition(Id(n),number,"name","rules",CardKind.Event,ops,side,GameEra.Early,CardDeckEntry.EraDeck,false,false,null,CardCatalogTests.Effect());
            CardDefinition Score(string n,int number)=>new CardDefinition(Id(n),number,"name","rules",CardKind.Scoring,null,CardAffiliation.None,GameEra.Early,CardDeckEntry.EraDeck,false,false,"region.europe",CardCatalogTests.Effect());
            return new CardCatalog(new[]{Event("a",1,3,CardAffiliation.USSR),Event("b",2,2,CardAffiliation.US),Event("c",3,3,CardAffiliation.Neutral),Score("d",4),Score("e",5),CardZonesTests.Catalog().GetCard(Id("china"))});
        }
        [TestCase("a","b",PlayerSide.US)][TestCase("b","a",PlayerSide.USSR)][TestCase("a","c",PlayerSide.US)]
        [TestCase("d","b",PlayerSide.USSR)][TestCase("b","d",PlayerSide.US)][TestCase("d","e",PlayerSide.US)]
        public void Ordinary_printed_and_scoring_values_determine_order(string us,string su,PlayerSide first)
        {
            var result=HeadlineOrderRules.BuildOrdinaryOrder(new HeadlineOrderParameters(0,PlayerSide.US),Catalog(),Id(us),Id(su));
            Assert.That(result,Has.Count.EqualTo(2));Assert.That(result[0].PlayedBy,Is.EqualTo(first));Assert.That(result[1].PlayedBy,Is.Not.EqualTo(first));
            Assert.That(result.Single(x=>x.PlayedBy==PlayerSide.US).Card,Is.EqualTo(Id(us)));Assert.That(result.Single(x=>x.PlayedBy==PlayerSide.USSR).Card,Is.EqualTo(Id(su)));
            foreach(var item in result)Assert.That(item.Value,Is.EqualTo(Catalog().GetCard(item.Card).PrintedOps??0));
        }
        [Test] public void Parameters_drive_tie_and_scoring_values_without_subtraction_overflow()
        {
            Assert.That(HeadlineOrderRules.BuildOrdinaryOrder(new HeadlineOrderParameters(0,PlayerSide.USSR),Catalog(),Id("a"),Id("c"))[0].PlayedBy,Is.EqualTo(PlayerSide.USSR));
            var result=HeadlineOrderRules.BuildOrdinaryOrder(new HeadlineOrderParameters(int.MaxValue,PlayerSide.US),Catalog(),Id("d"),Id("a"));
            Assert.That(result[0].Card,Is.EqualTo(Id("d")));Assert.That(result[0].Value,Is.EqualTo(int.MaxValue));
        }
        [Test] public void Played_by_is_preserved_even_when_event_affiliation_belongs_to_opponent()
        {
            var catalog=Catalog();var result=HeadlineOrderRules.BuildOrdinaryOrder(new HeadlineOrderParameters(0,PlayerSide.US),catalog,Id("a"),Id("b"));
            Assert.That(catalog.GetCard(Id("a")).Affiliation,Is.EqualTo(CardAffiliation.USSR));Assert.That(result[0].PlayedBy,Is.EqualTo(PlayerSide.US));
            Assert.Throws<NotSupportedException>(()=>((IList<HeadlineOrderEntry>)result).Clear());Assert.That(catalog.Cards,Has.Count.EqualTo(6));
        }
        [Test] public void Canonical_shared_parameters_and_printed_card_catalog_are_used()
        {
            var parameters=HeadlineParametersLoader.Load(TurnCalendarTests.Source());Assert.That(parameters.ScoringValue,Is.Zero);Assert.That(parameters.TieFirstSide,Is.EqualTo(PlayerSide.US));
            var result=HeadlineOrderRules.BuildOrdinaryOrder(parameters,CardCatalogLoaderTests.Load(),Id("asia_scoring"),Id("nasser"));
            Assert.That(result[0].PlayedBy,Is.EqualTo(PlayerSide.USSR));Assert.That(result[0].Value,Is.EqualTo(1));Assert.That(result[1].Value,Is.Zero);
            var data=JObject.Parse(TurnCalendarTests.Source());data["headline"]["scoring_value"]=3;data["headline"]["tie_first_side"]="USSR";
            var configured=HeadlineParametersLoader.Load(data.ToString());Assert.That(configured.ScoringValue,Is.EqualTo(3));Assert.That(configured.TieFirstSide,Is.EqualTo(PlayerSide.USSR));
        }
        [TestCase("same-card")][TestCase("china-us")][TestCase("china-su")][TestCase("unknown")][TestCase("default")][TestCase("null-catalog")][TestCase("null-parameters")]
        public void Invalid_order_inputs_fail_before_producing_an_order(string mutation)
        {
            var catalog=Catalog();var parameters=new HeadlineOrderParameters(0,PlayerSide.US);var us=Id("a");var su=Id("b");
            if(mutation=="same-card")su=us;else if(mutation=="china-us")us=Id("china");else if(mutation=="china-su")su=Id("china");else if(mutation=="unknown")us=Id("unknown");else if(mutation=="default")su=default;else if(mutation=="null-catalog")catalog=null;else parameters=null;
            Assert.That(()=>HeadlineOrderRules.BuildOrdinaryOrder(parameters,catalog,us,su),Throws.InstanceOf<ArgumentException>());
        }
        [Test] public void Invalid_parameter_values_are_rejected()
        {Assert.Throws<ArgumentOutOfRangeException>(()=>new HeadlineOrderParameters(-1,PlayerSide.US));Assert.Throws<ArgumentOutOfRangeException>(()=>new HeadlineOrderParameters(0,(PlayerSide)99));}
        [TestCase("schema")][TestCase("version")][TestCase("source")][TestCase("missing-headline")][TestCase("array")]
        [TestCase("sort")][TestCase("sort-null")][TestCase("tie")][TestCase("missing-value")][TestCase("missing-sort")][TestCase("missing-tie")]
        [TestCase("extra")][TestCase("float")][TestCase("string")][TestCase("negative")][TestCase("overflow")][TestCase("bool")][TestCase("null")]
        public void Malformed_or_unsupported_profile_fields_are_rejected(string mutation)
        {
            var root=JObject.Parse(TurnCalendarTests.Source());var h=(JObject)root["headline"];
            switch(mutation)
            {
                case "schema":root["schema_version"]=2;break;case "version":root["content_version"]="9.0.0";break;case "source":root["source"]["id"]="unknown";break;
                case "missing-headline":root.Remove("headline");break;case "array":root["headline"]=new JArray();break;
                case "sort":h["sort"]="ascending";break;case "sort-null":h["sort"]=null;break;case "tie":h["tie_first_side"]="unknown";break;
                case "missing-value":h.Remove("scoring_value");break;case "missing-sort":h.Remove("sort");break;case "missing-tie":h.Remove("tie_first_side");break;
                case "extra":h["unknown"]=1;break;case "float":h["scoring_value"]=1.0;break;case "string":h["scoring_value"]="0";break;
                case "negative":h["scoring_value"]=-1;break;case "overflow":h["scoring_value"]=(long)int.MaxValue+1;break;case "bool":h["scoring_value"]=true;break;case "null":h["scoring_value"]=null;break;
            }
            Assert.That(()=>HeadlineParametersLoader.Load(root.ToString()),Throws.InstanceOf<ArgumentException>());
        }
        [Test] public void Duplicate_fields_and_trailing_json_are_rejected()
        {
            var source=TurnCalendarTests.Source();Assert.That(source,Does.Contain("\"headline\": {"));
            Assert.That(()=>HeadlineParametersLoader.Load(source.Replace("\"headline\": {","\"headline\": {}, \"headline\": {")),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>HeadlineParametersLoader.Load(source+" {}"),Throws.InstanceOf<ArgumentException>());
        }
    }
}
