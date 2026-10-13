using System;
using System.Linq;
using ColdWarStruggle.Core;
using UnityEngine;
namespace ColdWarStruggle.Presentation
{
    public sealed partial class WorldMapView
    {
        Texture2D landTexture,markerTexture;
        int dragControl;
        static readonly Color Ocean=new Color(.035f,.09f,.13f),Neutral=new Color(.68f,.75f,.77f),Us=new Color(.32f,.66f,1),Ussr=new Color(1,.40f,.34f),Allowed=new Color(.32f,.89f,.65f),Selected=new Color(1,.84f,.33f);
        public override void Draw(Rect area,string region,GUIStyle label,GUIStyle small)
        {
            if(IsDisposed || Context==null || Frame==null)return;
            if(landTexture==null)landTexture=WorldMapTexture.Build(assets);
            if(markerTexture==null)markerTexture=WorldMapTexture.Marker();
            GUI.BeginGroup(area);
            try
            {
                var mapArea=new Rect(0,0,area.width,area.height-72);
                var currentSelection=Frame.Countries.SingleOrDefault(c=>c.Selected);
                if(GUI.Button(new Rect(0,0,70,28),"全图"))Viewport.Reset();
                if(GUI.Button(new Rect(78,0,80,28),"定位所选") && currentSelection!=null)
                    RequestCountryFocus(currentSelection.Id);
                if(GUI.Button(new Rect(166,0,32,28),"−"))Viewport.ZoomAt(1/1.4f,mapArea.center,mapArea);
                if(GUI.Button(new Rect(204,0,32,28),"+"))Viewport.ZoomAt(1.4f,mapArea.center,mapArea);
                GUI.Label(new Rect(250,0,area.width-250,28),$"{Viewport.Zoom:0.0}×  滚轮缩放 / 右键拖动",small);
                CountryId? detail=null;
                GUI.BeginGroup(new Rect(0,36,mapArea.width,mapArea.height));
                try
                {
                    PrepareViewport(mapArea);HandlePointer(mapArea);
                    Paint(mapArea,Ocean);
                    var topLeft=Viewport.Project(new Vector2(0,1),mapArea);var bottomRight=Viewport.Project(new Vector2(1,0),mapArea);
                    GUI.DrawTexture(new Rect(topLeft,bottomRight-topLeft),landTexture,ScaleMode.StretchToFill);
                    var hovered=HitTest(Event.current.mousePosition,mapArea);
                    var selected=Frame.Countries.SingleOrDefault(c=>c.Selected);
                    foreach(var c in VisibleMarkers(mapArea,Event.current.mousePosition))DrawMarker(c,mapArea,small,hovered.HasValue && c.Id.Equals(hovered.Value));
                    DrawLabels(mapArea,small,hovered);
                    DrawPower("superpower.us","US",Us,mapArea,small);DrawPower("superpower.ussr","USSR",Ussr,mapArea,small);
                    detail=hovered??selected?.Id;
                }
                finally {GUI.EndGroup();}
                if(detail.HasValue)DrawDetail(detail.Value,new Rect(0,0,area.width,area.height),small);
            }
            finally {GUI.EndGroup();}
        }
        void HandlePointer(Rect area)
        {
            int id=GUIUtility.GetControlID(FocusType.Passive);var e=Event.current;
            if(e.type==EventType.ScrollWheel && area.Contains(e.mousePosition))
            { Viewport.ZoomAt(Mathf.Pow(1.16f,-Mathf.Clamp(e.delta.y,-10,10)),e.mousePosition,area);e.Use(); }
            if(e.type==EventType.MouseDown && area.Contains(e.mousePosition))
            {
                if(e.button==1) {GUIUtility.hotControl=id;dragControl=id;e.Use();}
                else if(e.button==0) {SelectAt(e.mousePosition,area);e.Use();}
            }
            if(e.type==EventType.MouseDrag && dragControl==id && GUIUtility.hotControl==id)
            {Viewport.Pan(e.delta,area);e.Use();}
            if(e.rawType==EventType.MouseUp && dragControl==id && GUIUtility.hotControl==id)
            {GUIUtility.hotControl=0;dragControl=0;e.Use();}
        }
        void FocusRegion(string region,Rect area)
        {
            var points=Context.Nodes.Where(n=>n.Region==region).Select(n=>assets.Anchors[n.Id.Value]).ToArray();if(points.Length==0)return;
            var min=new Vector2(points.Min(p=>p.x),points.Min(p=>p.y));var max=new Vector2(points.Max(p=>p.x),points.Max(p=>p.y));
            float width=Mathf.Min(area.width,area.height*2);
            float zoom=Mathf.Min((area.width-50)/(width*Mathf.Max(.03f,max.x-min.x)),(area.height-60)/(width*.5f*Mathf.Max(.03f,max.y-min.y)));
            Viewport.Focus((min+max)/2,Mathf.Max(1,zoom),area);
        }
        void DrawMarker(MapCountryFrame c,Rect area,GUIStyle small,bool hovered)
        {
            var p=Viewport.Project(assets.Anchors[c.Id.Value],area);if(!area.Contains(p))return;
            float radius=c.Selected || hovered?10:7;
            if(c.Allowed || c.Selected || c.Drafted || hovered)Circle(p,radius+3,c.Selected?Selected:c.Drafted || hovered?Color.white:Allowed);
            Circle(p,radius,c.Controller==PlayerSide.US?Us:c.Controller==PlayerSide.USSR?Ussr:Neutral);
            Circle(p,radius-2,Ocean);
            if(c.Drafted)GUI.Label(new Rect(p.x-6,p.y-10,18,20),"✓",small);
            else if(c.US>0 || c.USSR>0)Circle(p,3,c.US>c.USSR?Us:c.USSR>c.US?Ussr:Color.white);
        }
        void DrawPower(string id,string text,Color color,Rect area,GUIStyle small)
        {
            var p=Viewport.Project(assets.Anchors[id],area);if(!area.Contains(p))return;
            var style=new GUIStyle(small){fontSize=16,fontStyle=FontStyle.Bold};style.normal.textColor=color;
            GUI.Label(new Rect(p.x-25,p.y-12,74,26),text,style);
        }
        void DrawDetail(CountryId id,Rect area,GUIStyle small)
        {
            var n=Context.Nodes.Single(n=>n.Id.Equals(id));var c=Frame.Countries.Single(c=>c.Id.Equals(id));
            Paint(new Rect(0,area.height-32,area.width,32),new Color(.045f,.07f,.10f));
            GUI.Label(new Rect(10,area.height-30,area.width-16,28),$"{n.Name}   美 {c.US} / 苏 {c.USSR}   稳定 {n.Stability}"+(n.Battleground?"  战场":"")+(c.Allowed?"  · 合法目标":""),small);
        }
        void Circle(Vector2 p,float radius,Color color)
        {var old=GUI.color;GUI.color=color;GUI.DrawTexture(new Rect(p.x-radius,p.y-radius,radius*2,radius*2),markerTexture);GUI.color=old;}
        static void Paint(Rect r,Color color)
        {var old=GUI.color;GUI.color=color;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=old;}
    }
}
