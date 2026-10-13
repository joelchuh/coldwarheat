using System;
using System.Collections.Generic;
using System.Linq;
using ColdWarStruggle.Core;
using UnityEngine;
namespace ColdWarStruggle.Presentation
{
    public sealed partial class WorldMapView : DrawableMapView
    {
        readonly WorldMapAssets assets;
        string pendingRegion;
        CountryId? pendingCountry;
        public override DiagnosticMapKind Kind=>DiagnosticMapKind.World;
        public MapViewport Viewport { get; }=new MapViewport();
        public WorldMapView(WorldMapAssets assets) { this.assets=assets??throw new ArgumentNullException(nameof(assets)); }
        public override void Initialize(MapViewContext context)
        {
            base.Initialize(context);
            if(context.Nodes.Count!=assets.Anchors.Count-2 || context.Nodes.Any(n=>!assets.Anchors.ContainsKey(n.Id.Value)))
            { Dispose();throw new ArgumentException("World layout does not cover the map context."); }
            Viewport.Reset();pendingRegion=null;pendingCountry=null;
        }
        public CountryId? HitTest(Vector2 point,Rect area)
        {
            if(IsDisposed || Context==null || Frame==null || !area.Contains(point))return null;
            CountryId? best=null;float distance=121;
            foreach(var node in Context.Nodes.OrderBy(n=>n.Id.Value,StringComparer.Ordinal))
            {
                var position=Viewport.Project(assets.Anchors[node.Id.Value],area);
                if(!area.Contains(position))continue;
                float d=(position-point).sqrMagnitude;
                if(d<distance || d==distance && !best.HasValue) { best=node.Id;distance=d; }
            }
            return best;
        }
        public bool SelectAt(Vector2 point,Rect area)
        { var country=HitTest(point,area);return country.HasValue && TrySelect(country.Value); }
        public bool RequestRegionFocus(string region)
        {
            if(IsDisposed || Frame==null || Context==null || !Context.Nodes.Any(n=>n.Region==region))return false;
            pendingRegion=region;pendingCountry=null;return true;
        }
        public bool RequestCountryFocus(CountryId country)
        {
            if(IsDisposed || Frame==null || Context==null || !Context.Nodes.Any(n=>n.Id.Equals(country)))return false;
            pendingCountry=country;pendingRegion=null;return true;
        }
        public void PrepareViewport(Rect area)
        {
            if(IsDisposed || Frame==null || Context==null)return;
            if(pendingCountry.HasValue)Viewport.Focus(assets.Anchors[pendingCountry.Value.Value],8,area);
            else if(pendingRegion!=null)FocusRegion(pendingRegion,area);
            pendingCountry=null;pendingRegion=null;
        }
        public IReadOnlyList<MapCountryFrame> VisibleMarkers(Rect area,Vector2 pointer)
        {
            if(IsDisposed || Frame==null || Context==null)return Array.Empty<MapCountryFrame>();
            var hovered=HitTest(pointer,area);
            return Array.AsReadOnly(Frame.Countries.Where(c=>area.Contains(Viewport.Project(assets.Anchors[c.Id.Value],area)))
                .OrderBy(c=>hovered.HasValue && c.Id.Equals(hovered.Value)?2:c.Selected?1:0)
                .ThenBy(c=>c.Id.Value,StringComparer.Ordinal).ToArray());
        }
        public override void Dispose()
        {
            if(IsDisposed)return;ReleaseTexture(landTexture);ReleaseTexture(markerTexture);
            landTexture=null;markerTexture=null;
            if(dragControl!=0 && GUIUtility.hotControl==dragControl)GUIUtility.hotControl=0;
            dragControl=0;pendingCountry=null;pendingRegion=null;base.Dispose();
        }
        static void ReleaseTexture(Texture2D texture)
        { if(texture==null)return;if(UnityEngine.Application.isPlaying)UnityEngine.Object.Destroy(texture);else UnityEngine.Object.DestroyImmediate(texture); }
    }
}
