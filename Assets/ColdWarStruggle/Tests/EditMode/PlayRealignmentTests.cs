using System;
using System.Linq;
using System.Threading.Tasks;
using ColdWarStruggle.Core;
using ColdWarStruggle.Application;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class PlayRealignmentTests
    {
        internal static GameState Ready(string card="card.olympic_games",int level=5)=>PlayScoringTests.Ready(card,configure:d=> {
            var f=d.Risk.Defcon;var risk=new PracticeRiskDefinition(d.Risk.Coup,new DefconDefinition(f.Map,level,f.MaximumLevel,f.NuclearLevel,f.TurnStartImprovement,f.BattlegroundCoupDegradation,f.RestrictedActions,f.BlockedRegions),d.Risk.Military,d.Risk.Victory);
            return new ActionPracticeDefinition(d.Cards,d.Ops,d.Space,d.Calendar,risk,d.Scoring,PlayInfluenceEventTests.Effects(f.Map,d.Cards),PlayDefconVpEventTests.Effects(d.Cards),PlayTurnOpsEventTests.Effects(d.Cards),RealignmentTests.Definition());});
        internal static CardActionTransition Start(GameState s,string card="card.olympic_games")=>ActionPracticeRules.Plan(s,s.Practice.Actor,CardActionKind.Realignment,"start-realign",new CardId(card));
        internal static CardActionTransition Roll(GameState s,string target="angola")=>ActionPracticeRules.Plan(s,s.Practice.Actor,CardActionKind.RollRealignment,"roll",target:RealignmentTests.C(target));
        internal static GameState Started(string card="card.olympic_games",int level=5)
        {
            var before=Ready(card,level);var b=before.Countries.ToDictionary(x=>x.Key,x=>x.Value);b[RealignmentTests.C("angola")]=new CountryState(3,3);before=PlayScoringTests.Replace(before,b,Pcg32Random.FromSeed(42,54).Capture());var result=Start(before,card);Assert.That(result.Accepted,Is.True,result.Error.ToString());return result.NextState;
        }
        [Test] public void Starting_freezes_quote_moves_card_without_rolling_and_locks_operation_mode()
        {
            var s=Ready();var p=Start(s);Assert.That(p.Accepted,Is.True);var n=p.NextState;Assert.That(n.Practice.Stage,Is.EqualTo(ActionPracticeStage.Realigning));Assert.That(n.Practice.ActiveCard,Is.EqualTo(new CardId("card.olympic_games")));Assert.That(n.Practice.CompletedActions,Is.Zero);Assert.That(n.Operations.Mode,Is.EqualTo(OperationsMode.Realignment));Assert.That(n.Operations.Budget,Is.EqualTo(2));Assert.That(n.SpentOps,Is.Zero);Assert.That(n.Cards.ResolvingCards,Is.EqualTo(new[]{new CardId("card.olympic_games")}));Pcg32RandomTests.Same(n.RandomState,s.RandomState);
            Assert.That(InfluenceRules.PlanPlacement(n,n.Operations,new[]{RealignmentTests.C("iran")}).Error,Is.EqualTo(RuleError.ModeMismatch));Assert.That(ActionPracticeRules.Plan(n,n.Practice.Actor,CardActionKind.FinishInfluence,"wrong").Error,Is.EqualTo(RuleError.ActionNotReady));Assert.That(Start(n).Error,Is.EqualTo(RuleError.ActionNotReady));
        }
        [Test] public void Published_PCG_sequence_is_US_then_USSR_each_step_and_tie_spends_one_point()
        {
            var s=Started();var p=Roll(s);Assert.That(p.Accepted,Is.True);Assert.That(p.Realignment.UsRoll,Is.EqualTo(4));Assert.That(p.Realignment.UssrRoll,Is.EqualTo(4));Assert.That(p.Realignment.Removed,Is.Zero);Assert.That(p.NextState.SpentOps,Is.EqualTo(1));Assert.That(p.NextState.RandomState.DrawCount,Is.EqualTo(2));Assert.That(p.NextState.Practice,Is.SameAs(s.Practice));Assert.That(p.NextState.Cards,Is.SameAs(s.Cards));Assert.That(p.NextState.Version,Is.EqualTo(s.Version+1));Assert.That(p.NextState.EventSequence,Is.EqualTo(s.EventSequence+1));
            var again=Roll(p.NextState);Assert.That(again.Accepted,Is.True);Assert.That(again.Realignment.UsRoll,Is.EqualTo(3));Assert.That(again.Realignment.UssrRoll,Is.EqualTo(2));Assert.That(again.Realignment.RemovedSide,Is.EqualTo(PlayerSide.USSR));Assert.That(again.Realignment.Removed,Is.EqualTo(1));Assert.That(again.NextState.SpentOps,Is.EqualTo(2));Assert.That(again.NextState.RandomState.DrawCount,Is.EqualTo(4));Assert.That(Roll(again.NextState).Error,Is.EqualTo(RuleError.InsufficientBudget));Assert.That(s.SpentOps,Is.Zero);Assert.That(s.RandomState.DrawCount,Is.Zero);
        }
        [Test] public void Explicit_finish_discards_starred_OPS_card_and_completes_exactly_one_root()
        {
            var s=Started("card.fidel");var p=ActionPracticeRules.Plan(s,s.Practice.Actor,CardActionKind.FinishRealignment,"finish");Assert.That(p.Accepted,Is.True);Assert.That(p.NextState.Cards.DiscardPile,Does.Contain(new CardId("card.fidel")));Assert.That(p.NextState.Cards.RemovedCards,Is.Empty);Assert.That(p.NextState.Cards.ResolvingCards,Is.Empty);Assert.That(p.NextState.Practice.Actor,Is.EqualTo(PlayerSide.US));Assert.That(p.NextState.Practice.CompletedActions,Is.EqualTo(1));Assert.That(p.NextState.Operations.Budget,Is.Zero);Assert.That(p.NextState.SpentOps,Is.Zero);Pcg32RandomTests.Same(p.NextState.RandomState,s.RandomState);
        }
        [TestCase("card.containment",3)][TestCase("card.red_scare_purge",1)] public void Flat_ops_changes_roll_budget_but_never_adds_to_dice_and_persists_after_finish(string eventCard,int budget)
        {
            var s=PlayTurnOpsEventTests.Play(Ready(eventCard),eventCard).NextState;s=PlayTurnOpsEventTests.Give(s,PlayerSide.US,"card.defectors");var b=RegionScoringTests.Empty(s.Map);b[RealignmentTests.C("angola")]=new CountryState(3,3);s=PlayScoringTests.Replace(s,b,Pcg32Random.FromSeed(42,54).Capture());var start=Start(s,"card.defectors");Assert.That(start.Accepted,Is.True);Assert.That(start.NextState.Operations.Budget,Is.EqualTo(budget));var roll=Roll(start.NextState);Assert.That(roll.Accepted,Is.True);Assert.That(roll.Realignment.US.Total,Is.Zero);Assert.That(roll.Realignment.USSR.Total,Is.Zero);Assert.That(roll.Realignment.Removed,Is.Zero);var done=ActionPracticeRules.Plan(roll.NextState,PlayerSide.US,CardActionKind.FinishRealignment,"finish");Assert.That(done.NextState.Practice.OpsEffects,Is.SameAs(s.Practice.OpsEffects));
        }
        [Test] public void Every_roll_rechecks_current_opponent_presence_and_neighbor_control()
        {
            var s=Started("card.socialist_governments");var b=RegionScoringTests.Empty(s.Map);b[RealignmentTests.C("angola")]=new CountryState(1,3);s=PlayScoringTests.Replace(s,b);var p=Roll(s);Assert.That(p.Accepted,Is.True);Assert.That(p.Realignment.USSR.MoreInfluence,Is.EqualTo(1));Assert.That(p.Realignment.Removed,Is.EqualTo(1));Assert.That(ActionPracticeRules.CheckRealignmentTarget(p.NextState,PlayerSide.USSR,RealignmentTests.C("angola")),Is.EqualTo(RuleError.IllegalTarget));Assert.That(Roll(p.NextState).Error,Is.EqualTo(RuleError.IllegalTarget));
            // Angola no longer has US influence; the adjacent country's modifiers use this accepted new board.
            var nextBoard=p.NextState.Countries.ToDictionary(x=>x.Key,x=>x.Value);nextBoard[RealignmentTests.C("zaire")]=new CountryState(2,2);var next=PlayScoringTests.Replace(p.NextState,nextBoard);var q=Roll(next,"zaire");Assert.That(q.Accepted,Is.True);Assert.That(q.Realignment.USSR.ControlledNeighbors,Does.Contain(RealignmentTests.C("angola")));Assert.That(q.NextState.SpentOps,Is.EqualTo(2));
        }
        [TestCase("unknown",RuleError.UnknownCountry)][TestCase("france",RuleError.IllegalTarget)][TestCase("cuba",RuleError.IllegalTarget)]
        public void Illegal_target_does_not_roll_spend_or_mutate(string target,RuleError expected)
        {
            var s=Started(level:2);var r=Roll(s,target);Assert.That(r.Error,Is.EqualTo(expected));Assert.That(r.NextState,Is.Null);Assert.That(s.SpentOps,Is.Zero);Assert.That(s.RandomState.DrawCount,Is.Zero);Assert.That(s.Cards.ResolvingCards.Count,Is.EqualTo(1));
        }
        [Test] public void Battleground_realignment_does_not_degrade_defcon_or_award_military_and_no_victory_check()
        {
            var s=Started(level:2);var p=Roll(s);Assert.That(p.Accepted,Is.True);Assert.That(p.NextState.Practice.Risk,Is.SameAs(s.Practice.Risk));Assert.That(p.NextState.Practice.Risk.Defcon.Level,Is.EqualTo(2));Assert.That(p.NextState.Practice.Risk.Military.UssrEarned,Is.Zero);Assert.That(p.NextState.Practice.Space,Is.SameAs(s.Practice.Space));Assert.That(p.NextState.Practice.VpForUS,Is.EqualTo(s.Practice.VpForUS));Assert.That(p.NextState.Ended,Is.False);
        }
        [TestCase("version")][TestCase("sequence")][TestCase("second-die")]
        public void Overflow_cannot_publish_first_die_or_spending(string which)
        {
            var s=Started();var seed=s.RandomState;s=PlayScoringTests.Replace(s,version:which=="version"?long.MaxValue:(long?)null,sequence:which=="sequence"?long.MaxValue:(long?)null,random:which=="second-die"?new RandomSnapshot(seed.AlgorithmId,seed.State,seed.Increment,ulong.MaxValue-1):seed);var before=s.RandomState;var p=Roll(s);Assert.That(p.Error,Is.EqualTo(RuleError.NumericOverflow));Assert.That(p.NextState,Is.Null);Assert.That(s.SpentOps,Is.Zero);Pcg32RandomTests.Same(before,s.RandomState);Assert.That(s.GetInfluence(RealignmentTests.C("angola"),PlayerSide.USSR),Is.EqualTo(3));
        }
        [Test] public void Begin_and_finish_overflow_do_not_move_card_or_advance_root()
        {
            var s=PlayScoringTests.Replace(Ready(),version:long.MaxValue);Assert.That(Start(s).Error,Is.EqualTo(RuleError.NumericOverflow));Assert.That(s.Cards.GetHand(PlayerSide.USSR),Does.Contain(new CardId("card.olympic_games")));s=PlayScoringTests.Replace(Started(),sequence:long.MaxValue);Assert.That(ActionPracticeRules.Plan(s,PlayerSide.USSR,CardActionKind.FinishRealignment,"finish").Error,Is.EqualTo(RuleError.NumericOverflow));Assert.That(s.Practice.CompletedActions,Is.Zero);Assert.That(s.Cards.ResolvingCards.Count,Is.EqualTo(1));
        }
        [Test] public void Payload_stage_actor_and_unsupported_card_routes_reject_before_mutation()
        {
            var s=Ready();var id=new CardId("card.olympic_games");Assert.That(ActionPracticeRules.Plan(s,PlayerSide.US,CardActionKind.Realignment,"wrong",id).Error,Is.EqualTo(RuleError.WrongActor));Assert.That(ActionPracticeRules.Plan(s,PlayerSide.USSR,CardActionKind.Realignment,"bad",id,RealignmentTests.C("angola")).Error,Is.EqualTo(RuleError.InvalidCommand));Assert.That(Roll(s).Error,Is.EqualTo(RuleError.ActionNotReady));
            Assert.That(Start(Ready("card.europe_scoring"),"card.europe_scoring").Error,Is.EqualTo(RuleError.UnsupportedCardAction));Assert.That(Start(Ready("card.containment"),"card.containment").Error,Is.EqualTo(RuleError.UnsupportedCardAction));Assert.That(Start(PlayScoringTests.Ready("card.olympic_games")).Error,Is.EqualTo(RuleError.UnsupportedCardAction));
            s=Started();Assert.That(ActionPracticeRules.Plan(s,PlayerSide.USSR,CardActionKind.RollRealignment,"bad",id,RealignmentTests.C("angola")).Error,Is.EqualTo(RuleError.InvalidCommand));Assert.That(ActionPracticeRules.Plan(s,PlayerSide.USSR,CardActionKind.FinishRealignment,"bad",target:RealignmentTests.C("angola")).Error,Is.EqualTo(RuleError.InvalidCommand));Assert.That(ActionPracticeRules.Plan(s,PlayerSide.US,CardActionKind.RollRealignment,"wrong",target:RealignmentTests.C("angola")).Error,Is.EqualTo(RuleError.WrongActor));
        }
        [Test] public void Realignment_definition_requires_risk_rules()
        {var d=Ready().Practice.Definition;Assert.Throws<ArgumentException>(()=>new ActionPracticeDefinition(d.Cards,d.Ops,d.Space,d.Calendar,realignment:d.Realignment));}
        [Test] public async Task Host_replays_same_dice_and_rejects_stale_or_conflicting_roll_without_spending_twice()
        {
            var h=new LocalGameHost(Started(),true);var session=h.UssrSession;var v=session.ReadView();var command=new CardActionCommand("roll",v.GameId,v.Version,CardActionKind.RollRealignment,target:RealignmentTests.C("angola"));var result=await session.SubmitCardActionAsync(command);Assert.That(result.Accepted,Is.True);Assert.That(await session.SubmitCardActionAsync(command),Is.SameAs(result));Assert.That(session.ReadView().RemainingOps,Is.EqualTo(1));
            Assert.That((await session.SubmitCardActionAsync(new CardActionCommand("roll",v.GameId,v.Version,CardActionKind.RollRealignment,target:RealignmentTests.C("iran")))).Error,Is.EqualTo(RuleError.CommandIdConflict));Assert.That((await session.SubmitCardActionAsync(new CardActionCommand("stale",v.GameId,v.Version,CardActionKind.RollRealignment,target:RealignmentTests.C("angola")))).Error,Is.EqualTo(RuleError.StaleVersion));Assert.That(session.ReadView().Version,Is.EqualTo(result.View.Version));
        }
    }
}
