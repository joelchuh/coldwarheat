using System;
using System.Collections.Generic;
using System.Linq;
using ColdWarStruggle.Core;
using UnityEngine;
namespace ColdWarStruggle.Presentation
{
    public sealed class MapLabelRequest
    {
        public CountryId Country { get; }
        public Vector2 Anchor { get; }
        public Vector2 Size { get; }
        public int Priority { get; }
        public MapLabelRequest(CountryId country,Vector2 anchor,Vector2 size,int priority)
        { Country=country;Anchor=anchor;Size=size;Priority=priority; }
    }
    public sealed class MapLabelPlacement
    {
        public CountryId Country { get; }
        public Rect Bounds { get; }
        internal MapLabelPlacement(CountryId country,Rect bounds) {Country=country;Bounds=bounds;}
    }
    public static class MapLabelLayout
    {
        public static IReadOnlyList<MapLabelPlacement> Arrange(Rect area,IEnumerable<MapLabelRequest> requests)
        {
            if(requests==null || !Finite(area.x) || !Finite(area.y) || !Finite(area.width) || !Finite(area.height) || area.width<=0 || area.height<=0 || area.width>100000 || area.height>100000)
                throw new ArgumentException("Invalid map label viewport.");
            var nodes=requests.Take(129).ToArray();
            if(nodes.Length>128 || nodes.Any(n=>n==null || string.IsNullOrWhiteSpace(n.Country.Value) || !Finite(n.Anchor.x) || !Finite(n.Anchor.y) || !area.Contains(n.Anchor)
                || !Finite(n.Size.x) || !Finite(n.Size.y) || n.Size.x<=0 || n.Size.y<=0 || n.Size.x>4096 || n.Size.y>4096 || n.Priority<0 || n.Priority>100)
                || nodes.Select(n=>n.Country).Distinct().Count()!=nodes.Length)throw new ArgumentException("Invalid map label requests.");
            var obstacles=nodes.Select(n=>new Rect(n.Anchor-Vector2.one*14,Vector2.one*28)).ToArray();
            var placed=new List<MapLabelPlacement>();
            foreach(var n in nodes.OrderByDescending(n=>n.Priority).ThenBy(n=>n.Country.Value,StringComparer.Ordinal))
                foreach(var position in Candidates(n.Anchor,n.Size))
                {
                    var box=new Rect(position,n.Size);var padded=new Rect(box.x-2,box.y-2,box.width+4,box.height+4);
                    if(padded.xMin<area.xMin || padded.yMin<area.yMin || padded.xMax>area.xMax || padded.yMax>area.yMax ||
                        obstacles.Any(r=>r.Overlaps(padded)) || placed.Any(p=>p.Bounds.Overlaps(padded)))continue;
                    placed.Add(new MapLabelPlacement(n.Country,box));break;
                }
            return placed.AsReadOnly();
        }
        static IEnumerable<Vector2> Candidates(Vector2 p,Vector2 size)
        {
            yield return p+new Vector2(16,-size.y/2);
            yield return p+new Vector2(-size.x-16,-size.y/2);
            yield return p+new Vector2(-size.x/2,-size.y-16);
            yield return p+new Vector2(-size.x/2,16);
            yield return p+new Vector2(16,-size.y-8);
            yield return p+new Vector2(16,8);
            yield return p+new Vector2(-size.x-16,-size.y-8);
            yield return p+new Vector2(-size.x-16,8);
        }
        static bool Finite(float f)=>!float.IsNaN(f) && !float.IsInfinity(f);
    }
}
