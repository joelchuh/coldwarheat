using System;
using System.Collections.Generic;
using System.Linq;
using ColdWarStruggle.Core;
using ColdWarStruggle.Presentation;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class MapViewTests
    {
        internal static readonly CountryId France=new CountryId("country.france"),Angola=new CountryId("country.angola");
        internal static MapNode[] Nodes()=>new[]{new MapNode(France,"法国","region.europe",3,true),new MapNode(Angola,"安哥拉","region.africa",1,true)};
        internal static MapCountryFrame[] Countries()=>new[]{new MapCountryFrame(France,3,0,PlayerSide.US,true,false,true),new MapCountryFrame(Angola,0,1,PlayerSide.USSR,false,false,false)};
        internal static MapFrame Frame(string context="a",long version=0,long sequence=1,string interaction="ops",IEnumerable<MapCountryFrame> countries=null)=>new MapFrame(context,version,sequence,interaction,countries??Countries());
        [Test] public void Context_and_frame_take_immutable_copies()
        {
            var nodes=Nodes();var context=new MapViewContext("a",nodes);nodes[0]=nodes[1];Assert.That(context.Nodes[0].Id,Is.EqualTo(France));
            Assert.Throws<NotSupportedException>(()=>((IList<MapNode>)context.Nodes).Clear());
            var countries=Countries();var f=Frame(countries:countries);countries[0]=countries[1];Assert.That(f.Countries[0].Id,Is.EqualTo(France));
            Assert.Throws<NotSupportedException>(()=>((IList<MapCountryFrame>)f.Countries).Clear());
        }
        [Test] public void Duplicate_or_empty_country_mapping_is_rejected()
        {
            Assert.Throws<ArgumentException>(()=>new MapViewContext("a",Array.Empty<MapNode>()));
            Assert.Throws<ArgumentException>(()=>new MapViewContext("a",new[]{Nodes()[0],Nodes()[0]}));
            Assert.Throws<ArgumentException>(()=>Frame(countries:new[]{Countries()[0],Countries()[0]}));
        }
        [Test] public void Unknown_missing_or_wrong_context_frame_cannot_replace_current_frame()
        {
            var v=new MapViewBuffer();v.Initialize(new MapViewContext("a",Nodes()));var first=Frame();Assert.That(v.Present(first),Is.True);
            Assert.That(v.Present(Frame("b",sequence:2)),Is.False);
            Assert.That(v.Present(Frame(sequence:2,countries:Countries().Take(1))),Is.False);
            var unknown=new MapCountryFrame(new CountryId("country.unknown"),0,0,null,false,false,false);
            Assert.That(v.Present(Frame(sequence:2,countries:new[]{Countries()[0],unknown})),Is.False);Assert.That(v.Frame,Is.SameAs(first));
        }
        [Test] public void Versions_and_frame_sequence_prevent_rollback_but_allow_same_version_draft_updates()
        {
            var v=new MapViewBuffer();v.Initialize(new MapViewContext("a",Nodes()));Assert.That(v.Present(Frame(version:2,sequence:3)),Is.True);
            Assert.That(v.Present(Frame(version:1,sequence:4)),Is.False);Assert.That(v.Present(Frame(version:2,sequence:2)),Is.False);
            Assert.That(v.Present(Frame(version:2,sequence:3)),Is.False);Assert.That(v.Present(Frame(version:2,sequence:4)),Is.True);
        }
        [Test] public void Picking_requires_known_country_and_current_frame_and_reports_stamped_intent()
        {
            var v=new MapViewBuffer();v.Initialize(new MapViewContext("a",Nodes()));MapIntent seen=null;v.Intent+=x=>seen=x;
            Assert.That(v.TrySelect(France),Is.False);v.Present(Frame());Assert.That(v.TrySelect(new CountryId("unknown")),Is.False);
            Assert.That(v.TrySelect(Angola),Is.True);Assert.That(seen.Country,Is.EqualTo(Angola));Assert.That(seen.ContextId,Is.EqualTo("a"));Assert.That(seen.Sequence,Is.EqualTo(1));
            // Non-actionable countries remain inspectable; the view does not turn selection into a command.
            Assert.That(v.Frame.Countries[1].USSR,Is.EqualTo(1));
        }
        [Test] public void Clear_reinitialize_focus_and_dispose_do_not_reuse_old_interaction()
        {
            var v=new MapViewBuffer();v.Initialize(new MapViewContext("a",Nodes()));v.Present(Frame());v.FocusOn(France);Assert.That(v.FocusedCountry,Is.EqualTo(France));
            Assert.Throws<ArgumentException>(()=>v.FocusOn(new CountryId("unknown")));
            v.Clear();Assert.That(v.Frame,Is.Null);Assert.That(v.TrySelect(France),Is.False);
            v.Initialize(new MapViewContext("b",Nodes()));Assert.That(v.Present(Frame()),Is.False);Assert.That(v.Present(Frame("b")),Is.True);
            v.Dispose();v.Dispose();Assert.That(v.IsDisposed,Is.True);Assert.That(v.Frame,Is.Null);Assert.That(v.Context,Is.Null);Assert.That(v.TrySelect(France),Is.False);
            Assert.Throws<ObjectDisposedException>(()=>v.Initialize(new MapViewContext("c",Nodes())));Assert.Throws<ObjectDisposedException>(()=>v.FocusOn(France));Assert.That(v.Present(Frame("b",sequence:2)),Is.False);
        }
        [Test] public void Clearing_display_does_not_allow_an_old_frame_to_return()
        {
            var v=new MapViewBuffer();v.Initialize(new MapViewContext("a",Nodes()));v.Present(Frame(version:2,sequence:5));v.Clear();
            Assert.That(v.Present(Frame(version:2,sequence:5)),Is.False);Assert.That(v.Present(Frame(version:1,sequence:6)),Is.False);
            Assert.That(v.Present(Frame(version:2,sequence:6)),Is.True);
        }
        [Test] public void Invalid_initialization_leaves_no_old_mapping_or_frame_available()
        {
            var v=new MapViewBuffer();v.Initialize(new MapViewContext("a",Nodes()));v.Present(Frame());
            Assert.Throws<ArgumentNullException>(()=>v.Initialize(null));Assert.That(v.Context,Is.Null);Assert.That(v.Frame,Is.Null);
            Assert.That(v.Present(Frame(sequence:2)),Is.False);Assert.That(v.TrySelect(France),Is.False);
        }
    }
}
