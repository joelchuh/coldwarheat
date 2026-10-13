using System;
using System.IO;
using ColdWarStruggle.Bootstrap;
using ColdWarStruggle.Infrastructure;
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
        public static void Build()
        {
            Prepare();Directory.CreateDirectory("Builds/FirstDemo");
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName="Builds/FirstDemo/ColdWarStruggleDemo.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
            if(report.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("Demo build failed: "+report.summary.result);
            Debug.Log("FIRST_DEMO_BUILD_SUCCEEDED "+report.summary.totalSize);
        }
        public static void SynchronizeData()
        {
            string root=Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath,".."));
            string payload=Path.Combine(UnityEngine.Application.streamingAssetsPath,"ColdWarStruggle");
            // Validate canonical inputs before refreshing any generated runtime copies.
            DemoCatalog.Load(Read(root,"Data/Maps/deluxe-2015/world.json"),Read(root,"Docs/Contracts/map.schema.json"),
                Read(root,"Data/Demo/influence-scenarios.json"),Read(root,"Data/Rulesets/deluxe-2015/influence-placement.json"),
                Read(root,"Data/Localization/en-US.catalog-names.json"),Read(root,"Data/Maps/deluxe-2015/world.profile.json"));
            Directory.CreateDirectory(payload);
            Copy(root,"Data/Maps/deluxe-2015/world.json",payload,"world.json");Copy(root,"Docs/Contracts/map.schema.json",payload,"map.schema.json");
            Copy(root,"Data/Maps/deluxe-2015/world.profile.json",payload,"world.profile.json");
            Copy(root,"Data/Demo/influence-scenarios.json",payload,"influence-scenarios.json");Copy(root,"Data/Rulesets/deluxe-2015/influence-placement.json",payload,"influence-placement.json");
            Copy(root,"Data/Localization/en-US.catalog-names.json",payload,"names.json");
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
