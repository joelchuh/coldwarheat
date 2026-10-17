using System;
using System.IO;
using ColdWarStruggle.Bootstrap;
using ColdWarStruggle.Infrastructure;
using ColdWarStruggle.Presentation;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace ColdWarStruggle.Editor
{
    public static class DemoBuild
    {
        const string ScenePath="Assets/ColdWarStruggle/Scenes/FirstDemo.unity";
        [MenuItem("ColdWarStruggle/Prepare First Demo")]
        public static void Prepare()
        {
            SynchronizeData();
            if(!File.Exists(ScenePath))
            {
                if(!UnityEngine.Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    throw new OperationCanceledException("Demo scene creation cancelled.");
                Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                var go=new GameObject("FirstDemo");go.AddComponent<DemoBootstrap>();
                var cam=new GameObject("Camera").AddComponent<Camera>();cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.035f,.057f,.095f);
                EditorSceneManager.SaveScene(scene,ScenePath);
            }
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};
            PlayerSettings.companyName="ColdWarStruggle";PlayerSettings.productName="ColdWarStruggle First Demo";
            PlayerSettings.defaultScreenWidth=1280;PlayerSettings.defaultScreenHeight=820;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;
            PlayerSettings.resizableWindow=true;PlayerSettings.runInBackground=false;
            AssetDatabase.Refresh();AssetDatabase.SaveAssets();
        }
        [MenuItem("ColdWarStruggle/Play First Demo")]
        public static void Play()
        { if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;Prepare();EditorSceneManager.OpenScene(ScenePath);EditorApplication.isPlaying=true; }
        [MenuItem("ColdWarStruggle/Build Windows Demo")]
        public static void Build()=>BuildAt("Builds/FirstDemo");
        [MenuItem("ColdWarStruggle/Build Side-by-side Preview")]
        public static void BuildPreview()=>BuildAt("Builds/PreviewDemo");
        [MenuItem("ColdWarStruggle/Build Playable Actions Demo")]
        public static void BuildPlayable()=>BuildAt("Builds/PlayableDemo");
        [MenuItem("ColdWarStruggle/Build Risk Actions Demo")]
        public static void BuildRisk()=>BuildAt("Builds/RiskDemo");
        [MenuItem("ColdWarStruggle/Build Scoring Actions Demo")]
        public static void BuildScoring()=>BuildAt("Builds/ScoringDemo");
        [MenuItem("ColdWarStruggle/Build Quick Playtest")]
        public static void BuildPlaytest()=>BuildAt("Builds/PlaytestDemo","ColdWarStruggle Playtest");
        [MenuItem("ColdWarStruggle/Build Event Playtest")]
        public static void BuildEventPlaytest()=>BuildAt("Builds/EventPlaytest","ColdWarStruggle Event Playtest");
        [MenuItem("ColdWarStruggle/Build DEFCON Event Playtest")]
        public static void BuildDefconEventPlaytest()=>BuildAt("Builds/DefconEventPlaytest","ColdWarStruggle DEFCON Event Playtest");
        [MenuItem("ColdWarStruggle/Build Realignment Playtest")]
        public static void BuildRealignmentPlaytest()=>BuildAt("Builds/RealignmentPlaytest","ColdWarStruggle Realignment Playtest");
        [MenuItem("ColdWarStruggle/Build OPS Event Playtest")]
        public static void BuildOpsEventPlaytest()=>BuildAt("Builds/OpsEventPlaytest","ColdWarStruggle OPS Event Playtest");
        [MenuItem("ColdWarStruggle/Build Opponent Event Playtest")]
        public static void BuildOpponentEventPlaytest()=>BuildAt("Builds/OpponentEventPlaytest","ColdWarStruggle Opponent Event Playtest");
        [MenuItem("ColdWarStruggle/Build Opponent OPS Playtest")]
        public static void BuildOpponentOpsPlaytest()=>BuildAt("Builds/OpponentOpsPlaytest","ColdWarStruggle Opponent OPS Playtest");
        [MenuItem("ColdWarStruggle/Build China Playtest")]
        public static void BuildChinaPlaytest()=>BuildAt("Builds/ChinaPlaytest","ColdWarStruggle China Playtest");
        [MenuItem("ColdWarStruggle/Build Turn End Playtest")]
        public static void BuildTurnEndPlaytest()=>BuildAt("Builds/TurnEndPlaytest","ColdWarStruggle Turn End Playtest");
        [MenuItem("ColdWarStruggle/Build Continuous Turn Playtest")]
        public static void BuildContinuousPlaytest()=>BuildAt("Builds/ContinuousPlaytest","ColdWarStruggle Continuous Playtest");
        [MenuItem("ColdWarStruggle/Build Localized Playtest")]
        public static void BuildLocalizedPlaytest()=>BuildAt("Builds/LocalizedPlaytest","ColdWarStruggle Localized Playtest");
        [MenuItem("ColdWarStruggle/Build Headlines Playtest")]
        public static void BuildHeadlinesPlaytest()=>BuildAt("Builds/HeadlinesPlaytest","ColdWarStruggle Headlines Playtest");
        [MenuItem("ColdWarStruggle/Build Final Scoring Playtest")]
        public static void BuildFinalScoringPlaytest()=>BuildAt("Builds/FinalScoringPlaytest","ColdWarStruggle Final Scoring Playtest");
        [MenuItem("ColdWarStruggle/Build Space Discard Playtest")]
        public static void BuildSpaceDiscardPlaytest()=>BuildAt("Builds/SpaceDiscardPlaytest","ColdWarStruggle Space Discard Playtest");
        [MenuItem("ColdWarStruggle/Build Space Extra Playtest")]
        public static void BuildSpaceExtraPlaytest()=>BuildAt("Builds/SpaceExtraPlaytest","ColdWarStruggle Space Extra Playtest");
        [MenuItem("ColdWarStruggle/Build Empty Hand Playtest")]
        public static void BuildEmptyHandPlaytest()=>BuildAt("Builds/EmptyHandPlaytest","ColdWarStruggle Empty Hand Playtest");
        [MenuItem("ColdWarStruggle/Build Independent Reds Playtest")]
        public static void BuildIndependentRedsPlaytest()=>BuildAt("Builds/IndependentRedsPlaytest","ColdWarStruggle Independent Reds Playtest");
        [MenuItem("ColdWarStruggle/Build Practice Browser Playtest")]
        public static void BuildPracticeBrowserPlaytest()=>BuildAt("Builds/PracticeBrowserPlaytest","ColdWarStruggle Practice Browser Playtest");
        [MenuItem("ColdWarStruggle/Build Blockade Playtest")]
        public static void BuildBlockadePlaytest()=>BuildAt("Builds/BlockadePlaytest","ColdWarStruggle Blockade Playtest");
        [MenuItem("ColdWarStruggle/Build Five Year Plan Playtest")]
        public static void BuildFiveYearPlanPlaytest()=>BuildAt("Builds/FiveYearPlanPlaytest","ColdWarStruggle Five Year Plan Playtest");
        [MenuItem("ColdWarStruggle/Build War Playtest")]
        public static void BuildWarPlaytest()=>BuildAt("Builds/WarPlaytest","ColdWarStruggle War Playtest");
        [MenuItem("ColdWarStruggle/Build Result Reader Playtest")]
        public static void BuildResultReaderPlaytest()=>BuildAt("Builds/ResultReaderPlaytest","ColdWarStruggle Result Reader Playtest");
        [MenuItem("ColdWarStruggle/Build Space Event Playtest")]
        public static void BuildSpaceEventPlaytest()=>BuildAt("Builds/SpaceEventPlaytest","ColdWarStruggle Space Event Playtest");
        [MenuItem("ColdWarStruggle/Build Olympic Playtest")]
        public static void BuildOlympicPlaytest()=>BuildAt("Builds/OlympicPlaytest","ColdWarStruggle Olympic Playtest");
        [MenuItem("ColdWarStruggle/Build Payload Manifest Playtest")]
        public static void BuildPayloadManifestPlaytest()=>BuildAt("Builds/PayloadManifestPlaytest","ColdWarStruggle Payload Manifest Playtest");
        static void BuildAt(string directory,string productName=null)
        {
            Prepare();Directory.CreateDirectory(directory);
            var previousProduct=PlayerSettings.productName;
            try
            {
                if(productName!=null)PlayerSettings.productName=productName;
                var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName=Path.Combine(directory,"ColdWarStruggleDemo.exe"),target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
                if(report.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("Demo build failed: "+report.summary.result);
                Debug.Log("FIRST_DEMO_BUILD_SUCCEEDED "+report.summary.totalSize);
            }
            finally { PlayerSettings.productName=previousProduct;AssetDatabase.SaveAssets(); }
        }
        public static void SynchronizeData()
        {
            string root=Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath,".."));
            // Validate canonical inputs before refreshing any generated runtime copies.
            var printedCards=CardCatalogLoader.LoadPrintedSubset(Read(root,"Data/Cards/deluxe-2015/cards.json"),Read(root,"Docs/Contracts/cards-v2.schema.json"),Read(root,"Data/Cards/deluxe-2015/cards.printed-profile.json"));
            var catalog=DemoCatalog.Load(Read(root,"Data/Maps/deluxe-2015/world.json"),Read(root,"Docs/Contracts/map.schema.json"),
                Read(root,"Data/Demo/influence-scenarios.json"),Read(root,"Data/Rulesets/deluxe-2015/influence-placement.json"),
                Read(root,"Data/Localization/en-US.catalog-names.json"),Read(root,"Data/Maps/deluxe-2015/world.profile.json"))
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
                    .WithFiveYearPlanScenario(Read(root,"Data/Demo/five-year-plan-scenario.json"))
                    .WithEarlyWars(Read(root,"Data/Cards/deluxe-2015/war-events-batch1.parameters.json"),Read(root,"Docs/Contracts/war-events-batch1.schema.json"),Read(root,"Data/Cards/deluxe-2015/cards.json"))
                    .WithEarlyWarScenarios(Read(root,"Data/Demo/early-war-scenarios.json"))
                    .WithSpaceEvents(Read(root,"Data/Cards/deluxe-2015/space-events-batch1.parameters.json"),Read(root,"Docs/Contracts/space-events-batch1.schema.json"),Read(root,"Data/Cards/deluxe-2015/cards.json"))
                    .WithSpaceEventScenarios(Read(root,"Data/Demo/space-event-scenarios.json"))
                    .WithOlympicEvents(Read(root,"Data/Cards/deluxe-2015/olympic-games.parameters.json"),Read(root,"Docs/Contracts/olympic-games.schema.json"),Read(root,"Data/Rulesets/deluxe-2015/standard-dice.json"),Read(root,"Docs/Contracts/standard-dice.schema.json"),Read(root,"Data/Cards/deluxe-2015/cards.json"))
                    .WithOlympicScenarios(Read(root,"Data/Demo/olympic-scenarios.json"));
            CatalogNamesLoader.Load(Read(root,"Data/Localization/zh-CN.catalog-names.json"),"zh-CN",catalog.Names.Keys);
            CatalogNamesLoader.Load(Read(root,"Data/Localization/en-US.catalog-names.json"),"en-US",catalog.Names.Keys);
            WorldMapAssets.Load(Read(root,"Data/Presentation/world-layout.json"),Read(root,"Data/Presentation/world-land.json"),catalog.Map);
            PlayerPayloadPublisher.Synchronize(root);
            AssetDatabase.Refresh();
        }
        static string Read(string root,string relative)=>File.ReadAllText(Path.Combine(root,relative));

    }
    public sealed class DemoBuildPreprocessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;
        public void OnPreprocessBuild(BuildReport report) => DemoBuild.SynchronizeData();
    }
}
