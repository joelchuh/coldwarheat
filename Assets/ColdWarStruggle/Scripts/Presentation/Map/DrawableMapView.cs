using System;
using UnityEngine;
namespace ColdWarStruggle.Presentation
{
    public abstract class DrawableMapView : MapViewBuffer
    {
        public abstract DiagnosticMapKind Kind { get; }
        public abstract void Draw(Rect area,string region,GUIStyle label,GUIStyle small);
        public static DrawableMapView CreateView(DiagnosticMapKind kind,WorldMapAssets assets)
        {
            if(kind==DiagnosticMapKind.World)return new WorldMapView(assets);
            return DiagnosticMapView.Create(kind);
        }
        public static DiagnosticMapKind NextKind(DiagnosticMapKind current,bool worldAvailable)
        {
            switch(current)
            {
                case DiagnosticMapKind.Tiles:return DiagnosticMapKind.Table;
                case DiagnosticMapKind.Table:return worldAvailable?DiagnosticMapKind.World:DiagnosticMapKind.Tiles;
                case DiagnosticMapKind.World when worldAvailable:return DiagnosticMapKind.Tiles;
                default:throw new ArgumentOutOfRangeException(nameof(current));
            }
        }
    }
}
