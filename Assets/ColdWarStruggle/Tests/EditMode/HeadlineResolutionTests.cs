using System;
using System.Collections.Generic;
using System.Linq;
using ColdWarStruggle.Core;
using ColdWarStruggle.Infrastructure;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class HeadlineResolutionTests
    {
        internal static CardId Id(string n)=>new CardId("card."+n);
        internal static HeadlineOrderParameters Parameters()=>HeadlineParametersLoader.Load(TurnCalendarTests.Source());
        // Explicit all-era/altered track fixture. This is not a production opening deal.
        internal static GameState Ready(string us="fidel",string su="romanian_abdication",int level=5,int vp=0,bool empty=true)
        {
            var s=PlayTurnOpsEventTests.Ready("card."+us,level,vp);
            s=PlayTurnOpsEventTests.Give(s,PlayerSide.US,"card."+us);s=PlayTurnOpsEventTests.Give(s,PlayerSide.USSR,"card."+su);
            if(empty)s=PlayInfluenceEventTests.Copy(s,RegionScoringTests.Empty(s.Map));
            var selection=HeadlineSelectionState.Begin(s.Practice.Definition.Cards,s.Cards,s.Practice.Space.Track,new[]{Id(us),Id(su)});
            selection=HeadlineSelectionRules.PlanSelection(selection,PlayerSide.US,Id(us)).State;
            selection=HeadlineSelectionRules.PlanSelection(selection,PlayerSide.USSR,Id(su)).State;
            Assert.That(selection.Ready,Is.True);return PlayInfluenceEventTests.Copy(s,cards:selection.Cards);
        }
        internal static HeadlineResolutionTransition Begin(GameState s)=>HeadlineResolutionRules.Begin(s,Parameters());
        internal static void Completed(GameState source,GameState done)
        {
            Assert.That(done.Ended,Is.False);Assert.That(done.Practice.CompletedActions,Is.Zero);Assert.That(done.Practice.Stage,Is.EqualTo(ActionPracticeStage.ChooseCard));
            Assert.That(done.Practice.Actor,Is.EqualTo(PlayerSide.USSR));Assert.That(done.Practice.ActionRound,Is.EqualTo(1));Assert.That(done.Practice.Headline.IsComplete,Is.True);
            Assert.That(done.Practice.Headline.CompletedCount,Is.EqualTo(2));Assert.That(done.Cards.HeadlineReserved,Is.Empty);Assert.That(done.Cards.ResolvingCards,Is.Empty);
            Assert.That(done.Practice.ActiveCard,Is.Null);Assert.That(done.InfluenceEvent,Is.Null);Assert.That(done.Operations.Budget,Is.Zero);Assert.That(done.SpentOps,Is.Zero);
            Assert.That(done.Practice.Space,Is.SameAs(source.Practice.Space));Assert.That(done.Practice.Risk.Military,Is.SameAs(source.Practice.Risk.Military));Pcg32RandomTests.Same(source.RandomState,done.RandomState);
            Assert.That(source.Cards.HeadlineReserved,Has.Count.EqualTo(2));Assert.That(source.Practice.Headline,Is.Null);
        }
        [TestCase("fidel","romanian_abdication",PlayerSide.US)][TestCase("nasser","fidel",PlayerSide.USSR)]
        public void Immediate_headlines_execute_in_printed_order_without_spending_ordinary_actions(string us,string su,PlayerSide first)
        {
            var s=Ready(us,su);var r=Begin(s);Assert.That(r.Accepted,Is.True,r.Error.ToString());Completed(s,r.NextState);
            Assert.That(r.Steps,Has.Count.EqualTo(2));Assert.That(r.Steps[0].PlayedBy,Is.EqualTo(first));Assert.That(r.Steps.Select(x=>x.Card),Is.EqualTo(r.NextState.Practice.Headline.Order.Select(x=>x.Card)));
            Assert.That(r.NextState.Cards.RemovedCards,Is.SupersetOf(new[]{Id(us),Id(su)}));Assert.That(r.NextState.Practice.VpForUS,Is.Zero);
            Assert.Throws<NotSupportedException>(()=>((IList<HeadlineResolutionStep>)r.Steps).Clear());
        }
        [Test] public void Starred_no_change_event_still_completes_and_is_removed()
        {
            var s=Ready();var board=s.Countries.ToDictionary(x=>x.Key,x=>x.Value);board[PlayInfluenceEventTests.C("cuba")]=new CountryState(0,5);board[PlayInfluenceEventTests.C("romania")]=new CountryState(0,5);s=PlayInfluenceEventTests.Copy(s,board);
            var r=Begin(s);Assert.That(r.Accepted,Is.True);Completed(s,r.NextState);
            foreach(var step in r.Steps)Assert.That(step.Result.EventTransition.NextState.InfluenceEvent.Outcome,Is.EqualTo(InfluenceEventStatus.ResolvedNoChange));
            Assert.That(r.NextState.Countries,Is.EquivalentTo(s.Countries));Assert.That(r.NextState.Cards.RemovedCards,Is.SupersetOf(new[]{Id("fidel"),Id("romanian_abdication")}));
        }
        [TestCase("europe")][TestCase("asia")][TestCase("middle_east")][TestCase("africa")][TestCase("central_america")][TestCase("south_america")][TestCase("southeast_asia")]
        public void Every_supported_scoring_headline_resolves_after_higher_printed_event(string region)
        {
            var s=Ready(region+"_scoring","containment");var r=Begin(s);Assert.That(r.Accepted,Is.True,r.Error.ToString());Completed(s,r.NextState);
            Assert.That(r.Steps[0].Card,Is.EqualTo(Id("containment")));Assert.That(r.Steps[1].Result.ScoringResult.RegionId,Is.EqualTo("region."+region));
            Assert.That(r.NextState.Practice.Headline.Order[1].Value,Is.Zero);Assert.That(r.NextState.Practice.VpForUS,Is.Zero);Assert.That(r.NextState.Practice.OpsEffects.Instances,Has.Count.EqualTo(1));
            Assert.That(region=="southeast_asia"?r.NextState.Cards.RemovedCards:r.NextState.Cards.DiscardPile,Does.Contain(Id(region+"_scoring")));
        }
        [Test] public void Two_scoring_headlines_tie_by_shared_policy_and_apply_separate_dispositions()
        {
            var s=Ready("southeast_asia_scoring","africa_scoring");var r=Begin(s);Assert.That(r.Accepted,Is.True);Completed(s,r.NextState);
            Assert.That(r.Steps[0].PlayedBy,Is.EqualTo(PlayerSide.US));Assert.That(r.Steps.All(x=>x.Result.ScoringResult!=null),Is.True);
            Assert.That(r.NextState.Cards.RemovedCards,Does.Contain(Id("southeast_asia_scoring")));Assert.That(r.NextState.Cards.DiscardPile,Does.Contain(Id("africa_scoring")));
        }
        [Test] public void Later_scoring_reads_influence_changes_from_the_first_headline()
        {
            var s=Ready("middle_east_scoring","nasser");var r=Begin(s);Assert.That(r.Accepted,Is.True);Completed(s,r.NextState);
            Assert.That(r.Steps[0].Card,Is.EqualTo(Id("nasser")));Assert.That(r.NextState.GetInfluence(PlayInfluenceEventTests.C("egypt"),PlayerSide.USSR),Is.EqualTo(2));
            Assert.That(r.Steps[1].Result.ScoringResult.NetVpForUS,Is.LessThan(0));Assert.That(r.NextState.Practice.VpForUS,Is.EqualTo(r.Steps[1].Result.ScoringResult.NetVpForUS));
            Assert.That(s.Practice.VpForUS,Is.Zero);Assert.That(s.GetInfluence(PlayInfluenceEventTests.C("egypt"),PlayerSide.USSR),Is.Zero);
        }
        [Test] public void Turn_modifiers_keep_distinct_headline_roots_and_do_not_reprice_the_frozen_order()
        {
            var s=Ready("red_scare_purge","containment");var r=Begin(s);Assert.That(r.Accepted,Is.True,r.Error.ToString());Completed(s,r.NextState);
            var effects=r.NextState.Practice.OpsEffects.Instances;Assert.That(effects,Has.Count.EqualTo(2));Assert.That(effects.Select(x=>x.ResolutionId).Distinct().Count(),Is.EqualTo(2));
            Assert.That(effects.Single(x=>x.Definition.Card.Equals(Id("red_scare_purge"))).AffectedSide,Is.EqualTo(PlayerSide.USSR));
            Assert.That(effects.Single(x=>x.Definition.Card.Equals(Id("containment"))).AffectedSide,Is.EqualTo(PlayerSide.US));
            Assert.That(r.NextState.Practice.Headline.Order[1].Value,Is.EqualTo(s.Practice.Definition.Cards.GetCard(Id("containment")).PrintedOps));
            Assert.That(ActionPracticeRules.QuoteCard(r.NextState,PlayerSide.USSR,Id("containment"),OpsValuePurpose.Influence).GeneralValue,Is.EqualTo(r.NextState.Practice.Headline.Order[1].Value-1));
        }
        [TestCase(PlayerSide.US)][TestCase(PlayerSide.USSR)]
        public void Nuclear_loss_belongs_to_headline_player_not_event_beneficiary_and_stops_the_second_card(PlayerSide player)
        {
            var s=Ready(player==PlayerSide.US?"duck_and_cover":"fidel",player==PlayerSide.US?"fidel":"duck_and_cover",level:2);var r=Begin(s);Assert.That(r.Accepted,Is.True);
            var n=r.NextState;Assert.That(n.Ended,Is.True);Assert.That(n.Practice.Risk.Result.Winner,Is.EqualTo(player==PlayerSide.US?PlayerSide.USSR:PlayerSide.US));
            Assert.That(n.Practice.Actor,Is.EqualTo(player));Assert.That(n.Practice.CompletedActions,Is.Zero);Assert.That(n.Practice.Headline.CompletedCount,Is.Zero);Assert.That(r.Steps,Has.Count.EqualTo(1));
            Assert.That(r.Steps[0].Result.DefconVpEvent.Award,Is.Null);Assert.That(n.Cards.ResolvingCards,Is.EqualTo(new[]{Id("duck_and_cover")}));Assert.That(n.Cards.HeadlineReserved.Values,Is.EqualTo(new[]{Id("fidel")}));
            Assert.That(n.GetInfluence(PlayInfluenceEventTests.C("cuba"),PlayerSide.USSR),Is.Zero);Assert.That(HeadlineResolutionRules.Advance(n).Error,Is.EqualTo(RuleError.GameAlreadyEnded));
        }
        [Test] public void Vp_win_stops_nuclear_test_ban_before_defcon_improvement_and_second_headline()
        {
            var s=Ready("nuclear_test_ban","fidel",level:3,vp:19);var r=Begin(s);Assert.That(r.Accepted,Is.True);var n=r.NextState;
            Assert.That(n.Ended,Is.True);Assert.That(n.Practice.Risk.Result.Winner,Is.EqualTo(PlayerSide.US));Assert.That(n.Practice.VpForUS,Is.EqualTo(20));Assert.That(n.Practice.Risk.Defcon.Level,Is.EqualTo(3));
            Assert.That(n.Cards.ResolvingCards,Does.Contain(Id("nuclear_test_ban")));Assert.That(n.Cards.GetHeadline(PlayerSide.USSR),Is.EqualTo(Id("fidel")));Assert.That(r.Steps,Has.Count.EqualTo(1));
        }
        [TestCase(PlayerSide.US)][TestCase(PlayerSide.USSR)] public void Europe_control_ends_before_the_other_scoring_headline(PlayerSide winner)
        {
            var s=Ready("europe_scoring","asia_scoring");var board=s.Countries.ToDictionary(x=>x.Key,x=>x.Value);
            RegionScoringTests.Control(s.Map,board,winner,s.Map.GetCountriesInRegion("region.europe").Where(c=>c.Battleground).Select(c=>c.Id.Value).ToArray());s=PlayInfluenceEventTests.Copy(s,board);
            var r=Begin(s);Assert.That(r.Accepted,Is.True);Assert.That(r.NextState.Ended,Is.True);Assert.That(r.NextState.Practice.Risk.Result.Winner,Is.EqualTo(winner));Assert.That(r.Steps,Has.Count.EqualTo(1));
            Assert.That(r.NextState.Cards.GetHeadline(PlayerSide.USSR),Is.EqualTo(Id("asia_scoring")));Assert.That(r.NextState.Practice.CompletedActions,Is.Zero);
        }
        [Test] public void Second_headline_terminal_keeps_the_completed_first_prefix()
        {
            var s=Ready("containment","duck_and_cover",level:2);var r=Begin(s);Assert.That(r.Accepted,Is.True);var n=r.NextState;
            Assert.That(n.Ended,Is.True);Assert.That(n.Practice.Headline.CompletedCount,Is.EqualTo(1));Assert.That(n.Practice.CompletedActions,Is.Zero);Assert.That(n.Cards.RemovedCards,Does.Contain(Id("containment")));
            Assert.That(n.Practice.OpsEffects.Instances,Has.Count.EqualTo(1));Assert.That(n.Cards.ResolvingCards,Does.Contain(Id("duck_and_cover")));Assert.That(n.Cards.HeadlineReserved,Is.Empty);Assert.That(r.Steps,Has.Count.EqualTo(2));
        }
        [TestCase(true)][TestCase(false)] public void Waiting_first_or_second_headline_uses_its_actor_and_blocks_ordinary_actions(bool first)
        {
            var s=first?Ready("decolonization","fidel"):Ready("fidel","truman_doctrine",empty:false);var r=Begin(s);Assert.That(r.Accepted,Is.True);var n=r.NextState;
            Assert.That(n.InfluenceEvent.Stage,Is.EqualTo(InfluenceEventStage.AwaitingChoice));Assert.That(n.Practice.Actor,Is.EqualTo(first?PlayerSide.US:PlayerSide.USSR));
            Assert.That(n.Practice.CompletedActions,Is.Zero);Assert.That(n.Practice.Headline.CompletedCount,Is.EqualTo(first?0:1));Assert.That(n.Cards.HeadlineReserved.Count,Is.EqualTo(first?1:0));
            Assert.That(n.Cards.ResolvingCards,Is.EqualTo(new[]{Id(first?"decolonization":"truman_doctrine")}));Assert.That(n.InfluenceEvent.PhasingPlayer,Is.EqualTo(n.Practice.Actor));
            Assert.That(n.InfluenceEvent.Owner,Is.EqualTo(first?PlayerSide.USSR:PlayerSide.US));Assert.That(n.InfluenceEvent.Id,Is.EqualTo(n.Practice.Headline.CurrentResolutionId));
            Assert.That(ActionPracticeRules.CanPlay(n,n.Practice.Actor,n.Cards.GetHand(n.Practice.Actor).First(),CardActionKind.Event),Is.EqualTo(RuleError.ActionNotReady));
            Assert.That(HeadlineResolutionRules.Advance(n).Error,Is.EqualTo(RuleError.ActionNotReady));
        }
        [Test] public void Supported_set_is_derived_from_registered_services_and_excludes_special_unimplemented_cards()
        {
            var s=Ready();var supported=HeadlineResolutionRules.SupportedCards(s.Practice.Definition);
            Assert.That(supported,Has.Count.EqualTo(19));Assert.That(supported,Has.No.Member(Id("defectors")));Assert.That(supported,Has.No.Member(s.Cards.China.Id));
            foreach(var c in s.Practice.Definition.Cards.Cards.Where(c=>c.Kind==CardKind.Scoring))Assert.That(supported,Does.Contain(c.Id));
            Assert.Throws<NotSupportedException>(()=>((IList<CardId>)supported).Clear());
        }
        [TestCase("blockade")][TestCase("defectors")] public void Unsupported_or_special_headline_fails_before_any_effect_or_card_move(string card)
        {
            var s=Ready(card,"fidel");var r=Begin(s);Assert.That(r.Error,Is.EqualTo(RuleError.UnsupportedCardAction));Assert.That(r.NextState,Is.Null);Assert.That(r.Steps,Is.Empty);
            Assert.That(s.Cards.HeadlineReserved,Has.Count.EqualTo(2));Assert.That(s.Cards.ResolvingCards,Is.Empty);Assert.That(s.Practice.Headline,Is.Null);
        }
        [Test] public void Incomplete_or_missing_authority_and_duplicate_begin_fail_closed()
        {
            Assert.That(Begin(null).Error,Is.EqualTo(RuleError.InvalidCommand));var s=Ready();Assert.That(HeadlineResolutionRules.Begin(s,null).Error,Is.EqualTo(RuleError.InvalidDefinition));
            Assert.That(Begin(PlayTurnOpsEventTests.Ready()).Error,Is.EqualTo(RuleError.ActionNotReady));Assert.That(HeadlineResolutionRules.Advance(s).Error,Is.EqualTo(RuleError.ActionNotReady));
            var done=Begin(s).NextState;Assert.That(Begin(done).Error,Is.EqualTo(RuleError.ActionNotReady));Assert.That(HeadlineResolutionRules.Advance(done).Error,Is.EqualTo(RuleError.ActionNotReady));
        }
        [TestCase(true,0)][TestCase(false,0)][TestCase(true,2)][TestCase(false,2)]
        public void Overflow_drops_all_effects_in_the_current_atomic_batch(bool version,int room)
        {
            var s=Ready("red_scare_purge","containment");s=PlayInfluenceEventTests.Copy(s,version:version?long.MaxValue-room:(long?)null,sequence:version?null:(long?)long.MaxValue-room);
            var r=Begin(s);Assert.That(r.Error,Is.EqualTo(RuleError.NumericOverflow));Assert.That(r.NextState,Is.Null);Assert.That(r.Steps,Is.Empty);Assert.That(s.Practice.OpsEffects.Instances,Is.Empty);Assert.That(s.Cards.HeadlineReserved,Has.Count.EqualTo(2));
        }
        [Test] public void Second_headline_effect_overflow_does_not_publish_the_first_effect_draft()
        {
            var s=Ready("fidel","nasser");var board=s.Countries.ToDictionary(x=>x.Key,x=>x.Value);board[PlayInfluenceEventTests.C("egypt")]=new CountryState(0,int.MaxValue);s=PlayInfluenceEventTests.Copy(s,board);
            var r=Begin(s);Assert.That(r.Error,Is.EqualTo(RuleError.NumericOverflow));Assert.That(r.NextState,Is.Null);Assert.That(r.Steps,Is.Empty);Assert.That(s.GetInfluence(PlayInfluenceEventTests.C("cuba"),PlayerSide.USSR),Is.Zero);Assert.That(s.Cards.RemovedCards,Is.Empty);
        }
        [Test] public void First_ordinary_action_after_headlines_starts_at_soviet_round_one()
        {
            var source=Ready();var after=Begin(source).NextState;var card=ActionPracticeTests.Playable(after,CardActionKind.Influence);
            var start=ActionPracticeRules.Plan(after,PlayerSide.USSR,CardActionKind.Influence,"ordinary",card);Assert.That(start.Accepted,Is.True);Assert.That(start.NextState.Practice.Headline,Is.SameAs(after.Practice.Headline));
            var end=ActionPracticeRules.Plan(start.NextState,PlayerSide.USSR,CardActionKind.FinishInfluence,"finish");Assert.That(end.Accepted,Is.True);Assert.That(end.NextState.Practice.CompletedActions,Is.EqualTo(1));Assert.That(end.NextState.Practice.Actor,Is.EqualTo(PlayerSide.US));
        }
    }
}
