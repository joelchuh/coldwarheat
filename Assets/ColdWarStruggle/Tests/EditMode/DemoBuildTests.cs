using System.Linq;
using System.IO;
using ColdWarStruggle.Editor;
using NUnit.Framework;
using UnityEditor.Build;

namespace ColdWarStruggle.Tests
{
    public class DemoBuildTests
    {
        [TestCase("Data/Demo/headline-scenario.json","headline-scenario.json")]
        [TestCase("Data/Cards/deluxe-2015/defectors.parameters.json","defectors.parameters.json")]
        [TestCase("Docs/Contracts/defectors.schema.json","defectors.schema.json")]
        [TestCase("Data/Demo/independent-reds-scenario.json","independent-reds-scenario.json")]
        [TestCase("Data/Cards/deluxe-2015/early-influence-batch3.parameters.json","early-influence-batch3.parameters.json")]
        [TestCase("Docs/Contracts/early-influence-batch3.schema.json","early-influence-batch3.schema.json")]
        public void Playable_payload_matches_canonical_source(string source,string payload)
        {
            DemoBuild.SynchronizeData();var root=Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath,".."));
            Assert.That(File.ReadAllBytes(Path.Combine(UnityEngine.Application.streamingAssetsPath,"ColdWarStruggle",payload)),Is.EqualTo(File.ReadAllBytes(Path.Combine(root,source))));
        }
        [Test] public void Standard_build_has_a_data_preprocessor()
        {
            Assert.That(typeof(DemoBuild).Assembly.GetTypes().Any(t =>
                !t.IsAbstract && typeof(IPreprocessBuildWithReport).IsAssignableFrom(t)), Is.True,
                "Standard Build must validate and refresh the canonical demo data too.");
        }
        [Test] public void Build_callback_replaces_stale_data_and_restores_missing_data()
        {
            var path=Path.Combine(UnityEngine.Application.streamingAssetsPath,"ColdWarStruggle/world.json");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var existed=File.Exists(path);var original=existed?File.ReadAllBytes(path):null;
            try
            {
                File.WriteAllText(path,"stale generated data");
                new DemoBuildPreprocessor().OnPreprocessBuild(null);
                Assert.That(File.ReadAllText(path),Is.EqualTo(DefinitionTests.MapJson));
                File.Delete(path); // Only this generated payload; never the authoritative Data source.
                new DemoBuildPreprocessor().OnPreprocessBuild(null);
                Assert.That(File.ReadAllText(path),Is.EqualTo(DefinitionTests.MapJson));
            }
            finally { if(existed)File.WriteAllBytes(path,original); }
        }
    }
}
