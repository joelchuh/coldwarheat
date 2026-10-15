using System;
using System.Globalization;
using System.Linq;
using ColdWarStruggle.Core;
using Newtonsoft.Json.Linq;
namespace ColdWarStruggle.Infrastructure
{
    public static class HeadlineParametersLoader
    {
        public static HeadlineOrderParameters Load(string turnFlowJson)
        {
            // Reuse the existing supported turn-flow profile gate rather than copy version/source facts.
            TurnCalendarLoader.Load(turnFlowJson);
            var root=(JObject)ContractJson.Parse(turnFlowJson);
            var allowed=new[]{"sort","tie_first_side","scoring_value"};
            if(root["headline"] is not JObject headline || headline.Count!=allowed.Length || headline.Properties().Any(p=>!allowed.Contains(p.Name)))
                throw new ArgumentException("Invalid headline parameter fields.");
            if(headline["sort"]?.Type!=JTokenType.String || (string)headline["sort"]!="descending_headline_value")
                throw new ArgumentException("Unsupported headline ordering strategy.");
            if(headline["tie_first_side"]?.Type!=JTokenType.String)throw new ArgumentException("Invalid headline tie side.");
            var side=(string)headline["tie_first_side"] switch {"US"=>PlayerSide.US,"USSR"=>PlayerSide.USSR,_=>throw new ArgumentException("Unknown headline tie side.")};
            var token=headline["scoring_value"];
            if(token?.Type!=JTokenType.Integer || !int.TryParse(token.ToString(),NumberStyles.None,CultureInfo.InvariantCulture,out var scoringValue))
                throw new ArgumentException("Invalid nonnegative headline scoring value.");
            return new HeadlineOrderParameters(scoringValue,side);
        }
    }
}
