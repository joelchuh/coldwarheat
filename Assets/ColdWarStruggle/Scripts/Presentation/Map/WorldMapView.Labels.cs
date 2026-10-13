using System.Linq;
using ColdWarStruggle.Core;
using UnityEngine;
namespace ColdWarStruggle.Presentation
{
    public sealed partial class WorldMapView
    {
        void DrawLabels(Rect area,GUIStyle small,CountryId? hovered)
        {
            if(Viewport.Zoom<5)return;
            var style=new GUIStyle(small){fontSize=12,wordWrap=false};
            var visible=Frame.Countries.Where(c=>area.Contains(Viewport.Project(assets.Anchors[c.Id.Value],area))).ToArray();
            var nodes=Context.Nodes.ToDictionary(n=>n.Id);
            var requests=visible.Select(c=>
            {
                float width=Mathf.Max(style.CalcSize(new GUIContent(nodes[c.Id].Name)).x,style.CalcSize(new GUIContent($"美{c.US} / 苏{c.USSR}")).x)+12;
                return new MapLabelRequest(c.Id,Viewport.Project(assets.Anchors[c.Id.Value],area),new Vector2(Mathf.Min(220,Mathf.Max(60,width)),36),
                    hovered.HasValue && c.Id.Equals(hovered.Value)?3:c.Selected?2:c.US>0 || c.USSR>0?1:0);
            }).ToArray();
            foreach(var placement in MapLabelLayout.Arrange(area,requests))
            {
                var c=visible.Single(c=>c.Id.Equals(placement.Country));var r=placement.Bounds;
                var anchor=Viewport.Project(assets.Anchors[c.Id.Value],area);
                var edge=new Vector2(Mathf.Clamp(anchor.x,r.xMin,r.xMax),Mathf.Clamp(anchor.y,r.yMin,r.yMax));
                var direction=edge-anchor;if(direction.magnitude>14)Line(anchor+direction.normalized*14,edge,Neutral*.75f);
                Paint(r,new Color(.025f,.065f,.095f,.88f));
                style.normal.textColor=c.Selected?Selected:Color.white;
                GUI.Label(new Rect(r.x+5,r.y,r.width-10,19),nodes[c.Id].Name,style);
                style.normal.textColor=Neutral;GUI.Label(new Rect(r.x+5,r.y+16,r.width-10,19),$"美{c.US} / 苏{c.USSR}",style);
            }
        }
        static void Line(Vector2 from,Vector2 to,Color color)
        {
            var delta=to-from;var matrix=GUI.matrix;
            try { GUIUtility.RotateAroundPivot(Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg,from);Paint(new Rect(from.x,from.y,delta.magnitude,1),color); }
            finally {GUI.matrix=matrix;}
        }
    }
}
