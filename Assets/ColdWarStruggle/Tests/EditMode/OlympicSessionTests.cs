using System;
using System.Linq;
using System.Threading.Tasks;
using ColdWarStruggle.Core;
using ColdWarStruggle.Application;
using Newtonsoft.Json;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public sealed class OlympicSessionTests
    {
        static IGameSession Side(LocalGameHost h,PlayerSide side)=>side==PlayerSide.US?h.UsSession:h.UssrSession;
        static async Task<CardActionResult> Send(IGameSession s,CardActionKind kind,CardId? card=null,CountryId? country=null,string id="command")
            =>await s.SubmitCardActionAsync(new CardActionCommand(id,s.ReadView().GameId,s.ReadView().Version,kind,card,country));
        [TestCase(PlayerSide.US)][TestCase(PlayerSide.USSR)]
        public async Task Fixed_opponent_choice_and_public_contest_receipt_preserve_hidden_boundary(PlayerSide sponsor)
        {
            var h=new LocalGameHost(OlympicPlayTests.Ready(sponsor,0,5,6,1),true);var s=Side(h,sponsor);var o=Side(h,sponsor==PlayerSide.US?PlayerSide.USSR:PlayerSide.US);
            var begin=await Send(s,CardActionKind.Event,OlympicPlayTests.Card);Assert.That(begin.Accepted,Is.True);Assert.That(begin.Olympic.Outcome,Is.EqualTo(OlympicPracticeOutcome.AwaitingChoice));
            Assert.That(s.ReadView().Olympic.CanRespond,Is.False);Assert.That(o.ReadView().Olympic.CanRespond,Is.True);Assert.That(o.ReadView().Olympic.Sponsor,Is.EqualTo(sponsor));
            var wrong=await Send(s,CardActionKind.BoycottOlympic,id:"wrong-side");Assert.That(wrong.Error,Is.EqualTo(RuleError.WrongActor));
            var done=await Send(o,CardActionKind.ParticipateOlympic);Assert.That(done.Accepted,Is.True);Assert.That(done.Olympic.LastPair.Round,Is.EqualTo(1));Assert.That(done.Olympic.LastPair.Winner,Is.EqualTo(PlayerSide.US));Assert.That(done.Olympic.LastPair.AwardVp,Is.EqualTo(2));
            Assert.That(done.Olympic.VpForUS,Is.EqualTo(2));Assert.That(done.View.Olympic,Is.Null);Assert.That(done.Olympic.Terminal,Is.False);
            foreach(var dto in new object[]{begin.View.Olympic,done.Olympic})
            {var json=JsonConvert.SerializeObject(dto);foreach(var forbidden in new[]{"Random","SourceCards","GameState","DrawPile","Hand","Catalog","Continuation","Flow"})Assert.That(json,Does.Not.Contain(forbidden));}
        }
        [Test] public async Task Internal_pair_pump_never_replays_old_response_and_is_absent_from_player_interface()
        {
            Assert.That(typeof(IGameSession).GetMethods().Any(m=>m.Name=="AdvanceInternalOlympic"),Is.False);
            var h=new LocalGameHost(OlympicPlayTests.Ready(PlayerSide.US,0,5,1,3,6,1),true);await Send(h.UsSession,CardActionKind.Event,OlympicPlayTests.Card);
            var o=h.UssrSession;var v=o.ReadView();var cmd=new CardActionCommand("response",v.GameId,v.Version,CardActionKind.ParticipateOlympic);
            var tie=await o.SubmitCardActionAsync(cmd);Assert.That(tie.Olympic.Outcome,Is.EqualTo(OlympicPracticeOutcome.Tie));Assert.That(tie.View.Olympic.WaitingForInternal,Is.True);Assert.That(tie.View.Olympic.PendingOwner,Is.Null);
            Assert.That((await Send(o,CardActionKind.BoycottOlympic,id:"change-branch")).Accepted,Is.False);
            var done=h.AdvanceInternalOlympic(PlayerSide.US);Assert.That(done.Accepted,Is.True);Assert.That(done.Olympic.LastPair.Round,Is.EqualTo(2));Assert.That(done.View.Practice.VpForUS,Is.EqualTo(2));
            var current=o.ReadView();Assert.That(await o.SubmitCardActionAsync(cmd),Is.SameAs(tie));Assert.That(o.ReadView().Version,Is.EqualTo(current.Version));
            Assert.That((await o.SubmitCardActionAsync(new CardActionCommand(cmd.CommandId,cmd.GameId,cmd.ExpectedVersion,CardActionKind.BoycottOlympic))).Error,Is.EqualTo(RuleError.CommandIdConflict));
            Assert.That(h.AdvanceInternalOlympic(PlayerSide.USSR).Accepted,Is.False);Assert.That(o.ReadView().Version,Is.EqualTo(current.Version));
        }
        [TestCase("version",RuleError.StaleVersion)][TestCase("game",RuleError.WrongGame)][TestCase("payload",RuleError.InvalidCommand)]
        public async Task Stale_or_malformed_choice_does_not_consume_decision(string defect,RuleError error)
        {
            var h=new LocalGameHost(OlympicPlayTests.Ready(),true);await Send(h.UssrSession,CardActionKind.Event,OlympicPlayTests.Card);var s=h.UsSession;var v=s.ReadView();
            var cmd=new CardActionCommand("bad",defect=="game"?"foreign":v.GameId,defect=="version"?v.Version-1:v.Version,CardActionKind.ParticipateOlympic,defect=="payload"?OlympicPlayTests.Card:(CardId?)null);
            Assert.That((await s.SubmitCardActionAsync(cmd)).Error,Is.EqualTo(error));Assert.That(s.ReadView().Version,Is.EqualTo(v.Version));Assert.That(s.ReadView().Olympic.CanRespond,Is.True);
        }
        [Test] public async Task Queries_and_coup_execution_use_modified_event_budget_and_final_live_defcon()
        {
            var h=new LocalGameHost(OlympicOperationsTests.Pending(level:3,purge:true),true);var s=h.UssrSession;var v=s.ReadView();var options=s.QueryCardActions().PendingOperations;
            Assert.That(options.CurrentOps,Is.EqualTo(3));Assert.That(options.Influence,Is.EqualTo(RuleError.None));Assert.That(h.UsSession.QueryCardActions().PendingOperations.Influence,Is.EqualTo(RuleError.WrongActor));
            var target=new CountryId("country.angola");var quote=s.QueryCoupQuote(OlympicPlayTests.Card,target,v.Version);Assert.That(quote.Strength,Is.EqualTo(3));
            var r=await Send(s,CardActionKind.UseOlympicCoup,country:target);Assert.That(r.Accepted,Is.True);Assert.That(r.Coup.Strength,Is.EqualTo(3));Assert.That(r.Coup.MilitaryCredit,Is.EqualTo(3));
            Assert.That(r.Olympic.DefconBefore,Is.EqualTo(3));Assert.That(r.Olympic.DefconAfter,Is.EqualTo(1));Assert.That(r.Olympic.Terminal,Is.True);Assert.That(r.Olympic.Winner,Is.EqualTo(PlayerSide.US));
        }
        [Test] public async Task Influence_mode_query_has_event_budget_and_fixed_session_placement()
        {
            var h=new LocalGameHost(OlympicOperationsTests.Pending(),true);var s=h.UssrSession;Assert.That(s.QueryCardActions().PendingOperations.CurrentOps,Is.EqualTo(4));
            var r=await Send(s,CardActionKind.UseOlympicInfluence);Assert.That(r.Accepted,Is.True);Assert.That(r.View.RemainingOps,Is.EqualTo(4));Assert.That(r.View.Olympic.Mode,Is.EqualTo(OperationsMode.Influence));
            Assert.That(s.QueryCardActions().PendingOperations,Is.Null);Assert.That((await Send(s,CardActionKind.UseOlympicRealignment,id:"switch")).Accepted,Is.False);
            Assert.That((await Send(s,CardActionKind.FinishInfluence,id:"finish")).Olympic.Outcome,Is.EqualTo(OlympicPracticeOutcome.OperationsCompleted));
        }
        [Test] public void Invalid_internal_viewer_is_rejected_without_mutation()
        {
            var h=new LocalGameHost(OlympicPlayTests.Ready(),true);var before=h.UsSession.ReadView();Assert.Throws<ArgumentOutOfRangeException>(()=>h.AdvanceInternalOlympic((PlayerSide)99));Assert.That(h.UsSession.ReadView().Version,Is.EqualTo(before.Version));
        }
    }
}
