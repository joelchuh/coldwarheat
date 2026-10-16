using System;
using System.Collections.Generic;
using System.Linq;
using ColdWarStruggle.Core;
using ColdWarStruggle.Infrastructure;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class WarPlayTests
    {
        internal static CardId Id(string n)=>new CardId("card."+n);
        internal static CountryId Country(string n)=>new CountryId("country."+n);
        internal static ActionPracticeDefinition Bind(ActionPracticeDefinition d,WarEventCatalog wars=null)
            =>new ActionPracticeDefinition(d.Cards,d.Ops,d.Space,d.Calendar,d.Risk,d.Scoring,d.InfluenceEvents,d.DefconVpEvents,d.TurnOpsEvents,d.Realignment,d.China,
                d.TurnEndEnabled,d.NextTurnEnabled,d.HeadlineParameters,d.HeadlineDefectors,d.FinalScoring,d.Blockade,d.FiveYearPlan,
                wars??EarlyWarDefinitionLoader.Load(EarlyWarDefinitionTests.Data(),EarlyWarDefinitionTests.Schema(),d.Risk.Defcon.Map,d.Cards,BudgetedRemovalCatalogTests.KnownEffects));
        internal static GameState Ready(string card="indo_pakistani_war",PlayerSide actor=PlayerSide.USSR,int vp=0,int level=5,bool win=true)
        {
            var s=PlayScoringTests.Ready("card."+card,vp,empty:true,configure:d=>
            {
                var f=d.Risk.Defcon;var risk=new PracticeRiskDefinition(d.Risk.Coup,new DefconDefinition(f.Map,level,f.MaximumLevel,f.NuclearLevel,f.TurnStartImprovement,f.BattlegroundCoupDegradation,f.RestrictedActions,f.BlockedRegions),d.Risk.Military,d.Risk.Victory);
                return Bind(new ActionPracticeDefinition(d.Cards,d.Ops,d.Space,d.Calendar,risk,d.Scoring));
            });
            if(actor==PlayerSide.US)
            {
                s=PlayTurnOpsEventTests.Give(s,PlayerSide.US,"card."+card);s=PlayTurnOpsEventTests.Give(s,PlayerSide.USSR,"card.olympic_games");
                var prep=ActionPracticeRules.Plan(s,PlayerSide.USSR,CardActionKind.Influence,"prep",Id("olympic_games"));Assert.That(prep.Accepted,Is.True,prep.Error.ToString());
                var end=ActionPracticeRules.Plan(prep.NextState,PlayerSide.USSR,CardActionKind.FinishInfluence,"prep-end");Assert.That(end.Accepted,Is.True,end.Error.ToString());s=end.NextState;
            }
            var board=s.Countries.ToDictionary(x=>x.Key,x=>x.Value);
            // Both sides remain below control in each target, so each war has zero control penalty.
            foreach(var n in new[]{"south_korea","israel","india","pakistan"})board[Country(n)]=new CountryState(3,2);
            return PlayScoringTests.Replace(s,board,Die(win?6:1));
        }
        internal static RandomSnapshot Die(int desired)
        {
            for(ulong seed=0;seed<1000;seed++)
            {var r=Pcg32Random.FromSeed(seed,54);var snapshot=r.Capture();if(r.NextInt(0,6)+1==desired)return snapshot;}
            throw new InvalidOperationException("Test seed search exhausted.");
        }
        internal static CardActionTransition Play(GameState s,string name="indo_pakistani_war")=>ActionPracticeRules.Plan(s,s.Practice.Actor,CardActionKind.Event,"war",Id(name));
        internal static CardActionTransition Choose(GameState s,string target="india")=>ActionPracticeRules.Plan(s,s.Practice.War.Owner,CardActionKind.ResolveWar,"resolve",target:Country(target));
        [TestCase("korean_war",true)][TestCase("korean_war",false)][TestCase("arab_israeli_war",true)][TestCase("arab_israeli_war",false)]
        public void Fixed_wars_apply_one_whole_result_and_dispose_the_real_card(string name,bool win)
        {
            var s=Ready(name,win:win);var r=Play(s,name);Assert.That(r.Accepted,Is.True,r.Error.ToString());var n=r.NextState;var target=Country(name=="korean_war"?"south_korea":"israel");
            Assert.That(r.WarResult,Is.Not.Null);Assert.That(r.WarResult.Calculation.Success,Is.EqualTo(win));Assert.That(r.WarResult.Calculation.Beneficiary,Is.EqualTo(PlayerSide.USSR));
            Assert.That(n.GetInfluence(target,PlayerSide.US),Is.EqualTo(win?0:3));Assert.That(n.GetInfluence(target,PlayerSide.USSR),Is.EqualTo(win?5:2));
            Assert.That(n.Practice.VpForUS,Is.EqualTo(win?-2:0));Assert.That(n.Practice.Risk.Military.UssrEarned,Is.EqualTo(2));Assert.That(n.Practice.Risk.Defcon,Is.SameAs(s.Practice.Risk.Defcon));
            Assert.That(n.Practice.CompletedActions,Is.EqualTo(1));Assert.That(n.Practice.Actor,Is.EqualTo(PlayerSide.US));Assert.That(n.Practice.War,Is.Null);Assert.That(n.Cards.ResolvingCards,Is.Empty);
            Assert.That(name=="korean_war"?n.Cards.RemovedCards:n.Cards.DiscardPile,Has.Member(Id(name)));Assert.That(n.Cards.GetHand(PlayerSide.USSR),Has.No.Member(Id(name)));
            Assert.That(s.Cards.GetHand(PlayerSide.USSR),Has.Member(Id(name)));Assert.That(s.GetInfluence(target,PlayerSide.US),Is.EqualTo(3));Assert.That(s.Practice.Risk.Military.UssrEarned,Is.Zero);
            var rng=new Pcg32Random(s.RandomState);rng.NextInt(0,6);Pcg32RandomTests.Same(n.RandomState,rng.Capture());
            Assert.That(n.Countries.Where(x=>!x.Key.Equals(target)),Is.EquivalentTo(s.Countries.Where(x=>!x.Key.Equals(target))));
        }
        [TestCase(PlayerSide.US,"india",true)][TestCase(PlayerSide.US,"pakistan",false)][TestCase(PlayerSide.USSR,"india",false)][TestCase(PlayerSide.USSR,"pakistan",true)]
        public void Indo_choice_waits_without_rng_or_rewards_then_finishes_for_the_event_owner(PlayerSide actor,string target,bool win)
        {
            var s=Ready(actor:actor,win:win);var b=Play(s);Assert.That(b.Accepted,Is.True,b.Error.ToString());var wait=b.NextState;
            Assert.That(wait.Practice.War.Owner,Is.EqualTo(actor));Assert.That(wait.Practice.War.RootActor,Is.EqualTo(actor));Assert.That(wait.Practice.War.ExpectedVersion,Is.EqualTo(wait.Version));
            Assert.That(wait.Practice.War.Candidates,Is.EquivalentTo(new[]{Country("india"),Country("pakistan")}));Assert.Throws<NotSupportedException>(()=>((IList<CountryId>)wait.Practice.War.Candidates).Clear());
            Assert.That(wait.Practice.CompletedActions,Is.EqualTo(s.Practice.CompletedActions));Assert.That(wait.Practice.Risk,Is.SameAs(s.Practice.Risk));Assert.That(wait.Countries,Is.EquivalentTo(s.Countries));Pcg32RandomTests.Same(s.RandomState,wait.RandomState);
            Assert.That(wait.Cards.ResolvingCards,Is.EqualTo(new[]{Id("indo_pakistani_war")}));Assert.That(b.WarResult,Is.Null);
            var r=Choose(wait,target);Assert.That(r.Accepted,Is.True,r.Error.ToString());var n=r.NextState;
            Assert.That(r.WarResult.Calculation.Beneficiary,Is.EqualTo(actor));Assert.That(n.GetInfluence(Country(target),actor),Is.EqualTo(win?5:actor==PlayerSide.US?3:2));
            Assert.That(n.Practice.VpForUS,Is.EqualTo(win?(actor==PlayerSide.US?2:-2):0));Assert.That(actor==PlayerSide.US?n.Practice.Risk.Military.UsEarned:n.Practice.Risk.Military.UssrEarned,Is.EqualTo(2));
            Assert.That(n.Practice.War,Is.Null);Assert.That(n.Practice.CompletedActions,Is.EqualTo(s.Practice.CompletedActions+1));Assert.That(n.Cards.DiscardPile,Has.Member(Id("indo_pakistani_war")));Assert.That(n.Cards.ResolvingCards,Is.Empty);
        }
        [TestCase("korean_war")][TestCase("arab_israeli_war")] public void US_playing_a_fixed_war_still_awards_USSR(string name)
        {
            var s=Ready(name,PlayerSide.US);var r=Play(s,name);Assert.That(r.Accepted,Is.True,r.Error.ToString());Assert.That(r.NextState.Practice.Risk.Military.UssrEarned,Is.EqualTo(2));Assert.That(r.NextState.Practice.Risk.Military.UsEarned,Is.Zero);Assert.That(r.NextState.Practice.VpForUS,Is.EqualTo(-2));Assert.That(r.NextState.Practice.CompletedActions,Is.EqualTo(2));
        }
        [TestCase("korean_war","south_korea")][TestCase("arab_israeli_war","israel")][TestCase("indo_pakistani_war","india")]
        public void Defcon_two_and_empty_target_do_not_gate_war(string name,string target)
        {
            var s=Ready(name,level:2);var board=RegionScoringTests.Empty(s.Map);s=PlayScoringTests.Replace(s,board);var r=Play(s,name);Assert.That(r.Accepted,Is.True,r.Error.ToString());if(r.NextState.Practice.War!=null)r=Choose(r.NextState,target);
            Assert.That(r.Accepted,Is.True,r.Error.ToString());Assert.That(r.WarResult.Calculation.Captured,Is.Zero);Assert.That(r.NextState.Practice.VpForUS,Is.EqualTo(-2));Assert.That(r.NextState.Practice.Risk.Defcon.Level,Is.EqualTo(2));Assert.That(r.NextState.Practice.Risk.Military.UssrEarned,Is.EqualTo(2));
        }
        [TestCase(PlayerSide.US)][TestCase(PlayerSide.USSR)] public void Winning_war_completes_rewards_before_terminal_and_does_not_advance_root(PlayerSide actor)
        {
            var s=Ready(actor:actor,vp:actor==PlayerSide.US?19:-19);var b=Play(s);Assert.That(b.Accepted,Is.True,b.Error.ToString());var r=Choose(b.NextState);Assert.That(r.Accepted,Is.True,r.Error.ToString());var n=r.NextState;
            Assert.That(n.Ended,Is.True);Assert.That(n.Practice.Risk.Result.Winner,Is.EqualTo(actor));Assert.That(n.Practice.VpForUS,Is.EqualTo(actor==PlayerSide.US?21:-21));Assert.That(n.GetInfluence(Country("india"),actor),Is.EqualTo(5));
            Assert.That(n.Practice.CompletedActions,Is.EqualTo(s.Practice.CompletedActions));Assert.That(n.Cards.ResolvingCards,Has.Member(Id("indo_pakistani_war")));Assert.That(n.Practice.War,Is.Null);Assert.That(ActionPracticeRules.CanResolveWar(n,actor),Is.EqualTo(RuleError.GameAlreadyEnded));
        }
        [TestCase("side")][TestCase("target")][TestCase("card")][TestCase("missing")][TestCase("version")][TestCase("random")][TestCase("board")][TestCase("game")]
        public void Invalid_choice_or_stale_authority_is_rejected_without_a_result(string defect)
        {
            var b=Play(Ready());Assert.That(b.Accepted,Is.True,b.Error.ToString());var original=b.NextState;var s=original;
            if(defect=="version")s=PlayScoringTests.Replace(s,version:s.Version+1);
            if(defect=="random")s=PlayScoringTests.Replace(s,random:Die(1));
            if(defect=="board"){var board=s.Countries.ToDictionary(x=>x.Key,x=>x.Value);board[Country("india")]=new CountryState(7,8);s=PlayScoringTests.Replace(s,board);}
            if(defect=="game")s=new GameState(s.Map,s.Countries,s.Operations,"other",s.Version,s.SpentOps,s.EventSequence,turn:s.Turn,cards:s.Cards,randomState:s.RandomState,practice:s.Practice);
            var r=ActionPracticeRules.Plan(s,defect=="side"?PlayerSide.US:PlayerSide.USSR,CardActionKind.ResolveWar,"choice",defect=="card"?Id("indo_pakistani_war"):(CardId?)null,defect=="missing"?null:Country(defect=="target"?"israel":"india"));
            Assert.That(r.Accepted,Is.False);Assert.That(r.NextState,Is.Null);Assert.That(r.WarResult,Is.Null);Assert.That(original.Practice.Risk.Military.UssrEarned,Is.Zero);Assert.That(original.Practice.CompletedActions,Is.Zero);Pcg32RandomTests.Same(original.RandomState,Ready().RandomState);
        }
        [Test] public void Pending_war_cannot_be_skipped_or_replaced_by_another_card()
        {
            var b=Play(Ready());Assert.That(b.Accepted,Is.True,b.Error.ToString());var s=b.NextState;
            foreach(var kind in new[]{CardActionKind.FinishInfluence,CardActionKind.StartPractice,CardActionKind.SkipEmptyHand,CardActionKind.AssessTurnEnd,CardActionKind.BeginNextTurn,CardActionKind.FinalScoring})Assert.That(ActionPracticeRules.Plan(s,PlayerSide.USSR,kind,"bypass").Accepted,Is.False,kind.ToString());
            Assert.That(ActionPracticeRules.CanPlay(s,PlayerSide.USSR,s.Cards.GetHand(PlayerSide.USSR).First(),CardActionKind.Influence),Is.EqualTo(RuleError.EventInProgress));
        }
        [TestCase(false)][TestCase(true)] public void Commit_overflow_never_publishes_war_rewards(bool choice)
        {
            var s=Ready(choice?"indo_pakistani_war":"korean_war");if(choice){var b=Play(s);Assert.That(b.Accepted,Is.True,b.Error.ToString());s=b.NextState;}
            // Event sequence overflow preserves the decision version and tests the actual commit boundary.
            s=PlayScoringTests.Replace(s,sequence:long.MaxValue);var r=choice?Choose(s):Play(s,"korean_war");Assert.That(r.Error,Is.EqualTo(RuleError.NumericOverflow));Assert.That(r.NextState,Is.Null);Assert.That(s.Practice.Risk.Military.UssrEarned,Is.Zero);Assert.That(s.GetInfluence(Country(choice?"india":"south_korea"),PlayerSide.US),Is.EqualTo(3));
        }
        [Test] public void War_registration_requires_shared_risk_map_and_card_catalog()
        {
            var d=Ready().Practice.Definition;
            Assert.Throws<ArgumentException>(()=>new ActionPracticeDefinition(d.Cards,d.Ops,d.Space,d.Calendar,wars:d.Wars));
            Assert.Throws<ArgumentException>(()=>Bind(d,EarlyWarDefinitionTests.Load()));
        }
    }
}
