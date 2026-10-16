using System;
using ColdWarStruggle.Core;
using ColdWarStruggle.Application;
using ColdWarStruggle.Presentation;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class TurnEndFeedbackTests
    {
        static GameState SpaceBoundary(SpaceAbilityKind kind)
        {
            var space=new SpaceRaceDefinition(0,1,1,1,new[]{new SpaceStepDefinition("one",1,1,1,1,0,0,"ability")},
                new[]{new SpaceAbilityDefinition("ability",kind,kind==SpaceAbilityKind.ActionRoundAllowance?8:1)});
            var s=PlayTurnOpsEventTests.Give(NextTurnPlayTests.Ready(space:space),PlayerSide.USSR,"card.olympic_games");
            return NextTurnPlayTests.Complete(NextTurnPlayTests.Apply(s,CardActionKind.Space,"card.olympic_games"));
        }
        static DemoController Controller(GameState state,PlayerSide side=PlayerSide.US)
        {var host=new LocalGameHost(state,true);var c=new DemoController(_=>host);c.LoadScenario("feedback",side,ChinaCardPlayTests.C("iran"));return c;}
        [TestCase(PlayerSide.US)][TestCase(PlayerSide.USSR)]
        public void Extra_action_choice_explains_pending_assessment_without_exposing_hand(PlayerSide side)
        {
            var s=SpaceBoundary(SpaceAbilityKind.ActionRoundAllowance);var c=Controller(s,side);var v=c.ReadView();
            Assert.That(v.Practice.TurnEndAvailable,Is.False);Assert.That(v.Practice.TurnEndError,Is.EqualTo(RuleError.ActionNotReady));
            Assert.That(c.TurnEndPrompt,Does.Contain("太空").And.Contain("额外行动").And.Contain("放弃"));
            Assert.That(v.OwnHand,Is.EqualTo(s.GetHand(side)));Assert.That(v.Version,Is.EqualTo(s.Version));
        }
        [Test] public void Available_assessment_and_unfinished_actions_have_distinct_prompts()
        {
            var c=Controller(NextTurnPlayTests.Complete(NextTurnPlayTests.Ready()));Assert.That(c.ReadView().Practice.TurnEndError,Is.EqualTo(RuleError.None));
            Assert.That(c.TurnEndPrompt,Does.Contain("军事").And.Contain("扣留计分牌"));
            Assert.That(Controller(NextTurnPlayTests.Ready()).TurnEndPrompt,Is.Null);
        }
        [Test] public void Pending_end_turn_discard_is_named_after_successful_assessment()
        {
            var s=NextTurnPlayTests.Apply(SpaceBoundary(SpaceAbilityKind.EndTurnDiscardMaximum),CardActionKind.AssessTurnEnd);var c=Controller(s);
            Assert.That(c.ReadView().Practice.NextTurnAvailable,Is.False);Assert.That(c.ReadView().Practice.NextTurnError,Is.EqualTo(RuleError.ActionNotReady));
            Assert.That(c.TurnEndMessage,Does.Contain("太空").And.Contain("回合末弃牌").And.Contain("等待"));
        }
        [Test] public void Final_calendar_turn_reports_missing_final_scoring_instead_of_generic_boundary()
        {
            var cal=new TurnCalendar(new[]{new EraTurnParameters(GameEra.Early,1,1,8,1),new EraTurnParameters(GameEra.Mid,2,2,9,1),new EraTurnParameters(GameEra.Late,3,3,9,1)});
            var s=NextTurnPlayTests.Ready(calendar:cal);for(int i=1;i<=3;i++){s=NextTurnPlayTests.Assessed(s);if(i<3)s=NextTurnPlayTests.Next(s).NextState;}
            var c=Controller(s);Assert.That(c.ReadView().Practice.NextTurnError,Is.EqualTo(RuleError.ActionSliceComplete));
            Assert.That(c.TurnEndMessage,Does.Contain("最终计分").And.Contain("尚未接入"));Assert.That(s.Ended,Is.False);
        }
    }
}
