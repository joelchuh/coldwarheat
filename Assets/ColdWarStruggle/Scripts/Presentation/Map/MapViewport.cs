using System;
using UnityEngine;
namespace ColdWarStruggle.Presentation
{
    public sealed class MapViewport
    {
        public Vector2 Center { get; private set; }=new Vector2(.5f,.5f);
        public float Zoom { get; private set; }=1;
        public Vector2 Project(Vector2 point,Rect area)
        { Validate(point);var size=Size(area);return area.center+new Vector2((point.x-Center.x)*size.x,-(point.y-Center.y)*size.y); }
        public Vector2 Unproject(Vector2 screen,Rect area)
        { Validate(screen);var size=Size(area);var delta=screen-area.center;return Center+new Vector2(delta.x/size.x,-delta.y/size.y); }
        public void ZoomAt(float factor,Vector2 screen,Rect area)
        {
            if(!Finite(factor) || factor<=0)throw new ArgumentOutOfRangeException(nameof(factor));
            var before=Unproject(screen,area);Zoom=Mathf.Clamp(Zoom*factor,1,12);
            Center+=before-Unproject(screen,area);Clamp(area);
        }
        public void Pan(Vector2 pixels,Rect area)
        { Validate(pixels);var size=Size(area);Center+=new Vector2(-pixels.x/size.x,pixels.y/size.y);Clamp(area); }
        public void Focus(Vector2 point,float zoom,Rect area)
        {
            Validate(point);Size(area);
            if(point.x<0 || point.x>1 || point.y<0 || point.y>1 || !Finite(zoom) || zoom<=0)throw new ArgumentException("Invalid map focus.");
            Center=point;Zoom=Mathf.Clamp(zoom,1,12);Clamp(area);
        }
        public void Reset() { Center=new Vector2(.5f,.5f);Zoom=1; }
        void Clamp(Rect area)
        {
            var size=Size(area);float x=Mathf.Min(.5f,area.width/(2*size.x)),y=Mathf.Min(.5f,area.height/(2*size.y));
            Center=new Vector2(Mathf.Clamp(Center.x,x,1-x),Mathf.Clamp(Center.y,y,1-y));
        }
        Vector2 Size(Rect area)
        {
            if(!Finite(area.x) || !Finite(area.y) || !Finite(area.width) || !Finite(area.height) || area.width<=0 || area.height<=0 || area.width>100000 || area.height>100000)
                throw new ArgumentException("Invalid map viewport.");
            float width=Mathf.Min(area.width,area.height*2)*Zoom;return new Vector2(width,width/2);
        }
        static bool Finite(float f)=>!float.IsNaN(f) && !float.IsInfinity(f);
        static void Validate(Vector2 p) { if(!Finite(p.x) || !Finite(p.y))throw new ArgumentException("Invalid map coordinate."); }
    }
}
