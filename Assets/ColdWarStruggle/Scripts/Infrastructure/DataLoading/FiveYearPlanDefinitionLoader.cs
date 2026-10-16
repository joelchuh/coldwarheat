using System;
using System.Collections.Generic;
using System.Linq;
using ColdWarStruggle.Core;
namespace ColdWarStruggle.Infrastructure
{
    public static class FiveYearPlanDefinitionLoader
    {
        public static FiveYearPlanDefinition Load(string json,string schema,CardCatalog cards,IReadOnlyCollection<string> knownEffectIds)
        {
            if(cards==null || knownEffectIds==null)throw new ArgumentNullException();
            if(knownEffectIds.Count==0 || knownEffectIds.Any(string.IsNullOrWhiteSpace))throw new ArgumentException("Known effect references required.");
            try
            {
                var p=HandEventContract.Load(json,schema,"five_year_plan");var effect=(string)p["effect_id"];
                if((string)p["card_id"]!="card.five_year_plan" || effect!="effect.five_year_plan" || !knownEffectIds.Contains(effect)
                    || (string)p["template"]!="RandomDiscardWithAffiliationEvent" || (string)p["hand_owner"]!="USSR" || (string)p["source_zone"]!="Hand"
                    || (string)p["candidate_kind_policy"]!="AllOrdinaryCardsIncludingScoring" || (int)p["discard_count"]!=1
                    || (string)p["sampling_policy"]!="UniformBoundedDrawOnStableCardIdOrder" || (string)p["empty_hand_policy"]!="ResolvedNoChangeWithoutRandom"
                    || (string)p["singleton_policy"]!="OneLogicalBoundedDraw" || (string)p["immediate_event_affiliation"]!="US"
                    || (string)p["child_event_controller"]!="US" || (string)p["child_event_source"]!="RandomDiscardTriggeredEvent"
                    || (bool)p["creates_card_use"] || (bool)p["automatically_grants_discarded_card_ops"] || (bool)p["consumes_root_slot"]
                    || (string)p["child_phasing_policy"]!="InheritRootPhasingPlayer" || (string)p["ordinary_discard_destination"]!="DiscardPile"
                    || (string)p["triggered_event_zone"]!="InResolution" || (string)p["triggered_event_disposition"]!="ExistingEventOutcomeAndPrintedRemovalPolicy"
                    || (string)p["candidate_visibility"]!="AuthorityOnly" || (string)p["selected_card_visibility"]!="BothPlayersAfterAcceptedDiscard")
                    throw new ArgumentException("Unsupported Five Year Plan semantics.");
                return new FiveYearPlanDefinition(cards,new CardId((string)p["card_id"]),effect,PlayerSide.USSR,CardAffiliation.US,(int)p["discard_count"]);
            }
            catch(OverflowException e){throw new ArgumentException("Five Year Plan parameter out of range.",e);}
        }
    }
}
