using System;
using System.Globalization;
using System.Linq;
using ColdWarStruggle.Core;
using Newtonsoft.Json.Linq;
namespace ColdWarStruggle.Infrastructure
{
    public static class MilitaryOperationsLoader
    {
        // Base accounting parameters only; card rewards still require a verified caller.
        public static MilitaryOperationsDefinition LoadSubset(string json)
        {
            var root=Fields(ContractJson.Parse(json),"schema_version","id","status","scope","source","initial_earned","reset_earned",
                "reset_boundary","requirement_source","penalty_vp_per_shortfall","credit_sources","storage_policy");
            if(Integer(root["schema_version"])!=1 || Text(root["id"])!="rules.deluxe2015.military-operations"
                || Text(root["status"])!="verified-subset" || Text(root["reset_boundary"])!="MilitaryAssessmentCommitted"
                || Text(root["requirement_source"])!="CurrentDefconAtAssessment"
                || Text(root["storage_policy"])!="StoreNonnegativeEarnedTotalWithoutInventedCap")
                throw new ArgumentException("Unsupported military operations profile.");
            Text(root["scope"]);
            var source=Fields(root["source"],"id","url","checked_on","sections");
            if(Text(source["id"])!="R2015" || Text(source["url"])!="https://www.gmtgames.com/nnts/TS_Rules-2015.pdf"
                || !DateTime.TryParseExact(Text(source["checked_on"]),"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out _))
                throw new ArgumentException("Unsupported military source.");
            var sections=new[]{"4.5E","7.4.2","7.5","8.2"};
            if(source["sections"] is not JArray refs || refs.Count!=sections.Length
                || !refs.Select(Text).OrderBy(x=>x,StringComparer.Ordinal).SequenceEqual(sections.OrderBy(x=>x,StringComparer.Ordinal)))
                throw new ArgumentException("Unsupported military source sections.");
            var kinds=(MilitaryCreditKind[])Enum.GetValues(typeof(MilitaryCreditKind));
            var inputs=Fields(root["credit_sources"],kinds.Select(k=>k.ToString()).ToArray());
            var sources=kinds.ToDictionary(kind=>kind,kind=>Text(inputs[kind.ToString()]) switch
            {
                "EffectiveMilitaryOperationsCredit"=>MilitaryCreditBasis.EffectiveMilitaryOperationsCredit,
                "VerifiedCardParameter"=>MilitaryCreditBasis.VerifiedCardParameter,
                "None"=>MilitaryCreditBasis.None,
                _=>throw new ArgumentException("Unknown military credit basis.")
            });
            return new MilitaryOperationsDefinition(Integer(root["initial_earned"]),Integer(root["reset_earned"]),Integer(root["penalty_vp_per_shortfall"]),sources);
        }
        static JObject Fields(JToken token,params string[] expected)
        {
            if(token is not JObject obj || obj.Count!=expected.Length || obj.Properties().Any(p=>!expected.Contains(p.Name)))
                throw new ArgumentException("Unexpected military operations fields.");
            return obj;
        }
        static string Text(JToken token)
        {
            if(token?.Type!=JTokenType.String || string.IsNullOrWhiteSpace((string)token))throw new ArgumentException("Nonempty text required.");
            return (string)token;
        }
        static int Integer(JToken token)
        {
            if(token?.Type!=JTokenType.Integer || !int.TryParse(token.ToString(),NumberStyles.Integer,CultureInfo.InvariantCulture,out var number))
                throw new ArgumentException("Military integer out of range or wrong type.");
            return number;
        }
    }
}
