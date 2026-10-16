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
    public class WarSessionTests
    {
        static CardId Id(string n)=>WarPlayTests.Id(n);
        static CountryId C(string n)=>WarPlayTests.Country(n);
        internal static CardActionCommand Command(IGameSession s,CardActionKind kind,CardId? card=null,CountryId? target=null,string id="war-command")
        {var v=s.ReadView();return new CardActionCommand(id,v.GameId,v.Version,kind,card,target);}
        internal static async Task<LocalGameHost> Pending(PlayerSide side=PlayerSide.USSR)
        {
            var h=new LocalGameHost(WarPlayTests.Ready(actor:side),true);var s=side==PlayerSide.US?h.UsSession:h.UssrSession;
            Assert.That((await s.SubmitCardActionAsync(Command(s,CardActionKind.Event,Id("indo_pakistani_war"),id:"root"))).Accepted,Is.True);return h;
        }
        [TestCase(PlayerSide.US)][TestCase(PlayerSide.USSR)] public async Task War_waiting_view_is_public_but_only_bound_owner_can_confirm(PlayerSide owner)
        {
            var h=await Pending(owner);var us=h.UsSession.ReadView();var su=h.UssrSession.ReadView();Assert.That(us.War,Is.Not.Null);Assert.That(su.War,Is.Not.Null);
            Assert.That(us.War.Owner,Is.EqualTo(owner));Assert.That(us.War.CanChoose,Is.EqualTo(owner==PlayerSide.US));Assert.That(su.War.CanChoose,Is.EqualTo(owner==PlayerSide.USSR));Assert.That(us.War.Candidates,Is.EquivalentTo(new[]{C("india"),C("pakistan")}));Assert.That(su.War.Candidates,Is.EquivalentTo(us.War.Candidates));
            Assert.Throws<NotSupportedException>(()=>((IList<CountryId>)us.War.Candidates).Clear());
            var json=JsonConvert.SerializeObject(us.War);Assert.That(json,Does.Not.Contain("Random").And.Not.Contain("DrawPile").And.Not.Contain("Cards").And.Not.Contain("Countries"));
        }
        [TestCase("korean_war",true)][TestCase("arab_israeli_war",false)] public async Task Fixed_war_public_result_contains_closed_math_without_authority_state_and_retries_once(string name,bool win)
        {
            var initial=WarPlayTests.Ready(name,win:win);var h=new LocalGameHost(initial,true);var s=h.UssrSession;var cmd=Command(s,CardActionKind.Event,Id(name));var r=await s.SubmitCardActionAsync(cmd);
            Assert.That(r.Accepted,Is.True);Assert.That(r.War,Is.Not.Null);Assert.That(r.War.Card,Is.EqualTo(Id(name)));Assert.That(r.War.Success,Is.EqualTo(win));Assert.That(r.War.Roll,Is.EqualTo(win?6:1));Assert.That(r.War.AdjustedRoll,Is.EqualTo(r.War.Roll));Assert.That(r.War.WinningResult,Is.EqualTo(4));Assert.That(r.War.Beneficiary,Is.EqualTo(PlayerSide.USSR));Assert.That(r.War.MilitaryCredit,Is.EqualTo(2));Assert.That(r.War.MilitaryBefore,Is.Zero);Assert.That(r.War.MilitaryAfter,Is.EqualTo(2));Assert.That(r.War.VpAfter,Is.EqualTo(win?-2:0));Assert.That(r.War.Before.US,Is.EqualTo(3));Assert.That(r.War.After.US,Is.EqualTo(win?0:3));
            var json=JsonConvert.SerializeObject(r.War);Assert.That(json,Does.Not.Contain("Random").And.Not.Contain("DrawPile").And.Not.Contain("GameState").And.Not.Contain("Catalog"));
            Assert.That(await s.SubmitCardActionAsync(cmd),Is.SameAs(r));Assert.That(s.ReadView().Version,Is.EqualTo(r.View.Version));Assert.That(s.ReadView().Practice.UssrMilitary,Is.EqualTo(2));
        }
        [Test] public async Task Target_confirmation_and_parent_retries_never_roll_twice_or_restore_pending_state()
        {
            var h=new LocalGameHost(WarPlayTests.Ready(),true);var s=h.UssrSession;var root=Command(s,CardActionKind.Event,Id("indo_pakistani_war"),id:"root");var opened=await s.SubmitCardActionAsync(root);var cmd=Command(s,CardActionKind.ResolveWar,target:C("india"));var r=await s.SubmitCardActionAsync(cmd);
            Assert.That(r.Accepted,Is.True);Assert.That(r.War,Is.Not.Null);Assert.That(r.View.War,Is.Null);Assert.That(r.View.Practice.CompletedActions,Is.EqualTo(1));Assert.That(await s.SubmitCardActionAsync(cmd),Is.SameAs(r));Assert.That(await s.SubmitCardActionAsync(root),Is.SameAs(opened));Assert.That(s.ReadView().War,Is.Null);Assert.That(s.ReadView().Version,Is.EqualTo(r.View.Version));Assert.That(s.ReadView().Practice.UssrMilitary,Is.EqualTo(2));
            var conflict=await s.SubmitCardActionAsync(new CardActionCommand(cmd.CommandId,cmd.GameId,cmd.ExpectedVersion,cmd.Kind,target:C("pakistan")));Assert.That(conflict.Error,Is.EqualTo(RuleError.CommandIdConflict));Assert.That(s.ReadView().Version,Is.EqualTo(r.View.Version));
        }
        [TestCase("identity",RuleError.WrongActor)][TestCase("country",RuleError.InvalidSelection)][TestCase("version",RuleError.StaleVersion)][TestCase("game",RuleError.WrongGame)]
        public async Task Rejected_response_keeps_choice_board_and_tracks(string defect,RuleError expected)
        {
            var h=await Pending();var s=defect=="identity"?h.UsSession:h.UssrSession;var before=s.ReadView();var c=Command(s,CardActionKind.ResolveWar,target:C(defect=="country"?"israel":"india"));if(defect=="version" || defect=="game")c=new CardActionCommand(c.CommandId,defect=="game"?"other":c.GameId,defect=="version"?c.ExpectedVersion-1:c.ExpectedVersion,c.Kind,target:c.Target);
            var r=await s.SubmitCardActionAsync(c);Assert.That(r.Error,Is.EqualTo(expected));Assert.That(r.War,Is.Null);Assert.That(r.View.War,Is.Not.Null);Assert.That(r.View.Version,Is.EqualTo(before.Version));Assert.That(r.View.Practice.UssrMilitary,Is.Zero);Assert.That(r.View.Countries.Single(x=>x.Definition.Id.Equals(C("india"))).US,Is.EqualTo(3));
        }
        [Test] public async Task Coup_then_war_preserves_both_public_receipts()
        {
            var h=new LocalGameHost(WarPlayTests.Ready("korean_war",PlayerSide.US),true);var s=h.UsSession;Assert.That((await s.SubmitCardActionAsync(Command(s,CardActionKind.OpponentOpsFirst,Id("korean_war"),id:"root"))).Accepted,Is.True);
            var r=await s.SubmitCardActionAsync(Command(s,CardActionKind.UseOpponentCoup,target:C("south_korea")));Assert.That(r.Accepted,Is.True,r.Error.ToString());Assert.That(r.Coup,Is.Not.Null);Assert.That(r.War,Is.Not.Null);Assert.That(r.View.Practice.CompletedActions,Is.EqualTo(2));Assert.That(r.View.Practice.UsMilitary,Is.EqualTo(2));Assert.That(r.View.Practice.UssrMilitary,Is.EqualTo(2));
        }
        [Test] public async Task Headline_public_waiting_owner_and_response_steps_cover_war_then_following_choice()
        {
            var h=new LocalGameHost(WarContinuationTests.Headlines("indo_pakistani_war","truman_doctrine"),true);Assert.That(HeadlineSessionTests.Submit(h.UsSession,"indo_pakistani_war").Accepted,Is.True);var selected=HeadlineSessionTests.Submit(h.UssrSession,"truman_doctrine");Assert.That(selected.Accepted,Is.True);Assert.That(selected.View.War,Is.Not.Null);Assert.That(selected.View.Headline.WaitingOwner,Is.EqualTo(PlayerSide.US));Assert.That(selected.Steps.Single().WaitingForChoice,Is.True);
            var r=await h.UsSession.SubmitCardActionAsync(Command(h.UsSession,CardActionKind.ResolveWar,target:C("india")));Assert.That(r.Accepted,Is.True);Assert.That(r.War,Is.Not.Null);Assert.That(r.HeadlineSteps.Count,Is.EqualTo(2));Assert.That(r.HeadlineSteps[0].War,Is.Not.Null);Assert.That(r.HeadlineSteps[0].WaitingForChoice,Is.False);Assert.That(r.HeadlineSteps[1].WaitingForChoice,Is.True);Assert.That(r.View.Practice.CompletedActions,Is.Zero);
        }
    }
}
