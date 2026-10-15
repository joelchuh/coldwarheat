using System;
using System.Collections.Generic;
using System.Linq;
using ColdWarStruggle.Core;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class HeadlineChoiceTests
    {
        internal static InfluenceEventTransition ResolveValid(GameState pending)
        {
            var e=pending.InfluenceEvent;var q=InfluenceEventFlow.QueryDecision(pending,e.Owner,out var error);Assert.That(error,Is.EqualTo(RuleError.None));
            if(q.Kind!=InfluenceDecisionKind.Allocation)return InfluenceEventFlow.Resolve(pending,e.Owner,e.DecisionId,q.LegalCountries.Take(q.RequiredTargets).ToArray());
            var remaining=q.RequiredAmount;var allocation=new List<InfluenceAllocation>();
            foreach(var cap in q.Capacities){var n=Math.Min(remaining,cap.Amount);if(n>0)allocation.Add(new InfluenceAllocation(cap.Country,n));remaining-=n;}
            Assert.That(remaining,Is.Zero);return InfluenceEventFlow.ResolveAllocation(pending,e.Owner,e.DecisionId,allocation);
        }
        [TestCase("truman_doctrine")][TestCase("decolonization")][TestCase("socialist_governments")][TestCase("suez_crisis")][TestCase("east_european_unrest")]
        public void Each_choice_handler_finishes_the_headline_then_continues_without_a_normal_action(string card)
        {
            var source=HeadlineResolutionTests.Ready(card,"nasser",empty:false);var pending=HeadlineResolutionTests.Begin(source).NextState;
            Assert.That(pending.InfluenceEvent.Stage,Is.EqualTo(InfluenceEventStage.AwaitingChoice));Assert.That(pending.Practice.Actor,Is.EqualTo(PlayerSide.US));
            var raw=ResolveValid(pending);Assert.That(raw.Accepted,Is.True);var result=HeadlineResolutionRules.CompleteChoice(raw);Assert.That(result.Accepted,Is.True,result.Error.ToString());
            HeadlineResolutionTests.Completed(source,result.NextState);Assert.That(result.Steps,Has.Count.EqualTo(2));Assert.That(result.Steps[0].Card,Is.EqualTo(HeadlineResolutionTests.Id(card)));
            Assert.That(result.Steps[0].Result.EventTransition,Is.SameAs(raw));Assert.That(result.Steps[0].PlayedBy,Is.EqualTo(PlayerSide.US));Assert.That(result.Steps[1].Card,Is.EqualTo(HeadlineResolutionTests.Id("nasser")));
            Assert.That(pending.Cards.ResolvingCards,Does.Contain(HeadlineResolutionTests.Id(card)));Assert.That(pending.Practice.Headline.CompletedCount,Is.Zero);
            var printed=source.Practice.Definition.Cards.GetCard(HeadlineResolutionTests.Id(card));Assert.That(printed.RemoveAfterResolvedEvent?result.NextState.Cards.RemovedCards:result.NextState.Cards.DiscardPile,Does.Contain(printed.Id));
        }
        [Test] public void Consecutive_choices_switch_beneficiaries_without_switching_or_consuming_ordinary_slots()
        {
            var source=HeadlineResolutionTests.Ready("decolonization","truman_doctrine",empty:false);var first=HeadlineResolutionTests.Begin(source).NextState;
            Assert.That(first.InfluenceEvent.Owner,Is.EqualTo(PlayerSide.USSR));Assert.That(first.Practice.Actor,Is.EqualTo(PlayerSide.US));
            var r=HeadlineResolutionRules.CompleteChoice(ResolveValid(first));Assert.That(r.Accepted,Is.True);var second=r.NextState;
            Assert.That(second.InfluenceEvent.Owner,Is.EqualTo(PlayerSide.US));Assert.That(second.Practice.Actor,Is.EqualTo(PlayerSide.USSR));Assert.That(second.Practice.CompletedActions,Is.Zero);Assert.That(second.Practice.Headline.CompletedCount,Is.EqualTo(1));
            Assert.That(second.Cards.HeadlineReserved,Is.Empty);Assert.That(second.Cards.ResolvingCards,Is.EqualTo(new[]{HeadlineResolutionTests.Id("truman_doctrine")}));Assert.That(second.Cards.DiscardPile,Does.Contain(HeadlineResolutionTests.Id("decolonization")));
            Assert.That(second.InfluenceEvent.DecisionId,Is.Not.EqualTo(first.InfluenceEvent.DecisionId));Assert.That(InfluenceEventFlow.Resolve(second,second.InfluenceEvent.Owner,first.InfluenceEvent.DecisionId,Array.Empty<CountryId>()).Error,Is.EqualTo(RuleError.StaleDecision));
            var done=HeadlineResolutionRules.CompleteChoice(ResolveValid(second));Assert.That(done.Accepted,Is.True);Assert.That(done.Steps,Has.Count.EqualTo(1));HeadlineResolutionTests.Completed(source,done.NextState);
            var repeat=InfluenceEventFlow.Resolve(done.NextState,PlayerSide.US,second.InfluenceEvent.DecisionId,Array.Empty<CountryId>());Assert.That(repeat.Accepted,Is.False);Assert.That(HeadlineResolutionRules.CompleteChoice(repeat).NextState,Is.Null);
        }
        [TestCase("wrong-owner")][TestCase("old-decision")][TestCase("too-few-targets")][TestCase("wrong-kind")]
        public void Rejected_choice_preserves_error_without_advancing_or_disposing(string invalid)
        {
            var pending=HeadlineResolutionTests.Begin(HeadlineResolutionTests.Ready("decolonization","fidel")).NextState;var e=pending.InfluenceEvent;
            var q=InfluenceEventFlow.QueryDecision(pending,e.Owner,out _);InfluenceEventTransition raw;
            switch(invalid)
            {
                case "wrong-owner":raw=InfluenceEventFlow.Resolve(pending,PlayerSide.US,e.DecisionId,q.LegalCountries.Take(q.RequiredTargets).ToArray());break;
                case "old-decision":raw=InfluenceEventFlow.Resolve(pending,e.Owner,"old",q.LegalCountries.Take(q.RequiredTargets).ToArray());break;
                case "wrong-kind":raw=InfluenceEventFlow.ResolveAllocation(pending,e.Owner,e.DecisionId,Array.Empty<InfluenceAllocation>());break;
                default:raw=InfluenceEventFlow.Resolve(pending,e.Owner,e.DecisionId,Array.Empty<CountryId>());break;
            }
            Assert.That(raw.Accepted,Is.False);var r=HeadlineResolutionRules.CompleteChoice(raw);Assert.That(r.Error,Is.EqualTo(raw.Error));Assert.That(r.NextState,Is.Null);Assert.That(r.Steps,Is.Empty);
            Assert.That(pending.Practice.Headline.CompletedCount,Is.Zero);Assert.That(pending.Cards.ResolvingCards,Does.Contain(HeadlineResolutionTests.Id("decolonization")));
        }
        [Test] public void Already_published_first_headline_remains_when_second_choice_is_rejected()
        {
            var source=HeadlineResolutionTests.Ready("fidel","truman_doctrine",empty:false);var pending=HeadlineResolutionTests.Begin(source).NextState;var e=pending.InfluenceEvent;var cuba=pending.GetInfluence(PlayInfluenceEventTests.C("cuba"),PlayerSide.USSR);
            var r=HeadlineResolutionRules.CompleteChoice(InfluenceEventFlow.Resolve(pending,PlayerSide.USSR,e.DecisionId,Array.Empty<CountryId>()));Assert.That(r.Accepted,Is.False);
            Assert.That(pending.Practice.Headline.CompletedCount,Is.EqualTo(1));Assert.That(pending.Cards.RemovedCards,Does.Contain(HeadlineResolutionTests.Id("fidel")));
            var done=HeadlineResolutionRules.CompleteChoice(ResolveValid(pending));Assert.That(done.Accepted,Is.True);HeadlineResolutionTests.Completed(source,done.NextState);Assert.That(done.NextState.GetInfluence(PlayInfluenceEventTests.C("cuba"),PlayerSide.USSR),Is.EqualTo(cuba));
        }
        [Test] public void Later_effect_error_discards_the_current_choice_draft_but_keeps_the_original_waiting_snapshot()
        {
            var source=HeadlineResolutionTests.Ready("decolonization","nasser");var board=source.Countries.ToDictionary(x=>x.Key,x=>x.Value);board[PlayInfluenceEventTests.C("egypt")]=new CountryState(0,int.MaxValue);source=PlayInfluenceEventTests.Copy(source,board);
            var pending=HeadlineResolutionTests.Begin(source).NextState;var raw=ResolveValid(pending);Assert.That(raw.Accepted,Is.True);Assert.That(raw.Changes,Is.Not.Empty);
            var r=HeadlineResolutionRules.CompleteChoice(raw);Assert.That(r.Error,Is.EqualTo(RuleError.NumericOverflow));Assert.That(r.NextState,Is.Null);Assert.That(r.Steps,Is.Empty);
            Assert.That(pending.Practice.Headline.CompletedCount,Is.Zero);Assert.That(pending.Cards.ResolvingCards,Does.Contain(HeadlineResolutionTests.Id("decolonization")));Assert.That(pending.Cards.GetHeadline(PlayerSide.USSR),Is.EqualTo(HeadlineResolutionTests.Id("nasser")));
            Assert.That(pending.Countries,Is.EquivalentTo(source.Countries));Assert.That(pending.Cards.DiscardPile,Is.Empty);
        }
        [Test] public void Choice_finalization_overflow_does_not_publish_only_the_influence_result()
        {
            var source=PlayInfluenceEventTests.Copy(HeadlineResolutionTests.Ready("decolonization","nasser"),version:long.MaxValue-4);
            var pending=HeadlineResolutionTests.Begin(source).NextState;var raw=ResolveValid(pending);Assert.That(raw.Accepted,Is.True);Assert.That(raw.NextState.Version,Is.EqualTo(long.MaxValue));
            var r=HeadlineResolutionRules.CompleteChoice(raw);Assert.That(r.Error,Is.EqualTo(RuleError.NumericOverflow));Assert.That(r.NextState,Is.Null);Assert.That(pending.Practice.Headline.CompletedCount,Is.Zero);
        }
        [Test] public void Second_headline_can_end_the_game_after_a_valid_first_choice()
        {
            var source=HeadlineResolutionTests.Ready("socialist_governments","duck_and_cover",level:2,empty:false);var pending=HeadlineResolutionTests.Begin(source).NextState;Assert.That(pending.InfluenceEvent,Is.Not.Null);
            var r=HeadlineResolutionRules.CompleteChoice(ResolveValid(pending));Assert.That(r.Accepted,Is.True);var n=r.NextState;
            Assert.That(n.Ended,Is.True);Assert.That(n.Practice.Risk.Result.Winner,Is.EqualTo(PlayerSide.US));Assert.That(n.Practice.Headline.CompletedCount,Is.EqualTo(1));Assert.That(n.Practice.CompletedActions,Is.Zero);
            Assert.That(n.Cards.DiscardPile,Does.Contain(HeadlineResolutionTests.Id("socialist_governments")));Assert.That(n.Cards.ResolvingCards,Does.Contain(HeadlineResolutionTests.Id("duck_and_cover")));Assert.That(r.Steps,Has.Count.EqualTo(2));
        }
        [Test] public void Scoring_after_a_choice_uses_the_committed_country_selection()
        {
            var source=HeadlineResolutionTests.Ready("decolonization","africa_scoring");var pending=HeadlineResolutionTests.Begin(source).NextState;var e=pending.InfluenceEvent;var q=InfluenceEventFlow.QueryDecision(pending,e.Owner,out _);
            var africa=source.Map.GetCountriesInRegion("region.africa").Select(c=>c.Id).ToHashSet();var targets=q.LegalCountries.Where(africa.Contains).Take(q.RequiredTargets).ToArray();
            var raw=InfluenceEventFlow.Resolve(pending,e.Owner,e.DecisionId,targets);Assert.That(raw.Accepted,Is.True);var r=HeadlineResolutionRules.CompleteChoice(raw);Assert.That(r.Accepted,Is.True);HeadlineResolutionTests.Completed(source,r.NextState);
            Assert.That(r.Steps[1].Result.ScoringResult.NetVpForUS,Is.LessThan(0));Assert.That(r.NextState.Practice.VpForUS,Is.EqualTo(r.Steps[1].Result.ScoringResult.NetVpForUS));
        }
        [Test] public void Null_pending_or_ordinary_transitions_cannot_be_finalized_as_a_headline_choice()
        {
            Assert.That(HeadlineResolutionRules.CompleteChoice(null).Error,Is.EqualTo(RuleError.InvalidCommand));
            var begun=HeadlineResolutionTests.Begin(HeadlineResolutionTests.Ready("decolonization","fidel"));Assert.That(HeadlineResolutionRules.CompleteChoice(begun.Steps[0].Result.EventTransition).Error,Is.EqualTo(RuleError.ActionNotReady));
            var ordinary=PlayInfluenceEventTests.Play(PlayInfluenceEventTests.Ready("card.truman_doctrine"),"card.truman_doctrine").NextState;var raw=ResolveValid(ordinary);
            Assert.That(HeadlineResolutionRules.CompleteChoice(raw).Error,Is.EqualTo(RuleError.ActionNotReady));Assert.That(ActionPracticeRules.CompleteEventAction(raw).NextState.Practice.CompletedActions,Is.EqualTo(1));
        }
        [Test] public void A_foreign_execution_id_cannot_finish_the_current_headline_root()
        {
            var pending=HeadlineResolutionTests.Begin(HeadlineResolutionTests.Ready("decolonization","fidel")).NextState;
            var wrong=new InfluenceEventExecution("foreign",pending.InfluenceEvent.Definition,pending.Practice.Actor,InfluenceEventPolicy.NoActiveInterference);
            var replaced=new GameState(pending.Map,pending.Countries,pending.Operations,pending.GameId,pending.Version,pending.SpentOps,pending.EventSequence,
                influenceEvent:wrong,turn:pending.Turn,cards:pending.Cards,randomState:pending.RandomState,practice:pending.Practice);
            var foreign=InfluenceEventFlow.Begin(replaced,pending.Practice.Actor);Assert.That(foreign.Accepted,Is.True);var raw=ResolveValid(foreign.NextState);Assert.That(raw.Accepted,Is.True);
            var r=HeadlineResolutionRules.CompleteChoice(raw);Assert.That(r.Error,Is.EqualTo(RuleError.InvalidDefinition));Assert.That(r.NextState,Is.Null);Assert.That(pending.Practice.Headline.CompletedCount,Is.Zero);
        }
    }
}
