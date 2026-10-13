using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace ColdWarStruggle.Infrastructure
{
    internal static class CanonicalJsonDigest
    {
        public static string Compute(JToken token)
        {
            using var hash=SHA256.Create();
            return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(Canonical(token).ToString(Formatting.None)))).Replace("-","").ToLowerInvariant();
        }
        static JToken Canonical(JToken token)=>token is JObject obj
            ?new JObject(obj.Properties().OrderBy(p=>p.Name,StringComparer.Ordinal).Select(p=>new JProperty(p.Name,Canonical(p.Value))))
            :token is JArray array?new JArray(array.Select(Canonical)):token.DeepClone();
    }
}
