using System.Threading.Tasks;
using ColdWarStruggle.Core;
using ColdWarStruggle.Application;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class SpaceExtraActionSessionTests
    {
        [TestCase(PlayerSide.US)] [TestCase(PlayerSide.USSR)]
        public async Task Identity_version_dedup_and_public_progress_are_enforced(PlayerSide owner)
        {
            var s=SpaceExtraActionPlayTests.Ready(owner,2);var h=new LocalGameHost(s,true);var own=owner==PlayerSide.US?h.UsSession:h.UssrSession;var other=owner==PlayerSide.US?h.UssrSession:h.UsSession;
            var v=own.ReadView();Assert.That(v.Practice.IsExtraAction,Is.True);Assert.That(v.Practice.CanDeclineExtraActions,Is.True);Assert.That(v.Practice.UsCompletedActions,Is.EqualTo(6));Assert.That(v.Practice.UssrCompletedActions,Is.EqualTo(6));Assert.That(v.Practice.RoundLimit,Is.EqualTo(8));
            Assert.That(other.ReadView().Practice.CanDeclineExtraActions,Is.False);Assert.That(other.ReadView().OwnHand,Is.EqualTo(s.Cards.GetHand(owner==PlayerSide.US?PlayerSide.USSR:PlayerSide.US)));
            var cmd=new CardActionCommand("decline",v.GameId,v.Version,CardActionKind.DeclineExtraActions);
            Assert.That((await other.SubmitCardActionAsync(cmd)).Error,Is.EqualTo(RuleError.WrongActor));
            Assert.That((await own.SubmitCardActionAsync(new CardActionCommand("old",v.GameId,v.Version-1,CardActionKind.DeclineExtraActions))).Error,Is.EqualTo(RuleError.StaleVersion));
            var done=await own.SubmitCardActionAsync(cmd);Assert.That(done.Accepted,Is.True,done.Error.ToString());Assert.That(await own.SubmitCardActionAsync(cmd),Is.SameAs(done));
            Assert.That(done.View.Practice.IsExtraAction,Is.False);Assert.That(done.View.Practice.CanDeclineExtraActions,Is.False);Assert.That(done.View.Version,Is.EqualTo(v.Version+1));
            Assert.That((await own.SubmitCardActionAsync(new CardActionCommand("decline",v.GameId,v.Version,CardActionKind.AssessTurnEnd))).Error,Is.EqualTo(RuleError.CommandIdConflict));
            Assert.That((await own.SubmitCardActionAsync(new CardActionCommand("again",v.GameId,done.View.Version,CardActionKind.DeclineExtraActions))).Error,Is.EqualTo(RuleError.ActionSliceComplete));
        }
    }
}
