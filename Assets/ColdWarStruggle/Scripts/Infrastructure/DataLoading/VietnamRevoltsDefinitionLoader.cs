using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ColdWarStruggle.Core;
using Newtonsoft.Json.Linq;
namespace ColdWarStruggle.Infrastructure
{
    public static class VietnamRevoltsDefinitionLoader
    {
        public static TurnOpsEffectDefinition Load(string json,string schema,MapDefinition map,CardCatalog cards)
        {
            if(map==null || cards==null)throw new ArgumentNullException();
            OpsGeneralDefinitionLoader.LoadSubset(json,schema);
            var root=ContractJson.Parse(json);var p=root["parameters"]["vietnam_revolts"];
            var card=new CardId((string)p["card_id"]);var effect=(string)p["effect_id"];var modifier=p["modifier"];
            if(card.Value!="card.vietnam_revolts" || effect!="effect.vietnam_revolts" || (string)p["template"]!="ActivateTurnOpsModifier"
                || (string)p["affected_side"]!="USSR" || (string)p["duration"]!="RemainderOfActivationTurn" || (string)p["expiry_boundary"]!="ExpireAndReset"
                || (string)modifier["kind"]!="AllTargetsInRegion" || (string)modifier["region_id"]!="region.southeast_asia"
                || (string)p["duplicate_activation"]!="IdempotentByResolutionId" || (string)p["repeat_effect_same_side"]!="RequiresRuling"
                || p["immediate_influence"] is not JObject grant)throw new ArgumentException("Unsupported Vietnam event semantics.");
            var country=new CountryId((string)grant["country_id"]);var region=(string)modifier["region_id"];
            if(country.Value!="country.vietnam" || (string)grant["side"]!="USSR" || !map.Contains(country)
                || !map.GetCountriesInRegion(region).Any(c=>c.Id.Equals(country)))throw new ArgumentException("Invalid Vietnam map binding.");
            var printed=cards.GetCard(card);
            if(printed.Kind!=CardKind.Event || printed.Affiliation!=CardAffiliation.USSR || printed.Effect?.EffectId!=effect)throw new ArgumentException("Invalid Vietnam printed binding.");
            var seen=new HashSet<string>(StringComparer.Ordinal);
            foreach(var reference in p["source_refs"])
            {
                var id=(string)reference["source_id"];
                if((id!="R2015" && id!="C-DELUXE-EN") || !seen.Add(id) || (string)reference["verification"]!="Verified"
                    || string.IsNullOrWhiteSpace((string)reference["locator"]))throw new ArgumentException("Invalid Vietnam source reference.");
            }
            if(seen.Count!=2)throw new ArgumentException("Incomplete Vietnam source references.");
            int Positive(JToken value)
            {
                if(value?.Type!=JTokenType.Integer || !int.TryParse(value.ToString(),NumberStyles.Integer,CultureInfo.InvariantCulture,out var number) || number<1)throw new ArgumentException("Positive bounded integer required.");
                return number;
            }
            return new TurnOpsEffectDefinition(card,effect,new OpsRegionalBonus(effect,region,Positive(modifier["delta"])),
                new AddFixedInfluenceEffect(card.Value,effect,PlayerSide.USSR,country,Positive(grant["amount"])));
        }
    }
}
