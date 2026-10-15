using System;
using System.Collections.Generic;
using System.Linq;
using ColdWarStruggle.Core;
using ColdWarStruggle.Application;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class DefectorsHeadlineResolutionTests
    {
        internal static CardId Id(string n)=>HeadlineFlowTests.Id(n);
        internal static ActionPracticeDefinition Bind(ActionPracticeDefinition d,DefectorsHeadlineDefinition defectors,PracticeInfluenceCatalog influences=null)
            =>new ActionPracticeDefinition(d.Cards,d.Ops,d.Space,d.Calendar,d.Risk,d.Scoring,influences??d.InfluenceEvents,d.DefconVpEvents,d.TurnOpsEvents,d.Realignment,d.China,d.TurnEndEnabled,d.NextTurnEnabled,d.HeadlineParameters,defectors);
        internal static GameState Prepared(string us="defectors",string su="fidel",bool collecting=false,int level=5,bool configured=true)
        {
            var s=HeadlineFlowTests.Prepared(us,su,enabled:collecting,level:level);var d=s.Practice.Definition;
            var definition=Bind(d,configured?DefectorsHeadlineDefinitionTests.Definition(d.Cards):null);
            return new GameState(s.Map,s.Countries,s.Operations,s.GameId,s.Version,s.SpentOps,s.EventSequence,turn:s.Turn,deployment:s.Deployment,cards:s.Cards,randomState:s.RandomState,practice:ActionPracticeState.Create(definition));
        }
        internal static GameState Ready(string us="defectors",string su="fidel",int level=5,bool configured=true)
        {
            var s=HeadlineFlowTests.Start(Prepared(us,su,level:level,configured:configured));
            var q=HeadlineSelectionState.Begin(s.Practice.Definition.Cards,s.Cards,s.Practice.Space.Track,new[]{Id(us),Id(su)});
            q=HeadlineSelectionRules.PlanSelection(q,PlayerSide.US,Id(us)).State;q=HeadlineSelectionRules.PlanSelection(q,PlayerSide.USSR,Id(su)).State;
            return PlayInfluenceEventTests.Copy(s,cards:q.Cards);
        }
        static HeadlineResolutionTransition Begin(GameState s)=>HeadlineResolutionTests.Begin(s);
        static GameState Accepted(HeadlineResolutionTransition r){Assert.That(r.Accepted,Is.True,r.Error.ToString());return r.NextState;}
        [Test] public void Only_configured_headlines_gain_the_card()
        {
            var s=Prepared();var d=s.Practice.Definition;
            Assert.That(HeadlineResolutionRules.SupportedCards(d),Has.Count.EqualTo(20));Assert.That(HeadlineResolutionRules.SupportedCards(d),Does.Contain(Id("defectors")));
            Assert.That(d.InfluenceEvents.Effects.ContainsKey(Id("defectors")),Is.False);Assert.That(HeadlineResolutionRules.SupportedCards(Bind(d,null)),Has.No.Member(Id("defectors")));
        }
        [Test] public void Foreign_catalog_definition_is_rejected()
        {var d=Prepared().Practice.Definition;var foreign=Prepared().Practice.Definition;Assert.Throws<ArgumentException>(()=>Bind(d,DefectorsHeadlineDefinitionTests.Definition(foreign.Cards)));}
        [Test] public void Duplicate_registered_handler_is_rejected()
        {
            var s=Prepared();var d=s.Practice.Definition;
            var influence=new PracticeInfluenceCatalog(s.Map,d.Cards,new[]{new ClearThenControlEffect("card.defectors","effect.defectors",PlayerSide.US,new CountryId("country.cuba"))});
            Assert.Throws<ArgumentException>(()=>Bind(d,d.HeadlineDefectors,influence));
        }
        [TestCase("red_scare_purge")][TestCase("containment")][TestCase("fidel")][TestCase("europe_scoring")][TestCase("asia_scoring")][TestCase("duck_and_cover")][TestCase("decolonization")]
        public void US_cancels_exact_opponent_without_any_effect_or_normal_action(string target)
        {
            var s=Ready(su:target,level:2);var r=Begin(s);var n=Accepted(r);HeadlineResolutionTests.Completed(s,n);
            Assert.That(n.Practice.Headline.OrderStrategy,Is.EqualTo(HeadlineOrderStrategy.USSelectedDefectorsFirst));
            Assert.That(n.Practice.Headline.Order[0].PlayedBy,Is.EqualTo(PlayerSide.US));Assert.That(r.Steps,Has.Count.EqualTo(2));
            Assert.That(n.Practice.Headline.CompletedRoots.Select(x=>x.Outcome),Is.EqualTo(new[]{HeadlineRootOutcome.Resolved,HeadlineRootOutcome.Cancelled}));
            Assert.That(n.Practice.Headline.CompletedRoots[1].CancellationSource,Is.EqualTo(Id("defectors")));
            Assert.That(n.Cards.DiscardPile,Is.EqualTo(new[]{Id("defectors"),Id(target)}));Assert.That(n.Cards.RemovedCards,Is.Empty);
            Assert.That(n.Countries,Is.EquivalentTo(s.Countries));Assert.That(n.Practice.VpForUS,Is.EqualTo(s.Practice.VpForUS));Assert.That(n.Practice.Risk,Is.SameAs(s.Practice.Risk));
            Assert.That(n.Practice.OpsEffects,Is.SameAs(s.Practice.OpsEffects));Assert.That(n.Ended,Is.False);
            foreach(var step in r.Steps){Assert.That(step.Result.EventTransition,Is.Null);Assert.That(step.Result.ScoringResult,Is.Null);Assert.That(step.Result.DefconVpEvent,Is.Null);Assert.That(step.Result.ActivatedOpsEffect,Is.Null);}
        }
        [TestCase("containment",PlayerSide.US)][TestCase("fidel",PlayerSide.US)][TestCase("nasser",PlayerSide.USSR)]
        public void USSR_self_cancels_in_ordinary_order_and_US_event_occurs(string us,PlayerSide first)
        {
            var s=Ready(us,"defectors");var r=Begin(s);var n=Accepted(r);HeadlineResolutionTests.Completed(s,n);
            Assert.That(r.Steps[0].PlayedBy,Is.EqualTo(first));Assert.That(n.Practice.Headline.OrderStrategy,Is.EqualTo(HeadlineOrderStrategy.OrdinaryPrintedValue));
            var roots=n.Practice.Headline.CompletedRoots;Assert.That(roots.Single(x=>x.PlayedBy==PlayerSide.US).Outcome,Is.EqualTo(HeadlineRootOutcome.Resolved));
            var su=roots.Single(x=>x.PlayedBy==PlayerSide.USSR);Assert.That(su.Outcome,Is.EqualTo(HeadlineRootOutcome.Cancelled));Assert.That(su.CancellationSource,Is.EqualTo(Id("defectors")));
            Assert.That(n.Practice.VpForUS,Is.Zero);Assert.That(n.Cards.DiscardPile,Is.EqualTo(new[]{Id("defectors")}));Assert.That(n.Cards.RemovedCards,Does.Contain(Id(us)));
            if(us=="containment")Assert.That(n.Practice.OpsEffects.Instances,Has.Count.EqualTo(1));else Assert.That(n.Countries,Is.Not.EquivalentTo(s.Countries));
        }
        [Test] public void Waiting_US_event_keeps_SU_self_cancellation_pending()
        {
            var s=Ready("decolonization","defectors");var r=Begin(s);var n=Accepted(r);
            Assert.That(n.InfluenceEvent.Stage,Is.EqualTo(InfluenceEventStage.AwaitingChoice));Assert.That(n.Practice.Headline.CompletedCount,Is.Zero);
            Assert.That(n.Cards.GetHeadline(PlayerSide.USSR),Is.EqualTo(Id("defectors")));Assert.That(n.Cards.DiscardPile,Is.Empty);
            Assert.That(HeadlineResolutionRules.Advance(n).Error,Is.EqualTo(RuleError.ActionNotReady));
        }
        [Test] public void Terminal_US_event_preserves_pending_SU_entity_without_cancellation()
        {
            var s=Ready("duck_and_cover","defectors",level:2);var n=Accepted(Begin(s));
            Assert.That(n.Ended,Is.True);Assert.That(n.Practice.Risk.Result.Winner,Is.EqualTo(PlayerSide.USSR));Assert.That(n.Practice.Headline.CompletedRoots,Is.Empty);
            Assert.That(n.Cards.GetHeadline(PlayerSide.USSR),Is.EqualTo(Id("defectors")));Assert.That(n.Cards.ResolvingCards,Is.EqualTo(new[]{Id("duck_and_cover")}));Assert.That(n.Cards.DiscardPile,Is.Empty);
        }
        [TestCase(true)][TestCase(false)] public void Overflow_during_target_cancellation_returns_no_prefix(bool version)
        {
            var s=Ready();s=PlayInfluenceEventTests.Copy(s,version:version?long.MaxValue-2:s.Version,sequence:version?s.EventSequence:long.MaxValue-2);
            var r=Begin(s);Assert.That(r.Error,Is.EqualTo(RuleError.NumericOverflow));Assert.That(r.NextState,Is.Null);Assert.That(r.Steps,Is.Empty);
            Assert.That(s.Cards.HeadlineReserved,Has.Count.EqualTo(2));Assert.That(s.Cards.DiscardPile,Is.Empty);Assert.That(s.Practice.Headline,Is.Null);
        }
        [TestCase("blockade",true)][TestCase("fidel",false)] public void Unknown_target_or_disabled_binding_never_bypasses_coverage(string target,bool configured)
        {
            var s=Ready(su:target,configured:configured);var r=Begin(s);Assert.That(r.Error,Is.EqualTo(RuleError.UnsupportedCardAction));Assert.That(r.NextState,Is.Null);Assert.That(r.Steps,Is.Empty);
        }
        [Test] public void Collection_advertises_and_executes_the_bound_headline()
        {
            var s=HeadlineFlowTests.Start(Prepared(collecting:true));Assert.That(HeadlineSelectionView.Create(s.Practice.HeadlineSelection,PlayerSide.US).Candidates,Does.Contain(Id("defectors")));
            s=HeadlineFlowTests.Choose(s,PlayerSide.US,"defectors");var r=HeadlineFlowRules.Submit(s,PlayerSide.USSR,Id("fidel"));var n=Accepted(r);
            Assert.That(n.Practice.Headline.CompletedCount,Is.EqualTo(2));Assert.That(n.Practice.Actor,Is.EqualTo(PlayerSide.USSR));Assert.That(n.Practice.CompletedActions,Is.Zero);
            Assert.That(Begin(n).Error,Is.EqualTo(RuleError.ActionNotReady));Assert.That(HeadlineResolutionRules.Advance(n).Error,Is.EqualTo(RuleError.ActionNotReady));
        }
    }
}
