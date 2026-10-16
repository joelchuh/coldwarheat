using System;
using System.Collections.Generic;
using System.Linq;
using ColdWarStruggle.Core;
using ColdWarStruggle.Infrastructure;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class FiveYearPlanPlayTests
    {
        internal static CardId Id(string n)=>new CardId("card."+n);
        internal static CountryId C(string n)=>new CountryId("country."+n);
        internal static ActionPracticeDefinition Bind(ActionPracticeDefinition d)
        {
            var influence=d.InfluenceEvents??PlayInfluenceEventTests.Effects(d.Risk.Defcon.Map,d.Cards);
            if(!influence.Effects.ContainsKey(Id("independent_reds")))influence=new PracticeInfluenceCatalog(influence.Map,d.Cards,influence.Effects.Values.Append(
                IndependentRedsCatalogLoader.Load(IndependentRedsCatalogTests.Data(),IndependentRedsCatalogTests.Schema(),influence.Map,BudgetedRemovalCatalogTests.KnownEffects)));
            return new ActionPracticeDefinition(d.Cards,d.Ops,d.Space,d.Calendar,d.Risk,d.Scoring,influence,d.DefconVpEvents??PlayDefconVpEventTests.Effects(d.Cards),d.TurnOpsEvents??PlayTurnOpsEventTests.Effects(d.Cards),d.Realignment,d.China,d.TurnEndEnabled,d.NextTurnEnabled,d.HeadlineParameters,d.HeadlineDefectors,d.FinalScoring,d.Blockade,
                FiveYearPlanDefinitionLoader.Load(BlockadeDefinitionTests.Data(),BlockadeDefinitionTests.Schema(),d.Cards,BudgetedRemovalCatalogTests.KnownEffects));
        }
        internal static GameState Ready(string child="independent_reds",PlayerSide actor=PlayerSide.USSR,int level=5,int vp=0,bool emptyBoard=false)
        {
            var s=PlayScoringTests.Ready("card.five_year_plan",vp,configure:d=>
            {
                var f=d.Risk.Defcon;var risk=new PracticeRiskDefinition(d.Risk.Coup,new DefconDefinition(f.Map,level,f.MaximumLevel,f.NuclearLevel,f.TurnStartImprovement,f.BattlegroundCoupDegradation,f.RestrictedActions,f.BlockedRegions),d.Risk.Military,d.Risk.Victory);
                return Bind(new ActionPracticeDefinition(d.Cards,d.Ops,d.Space,d.Calendar,risk,d.Scoring));
            });
            var su=new List<CardId>();if(child!=null)su.Add(Id(child));if(actor==PlayerSide.USSR)su.Add(Id("five_year_plan"));else su.Add(Id("olympic_games"));
            var us=actor==PlayerSide.US?new[]{Id("five_year_plan")}:Array.Empty<CardId>();
            var hands=new Dictionary<PlayerSide,IReadOnlyList<CardId>>{{PlayerSide.US,us},{PlayerSide.USSR,su}};var used=su.Concat(us).ToHashSet();
            var cards=new CardZones(s.Cards.Catalog,s.Cards.Catalog.Cards.Where(c=>c.Kind!=CardKind.China && !used.Contains(c.Id)).Select(c=>c.Id),hands,Array.Empty<CardId>(),Array.Empty<CardId>(),Array.Empty<CardId>(),Array.Empty<CardId>(),s.Cards.China,s.Cards.EnteredEras);
            var board=RegionScoringTests.Empty(s.Map);if(!emptyBoard){board[C("yugoslavia")]=new CountryState(1,3);board[C("france")]=new CountryState(0,1);}
            s=PlayInfluenceEventTests.Copy(s,board,cards:cards);
            if(actor==PlayerSide.US)
            {
                var prep=ActionPracticeRules.Plan(s,PlayerSide.USSR,CardActionKind.Influence,"prep",Id("olympic_games"));Assert.That(prep.Accepted,Is.True,prep.Error.ToString());
                var done=ActionPracticeRules.Plan(prep.NextState,PlayerSide.USSR,CardActionKind.FinishInfluence,"prep-finish");Assert.That(done.Accepted,Is.True);s=done.NextState;
            }
            return s;
        }
        internal static CardActionTransition Begin(GameState s)=>ActionPracticeRules.Plan(s,s.Practice.Actor,CardActionKind.Event,"five-year",Id("five_year_plan"));
        internal static InfluenceEventTransition Choose(GameState s,string country="yugoslavia")=>ActionPracticeRules.CompleteEventAction(InfluenceEventFlow.Resolve(s,PlayerSide.US,s.InfluenceEvent.DecisionId,new[]{C(country)}));
        static void Completed(GameState before,GameState after)
        {
            Assert.That(after.Practice.CompletedActions,Is.EqualTo(before.Practice.CompletedActions+1));Assert.That(after.Practice.ActiveCard,Is.Null);Assert.That(after.Practice.FiveYearPlan,Is.Null);
            Assert.That(after.Cards.ResolvingCards,Is.Empty);Assert.That(after.Cards.DiscardPile,Has.Member(Id("five_year_plan")));Assert.That(after.InfluenceEvent,Is.Null);Assert.That(after.Operations.Budget,Is.Zero);
        }
        [TestCase(PlayerSide.US)][TestCase(PlayerSide.USSR)] public void Empty_hand_finishes_one_root_without_random(PlayerSide actor)
        {
            var s=Ready(null,actor);var r=Begin(s);Assert.That(r.Accepted,Is.True,r.Error.ToString());Completed(s,r.NextState);Pcg32RandomTests.Same(s.RandomState,r.NextState.RandomState);
            Assert.That(r.FiveYearPlanResult.Outcome,Is.EqualTo(FiveYearPlanOutcome.NoCard));Assert.That(r.FiveYearPlanResult.SelectedCard,Is.Null);
        }
        [TestCase("asia_scoring",PlayerSide.US)][TestCase("asia_scoring",PlayerSide.USSR)][TestCase("olympic_games",PlayerSide.USSR)][TestCase("fidel",PlayerSide.USSR)]
        public void Non_US_draw_only_discards_without_event_score_or_extra_action(string child,PlayerSide actor)
        {
            var s=Ready(child,actor);var r=Begin(s);Assert.That(r.Accepted,Is.True,r.Error.ToString());var n=r.NextState;Completed(s,n);
            Assert.That(n.Cards.DiscardPile,Has.Member(Id(child)));Assert.That(n.Cards.RemovedCards,Is.Empty);Assert.That(n.Practice.VpForUS,Is.EqualTo(s.Practice.VpForUS));Assert.That(n.Countries,Is.EquivalentTo(s.Countries));
            Assert.That(n.RandomState.DrawCount,Is.EqualTo(s.RandomState.DrawCount+1));Assert.That(r.FiveYearPlanResult.SelectedCard,Is.EqualTo(Id(child)));Assert.That(r.FiveYearPlanResult.Outcome,Is.EqualTo(FiveYearPlanOutcome.Discarded));Assert.That(r.EventTransition,Is.Null);
        }
        [TestCase(PlayerSide.US)][TestCase(PlayerSide.USSR)] public void US_child_choice_keeps_parent_actor_card_and_random_then_returns_once(PlayerSide actor)
        {
            var s=Ready(actor:actor);var r=Begin(s);Assert.That(r.Accepted,Is.True,r.Error.ToString());var p=r.NextState;
            Assert.That(p.Practice.Actor,Is.EqualTo(actor));Assert.That(p.Practice.ActiveCard,Is.EqualTo(Id("five_year_plan")));Assert.That(p.Practice.CompletedActions,Is.EqualTo(s.Practice.CompletedActions));
            Assert.That(p.Cards.ResolvingCards,Is.EqualTo(new[]{Id("five_year_plan"),Id("independent_reds")}));Assert.That(p.InfluenceEvent.Owner,Is.EqualTo(PlayerSide.US));Assert.That(p.InfluenceEvent.PhasingPlayer,Is.EqualTo(actor));Assert.That(p.Practice.FiveYearPlan.Selection.SelectedCard,Is.EqualTo(Id("independent_reds")));
            Assert.That(p.InfluenceEvent.Id,Is.EqualTo(p.Practice.FiveYearPlan.ChildResolutionId));Assert.That(p.InfluenceEvent.Id,Is.Not.EqualTo(p.Practice.FiveYearPlan.RootResolutionId));Assert.That(r.FiveYearPlanResult.Outcome,Is.EqualTo(FiveYearPlanOutcome.AwaitingUsEvent));
            var done=Choose(p);Assert.That(done.Accepted,Is.True,done.Error.ToString());Completed(s,done.NextState);Assert.That(done.NextState.GetInfluence(C("yugoslavia"),PlayerSide.US),Is.EqualTo(3));Pcg32RandomTests.Same(p.RandomState,done.NextState.RandomState);
            var printed=s.Cards.Catalog.GetCard(Id("independent_reds"));Assert.That(printed.RemoveAfterResolvedEvent?done.NextState.Cards.RemovedCards:done.NextState.Cards.DiscardPile,Has.Member(printed.Id));Assert.That(done.FiveYearPlanResult.Outcome,Is.EqualTo(FiveYearPlanOutcome.UsEventResolved));
        }
        [Test] public void US_no_candidate_event_still_happens_and_disposes_star()
        {
            var s=Ready("truman_doctrine",emptyBoard:true);var board=s.Countries.ToDictionary(x=>x.Key,x=>x.Value);
            foreach(var country in s.Map.GetCountriesInRegion("region.europe"))board[country.Id]=new CountryState(country.Stability,0);
            s=PlayInfluenceEventTests.Copy(s,board);var r=Begin(s);Assert.That(r.Accepted,Is.True,r.Error.ToString());Completed(s,r.NextState);
            Assert.That(r.NextState.Cards.RemovedCards,Has.Member(Id("truman_doctrine")));Assert.That(r.FiveYearPlanResult.Outcome,Is.EqualTo(FiveYearPlanOutcome.UsEventResolved));Assert.That(r.EventTransition.NextState.InfluenceEvent.Outcome,Is.EqualTo(InfluenceEventStatus.ResolvedNoChange));
        }
        [Test] public void Truman_zero_influence_still_requires_a_legal_uncontrolled_country_choice()
        {
            var s=Ready("truman_doctrine",emptyBoard:true);var r=Begin(s);Assert.That(r.Accepted,Is.True);Assert.That(r.NextState.InfluenceEvent.Stage,Is.EqualTo(InfluenceEventStage.AwaitingChoice));
            var done=Choose(r.NextState,"france");Assert.That(done.Accepted,Is.True,done.Error.ToString());Completed(s,done.NextState);Assert.That(done.Changes,Is.Empty);Assert.That(done.NextState.Cards.RemovedCards,Has.Member(Id("truman_doctrine")));
        }
        [Test] public void Containment_child_activates_for_US_with_child_id_without_OPS()
        {
            var s=Ready("containment");var r=Begin(s);Assert.That(r.Accepted,Is.True,r.Error.ToString());Completed(s,r.NextState);
            Assert.That(r.ActivatedOpsEffect.Controller,Is.EqualTo(PlayerSide.US));Assert.That(r.ActivatedOpsEffect.ResolutionId,Does.Contain(".child."));Assert.That(r.NextState.Cards.RemovedCards,Has.Member(Id("containment")));Assert.That(r.NextState.Practice.OpsEffects.Instances.Count,Is.EqualTo(1));Assert.That(r.FiveYearPlanResult.Outcome,Is.EqualTo(FiveYearPlanOutcome.UsEventResolved));
        }
        [Test] public void Duck_child_awards_US_and_completes_parent_after_track_change()
        {
            var s=Ready("duck_and_cover");var r=Begin(s);Assert.That(r.Accepted,Is.True,r.Error.ToString());Completed(s,r.NextState);
            Assert.That(r.NextState.Practice.Risk.Defcon.Level,Is.EqualTo(4));Assert.That(r.NextState.Practice.VpForUS,Is.EqualTo(1));Assert.That(r.DefconVpEvent.AwardSide,Is.EqualTo(PlayerSide.US));Assert.That(r.NextState.Cards.DiscardPile,Has.Member(Id("duck_and_cover")));
        }
        [TestCase(PlayerSide.US,2,0)][TestCase(PlayerSide.USSR,2,0)][TestCase(PlayerSide.USSR,5,19)]
        public void Terminal_child_keeps_parent_audit_and_original_nuclear_responsibility(PlayerSide actor,int level,int vp)
        {
            var s=Ready("duck_and_cover",actor,level,vp);var r=Begin(s);Assert.That(r.Accepted,Is.True,r.Error.ToString());var n=r.NextState;
            Assert.That(n.Ended,Is.True);Assert.That(n.Practice.CompletedActions,Is.EqualTo(s.Practice.CompletedActions));Assert.That(n.Practice.ActiveCard,Is.EqualTo(Id("five_year_plan")));Assert.That(n.Cards.ResolvingCards,Is.EqualTo(new[]{Id("five_year_plan"),Id("duck_and_cover")}));
            Assert.That(n.Practice.Risk.Result.Winner,Is.EqualTo(level==2?(actor==PlayerSide.US?PlayerSide.USSR:PlayerSide.US):PlayerSide.US));Assert.That(r.FiveYearPlanResult.Outcome,Is.EqualTo(FiveYearPlanOutcome.Interrupted));Assert.That(Begin(n).Error,Is.EqualTo(RuleError.GameAlreadyEnded));
        }
        [Test] public void Unknown_US_handler_rejects_before_random_or_parent_movement()
        {
            var s=Ready("marshall_plan");var r=Begin(s);Assert.That(r.Error,Is.EqualTo(RuleError.UnsupportedCardAction));Assert.That(r.NextState,Is.Null);Assert.That(s.Cards.ResolvingCards,Is.Empty);Assert.That(s.Cards.GetHand(PlayerSide.USSR),Has.Member(Id("five_year_plan")));
        }
        [TestCase(0)][TestCase(1)][TestCase(2)][TestCase(3)] public void Version_overflow_never_publishes_random_child_or_parent_partial_work(int room)
        {
            var s=PlayScoringTests.Replace(Ready("containment"),version:long.MaxValue-room);var r=Begin(s);Assert.That(r.Error,Is.EqualTo(RuleError.NumericOverflow));Assert.That(r.NextState,Is.Null);Assert.That(s.Cards.ResolvingCards,Is.Empty);Assert.That(s.Practice.OpsEffects.Instances,Is.Empty);
        }
        [Test] public void Pending_child_rejects_wrong_owner_stale_choice_and_other_card_actions()
        {
            var r=Begin(Ready());Assert.That(r.Accepted,Is.True,r.Error.ToString());var s=r.NextState;
            Assert.That(InfluenceEventFlow.Resolve(s,PlayerSide.USSR,s.InfluenceEvent.DecisionId,new[]{C("yugoslavia")}).Error,Is.EqualTo(RuleError.WrongActor));
            var stale=PlayInfluenceEventTests.Copy(s,version:s.Version+1);Assert.That(Choose(stale).Error,Is.EqualTo(RuleError.StaleDecision));Assert.That(Begin(s).Error,Is.EqualTo(RuleError.EventInProgress));
            Assert.That(ActionPracticeRules.Plan(s,s.Practice.Actor,CardActionKind.FinishInfluence,"bypass").Accepted,Is.False);Assert.That(s.Cards.ResolvingCards.Count,Is.EqualTo(2));
        }
        [Test] public void Foreign_catalog_definition_is_rejected_at_registration()
        {
            var d=Ready().Practice.Definition;var foreign=FiveYearPlanDefinitionTests.Load();
            Assert.That(()=>new ActionPracticeDefinition(d.Cards,d.Ops,d.Space,d.Calendar,d.Risk,d.Scoring,d.InfluenceEvents,d.DefconVpEvents,d.TurnOpsEvents,fiveYearPlan:foreign),Throws.InstanceOf<ArgumentException>());
        }
    }
}
