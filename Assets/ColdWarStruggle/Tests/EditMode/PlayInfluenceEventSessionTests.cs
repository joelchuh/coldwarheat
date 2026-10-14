using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using ColdWarStruggle.Core;
using ColdWarStruggle.Application;
using ColdWarStruggle.Presentation;
using Newtonsoft.Json;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class PlayInfluenceEventSessionTests
    {
        static LocalGameHost Host(string card="card.fidel")=>new LocalGameHost(PlayInfluenceEventTests.Ready(card),true);
        static CardActionCommand Event(IGameSession s,string card,string id="event")
        {var v=s.ReadView();return new CardActionCommand(id,v.GameId,v.Version,CardActionKind.Event,new CardId(card));}
        static EventCommandEnvelope Choice(IGameSession s,string id="choice")
        {var v=s.ReadView();var q=s.QueryEventTargets(v.Event.ExecutionId,v.Version);return new EventCommandEnvelope(id,v.GameId,v.Version,EventCommandKind.Resolve,v.Event.ExecutionId,v.Event.DecisionId,q.Targets.Take(q.RequiredTargets));}
        [Test] public async Task Immediate_event_exposes_permission_result_and_repeat_receipt_without_private_state()
        {
            var h=Host();var s=h.UssrSession;Assert.That(s.QueryCardActions().Cards.Single(c=>c.Card.Value=="card.fidel").Event,Is.EqualTo(RuleError.None));var command=Event(s,"card.fidel");var result=await s.SubmitCardActionAsync(command);
            Assert.That(result.Accepted,Is.True);Assert.That(result.Event.Card.Value,Is.EqualTo("card.fidel"));Assert.That(result.Event.Stage,Is.EqualTo(InfluenceEventStage.Resolved));Assert.That(result.Event.Outcome,Is.EqualTo(InfluenceEventStatus.ResolvedChange));Assert.That(result.Event.Changes,Is.Not.Empty);Assert.That(result.View.Practice.CompletedActions,Is.EqualTo(1));
            var json=JsonConvert.SerializeObject(result.Event);foreach(var name in new[]{"Definition","RandomState","DrawCount","EffectId","Hands","NextState"})Assert.That(json,Does.Not.Contain(name));var full=JsonConvert.SerializeObject(result);foreach(var card in h.UsSession.ReadView().OwnHand)Assert.That(full,Does.Not.Contain(card));
            Assert.That(await s.SubmitCardActionAsync(command),Is.SameAs(result));Assert.That(s.ReadView().Version,Is.EqualTo(result.View.Version));
        }
        [Test] public async Task Opponent_owned_event_choice_and_card_disposal_share_one_commit_and_old_receipts_remain()
        {
            var h=Host("card.truman_doctrine");var start=Event(h.UssrSession,"card.truman_doctrine");var first=await h.UssrSession.SubmitCardActionAsync(start);Assert.That(first.Accepted,Is.True);Assert.That(first.Event.Stage,Is.EqualTo(InfluenceEventStage.AwaitingChoice));Assert.That(first.View.Event.Owner,Is.EqualTo(PlayerSide.US));Assert.That(first.View.Practice.Actor,Is.EqualTo(PlayerSide.USSR));
            var pending=h.UsSession.ReadView();Assert.That(h.UssrSession.QueryEventTargets(pending.Event.ExecutionId,pending.Version).Error,Is.EqualTo(RuleError.WrongActor));var choice=Choice(h.UsSession);var result=await h.UsSession.SubmitEventAsync(choice);
            Assert.That(result.Accepted,Is.True);Assert.That(result.View.Event,Is.Null);Assert.That(result.View.Cards.ResolvingCount,Is.Zero);Assert.That(result.View.Cards.RemovedCount,Is.EqualTo(1));Assert.That(result.View.Practice.CompletedActions,Is.EqualTo(1));Assert.That(result.View.Practice.Actor,Is.EqualTo(PlayerSide.US));Assert.That(result.ResolutionSequence,Is.Not.Null);
            var v=result.View;var card=h.UsSession.QueryCardActions().Cards.First(c=>c.Influence==RuleError.None).Card;Assert.That((await h.UsSession.SubmitCardActionAsync(new CardActionCommand("next",v.GameId,v.Version,CardActionKind.Influence,card))).Accepted,Is.True);var current=h.UsSession.ReadView().Version;
            Assert.That(await h.UsSession.SubmitEventAsync(choice),Is.SameAs(result));Assert.That(await h.UssrSession.SubmitCardActionAsync(start),Is.SameAs(first));Assert.That(h.UsSession.ReadView().Version,Is.EqualTo(current));
        }
        [Test] public async Task Allocation_is_whole_and_invalid_choice_does_not_consume_card_or_amount()
        {
            var h=Host("card.socialist_governments");var s=h.UssrSession;await s.SubmitCardActionAsync(Event(s,"card.socialist_governments"));var v=s.ReadView();var q=s.QueryEventTargets(v.Event.ExecutionId,v.Version);
            var bad=EventCommandEnvelope.Allocate("bad",v.GameId,v.Version,v.Event.ExecutionId,v.Event.DecisionId,new[]{new InfluenceAllocation(PlayInfluenceEventTests.C("iran"),q.RequiredAmount)});
            Assert.That((await s.SubmitEventAsync(bad)).Accepted,Is.False);Assert.That(s.ReadView().Version,Is.EqualTo(v.Version));Assert.That(s.ReadView().Cards.ResolvingCount,Is.EqualTo(1));
            var amount=q.RequiredAmount;var allocations=new List<InfluenceAllocation>();foreach(var cap in q.Capacities){var n=Math.Min(cap.Amount,amount);if(n>0)allocations.Add(new InfluenceAllocation(cap.Country,n));amount-=n;}
            var result=await s.SubmitEventAsync(EventCommandEnvelope.Allocate("valid",v.GameId,v.Version,v.Event.ExecutionId,v.Event.DecisionId,allocations));Assert.That(result.Accepted,Is.True);Assert.That(result.View.Event,Is.Null);Assert.That(result.View.Practice.CompletedActions,Is.EqualTo(1));Assert.That(result.View.Cards.DiscardCount,Is.EqualTo(1));Assert.That(result.Changes.Sum(c=>c.Before-c.After),Is.EqualTo(q.RequiredAmount));
        }
        [Test] public async Task Selection_finalization_overflow_rejects_entire_host_commit()
        {
            var state=PlayInfluenceEventTests.Copy(PlayInfluenceEventTests.Ready("card.truman_doctrine"),version:long.MaxValue-3);var h=new LocalGameHost(state,true);await h.UssrSession.SubmitCardActionAsync(Event(h.UssrSession,"card.truman_doctrine"));var pending=h.UsSession.ReadView();
            var result=await h.UsSession.SubmitEventAsync(Choice(h.UsSession));Assert.That(result.Error,Is.EqualTo(RuleError.NumericOverflow));var after=h.UsSession.ReadView();Assert.That(after.Version,Is.EqualTo(pending.Version));Assert.That(after.Countries.Select(c=>(c.US,c.USSR)),Is.EqualTo(pending.Countries.Select(c=>(c.US,c.USSR))));Assert.That(after.Cards.ResolvingCount,Is.EqualTo(1));Assert.That(after.Cards.RemovedCount,Is.Zero);
        }
        [Test] public async Task Event_envelopes_keep_game_version_and_cross_type_id_guards()
        {
            var h=Host();var s=h.UssrSession;var c=Event(s,"card.fidel");var result=await s.SubmitCardActionAsync(c);Assert.That(result.Accepted,Is.True);
            var conflict=await s.SubmitEventAsync(new EventCommandEnvelope(c.CommandId,c.GameId,c.ExpectedVersion,EventCommandKind.Begin,"practice.event.0"));Assert.That(conflict.Error,Is.EqualTo(RuleError.CommandIdConflict));var v=s.ReadView();
            Assert.That((await s.SubmitCardActionAsync(new CardActionCommand("stale",v.GameId,0,CardActionKind.Event,c.Card))).Error,Is.EqualTo(RuleError.StaleVersion));Assert.That((await s.SubmitCardActionAsync(new CardActionCommand("wrong","other-game",v.Version,CardActionKind.Event,c.Card))).Error,Is.EqualTo(RuleError.WrongGame));
        }
        static DemoController Controller(string card)
        {var c=new DemoController(_=>Host(card));c.LoadScenario("event",PlayerSide.USSR,PlayInfluenceEventTests.C("france"));return c;}
        [Test] public async Task Controller_can_play_event_resolve_allocation_and_clear_previous_result_for_next_card()
        {
            var c=Controller("card.socialist_governments");c.SelectCard(new CardId("card.socialist_governments"));Assert.That((await c.PlayCardAsync(CardActionKind.Event)).Accepted,Is.True);Assert.That(c.SelectedCard,Is.Null);
            var q=c.EventTargets();var remaining=q.RequiredAmount;foreach(var cap in q.Capacities){var n=Math.Min(cap.Amount,remaining);for(int i=0;i<n;i++)Assert.That(c.ChangeEventAllocation(cap.Country,1),Is.True);remaining-=n;}
            Assert.That(c.CanConfirmEvent,Is.True);Assert.That((await c.ConfirmEventAsync()).Accepted,Is.True);Assert.That(c.ReadView().Event,Is.Null);Assert.That(c.ReadView().Practice.Actor,Is.EqualTo(PlayerSide.US));Assert.That(c.LastEventResult.ResolutionSequence,Is.Not.Null);var version=c.ReadView().Version;await c.RetryEventAsync();Assert.That(c.ReadView().Version,Is.EqualTo(version));
            c.SelectSide(PlayerSide.US);var card=c.CardOptions().Cards.First(x=>x.Space==RuleError.None).Card;c.SelectCard(card);await c.PlayCardAsync(CardActionKind.Space);Assert.That(c.LastEventResult,Is.Null);
        }
        sealed class MapProbe:IMapView
        {
            public MapFrame Frame;public event Action<MapIntent> Intent;
            public void Initialize(MapViewContext context){} public bool Present(MapFrame f){Frame=f;return true;}public void FocusOn(CountryId c){}public void Clear(){Frame=null;}public void Dispose(){}
            public MapIntent Input(CountryId c)=>new MapIntent(Frame.ContextId,Frame.Version,Frame.Sequence,Frame.InteractionKey,c);
            public void Send(MapIntent i)=>Intent?.Invoke(i);
        }
        [Test] public async Task Map_choice_respects_new_owner_and_rejects_retired_intent_after_completion()
        {
            var c=Controller("card.truman_doctrine");c.SelectCard(new CardId("card.truman_doctrine"));await c.PlayCardAsync(CardActionKind.Event);c.SelectSide(PlayerSide.US);
            using var p=new MapPresenter(c,new Dictionary<string,string>());var map=new MapProbe();p.Attach(map);var target=c.EventTargets().Targets.First();var intent=map.Input(target);map.Send(intent);Assert.That(c.EventDraft,Is.EqualTo(new[]{target}));Assert.That((await c.ConfirmEventAsync()).Accepted,Is.True);
            p.Refresh();var version=c.ReadView().Version;map.Send(intent);Assert.That(c.EventDraft,Is.Empty);Assert.That(c.ReadView().Version,Is.EqualTo(version));Assert.That(c.ReadView().Event,Is.Null);
        }
    }
}
