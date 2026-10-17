using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ColdWarStruggle.Core;
using ColdWarStruggle.Infrastructure;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public sealed class EventFactStateTests
    {
        static InfluenceEventFactDefinition Definition()=>EventFactFoundationTests.Definition();
        static GameState Board(InfluenceEventFactDefinition d)=>EventFactFoundationTests.Board(d);
        static InfluenceEventTransition Complete(GameState s)=>EventFactFoundationTests.Complete(s);
        static GameState Bind(GameState s,EventFactLedger ledger,long? version=null,long? sequence=null)
            =>new GameState(s.Map,s.Countries,s.Operations,s.GameId,version??s.Version,s.SpentOps,sequence??s.EventSequence,s.Ended,
                influenceEvent:s.InfluenceEvent,turn:s.Turn,deployment:s.Deployment,cards:s.Cards,randomState:s.RandomState,practice:s.Practice,japanPact:s.JapanPact,eventFacts:ledger);
        // Bounded calls to the existing internal snapshot-copy seam; no production reflection.
        static GameState Call(GameState s,string method,params object[] args)
        {
            try{return (GameState)typeof(GameState).GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(s,args);}
            catch(TargetInvocationException e){throw e.InnerException;}
        }
        static GameState Attach(GameState s,EventFactLedger ledger)=>Call(s,"WithEventFacts",ledger);
        static EventFactLedger Record(InfluenceEventFactDefinition d,EventFactLedger ledger,string id,long version,long sequence)
        {
            var r=ledger.RecordCompleted(Complete(EventFactFoundationTests.Board(d,id:id,version:version,sequence:sequence)));
            Assert.That(r.Accepted,Is.True,r.Error.ToString());return r.After;
        }
        static GameState Recorded()
        {
            var d=Definition();var raw=Complete(Board(d));var ledger=EventFactLedger.Create(d,raw.NextState.GameId,true).RecordCompleted(raw).After;
            return Bind(raw.NextState,ledger);
        }
        [Test] public void Legacy_unconfigured_state_stays_null_without_inventing_known_empty_history()
        {Assert.That(Board(Definition()).EventFacts,Is.Null);}
        [TestCase(true)][TestCase(false)] public void Explicit_empty_history_keeps_its_definition_and_knowledge(bool known)
        {
            var d=Definition();var s=Board(d);var ledger=EventFactLedger.Create(d,s.GameId,known);var n=Bind(s,ledger);
            Assert.That(n.EventFacts,Is.SameAs(ledger));Assert.That(n.EventFacts.HistoryKnown,Is.EqualTo(known));Assert.That(s.EventFacts,Is.Null);
        }
        [TestCase("game")][TestCase("map")][TestCase("catalog")][TestCase("missing-cards")][TestCase("version")][TestCase("sequence")]
        public void Constructor_rejects_foreign_or_future_evidence(string fault)
        {
            var s=Recorded();Assert.That(s.EventFacts,Is.Not.Null);var f=s.EventFacts.Facts.Single();
            var cards=fault=="missing-cards"?null:fault=="catalog"?FiveYearPlanRulesTests.Zones(CardCatalogLoaderTests.Load(),Array.Empty<string>(),parentResolving:false):s.Cards;
            Assert.That(()=>new GameState(fault=="map"?DefinitionTests.World():s.Map,s.Countries,s.Operations,fault=="game"?"other":s.GameId,
                fault=="version"?f.CommittedVersion-1:s.Version,eventSequence:fault=="sequence"?f.CommitSequence-1:s.EventSequence,
                cards:cards,randomState:cards==null?null:s.RandomState,eventFacts:s.EventFacts),Throws.InstanceOf<ArgumentException>());
        }
        [Test] public void Genuine_flow_append_attaches_without_increment_and_retry_is_identity()
        {
            var d=Definition();var s=Board(d);var empty=EventFactLedger.Create(d,s.GameId,true);s=Bind(s,empty);Assert.That(s.EventFacts,Is.SameAs(empty));
            var raw=Complete(s);Assert.That(raw.NextState.EventFacts,Is.SameAs(empty));var r=empty.RecordCompleted(raw);Assert.That(r.Accepted,Is.True);
            var n=Attach(raw.NextState,r.After);Assert.That(n,Is.Not.Null);Assert.That(n.EventFacts,Is.SameAs(r.After));Assert.That(n.EventFacts.Facts.Count,Is.EqualTo(1));
            Assert.That(n.Version,Is.EqualTo(raw.NextState.Version));Assert.That(n.EventSequence,Is.EqualTo(raw.NextState.EventSequence));
            Assert.That(n.Countries,Is.EqualTo(raw.NextState.Countries));Assert.That(n.Cards,Is.SameAs(raw.NextState.Cards));Assert.That(n.RandomState,Is.SameAs(raw.NextState.RandomState));
            Assert.That(n.Operations,Is.SameAs(raw.NextState.Operations));Assert.That(n.InfluenceEvent,Is.SameAs(raw.NextState.InfluenceEvent));Assert.That(s.EventFacts.Facts,Is.Empty);
            Assert.That(Attach(n,n.EventFacts),Is.SameAs(n));
        }
        [TestCase("null")][TestCase("clear")][TestCase("unknown")][TestCase("definition")][TestCase("game")][TestCase("rewrite")][TestCase("batch")]
        public void Internal_copy_cannot_reset_rewrite_or_batch_replace_history(string fault)
        {
            var s=Recorded();Assert.That(s.EventFacts,Is.Not.Null);var d=s.EventFacts.Definition;var prior=s.EventFacts;
            var replacement=fault=="null"?null:fault=="definition"?EventFactLedger.Create(Definition(),s.GameId,true)
                :fault=="game"?EventFactLedger.Create(d,"other",true):EventFactLedger.Create(d,s.GameId,fault!="unknown");
            if(fault=="rewrite")replacement=Record(d,Record(d,replacement,"resolution.1",3,7),"second",20,30);
            if(fault=="batch")replacement=Record(d,Record(d,prior,"second",20,30),"third",40,60);
            s=Bind(s,prior,version:100,sequence:100);
            Assert.That(()=>Attach(s,replacement),Throws.InstanceOf<ArgumentException>());Assert.That(s.EventFacts,Is.SameAs(prior));Assert.That(prior.Facts.Count,Is.EqualTo(1));
        }
        [Test] public void Unknown_history_cannot_be_relabelled_known_by_an_append()
        {
            var d=Definition();var s=Bind(Board(d),EventFactLedger.Create(d,"facts",false),100,100);
            var replacement=Record(d,EventFactLedger.Create(d,"facts",true),"new",20,30);
            Assert.That(()=>Attach(s,replacement),Throws.InstanceOf<ArgumentException>());
        }
        [Test] public void Unconfigured_state_cannot_gain_a_ledger_through_copy_helper()
        {var d=Definition();var s=Board(d);Assert.That(()=>Attach(s,EventFactLedger.Create(d,s.GameId,true)),Throws.InstanceOf<ArgumentException>());}
        [TestCase("Apply")][TestCase("ApplyRealignment")][TestCase("WithPractice")][TestCase("WithNextPracticeTurn")][TestCase("WithInfluenceEvent")][TestCase("WithDeployment")]
        public void Every_state_copy_retains_same_immutable_ledger(string method)
        {
            var s=Recorded();Assert.That(s.EventFacts,Is.Not.Null);var turn=PlayScoringTests.Ready().Practice.Definition.Calendar.ForTurn(2);
            object[] args=method=="Apply"?new object[]{s.Countries,0,1,null}:method=="ApplyRealignment"?new object[]{s.Countries,0,s.RandomState,null}
                :method=="WithPractice"?new object[]{null,s.Operations,s.Cards,s.RandomState,null,null,null}
                :method=="WithNextPracticeTurn"?new object[]{null,s.Operations,s.Cards,s.RandomState,turn}:new object[]{null};
            var n=Call(s,method,args);Assert.That(n.EventFacts,Is.SameAs(s.EventFacts));Assert.That(n.EventFacts.Facts.Single(),Is.SameAs(s.EventFacts.Facts.Single()));
            if(method=="WithNextPracticeTurn"){Assert.That(n.Turn.Number,Is.EqualTo(2));Assert.That(n.InfluenceEvent,Is.Null);}
        }
        [Test] public void Ordinary_card_action_and_finish_preserve_existing_fact_without_recording_ops_play()
        {
            var s=PlayInfluenceEventTests.Ready();var effect=s.Practice.Definition.InfluenceEvents.Effects[new CardId("card.fidel")];
            var d=EventFactDefinitionLoader.Load(PlayInfluenceEventTests.Read("Data/Cards/deluxe-2015/treaty-protection.parameters.json"),PlayInfluenceEventTests.Read("Docs/Contracts/treaty-protection.schema.json"),s.Map,s.Cards.Catalog,new[]{effect});
            var raw=Complete(new GameState(s.Map,s.Countries,s.Operations,s.GameId,cards:s.Cards,randomState:s.RandomState,
                influenceEvent:new InfluenceEventExecution("prior-fidel",effect,PlayerSide.USSR,InfluenceEventPolicy.NoActiveInterference)));
            var ledger=EventFactLedger.Create(d,s.GameId,true).RecordCompleted(raw).After;s=Bind(s,ledger,40,40);Assert.That(s.EventFacts,Is.SameAs(ledger));
            var begun=ActionPracticeRules.Plan(s,s.Practice.Actor,CardActionKind.Influence,"ops",new CardId("card.fidel"));Assert.That(begun.Accepted,Is.True,begun.Error.ToString());
            var done=ActionPracticeRules.Plan(begun.NextState,s.Practice.Actor,CardActionKind.FinishInfluence,"finish");Assert.That(done.Accepted,Is.True,done.Error.ToString());
            Assert.That(done.NextState.EventFacts,Is.SameAs(ledger));Assert.That(ledger.Facts.Count,Is.EqualTo(1));Assert.That(done.NextState.Practice.CompletedActions,Is.EqualTo(s.Practice.CompletedActions+1));
        }
    }
}
