using System;
using System.Linq;
using System.Collections.Generic;
using ColdWarStruggle.Core;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class FinalScoringPlayTests
    {
        internal static ActionPracticeDefinition Definition(ActionPracticeDefinition d,bool enabled=true,int chinaVp=1,SpaceRaceDefinition space=null,bool nextTurn=true)
            =>new ActionPracticeDefinition(d.Cards,d.Ops,space??d.Space,d.Calendar,d.Risk,d.Scoring,d.InfluenceEvents,d.DefconVpEvents,d.TurnOpsEvents,d.Realignment,d.China,true,nextTurn,
                finalScoring:enabled?FinalScoringTests.Definition(d.Scoring.Map,chinaVp,d.Scoring):null);
        internal static GameState Ready(int vp=0,int turn=10,bool enabled=true,int chinaVp=1,PlayerSide china=PlayerSide.US,SpaceRaceDefinition space=null,bool nextTurn=true)
        {
            var baseState=NextTurnPlayTests.Ready();var d=Definition(baseState.Practice.Definition,enabled,chinaVp,space,nextTurn);
            var p=ActionPracticeState.CreateAtTurn(d,turn,vp);var board=RegionScoringTests.Empty(baseState.Map);
            var hands=new Dictionary<PlayerSide,IReadOnlyList<CardId>>();var used=new HashSet<CardId>();
            foreach(var side in new[]{PlayerSide.USSR,PlayerSide.US})
            {
                hands[side]=d.Cards.Cards.Where(c=>c.Kind==CardKind.Event && !used.Contains(c.Id)
                    && (c.Affiliation==CardAffiliation.Neutral || c.Affiliation==(side==PlayerSide.US?CardAffiliation.US:CardAffiliation.USSR)))
                    .Take(p.Space.Turn.Parameters.NormalActionsPerSide).Select(c=>c.Id).ToArray();
                foreach(var c in hands[side])used.Add(c);
            }
            var cards=new CardZones(d.Cards,d.Cards.Cards.Where(c=>c.Kind!=CardKind.China && !used.Contains(c.Id)).Select(c=>c.Id),hands,
                Array.Empty<CardId>(),Array.Empty<CardId>(),Array.Empty<CardId>(),Array.Empty<CardId>(),FinalScoringTests.China(china,CardFace.FaceDown),new[]{GameEra.Early,GameEra.Mid,GameEra.Late});
            var ops=OperationsContext.FromRoundStart("late-fixture",PlayerSide.USSR,0,baseState.Map,board,baseState.Operations.Costs);
            var s=new GameState(baseState.Map,board,ops,"final-fixture",turn:p.Space.Turn,cards:cards,randomState:baseState.RandomState,practice:p);
            return NextTurnPlayTests.Apply(s,CardActionKind.StartPractice);
        }
        internal static GameState Complete(GameState s)
        {
            while(s.Practice.CompletedActions<s.Turn.Parameters.NormalActionsPerSide*2)
            {var card=s.Cards.GetHand(s.Practice.Actor).First();s=NextTurnPlayTests.Apply(s,CardActionKind.Influence,card.Value);s=NextTurnPlayTests.Apply(s,CardActionKind.FinishInfluence);}
            return s;
        }
        internal static GameState Assessed(int vp=0,int chinaVp=1,PlayerSide china=PlayerSide.US)
            =>NextTurnPlayTests.Apply(Complete(Ready(vp:vp,chinaVp:chinaVp,china:china)),CardActionKind.AssessTurnEnd);
        internal static CardActionTransition Finish(GameState s,PlayerSide side=PlayerSide.US)=>ActionPracticeRules.Plan(s,side,CardActionKind.FinalScoring,"final");
        [TestCase(-2,-1,PlayerSide.USSR)] [TestCase(-1,0,null)] [TestCase(0,1,PlayerSide.US)] [TestCase(19,20,PlayerSide.US)]
        public void Final_batch_finishes_once_and_keeps_original_audit(int before,int after,PlayerSide? winner)
        {
            var s=Assessed(before);var r=Finish(s);Assert.That(r.Accepted,Is.True,r.Error.ToString());var n=r.NextState;
            Assert.That(n.Ended,Is.True);Assert.That(n.Practice.VpForUS,Is.EqualTo(after));Assert.That(n.Practice.Risk.Result.Winner,Is.EqualTo(winner));
            Assert.That(n.Practice.Risk.Result.Checkpoint,Is.EqualTo(VictoryCheckpoint.FinalScoringCompleted));Assert.That(n.Practice.FinalScoring.Decision,Is.SameAs(n.Practice.Risk.Result));
            Assert.That(n.Practice.FinalScoring.BeforeVp,Is.EqualTo(before));Assert.That(n.Practice.FinalScoring.Result.StateVersion,Is.EqualTo(s.Version));
            Assert.That(n.Practice.FinalScoring.Result.Regions.Count,Is.EqualTo(6));Assert.That(n.Practice.TurnEnd,Is.SameAs(s.Practice.TurnEnd));Assert.That(n.Practice.TurnEnd.Decision.SignedVp,Is.EqualTo(before));
            Assert.That(n.Version,Is.EqualTo(s.Version+1));Assert.That(n.EventSequence,Is.EqualTo(s.EventSequence+1));Assert.That(n.Practice.CompletedActions,Is.EqualTo(14));
            Assert.That(n.Cards,Is.SameAs(s.Cards));Assert.That(n.RandomState,Is.SameAs(s.RandomState));Assert.That(n.Countries,Is.EquivalentTo(s.Countries));Assert.That(s.Ended,Is.False);
            Assert.That(Finish(n).Error,Is.EqualTo(RuleError.GameAlreadyEnded));Assert.That(ActionPracticeRules.CanBeginNextTurn(n,PlayerSide.US),Is.EqualTo(RuleError.GameAlreadyEnded));
        }
        [TestCase(PlayerSide.US)] [TestCase(PlayerSide.USSR)] public void Europe_control_is_terminal_without_inventing_numeric_delta(PlayerSide winner)
        {
            var s=Ready(vp:winner==PlayerSide.US?-19:19);var board=s.Countries.ToDictionary(x=>x.Key,x=>x.Value);
            RegionScoringTests.Control(s.Map,board,winner,s.Map.GetCountriesInRegion("region.europe").Where(x=>x.Battleground).Select(x=>x.Id.Value).ToArray());
            s=NextTurnPlayTests.Apply(Complete(PlayScoringTests.Replace(s,board)),CardActionKind.AssessTurnEnd);
            var n=Finish(s).NextState;Assert.That(n,Is.Not.Null);Assert.That(n.Practice.Risk.Result.Winner,Is.EqualTo(winner));Assert.That(n.Practice.VpForUS,Is.EqualTo(s.Practice.VpForUS));Assert.That(n.Practice.FinalScoring.Result.NetVpForUS,Is.Null);
        }
        [Test] public void Early_unassessed_wrong_turn_and_unconfigured_are_rejected()
        {
            Assert.That(Finish(Ready()).Error,Is.EqualTo(RuleError.ActionNotReady));Assert.That(Finish(Complete(Ready())).Error,Is.EqualTo(RuleError.ActionNotReady));
            Assert.That(Finish(NextTurnPlayTests.Apply(Complete(Ready(turn:9)),CardActionKind.AssessTurnEnd)).Error,Is.EqualTo(RuleError.ActionNotReady));
            Assert.That(Finish(NextTurnPlayTests.Apply(Complete(Ready(enabled:false)),CardActionKind.AssessTurnEnd)).Error,Is.EqualTo(RuleError.UnsupportedCardAction));
        }
        [TestCase(SpaceAbilityKind.EndTurnDiscardMaximum,true)]
        [TestCase(SpaceAbilityKind.EndTurnDiscardMaximum,false)]
        [TestCase(SpaceAbilityKind.ActionRoundAllowance,true)]
        [TestCase(SpaceAbilityKind.ActionRoundAllowance,false)]
        public void Final_scoring_cannot_bypass_unhandled_space_entitlements(SpaceAbilityKind kind,bool nextTurn)
        {
            var space=new SpaceRaceDefinition(0,1,1,1,new[]{new SpaceStepDefinition("one",1,1,1,1,0,0,"ability")},new[]{new SpaceAbilityDefinition("ability",kind,kind==SpaceAbilityKind.ActionRoundAllowance?8:1)});
            var s=PlayTurnOpsEventTests.Give(Ready(space:space,nextTurn:nextTurn),PlayerSide.USSR,"card.olympic_games");
            s=NextTurnPlayTests.Apply(s,CardActionKind.Space,"card.olympic_games");s=Complete(s);
            if(kind==SpaceAbilityKind.ActionRoundAllowance)
            {
                Assert.That(ActionPracticeRules.CanAssessTurnEnd(s,PlayerSide.US),Is.EqualTo(RuleError.UnsupportedCardAction));
                Assert.That(Finish(s).Error,Is.EqualTo(RuleError.ActionNotReady));
            }
            else
            {
                s=PlayTurnOpsEventTests.Give(s,PlayerSide.USSR,"card.blockade");
                s=NextTurnPlayTests.Apply(s,CardActionKind.AssessTurnEnd);
                Assert.That(Finish(s).Error,Is.EqualTo(RuleError.ActionNotReady));
            }
            var version=s.Version;var cards=s.Cards;var random=s.RandomState;
            var rejected=Finish(s);Assert.That(rejected.Accepted,Is.False);Assert.That(rejected.NextState,Is.Null);
            Assert.That(s.Version,Is.EqualTo(version));Assert.That(s.Cards,Is.SameAs(cards));Assert.That(s.RandomState,Is.SameAs(random));Assert.That(s.Practice.FinalScoring,Is.Null);
        }
        [Test] public void Invalid_identity_or_command_payload_never_reaches_calculator()
        {
            Assert.That(ActionPracticeRules.CanCompleteFinalScoring(null,PlayerSide.US),Is.EqualTo(RuleError.InvalidCommand));var s=Assessed();
            Assert.That(Finish(s,(PlayerSide)99).Error,Is.EqualTo(RuleError.InvalidCommand));
            foreach(var id in new[]{"",new string('a',129)})Assert.That(ActionPracticeRules.Plan(s,PlayerSide.US,CardActionKind.FinalScoring,id).Error,Is.EqualTo(RuleError.InvalidCommand));
            Assert.That(ActionPracticeRules.Plan(s,PlayerSide.US,CardActionKind.FinalScoring,"x",new CardId("card.china")).Error,Is.EqualTo(RuleError.InvalidCommand));
            Assert.That(ActionPracticeRules.Plan(s,PlayerSide.US,CardActionKind.FinalScoring,"x",target:new CountryId("country.iran")).Error,Is.EqualTo(RuleError.InvalidCommand));
        }
        [TestCase(true)] [TestCase(false)] public void Publication_overflow_returns_no_partial_result(bool version)
        {var s=Assessed();s=PlayInfluenceEventTests.Copy(s,version:version?long.MaxValue:s.Version,sequence:version?s.EventSequence:long.MaxValue);var r=Finish(s);Assert.That(r.Error,Is.EqualTo(RuleError.NumericOverflow));Assert.That(r.NextState,Is.Null);Assert.That(s.Practice.FinalScoring,Is.Null);}
        [Test] public void Vp_overflow_returns_no_partial_result()
        {var s=Assessed(1,int.MaxValue);var r=Finish(s);Assert.That(r.Error,Is.EqualTo(RuleError.NumericOverflow));Assert.That(r.NextState,Is.Null);Assert.That(s.Ended,Is.False);}
        [Test] public void Changed_hand_after_audit_is_rejected()
        {var s=PlayTurnOpsEventTests.Give(Assessed(),PlayerSide.US,"card.europe_scoring");Assert.That(Finish(s).Error,Is.EqualTo(RuleError.StaleContext));}
        [Test] public void Same_number_but_foreign_turn_parameters_do_not_authorize_scoring()
        {
            var s=Assessed();var cal=new TurnCalendar(new[]{new EraTurnParameters(GameEra.Early,1,3,8,6),new EraTurnParameters(GameEra.Mid,4,7,9,7),new EraTurnParameters(GameEra.Late,8,10,9,8)});
            s=new GameState(s.Map,s.Countries,s.Operations,s.GameId,s.Version,s.SpentOps,s.EventSequence,turn:cal.ForTurn(10),cards:s.Cards,randomState:s.RandomState,practice:s.Practice);
            Assert.That(Finish(s).Error,Is.EqualTo(RuleError.StaleContext));
        }
        [TestCase(0)] [TestCase(11)] public void Scenario_initialization_rejects_out_of_calendar_turn(int turn)
        {var d=NextTurnPlayTests.Ready().Practice.Definition;Assert.Throws<ArgumentOutOfRangeException>(()=>ActionPracticeState.CreateAtTurn(d,turn,0));}
        [TestCase(-20)] [TestCase(20)] public void Scenario_initialization_rejects_already_terminal_vp(int vp)
        {var d=NextTurnPlayTests.Ready().Practice.Definition;Assert.Throws<ArgumentOutOfRangeException>(()=>ActionPracticeState.CreateAtTurn(d,10,vp));}
        [Test] public void Scenario_initialization_is_fresh_and_retains_default_turn_one_gate()
        {
            var d=NextTurnPlayTests.Ready().Practice.Definition;var p=ActionPracticeState.CreateAtTurn(d,10,7);
            Assert.That(p.Stage,Is.EqualTo(ActionPracticeStage.AwaitingStart));Assert.That(p.CompletedActions,Is.Zero);Assert.That(p.TurnEnd,Is.Null);Assert.That(p.FinalScoring,Is.Null);
            Assert.That(p.Space.Turn.Number,Is.EqualTo(10));Assert.That(p.Risk.Military.Turn.Number,Is.EqualTo(10));Assert.That(p.VpForUS,Is.EqualTo(7));
            Assert.That(ActionPracticeState.Create(d).Space.Turn.Number,Is.EqualTo(1));
        }
        [Test] public void Final_definition_must_bind_shared_scoring_turn_end_and_ten_turn_calendar()
        {
            var d=NextTurnPlayTests.Ready().Practice.Definition;var f=FinalScoringTests.Definition(d.Scoring.Map,regions:d.Scoring);
            Assert.Throws<ArgumentException>(()=>new ActionPracticeDefinition(d.Cards,d.Ops,d.Space,d.Calendar,d.Risk,d.Scoring,china:d.China,finalScoring:f));
            Assert.Throws<ArgumentException>(()=>new ActionPracticeDefinition(d.Cards,d.Ops,d.Space,d.Calendar,d.Risk,d.Scoring,china:d.China,turnEndEnabled:true,finalScoring:FinalScoringTests.Definition(d.Scoring.Map)));
            var cal=new TurnCalendar(new[]{new EraTurnParameters(GameEra.Early,1,1,8,6),new EraTurnParameters(GameEra.Mid,2,2,9,7),new EraTurnParameters(GameEra.Late,3,3,9,7)});
            Assert.Throws<ArgumentException>(()=>new ActionPracticeDefinition(d.Cards,d.Ops,d.Space,cal,d.Risk,d.Scoring,china:d.China,turnEndEnabled:true,finalScoring:f));
        }
    }
}
