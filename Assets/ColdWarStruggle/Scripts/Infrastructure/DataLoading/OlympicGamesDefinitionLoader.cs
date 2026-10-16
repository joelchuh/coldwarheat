using System;
using System.Collections.Generic;
using System.Linq;
using ColdWarStruggle.Core;
using Newtonsoft.Json.Linq;
namespace ColdWarStruggle.Infrastructure
{
    public static class OlympicGamesDefinitionLoader
    {
        public static OlympicGamesDefinition Load(string json,string schema,CardCatalog cards,StandardDieDefinition die,IReadOnlyCollection<string> knownEffectIds)
        {
            if(cards==null || die==null || knownEffectIds==null)throw new ArgumentNullException();
            if(!knownEffectIds.Contains("effect.olympic_games") || knownEffectIds.Any(string.IsNullOrWhiteSpace))throw new ArgumentException("Known Olympic effect required.");
            var root=RuleParameterContract.Load(json,schema,"olympic-games");RuleParameterContract.Profile(root,"effects.deluxe2015.olympic_games");
            var sources=RuleParameterContract.Sources(root,new[]{
                ("C-DELUXE-EN","https://www.gmtgames.com/nnts/TS_Cards_Deluxe.pdf","English Deluxe card images"),
                ("R2015","https://www.gmtgames.com/nnts/TS_Rules-2015.pdf","Deluxe rules 2015"),
                ("F2010","https://www.gmtgames.com/nnts/FAQv5.pdf","FAQ v5 2010, compatible Deluxe answers only"),
                ("PROJECT-OG1",(string)null,"Atomic paired-roll chain and internal scheduling contracts, 2026-10-12")});
            var p=root.SelectToken("parameters.olympic_games") as JObject;
            if(p==null)throw new ArgumentException("Missing Olympic parameters.");
            var expected=new Dictionary<string,object>{
                {"card_id","card.olympic_games"},{"effect_id","effect.olympic_games"},{"template","OpponentChoiceOlympicContestOrBoycottOps"},
                {"sponsor","EffectController"},{"opponent","OpponentOfSponsor"},{"branch_decision_owner","OpponentOfSponsor"},{"branches",new[]{"Participate","Boycott"}},
                {"participate.draw_order",new[]{"US","USSR"}},{"participate.sponsor_die_modifier",2},{"participate.comparison","StrictlyHigherModifiedTotal"},
                {"participate.winner_vp",2},{"participate.tie_policy","MandatoryNewPairedRoll"},{"participate.tie_vp",0},{"participate.vp_checkpoint","VpBatchClosed"},
                {"participate.military_credit","None"},{"participate.branch_change_after_accepted_pair",false},
                {"participate.resume_state_policy","ValidateInitialRunningState_UnchangedDefcon_PhaseSpecificTerminalBeforeEveryPair"},
                {"boycott.defcon_delta",-1},{"boycott.ordered_steps",new[]{"DegradeDefcon","NuclearWarReachedCheckpoint","IfContinueOfferOptionalAsIfCardOperations"}},
                {"boycott.nuclear_loser","InheritedRootPhasingPlayer"},{"boycott.terminal_grant","None"},
                {"boycott.operations.actor","Sponsor"},{"boycott.operations.base_ops",4},{"boycott.operations.budget_source","VerifiedEventLiteral"},
                {"boycott.operations.allowed_modes",new[]{"Influence","Coup","Realignment"}},{"boycott.operations.may_decline",true},
                {"boycott.operations.mode_lock","FirstAcceptedOperation"},{"boycott.operations.finish_policy","BetweenAcceptedOperationsMayForfeitRemainder"},
                {"boycott.operations.coup_credit_kind","OperationsCoup"},{"boycott.operations.geographic_exemption",false},{"boycott.operations.treaty_exemption",false},
                {"boycott.operations.space_allowed",false},{"boycott.operations.extra_card",false},{"boycott.operations.extra_root_action",false},
                {"boycott.operations.modifier_policy","ActorEffectsAndVerifiedRegionalHistory"},
                {"boycott.operations.influence_baseline","RootActionRoundStartActorOrSeparatelyVerifiedNamedPolicy"},
                {"boycott.operations.missing_influence_baseline","RequiresRulingOnlyInfluenceQueryOrChoice"},
                {"boycott.operations.ordinary_nuclear_loser","InheritedRootPhasingPlayer"},{"boycott.operations.fatal_crisis_loser","ActualCoupActor"}};
            foreach(var item in expected)RuleParameterContract.Require(p.SelectToken(item.Key),item.Value);
            RuleParameterContract.Reference(p.SelectToken("participate.die_ref"),"rules.deluxe2015.standard-dice","/definitions/standard_d6");
            RuleParameterContract.Reference(p.SelectToken("boycott.defcon_ref"),"rules.deluxe2015.defcon","");
            RuleParameterContract.Reference(p.SelectToken("boycott.operations.ops_policy_ref"),"effects.deluxe2015.persistent_batch1","/ops_policy");
            RuleParameterContract.Reference(p.SelectToken("boycott.operations.military_policy_ref"),"rules.deluxe2015.military-operations","/credit_sources/OperationsCoup");
            RuleParameterContract.Reference(p.SelectToken("boycott.operations.intervention_ref"),"effects.deluxe2015.defcon_events_batch1","/intervention_policy");
            var shared=new[]{("standard_die","rules.deluxe2015.standard-dice","/definitions/standard_d6"),("defcon","rules.deluxe2015.defcon",""),
                ("victory","rules.deluxe2015.victory",""),("ops","effects.deluxe2015.persistent_batch1","/ops_policy"),
                ("military","rules.deluxe2015.military-operations","/credit_sources/OperationsCoup"),("coup_intervention","effects.deluxe2015.defcon_events_batch1","/intervention_policy")};
            foreach(var r in shared)RuleParameterContract.Reference(root.SelectToken("shared_refs."+r.Item1),r.Item2,r.Item3);
            if(!(p["source_refs"] is JArray references) || references.Count!=sources.Count)throw new ArgumentException("Incomplete Olympic evidence.");
            var seen=new HashSet<string>(StringComparer.Ordinal);
            foreach(var r in references)
            {
                if(!(r is JObject) || r["source_id"]?.Type!=JTokenType.String || !sources.Contains((string)r["source_id"]) || !seen.Add((string)r["source_id"]))throw new ArgumentException("Invalid Olympic evidence.");
                RuleParameterContract.Require(r["verification"],"Verified");
            }
            return new OlympicGamesDefinition(cards,die,new CardId((string)p["card_id"]),(string)p["effect_id"],(int)p["participate"]["sponsor_die_modifier"],
                (int)p["participate"]["winner_vp"],(int)p["participate"]["tie_vp"],(int)p["boycott"]["defcon_delta"],(int)p["boycott"]["operations"]["base_ops"]);
        }
    }
}
