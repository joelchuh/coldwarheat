using System;
using System.Globalization;
using System.Linq;
using ColdWarStruggle.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace ColdWarStruggle.Infrastructure
{
    public static class RandomSnapshotJson
    {
        static readonly string[] Fields={"schema_version","algorithm_id","state","increment","draw_count"};
        public static string Serialize(RandomSnapshot snapshot)
        {
            if(snapshot==null)throw new ArgumentNullException(nameof(snapshot));
            return new JObject
            {
                ["schema_version"]=1,
                ["algorithm_id"]=snapshot.AlgorithmId,
                ["state"]=snapshot.State.ToString("x16",CultureInfo.InvariantCulture),
                ["increment"]=snapshot.Increment.ToString("x16",CultureInfo.InvariantCulture),
                ["draw_count"]=snapshot.DrawCount.ToString("x16",CultureInfo.InvariantCulture)
            }.ToString(Formatting.None);
        }
        public static RandomSnapshot Deserialize(string json)
        {
            if(ContractJson.Parse(json) is not JObject value || value.Count!=Fields.Length ||
                value.Properties().Any(p=>!Fields.Contains(p.Name)))throw new ArgumentException("Invalid random snapshot fields.");
            if(value["schema_version"].Type!=JTokenType.Integer || !JToken.DeepEquals(value["schema_version"],new JValue(1)))
                throw new ArgumentException("Unsupported random snapshot version.");
            if(value["algorithm_id"].Type!=JTokenType.String)throw new ArgumentException("Missing random algorithm.");
            return new RandomSnapshot((string)value["algorithm_id"],Hex(value["state"]),Hex(value["increment"]),Hex(value["draw_count"]));
        }
        static ulong Hex(JToken token)
        {
            if(token.Type!=JTokenType.String)throw new ArgumentException("Random state requires a hexadecimal string.");
            var text=(string)token;
            if(text.Length!=16 || text.Any(c=>!(c>='0' && c<='9') && !(c>='a' && c<='f')))
                throw new ArgumentException("Random state requires 16 lowercase hexadecimal digits.");
            return ulong.Parse(text,NumberStyles.AllowHexSpecifier,CultureInfo.InvariantCulture);
        }
    }
}
