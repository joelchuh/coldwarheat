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
            string payload=Path.Combine(UnityEngine.Application.streamingAssetsPath,"ColdWarStruggle");
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
                    .WithQuickStartScenario(Read(root,"Data/Demo/quick-start-scenario.json"));
            WorldMapAssets.Load(Read(root,"Data/Presentation/world-layout.json"),Read(root,"Data/Presentation/world-land.json"),catalog.Map);
            Directory.CreateDirectory(payload);
            Copy(root,"Data/Cards/deluxe-2015/defcon-vp-events.parameters.json",payload,"defcon-vp-events.parameters.json");Copy(root,"Docs/Contracts/defcon-vp-events.schema.json",payload,"defcon-vp-events.schema.json");
            Copy(root,"Data/Maps/deluxe-2015/world.json",payload,"world.json");Copy(root,"Docs/Contracts/map.schema.json",payload,"map.schema.json");
            Copy(root,"Data/Maps/deluxe-2015/world.profile.json",payload,"world.profile.json");
            Copy(root,"Data/Demo/influence-scenarios.json",payload,"influence-scenarios.json");Copy(root,"Data/Rulesets/deluxe-2015/influence-placement.json",payload,"influence-placement.json");
            Copy(root,"Data/Localization/en-US.catalog-names.json",payload,"names.json");
            Copy(root,"Data/Demo/event-scenarios.json",payload,"event-scenarios.json");
            Copy(root,"Data/Cards/deluxe-2015/early-influence-batch1.parameters.json",payload,"early-influence-batch1.parameters.json");
            Copy(root,"Docs/Contracts/early-influence-parameters.schema.json",payload,"early-influence-parameters.schema.json");
            Copy(root,"Data/Demo/allocation-scenarios.json",payload,"allocation-scenarios.json");
            Copy(root,"Data/Cards/deluxe-2015/early-influence-batch2.parameters.json",payload,"early-influence-batch2.parameters.json");
            Copy(root,"Docs/Contracts/early-influence-batch2.schema.json",payload,"early-influence-batch2.schema.json");
            Copy(root,"Data/Cards/deluxe-2015/cards.json",payload,"cards.json");
            Copy(root,"Data/Demo/era-scenarios.json",payload,"era-scenarios.json");
            Copy(root,"Data/Rulesets/deluxe-2015/turn-flow.json",payload,"turn-flow.json");
            Copy(root,"Data/Presentation/world-layout.json",payload,"world-layout.json");
            Copy(root,"Data/Presentation/world-land.json",payload,"world-land.json");
            Copy(root,"Data/Demo/deployment-scenario.json",payload,"deployment-scenario.json");
            Copy(root,"Data/Rulesets/deluxe-2015/setup.json",payload,"setup.json");
            Copy(root,"Docs/Contracts/setup.schema.json",payload,"setup.schema.json");
            Copy(root,"Data/Demo/initial-deal-scenario.json",payload,"initial-deal-scenario.json");
            Copy(root,"Data/Demo/quick-start-scenario.json",payload,"quick-start-scenario.json");
            Copy(root,"Docs/Contracts/cards-v2.schema.json",payload,"cards-v2.schema.json");
            Copy(root,"Data/Cards/deluxe-2015/cards.printed-profile.json",payload,"cards.printed-profile.json");
            Copy(root,"Data/Cards/deluxe-2015/persistent-effects-batch1.parameters.json",payload,"persistent-effects-batch1.parameters.json");
            Copy(root,"Docs/Contracts/persistent-effects-batch1.schema.json",payload,"persistent-effects-batch1.schema.json");
            Copy(root,"Data/Rulesets/deluxe-2015/space-race.json",payload,"space-race.json");
            Copy(root,"Data/Rulesets/deluxe-2015/coup.json",payload,"coup.json");
            Copy(root,"Data/Rulesets/deluxe-2015/realignment.json",payload,"realignment.json");
            Copy(root,"Data/Rulesets/deluxe-2015/defcon.json",payload,"defcon.json");
            Copy(root,"Data/Rulesets/deluxe-2015/military-operations.json",payload,"military-operations.json");
            Copy(root,"Data/Rulesets/deluxe-2015/victory.json",payload,"victory.json");
            Copy(root,"Data/Rulesets/deluxe-2015/region-scoring.json",payload,"region-scoring.json");
            Copy(root,"Docs/Contracts/region-scoring-rules.schema.json",payload,"region-scoring-rules.schema.json");
            Copy(root,"Docs/References/ThirdParty/Apache-2.0.txt",payload,"Apache-2.0.txt");
            Copy(root,"Docs/References/ThirdParty/pcg32.md",payload,"pcg32-notice.md");
            AssetDatabase.Refresh();
        }
        static string Read(string root,string relative)=>File.ReadAllText(Path.Combine(root,relative));
        static void Copy(string root,string relative,string output,string name)
        {
            var source=File.ReadAllBytes(Path.Combine(root,relative));var target=Path.Combine(output,name);
            if(!File.Exists(target) || !System.Linq.Enumerable.SequenceEqual(source,File.ReadAllBytes(target)))File.WriteAllBytes(target,source);
        }
    }
    public sealed class DemoBuildPreprocessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;
        public void OnPreprocessBuild(BuildReport report) => DemoBuild.SynchronizeData();
    }
}
