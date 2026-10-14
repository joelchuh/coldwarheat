using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;
namespace ColdWarStruggle.Presentation
{
    public sealed partial class DemoView
    {
        IReadOnlyDictionary<string,string> alternateNames;
        bool primaryNames=true;
        Rect lastMapArea=new Rect(0,0,2,1);
        public IReadOnlyDictionary<string,string> DisplayNames=>names;
        void ConfigureNameLanguages(IReadOnlyDictionary<string,string> alternate)
        {
            primaryNames=true;alternateNames=null;
            if(alternate==null)return;
            if(names==null || !new HashSet<string>(names.Keys,StringComparer.Ordinal).SetEquals(alternate.Keys)
                || names.Values.Any(string.IsNullOrWhiteSpace) || alternate.Values.Any(string.IsNullOrWhiteSpace))throw new ArgumentException("Name languages must cover identical IDs.");
            names=new ReadOnlyDictionary<string,string>(names.ToDictionary(p=>p.Key,p=>p.Value,StringComparer.Ordinal));
            alternateNames=new ReadOnlyDictionary<string,string>(alternate.ToDictionary(p=>p.Key,p=>p.Value,StringComparer.Ordinal));
        }
        public bool SwitchNameLanguage()
        {
            if(alternateNames==null)return false;
            var world=mapView as WorldMapView;var center=world?.Viewport.Center;var zoom=world?.Viewport.Zoom;
            mapPresenter.ReplaceNames(alternateNames);
            if(world!=null)world.Viewport.Focus(center.Value,zoom.Value,lastMapArea);
            var previous=names;names=alternateNames;alternateNames=previous;primaryNames=!primaryNames;
            return true;
        }
        void DrawNameLanguageButton()
        {
            if(alternateNames!=null && Button(1070,24,184,36,primaryNames?"名称：中文 → EN":"名称：EN → 中文"))
            { SwitchNameLanguage();GUIUtility.ExitGUI(); }
        }
    }
}
