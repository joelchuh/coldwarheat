using System;
using System.Collections.Generic;
using System.Linq;
using ColdWarStruggle.Core;
using ColdWarStruggle.Presentation;
using NUnit.Framework;
using UnityEngine;
namespace ColdWarStruggle.Tests
{
    public class MapLabelLayoutTests
    {
        static readonly Rect Area=new Rect(0,0,614,368);
        static MapLabelRequest R(string id,float x,float y,int priority=0,float w=110,float h=34)=>new MapLabelRequest(new CountryId("country."+id),new Vector2(x,y),new Vector2(w,h),priority);
        [Test] public void Dense_country_labels_avoid_each_other_hotspots_and_clipping()
        {
            var requests=new[]{R("israel",320,140,2),R("jordan",351,167),R("lebanon",334,106),R("syria",378,92),R("egypt",240,200)};
            var placed=MapLabelLayout.Arrange(Area,requests);Assert.That(placed.Count,Is.GreaterThanOrEqualTo(4));
            Assert.That(placed.Any(p=>p.Country.Equals(requests[0].Country)),Is.True);
            foreach(var p in placed)
            {
                Assert.That(Area.Contains(p.Bounds.min) && Area.Contains(p.Bounds.max),Is.True);
                foreach(var n in requests)Assert.That(p.Bounds.Overlaps(new Rect(n.Anchor-Vector2.one*14,Vector2.one*28)),Is.False);
                foreach(var other in placed.Where(o=>o!=p))Assert.That(p.Bounds.Overlaps(other.Bounds),Is.False);
            }
        }
        [Test] public void Highest_priority_wins_scarce_space_and_input_order_does_not_matter()
        {
            var a=R("a",150,100,0,100,32);var b=R("b",150,100,3,100,32);var area=new Rect(0,0,300,200);
            var normal=MapLabelLayout.Arrange(area,new[]{a,b});var reversed=MapLabelLayout.Arrange(area,new[]{b,a});
            Assert.That(normal.First().Country,Is.EqualTo(b.Country));
            Assert.That(normal.Select(p=>p.Country),Is.EqualTo(reversed.Select(p=>p.Country)));
            Assert.That(normal.Select(p=>p.Bounds),Is.EqualTo(reversed.Select(p=>p.Bounds)));
            Assert.Throws<NotSupportedException>(()=>((IList<MapLabelPlacement>)normal).Clear());
        }
        [Test] public void Oversized_or_edge_labels_are_omitted_or_repositioned_not_clipped()
        {
            var requests=new[]{R("corner",3,3,2,75,32),R("oversize",400,200,0,800,32)};
            var placed=MapLabelLayout.Arrange(Area,requests);Assert.That(placed.Any(p=>p.Country.Equals(requests[1].Country)),Is.False);
            foreach(var p in placed)Assert.That(Area.Contains(p.Bounds.min) && Area.Contains(p.Bounds.max),Is.True);
        }
        [Test] public void Empty_labels_are_valid_but_unbounded_or_duplicate_input_is_rejected()
        {
            Assert.That(MapLabelLayout.Arrange(Area,Array.Empty<MapLabelRequest>()),Is.Empty);
            var a=R("a",200,100);Assert.That(()=>MapLabelLayout.Arrange(Area,new[]{a,a}),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>MapLabelLayout.Arrange(Area,Enumerable.Range(0,129).Select(i=>R("n"+i,200,100))),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>MapLabelLayout.Arrange(Area,null),Throws.InstanceOf<ArgumentException>());
        }
        [TestCase("nan")] [TestCase("negative-size")] [TestCase("null-row")] [TestCase("outside")] [TestCase("bad-area")]
        public void Invalid_geometry_is_rejected(string kind)
        {
            var request=R("a",200,100);var area=Area;
            switch(kind)
            {
                case "nan":request=R("a",float.NaN,100);break;
                case "negative-size":request=R("a",200,100,w:-1);break;
                case "null-row":request=null;break;
                case "outside":request=R("a",900,100);break;
                case "bad-area":area=new Rect(0,0,0,0);break;
            }
            Assert.That(()=>MapLabelLayout.Arrange(area,new[]{request}),Throws.InstanceOf<ArgumentException>());
        }
    }
}
