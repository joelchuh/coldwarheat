using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using ColdWarStruggle.Core;
using ColdWarStruggle.Infrastructure;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class PlayInfluenceEventTests
    {
        internal static string Read(string path)=>File.ReadAllText(Path.Combine(DefinitionTests.Root,path));
        internal static PracticeInfluenceCatalog Effects(MapDefinition map,CardCatalog cards)
        {
            var first=EarlyInfluenceCatalogLoader.Load(Read("Data/Cards/deluxe-2015/early-influence-batch1.parameters.json"),Read("Docs/Contracts/early-influence-parameters.schema.json"),map);
            var data=Read("Data/Cards/deluxe-2015/early-influence-batch2.parameters.json");var schema=Read("Docs/Contracts/early-influence-batch2.schema.json");var refs=CardEffectReferenceIndex.Load(Read("Data/Cards/deluxe-2015/cards.json"));
            return new PracticeInfluenceCatalog(map,cards,first.Values.Concat(BudgetedRemovalCatalogLoader.Load(data,schema,map,refs).Values).Append(EraRemovalCatalogLoader.Load(data,schema,map,refs)));
        }
        internal static CountryId C(string n)=>new CountryId("country."+n);
        // Explicit all-era physical-card fixture reused from scoring, never the real default deal.
        internal static GameState Ready(string card="card.fidel")=>PlayScoringTests.Ready(card,configure:d=>new ActionPracticeDefinition(d.Cards,d.Ops,d.Space,d.Calendar,d.Risk,d.Scoring,Effects(d.Risk.Defcon.Map,d.Cards)));
        internal static CardActionTransition Play(GameState s,string card)=>ActionPracticeRules.Plan(s,s.Practice.Actor,CardActionKind.Event,"event",new CardId(card));
        internal static GameState Copy(GameState s,IReadOnlyDictionary<CountryId,CountryState> countries=null,long? version=null,long? sequence=null,CardZones cards=null)
            =>new GameState(s.Map,countries??s.Countries,s.Operations,s.GameId,version??s.Version,s.SpentOps,sequence??s.EventSequence,s.Ended,influenceEvent:s.InfluenceEvent,turn:s.Turn,deployment:s.Deployment,cards:cards??s.Cards,randomState:s.RandomState,practice:s.Practice);
        static void Completed(GameState before,GameState after,string card)
        {
            var id=new CardId(card);Assert.That(after.InfluenceEvent,Is.Null);Assert.That(after.Practice.CompletedActions,Is.EqualTo(before.Practice.CompletedActions+1));Assert.That(after.Cards.ResolvingCards,Is.Empty);
            Assert.That(after.Cards.GetHand(before.Practice.Actor),Has.No.Member(id));Assert.That(after.Cards.RemovedCards.Contains(id),Is.EqualTo(before.Practice.Definition.Cards.GetCard(id).RemoveAfterResolvedEvent));Assert.That(after.Cards.DiscardPile.Contains(id),Is.EqualTo(!before.Practice.Definition.Cards.GetCard(id).RemoveAfterResolvedEvent));
            Assert.That(after.Practice.Risk,Is.SameAs(before.Practice.Risk));Assert.That(after.Practice.Space,Is.SameAs(before.Practice.Space));Assert.That(after.Practice.VpForUS,Is.EqualTo(before.Practice.VpForUS));Assert.That(after.SpentOps,Is.Zero);Assert.That(after.Operations.Budget,Is.Zero);Pcg32RandomTests.Same(before.RandomState,after.RandomState);
        }
        [TestCase("fidel","cuba")][TestCase("romanian_abdication","romania")][TestCase("nasser","egypt")]
        public void Immediate_effect_uses_real_card_and_finishes_one_root_without_other_tracks(string card,string country)
        {
            var id="card."+card;var s=Ready(id);var board=s.Countries.ToDictionary(x=>x.Key,x=>x.Value);board[C(country)]=new CountryState(3,0);s=Copy(s,board);
            var result=Play(s,id);Assert.That(result.Accepted,Is.True);var n=result.NextState;Completed(s,n,id);Assert.That(n.Practice.Actor,Is.EqualTo(PlayerSide.US));
            Assert.That(n.GetInfluence(C(country),PlayerSide.US),Is.EqualTo(card=="nasser"?1:0));Assert.That(n.GetInfluence(C(country),PlayerSide.USSR),Is.EqualTo(card=="nasser"?2:s.Map.GetCountry(C(country)).Stability));
            Assert.That(result.EventTransition.NextState.InfluenceEvent.Outcome,Is.EqualTo(InfluenceEventStatus.ResolvedChange));Assert.That(s.GetInfluence(C(country),PlayerSide.US),Is.EqualTo(3));
        }
        [Test] public void No_change_starred_event_still_removes_card_and_completes()
        {
            var s=Ready();var b=s.Countries.ToDictionary(x=>x.Key,x=>x.Value);b[C("cuba")]=new CountryState(0,5);s=Copy(s,b);var p=Play(s,"card.fidel");Assert.That(p.Accepted,Is.True);Completed(s,p.NextState,"card.fidel");Assert.That(p.EventTransition.Changes,Is.Empty);Assert.That(p.EventTransition.NextState.InfluenceEvent.Outcome,Is.EqualTo(InfluenceEventStatus.ResolvedNoChange));
        }
        [TestCase("truman_doctrine")][TestCase("decolonization")][TestCase("socialist_governments")][TestCase("suez_crisis")][TestCase("east_european_unrest")]
        public void Choices_hold_the_physical_card_and_only_beneficiary_can_resolve(string card)
        {
            var id="card."+card;var s=Ready(id);var p=Play(s,id);Assert.That(p.Accepted,Is.True);var pending=p.NextState;var e=pending.InfluenceEvent;
            Assert.That(e.Stage,Is.EqualTo(InfluenceEventStage.AwaitingChoice));Assert.That(e.PhasingPlayer,Is.EqualTo(PlayerSide.USSR));Assert.That(pending.Practice.Stage,Is.EqualTo(ActionPracticeStage.ResolvingEvent));Assert.That(pending.Practice.CompletedActions,Is.Zero);Assert.That(pending.Cards.ResolvingCards,Is.EqualTo(new[]{new CardId(id)}));Assert.That(e.DecisionSourceVersion,Is.EqualTo(pending.Version));
            var query=InfluenceEventFlow.QueryDecision(pending,e.Owner,out var error);Assert.That(error,Is.EqualTo(RuleError.None));
            var other=e.Owner==PlayerSide.US?PlayerSide.USSR:PlayerSide.US;Assert.That(InfluenceEventFlow.Resolve(pending,other,e.DecisionId,Array.Empty<CountryId>()).Error,Is.EqualTo(RuleError.WrongActor));Assert.That(ActionPracticeRules.Plan(pending,PlayerSide.USSR,CardActionKind.FinishInfluence,"blocked").Error,Is.EqualTo(RuleError.EventInProgress));
            InfluenceEventTransition raw;
            if(query.Kind==InfluenceDecisionKind.Allocation)
            {var remaining=query.RequiredAmount;var allocations=new List<InfluenceAllocation>();foreach(var cap in query.Capacities){var amount=Math.Min(cap.Amount,remaining);if(amount>0)allocations.Add(new InfluenceAllocation(cap.Country,amount));remaining-=amount;}Assert.That(remaining,Is.Zero);raw=InfluenceEventFlow.ResolveAllocation(pending,e.Owner,e.DecisionId,allocations);}
            else raw=InfluenceEventFlow.Resolve(pending,e.Owner,e.DecisionId,query.LegalCountries.Take(query.RequiredTargets).ToArray());
            Assert.That(raw.Accepted,Is.True);var done=ActionPracticeRules.CompleteEventAction(raw);Assert.That(done.Accepted,Is.True);Completed(s,done.NextState,id);Assert.That(done.NextState.Practice.Actor,Is.EqualTo(PlayerSide.US));Assert.That(done.Changes,Is.EqualTo(raw.Changes));
        }
        [Test] public void Zero_available_removal_completes_without_fabricated_choice()
        {var s=Ready("card.suez_crisis");s=Copy(s,RegionScoringTests.Empty(s.Map));var p=Play(s,"card.suez_crisis");Assert.That(p.Accepted,Is.True);Completed(s,p.NextState,"card.suez_crisis");Assert.That(p.EventTransition.NextState.InfluenceEvent.Outcome,Is.EqualTo(InfluenceEventStatus.ResolvedNoChange));}
        [Test] public void Missing_configuration_unsupported_card_and_invalid_payload_do_not_move_cards()
        {
            var legacy=PlayScoringTests.Ready("card.fidel");Assert.That(Play(legacy,"card.fidel").Error,Is.EqualTo(RuleError.UnsupportedCardAction));var s=Ready("card.blockade");Assert.That(Play(s,"card.blockade").Error,Is.EqualTo(RuleError.UnsupportedCardAction));
            s=Ready();Assert.That(ActionPracticeRules.Plan(s,PlayerSide.USSR,CardActionKind.Event,"event").Error,Is.EqualTo(RuleError.InvalidCommand));Assert.That(ActionPracticeRules.Plan(s,PlayerSide.USSR,CardActionKind.Event,"event",new CardId("card.fidel"),C("cuba")).Error,Is.EqualTo(RuleError.InvalidCommand));Assert.That(ActionPracticeRules.Plan(s,PlayerSide.US,CardActionKind.Event,"event",new CardId("card.fidel")).Error,Is.EqualTo(RuleError.WrongActor));Assert.That(s.Cards.GetHand(PlayerSide.USSR),Does.Contain(new CardId("card.fidel")));
        }
        [TestCase(true)][TestCase(false)] public void Finalization_counter_overflow_discards_entire_immediate_transaction(bool version)
        {
            var s=Ready();var b=s.Countries.ToDictionary(x=>x.Key,x=>x.Value);b[C("cuba")]=new CountryState(0,5);s=Copy(s,b,version?long.MaxValue-2:(long?)null,version?null:(long?)long.MaxValue-2);
            var p=Play(s,"card.fidel");Assert.That(p.Error,Is.EqualTo(RuleError.NumericOverflow));Assert.That(p.NextState,Is.Null);Assert.That(s.Cards.RemovedCards,Is.Empty);Assert.That(s.Practice.CompletedActions,Is.Zero);Assert.That(s.Cards.GetHand(PlayerSide.USSR),Does.Contain(new CardId("card.fidel")));
        }
        [Test] public void Selection_finalization_overflow_does_not_publish_effect_only_state()
        {
            var s=Copy(Ready("card.truman_doctrine"),version:long.MaxValue-3);var pending=Play(s,"card.truman_doctrine").NextState;var e=pending.InfluenceEvent;var q=InfluenceEventFlow.QueryDecision(pending,e.Owner,out _);
            var raw=InfluenceEventFlow.Resolve(pending,e.Owner,e.DecisionId,q.LegalCountries.Take(q.RequiredTargets).ToArray());Assert.That(raw.Accepted,Is.True);var done=ActionPracticeRules.CompleteEventAction(raw);Assert.That(done.Error,Is.EqualTo(RuleError.NumericOverflow));Assert.That(done.NextState,Is.Null);Assert.That(pending.Practice.CompletedActions,Is.Zero);Assert.That(pending.Cards.ResolvingCards,Is.EqualTo(new[]{new CardId("card.truman_doctrine")}));
        }
        [Test] public void Ordinary_fixture_event_completion_is_not_absorbed_by_card_lifecycle()
        {
            var s=Ready();var definition=s.Practice.Definition.InfluenceEvents.Effects[new CardId("card.fidel")];var fixture=new GameState(s.Map,s.Countries,s.Operations,influenceEvent:new InfluenceEventExecution("fixture",definition,PlayerSide.USSR,InfluenceEventPolicy.NoActiveInterference));var raw=InfluenceEventFlow.Begin(fixture,PlayerSide.USSR);Assert.That(raw.Accepted,Is.True);Assert.That(ActionPracticeRules.CompleteEventAction(raw),Is.SameAs(raw));Assert.That(raw.NextState.InfluenceEvent.Stage,Is.EqualTo(InfluenceEventStage.Resolved));
        }
        [Test] public void Catalog_and_definition_reject_inconsistent_printed_binding_or_maps()
        {
            var s=Ready();var d=s.Practice.Definition;var list=d.InfluenceEvents.Effects.Values;
            Assert.That(list.Count(),Is.EqualTo(8));Assert.Throws<ArgumentException>(()=>new PracticeInfluenceCatalog(s.Map,d.Cards,list.Concat(list.Take(1))));
            Assert.Throws<ArgumentException>(()=>new PracticeInfluenceCatalog(s.Map,d.Cards,new[]{new ClearThenControlEffect("card.fidel","effect.nasser",PlayerSide.USSR,C("cuba"))}));
            Assert.Throws<ArgumentException>(()=>new PracticeInfluenceCatalog(s.Map,d.Cards,new[]{new ClearThenControlEffect("card.fidel","effect.fidel",PlayerSide.US,C("cuba"))}));
            Assert.Throws<ArgumentException>(()=>new ActionPracticeDefinition(d.Cards,d.Ops,d.Space,d.Calendar,d.Risk,d.Scoring,Effects(DefinitionTests.World(),d.Cards)));
        }
        [Test] public void Final_regular_slot_can_complete_an_opponent_event_without_starting_next_turn()
        {
            var s=Ready();var d=s.Practice.Definition;var final=new CardId("card.fidel");
            // Explicit all-era, large-hand fixture drives the real calendar to its final slot.
            var ordinary=d.Cards.Cards.Where(c=>c.Kind==CardKind.Event).ToArray();
            var hands=new Dictionary<PlayerSide,IReadOnlyList<CardId>>{
                {PlayerSide.USSR,ordinary.Where(c=>c.Affiliation==CardAffiliation.USSR && !c.Id.Equals(final)).Select(c=>c.Id).ToArray()},
                {PlayerSide.US,ordinary.Where(c=>c.Affiliation!=CardAffiliation.USSR || c.Id.Equals(final)).Select(c=>c.Id).ToArray()}};
            var zones=new CardZones(d.Cards,d.Cards.Cards.Where(c=>c.Kind==CardKind.Scoring).Select(c=>c.Id),hands,Array.Empty<CardId>(),Array.Empty<CardId>(),Array.Empty<CardId>(),Array.Empty<CardId>(),s.Cards.China,new[]{GameEra.Early,GameEra.Mid,GameEra.Late});s=Copy(s,cards:zones);
            var last=d.Calendar.ForTurn(1).Parameters.NormalActionsPerSide*2-1;
            for(int i=0;i<last;i++)
            {var actor=s.Practice.Actor;var id=s.Cards.GetHand(actor).First(c=>ActionPracticeRules.CanPlay(s,actor,c,CardActionKind.Influence)==RuleError.None);var begun=ActionPracticeRules.Plan(s,actor,CardActionKind.Influence,"ops-"+i,id);Assert.That(begun.Accepted,Is.True);var finished=ActionPracticeRules.Plan(begun.NextState,actor,CardActionKind.FinishInfluence,"finish-"+i);Assert.That(finished.Accepted,Is.True);s=finished.NextState;}
            Assert.That(s.Practice.Actor,Is.EqualTo(PlayerSide.US));var result=Play(s,final.Value);Assert.That(result.Accepted,Is.True);Completed(s,result.NextState,final.Value);Assert.That(result.NextState.Practice.Stage,Is.EqualTo(ActionPracticeStage.Complete));Assert.That(result.NextState.Turn.Number,Is.EqualTo(1));Assert.That(result.NextState.Ended,Is.False);
        }
    }
}
