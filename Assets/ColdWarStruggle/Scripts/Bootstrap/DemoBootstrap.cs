using System;
using System.IO;
using System.Linq;
using ColdWarStruggle.Infrastructure;
using ColdWarStruggle.Presentation;
using UnityEngine;
namespace ColdWarStruggle.Bootstrap
{
    public sealed class DemoBootstrap : MonoBehaviour
    {
        void Awake()
        {
            UnityEngine.Application.targetFrameRate=30;
            var view=gameObject.AddComponent<DemoView>();
            try
            {
#if UNITY_EDITOR
                string root=Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath,".."));
                var catalog=DemoCatalog.Load(Read(root,"Data/Maps/deluxe-2015/world.json"),Read(root,"Docs/Contracts/map.schema.json"),
                    Read(root,"Data/Demo/influence-scenarios.json"),Read(root,"Data/Rulesets/deluxe-2015/influence-placement.json"),Read(root,"Data/Localization/en-US.catalog-names.json"),Read(root,"Data/Maps/deluxe-2015/world.profile.json"));
#else
                string root=Path.Combine(UnityEngine.Application.streamingAssetsPath,"ColdWarStruggle");
                var catalog=DemoCatalog.Load(Read(root,"world.json"),Read(root,"map.schema.json"),Read(root,"influence-scenarios.json"),Read(root,"influence-placement.json"),Read(root,"names.json"),Read(root,"world.profile.json"));
#endif
                view.Initialize(new DemoController(catalog.CreateHost),catalog.Names,
                    catalog.Scenarios.Select(s=>new DemoScenarioOption(s.Id,s.Title,s.Actor,s.SelectedCountry)).ToArray());
            }
            catch(Exception e) { view.ShowError(e.Message);Debug.LogException(e); }
        }
        static string Read(string root,string file)=>File.ReadAllText(Path.Combine(root,file));
    }
}
