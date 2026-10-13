using System;
using System.Collections.Generic;
using System.Linq;
using ColdWarStruggle.Core;
namespace ColdWarStruggle.Infrastructure
{
    public static class EraRemovalCatalogLoader
    {
        public static EraCountryRemovalEffect Load(string json,string schema,MapDefinition map,IReadOnlyCollection<string> knownEffectIds)
        {
            if(map==null || knownEffectIds==null)throw new ArgumentNullException();
            if(knownEffectIds.Count==0 || knownEffectIds.Any(string.IsNullOrWhiteSpace))throw new ArgumentException("Known effect references are required.");
            try
            {
                var root=BudgetedRemovalCatalogLoader.ReadDocument(json,schema);var p=root["parameters"]["east_european_unrest"];
                var card=(string)p["card_id"];var effect=(string)p["effect_id"];
                if(card!="card.east_european_unrest" || effect!="effect.east_european_unrest" || !knownEffectIds.Contains(effect))
                    throw new ArgumentException("Invalid era removal identity or reference.");
                if((string)p["template"]!="DistinctCountryRemovalByEra" || (string)p["shortage_policy"]!="SelectAvailableDistinctThenClampPerCountry"
                    || (string)p["era_source"]["dataset_id"]!="rules.deluxe2015.turn-flow" || (string)p["era_source"]["json_pointer"]!="/era_bands")
                    throw new ArgumentException("Unsupported era removal semantics.");
                BudgetedRemovalCatalogLoader.ValidateSources(root,p);
                var region=(string)p["target_region_id"];if(map.GetCountriesInRegion(region).Count==0)throw new ArgumentException("Unknown removal region.");
                var amount=p["amount_by_era"];var amounts=new Dictionary<GameEra,int>{{GameEra.Early,(int)amount["early"]},{GameEra.Mid,(int)amount["mid"]},{GameEra.Late,(int)amount["late"]}};
                return new EraCountryRemovalEffect(card,effect,(string)p["beneficiary"]=="US"?PlayerSide.US:PlayerSide.USSR,region,(int)p["target_count"],amounts);
            }
            catch(OverflowException error) { throw new ArgumentException("Era removal parameter out of range.",error); }
        }
    }
}
