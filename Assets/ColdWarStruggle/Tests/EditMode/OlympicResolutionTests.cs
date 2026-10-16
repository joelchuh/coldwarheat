using System;
using System.Collections.Generic;
using System.Linq;
using ColdWarStruggle.Core;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public sealed class OlympicResolutionTests
    {
        internal sealed class Calls { public int Draws; public int Forks; }
        internal sealed class ScriptRandom : IRandomProvider
        {
            readonly int[] faces; readonly Calls calls; readonly ulong failAt; ulong cursor;
            internal ScriptRandom(int[] faces,ulong cursor=0,Calls calls=null,ulong failAt=ulong.MaxValue)
            {this.faces=(int[])faces.Clone();this.cursor=cursor;this.calls=calls??new Calls();this.failAt=failAt;}
            public RandomSnapshot Capture()=>new RandomSnapshot(RandomSnapshot.SupportedAlgorithm,cursor+1,1,cursor);
            public IRandomProvider Fork(){calls.Forks++;return new ScriptRandom(faces,cursor,calls,failAt);}
            public int NextInt(int min,int max)
            {calls.Draws++;cursor++;if(cursor==failAt)throw new InvalidOperationException("Injected sample failure.");return faces[(int)cursor-1]-1;}
        }
        internal static OlympicEventState Event(IRandomProvider random,int level=5,int vp=0)
        {
            var risk=PlayCoupTests.Prepared().Practice.Definition.Risk;
            return new OlympicEventState(OlympicDefinitionTests.Load(),new DefconState(risk.Defcon,level),risk.Victory,vp,random.Capture());
        }
        internal static CardZones Cards(OlympicEventState state,string placement="resolving",CardCatalog catalog=null)
        {
            var c=catalog??state.Definition.Cards;var id=state.Definition.Card.Id;
            var early=c.Cards.Where(x=>x.Kind!=CardKind.China && x.Era==GameEra.Early).Select(x=>x.Id).ToArray();
            var resolving=placement=="resolving"?new[]{id}:placement=="multiple"?new[]{id,early.First(x=>!x.Equals(id))}:Array.Empty<CardId>();
            var hand=placement=="hand"?new[]{id}:Array.Empty<CardId>();
            return new CardZones(c,early.Except(resolving).Except(hand),new Dictionary<PlayerSide,IReadOnlyList<CardId>>{{PlayerSide.US,hand},{PlayerSide.USSR,Array.Empty<CardId>()}},
                c.Cards.Where(x=>x.Kind!=CardKind.China && x.Era!=GameEra.Early).Select(x=>x.Id),Array.Empty<CardId>(),Array.Empty<CardId>(),resolving,
                new ChinaCardState(new CardId("card.china"),PlayerSide.USSR,CardFace.FaceUp),new[]{GameEra.Early});
        }
        internal static OlympicResolution Begin(IRandomProvider random,PlayerSide sponsor=PlayerSide.US,int level=5,int vp=0,long revision=7,OlympicSourceKind kind=OlympicSourceKind.ActionRound)
        {var s=Event(random,level,vp);return OlympicResolutionRules.Begin(s,Cards(s),"game", "olympic-root",revision,kind,sponsor,sponsor);}
        internal static OlympicResolution Respond(OlympicResolution s,IRandomProvider random,OlympicBranch branch=OlympicBranch.Participate)
            =>OlympicResolutionRules.Respond(s,s.Sponsor==PlayerSide.US?PlayerSide.USSR:PlayerSide.US,s.DecisionId,s.Revision,branch,random);

        [TestCase(PlayerSide.US,OlympicSourceKind.ActionRound)][TestCase(PlayerSide.USSR,OlympicSourceKind.ActionRound)]
        [TestCase(PlayerSide.US,OlympicSourceKind.Headline)][TestCase(PlayerSide.USSR,OlympicSourceKind.Headline)]
        public void Begin_binds_real_resolving_card_and_opponent_without_consuming_random(PlayerSide sponsor,OlympicSourceKind kind)
        {
            var calls=new Calls();var r=new ScriptRandom(new[]{6,6},calls:calls);var s=Begin(r,sponsor,kind:kind);
            Assert.That(s.Stage,Is.EqualTo(OlympicResolutionStage.AwaitingOpponent));Assert.That(s.PendingOwner,Is.EqualTo(sponsor==PlayerSide.US?PlayerSide.USSR:PlayerSide.US));
            Assert.That(s.SourceCards.ResolvingCards.Single(),Is.EqualTo(new CardId("card.olympic_games")));Assert.That(s.Current.Definition.Cards,Is.SameAs(s.SourceCards.Catalog));
            Assert.That(s.RootPhasing,Is.EqualTo(sponsor));Assert.That(s.SourceKind,Is.EqualTo(kind));Assert.That(s.Revision,Is.EqualTo(7));
            Assert.That(s.Branch,Is.Null);Assert.That(s.Continuation,Is.Null);Assert.That(s.Operations,Is.Null);Assert.That(s.LastPair,Is.Null);
            Assert.That(calls.Forks,Is.Zero);Assert.That(calls.Draws,Is.Zero);
        }
        [TestCase("hand")][TestCase("draw")][TestCase("multiple")][TestCase("catalog")][TestCase("kind")]
        [TestCase("sponsor")][TestCase("root")][TestCase("nested")][TestCase("game")][TestCase("id")][TestCase("revision")][TestCase("vp")][TestCase("nuclear")]
        public void Invalid_source_or_nonlive_state_is_rejected_before_any_choice(string fault)
        {
            var r=Pcg32Random.FromSeed(42,54);var s=Event(r,fault=="nuclear"?1:5,fault=="vp"?20:0);
            var cards=Cards(s,fault=="hand"||fault=="draw"||fault=="multiple"?fault:"resolving",fault=="catalog"?CardCatalogLoaderTests.Load():null);
            Assert.That(()=>OlympicResolutionRules.Begin(s,cards,fault=="game"?" ":"game",fault=="id"?" ":"root",fault=="revision"?-1:7,
                fault=="kind"?(OlympicSourceKind)88:OlympicSourceKind.ActionRound,fault=="sponsor"?(PlayerSide)88:PlayerSide.US,
                fault=="root"?(PlayerSide)88:fault=="nested"?PlayerSide.USSR:PlayerSide.US),Throws.InstanceOf<ArgumentException>());
            Pcg32RandomTests.Same(s.Random,r.Capture());
        }
        [TestCase(PlayerSide.US,6,1,2)][TestCase(PlayerSide.USSR,1,6,-2)]
        [TestCase(PlayerSide.US,1,6,-2)][TestCase(PlayerSide.USSR,6,1,2)]
        public void First_complete_pair_locks_choice_and_finishes_without_touching_source(PlayerSide sponsor,int us,int su,int vp)
        {
            var r=new ScriptRandom(new[]{us,su});var s=Begin(r,sponsor);var n=Respond(s,r);
            Assert.That(n.Stage,Is.EqualTo(OlympicResolutionStage.Completed));Assert.That(n.Branch,Is.EqualTo(OlympicBranch.Participate));Assert.That(n.Current.VpForUS,Is.EqualTo(vp));
            Assert.That(n.PendingOwner,Is.Null);Assert.That(n.Continuation,Is.Null);Assert.That(n.Operations,Is.Null);Assert.That(n.SourceCards,Is.SameAs(s.SourceCards));
            Assert.That(n.LastPair.Plan.Before,Is.SameAs(s.Current));Assert.That(n.LastPair.Previous,Is.Null);Assert.That(n.LastPair.RoundIndex,Is.EqualTo(1));Assert.That(n.LastPair.CommittedRevision,Is.EqualTo(8));
            Assert.That(s.Stage,Is.EqualTo(OlympicResolutionStage.AwaitingOpponent));Assert.That(s.Branch,Is.Null);Assert.That(r.Capture().DrawCount,Is.Zero);
        }
        [TestCase("actor")][TestCase("id")][TestCase("revision")][TestCase("branch")][TestCase("unknownActor")]
        public void Invalid_response_does_not_sample_or_lock_decision(string fault)
        {
            var calls=new Calls();var r=new ScriptRandom(new[]{6,1},calls:calls);var s=Begin(r);
            Assert.That(()=>OlympicResolutionRules.Respond(s,fault=="actor"?PlayerSide.US:fault=="unknownActor"?(PlayerSide)9:PlayerSide.USSR,
                fault=="id"?"other":s.DecisionId,fault=="revision"?6:7,fault=="branch"?(OlympicBranch)8:OlympicBranch.Participate,r),Throws.InstanceOf<ArgumentException>());
            Assert.That(calls.Forks,Is.Zero);Assert.That(calls.Draws,Is.Zero);Assert.That(s.LastPair,Is.Null);
        }
        [Test] public void Second_die_failure_leaves_original_choice_available_for_boycott()
        {
            var calls=new Calls();var r=new ScriptRandom(new[]{6,1},calls:calls,failAt:2);var s=Begin(r);
            Assert.Throws<InvalidOperationException>(()=>Respond(s,r));Assert.That(calls.Draws,Is.EqualTo(2));Assert.That(s.Branch,Is.Null);
            var boycott=Respond(s,null,OlympicBranch.Boycott);Assert.That(boycott.Stage,Is.EqualTo(OlympicResolutionStage.AwaitingOperations));
            Assert.That(boycott.Current.Defcon.Level,Is.EqualTo(4));Assert.That(boycott.LastPair,Is.Null);Assert.That(r.Capture().DrawCount,Is.Zero);
        }
        [Test] public void Ties_remove_player_choice_and_append_one_immutable_receipt_per_pair()
        {
            var faces=new[]{1,3,2,4,4,5};var r=new ScriptRandom(faces);var s=Begin(r);var a=Respond(s,r);
            Assert.That(a.Stage,Is.EqualTo(OlympicResolutionStage.Rolling));Assert.That(a.PendingOwner,Is.Null);Assert.That(a.Continuation.NextRoundIndex,Is.EqualTo(2));
            Assert.That(()=>Respond(a,new ScriptRandom(faces,cursor:2),OlympicBranch.Boycott),Throws.InstanceOf<InvalidOperationException>());
            var b=OlympicResolutionRules.ContinueOne(a,a.Continuation,new ScriptRandom(faces,cursor:2));
            Assert.That(b.LastPair.Previous,Is.SameAs(a.LastPair));Assert.That(b.LastPair.RoundIndex,Is.EqualTo(2));Assert.That(b.Revision,Is.EqualTo(9));Assert.That(b.Current.VpForUS,Is.Zero);
            var c=OlympicResolutionRules.ContinueOne(b,b.Continuation,new ScriptRandom(faces,cursor:4));
            Assert.That(c.Stage,Is.EqualTo(OlympicResolutionStage.Completed));Assert.That(c.LastPair.RoundIndex,Is.EqualTo(3));Assert.That(c.Revision,Is.EqualTo(10));Assert.That(c.Current.VpForUS,Is.EqualTo(2));
            Assert.That(c.Current.Random.DrawCount,Is.EqualTo(6));Assert.That(a.Current.Random.DrawCount,Is.EqualTo(2));Assert.That(a.LastPair.Previous,Is.Null);
        }
        [TestCase("old")][TestCase("foreign")][TestCase("null")]
        public void Continue_requires_the_exact_current_token_before_sampling(string fault)
        {
            var faces=new[]{1,3,2,4,4,5};var r=new ScriptRandom(faces);var a=Respond(Begin(r),r);
            var b=OlympicResolutionRules.ContinueOne(a,a.Continuation,new ScriptRandom(faces,cursor:2));
            var other=Respond(Begin(r),r);var calls=new Calls();var next=new ScriptRandom(faces,cursor:4,calls:calls);
            var token=fault=="old"?a.Continuation:fault=="foreign"?other.Continuation:null;
            Assert.That(()=>OlympicResolutionRules.ContinueOne(b,token,next),Throws.InstanceOf<ArgumentException>());Assert.That(calls.Forks,Is.Zero);
        }
        [TestCase(PlayerSide.US,3,OlympicSourceKind.ActionRound)][TestCase(PlayerSide.USSR,5,OlympicSourceKind.Headline)]
        public void Boycott_commits_defcon_and_waits_for_sponsor_then_explicit_decline_preserves_drop(PlayerSide sponsor,int level,OlympicSourceKind kind)
        {
            var r=Pcg32Random.FromSeed(42,54);var s=Begin(r,sponsor,level,kind:kind);var n=Respond(s,null,OlympicBranch.Boycott);
            Assert.That(n.Stage,Is.EqualTo(OlympicResolutionStage.AwaitingOperations));Assert.That(n.PendingOwner,Is.EqualTo(sponsor));Assert.That(n.Operations.Actor,Is.EqualTo(sponsor));Assert.That(n.Operations.BaseOps,Is.EqualTo(4));
            Assert.That(n.Current.Defcon.Level,Is.EqualTo(level-1));Assert.That(n.Current.Random,Is.SameAs(s.Current.Random));Assert.That(n.Branch,Is.EqualTo(OlympicBranch.Boycott));Assert.That(n.LastPair,Is.Null);
            var done=OlympicResolutionRules.DeclineOperations(n,sponsor,8);
            Assert.That(done.Stage,Is.EqualTo(OlympicResolutionStage.Completed));Assert.That(done.Revision,Is.EqualTo(9));Assert.That(done.Current,Is.SameAs(n.Current));Assert.That(done.Operations,Is.Null);Assert.That(done.Boycott,Is.SameAs(n.Boycott));Assert.That(done.SourceCards,Is.SameAs(s.SourceCards));
        }
        [TestCase(PlayerSide.US)][TestCase(PlayerSide.USSR)]
        public void Nuclear_boycott_finishes_before_any_operations(PlayerSide sponsor)
        {
            var r=Pcg32Random.FromSeed(42,54);var s=Begin(r,sponsor,2);var n=Respond(s,null,OlympicBranch.Boycott);
            Assert.That(n.Stage,Is.EqualTo(OlympicResolutionStage.Completed));Assert.That(n.Decision.Outcome,Is.EqualTo(VictoryOutcome.Finished));Assert.That(n.Decision.Winner,Is.EqualTo(sponsor==PlayerSide.US?PlayerSide.USSR:PlayerSide.US));
            Assert.That(n.Current.Defcon.Level,Is.EqualTo(1));Assert.That(n.Operations,Is.Null);Assert.That(n.Continuation,Is.Null);Assert.That(n.PendingOwner,Is.Null);
        }
        [TestCase(PlayerSide.US,19,6,1,21)][TestCase(PlayerSide.USSR,-19,1,6,-21)]
        public void Match_victory_returns_terminal_decision_without_parent_cleanup(PlayerSide sponsor,int before,int us,int su,int after)
        {
            var r=new ScriptRandom(new[]{us,su});var s=Begin(r,sponsor,vp:before);var n=Respond(s,r);
            Assert.That(n.Decision.Outcome,Is.EqualTo(VictoryOutcome.Finished));Assert.That(n.Current.VpForUS,Is.EqualTo(after));Assert.That(n.SourceCards.ResolvingCards.Single(),Is.EqualTo(s.Current.Definition.Card.Id));
        }
        [TestCase("actor")][TestCase("revision")][TestCase("before")][TestCase("after")]
        public void Invalid_decline_never_reverts_boycott(string fault)
        {
            var r=Pcg32Random.FromSeed(42,54);var s=Begin(r);var b=Respond(s,null,OlympicBranch.Boycott);
            var input=fault=="before"?s:fault=="after"?OlympicResolutionRules.DeclineOperations(b,PlayerSide.US,8):b;
            TestDelegate act=()=>OlympicResolutionRules.DeclineOperations(input,fault=="actor"?PlayerSide.USSR:PlayerSide.US,fault=="revision"?7:input.Revision);
            Assert.That(act,fault=="before"||fault=="after"?Throws.InstanceOf<InvalidOperationException>():Throws.InstanceOf<ArgumentException>());
            Assert.That(b.Current.Defcon.Level,Is.EqualTo(4));Assert.That(b.Stage,Is.EqualTo(OlympicResolutionStage.AwaitingOperations));
        }
        [TestCase("waiting")][TestCase("ops")][TestCase("completed")]
        public void Nonrolling_states_cannot_continue(string kind)
        {
            var r=new ScriptRandom(new[]{6,1});var s=Begin(r);if(kind=="ops")s=Respond(s,null,OlympicBranch.Boycott);if(kind=="completed")s=Respond(s,r);
            Assert.That(()=>OlympicResolutionRules.ContinueOne(s,null,r),Throws.InstanceOf<InvalidOperationException>());
        }
        [Test] public void Revision_overflow_is_checked_before_random_fork_or_boycott()
        {
            var calls=new Calls();var r=new ScriptRandom(new[]{1,3,6,1},calls:calls);var s=Begin(r,revision:long.MaxValue);
            Assert.Throws<OverflowException>(()=>Respond(s,r));Assert.Throws<OverflowException>(()=>Respond(s,null,OlympicBranch.Boycott));Assert.That(calls.Forks,Is.Zero);
            var a=Respond(Begin(r,revision:long.MaxValue-1),r);var before=calls.Forks;
            Assert.Throws<OverflowException>(()=>OlympicResolutionRules.ContinueOne(a,a.Continuation,new ScriptRandom(new[]{1,3,6,1},2,calls)));
            Assert.That(calls.Forks,Is.EqualTo(before));Assert.That(a.LastPair.RoundIndex,Is.EqualTo(1));
        }
        [Test] public void Real_PCG_planning_reconstructs_same_pair_without_mutating_original()
        {
            var r=Pcg32Random.FromSeed(42,54);var s=Begin(r);var a=Respond(s,r);var b=Respond(s,new Pcg32Random(s.Current.Random));
            Assert.That(a.LastPair.Plan.Calculation.UsRoll,Is.EqualTo(b.LastPair.Plan.Calculation.UsRoll));Assert.That(a.LastPair.Plan.Calculation.UssrRoll,Is.EqualTo(b.LastPair.Plan.Calculation.UssrRoll));
            Pcg32RandomTests.Same(a.Current.Random,b.Current.Random);Pcg32RandomTests.Same(s.Current.Random,r.Capture());
        }
        [Test] public void Null_inputs_fail_at_public_boundaries()
        {
            var r=Pcg32Random.FromSeed(42,54);var s=Event(r);
            Assert.Throws<ArgumentNullException>(()=>OlympicResolutionRules.Begin(null,Cards(s),"g","r",0,OlympicSourceKind.ActionRound,PlayerSide.US,PlayerSide.US));
            Assert.Throws<ArgumentNullException>(()=>OlympicResolutionRules.Begin(s,null,"g","r",0,OlympicSourceKind.ActionRound,PlayerSide.US,PlayerSide.US));
            Assert.Throws<ArgumentNullException>(()=>OlympicResolutionRules.Respond(null,PlayerSide.US,"id",0,OlympicBranch.Boycott,null));
            Assert.Throws<ArgumentNullException>(()=>OlympicResolutionRules.ContinueOne(null,null,null));
            Assert.Throws<ArgumentNullException>(()=>OlympicResolutionRules.DeclineOperations(null,PlayerSide.US,0));
        }
    }
}
