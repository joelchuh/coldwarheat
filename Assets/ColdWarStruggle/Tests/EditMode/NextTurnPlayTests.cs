using System;
using System.Linq;
using System.Collections.Generic;
using ColdWarStruggle.Core;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class NextTurnPlayTests
    {
        internal static GameState Ready(bool enabled=true,int level=5,int militaryInitial=0,TurnCalendar calendar=null,SpaceRaceDefinition space=null)
        {
            var s=PlayScoringTests.Ready("card.olympic_games",configure:d=>{
                var rd=d.Risk;var f=rd.Defcon;
                var risk=new PracticeRiskDefinition(rd.Coup,new DefconDefinition(f.Map,level,f.MaximumLevel,f.NuclearLevel,f.TurnStartImprovement,f.BattlegroundCoupDegradation,f.RestrictedActions,f.BlockedRegions),
                    new MilitaryOperationsDefinition(militaryInitial,rd.Military.ResetEarned,rd.Military.PenaltyVpPerShortfall,rd.Military.CreditSources),rd.Victory);
                return new ActionPracticeDefinition(d.Cards,d.Ops,space??d.Space,calendar??d.Calendar,risk,d.Scoring,
                    PlayInfluenceEventTests.Effects(f.Map,d.Cards),PlayDefconVpEventTests.Effects(d.Cards),PlayTurnOpsEventTests.Effects(d.Cards),RealignmentTests.Definition(),
                    new ChinaCardDefinition(d.Cards,f.Map,ChinaCardPlayTests.China,new OpsRegionalBonus("china","region.asia",1)),true,enabled);
            });
            var deal=InitialDealService.Create(s.Practice.Definition.Cards,new InitialDealDefinition(s.Practice.Space.Turn,CardZonesTests.China()),new Pcg32Random(s.RandomState));
            return new GameState(s.Map,s.Countries,s.Operations,s.GameId,s.Version,s.SpentOps,s.EventSequence,turn:s.Practice.Space.Turn,cards:deal.Cards,randomState:deal.Random,practice:s.Practice);
        }
        internal static GameState Apply(GameState s,CardActionKind kind,string card=null,CountryId? target=null)
        {var r=ActionPracticeRules.Plan(s,s.Practice.Actor,kind,"test-"+s.Version,card==null?(CardId?)null:new CardId(card),target);Assert.That(r.Accepted,Is.True,kind+":"+r.Error);return r.NextState;}
        internal static GameState Complete(GameState s)
        {
            while(s.Practice.CompletedActions<s.Turn.Parameters.NormalActionsPerSide*2)
            {s=PlayTurnOpsEventTests.Give(s,s.Practice.Actor,"card.olympic_games");s=Apply(s,CardActionKind.Influence,"card.olympic_games");s=Apply(s,CardActionKind.FinishInfluence);}
            // Explicit fixture removes held scoring into discard; default-deal session test uses actual legal plays.
            var c=s.Cards;var hands=new Dictionary<PlayerSide,IReadOnlyList<CardId>>();var discarded=c.DiscardPile.ToList();
            foreach(var side in new[]{PlayerSide.US,PlayerSide.USSR})
            {hands[side]=c.GetHand(side).Where(id=>s.Practice.Definition.Cards.GetCard(id).Kind!=CardKind.Scoring).ToArray();discarded.AddRange(c.GetHand(side).Except(hands[side]));}
            return PlayInfluenceEventTests.Copy(s,cards:new CardZones(s.Practice.Definition.Cards,c.DrawPile,hands,c.EraReserve,discarded,c.RemovedCards,c.ResolvingCards,c.China,c.EnteredEras));
        }
        internal static GameState Assessed(GameState s=null)=>Apply(Complete(s??Ready()),CardActionKind.AssessTurnEnd);
        internal static CardActionTransition Next(GameState s,PlayerSide side=PlayerSide.US)=>ActionPracticeRules.Plan(s,side,CardActionKind.BeginNextTurn,"next");
        [TestCase(3,4)][TestCase(5,5)] public void Next_turn_preserves_world_and_vp_refills_hands_and_resets_only_turn_state(int level,int expected)
        {
            var s=Assessed(Ready(level:level,militaryInitial:5));var p=Next(s);Assert.That(p.Accepted,Is.True,p.Error.ToString());var n=p.NextState;
            Assert.That(n.Turn.Number,Is.EqualTo(2));Assert.That(n.Practice.Space.Turn,Is.SameAs(n.Turn));Assert.That(n.Practice.Stage,Is.EqualTo(ActionPracticeStage.ChooseCard));Assert.That(n.Practice.Actor,Is.EqualTo(PlayerSide.USSR));Assert.That(n.Practice.CompletedActions,Is.Zero);Assert.That(n.Practice.TurnEnd,Is.Null);
            Assert.That(n.Practice.Risk.Defcon.Level,Is.EqualTo(expected));Assert.That(n.Practice.Risk.Military.UsEarned,Is.Zero);Assert.That(n.Practice.Risk.Military.UssrEarned,Is.Zero);Assert.That(n.Practice.Risk.Military.Assessment,Is.Null);Assert.That(n.Practice.Risk.Military.Turn,Is.SameAs(n.Turn));
            Assert.That(n.Cards.GetHand(PlayerSide.US),Has.Count.EqualTo(8));Assert.That(n.Cards.GetHand(PlayerSide.USSR),Has.Count.EqualTo(8));Assert.That(n.Cards.China,Is.SameAs(s.Cards.China));Assert.That(n.Countries,Is.EquivalentTo(s.Countries));Assert.That(n.Practice.VpForUS,Is.EqualTo(s.Practice.VpForUS));Assert.That(n.Practice.Space.Track,Is.SameAs(s.Practice.Space.Track));Assert.That(n.Practice.OpsEffects.Instances,Is.Empty);
            Assert.That(n.Version,Is.EqualTo(s.Version+1));Assert.That(n.EventSequence,Is.EqualTo(s.EventSequence+1));Assert.That(n.Operations.Budget,Is.Zero);Assert.That(n.Operations.Actor,Is.EqualTo(PlayerSide.USSR));Assert.That(n.Operations.Id,Is.Not.EqualTo(s.Operations.Id));Assert.That(s.Turn.Number,Is.EqualTo(1));
        }
        [Test] public void Same_action_slot_space_attempt_in_next_turn_has_distinct_receipt_and_preserves_history()
        {
            var s=PlayTurnOpsEventTests.Give(Ready(),PlayerSide.USSR,"card.olympic_games");s=Apply(s,CardActionKind.Space,"card.olympic_games");var first=s.Practice.Space.Receipts.Single().Key;
            s=Assessed(s);var before=s.Practice.Space;s=Next(s).NextState;Assert.That(s,Is.Not.Null);Assert.That(s.Practice.Space.UssrAttempts,Is.Zero);Assert.That(s.Practice.Space.Track,Is.SameAs(before.Track));Assert.That(s.Practice.Space.Receipts.ContainsKey(first),Is.True);
            s=PlayTurnOpsEventTests.Give(s,PlayerSide.USSR,"card.olympic_games");s=Apply(s,CardActionKind.Space,"card.olympic_games");Assert.That(s.Practice.Space.Receipts,Has.Count.EqualTo(2));Assert.That(s.Practice.Space.UssrAttempts,Is.EqualTo(1));Assert.That(s.Practice.Space.Receipts.Keys.Distinct().Count(),Is.EqualTo(2));
        }
        [Test] public void Same_action_slot_coup_in_next_turn_has_distinct_receipt_and_military_credit()
        {
            var s=Ready();var countries=s.Countries.ToDictionary(x=>x.Key,x=>x.Value);var target=ChinaCardPlayTests.C("afghanistan");countries[target]=new CountryState(100,100);s=PlayScoringTests.Replace(s,countries);
            s=PlayTurnOpsEventTests.Give(s,PlayerSide.USSR,"card.fidel");s=Apply(s,CardActionKind.Coup,"card.fidel",target);var first=s.Practice.Risk.CoupReceipts.Single().Key;
            s=Next(Assessed(s)).NextState;Assert.That(s,Is.Not.Null);Assert.That(s.Practice.Risk.CoupReceipts.ContainsKey(first),Is.True);Assert.That(s.Practice.Risk.Military.UssrEarned,Is.Zero);
            s=PlayTurnOpsEventTests.Give(s,PlayerSide.USSR,"card.fidel");s=Apply(s,CardActionKind.Coup,"card.fidel",target);Assert.That(s.Practice.Risk.CoupReceipts,Has.Count.EqualTo(2));Assert.That(s.Practice.Risk.Military.UssrEarned,Is.EqualTo(2));Assert.That(s.Practice.Risk.Military.AppliedCredits,Has.Count.EqualTo(2));
        }
        [Test] public void Unassessed_unconfigured_invalid_identity_and_foreign_payload_cannot_advance()
        {
            Assert.That(Next(Ready()).Error,Is.EqualTo(RuleError.ActionNotReady));Assert.That(Next(Complete(Ready())).Error,Is.EqualTo(RuleError.ActionNotReady));Assert.That(Next(Assessed(Ready(enabled:false))).Error,Is.EqualTo(RuleError.UnsupportedCardAction));
            var s=Assessed();Assert.That(Next(s,(PlayerSide)99).Error,Is.EqualTo(RuleError.InvalidCommand));Assert.That(ActionPracticeRules.Plan(s,PlayerSide.US,CardActionKind.BeginNextTurn,"x",ChinaCardPlayTests.China).Error,Is.EqualTo(RuleError.InvalidCommand));Assert.That(ActionPracticeRules.Plan(s,PlayerSide.US,CardActionKind.BeginNextTurn,"x",target:ChinaCardPlayTests.C("iran")).Error,Is.EqualTo(RuleError.InvalidCommand));
        }
        [TestCase(true)][TestCase(false)] public void Publication_overflow_does_not_commit_refill_or_track_resets(bool version)
        {var s=Assessed();s=PlayInfluenceEventTests.Copy(s,version:version?long.MaxValue:s.Version,sequence:version?s.EventSequence:long.MaxValue);var r=Next(s);Assert.That(r.Error,Is.EqualTo(RuleError.NumericOverflow));Assert.That(r.NextState,Is.Null);Assert.That(s.Turn.Number,Is.EqualTo(1));Assert.That(s.Practice.TurnEnd,Is.Not.Null);}
        [Test] public void Final_calendar_turn_is_not_advanced_or_reported_as_final_scoring()
        {
            var cal=new TurnCalendar(new[]{new EraTurnParameters(GameEra.Early,1,1,8,1),new EraTurnParameters(GameEra.Mid,2,2,9,1),new EraTurnParameters(GameEra.Late,3,3,9,1)});var s=Ready(calendar:cal);
            for(int i=1;i<=3;i++){s=Assessed(s);if(i<3){var p=Next(s);Assert.That(p.Accepted,Is.True);s=p.NextState;}}
            Assert.That(Next(s).Error,Is.EqualTo(RuleError.ActionSliceComplete));Assert.That(s.Ended,Is.False);Assert.That(s.Turn.Number,Is.EqualTo(3));
        }
        [TestCase(SpaceAbilityKind.EndTurnDiscardMaximum)][TestCase(SpaceAbilityKind.ActionRoundAllowance)]
        public void Unsupported_space_turn_entitlements_are_not_silently_skipped(SpaceAbilityKind kind)
        {
            var space=new SpaceRaceDefinition(0,1,1,1,new[]{new SpaceStepDefinition("one",1,1,1,1,0,0,"ability")},new[]{new SpaceAbilityDefinition("ability",kind,kind==SpaceAbilityKind.ActionRoundAllowance?8:1)});
            var s=PlayTurnOpsEventTests.Give(Ready(space:space),PlayerSide.USSR,"card.olympic_games");s=Apply(s,CardActionKind.Space,"card.olympic_games");s=Complete(s);
            if(kind==SpaceAbilityKind.ActionRoundAllowance)Assert.That(ActionPracticeRules.CanAssessTurnEnd(s,PlayerSide.US),Is.EqualTo(RuleError.UnsupportedCardAction));
            else {s=Apply(s,CardActionKind.AssessTurnEnd);Assert.That(Next(s).Error,Is.EqualTo(RuleError.UnsupportedCardAction));}
        }
        [Test] public void Insufficient_available_cards_reject_whole_preparation_and_preserve_snapshot()
        {
            var s=Assessed();var c=s.Cards;var hands=new Dictionary<PlayerSide,IReadOnlyList<CardId>>{{PlayerSide.US,c.GetHand(PlayerSide.US)},{PlayerSide.USSR,c.GetHand(PlayerSide.USSR)}};
            var zones=new CardZones(s.Practice.Definition.Cards,Array.Empty<CardId>(),hands,c.EraReserve,Array.Empty<CardId>(),c.RemovedCards.Concat(c.DrawPile).Concat(c.DiscardPile),c.ResolvingCards,c.China,c.EnteredEras);s=PlayInfluenceEventTests.Copy(s,cards:zones);
            var r=Next(s);Assert.That(r.Error,Is.EqualTo(RuleError.TurnPreparationUnavailable));Assert.That(r.NextState,Is.Null);Assert.That(s.Cards,Is.SameAs(zones));
        }
        [Test] public void Terminal_and_residual_resolving_snapshots_cannot_begin_a_turn()
        {
            var s=Assessed();var terminal=new GameState(s.Map,s.Countries,s.Operations,s.GameId,s.Version,s.SpentOps,s.EventSequence,true,turn:s.Turn,cards:s.Cards,randomState:s.RandomState,practice:s.Practice);
            Assert.That(Next(terminal).Error,Is.EqualTo(RuleError.GameAlreadyEnded));var c=s.Cards;var hands=new Dictionary<PlayerSide,IReadOnlyList<CardId>>{{PlayerSide.US,c.GetHand(PlayerSide.US)},{PlayerSide.USSR,c.GetHand(PlayerSide.USSR)}};
            var zones=new CardZones(s.Practice.Definition.Cards,c.DrawPile.Skip(1),hands,c.EraReserve,c.DiscardPile,c.RemovedCards,new[]{c.DrawPile[0]},c.China,c.EnteredEras);
            Assert.That(Next(PlayInfluenceEventTests.Copy(s,cards:zones)).Error,Is.EqualTo(RuleError.ActionNotReady));
        }
        [Test] public void Assessed_state_with_face_down_china_is_not_silently_repaired()
        {var s=ChinaCardPlayTests.SetChina(Assessed(),PlayerSide.US,CardFace.FaceDown);Assert.That(Next(s).Error,Is.EqualTo(RuleError.StaleContext));Assert.That(s.Cards.China.Face,Is.EqualTo(CardFace.FaceDown));}
        [Test] public void Next_turn_configuration_requires_turn_end_assessment()
        {var d=Ready().Practice.Definition;Assert.Throws<ArgumentException>(()=>new ActionPracticeDefinition(d.Cards,d.Ops,d.Space,d.Calendar,d.Risk,nextTurnEnabled:true));}
    }
}
