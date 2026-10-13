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
            gameObject.AddComponent<DemoCapture>();
            try
            {
#if UNITY_EDITOR
                string root=Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath,".."));
                var catalog=DemoCatalog.Load(Read(root,"Data/Maps/deluxe-2015/world.json"),Read(root,"Docs/Contracts/map.schema.json"),
                    Read(root,"Data/Demo/influence-scenarios.json"),Read(root,"Data/Rulesets/deluxe-2015/influence-placement.json"),Read(root,"Data/Localization/en-US.catalog-names.json"),Read(root,"Data/Maps/deluxe-2015/world.profile.json"))
                    .WithEventScenarios(Read(root,"Data/Demo/event-scenarios.json"),Read(root,"Data/Cards/deluxe-2015/early-influence-batch1.parameters.json"),Read(root,"Docs/Contracts/early-influence-parameters.schema.json"))
                    .WithAllocationScenarios(Read(root,"Data/Demo/allocation-scenarios.json"),Read(root,"Data/Cards/deluxe-2015/early-influence-batch2.parameters.json"),Read(root,"Docs/Contracts/early-influence-batch2.schema.json"),Read(root,"Data/Cards/deluxe-2015/cards.json"))
                    .WithEraScenarios(Read(root,"Data/Demo/era-scenarios.json"),Read(root,"Data/Cards/deluxe-2015/early-influence-batch2.parameters.json"),Read(root,"Docs/Contracts/early-influence-batch2.schema.json"),Read(root,"Data/Cards/deluxe-2015/cards.json"),Read(root,"Data/Rulesets/deluxe-2015/turn-flow.json"));
#else
                string root=Path.Combine(UnityEngine.Application.streamingAssetsPath,"ColdWarStruggle");
                var catalog=DemoCatalog.Load(Read(root,"world.json"),Read(root,"map.schema.json"),Read(root,"influence-scenarios.json"),Read(root,"influence-placement.json"),Read(root,"names.json"),Read(root,"world.profile.json"))
                    .WithEventScenarios(Read(root,"event-scenarios.json"),Read(root,"early-influence-batch1.parameters.json"),Read(root,"early-influence-parameters.schema.json"))
                    .WithAllocationScenarios(Read(root,"allocation-scenarios.json"),Read(root,"early-influence-batch2.parameters.json"),Read(root,"early-influence-batch2.schema.json"),Read(root,"cards.json"))
                    .WithEraScenarios(Read(root,"era-scenarios.json"),Read(root,"early-influence-batch2.parameters.json"),Read(root,"early-influence-batch2.schema.json"),Read(root,"cards.json"),Read(root,"turn-flow.json"));
#endif
                view.Initialize(new DemoController(catalog.CreateHost),catalog.Names,
                    catalog.Scenarios.Select(s=>new DemoScenarioOption(s.Id,s.Title,s.Actor,s.SelectedCountry)).ToArray(),
                    DemoLaunchOptions.ScenarioIndex(Environment.GetCommandLineArgs(),catalog.Scenarios.Select(s=>s.Id).ToArray()),
                    DemoLaunchOptions.MapKind(Environment.GetCommandLineArgs()));
            }
            catch(Exception e) { view.ShowError(e.Message);Debug.LogException(e); }
        }
        static string Read(string root,string file)=>File.ReadAllText(Path.Combine(root,file));
    }
}
