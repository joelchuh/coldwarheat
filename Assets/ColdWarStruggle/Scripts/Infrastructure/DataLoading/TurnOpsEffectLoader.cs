using System;
using System.Collections.Generic;
using ColdWarStruggle.Core;
using Newtonsoft.Json.Linq;
namespace ColdWarStruggle.Infrastructure
{
    public static class TurnOpsEffectLoader
    {
        public static IReadOnlyList<TurnOpsEffectDefinition> LoadFlatSubset(string json,string schema)
        {
            // Validate the complete enclosing schema and shared profile/source catalog, including excluded Vietnam data.
            OpsGeneralDefinitionLoader.LoadSubset(json,schema);
            var root=ContractJson.Parse(json);
            TurnOpsEffectDefinition Read(string name,string owner,TurnOpsAffectedSide side)
            {
                var p=root["parameters"][name];
                if((string)p["card_id"]!="card."+name || (string)p["effect_id"]!="effect."+name || (string)p["template"]!="ActivateTurnOpsModifier"
                    || (string)p["affected_side"]!=owner || (string)p["duration"]!="RemainderOfActivationTurn" || (string)p["expiry_boundary"]!="ExpireAndReset"
                    || (string)p["modifier"]["kind"]!="Flat" || p["modifier"]["region_id"].Type!=JTokenType.Null || p["immediate_influence"].Type!=JTokenType.Null
                    || (string)p["duplicate_activation"]!="IdempotentByResolutionId" || (string)p["repeat_effect_same_side"]!="RequiresRuling")throw new ArgumentException("Unsupported flat effect semantics.");
                var seen=new HashSet<string>(StringComparer.Ordinal);
                foreach(var source in p["source_refs"])
                {
                    var id=(string)source["source_id"];
                    if((id!="C-DELUXE-EN" && id!="R2015") || !seen.Add(id) || (string)source["verification"]!="Verified")throw new ArgumentException("Unverified or duplicate flat effect source.");
                }
                if(seen.Count!=2)throw new ArgumentException("Incomplete flat effect sources.");
                return new TurnOpsEffectDefinition(new CardId((string)p["card_id"]),(string)p["effect_id"],side,(int)p["modifier"]["delta"]);
            }
            return Array.AsReadOnly(new[]{Read("containment","US",TurnOpsAffectedSide.US),Read("red_scare_purge","OpponentOfEventController",TurnOpsAffectedSide.OpponentOfController)});
        }
    }
}
