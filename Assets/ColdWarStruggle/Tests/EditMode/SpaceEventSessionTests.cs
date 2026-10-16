using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ColdWarStruggle.Core;
using ColdWarStruggle.Application;
using Newtonsoft.Json;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class SpaceEventSessionTests
    {
        [TestCase(PlayerSide.US)][TestCase(PlayerSide.USSR)] public async Task Fixed_session_projects_only_public_event_and_retries_without_second_reward(PlayerSide side)
        {
            var h=new LocalGameHost(SpaceEventPlayTests.Ready(side),true);var s=side==PlayerSide.US?h.UsSession:h.UssrSession;
            var cmd=WarSessionTests.Command(s,CardActionKind.Event,SpaceEventPlayTests.Card);var r=await s.SubmitCardActionAsync(cmd);Assert.That(r.Accepted,Is.True,r.Error.ToString());
            var e=r.SpaceEvent;Assert.That(e,Is.Not.Null);Assert.That(r.Space,Is.Null);Assert.That(e.Card,Is.EqualTo(SpaceEventPlayTests.Card));Assert.That(e.Beneficiary,Is.EqualTo(side));Assert.That(e.FromPosition,Is.Zero);Assert.That(e.ToPosition,Is.EqualTo(1));Assert.That(e.FirstArrival,Is.True);Assert.That(e.AwardVp,Is.EqualTo(2));Assert.That(e.NetVpForUS,Is.EqualTo(side==PlayerSide.US?2:-2));
            Assert.That(await s.SubmitCardActionAsync(cmd),Is.SameAs(r));Assert.That(s.ReadView().Version,Is.EqualTo(r.View.Version));Assert.That(s.ReadView().Practice.VpForUS,Is.EqualTo(e.NetVpForUS));
            var json=JsonConvert.SerializeObject(e);Assert.That(json,Does.Not.Contain("GameState").And.Not.Contain("Random").And.Not.Contain("Catalog").And.Not.Contain("DrawPile").And.Not.Contain("Request").And.Not.Contain("Hand"));
            Assert.Throws<NotSupportedException>(()=>((IList<string>)e.AfterUS.ActiveAbilityIds).Clear());
        }
        [Test] public async Task Result_contains_both_sides_ability_changes_when_second_arrival_cancels_advantage()
        {
            var h=new LocalGameHost(SpaceEventPlayTests.Ready(us:2,su:1),true);var s=h.UssrSession;var r=await s.SubmitCardActionAsync(WarSessionTests.Command(s,CardActionKind.Event,SpaceEventPlayTests.Card));var e=r.SpaceEvent;
            Assert.That(e,Is.Not.Null);Assert.That(e.BeforeUS.AttemptLimit,Is.EqualTo(2));Assert.That(e.AfterUS.AttemptLimit,Is.EqualTo(1));Assert.That(e.BeforeUSSR.AttemptLimit,Is.EqualTo(1));Assert.That(e.AfterUSSR.AttemptLimit,Is.EqualTo(1));Assert.That(e.FirstArrival,Is.False);Assert.That(e.AwardVp,Is.Zero);
        }
        [TestCase("identity",RuleError.WrongActor)][TestCase("version",RuleError.StaleVersion)][TestCase("game",RuleError.WrongGame)][TestCase("target",RuleError.InvalidCommand)]
        public async Task Rejected_command_does_not_move_card_track_or_version(string defect,RuleError expected)
        {
            var h=new LocalGameHost(SpaceEventPlayTests.Ready(),true);var s=defect=="identity"?h.UsSession:h.UssrSession;var v=s.ReadView();
            var cmd=new CardActionCommand("bad",defect=="game"?"wrong":v.GameId,defect=="version"?v.Version-1:v.Version,CardActionKind.Event,SpaceEventPlayTests.Card,defect=="target"?new CountryId("country.india"):(CountryId?)null);
            var r=await s.SubmitCardActionAsync(cmd);Assert.That(r.Error,Is.EqualTo(expected));Assert.That(r.SpaceEvent,Is.Null);Assert.That(s.ReadView().Version,Is.EqualTo(v.Version));Assert.That(s.ReadView().Practice.VpForUS,Is.Zero);Assert.That(h.UssrSession.ReadView().OwnHand,Has.Member(SpaceEventPlayTests.Card.Value));
        }
        [Test] public async Task Old_retry_and_conflicting_id_do_not_rewind_later_action()
        {
            var state=PlayTurnOpsEventTests.Give(SpaceEventPlayTests.Ready(),PlayerSide.US,"card.olympic_games");var h=new LocalGameHost(state,true);var s=h.UssrSession;var cmd=WarSessionTests.Command(s,CardActionKind.Event,SpaceEventPlayTests.Card);var first=await s.SubmitCardActionAsync(cmd);Assert.That(first.SpaceEvent,Is.Not.Null);
            var us=h.UsSession;Assert.That((await us.SubmitCardActionAsync(WarSessionTests.Command(us,CardActionKind.Influence,new CardId("card.olympic_games"),id:"later"))).Accepted,Is.True);var current=us.ReadView();Assert.That(await s.SubmitCardActionAsync(cmd),Is.SameAs(first));Assert.That(us.ReadView().Version,Is.EqualTo(current.Version));Assert.That(us.ReadView().Practice.VpForUS,Is.EqualTo(-2));
            var conflict=await s.SubmitCardActionAsync(new CardActionCommand(cmd.CommandId,cmd.GameId,cmd.ExpectedVersion,CardActionKind.Influence,cmd.Card));Assert.That(conflict.Error,Is.EqualTo(RuleError.CommandIdConflict));Assert.That(us.ReadView().Version,Is.EqualTo(current.Version));
        }
        [Test] public void Headline_receipt_projects_scientist_after_another_immediate_event()
        {
            var h=new LocalGameHost(SpaceEventHeadlineTests.Ready(),true);Assert.That(HeadlineSessionTests.Submit(h.UsSession,SpaceEventHeadlineTests.Scientist).Accepted,Is.True);var r=HeadlineSessionTests.Submit(h.UssrSession,"fidel");Assert.That(r.Accepted,Is.True);
            var step=r.Steps.Single(x=>x.Card.Equals(SpaceEventPlayTests.Card));Assert.That(step.SpaceEvent,Is.Not.Null);Assert.That(step.SpaceEvent.Beneficiary,Is.EqualTo(PlayerSide.US));Assert.That(step.WaitingForChoice,Is.False);Assert.That(step.Completion,Is.Not.Null);Assert.That(r.View.Practice.CompletedActions,Is.Zero);
        }
    }
}
