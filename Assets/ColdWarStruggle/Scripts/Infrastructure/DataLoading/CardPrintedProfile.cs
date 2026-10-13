using System;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
namespace ColdWarStruggle.Infrastructure
{
    internal static class CardPrintedProfile
    {
        public static void Validate(JObject cards,string profileJson)
        {
            var p=ContractJson.Parse(profileJson) as JObject;
            var keys=new[]{"schema_version","dataset_id","description","cards_dataset_id","cards_content_version","canonicalization","sha256","reviewed_source_commit"};
            if(p==null || p.Properties().Count()!=keys.Length || keys.Any(k=>p[k]==null)
                || !JToken.DeepEquals(p["schema_version"],new JValue(1))
                || keys.Where(k=>k!="schema_version").Any(k=>p[k].Type!=JTokenType.String || string.IsNullOrWhiteSpace((string)p[k]))
                || (string)p["dataset_id"]!="cards.deluxe2015.printed_profile" || (string)p["cards_dataset_id"]!=(string)cards["dataset_id"]
                || (string)p["cards_content_version"]!=(string)cards["content_version"] || (string)p["canonicalization"]!="sorted-object-keys-json-utf8-v1"
                || !Regex.IsMatch((string)p["reviewed_source_commit"],"^[0-9a-f]{40}$",RegexOptions.CultureInvariant)
                || !string.Equals((string)p["sha256"],CanonicalJsonDigest.Compute(cards),StringComparison.Ordinal))
                throw new ArgumentException("Card source differs from the trusted printed profile.");
        }
    }
}
