using System;
using System.Collections.Generic;
using System.Linq;
using ColdWarStruggle.Core;
using ColdWarStruggle.Infrastructure;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public sealed class VietnamOpsFoundationTests
    {
        static CountryId C(string name)=>new CountryId("country."+name);
        static readonly CardId Card=new CardId("card.vietnam_revolts");
        internal static TurnOpsEffectDefinition Load(string json=null)=>VietnamRevoltsDefinitionLoader.Load(json??TurnOpsEffectTests.Data(),TurnOpsEffectTests.Schema(),DefinitionTests.World(),CardCatalogLoaderTests.Load());
        static GameTurnContext Turn(int n=1)=>PlayScoringTests.Ready().Practice.Definition.Calendar.ForTurn(n);
        static AddFixedInfluenceEffect Grant(int amount=2,PlayerSide side=PlayerSide.USSR)=>new AddFixedInfluenceEffect(Card.Value,"effect.vietnam_revolts",side,C("vietnam"),amount);
        static TurnOpsEffectDefinition Regional()=>new TurnOpsEffectDefinition(Card,"effect.vietnam_revolts",new OpsRegionalBonus("effect.vietnam_revolts","region.southeast_asia",1),Grant());
        static GameState Board(int us=7,int su=0,long version=0,long sequence=0)
        {
            var map=DefinitionTests.World();var countries=map.Countries.ToDictionary(c=>c.Id,c=>new CountryState(0,0));countries[C("vietnam")]=new CountryState(us,su);
            return new GameState(map,countries,new OperationsContext("fixed-grant",PlayerSide.US,0,Array.Empty<CountryId>(),costs:new InfluenceCosts(1,2)),version:version,eventSequence:sequence);
        }
        [Test] public void Loader_reads_canonical_grant_bonus_and_fixed_beneficiary_without_enabling_card()
        {
            var d=Load();Assert.That(d.Card,Is.EqualTo(Card));Assert.That(d.EffectId,Is.EqualTo("effect.vietnam_revolts"));Assert.That(d.AffectedSide,Is.EqualTo(TurnOpsAffectedSide.USSR));Assert.That(d.Delta,Is.EqualTo(1));Assert.That(d.RegionalBonus.Region,Is.EqualTo("region.southeast_asia"));Assert.That(d.ImmediateInfluence.Country,Is.EqualTo(C("vietnam")));Assert.That(d.ImmediateInfluence.Amount,Is.EqualTo(2));
            Assert.That(()=>new PracticeTurnOpsCatalog(CardCatalogLoaderTests.Load(),TurnOpsEffectTests.Effects().Append(d)),Throws.InstanceOf<ArgumentException>());Assert.That(TurnOpsEffectTests.Effects().Length,Is.EqualTo(2));
        }
        [Test] public void Flat_catalog_rejects_regional_effect_even_when_it_reuses_supported_purge_identity()
        {
            var id=new CardId("card.red_scare_purge");var effect="effect.red_scare_purge";
            var regional=new TurnOpsEffectDefinition(id,effect,new OpsRegionalBonus(effect,"region.southeast_asia",1),new AddFixedInfluenceEffect(id.Value,effect,PlayerSide.USSR,C("vietnam"),2));
            Assert.That(()=>new PracticeTurnOpsCatalog(CardCatalogLoaderTests.Load(),new[]{regional}),Throws.InstanceOf<ArgumentException>());
        }
        [Test] public void Grant_amount_is_read_from_data_instead_of_duplicated_constant()
        {var j=JObject.Parse(TurnOpsEffectTests.Data());j["parameters"]["vietnam_revolts"]["immediate_influence"]["amount"]=3;Assert.That(Load(j.ToString()).ImmediateInfluence.Amount,Is.EqualTo(3));}
        [TestCase("identity")][TestCase("effect")][TestCase("template")][TestCase("side")][TestCase("duration")][TestCase("expiry")][TestCase("kind")][TestCase("region")][TestCase("delta")][TestCase("country")][TestCase("grant-side")][TestCase("amount-zero")][TestCase("amount-string")][TestCase("amount-float")][TestCase("amount-overflow")][TestCase("missing-grant")][TestCase("repeat")][TestCase("duplicate-policy")][TestCase("profile")][TestCase("extra")][TestCase("unverified")][TestCase("unknown-ref")][TestCase("duplicate-ref")][TestCase("duplicate-source")]
        public void Unsupported_semantics_numeric_coercion_and_sources_are_rejected(string fault)
        {
            var j=JObject.Parse(TurnOpsEffectTests.Data());var v=j["parameters"]["vietnam_revolts"];
            switch(fault)
            {
                case "identity":v["card_id"]="card.containment";break;case "effect":v["effect_id"]="effect.containment";break;case "template":v["template"]="Flat";break;case "side":v["affected_side"]="US";break;
                case "duration":v["duration"]="Permanent";break;case "expiry":v["expiry_boundary"]="ActionEnd";break;case "kind":v["modifier"]["kind"]="Flat";break;case "region":v["modifier"]["region_id"]="region.asia";break;case "delta":v["modifier"]["delta"]=-1;break;
                case "country":v["immediate_influence"]["country_id"]="country.thailand";break;case "grant-side":v["immediate_influence"]["side"]="US";break;case "amount-zero":v["immediate_influence"]["amount"]=0;break;case "amount-string":v["immediate_influence"]["amount"]="2";break;case "amount-float":v["immediate_influence"]["amount"]=2.5;break;case "amount-overflow":v["immediate_influence"]["amount"]=2147483648L;break;case "missing-grant":v["immediate_influence"]=null;break;
                case "repeat":v["repeat_effect_same_side"]="Stack";break;case "duplicate-policy":v["duplicate_activation"]="ApplyAgain";break;case "profile":j["content_version"]="99";break;case "extra":v["script"]="x";break;
                case "unverified":v["source_refs"][0]["verification"]="Pending";break;case "unknown-ref":v["source_refs"][0]["source_id"]="unknown";break;case "duplicate-ref":((JArray)v["source_refs"]).Add(v["source_refs"][0].DeepClone());break;case "duplicate-source":((JArray)j["source_catalog"]).Add(j["source_catalog"][0].DeepClone());break;
            }
            Assert.That(()=>Load(j.ToString()),Throws.InstanceOf<ArgumentException>());
        }
        [Test] public void Missing_map_printed_binding_or_contract_cannot_load()
        {
            var data=TurnOpsEffectTests.Data();var schema=TurnOpsEffectTests.Schema();var cards=CardCatalogLoaderTests.Load();
            Assert.That(()=>VietnamRevoltsDefinitionLoader.Load(data,schema,null,cards),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>VietnamRevoltsDefinitionLoader.Load(data,schema,DefinitionTests.World(),new CardCatalog(cards.Cards.Where(c=>!c.Id.Equals(Card)))),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>VietnamRevoltsDefinitionLoader.Load(data,schema.Replace("urn:coldwarheat:contract:persistent-effects-batch1:1","unknown"),DefinitionTests.World(),cards),Throws.InstanceOf<ArgumentException>());
        }
        [TestCase(PlayerSide.US)][TestCase(PlayerSide.USSR)] public void Direct_grant_changes_only_beneficiary_without_ops_reachability_or_control_cost(PlayerSide side)
        {
            var s=Board(7,3,12,25);var e=Grant(2,side);var q=InfluenceEventRules.Query(s,e,InfluenceEventPolicy.NoActiveInterference);Assert.That(q.Status,Is.EqualTo(InfluenceEventStatus.Ready));Assert.That(q.RequiredTargets,Is.Zero);
            var p=InfluenceEventRules.Plan(s,e,null,InfluenceEventPolicy.NoActiveInterference);Assert.That(p.Resolved,Is.True);Assert.That(p.Changes.Count,Is.EqualTo(1));var c=p.Changes[0];Assert.That(c.Country,Is.EqualTo(C("vietnam")));Assert.That(c.Side,Is.EqualTo(side));Assert.That(c.Before,Is.EqualTo(side==PlayerSide.US?7:3));Assert.That(c.After-c.Before,Is.EqualTo(2));Assert.That(c.Sequence,Is.EqualTo(26));Assert.That(p.ResolutionSequence,Is.EqualTo(27));Assert.That(p.NextState.Version,Is.EqualTo(13));Assert.That(p.NextState.Operations,Is.SameAs(s.Operations));Assert.That(p.NextState.SpentOps,Is.Zero);Assert.That(s.GetInfluence(C("vietnam"),PlayerSide.USSR),Is.EqualTo(3));Assert.That(p.NextState.GetInfluence(C("vietnam"),side==PlayerSide.US?PlayerSide.USSR:PlayerSide.US),Is.EqualTo(side==PlayerSide.US?3:7));
        }
        [TestCase("amount")][TestCase("version")][TestCase("sequence")] public void Fixed_grant_overflow_never_returns_partial_state(string fault)
        {
            var s=Board(su:fault=="amount"?int.MaxValue:0,version:fault=="version"?long.MaxValue:0,sequence:fault=="sequence"?long.MaxValue-1:0);var p=InfluenceEventRules.Plan(s,Grant(),null,InfluenceEventPolicy.NoActiveInterference);Assert.That(p.Error,Is.EqualTo(InfluenceEventError.NumericOverflow));Assert.That(p.NextState,Is.Null);Assert.That(p.Changes,Is.Empty);Assert.That(s.GetInfluence(C("vietnam"),PlayerSide.USSR),Is.EqualTo(fault=="amount"?int.MaxValue:0));
        }
        [Test] public void Grant_rejects_selection_unknown_country_and_interference_without_board_change()
        {
            var s=Board();Assert.That(InfluenceEventRules.Plan(s,Grant(),new InfluenceEventSelection(PlayerSide.USSR,0,new[]{C("vietnam")}),InfluenceEventPolicy.NoActiveInterference).Error,Is.EqualTo(InfluenceEventError.InvalidSelection));
            Assert.That(InfluenceEventRules.Plan(s,new AddFixedInfluenceEffect(Card.Value,"effect.vietnam_revolts",PlayerSide.USSR,C("unknown"),2),null,InfluenceEventPolicy.NoActiveInterference).Error,Is.EqualTo(InfluenceEventError.InvalidDefinition));
            Assert.That(InfluenceEventRules.Plan(s,Grant(),null,InfluenceEventPolicy.Unknown).Status,Is.EqualTo(InfluenceEventStatus.RequiresRuling));Assert.That(InfluenceEventRules.Plan(s,Grant(),null,InfluenceEventPolicy.Forbidden).Status,Is.EqualTo(InfluenceEventStatus.Blocked));
        }
        [Test] public void Fixed_grant_respects_setup_and_finished_game_gates()
        {
            var s=Board();var ended=new GameState(s.Map,s.Countries,s.Operations,ended:true);
            Assert.That(InfluenceEventRules.Plan(ended,Grant(),null,InfluenceEventPolicy.NoActiveInterference).Error,Is.EqualTo(InfluenceEventError.GameAlreadyEnded));
            var setup=SetupDeploymentTests.Started();Assert.That(InfluenceEventRules.Plan(setup,Grant(),null,InfluenceEventPolicy.NoActiveInterference).Error,Is.EqualTo(InfluenceEventError.SetupInProgress));
        }
        [Test] public void Fixed_grant_flow_distinguishes_root_player_and_beneficiary_and_completes_without_choice()
        {
            var before=Board();var s=new GameState(before.Map,before.Countries,before.Operations,influenceEvent:new InfluenceEventExecution("grant",Grant(),PlayerSide.US,InfluenceEventPolicy.NoActiveInterference));
            Assert.That(InfluenceEventFlow.Begin(s,PlayerSide.USSR).Error,Is.EqualTo(RuleError.WrongActor));var plan=InfluenceEventFlow.Begin(s,PlayerSide.US);Assert.That(plan.Accepted,Is.True);Assert.That(plan.NextState.InfluenceEvent.Stage,Is.EqualTo(InfluenceEventStage.Resolved));Assert.That(plan.Changes.Single().Side,Is.EqualTo(PlayerSide.USSR));Assert.That(plan.DecisionRequestedSequence,Is.Null);Assert.That(plan.ResolutionSequence,Is.EqualTo(2));Assert.That(InfluenceEventFlow.Begin(plan.NextState,PlayerSide.US).Error,Is.EqualTo(RuleError.EventAlreadyResolved));
        }
        [TestCase(0)][TestCase(-1)] public void Fixed_grant_requires_positive_amount(int amount)=>Assert.That(()=>Grant(amount),Throws.InstanceOf<ArgumentException>());
        [Test] public void Regional_constructor_rejects_mismatched_grant_or_flat_overload_for_USSR()
        {
            var b=new OpsRegionalBonus("effect.vietnam_revolts","region.southeast_asia",1);
            Assert.That(()=>new TurnOpsEffectDefinition(Card,"effect.vietnam_revolts",b,Grant(side:PlayerSide.US)),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>new TurnOpsEffectDefinition(Card,"effect.other",b,Grant()),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>new TurnOpsEffectDefinition(Card,"effect.vietnam_revolts",null,Grant()),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>new TurnOpsEffectDefinition(Card,"effect.vietnam_revolts",b,null),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>new TurnOpsEffectDefinition(Card,"effect.vietnam_revolts",TurnOpsAffectedSide.USSR,-1),Throws.InstanceOf<ArgumentException>());
        }
        [TestCase(PlayerSide.US)][TestCase(PlayerSide.USSR)] public void Fixed_USSR_contribution_is_not_transferred_with_event_controller(PlayerSide controller)
        {
            var turn=Turn();var initial=TurnOpsEffectState.Create(turn);var e=Regional();var a=initial.Activate(e,controller,"vietnam",9,turn);Assert.That(a.Accepted,Is.True);Assert.That(a.Instance.AffectedSide,Is.EqualTo(PlayerSide.USSR));Assert.That(a.Instance.Controller,Is.EqualTo(controller));Assert.That(a.Instance.ExpiryBoundary,Is.EqualTo("ExpireAndReset"));Assert.That(initial.Instances,Is.Empty);Assert.That(a.After.GetModifiers(turn),Is.Empty);Assert.That(a.After.GetRegionalBonuses(turn,PlayerSide.US),Is.Empty);Assert.That(a.After.GetRegionalBonuses(turn,PlayerSide.USSR).Single(),Is.SameAs(e.RegionalBonus));Assert.Throws<NotSupportedException>(()=>((IList<OpsRegionalBonus>)a.After.GetRegionalBonuses(turn,PlayerSide.USSR)).Clear());
        }
        [Test] public void Regional_query_checks_context_and_retries_do_not_duplicate_contribution()
        {
            var t=Turn();var e=Regional();var a=TurnOpsEffectState.Create(t).Activate(e,PlayerSide.US,"r",8,t);var again=a.After.Activate(e,PlayerSide.US,"r",8,t);Assert.That(again.AlreadyApplied,Is.True);Assert.That(again.After,Is.SameAs(a.After));Assert.That(a.After.Activate(e,PlayerSide.USSR,"r",8,t).Error,Is.EqualTo(RuleError.CommandIdConflict));Assert.That(a.After.Activate(e,PlayerSide.USSR,"new",9,t).Error,Is.EqualTo(RuleError.EventRequiresRuling));Assert.That(a.After.GetRegionalBonuses(t,PlayerSide.USSR).Count,Is.EqualTo(1));
            Assert.That(()=>a.After.GetRegionalBonuses(Turn(2),PlayerSide.USSR),Throws.InstanceOf<ArgumentException>());Assert.That(()=>a.After.GetRegionalBonuses(t,(PlayerSide)99),Throws.InstanceOf<ArgumentException>());Assert.That(()=>a.After.GetRegionalBonuses(null,PlayerSide.USSR),Throws.InstanceOf<ArgumentException>());
        }
        [TestCase(OpsValuePurpose.Influence)][TestCase(OpsValuePurpose.Coup)][TestCase(OpsValuePurpose.Realignment)]
        public void China_purge_vietnam_combine_and_low_value_floor_applies_after_regional_bonus(OpsValuePurpose purpose)
        {
            var t=Turn();var effects=TurnOpsEffectState.Create(t).Activate(Regional(),PlayerSide.US,"v",1,t).After;effects=effects.Activate(TurnOpsEffectTests.Effects()[1],PlayerSide.US,"p",2,t).After;
            var map=DefinitionTests.World();var quote=OpsGeneralRules.QuoteCard(OpsGeneralTests.Definition(),OpsGeneralTests.Card(4),PlayerSide.USSR,purpose,effects.GetModifiers(t));var bonus=effects.GetRegionalBonuses(t,PlayerSide.USSR);var b=new OpsRegionalBudget(map,quote,bonus.Append(new OpsRegionalBonus("china","region.asia",1)));Assert.That(b.BudgetFor(C("vietnam")),Is.EqualTo(5));Assert.That(b.BudgetFor(C("japan")),Is.EqualTo(4));Assert.That(b.BudgetFor(C("iran")),Is.EqualTo(3));
            var low=OpsGeneralRules.QuoteCard(OpsGeneralTests.Definition(),OpsGeneralTests.Card(1),PlayerSide.USSR,purpose,effects.GetModifiers(t));Assert.That(new OpsRegionalBudget(map,low,bonus).BudgetFor(C("vietnam")),Is.EqualTo(1));
        }
        [TestCase(OpsValuePurpose.Space)][TestCase(OpsValuePurpose.OpsThreshold)] public void Regional_bonus_never_changes_non_map_quotes(OpsValuePurpose purpose)
        {var t=Turn();var effects=TurnOpsEffectState.Create(t).Activate(Regional(),PlayerSide.USSR,"v",1,t).After;var q=OpsGeneralRules.QuoteCard(OpsGeneralTests.Definition(),OpsGeneralTests.Card(1),PlayerSide.USSR,purpose,effects.GetModifiers(t));Assert.That(q.GeneralValue,Is.EqualTo(1));Assert.That(()=>new OpsRegionalBudget(DefinitionTests.World(),q,effects.GetRegionalBonuses(t,PlayerSide.USSR)),Throws.InstanceOf<ArgumentException>());}
        [Test] public void Accepted_outside_target_loses_only_relevant_bonus_and_rejected_target_keeps_prefix()
        {
            var t=Turn();var effects=TurnOpsEffectState.Create(t).Activate(Regional(),PlayerSide.USSR,"v",1,t).After;var q=OpsGeneralRules.QuoteCard(OpsGeneralTests.Definition(),OpsGeneralTests.Card(4),PlayerSide.USSR,OpsValuePurpose.Influence,effects.GetModifiers(t));var b=new OpsRegionalBudget(DefinitionTests.World(),q,effects.GetRegionalBonuses(t,PlayerSide.USSR).Append(new OpsRegionalBonus("china","region.asia",1)));
            Assert.That(b.TrySpend(C("vietnam"),4,out var prefix),Is.True);Assert.That(prefix.TrySpend(C("iran"),1,out var rejected),Is.False);Assert.That(rejected,Is.Null);Assert.That(prefix.EffectiveBudget,Is.EqualTo(6));Assert.That(prefix.TrySpend(C("japan"),1,out var asia),Is.True);Assert.That(asia.EffectiveBudget,Is.EqualTo(5));Assert.That(asia.EligibleBonuses.Select(x=>x.Id),Is.EqualTo(new[]{"china"}));Assert.That(asia.TrySpend(C("vietnam"),1,out _),Is.False);Assert.That(effects.GetRegionalBonuses(t,PlayerSide.USSR).Count,Is.EqualTo(1));
        }
    }
}
