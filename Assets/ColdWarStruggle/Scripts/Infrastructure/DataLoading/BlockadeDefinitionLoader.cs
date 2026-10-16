using System;
using System.Collections.Generic;
using System.Linq;
using ColdWarStruggle.Core;
namespace ColdWarStruggle.Infrastructure
{
    public static class BlockadeDefinitionLoader
    {
        public static BlockadeDefinition Load(string json,string schema,MapDefinition map,CardCatalog cards,OpsGeneralDefinition ops,IReadOnlyCollection<string> knownEffectIds)
        {
            if(map==null || cards==null || ops==null || knownEffectIds==null)throw new ArgumentNullException();
            if(knownEffectIds.Count==0 || knownEffectIds.Any(string.IsNullOrWhiteSpace))throw new ArgumentException("Known effect references required.");
            try
            {
                var p=HandEventContract.Load(json,schema,"blockade");var effect=(string)p["effect_id"];
                if((string)p["card_id"]!="card.blockade" || effect!="effect.blockade" || !knownEffectIds.Contains(effect)
                    || (string)p["template"]!="DiscardOrRemoveAllInfluence" || (string)p["decision_owner"]!="US" || (string)p["hand_owner"]!="US"
                    || (string)p["ops_purpose"]!="OpsThreshold" || (string)p["ops_policy_ref"]["dataset_id"]!="effects.deluxe2015.persistent_batch1"
                    || (string)p["ops_policy_ref"]["json_pointer"]!="/ops_policy" || (string)p["source_zone"]!="Hand" || (string)p["allowed_kind"]!="Event"
                    || !(bool)p["allow_decline"] || (string)p["discard_destination"]!="DiscardPile" || (string)p["discard_event_policy"]!="DoNotTrigger"
                    || (string)p["fallback_influence"]["side"]!="US" || (string)p["fallback_influence"]["operation"]!="RemoveAll"
                    || (string)p["candidate_visibility"]!="DecisionOwnerOnly")throw new ArgumentException("Unsupported blockade semantics.");
                return new BlockadeDefinition(map,cards,ops,new CardId((string)p["card_id"]),effect,PlayerSide.US,(int)p["minimum_effective_ops"],new CountryId((string)p["fallback_influence"]["country_id"]));
            }
            catch(OverflowException e){throw new ArgumentException("Blockade parameter out of range.",e);}
        }
    }
}
