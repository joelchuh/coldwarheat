using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace ColdWarStruggle.Infrastructure
{
    public sealed class PlayerPayloadEntry
    {
        public string Source { get; }
        public string Destination { get; }
        internal PlayerPayloadEntry(string source,string destination){Source=source;Destination=destination;}
    }
    public sealed class PlayerPayloadManifest
    {
        public const int MaximumJsonBytes=1048576;
        public IReadOnlyList<PlayerPayloadEntry> Entries { get; }
        PlayerPayloadManifest(IEnumerable<PlayerPayloadEntry> entries){Entries=Array.AsReadOnly(entries.ToArray());}
        static readonly Regex Segment=new Regex(@"\A[A-Za-z0-9][A-Za-z0-9._-]{0,127}\z",RegexOptions.CultureInvariant,TimeSpan.FromSeconds(1));
        static readonly HashSet<string> Devices=new HashSet<string>(new[]{"CON","PRN","AUX","NUL"},StringComparer.OrdinalIgnoreCase);
        // Json.NET accepts JavaScript extensions. Restrict the wire syntax before
        // its structural parser so the build and Python verifier share JSON input.
        static readonly Regex JsonLexeme=new Regex(
            "\\G(?:[ \\t\\r\\n]+|[{}\\[\\]:,]|\"(?:[^\"\\\\\\x00-\\x1F]|\\\\(?:[\"\\\\/bfnrt]|u[0-9A-Fa-f]{4}))*\"|(?:-?(?:0|[1-9][0-9]*)(?:\\.[0-9]+)?(?:[eE][+-]?[0-9]+)?|true|false|null)(?=[ \\t\\r\\n,}\\]:]|$))",
            RegexOptions.CultureInvariant,TimeSpan.FromSeconds(1));
        static void RequireJsonSyntax(string json)
        {
            string previous=null;
            for(var position=0;position<json.Length;)
            {
                var token=JsonLexeme.Match(json,position);
                if(!token.Success)throw new ArgumentException("Nonstandard JSON syntax rejected.");
                var value=token.Value;position+=token.Length;
                if(value[0]==' ' || value[0]=='\t' || value[0]=='\r' || value[0]=='\n')continue;
                if(previous=="," && (value=="}" || value=="]"))throw new ArgumentException("Trailing JSON comma rejected.");
                previous=value;
            }
        }
        static string Text(JToken value)
        {if(value?.Type!=JTokenType.String || string.IsNullOrWhiteSpace((string)value))throw new ArgumentException("Nonempty text required.");return (string)value;}
        static int CodePointCount(string value)
        {
            var count=0;
            for(var i=0;i<value.Length;i++,count++)
                if(char.IsHighSurrogate(value[i]) && i+1<value.Length && char.IsLowSurrogate(value[i+1]))i++;
            return count;
        }
        static void Fields(JObject value,params string[] names)
        {if(value==null || !new HashSet<string>(value.Properties().Select(x=>x.Name),StringComparer.Ordinal).SetEquals(names))throw new ArgumentException("Unexpected manifest fields.");}
        internal static void SafePath(string path,bool source)
        {
            var segments=path.Split('/');
            if(!source && segments.Length!=1)throw new ArgumentException("Destination must be a flat filename.");
            if(!source && path.EndsWith(".meta",StringComparison.OrdinalIgnoreCase))throw new ArgumentException("Unity metadata filenames are reserved.");
            foreach(var part in segments)
            {
                if(!Segment.IsMatch(part) || part.EndsWith(".",StringComparison.Ordinal))throw new ArgumentException("Unsafe manifest path.");
                var stem=part.Split('.')[0].ToUpperInvariant();
                if(Devices.Contains(stem) || (stem.Length==4 && (stem.StartsWith("COM",StringComparison.Ordinal) || stem.StartsWith("LPT",StringComparison.Ordinal)) && stem[3]>='1' && stem[3]<='9'))throw new ArgumentException("Device path rejected.");
            }
            if(source && !(path.StartsWith("Data/",StringComparison.Ordinal) || path.StartsWith("Docs/Contracts/",StringComparison.Ordinal) || path.StartsWith("Docs/References/ThirdParty/",StringComparison.Ordinal)))throw new ArgumentException("Source outside approved data directories.");
        }
        public static PlayerPayloadManifest Load(string json)
        {
            if(json==null || Encoding.UTF8.GetByteCount(json)>MaximumJsonBytes)throw new ArgumentException("Manifest size limit.");
            RequireJsonSyntax(json);
            try
            {
                using(var reader=new JsonTextReader(new StringReader(json)){MaxDepth=16,DateParseHandling=DateParseHandling.None})
                    while(reader.Read())if(reader.TokenType==JsonToken.Comment)throw new ArgumentException("JSON comments rejected.");
            }
            catch(JsonException e){throw new ArgumentException("Invalid manifest JSON.",e);}
            var root=ContractJson.Parse(json) as JObject;Fields(root,"schema_version","dataset_id","description","entries");
            if(root["schema_version"].Type!=JTokenType.Integer || root["schema_version"].ToString()!="1" || Text(root["dataset_id"])!="workflow.player_payloads" || CodePointCount(Text(root["description"]))>512)throw new ArgumentException("Invalid manifest identity.");
            var rows=root["entries"] as JArray;if(rows==null || rows.Count<1 || rows.Count>256)throw new ArgumentException("Manifest entry count.");
            var sources=new HashSet<string>(StringComparer.OrdinalIgnoreCase);var targets=new HashSet<string>(StringComparer.OrdinalIgnoreCase);var entries=new List<PlayerPayloadEntry>();
            foreach(var value in rows)
            {
                var row=value as JObject;Fields(row,"source","destination");var source=Text(row["source"]);var target=Text(row["destination"]);
                SafePath(source,true);SafePath(target,false);
                if(!sources.Add(source) || !targets.Add(target))throw new ArgumentException("Duplicate manifest source or destination.");
                entries.Add(new PlayerPayloadEntry(source,target));
            }
            return new PlayerPayloadManifest(entries);
        }
    }
}
