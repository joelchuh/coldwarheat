using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using ColdWarStruggle.Core;
using ColdWarStruggle.Infrastructure;
using ColdWarStruggle.Application;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class PlayTurnOpsEventTests
    {
        internal static PracticeTurnOpsCatalog Effects(CardCatalog cards)=>new PracticeTurnOpsCatalog(cards,TurnOpsEffectTests.Effects());
        internal static GameState Ready(string card="card.containment",int level=5,int vp=0)=>PlayScoringTests.Ready(card,vp,configure:d=> {
            var f=d.Risk.Defcon;var risk=new PracticeRiskDefinition(d.Risk.Coup,new DefconDefinition(f.Map,level,f.MaximumLevel,f.NuclearLevel,f.TurnStartImprovement,f.BattlegroundCoupDegradation,f.RestrictedActions,f.BlockedRegions),d.Risk.Military,d.Risk.Victory);
            return new ActionPracticeDefinition(d.Cards,d.Ops,d.Space,d.Calendar,risk,d.Scoring,PlayInfluenceEventTests.Effects(f.Map,d.Cards),PlayDefconVpEventTests.Effects(d.Cards),Effects(d.Cards));});
        // Explicit test-only rearrangement of physical cards; does not change live effects or other state.
        internal static GameState Give(GameState s,PlayerSide side,string card)
        {
            var id=new CardId(card);var c=s.Cards;var hands=new Dictionary<PlayerSide,IReadOnlyList<CardId>>();
            foreach(var player in new[]{PlayerSide.US,PlayerSide.USSR})hands[player]=c.GetHand(player).Where(x=>!x.Equals(id)).Concat(player==side?new[]{id}:Array.Empty<CardId>()).ToArray();
            var zones=new CardZones(s.Practice.Definition.Cards,c.DrawPile.Where(x=>!x.Equals(id)),hands,c.EraReserve.Where(x=>!x.Equals(id)),c.DiscardPile.Where(x=>!x.Equals(id)),c.RemovedCards.Where(x=>!x.Equals(id)),c.ResolvingCards.Where(x=>!x.Equals(id)),c.China,c.EnteredEras);
            return PlayInfluenceEventTests.Copy(s,cards:zones);
        }
        internal static CardActionTransition Play(GameState s,string card)=>ActionPracticeRules.Plan(s,s.Practice.Actor,CardActionKind.Event,"event",new CardId(card));
        static GameState Activated(string card="card.containment",int level=5,int vp=0)
        {var r=Play(Ready(card,level,vp),card);Assert.That(r.Accepted,Is.True,r.Error.ToString());return r.NextState;}
        [TestCase("card.containment",PlayerSide.US,true)][TestCase("card.red_scare_purge",PlayerSide.US,false)]
        public void Real_event_activates_once_and_card_disposal_does_not_expire_effect(string card,PlayerSide affected,bool removed)
        {
            var before=Ready(card);var s=Play(before,card);Assert.That(s.Accepted,Is.True);var n=s.NextState;var effect=n.Practice.OpsEffects.Instances.Single();
            Assert.That(effect.Controller,Is.EqualTo(PlayerSide.USSR));Assert.That(effect.AffectedSide,Is.EqualTo(affected));Assert.That(effect.ActivationSequence,Is.EqualTo(n.EventSequence));Assert.That(n.Practice.CompletedActions,Is.EqualTo(1));Assert.That(n.Practice.Actor,Is.EqualTo(PlayerSide.US));Assert.That(n.Practice.Stage,Is.EqualTo(ActionPracticeStage.ChooseCard));Assert.That(n.Cards.RemovedCards.Contains(new CardId(card)),Is.EqualTo(removed));Assert.That(n.Cards.DiscardPile.Contains(new CardId(card)),Is.EqualTo(!removed));Assert.That(n.Cards.ResolvingCards,Is.Empty);
            Assert.That(n.Countries,Is.EquivalentTo(before.Countries));Assert.That(n.Practice.Risk,Is.SameAs(before.Practice.Risk));Assert.That(n.Practice.Space,Is.SameAs(before.Practice.Space));Pcg32RandomTests.Same(n.RandomState,before.RandomState);Assert.That(before.Practice.OpsEffects.Instances,Is.Empty);
        }
        [TestCase("card.containment","card.cia_created",2)][TestCase("card.red_scare_purge","card.cia_created",1)][TestCase("card.containment","card.nuclear_test_ban",4)]
        public void Influence_budget_uses_current_ops_and_preserves_effect_after_finish(string eventCard,string used,int budget)
        {
            var s=Give(Activated(eventCard),PlayerSide.US,used);var a=ActionPracticeRules.Plan(s,PlayerSide.US,CardActionKind.Influence,"influence",new CardId(used));Assert.That(a.Accepted,Is.True);Assert.That(a.NextState.Operations.Budget,Is.EqualTo(budget));Assert.That(a.NextState.Practice.OpsEffects,Is.SameAs(s.Practice.OpsEffects));var done=ActionPracticeRules.Plan(a.NextState,PlayerSide.US,CardActionKind.FinishInfluence,"finish");Assert.That(done.Accepted,Is.True);Assert.That(done.NextState.Practice.OpsEffects,Is.SameAs(s.Practice.OpsEffects));Assert.That(done.NextState.Practice.CompletedActions,Is.EqualTo(2));
        }
        [TestCase("card.containment",3)][TestCase("card.red_scare_purge",1)] public void Coup_strength_and_military_credit_use_same_modified_quote(string eventCard,int ops)
        {
            var s=Give(Activated(eventCard),PlayerSide.US,"card.defectors");var p=ActionPracticeRules.Plan(s,PlayerSide.US,CardActionKind.Coup,"coup",new CardId("card.defectors"),new CountryId("country.nigeria"));Assert.That(p.Accepted,Is.True);Assert.That(p.CoupReceipt.Calculation.Strength,Is.EqualTo(ops));Assert.That(p.CoupReceipt.MilitaryCredit.Amount,Is.EqualTo(ops));Assert.That(p.NextState.Practice.Risk.Military.UsEarned,Is.EqualTo(ops));Assert.That(p.NextState.Practice.OpsEffects,Is.SameAs(s.Practice.OpsEffects));
        }
        [TestCase("card.containment","card.cia_created",true)][TestCase("card.red_scare_purge","card.defectors",false)]
        public void Space_query_and_execution_agree_on_modified_minimum(string eventCard,string used,bool allowed)
        {
            var s=Give(Activated(eventCard),PlayerSide.US,used);var id=new CardId(used);var expected=allowed?RuleError.None:RuleError.SpaceAttemptUnavailable;
            Assert.That(ActionPracticeRules.CanPlay(s,PlayerSide.US,id,CardActionKind.Space),Is.EqualTo(expected));var p=ActionPracticeRules.Plan(s,PlayerSide.US,CardActionKind.Space,"space",id);Assert.That(p.Error,Is.EqualTo(expected));
            if(allowed){Assert.That(p.NextState.Practice.OpsEffects,Is.SameAs(s.Practice.OpsEffects));Assert.That(p.NextState.Practice.Space.UsAttempts,Is.EqualTo(1));}else{Assert.That(p.NextState,Is.Null);Assert.That(s.Cards.GetHand(PlayerSide.US),Does.Contain(id));}
        }
        [Test] public void Opposite_controller_purge_and_containment_coexist_and_aggregate_for_actual_actor()
        {
            var s=Give(Activated(),PlayerSide.US,"card.red_scare_purge");var p=Play(s,"card.red_scare_purge");Assert.That(p.Accepted,Is.True);s=p.NextState;
            Assert.That(s.Practice.OpsEffects.Instances.Select(x=>x.AffectedSide),Is.EquivalentTo(new[]{PlayerSide.US,PlayerSide.USSR}));
            var q=ActionPracticeRules.QuoteCard(s,PlayerSide.USSR,new CardId("card.blockade"),OpsValuePurpose.Influence);Assert.That(q.GeneralValue,Is.EqualTo(1));Assert.That(q.ModifierTotal,Is.EqualTo(-1));
            Assert.That(ActionPracticeRules.QuoteCard(s,PlayerSide.US,new CardId("card.blockade"),OpsValuePurpose.Space).GeneralValue,Is.EqualTo(2));
        }
        [TestCase(true)][TestCase(false)] public void Overflow_drops_card_and_activation_together(bool version)
        {var s=Ready();s=PlayScoringTests.Replace(s,version:version?long.MaxValue:(long?)null,sequence:version?null:(long?)long.MaxValue);var p=Play(s,"card.containment");Assert.That(p.Error,Is.EqualTo(RuleError.NumericOverflow));Assert.That(p.NextState,Is.Null);Assert.That(s.Practice.OpsEffects.Instances,Is.Empty);Assert.That(s.Cards.GetHand(PlayerSide.USSR),Does.Contain(new CardId("card.containment")));}
        [Test] public void Same_turn_duplicate_effect_is_explicitly_unresolved_and_not_stacked()
        {var s=Give(Activated(),PlayerSide.US,"card.containment");var p=Play(s,"card.containment");Assert.That(p.Error,Is.EqualTo(RuleError.EventRequiresRuling));Assert.That(p.NextState,Is.Null);Assert.That(s.Practice.OpsEffects.Instances.Count,Is.EqualTo(1));}
        [Test] public void Existing_direct_events_and_scoring_ignore_flat_ops_but_preserve_instances()
        {
            var s=Give(Activated(),PlayerSide.US,"card.nasser");var n=Play(s,"card.nasser");Assert.That(n.Accepted,Is.True);Assert.That(n.NextState.GetInfluence(new CountryId("country.egypt"),PlayerSide.USSR),Is.EqualTo(s.GetInfluence(new CountryId("country.egypt"),PlayerSide.USSR)+2));Assert.That(n.NextState.Practice.OpsEffects,Is.SameAs(s.Practice.OpsEffects));
            s=Give(n.NextState,PlayerSide.USSR,"card.nuclear_test_ban");var b=Play(s,"card.nuclear_test_ban");Assert.That(b.Accepted,Is.True);Assert.That(b.DefconVpEvent.Award,Is.EqualTo(3));Assert.That(b.NextState.Practice.OpsEffects,Is.SameAs(s.Practice.OpsEffects));
            s=Give(b.NextState,PlayerSide.US,"card.africa_scoring");var score=PlayScoringTests.Score(s,"card.africa_scoring");Assert.That(score.Accepted,Is.True);Assert.That(score.NextState.Practice.OpsEffects,Is.SameAs(s.Practice.OpsEffects));
        }
        [Test] public void Pending_choice_and_resolution_keep_effect_without_rebinding_it_to_chooser()
        {
            var s=Give(Activated(),PlayerSide.US,"card.suez_crisis");var p=Play(s,"card.suez_crisis");Assert.That(p.Accepted,Is.True);var pending=p.NextState;Assert.That(pending.Practice.OpsEffects,Is.SameAs(s.Practice.OpsEffects));var e=pending.InfluenceEvent;Assert.That(e.Owner,Is.EqualTo(PlayerSide.USSR));
            var q=InfluenceEventFlow.QueryDecision(pending,e.Owner,out var error);Assert.That(error,Is.EqualTo(RuleError.None));var remaining=q.RequiredAmount;var a=new List<InfluenceAllocation>();foreach(var cap in q.Capacities){var amount=Math.Min(cap.Amount,remaining);if(amount>0)a.Add(new InfluenceAllocation(cap.Country,amount));remaining-=amount;}
            var done=ActionPracticeRules.CompleteEventAction(InfluenceEventFlow.ResolveAllocation(pending,e.Owner,e.DecisionId,a));Assert.That(done.Accepted,Is.True);Assert.That(done.NextState.Practice.OpsEffects,Is.SameAs(s.Practice.OpsEffects));Assert.That(done.NextState.Practice.CompletedActions,Is.EqualTo(2));
        }
        [TestCase("event")][TestCase("coup")][TestCase("space")][TestCase("scoring")]
        public void Terminal_paths_preserve_active_effects_and_stop_root_completion(string mode)
        {
            var s=Activated(level:2,vp:19);CardActionTransition result=null;
            if(mode=="event"){s=Give(s,PlayerSide.US,"card.duck_and_cover");result=Play(s,"card.duck_and_cover");}
            if(mode=="coup"){s=Give(s,PlayerSide.US,"card.defectors");var countries=s.Countries.ToDictionary(x=>x.Key,x=>x.Value);countries[new CountryId("country.nigeria")]=new CountryState(0,1);s=PlayScoringTests.Replace(s,countries);result=ActionPracticeRules.Plan(s,PlayerSide.US,CardActionKind.Coup,"coup",new CardId("card.defectors"),new CountryId("country.nigeria"));}
            if(mode=="space"){s=Give(s,PlayerSide.US,"card.cia_created");for(ulong seed=0;seed<24;seed++){var input=PlayScoringTests.Replace(s,random:Pcg32Random.FromSeed(seed,54).Capture());result=ActionPracticeRules.Plan(input,PlayerSide.US,CardActionKind.Space,"space",new CardId("card.cia_created"));if(result.NextState?.Ended==true)break;}}
            if(mode=="scoring"){s=Give(s,PlayerSide.US,"card.europe_scoring");var countries=s.Countries.ToDictionary(x=>x.Key,x=>x.Value);RegionScoringTests.Control(s.Map,countries,PlayerSide.US,s.Map.GetCountriesInRegion("region.europe").Where(x=>x.Battleground).Select(x=>x.Id.Value).ToArray());s=PlayScoringTests.Replace(s,countries);result=PlayScoringTests.Score(s,"card.europe_scoring");}
            Assert.That(result.Accepted,Is.True);Assert.That(result.NextState.Ended,Is.True);Assert.That(result.NextState.Practice.OpsEffects,Is.SameAs(s.Practice.OpsEffects));Assert.That(result.NextState.Practice.CompletedActions,Is.EqualTo(1));Assert.That(result.NextState.Cards.ResolvingCards.Count,Is.EqualTo(1));
        }
        [Test] public async Task Host_retries_original_event_without_second_activation_and_rejects_changed_command()
        {
            var h=new LocalGameHost(Ready(),true);var session=h.UssrSession;var v=session.ReadView();var cmd=new CardActionCommand("activate",v.GameId,v.Version,CardActionKind.Event,new CardId("card.containment"));var result=await session.SubmitCardActionAsync(cmd);Assert.That(result.Accepted,Is.True);var after=session.ReadView();Assert.That(await session.SubmitCardActionAsync(cmd),Is.SameAs(result));Assert.That(session.ReadView().Version,Is.EqualTo(after.Version));Assert.That((await session.SubmitCardActionAsync(new CardActionCommand("activate",v.GameId,v.Version,CardActionKind.Space,cmd.Card))).Error,Is.EqualTo(RuleError.CommandIdConflict));
        }
        [Test] public void Catalog_rejects_foreign_cards_overlap_and_unsupported_runtime_event()
        {
            var s=Ready();var d=s.Practice.Definition;Assert.Throws<ArgumentException>(()=>new ActionPracticeDefinition(d.Cards,d.Ops,d.Space,d.Calendar,turnOpsEvents:Effects(CardCatalogLoaderTests.Load())));
            var overlap=new PracticeInfluenceCatalog(s.Map,d.Cards,new[]{new ClearThenControlEffect("card.containment","effect.containment",PlayerSide.US,new CountryId("country.cuba"))});Assert.Throws<ArgumentException>(()=>new ActionPracticeDefinition(d.Cards,d.Ops,d.Space,d.Calendar,influenceEvents:overlap,turnOpsEvents:d.TurnOpsEvents));
            Assert.That(Play(Ready("card.vietnam_revolts"),"card.vietnam_revolts").Error,Is.EqualTo(RuleError.UnsupportedCardAction));Assert.That(Play(PlayScoringTests.Ready("card.containment"),"card.containment").Error,Is.EqualTo(RuleError.UnsupportedCardAction));
        }
    }
}
