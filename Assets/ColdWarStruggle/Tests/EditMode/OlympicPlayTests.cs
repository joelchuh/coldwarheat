using System;
using System.Linq;
using ColdWarStruggle.Core;
using ColdWarStruggle.Infrastructure;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public sealed class OlympicPlayTests
    {
        internal static readonly CardId Card=new CardId("card.olympic_games");
        internal static ActionPracticeDefinition Bind(ActionPracticeDefinition d,OlympicGamesDefinition olympic=null)
            =>new ActionPracticeDefinition(d.Cards,d.Ops,d.Space,d.Calendar,d.Risk,d.Scoring,d.InfluenceEvents,d.DefconVpEvents,d.TurnOpsEvents,d.Realignment,d.China,
                d.TurnEndEnabled,d.NextTurnEnabled,d.HeadlineParameters,d.HeadlineDefectors,d.FinalScoring,d.Blockade,d.FiveYearPlan,d.Wars,d.SpaceEvent,
                olympic??OlympicDefinitionTests.Load(cards:d.Cards));
        internal static GameState Ready(PlayerSide sponsor=PlayerSide.USSR,int vp=0,int level=5,params int[] faces)
        {
            var s=PlayScoringTests.Ready(Card.Value,vp,configure:d=>
            {
                var f=d.Risk.Defcon;var risk=new PracticeRiskDefinition(d.Risk.Coup,new DefconDefinition(f.Map,level,f.MaximumLevel,f.NuclearLevel,f.TurnStartImprovement,f.BattlegroundCoupDegradation,f.RestrictedActions,f.BlockedRegions),d.Risk.Military,d.Risk.Victory);
                return Bind(new ActionPracticeDefinition(d.Cards,d.Ops,d.Space,d.Calendar,risk,d.Scoring,turnOpsEvents:PlayTurnOpsEventTests.Effects(d.Cards),realignment:RealignmentTests.Definition()));
            });
            if(sponsor==PlayerSide.US)
            {s=PlayTurnOpsEventTests.Give(s,PlayerSide.US,Card.Value);s=PlayTurnOpsEventTests.Give(s,PlayerSide.USSR,"card.captured_nazi_scientist");s=SpaceEventPlayTests.Pass(s,"card.captured_nazi_scientist");}
            return faces.Length==0?s:PlayScoringTests.Replace(s,random:Dice(faces));
        }
        internal static RandomSnapshot Dice(params int[] faces)
        {
            // Locate a real PCG fixture matching literal faces; never substitute client-supplied dice in production.
            for(ulong seed=0;seed<2000000;seed++)
            {var r=Pcg32Random.FromSeed(seed,54);var before=r.Capture();if(faces.All(face=>r.NextInt(0,6)+1==face))return before;}
            throw new InvalidOperationException("Literal PCG fixture not found.");
        }
        internal static CardActionTransition Play(GameState s)=>ActionPracticeRules.Plan(s,s.Practice.Actor,CardActionKind.Event,"play-olympic",Card);
        internal static CardActionTransition Respond(GameState s,bool participate=true)
            =>ActionPracticeRules.Plan(s,s.Practice.Actor==PlayerSide.US?PlayerSide.USSR:PlayerSide.US,participate?CardActionKind.ParticipateOlympic:CardActionKind.BoycottOlympic,"olympic-response");
        [TestCase(PlayerSide.US)][TestCase(PlayerSide.USSR)]
        public void Real_hand_event_moves_only_its_card_and_waits_for_the_opponent(PlayerSide sponsor)
        {
            var s=Ready(sponsor);var r=Play(s);Assert.That(r.Accepted,Is.True,r.Error.ToString());var n=r.NextState;
            Assert.That(n.Practice.Olympic.Flow.Sponsor,Is.EqualTo(sponsor));Assert.That(n.Practice.Olympic.Flow.PendingOwner,Is.EqualTo(sponsor==PlayerSide.US?PlayerSide.USSR:PlayerSide.US));
            Assert.That(n.Cards.ResolvingCards.Single(),Is.EqualTo(Card));Assert.That(n.Cards.GetHand(sponsor),Has.No.Member(Card));Assert.That(s.Cards.GetHand(sponsor),Does.Contain(Card));
            Assert.That(n.Practice.CompletedActions,Is.EqualTo(s.Practice.CompletedActions));Assert.That(n.Version,Is.EqualTo(s.Version+1));Assert.That(n.Practice.Olympic.Flow.Revision,Is.EqualTo(n.Version));
            Assert.That(r.OlympicResult.Outcome,Is.EqualTo(OlympicPracticeOutcome.AwaitingChoice));Assert.That(n.Practice.Risk,Is.SameAs(s.Practice.Risk));Pcg32RandomTests.Same(n.RandomState,s.RandomState);
            Assert.That(ActionPracticeRules.CanRespondToOlympic(n,sponsor),Is.EqualTo(RuleError.WrongActor));Assert.That(ActionPracticeRules.CanRespondToOlympic(n,sponsor==PlayerSide.US?PlayerSide.USSR:PlayerSide.US),Is.EqualTo(RuleError.None));
        }
        [TestCase(PlayerSide.US,6,1,2)][TestCase(PlayerSide.USSR,1,6,-2)]
        [TestCase(PlayerSide.US,1,6,-2)][TestCase(PlayerSide.USSR,6,1,2)]
        public void Contest_applies_actual_winner_once_discards_source_and_completes_exactly_one_root(PlayerSide sponsor,int us,int su,int vp)
        {
            var s=Ready(sponsor,0,5,us,su);var started=Play(s).NextState;var r=Respond(started);Assert.That(r.Accepted,Is.True,r.Error.ToString());var n=r.NextState;
            Assert.That(n.Practice.VpForUS,Is.EqualTo(vp));Assert.That(n.Practice.CompletedActions,Is.EqualTo(s.Practice.CompletedActions+1));Assert.That(n.Practice.Actor,Is.Not.EqualTo(sponsor));Assert.That(n.Practice.Olympic,Is.Null);
            Assert.That(n.Cards.DiscardPile,Does.Contain(Card));Assert.That(n.Cards.RemovedCards,Has.No.Member(Card));Assert.That(n.Cards.ResolvingCards,Is.Empty);
            Assert.That(r.OlympicResult.Flow.LastPair.Plan.Calculation.UsRoll,Is.EqualTo(us));Assert.That(r.OlympicResult.Flow.LastPair.Plan.Calculation.UssrRoll,Is.EqualTo(su));Assert.That(r.OlympicResult.Outcome,Is.EqualTo(OlympicPracticeOutcome.ContestCompleted));
            Assert.That(n.Countries,Is.EquivalentTo(s.Countries));Assert.That(n.Practice.Risk.Military,Is.SameAs(s.Practice.Risk.Military));Assert.That(n.Practice.Space,Is.SameAs(s.Practice.Space));Assert.That(n.Practice.OpsEffects,Is.SameAs(s.Practice.OpsEffects));Assert.That(started.Practice.VpForUS,Is.Zero);
            Assert.That(Respond(n).Accepted,Is.False);Assert.That(ActionPracticeRules.PlanOlympicContinuation(n).Accepted,Is.False);
        }
        [TestCase(PlayerSide.US,19,6,1,21)][TestCase(PlayerSide.USSR,-19,1,6,-21)]
        public void VP_terminal_retains_resolving_audit_and_never_completes_the_root(PlayerSide sponsor,int vp,int us,int su,int after)
        {
            var s=Ready(sponsor,vp,5,us,su);var r=Respond(Play(s).NextState);Assert.That(r.Accepted,Is.True);var n=r.NextState;
            Assert.That(n.Ended,Is.True);Assert.That(n.Practice.VpForUS,Is.EqualTo(after));Assert.That(n.Practice.Risk.Result.Winner,Is.EqualTo(sponsor));Assert.That(n.Practice.CompletedActions,Is.EqualTo(s.Practice.CompletedActions));Assert.That(n.Cards.ResolvingCards.Single(),Is.EqualTo(Card));
            Assert.That(ActionPracticeRules.PlanOlympicContinuation(n).Error,Is.EqualTo(RuleError.GameAlreadyEnded));Assert.That(Respond(n).Error,Is.EqualTo(RuleError.GameAlreadyEnded));
        }
        [Test] public void First_tie_commits_both_dice_then_internal_continuation_finishes_from_that_cursor()
        {
            var s=Ready(PlayerSide.US,0,5,1,3,6,1);var a=Respond(Play(s).NextState);Assert.That(a.Accepted,Is.True);var tie=a.NextState;
            Assert.That(tie.Practice.Olympic.Flow.Stage,Is.EqualTo(OlympicResolutionStage.Rolling));Assert.That(tie.Practice.Olympic.Flow.PendingOwner,Is.Null);Assert.That(tie.RandomState.DrawCount,Is.EqualTo(2));Assert.That(tie.Practice.CompletedActions,Is.EqualTo(s.Practice.CompletedActions));
            Assert.That(Respond(tie,false).Accepted,Is.False);Assert.That(Respond(tie).Accepted,Is.False);
            var final=ActionPracticeRules.PlanOlympicContinuation(tie);Assert.That(final.Accepted,Is.True,final.Error.ToString());Assert.That(final.NextState.RandomState.DrawCount,Is.EqualTo(4));Assert.That(final.NextState.Practice.VpForUS,Is.EqualTo(2));
            Assert.That(final.OlympicResult.Flow.LastPair.RoundIndex,Is.EqualTo(2));Assert.That(final.OlympicResult.Flow.LastPair.Previous.Plan.Calculation.Tie,Is.True);Assert.That(tie.RandomState.DrawCount,Is.EqualTo(2));
        }
        [TestCase("actor")][TestCase("card")][TestCase("target")][TestCase("id")]
        public void Bad_response_payload_has_no_effect(string fault)
        {
            var s=Play(Ready()).NextState;var r=ActionPracticeRules.Plan(s,fault=="actor"?PlayerSide.USSR:PlayerSide.US,CardActionKind.ParticipateOlympic,fault=="id"?" ":"response",fault=="card"?Card:(CardId?)null,fault=="target"?new CountryId("country.france"):(CountryId?)null);
            Assert.That(r.Accepted,Is.False);Assert.That(r.NextState,Is.Null);Assert.That(s.Practice.Olympic.Flow.Stage,Is.EqualTo(OlympicResolutionStage.AwaitingOpponent));
        }
        [TestCase("game")][TestCase("version")][TestCase("random")][TestCase("board")]
        public void A_foreign_authority_snapshot_cannot_answer_the_saved_decision(string fault)
        {
            var s=Play(Ready()).NextState;var board=s.Countries.ToDictionary(x=>x.Key,x=>x.Value);
            if(fault=="board")board[new CountryId("country.france")]=new CountryState(12,0);
            var forged=new GameState(s.Map,board,s.Operations,fault=="game"?"other":s.GameId,fault=="version"?s.Version+1:s.Version,s.SpentOps,s.EventSequence,turn:s.Turn,cards:s.Cards,randomState:fault=="random"?Pcg32Random.FromSeed(99,54).Capture():s.RandomState,practice:s.Practice);
            Assert.That(Respond(forged).Error,Is.EqualTo(RuleError.StaleContext));
        }
        [TestCase(PlayerSide.US)][TestCase(PlayerSide.USSR)]
        public void Boycott_nuclear_loss_is_the_original_sponsor_and_has_no_ops_or_root_cleanup(PlayerSide sponsor)
        {
            var s=Ready(sponsor,0,2);var r=Respond(Play(s).NextState,false);Assert.That(r.Accepted,Is.True);var n=r.NextState;
            Assert.That(n.Ended,Is.True);Assert.That(n.Practice.Risk.Defcon.Level,Is.EqualTo(1));Assert.That(n.Practice.Risk.Result.Winner,Is.Not.EqualTo(sponsor));Assert.That(n.Operations.Budget,Is.Zero);
            Assert.That(n.Practice.CompletedActions,Is.EqualTo(s.Practice.CompletedActions));Assert.That(n.Cards.ResolvingCards.Single(),Is.EqualTo(Card));Assert.That(r.OlympicResult.Flow.Operations,Is.Null);Pcg32RandomTests.Same(s.RandomState,n.RandomState);
        }
        [TestCase(PlayerSide.US)][TestCase(PlayerSide.USSR)]
        public void Surviving_boycott_commits_before_waiting_for_sponsor_operations(PlayerSide sponsor)
        {
            var s=Ready(sponsor,0,3);var r=Respond(Play(s).NextState,false);Assert.That(r.Accepted,Is.True);var n=r.NextState;
            Assert.That(n.Practice.Stage,Is.EqualTo(ActionPracticeStage.ChoosingOperations));Assert.That(n.Practice.Olympic.Flow.PendingOwner,Is.EqualTo(sponsor));Assert.That(n.Practice.Risk.Defcon.Level,Is.EqualTo(2));Assert.That(n.Practice.VpForUS,Is.Zero);
            Assert.That(n.Operations.Budget,Is.Zero);Assert.That(n.Practice.CompletedActions,Is.EqualTo(s.Practice.CompletedActions));Assert.That(Respond(n).Accepted,Is.False);Assert.That(ActionPracticeRules.PlanOlympicContinuation(n).Accepted,Is.False);Pcg32RandomTests.Same(n.RandomState,s.RandomState);
        }
        [Test] public void Definition_binding_rejects_foreign_catalog_and_missing_risk()
        {
            var d=Ready().Practice.Definition;Assert.Throws<ArgumentException>(()=>Bind(d,OlympicDefinitionTests.Load()));
            Assert.Throws<ArgumentException>(()=>new ActionPracticeDefinition(d.Cards,d.Ops,d.Space,d.Calendar,olympic:d.Olympic));
        }
        [Test] public void Olympic_is_not_yet_registered_as_a_headline_candidate()
        {var d=Ready().Practice.Definition;Assert.That(HeadlineResolutionRules.SupportedCards(d),Has.No.Member(Card));}
        [Test] public void Null_or_unrelated_state_has_no_internal_work()
        {
            Assert.That(ActionPracticeRules.PlanOlympicContinuation(null).Error,Is.EqualTo(RuleError.InvalidCommand));
            Assert.That(ActionPracticeRules.CanRespondToOlympic(null,PlayerSide.US),Is.EqualTo(RuleError.InvalidCommand));
            Assert.That(ActionPracticeRules.PlanOlympicContinuation(Ready()).Error,Is.EqualTo(RuleError.ActionNotReady));
        }
    }
}
