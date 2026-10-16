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
                var printedCards=CardCatalogLoader.LoadPrintedSubset(Read(root,"Data/Cards/deluxe-2015/cards.json"),Read(root,"Docs/Contracts/cards-v2.schema.json"),Read(root,"Data/Cards/deluxe-2015/cards.printed-profile.json"));
                var catalog=DemoCatalog.Load(Read(root,"Data/Maps/deluxe-2015/world.json"),Read(root,"Docs/Contracts/map.schema.json"),
                    Read(root,"Data/Demo/influence-scenarios.json"),Read(root,"Data/Rulesets/deluxe-2015/influence-placement.json"),Read(root,"Data/Localization/en-US.catalog-names.json"),Read(root,"Data/Maps/deluxe-2015/world.profile.json"))
                    .WithEventScenarios(Read(root,"Data/Demo/event-scenarios.json"),Read(root,"Data/Cards/deluxe-2015/early-influence-batch1.parameters.json"),Read(root,"Docs/Contracts/early-influence-parameters.schema.json"))
                    .WithAllocationScenarios(Read(root,"Data/Demo/allocation-scenarios.json"),Read(root,"Data/Cards/deluxe-2015/early-influence-batch2.parameters.json"),Read(root,"Docs/Contracts/early-influence-batch2.schema.json"),Read(root,"Data/Cards/deluxe-2015/cards.json"))
                    .WithEraScenarios(Read(root,"Data/Demo/era-scenarios.json"),Read(root,"Data/Cards/deluxe-2015/early-influence-batch2.parameters.json"),Read(root,"Docs/Contracts/early-influence-batch2.schema.json"),Read(root,"Data/Cards/deluxe-2015/cards.json"),Read(root,"Data/Rulesets/deluxe-2015/turn-flow.json"))
                    .WithDeploymentScenario(Read(root,"Data/Demo/deployment-scenario.json"),Read(root,"Data/Rulesets/deluxe-2015/setup.json"),Read(root,"Docs/Contracts/setup.schema.json"))
                    .WithInitialDealScenario(Read(root,"Data/Demo/initial-deal-scenario.json"),Read(root,"Data/Rulesets/deluxe-2015/setup.json"),Read(root,"Docs/Contracts/setup.schema.json"),Read(root,"Data/Rulesets/deluxe-2015/turn-flow.json"),printedCards,Read(root,"Data/Localization/en-US.catalog-names.json"))
                    .WithActionPractice(Read(root,"Data/Cards/deluxe-2015/persistent-effects-batch1.parameters.json"),Read(root,"Docs/Contracts/persistent-effects-batch1.schema.json"),Read(root,"Data/Rulesets/deluxe-2015/space-race.json"),Read(root,"Data/Rulesets/deluxe-2015/turn-flow.json"))
                    .WithPracticeRisk(Read(root,"Data/Rulesets/deluxe-2015/coup.json"),Read(root,"Data/Rulesets/deluxe-2015/defcon.json"),Read(root,"Data/Rulesets/deluxe-2015/military-operations.json"),Read(root,"Data/Rulesets/deluxe-2015/victory.json"))
                    .WithPracticeScoring(Read(root,"Data/Rulesets/deluxe-2015/region-scoring.json"),Read(root,"Docs/Contracts/region-scoring-rules.schema.json"))
                    .WithPracticeInfluenceEvents(Read(root,"Data/Cards/deluxe-2015/early-influence-batch1.parameters.json"),Read(root,"Docs/Contracts/early-influence-parameters.schema.json"),Read(root,"Data/Cards/deluxe-2015/early-influence-batch2.parameters.json"),Read(root,"Docs/Contracts/early-influence-batch2.schema.json"),Read(root,"Data/Cards/deluxe-2015/cards.json"))
                    .WithPracticeDefconVpEvents(Read(root,"Data/Cards/deluxe-2015/defcon-vp-events.parameters.json"),Read(root,"Docs/Contracts/defcon-vp-events.schema.json"))
                    .WithPracticeTurnOpsEvents(Read(root,"Data/Cards/deluxe-2015/persistent-effects-batch1.parameters.json"),Read(root,"Docs/Contracts/persistent-effects-batch1.schema.json"))
                    .WithPracticeRealignment(Read(root,"Data/Rulesets/deluxe-2015/realignment.json"))
                    .WithPracticeChina(Read(root,"Data/Cards/deluxe-2015/persistent-effects-batch1.parameters.json"),Read(root,"Docs/Contracts/persistent-effects-batch1.schema.json"))
                    .WithPracticeTurnEnd()
                    .WithPracticeNextTurn()
                    .WithQuickStartScenario(Read(root,"Data/Demo/quick-start-scenario.json"))
                    .WithHeadlineScenario(Read(root,"Data/Demo/headline-scenario.json"),Read(root,"Data/Rulesets/deluxe-2015/turn-flow.json"),Read(root,"Data/Cards/deluxe-2015/defectors.parameters.json"),Read(root,"Docs/Contracts/defectors.schema.json"))
                    .WithFinalScoringScenario(Read(root,"Data/Demo/final-scoring-scenario.json"),Read(root,"Data/Rulesets/deluxe-2015/region-scoring.json"),Read(root,"Docs/Contracts/region-scoring-rules.schema.json"),Read(root,"Data/Rulesets/deluxe-2015/scoring-card-parameters.json"),Read(root,"Docs/Contracts/scoring-card-parameters.schema.json"))
                    .WithSpaceDiscardScenarios(Read(root,"Data/Demo/space-end-turn-discard-scenarios.json"))
                    .WithSpaceExtraScenarios(Read(root,"Data/Demo/space-extra-action-scenarios.json"))
                    .WithEmptyHandScenario(Read(root,"Data/Demo/empty-hand-scenario.json"))
                    .WithIndependentReds(Read(root,"Data/Cards/deluxe-2015/early-influence-batch3.parameters.json"),Read(root,"Docs/Contracts/early-influence-batch3.schema.json"),Read(root,"Data/Cards/deluxe-2015/cards.json"))
                    .WithIndependentRedsScenario(Read(root,"Data/Demo/independent-reds-scenario.json"))
                    .WithBlockade(Read(root,"Data/Cards/deluxe-2015/hand-events-batch1.parameters.json"),Read(root,"Docs/Contracts/hand-events-batch1.schema.json"),Read(root,"Data/Cards/deluxe-2015/cards.json"))
                    .WithBlockadeScenario(Read(root,"Data/Demo/blockade-scenario.json"))
                    .WithFiveYearPlan(Read(root,"Data/Cards/deluxe-2015/hand-events-batch1.parameters.json"),Read(root,"Docs/Contracts/hand-events-batch1.schema.json"),Read(root,"Data/Cards/deluxe-2015/cards.json"))
                    .WithFiveYearPlanScenario(Read(root,"Data/Demo/five-year-plan-scenario.json"));
                var displayNames=CatalogNamesLoader.Load(Read(root,"Data/Localization/zh-CN.catalog-names.json"),"zh-CN",catalog.Names.Keys);
                var worldAssets=WorldMapAssets.Load(Read(root,"Data/Presentation/world-layout.json"),Read(root,"Data/Presentation/world-land.json"),catalog.Map);
