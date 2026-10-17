using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
namespace ColdWarStruggle.Infrastructure
{
    // Build-time generated copies only. No rule decisions, deletion, process execution or Git access.
    public static class PlayerPayloadPublisher
    {
        const long MaximumPayloadBytes=64L*1024*1024,MaximumTotalBytes=256L*1024*1024;
        static void NoLinks(string path,bool allowMissing=false)
        {
            for(var current=Path.GetFullPath(path);current!=null;current=Path.GetDirectoryName(current))
            {
                try {if((File.GetAttributes(current)&FileAttributes.ReparsePoint)!=0)throw new IOException("Linked path rejected: "+current);}
                catch(FileNotFoundException){if(!allowMissing)throw;}
                catch(DirectoryNotFoundException){if(!allowMissing)throw;}
            }
        }
        static byte[] ReadStable(string path,long maximum)
        {
            NoLinks(path);var stamp=File.GetLastWriteTimeUtc(path);
            using(var stream=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read))
            {
                if(stream.Length>maximum)throw new IOException("Payload size limit: "+path);
                var data=new byte[(int)stream.Length];int done=0;
                while(done<data.Length){int count=stream.Read(data,done,data.Length-done);if(count==0)throw new IOException("Input changed while reading.");done+=count;}
                if(stream.ReadByte()!=-1 || stream.Length!=data.Length || File.GetLastWriteTimeUtc(path)!=stamp)throw new IOException("Input changed while reading.");
                return data;
            }
        }
        public static void Synchronize(string projectRoot)
        {
            if(string.IsNullOrWhiteSpace(projectRoot) || !Path.IsPathFullyQualified(projectRoot) || projectRoot.StartsWith(@"\\",StringComparison.Ordinal) || projectRoot.StartsWith("//",StringComparison.Ordinal))throw new ArgumentException("Local absolute project root required.");
            var root=Path.GetFullPath(projectRoot).TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar);
            if(root.Length==0 || root.Length<=Path.GetPathRoot(projectRoot).TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar).Length)throw new ArgumentException("Drive root rejected.");
            NoLinks(root);if(!Directory.Exists(root))throw new ArgumentException("Project directory required.");
            var json=ReadStable(Path.Combine(root,"Tools/Configuration/player-payloads.json"),PlayerPayloadManifest.MaximumJsonBytes);
            var manifest=PlayerPayloadManifest.Load(new UTF8Encoding(false,true).GetString(json).TrimStart('\uFEFF'));
            var output=Path.Combine(root,"Assets/StreamingAssets/ColdWarStruggle");NoLinks(output,true);
            var names=new HashSet<string>(manifest.Entries.Select(x=>x.Destination),StringComparer.OrdinalIgnoreCase);
            if(Directory.Exists(output))foreach(var path in Directory.EnumerateFileSystemEntries(output))
            {
                NoLinks(path);
                if(Directory.Exists(path) || (!path.EndsWith(".meta",StringComparison.OrdinalIgnoreCase) && !names.Contains(Path.GetFileName(path))))throw new IOException("Unexpected generated payload: "+path);
            }
            var pending=new List<(string Target,byte[] Bytes,bool Changed)>();long total=0;
            foreach(var entry in manifest.Entries)
            {
                var source=Path.Combine(root,entry.Source);var target=Path.Combine(output,entry.Destination);NoLinks(target,true);
                var info=new FileInfo(source);NoLinks(source);if(info.Length>MaximumPayloadBytes || info.Length>MaximumTotalBytes-total)throw new IOException("Payload byte limit.");
                var bytes=ReadStable(source,MaximumPayloadBytes);total=checked(total+bytes.Length);if(total>MaximumTotalBytes)throw new IOException("Total payload byte limit.");
                var changed=!File.Exists(target) || !bytes.SequenceEqual(ReadStable(target,MaximumPayloadBytes));pending.Add((target,bytes,changed));
            }
            // All reads and validation succeed before any generated file is touched. IO writes are not a filesystem transaction.
            Directory.CreateDirectory(output);NoLinks(output);
            foreach(var item in pending)if(item.Changed){NoLinks(item.Target,true);File.WriteAllBytes(item.Target,item.Bytes);}
        }
    }
}
