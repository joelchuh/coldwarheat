using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using ColdWarStruggle.Core;
using ColdWarStruggle.Infrastructure;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class HeadlineFlowTests
    {
        internal static CardId Id(string name)=>new CardId("card."+name);
        // Deliberate all-era/two-card hands fixture, not a production deal.
        internal static GameState Prepared(string us="fidel",string su="romanian_abdication",bool enabled=true,bool handlers=true,bool earlyOnly=false)
        {
            var s=PlayCoupTests.Prepared();var d=s.Practice.Definition;
            var scoring=RegionScoringDefinitionLoader.LoadSubset(File.ReadAllText(Path.Combine(DefinitionTests.Root,"Data/Rulesets/deluxe-2015/region-scoring.json")),File.ReadAllText(Path.Combine(DefinitionTests.Root,"Docs/Contracts/region-scoring-rules.schema.json")),s.Map);
            var definition=new ActionPracticeDefinition(d.Cards,d.Ops,d.Space,d.Calendar,d.Risk,handlers?scoring:null,
                handlers?PlayInfluenceEventTests.Effects(s.Map,d.Cards):null,handlers?PlayDefconVpEventTests.Effects(d.Cards):null,
                handlers?PlayTurnOpsEventTests.Effects(d.Cards):null,RealignmentTests.Definition(),
                new ChinaCardDefinition(d.Cards,s.Map,ChinaCardPlayTests.China,new OpsRegionalBonus("china","region.asia",1)),true,true,
                enabled?HeadlineResolutionTests.Parameters():null);
            var hands=new Dictionary<PlayerSide,IReadOnlyList<CardId>>{{PlayerSide.US,new[]{Id(us)}},{PlayerSide.USSR,new[]{Id(su)}}};
            var used=hands.Values.SelectMany(x=>x).ToHashSet();
            var cards=new CardZones(d.Cards,d.Cards.Cards.Where(c=>c.Kind!=CardKind.China&&!used.Contains(c.Id)&&(!earlyOnly||c.Era==GameEra.Early)).Select(c=>c.Id),hands,
                earlyOnly?d.Cards.Cards.Where(c=>c.Kind!=CardKind.China&&c.Era!=GameEra.Early).Select(c=>c.Id):Array.Empty<CardId>(),Array.Empty<CardId>(),Array.Empty<CardId>(),Array.Empty<CardId>(),s.Cards.China,earlyOnly?new[]{GameEra.Early}:new[]{GameEra.Early,GameEra.Mid,GameEra.Late});
            return new GameState(s.Map,RegionScoringTests.Empty(s.Map),s.Operations,s.GameId,s.Version,s.SpentOps,s.EventSequence,
                turn:s.Turn,deployment:s.Deployment,cards:cards,randomState:s.RandomState,practice:ActionPracticeState.Create(definition));
        }
        internal static GameState Start(GameState s)
        {var r=ActionPracticeRules.Plan(s,PlayerSide.USSR,CardActionKind.StartPractice,"start");Assert.That(r.Accepted,Is.True,r.Error.ToString());return r.NextState;}
        internal static GameState Ready(string us="fidel",string su="romanian_abdication")=>Start(Prepared(us,su));
        internal static GameState Choose(GameState s,PlayerSide side,string card)
        {var r=HeadlineFlowRules.Submit(s,side,Id(card));Assert.That(r.Accepted,Is.True,r.Error.ToString());return r.NextState;}
        static void Rejected(HeadlineResolutionTransition r,RuleError error)
        {Assert.That(r.Error,Is.EqualTo(error));Assert.That(r.NextState,Is.Null);Assert.That(r.Steps,Is.Empty);}
        [Test] public void Enabled_start_enters_bound_collection_without_redeal_or_ops()
        {
            var s=Prepared();var n=Start(s);Assert.That(n.Practice.Stage,Is.EqualTo(ActionPracticeStage.CollectingHeadline));
            Assert.That(n.Practice.HeadlineSelection,Is.Not.Null);Assert.That(n.Practice.Headline,Is.Null);
            Assert.That(n.Cards,Is.SameAs(n.Practice.HeadlineSelection.Cards));Assert.That(n.Practice.HeadlineSelection.Catalog,Is.SameAs(n.Practice.Definition.Cards));
            Assert.That(n.Cards.GetHand(PlayerSide.US),Is.EqualTo(s.Cards.GetHand(PlayerSide.US)));Assert.That(n.Cards.DrawPile,Is.EqualTo(s.Cards.DrawPile));
            Pcg32RandomTests.Same(n.RandomState,s.RandomState);Assert.That(n.Deployment,Is.Null);Assert.That(n.Practice.CompletedActions,Is.Zero);Assert.That(n.Operations.Budget,Is.Zero);
        }
        [Test] public void Default_definition_keeps_legacy_start_and_cannot_begin_collection()
        {
            var s=Prepared(enabled:false);var n=Start(s);Assert.That(n.Practice.Stage,Is.EqualTo(ActionPracticeStage.ChooseCard));
            Assert.That(n.Practice.HeadlineSelection,Is.Null);Assert.That(n.Version,Is.EqualTo(s.Version+1));
            Rejected(HeadlineFlowRules.BeginCollection(n),RuleError.UnsupportedCardAction);
        }
        [TestCase(PlayerSide.US)][TestCase(PlayerSide.USSR)] public void First_submission_locks_exactly_one_card_and_second_executes_both(PlayerSide first)
        {
            var s=Ready();var other=first==PlayerSide.US?PlayerSide.USSR:PlayerSide.US;
            var firstCard=first==PlayerSide.US?"fidel":"romanian_abdication";var otherCard=other==PlayerSide.US?"fidel":"romanian_abdication";
            var r=HeadlineFlowRules.Submit(s,first,Id(firstCard));Assert.That(r.Accepted,Is.True);var locked=r.NextState;
            Assert.That(r.Steps,Is.Empty);Assert.That(locked.Cards.GetHeadline(first),Is.EqualTo(Id(firstCard)));Assert.That(locked.Cards.GetHand(first),Is.Empty);
            Assert.That(locked.Version,Is.EqualTo(s.Version+1));Assert.That(locked.Countries,Is.EquivalentTo(s.Countries));Assert.That(s.Cards.HeadlineReserved,Is.Empty);
            Assert.That(locked.Cards,Is.SameAs(locked.Practice.HeadlineSelection.Cards));Assert.That(locked.Practice.CompletedActions,Is.Zero);
            var done=HeadlineFlowRules.Submit(locked,other,Id(otherCard));Assert.That(done.Accepted,Is.True);Assert.That(done.Steps,Has.Count.EqualTo(2));
            Assert.That(done.NextState.Practice.HeadlineSelection,Is.Null);Assert.That(done.NextState.Practice.Headline.IsComplete,Is.True);
            Assert.That(done.NextState.Practice.Stage,Is.EqualTo(ActionPracticeStage.ChooseCard));Assert.That(done.NextState.Practice.Actor,Is.EqualTo(PlayerSide.USSR));Assert.That(done.NextState.Practice.CompletedActions,Is.Zero);
            Assert.That(locked.Cards.HeadlineReserved,Has.Count.EqualTo(1));Pcg32RandomTests.Same(s.RandomState,done.NextState.RandomState);
        }
        [Test] public void Empty_reservation_collection_already_blocks_ordinary_actions()
        {
            var s=Ready();Assert.That(s.Cards.HeadlineReserved,Is.Empty);
            foreach(var side in new[]{PlayerSide.US,PlayerSide.USSR})Assert.That(ActionPracticeRules.CanPlay(s,side,Id("fidel"),CardActionKind.Event),Is.EqualTo(RuleError.ActionNotReady));
            Rejected(HeadlineResolutionRules.Begin(s,HeadlineResolutionTests.Parameters()),RuleError.ActionNotReady);
        }
        [Test] public void Duplicate_lock_and_repeated_begin_do_not_mutate()
        {
            var s=Choose(Ready(),PlayerSide.US,"fidel");Rejected(HeadlineFlowRules.Submit(s,PlayerSide.US,Id("fidel")),RuleError.ActionNotReady);
            Rejected(HeadlineFlowRules.BeginCollection(s),RuleError.ActionNotReady);Assert.That(s.Cards.GetHeadline(PlayerSide.US),Is.EqualTo(Id("fidel")));
        }
        [Test] public void Unsupported_hand_card_is_not_an_automatic_noop()
        {var s=Ready("blockade");Rejected(HeadlineFlowRules.Submit(s,PlayerSide.US,Id("blockade")),RuleError.UnsupportedCardAction);Assert.That(s.Cards.GetHand(PlayerSide.US),Does.Contain(Id("blockade")));}
        [Test] public void Wrong_ownership_and_unknown_china_and_side_are_rejected()
        {
            var s=Ready();Rejected(HeadlineFlowRules.Submit(s,PlayerSide.US,Id("romanian_abdication")),RuleError.CardNotOwned);
            Rejected(HeadlineFlowRules.Submit(s,PlayerSide.US,default),RuleError.InvalidCommand);
            Rejected(HeadlineFlowRules.Submit(s,PlayerSide.US,s.Cards.China.Id),RuleError.UnsupportedCardAction);
            Rejected(HeadlineFlowRules.Submit(s,(PlayerSide)99,Id("fidel")),RuleError.InvalidCommand);
        }
        [Test] public void No_registered_handlers_leaves_explicit_collection_with_no_candidates()
        {
            var s=Start(Prepared(handlers:false));Assert.That(s.Practice.Stage,Is.EqualTo(ActionPracticeStage.CollectingHeadline));
            Assert.That(s.Practice.HeadlineSelection.SupportedCards,Is.Empty);Assert.That(HeadlineSelectionRules.CanChoose(s.Practice.HeadlineSelection,PlayerSide.US),Is.False);
            Rejected(HeadlineFlowRules.Submit(s,PlayerSide.US,Id("fidel")),RuleError.UnsupportedCardAction);
        }
        [Test] public void Collection_card_snapshot_cannot_be_replaced_independently()
        {
            var s=Ready();var selected=HeadlineSelectionRules.PlanSelection(s.Practice.HeadlineSelection,PlayerSide.US,Id("fidel"));
            Assert.Throws<ArgumentException>(()=>PlayInfluenceEventTests.Copy(s,cards:selected.State.Cards));
        }
        [TestCase(true)][TestCase(false)] public void First_submission_overflow_returns_no_draft(bool version)
        {
            var s=Ready();s=PlayInfluenceEventTests.Copy(s,version:version?long.MaxValue:(long?)null,sequence:version?null:(long?)long.MaxValue);
            Rejected(HeadlineFlowRules.Submit(s,PlayerSide.US,Id("fidel")),RuleError.NumericOverflow);Assert.That(s.Cards.HeadlineReserved,Is.Empty);
        }
        [TestCase(true)][TestCase(false)] public void Later_resolution_overflow_keeps_the_first_published_lock(bool version)
        {
            var s=Choose(Ready(),PlayerSide.US,"fidel");s=PlayInfluenceEventTests.Copy(s,version:version?long.MaxValue-3:(long?)null,sequence:version?null:(long?)long.MaxValue-3);
            Rejected(HeadlineFlowRules.Submit(s,PlayerSide.USSR,Id("romanian_abdication")),RuleError.NumericOverflow);
            Assert.That(s.Cards.HeadlineReserved,Has.Count.EqualTo(1));Assert.That(s.Cards.GetHand(PlayerSide.USSR),Does.Contain(Id("romanian_abdication")));Assert.That(s.Cards.RemovedCards,Is.Empty);
        }
        [Test] public void After_resolving_both_the_same_turn_cannot_reopen_collection()
        {
            var s=Choose(Choose(Ready(),PlayerSide.US,"fidel"),PlayerSide.USSR,"romanian_abdication");
            Rejected(HeadlineFlowRules.BeginCollection(s),RuleError.ActionNotReady);Rejected(HeadlineFlowRules.Submit(s,PlayerSide.US,Id("fidel")),RuleError.ActionNotReady);
        }
        [Test] public void Setup_and_null_states_cannot_skip_to_headline()
        {
            Rejected(HeadlineFlowRules.BeginCollection(null),RuleError.InvalidCommand);Rejected(HeadlineFlowRules.Submit(null,PlayerSide.US,Id("fidel")),RuleError.InvalidCommand);
            Rejected(HeadlineFlowRules.BeginCollection(Prepared()),RuleError.SetupInProgress);
        }
        [Test] public void Second_submission_stops_at_required_event_owner_choice()
        {
            var s=Choose(Ready("decolonization","nasser"),PlayerSide.US,"decolonization");var r=HeadlineFlowRules.Submit(s,PlayerSide.USSR,Id("nasser"));
            Assert.That(r.Accepted,Is.True);Assert.That(r.Steps,Has.Count.EqualTo(1));Assert.That(r.NextState.InfluenceEvent.Owner,Is.EqualTo(PlayerSide.USSR));
            Assert.That(r.NextState.Practice.Actor,Is.EqualTo(PlayerSide.US));Assert.That(r.NextState.Practice.Stage,Is.EqualTo(ActionPracticeStage.ResolvingEvent));
            Assert.That(r.NextState.Cards.GetHeadline(PlayerSide.USSR),Is.EqualTo(Id("nasser")));Assert.That(r.NextState.Practice.HeadlineSelection,Is.Null);
        }
        [Test] public void Next_turn_reenters_collection_with_one_deal_and_one_defcon_change()
        {
            var s=Choose(Choose(Start(Prepared(earlyOnly:true)),PlayerSide.US,"fidel"),PlayerSide.USSR,"romanian_abdication");s=NextTurnPlayTests.Assessed(s);
            var expected=TurnDealService.Plan(s.Practice.Definition.Cards,s.Cards,s.Practice.Definition.Calendar.ForTurn(2),new Pcg32Random(s.RandomState));
            var r=NextTurnPlayTests.Next(s);Assert.That(r.Accepted,Is.True,r.Error.ToString());var n=r.NextState;
            Assert.That(n.Practice.Stage,Is.EqualTo(ActionPracticeStage.CollectingHeadline));Assert.That(n.Practice.Headline,Is.Null);Assert.That(n.Turn.Number,Is.EqualTo(2));
            Assert.That(n.Cards.GetHand(PlayerSide.US),Is.EqualTo(expected.Cards.GetHand(PlayerSide.US)));Assert.That(n.Cards.DrawPile,Is.EqualTo(expected.Cards.DrawPile));Pcg32RandomTests.Same(n.RandomState,expected.Random);
            Assert.That(n.Practice.Risk.Defcon.Level,Is.EqualTo(Math.Min(s.Practice.Risk.Defcon.Level+1,5)));Assert.That(NextTurnPlayTests.Next(n).Error,Is.EqualTo(RuleError.ActionNotReady));
        }
    }
}
