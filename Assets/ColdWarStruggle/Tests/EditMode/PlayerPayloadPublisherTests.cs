using System;
using System.IO;
using ColdWarStruggle.Infrastructure;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public sealed class PlayerPayloadPublisherTests
    {
        static string Fixture()
        {
            var root=Path.Combine(Path.GetTempPath(),"coldwarheat-payload-"+Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(root,"Tools/Configuration"));Directory.CreateDirectory(Path.Combine(root,"Data"));
            File.WriteAllText(Path.Combine(root,"Tools/Configuration/player-payloads.json"),PlayerPayloadManifestTests.Sample().ToString());
            File.WriteAllBytes(Path.Combine(root,"Data/value.json"),new byte[]{0,1,2,255});return root;
        }
        static string Target(string root)=>Path.Combine(root,"Assets/StreamingAssets/ColdWarStruggle/value.json");
        [Test] public void Generates_exact_bytes_and_unchanged_second_run_preserves_file_timestamp()
        {
            var root=Fixture();PlayerPayloadPublisher.Synchronize(root);Assert.That(File.ReadAllBytes(Target(root)),Is.EqualTo(new byte[]{0,1,2,255}));
            var stamp=new DateTime(2020,1,1,0,0,0,DateTimeKind.Utc);File.SetLastWriteTimeUtc(Target(root),stamp);
            PlayerPayloadPublisher.Synchronize(root);Assert.That(File.GetLastWriteTimeUtc(Target(root)),Is.EqualTo(stamp));
        }
        [Test] public void Missing_later_source_does_not_partially_overwrite_existing_payload()
        {
            var root=Fixture();Directory.CreateDirectory(Path.GetDirectoryName(Target(root)));File.WriteAllText(Target(root),"previous");
            var j=PlayerPayloadManifestTests.Sample();((JArray)j["entries"]).Add(new JObject{["source"]="Data/missing.json",["destination"]="later.json"});
            File.WriteAllText(Path.Combine(root,"Tools/Configuration/player-payloads.json"),j.ToString());
            Assert.Catch<IOException>(()=>PlayerPayloadPublisher.Synchronize(root));Assert.That(File.ReadAllText(Target(root)),Is.EqualTo("previous"));
        }
        [Test] public void Extra_generated_payload_is_rejected_without_deletion_or_overwrite()
        {
            var root=Fixture();Directory.CreateDirectory(Path.GetDirectoryName(Target(root)));var extra=Path.Combine(Path.GetDirectoryName(Target(root)),"old.json");File.WriteAllText(extra,"keep");
            Assert.Throws<IOException>(()=>PlayerPayloadPublisher.Synchronize(root));Assert.That(File.ReadAllText(extra),Is.EqualTo("keep"));Assert.That(File.Exists(Target(root)),Is.False);
        }
        [Test] public void Unity_meta_is_retained_and_not_treated_as_runtime_payload()
        {
            var root=Fixture();Directory.CreateDirectory(Path.GetDirectoryName(Target(root)));File.WriteAllText(Target(root)+".meta","unity");
            PlayerPayloadPublisher.Synchronize(root);Assert.That(File.ReadAllText(Target(root)+".meta"),Is.EqualTo("unity"));
        }
        [Test] public void Oversized_source_is_rejected_before_generation()
        {
            var root=Fixture();using(var f=File.OpenWrite(Path.Combine(root,"Data/value.json")))f.SetLength(64L*1024*1024+1);
            Assert.Throws<IOException>(()=>PlayerPayloadPublisher.Synchronize(root));Assert.That(File.Exists(Target(root)),Is.False);
        }
        [TestCase("root")][TestCase("source")][TestCase("target")]
        public void Existing_windows_junction_in_read_or_write_path_is_rejected(string where)
        {
            if(Environment.OSVersion.Platform!=PlatformID.Win32NT)Assert.Ignore("Real Windows junction verification.");
            var root=Fixture();var outside=root+"-outside";Directory.CreateDirectory(outside);File.WriteAllText(Path.Combine(outside,"value.json"),"untouched");
            string link,target;
            if(where=="root"){link=root+"-link";target=root;}
            else if(where=="source")
            {
                link=Path.Combine(root,"Data/linked");target=outside;var j=PlayerPayloadManifestTests.Sample();j["entries"][0]["source"]="Data/linked/value.json";
                File.WriteAllText(Path.Combine(root,"Tools/Configuration/player-payloads.json"),j.ToString());
            }
            else {Directory.CreateDirectory(Path.Combine(root,"Assets/StreamingAssets"));link=Path.GetDirectoryName(Target(root));target=outside;}
            var start=new System.Diagnostics.ProcessStartInfo("cmd.exe","/d /c mklink /J \""+link+"\" \""+target+"\"")
                {UseShellExecute=false,CreateNoWindow=true,WindowStyle=System.Diagnostics.ProcessWindowStyle.Hidden,RedirectStandardOutput=true,RedirectStandardError=true};
            using(var process=System.Diagnostics.Process.Start(start))
            {Assert.That(process.WaitForExit(10000),Is.True);Assert.That(process.ExitCode,Is.Zero,process.StandardError.ReadToEnd());}
            Assert.Throws<IOException>(()=>PlayerPayloadPublisher.Synchronize(where=="root"?link:root));
            Assert.That(File.ReadAllText(Path.Combine(outside,"value.json")),Is.EqualTo("untouched"));
        }
        [Test] public void Relative_or_drive_root_cannot_be_used_as_project_root()
        {
            Assert.Throws<ArgumentException>(()=>PlayerPayloadPublisher.Synchronize("relative"));
            Assert.Throws<ArgumentException>(()=>PlayerPayloadPublisher.Synchronize(Path.GetPathRoot(Path.GetTempPath())));
        }
    }
}
