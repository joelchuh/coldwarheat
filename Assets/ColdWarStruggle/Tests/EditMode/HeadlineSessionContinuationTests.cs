using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using ColdWarStruggle.Core;
using ColdWarStruggle.Application;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class HeadlineSessionContinuationTests
    {
        static CountryId C(string n)=>new CountryId("country."+n);
        static IGameSession Owner(LocalGameHost h)=>h.UsSession.ReadView().Event.Owner==PlayerSide.US?h.UsSession:h.UssrSession;
        internal static EventCommandEnvelope Choice(IGameSession session,string id="choice")
        {
            var v=session.ReadView();var q=session.QueryEventTargets(v.Event.ExecutionId,v.Version);Assert.That(q.Error,Is.EqualTo(RuleError.None));
            if(q.Kind!=InfluenceDecisionKind.Allocation)return new EventCommandEnvelope(id,v.GameId,v.Version,EventCommandKind.Resolve,v.Event.ExecutionId,q.DecisionId,q.Targets.Take(q.RequiredTargets));
            var list=new List<InfluenceAllocation>();var remaining=q.RequiredAmount;
            foreach(var cap in q.Capacities){var n=Math.Min(remaining,cap.Amount);if(n>0)list.Add(new InfluenceAllocation(cap.Country,n));remaining-=n;}
            Assert.That(remaining,Is.Zero);return EventCommandEnvelope.Allocate(id,v.GameId,v.Version,v.Event.ExecutionId,q.DecisionId,list);
        }
        static LocalGameHost Pending(string us="decolonization",string su="nasser",Func<GameState,GameState> change=null)
        {
            var s=HeadlineFlowTests.Ready(us,su);if(change!=null)s=change(s);var h=new LocalGameHost(s,true);
            Assert.That(HeadlineSessionTests.Submit(h.UsSession,us).Accepted,Is.True);Assert.That(HeadlineSessionTests.Submit(h.UssrSession,su).Accepted,Is.True);return h;
        }
        static GameState France(GameState s,int us=0,int su=1)
        {var board=s.Countries.ToDictionary(x=>x.Key,x=>x.Value);board[C("france")]=new CountryState(us,su);return PlayInfluenceEventTests.Copy(s,board);}
        [Test] public void Valid_choice_publishes_current_and_following_effects_in_one_final_view()
        {
            var h=Pending();var session=Owner(h);var r=session.SubmitEventAsync(Choice(session)).Result;
            Assert.That(r.Accepted,Is.True,r.Error.ToString());Assert.That(r.View.Headline.Status,Is.EqualTo(PublicHeadlineStatus.Completed));Assert.That(r.View.Event,Is.Null);
            Assert.That(r.HeadlineSteps.Select(x=>x.Card),Is.EqualTo(new[]{HeadlineFlowTests.Id("decolonization"),HeadlineFlowTests.Id("nasser")}));
            Assert.That(r.HeadlineSteps[0].Event.Changes,Is.EqualTo(r.Changes));Assert.That(r.HeadlineSteps[1].Event.Changes,Is.Not.Empty);
            Assert.That(r.View.Practice.CompletedActions,Is.Zero);Assert.That(r.View.Countries.Single(x=>x.Definition.Id.Equals(C("egypt"))).USSR,Is.EqualTo(2));
        }
        [Test] public void Consecutive_owners_wait_and_historical_choice_replay_never_advances_again()
        {
            var h=Pending("decolonization","truman_doctrine",s=>France(s));var first=Owner(h);Assert.That(first.Side,Is.EqualTo(PlayerSide.USSR));var c=Choice(first,"first");
            var r=first.SubmitEventAsync(c).Result;Assert.That(r.Accepted,Is.True);Assert.That(r.View.Event.Owner,Is.EqualTo(PlayerSide.US));Assert.That(r.View.Headline.CompletedCount,Is.EqualTo(1));Assert.That(r.HeadlineSteps,Has.Count.EqualTo(2));Assert.That(r.HeadlineSteps[1].WaitingForChoice,Is.True);
            var second=Owner(h);var done=second.SubmitEventAsync(Choice(second,"second")).Result;Assert.That(done.Accepted,Is.True);Assert.That(done.HeadlineSteps,Has.Count.EqualTo(1));Assert.That(done.View.Headline.Status,Is.EqualTo(PublicHeadlineStatus.Completed));
            Assert.That(first.SubmitEventAsync(c).Result,Is.SameAs(r));Assert.That(first.ReadView().Version,Is.EqualTo(done.View.Version));Assert.That(first.ReadView().Headline.CompletedCount,Is.EqualTo(2));
        }
        [Test] public void Following_effect_failure_does_not_publish_raw_success_changes_or_markers()
        {
            var h=Pending(change:s=>{var b=s.Countries.ToDictionary(x=>x.Key,x=>x.Value);b[C("egypt")]=new CountryState(0,int.MaxValue);return PlayInfluenceEventTests.Copy(s,b);});
            var session=Owner(h);var before=session.ReadView();var c=Choice(session);var r=session.SubmitEventAsync(c).Result;
            Assert.That(r.Error,Is.EqualTo(RuleError.NumericOverflow));Assert.That(r.Changes,Is.Empty);Assert.That(r.HeadlineSteps,Is.Empty);Assert.That(r.ResolutionSequence,Is.Null);Assert.That(r.DecisionRequestedSequence,Is.Null);
            Assert.That(r.View.Version,Is.EqualTo(before.Version));Assert.That(r.View.Headline.CompletedCount,Is.Zero);Assert.That(r.View.Event.DecisionId,Is.EqualTo(before.Event.DecisionId));Assert.That(r.View.Countries.Select(x=>(x.US,x.USSR)),Is.EqualTo(before.Countries.Select(x=>(x.US,x.USSR))));
            Assert.That(session.SubmitEventAsync(c).Result,Is.SameAs(r));Assert.That(h.ReadLocalTestSnapshot().ReservedHeadlines[PlayerSide.USSR],Is.EqualTo(HeadlineFlowTests.Id("nasser")));
        }
        [Test] public void Completion_overflow_does_not_publish_choice_only_changes()
        {
            var source=PlayInfluenceEventTests.Copy(HeadlineResolutionTests.Ready("decolonization","nasser"),version:long.MaxValue-4);
            var h=new LocalGameHost(HeadlineResolutionTests.Begin(source).NextState,true);var session=Owner(h);var before=session.ReadView();
            var r=session.SubmitEventAsync(Choice(session)).Result;Assert.That(r.Error,Is.EqualTo(RuleError.NumericOverflow));Assert.That(r.Changes,Is.Empty);Assert.That(r.HeadlineSteps,Is.Empty);Assert.That(r.View.Version,Is.EqualTo(before.Version));
        }
        [TestCase("owner",RuleError.WrongActor)][TestCase("decision",RuleError.StaleDecision)][TestCase("version",RuleError.StaleVersion)]
        public void Invalid_choice_keeps_current_root_and_empty_public_steps(string kind,RuleError error)
        {
            var h=Pending();var owner=Owner(h);var c=Choice(owner);var before=owner.ReadView();var session=kind=="owner"?h.UsSession:owner;
            var bad=new EventCommandEnvelope(c.CommandId,c.GameId,kind=="version"?c.ExpectedVersion-1:c.ExpectedVersion,c.Kind,c.ExecutionId,kind=="decision"?"old":c.DecisionId,c.Targets);
            var r=session.SubmitEventAsync(bad).Result;Assert.That(r.Error,Is.EqualTo(error));Assert.That(r.HeadlineSteps,Is.Empty);Assert.That(r.Changes,Is.Empty);Assert.That(r.View.Version,Is.EqualTo(before.Version));
        }
        [Test] public void Published_first_headline_is_retained_when_second_choice_is_invalid()
        {
            var h=Pending("fidel","truman_doctrine",s=>France(s));var owner=Owner(h);var before=owner.ReadView();Assert.That(before.Headline.CompletedCount,Is.EqualTo(1));
            var bad=new EventCommandEnvelope("bad",before.GameId,before.Version,EventCommandKind.Resolve,before.Event.ExecutionId,before.Event.DecisionId,new[]{C("cuba")});var r=owner.SubmitEventAsync(bad).Result;
            Assert.That(r.Accepted,Is.False);Assert.That(r.HeadlineSteps,Is.Empty);Assert.That(r.View.Headline.CompletedCount,Is.EqualTo(1));Assert.That(r.View.Countries.Single(x=>x.Definition.Id.Equals(C("cuba"))).USSR,Is.EqualTo(3));
        }
        [Test] public void First_headline_nuclear_terminal_rejects_later_commands_and_preserves_second_card()
        {
            var h=new LocalGameHost(HeadlineFlowTests.Start(HeadlineFlowTests.Prepared("duck_and_cover","nasser",level:2)),true);
            HeadlineSessionTests.Submit(h.UsSession,"duck_and_cover");var r=HeadlineSessionTests.Submit(h.UssrSession,"nasser");
            Assert.That(r.Accepted,Is.True);Assert.That(r.View.Headline.Status,Is.EqualTo(PublicHeadlineStatus.Terminal));Assert.That(r.View.Practice.Winner,Is.EqualTo(PlayerSide.USSR));Assert.That(r.Steps.Single().Terminal,Is.True);
            Assert.That(h.ReadLocalTestSnapshot().ReservedHeadlines[PlayerSide.USSR],Is.EqualTo(HeadlineFlowTests.Id("nasser")));Assert.That(r.View.Countries.Single(x=>x.Definition.Id.Equals(C("egypt"))).USSR,Is.Zero);
            Assert.That(HeadlineSessionTests.Submit(h.UssrSession,"nasser","later").Error,Is.EqualTo(RuleError.GameAlreadyEnded));
        }
        [Test] public void Choice_then_second_nuclear_event_returns_both_steps_and_correct_responsibility()
        {
            var source=HeadlineFlowTests.Start(HeadlineFlowTests.Prepared("socialist_governments","duck_and_cover",level:2));source=France(source,2,0);var h=new LocalGameHost(source,true);
            HeadlineSessionTests.Submit(h.UsSession,"socialist_governments");HeadlineSessionTests.Submit(h.UssrSession,"duck_and_cover");var owner=Owner(h);var r=owner.SubmitEventAsync(Choice(owner)).Result;
            Assert.That(r.Accepted,Is.True);Assert.That(r.HeadlineSteps,Has.Count.EqualTo(2));Assert.That(r.HeadlineSteps[1].Terminal,Is.True);Assert.That(r.HeadlineSteps[1].DefconVpEvent.Terminal,Is.True);
            Assert.That(r.View.Practice.Winner,Is.EqualTo(PlayerSide.US));Assert.That(r.View.Headline.Status,Is.EqualTo(PublicHeadlineStatus.Terminal));Assert.That(r.View.Headline.CompletedCount,Is.EqualTo(1));Assert.That(r.View.Practice.CompletedActions,Is.Zero);
        }
        [Test] public void Event_cancellation_and_cross_type_receipt_conflict_do_not_consume_the_choice()
        {
            var h=Pending();var session=Owner(h);var c=Choice(session);using var token=new CancellationTokenSource();token.Cancel();
            Assert.That(session.SubmitEventAsync(c,token.Token).IsCanceled,Is.True);var before=session.ReadView();
            var conflict=new EventCommandEnvelope("headline",c.GameId,c.ExpectedVersion,c.Kind,c.ExecutionId,c.DecisionId,c.Targets);Assert.That(session.SubmitEventAsync(conflict).Result.Error,Is.EqualTo(RuleError.CommandIdConflict));
            Assert.That(session.ReadView().Version,Is.EqualTo(before.Version));Assert.That(session.SubmitEventAsync(c).Result.Accepted,Is.True);
        }
        [Test] public void Next_turn_host_replay_does_not_deal_twice_and_space_owner_sees_first_card()
        {
            // Synthetic always-success one-step milestone proves integration without guessing dice or internal mutation.
            var space=new SpaceRaceDefinition(0,1,1,1,new[]{new SpaceStepDefinition("space.one",1,1,1,1,0,0,"ability.headline"),new SpaceStepDefinition("space.two",2,1,1,1,0,0)},new[]{new SpaceAbilityDefinition("ability.headline",SpaceAbilityKind.HeadlineSelectionPolicy,headlinePolicy:"OpponentRevealsFirst")});
            var s=HeadlineFlowTests.Start(HeadlineFlowTests.Prepared(earlyOnly:true,space:space));s=HeadlineFlowTests.Choose(s,PlayerSide.US,"fidel");s=HeadlineFlowTests.Choose(s,PlayerSide.USSR,"romanian_abdication");
            s=PlayTurnOpsEventTests.Give(s,PlayerSide.USSR,"card.olympic_games");s=NextTurnPlayTests.Apply(s,CardActionKind.Influence,"card.olympic_games");s=NextTurnPlayTests.Apply(s,CardActionKind.FinishInfluence);
            s=PlayTurnOpsEventTests.Give(s,PlayerSide.US,"card.olympic_games");s=NextTurnPlayTests.Apply(s,CardActionKind.Space,"card.olympic_games");Assert.That(s.Practice.Space.Track.US,Is.EqualTo(1));s=NextTurnPlayTests.Assessed(s);
            var h=new LocalGameHost(s,true);var v=h.UsSession.ReadView();var next=new CardActionCommand("turn2",v.GameId,v.Version,CardActionKind.BeginNextTurn);var r=h.UsSession.SubmitCardActionAsync(next).Result;Assert.That(r.Accepted,Is.True);
            Assert.That(h.UsSession.SubmitCardActionAsync(next).Result,Is.SameAs(r));Assert.That(r.View.Headline.Status,Is.EqualTo(PublicHeadlineStatus.Collecting));Assert.That(r.View.Headline.Selection.CanChoose,Is.False);Assert.That(r.View.Headline.Order,Is.Empty);
            var us=h.UsSession.ReadView();var su=h.UssrSession.ReadView();Assert.That(su.Headline.Selection.CanChoose,Is.True);var card=su.Headline.Selection.Candidates.First();
            var first=h.UssrSession.SubmitHeadlineAsync(new HeadlineCommand("first",su.GameId,su.Version,card)).Result;Assert.That(first.Accepted,Is.True);
            var revealed=h.UsSession.ReadView();Assert.That(revealed.Headline.Selection.OpponentCard,Is.EqualTo(card));Assert.That(revealed.Headline.Selection.CanChoose,Is.True);
            var own=revealed.Headline.Selection.Candidates.First();Assert.That(h.UsSession.SubmitHeadlineAsync(new HeadlineCommand("second",revealed.GameId,revealed.Version,own)).Result.Accepted,Is.True);
            var current=h.UsSession.ReadView().Version;Assert.That(h.UsSession.SubmitCardActionAsync(next).Result,Is.SameAs(r));Assert.That(h.UsSession.ReadView().Version,Is.EqualTo(current));
        }
        [Test] public void Flat_ops_headlines_return_both_public_effects_without_normal_action_budget()
        {
            var h=HeadlineSessionTests.Host("containment","red_scare_purge");HeadlineSessionTests.Submit(h.UsSession,"containment");var r=HeadlineSessionTests.Submit(h.UssrSession,"red_scare_purge");
            Assert.That(r.Accepted,Is.True);Assert.That(r.Steps,Has.Count.EqualTo(2));Assert.That(r.Steps.All(x=>x.ActivatedOpsEffect!=null),Is.True);Assert.That(r.View.RemainingOps,Is.Zero);Assert.That(r.View.Practice.CompletedActions,Is.Zero);
        }
    }
}
