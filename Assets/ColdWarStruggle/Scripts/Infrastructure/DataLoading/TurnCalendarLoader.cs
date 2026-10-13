using System;
using System.Linq;
using ColdWarStruggle.Core;
using Newtonsoft.Json.Linq;
namespace ColdWarStruggle.Infrastructure
{
    public static class TurnCalendarLoader
    {
        // Loads era parameters only; headline/dealing/turn-end orchestration remains separate.
        public static TurnCalendar Load(string json)
        {
            var root=ContractJson.Parse(json) as JObject;
            if(root==null || !JToken.DeepEquals(root["schema_version"],new JValue(3))
                || (string)root["id"]!="rules.deluxe2015.turn-flow" || (string)root["content_version"]!="0.3.0"
                || (string)root["status"]!="verified-subset" || root["source"] is not JObject source || (string)source["id"]!="R2015")
                throw new ArgumentException("Unsupported turn calendar profile.");
            if(root["era_bands"] is not JArray bands || bands.Count!=3)throw new ArgumentException("Invalid era bands.");
            var allowed=new[]{"id","first_turn","last_turn","hand_target","normal_actions_per_side"};
            return new TurnCalendar(bands.Select(token=>
            {
                if(token is not JObject band || band.Count!=allowed.Length || band.Properties().Any(p=>!allowed.Contains(p.Name)))
                    throw new ArgumentException("Invalid era parameter fields.");
                var era=(string)band["id"] switch { "early"=>GameEra.Early,"mid"=>GameEra.Mid,"late"=>GameEra.Late,_=>throw new ArgumentException("Unknown era.") };
                return new EraTurnParameters(era,Positive(band["first_turn"]),Positive(band["last_turn"]),Positive(band["hand_target"]),Positive(band["normal_actions_per_side"]));
            }));
        }
        static int Positive(JToken token)
        {
            if(token?.Type!=JTokenType.Integer || !int.TryParse(token.ToString(),System.Globalization.NumberStyles.None,
                System.Globalization.CultureInfo.InvariantCulture,out var value) || value<1)throw new ArgumentException("Invalid positive era integer.");
            return value;
        }
    }
}
