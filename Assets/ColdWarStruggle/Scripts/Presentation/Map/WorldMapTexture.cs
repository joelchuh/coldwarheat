using System.Collections.Generic;
using UnityEngine;
namespace ColdWarStruggle.Presentation
{
    // Rasterized once per adapter, from public-domain geometry; never queried by rule logic.
    internal static class WorldMapTexture
    {
        public static Texture2D Build(WorldMapAssets assets)
        {
            const int width=2048,height=1024;
            var ocean=new Color32(9,23,33,255);var land=new Color32(49,72,77,255);var grid=new Color32(26,44,53,255);
            var pixels=new Color32[width*height];for(int i=0;i<pixels.Length;i++)pixels[i]=ocean;
            var intersections=new List<float>();
            for(int y=0;y<height;y++)
            {
                float latitude=(y+.5f)/height;intersections.Clear();
                foreach(var ring in assets.LandRings)
                    for(int i=1;i<ring.Count;i++)
                    {
                        var a=ring[i-1];var b=ring[i];
                        if((a.y<=latitude && b.y>latitude) || (b.y<=latitude && a.y>latitude))
                            intersections.Add(a.x+(latitude-a.y)*(b.x-a.x)/(b.y-a.y));
                    }
                intersections.Sort();
                for(int i=0;i+1<intersections.Count;i+=2)
                {
                    int first=Mathf.Clamp(Mathf.CeilToInt(intersections[i]*width-.5f),0,width-1),last=Mathf.Clamp(Mathf.FloorToInt(intersections[i+1]*width-.5f),0,width-1);
                    for(int x=first;x<=last;x++)pixels[y*width+x]=land;
                }
            }
            for(int n=1;n<12;n++) {int x=n*width/12;for(int y=0;y<height;y++)if(pixels[y*width+x].Equals(ocean))pixels[y*width+x]=grid;}
            for(int n=1;n<6;n++) {int y=n*height/6;for(int x=0;x<width;x++)if(pixels[y*width+x].Equals(ocean))pixels[y*width+x]=grid;}
            var texture=new Texture2D(width,height,TextureFormat.RGBA32,false){name="World placeholder land",wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear};
            texture.SetPixels32(pixels);texture.Apply(false,true);return texture;
        }
        public static Texture2D Marker()
        {
            const int size=32;var pixels=new Color32[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {float d=Vector2.Distance(new Vector2(x+.5f,y+.5f),new Vector2(16,16));pixels[y*size+x]=new Color(1,1,1,Mathf.Clamp01(16-d));}
            var texture=new Texture2D(size,size,TextureFormat.RGBA32,false){name="World marker",wrapMode=TextureWrapMode.Clamp};texture.SetPixels32(pixels);texture.Apply(false,true);return texture;
        }
    }
}
