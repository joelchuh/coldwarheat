using System;
using System.Linq;
using System.Collections.Generic;
using ColdWarStruggle.Core;
using ColdWarStruggle.Infrastructure;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class TurnOpsEffectTests
    {
        internal static string Data()=>PlayInfluenceEventTests.Read("Data/Cards/deluxe-2015/persistent-effects-batch1.parameters.json");
        internal static string Schema()=>PlayInfluenceEventTests.Read("Docs/Contracts/persistent-effects-batch1.schema.json");
        internal static TurnOpsEffectDefinition[] Effects()=>TurnOpsEffectLoader.LoadFlatSubset(Data(),Schema()).ToArray();
        static GameTurnContext Turn(int number=1)=>PlayScoringTests.Ready().Practice.Definition.Calendar.ForTurn(number);
        [Test] public void Actual_dataset_loads_only_flat_events_and_values_come_from_parameters()
        {
            var e=Effects();Assert.That(e.Select(x=>x.Card.Value),Is.EquivalentTo(new[]{"card.containment","card.red_scare_purge"}));Assert.That(e[0].Delta,Is.EqualTo(1));Assert.That(e[1].Delta,Is.EqualTo(-1));
            var c=new PracticeTurnOpsCatalog(CardCatalogLoaderTests.Load(),e);Assert.That(c.Effects.Count,Is.EqualTo(2));
        }
        [TestCase("affected_side","USSR")][TestCase("duration","Permanent")][TestCase("expiry_boundary","ActionEnd")][TestCase("repeat_effect_same_side","Stack")]
        public void Unsupported_semantics_are_rejected(string key,string value)
        {var j=JObject.Parse(Data());j["parameters"]["containment"][key]=value;Assert.Throws<ArgumentException>(()=>TurnOpsEffectLoader.LoadFlatSubset(j.ToString(),Schema()));}
        [Test] public void Loader_rejects_unverified_source_and_invalid_excluded_record()
        {
            var j=JObject.Parse(Data());j["parameters"]["red_scare_purge"]["source_refs"][0]["verification"]="Pending";Assert.Throws<ArgumentException>(()=>TurnOpsEffectLoader.LoadFlatSubset(j.ToString(),Schema()));
            j=JObject.Parse(Data());j["parameters"]["vietnam_revolts"]["modifier"]["delta"]="1";Assert.Throws<ArgumentException>(()=>TurnOpsEffectLoader.LoadFlatSubset(j.ToString(),Schema()));
        }
        [TestCase(PlayerSide.US,PlayerSide.USSR)][TestCase(PlayerSide.USSR,PlayerSide.US)]
        public void Activation_binds_fixed_US_and_controller_opponent_not_printed_affiliation(PlayerSide controller,PlayerSide opponent)
        {
            var t=Turn();var s=TurnOpsEffectState.Create(t);var e=Effects();var a=s.Activate(e[0],controller,"contain",10,t);Assert.That(a.Accepted,Is.True);Assert.That(a.Instance.AffectedSide,Is.EqualTo(PlayerSide.US));
            var b=a.After.Activate(e[1],controller,"purge",20,t);Assert.That(b.Accepted,Is.True);Assert.That(b.Instance.AffectedSide,Is.EqualTo(opponent));Assert.That(b.Instance.Controller,Is.EqualTo(controller));Assert.That(b.Instance.Turn,Is.EqualTo(1));Assert.That(b.Instance.ActivationSequence,Is.EqualTo(20));Assert.That(b.Instance.ExpiryBoundary,Is.EqualTo("ExpireAndReset"));Assert.That(s.Instances,Is.Empty);Assert.That(a.After.Instances.Count,Is.EqualTo(1));
            Assert.Throws<NotSupportedException>(()=>((IList<TurnOpsEffectInstance>)b.After.Instances).Clear());
        }
        [Test] public void Same_resolution_is_idempotent_but_changed_request_conflicts_and_repeat_requires_ruling()
        {
            var t=Turn();var e=Effects()[0];var a=TurnOpsEffectState.Create(t).Activate(e,PlayerSide.USSR,"r",12,t);var b=a.After.Activate(e,PlayerSide.USSR,"r",12,t);
            Assert.That(b.Accepted,Is.True);Assert.That(b.AlreadyApplied,Is.True);Assert.That(b.After,Is.SameAs(a.After));Assert.That(b.Instance,Is.SameAs(a.Instance));
            Assert.That(a.After.Activate(e,PlayerSide.US,"r",12,t).Error,Is.EqualTo(RuleError.CommandIdConflict));Assert.That(a.After.Activate(e,PlayerSide.USSR,"r",13,t).Error,Is.EqualTo(RuleError.CommandIdConflict));
            var repeat=a.After.Activate(e,PlayerSide.USSR,"other",13,t);Assert.That(repeat.Error,Is.EqualTo(RuleError.EventRequiresRuling));Assert.That(repeat.After,Is.Null);Assert.That(a.After.Instances.Count,Is.EqualTo(1));
        }
        [Test] public void Different_affected_sides_can_hold_purge_but_new_turn_cannot_silently_keep_or_expire_it()
        {
            var t=Turn();var e=Effects()[1];var a=TurnOpsEffectState.Create(t).Activate(e,PlayerSide.US,"a",1,t).After;var b=a.Activate(e,PlayerSide.USSR,"b",2,t);Assert.That(b.Accepted,Is.True);Assert.That(b.After.GetModifiers(t).Select(m=>m.Id).Distinct().Count(),Is.EqualTo(2));
            Assert.That(b.After.Activate(e,PlayerSide.US,"c",3,Turn(2)).Error,Is.EqualTo(RuleError.StaleContext));Assert.Throws<ArgumentException>(()=>b.After.GetModifiers(Turn(2)));Assert.That(b.After.Instances.Count,Is.EqualTo(2));
        }
        [Test] public void Contributions_aggregate_before_one_floor_and_cap_for_actual_ops_actor()
        {
            var t=Turn();var effects=Effects();var s=TurnOpsEffectState.Create(t).Activate(effects[0],PlayerSide.USSR,"a",1,t).After.Activate(effects[1],PlayerSide.USSR,"b",2,t).After;
            var cards=CardCatalogLoaderTests.Load();var policy=OpsGeneralDefinitionLoader.LoadSubset(Data(),Schema());
            foreach(var id in new[]{"card.cia_created","card.nuclear_test_ban"}) foreach(OpsValuePurpose purpose in Enum.GetValues(typeof(OpsValuePurpose)))
            {var card=cards.GetCard(new CardId(id));var q=OpsGeneralRules.QuoteCard(policy,card,PlayerSide.US,purpose,s.GetModifiers(t));Assert.That(q.ModifierTotal,Is.Zero);Assert.That(q.GeneralValue,Is.EqualTo(card.PrintedOps));Assert.That(q.AppliedModifiers.Count,Is.EqualTo(2));Assert.That(OpsGeneralRules.QuoteCard(policy,card,PlayerSide.USSR,purpose,s.GetModifiers(t)).AppliedModifiers,Is.Empty);}
        }
        [Test] public void Invalid_context_catalog_and_activation_rejected_without_partial_state()
        {
            var t=Turn();var e=Effects();var s=TurnOpsEffectState.Create(t);Assert.Throws<ArgumentNullException>(()=>TurnOpsEffectState.Create(null));
            Assert.That(s.Activate(e[0],(PlayerSide)99,"x",1,t).Error,Is.EqualTo(RuleError.InvalidCommand));Assert.That(s.Activate(e[0],PlayerSide.US," ",1,t).Error,Is.EqualTo(RuleError.InvalidCommand));Assert.That(s.Activate(null,PlayerSide.US,"x",1,t).Error,Is.EqualTo(RuleError.InvalidDefinition));Assert.That(s.Activate(e[0],PlayerSide.US,"x",-1,t).Error,Is.EqualTo(RuleError.InvalidCommand));Assert.That(s.Instances,Is.Empty);
            var cards=CardCatalogLoaderTests.Load();Assert.Throws<ArgumentException>(()=>new PracticeTurnOpsCatalog(cards,new[]{e[0],e[0]}));
            Assert.Throws<ArgumentException>(()=>new PracticeTurnOpsCatalog(cards,new[]{new TurnOpsEffectDefinition(new CardId("card.nasser"),"effect.nasser",TurnOpsAffectedSide.US,1)}));
            Assert.Throws<ArgumentException>(()=>new TurnOpsEffectDefinition(default,"effect.x",TurnOpsAffectedSide.US,1));Assert.Throws<ArgumentException>(()=>new TurnOpsEffectDefinition(e[0].Card,"effect.x",(TurnOpsAffectedSide)99,1));Assert.Throws<ArgumentException>(()=>new TurnOpsEffectDefinition(e[0].Card,"effect.x",TurnOpsAffectedSide.US,-1));
        }
    }
}
