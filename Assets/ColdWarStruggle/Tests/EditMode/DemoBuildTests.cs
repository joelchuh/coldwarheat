using System.Linq;
using System.IO;
using ColdWarStruggle.Editor;
using NUnit.Framework;
using UnityEditor.Build;

namespace ColdWarStruggle.Tests
{
    public class DemoBuildTests
    {
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
