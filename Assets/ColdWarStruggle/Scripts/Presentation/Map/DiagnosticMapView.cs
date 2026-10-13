using System;
using System.Collections.Generic;
using System.Linq;
using ColdWarStruggle.Core;
using UnityEngine;
namespace ColdWarStruggle.Presentation
{
    public enum DiagnosticMapKind { Tiles, Table }
    public abstract class DiagnosticMapView : MapViewBuffer
    {
        public abstract DiagnosticMapKind Kind { get; }
        protected abstract int Columns { get; }
        protected abstract float RowHeight { get; }
        protected virtual float HeaderHeight=>0;
        protected static readonly Color Normal=new Color(.10f,.14f,.20f),Selected=new Color(.18f,.27f,.39f),Green=new Color(.36f,.83f,.65f),Muted=new Color(.58f,.66f,.76f);
        Dictionary<CountryId,MapNode> nodes=new Dictionary<CountryId,MapNode>();
        Vector2 scroll;
        string previousRegion;
        CountryId? previousFocus;
        public override void Initialize(MapViewContext context)
        {
            base.Initialize(context);nodes=context.Nodes.ToDictionary(n=>n.Id);scroll=Vector2.zero;previousRegion=null;previousFocus=null;
        }
        public IReadOnlyList<CountryId> VisibleCountries(string region)=>Array.AsReadOnly(Context==null?Array.Empty<CountryId>():Context.Nodes.Where(n=>n.Region==region).Select(n=>n.Id).ToArray());
        public void Draw(Rect area,string region,GUIStyle label,GUIStyle small)
        {
            if(IsDisposed || Frame==null || Context==null)return;
            var current=Frame;var countries=current.Countries.Where(c=>nodes[c.Id].Region==region).ToArray();
            if(previousRegion!=region) { scroll=Vector2.zero;previousRegion=region; }
            if(FocusedCountry.HasValue && !Nullable.Equals(previousFocus,FocusedCountry))
            {
                var index=Array.FindIndex(countries,c=>c.Id.Equals(FocusedCountry.Value));
                if(index>=0) {scroll.y=(index/Columns)*RowHeight;previousFocus=FocusedCountry;}
            }
            DrawHeader(new Rect(area.x,area.y,area.width,HeaderHeight),small);
            var viewport=new Rect(area.x,area.y+HeaderHeight,area.width,area.height-HeaderHeight);
            float width=area.width-23,cellWidth=width/Columns;
            scroll=GUI.BeginScrollView(viewport,scroll,new Rect(0,0,width,((countries.Length+Columns-1)/Columns)*RowHeight));
            try
            {
                for(int i=0;i<countries.Length;i++)
                {
                    var c=countries[i];var box=new Rect((i%Columns)*cellWidth,(i/Columns)*RowHeight,cellWidth-8,RowHeight-8);
                    Block(box,c.Selected?Selected:Normal);Block(new Rect(box.x,box.y,4,box.height),c.Allowed?Green:Muted*.45f);
                    if(GUI.Button(box,"",GUIStyle.none))TrySelect(c.Id);
                    DrawCountry(box,nodes[c.Id],c,label,small);
                }
            }
            finally { GUI.EndScrollView(); }
        }
        protected virtual void DrawHeader(Rect area,GUIStyle small) {}
        protected abstract void DrawCountry(Rect box,MapNode node,MapCountryFrame country,GUIStyle label,GUIStyle small);
        protected static string Prefix(MapCountryFrame country)=>country.Drafted?"✓ ":country.Selected?"› ":"";
        protected static void Block(Rect r,Color color) {var old=GUI.color;GUI.color=color;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=old;}
        public override void Dispose() { base.Dispose();nodes.Clear(); }
        public static DiagnosticMapView Create(DiagnosticMapKind kind)
        {
            switch(kind)
            {
                case DiagnosticMapKind.Tiles:return new CountryTilesMapView();
                case DiagnosticMapKind.Table:return new CountryTableMapView();
                default:throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }
    }
    public sealed class CountryTilesMapView : DiagnosticMapView
    {
        public override DiagnosticMapKind Kind=>DiagnosticMapKind.Tiles;
        protected override int Columns=>2;
        protected override float RowHeight=>75;
        protected override void DrawCountry(Rect box,MapNode node,MapCountryFrame c,GUIStyle label,GUIStyle small)
        {
            GUI.Label(new Rect(box.x+12,box.y+6,box.width-20,27),Prefix(c)+node.Name,label);
            GUI.Label(new Rect(box.x+12,box.y+34,box.width-20,28),$"美 {c.US}   苏 {c.USSR}    稳定 {node.Stability}"+(node.Battleground?"  战场":""),small);
        }
    }
    public sealed class CountryTableMapView : DiagnosticMapView
    {
        public override DiagnosticMapKind Kind=>DiagnosticMapKind.Table;
        protected override int Columns=>1;
        protected override float RowHeight=>49;
        protected override float HeaderHeight=>30;
        protected override void DrawHeader(Rect r,GUIStyle small)
        {
            GUI.Label(new Rect(r.x+12,r.y,215,28),"国家",small);
            GUI.Label(new Rect(r.x+235,r.y,66,28),"美国",small);GUI.Label(new Rect(r.x+305,r.y,66,28),"苏联",small);
            GUI.Label(new Rect(r.x+375,r.y,96,28),"稳定 / 战场",small);GUI.Label(new Rect(r.x+492,r.y,92,28),"控制方",small);
        }
        protected override void DrawCountry(Rect r,MapNode n,MapCountryFrame c,GUIStyle label,GUIStyle small)
        {
            GUI.Label(new Rect(r.x+12,r.y+6,215,32),Prefix(c)+n.Name,label);
            GUI.Label(new Rect(r.x+235,r.y+6,66,32),c.US.ToString(),label);GUI.Label(new Rect(r.x+305,r.y+6,66,32),c.USSR.ToString(),label);
            GUI.Label(new Rect(r.x+375,r.y+6,106,32),n.Stability+(n.Battleground?" / 战场":""),small);
            GUI.Label(new Rect(r.x+492,r.y+6,92,32),c.Controller==PlayerSide.US?"美国":c.Controller==PlayerSide.USSR?"苏联":"—",small);
        }
    }
}