#else
                string root=Path.Combine(UnityEngine.Application.streamingAssetsPath,"ColdWarStruggle");
                var printedCards=CardCatalogLoader.LoadPrintedSubset(Read(root,"cards.json"),Read(root,"cards-v2.schema.json"),Read(root,"cards.printed-profile.json"));
                var catalog=DemoCatalog.Load(Read(root,"world.json"),Read(root,"map.schema.json"),Read(root,"influence-scenarios.json"),Read(root,"influence-placement.json"),Read(root,"names.json"),Read(root,"world.profile.json"))
                    .WithEventScenarios(Read(root,"event-scenarios.json"),Read(root,"early-influence-batch1.parameters.json"),Read(root,"early-influence-parameters.schema.json"))
                    .WithAllocationScenarios(Read(root,"allocation-scenarios.json"),Read(root,"early-influence-batch2.parameters.json"),Read(root,"early-influence-batch2.schema.json"),Read(root,"cards.json"))
                    .WithEraScenarios(Read(root,"era-scenarios.json"),Read(root,"early-influence-batch2.parameters.json"),Read(root,"early-influence-batch2.schema.json"),Read(root,"cards.json"),Read(root,"turn-flow.json"))
                    .WithDeploymentScenario(Read(root,"deployment-scenario.json"),Read(root,"setup.json"),Read(root,"setup.schema.json"))
                    .WithInitialDealScenario(Read(root,"initial-deal-scenario.json"),Read(root,"setup.json"),Read(root,"setup.schema.json"),Read(root,"turn-flow.json"),printedCards,Read(root,"names.json"))
                    .WithActionPractice(Read(root,"persistent-effects-batch1.parameters.json"),Read(root,"persistent-effects-batch1.schema.json"),Read(root,"space-race.json"),Read(root,"turn-flow.json"))
                    .WithPracticeRisk(Read(root,"coup.json"),Read(root,"defcon.json"),Read(root,"military-operations.json"),Read(root,"victory.json"))
                    .WithPracticeScoring(Read(root,"region-scoring.json"),Read(root,"region-scoring-rules.schema.json"))
                    .WithPracticeInfluenceEvents(Read(root,"early-influence-batch1.parameters.json"),Read(root,"early-influence-parameters.schema.json"),Read(root,"early-influence-batch2.parameters.json"),Read(root,"early-influence-batch2.schema.json"),Read(root,"cards.json"))
                    .WithPracticeDefconVpEvents(Read(root,"defcon-vp-events.parameters.json"),Read(root,"defcon-vp-events.schema.json"))
                    .WithPracticeTurnOpsEvents(Read(root,"persistent-effects-batch1.parameters.json"),Read(root,"persistent-effects-batch1.schema.json"))
                    .WithPracticeRealignment(Read(root,"realignment.json"))
                    .WithPracticeChina(Read(root,"persistent-effects-batch1.parameters.json"),Read(root,"persistent-effects-batch1.schema.json"))
                    .WithPracticeTurnEnd()
                    .WithPracticeNextTurn()
                    .WithQuickStartScenario(Read(root,"quick-start-scenario.json"))
                    .WithHeadlineScenario(Read(root,"headline-scenario.json"),Read(root,"turn-flow.json"),Read(root,"defectors.parameters.json"),Read(root,"defectors.schema.json"))
                    .WithFinalScoringScenario(Read(root,"final-scoring-scenario.json"),Read(root,"region-scoring.json"),Read(root,"region-scoring-rules.schema.json"),Read(root,"scoring-card-parameters.json"),Read(root,"scoring-card-parameters.schema.json"))
                    .WithSpaceDiscardScenarios(Read(root,"space-end-turn-discard-scenarios.json"))
                    .WithSpaceExtraScenarios(Read(root,"space-extra-action-scenarios.json"))
                    .WithEmptyHandScenario(Read(root,"empty-hand-scenario.json"))
                    .WithIndependentReds(Read(root,"early-influence-batch3.parameters.json"),Read(root,"early-influence-batch3.schema.json"),Read(root,"cards.json"))
                    .WithIndependentRedsScenario(Read(root,"independent-reds-scenario.json"))
                    .WithBlockade(Read(root,"hand-events-batch1.parameters.json"),Read(root,"hand-events-batch1.schema.json"),Read(root,"cards.json"))
                    .WithBlockadeScenario(Read(root,"blockade-scenario.json"))
                    .WithFiveYearPlan(Read(root,"hand-events-batch1.parameters.json"),Read(root,"hand-events-batch1.schema.json"),Read(root,"cards.json"))
                    .WithFiveYearPlanScenario(Read(root,"five-year-plan-scenario.json"));
                var displayNames=CatalogNamesLoader.Load(Read(root,"names.zh-CN.json"),"zh-CN",catalog.Names.Keys);
                var worldAssets=WorldMapAssets.Load(Read(root,"world-layout.json"),Read(root,"world-land.json"),catalog.Map);
#endif
                var mapKind=DemoLaunchOptions.MapKind(Environment.GetCommandLineArgs());
                view.Initialize(new DemoController(catalog.CreateHost),displayNames,
                    catalog.Scenarios.Select(s=>new DemoScenarioOption(s.Id,s.Title,s.Actor,s.SelectedCountry)).ToArray(),
                    DemoLaunchOptions.ScenarioIndex(Environment.GetCommandLineArgs(),catalog.Scenarios.Select(s=>s.Id).ToArray()),
                    mapKind,worldAssets,DemoLaunchOptions.FocusSelected(Environment.GetCommandLineArgs(),mapKind),catalog.PrintedCards,DemoLaunchOptions.ShowHands(Environment.GetCommandLineArgs()),catalog.Names);
            }
            catch(Exception e) { view.ShowError(e.Message);Debug.LogException(e); }
        }
        static string Read(string root,string file)=>File.ReadAllText(Path.Combine(root,file));
    }
}
