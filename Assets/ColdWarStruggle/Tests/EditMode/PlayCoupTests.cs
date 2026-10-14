using System;
using System.IO;
using System.Linq;
using ColdWarStruggle.Core;
using ColdWarStruggle.Infrastructure;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class PlayCoupTests
    {
        static string Read(string file)=>File.ReadAllText(Path.Combine(DefinitionTests.Root,"Data/Rulesets/deluxe-2015/"+file+".json"));
        internal static GameState Prepared()
        {
            var s=ActionPracticeTests.Prepared();var d=s.Practice.Definition;
            var risk=new PracticeRiskDefinition(CoupDefinitionLoader.LoadSubset(Read("coup")),DefconDefinitionLoader.LoadSubset(Read("defcon"),s.Map),MilitaryOperationsLoader.LoadSubset(Read("military-operations")),VictoryDefinitionLoader.LoadSubset(Read("victory")));
            var practice=ActionPracticeState.Create(new ActionPracticeDefinition(d.Cards,d.Ops,d.Space,d.Calendar,risk));
            // Extra opponent presence gives the fourth battleground coup a legal African target at DEFCON2.
            var countries=s.Countries.ToDictionary(x=>x.Key,x=>x.Value);countries[new CountryId("country.nigeria")]=new CountryState(0,1);
            return new GameState(s.Map,countries,s.Operations,s.GameId,s.Version,s.SpentOps,s.EventSequence,turn:s.Turn,deployment:s.Deployment,cards:s.Cards,randomState:s.RandomState,practice:practice);
        }
        internal static GameState Ready()=>ActionPracticeRules.Plan(Prepared(),PlayerSide.USSR,CardActionKind.StartPractice,"start").NextState;
        internal static CardId Card(GameState s)=>s.Cards.GetHand(s.Practice.Actor).First(id=> {var d=s.Practice.Definition.Cards.GetCard(id);return d.Kind==CardKind.Event && (d.Affiliation==CardAffiliation.Neutral || (s.Practice.Actor==PlayerSide.USSR?d.Affiliation==CardAffiliation.USSR:d.Affiliation==CardAffiliation.US));});
        internal static CardActionTransition Coup(GameState s,string country,string id="coup")=>ActionPracticeRules.Plan(s,s.Practice.Actor,CardActionKind.Coup,id,Card(s),new CountryId("country."+country));
        [Test] public void Risk_tracks_survive_explicit_start_and_ordinary_influence()
        {
            var s=Ready();Assert.That(s.Practice.Risk,Is.Not.Null);Assert.That(s.Practice.Risk.Defcon.Level,Is.EqualTo(5));Assert.That(s.Practice.Risk.Military.UssrEarned,Is.Zero);
            var n=ActionPracticeRules.Plan(s,PlayerSide.USSR,CardActionKind.Influence,"ops",Card(s)).NextState;
            n=InfluenceRules.PlanPlacement(n,n.Operations,new[]{new CountryId("country.poland")}).NextState;
            n=ActionPracticeRules.Plan(n,PlayerSide.USSR,CardActionKind.FinishInfluence,"end").NextState;
            Assert.That(n.Practice.Risk,Is.SameAs(s.Practice.Risk));
        }
        [Test] public void Battleground_coup_commits_card_military_defcon_rng_and_country_together()
        {
            var s=Ready();var id=Card(s);var ops=s.Practice.Definition.Cards.GetCard(id).PrintedOps.Value;var p=Coup(s,"iran");var n=p.NextState;
            Assert.That(p.Accepted,Is.True);Assert.That(n.Cards.DiscardPile,Does.Contain(id));Assert.That(n.Cards.GetHand(PlayerSide.USSR),Has.No.Member(id));Assert.That(n.Practice.Risk.Defcon.Level,Is.EqualTo(4));
            Assert.That(n.Practice.Risk.Military.UssrEarned,Is.EqualTo(ops));Assert.That(n.Practice.Risk.CoupReceipts.Count,Is.EqualTo(1));Assert.That(n.Practice.CompletedActions,Is.EqualTo(1));
            Assert.That(n.Countries[new CountryId("country.iran")],Is.EqualTo(p.CoupReceipt.Calculation.After));Pcg32RandomTests.Same(n.RandomState,p.CoupReceipt.RandomAfter);
            Assert.That(n.Version,Is.EqualTo(s.Version+1));Assert.That(s.Practice.Risk.CoupReceipts,Is.Empty);Assert.That(s.Cards.DiscardPile,Is.Empty);
        }
        [Test] public void Non_battleground_coup_keeps_defcon_but_awards_military()
        {var p=Coup(Ready(),"australia");Assert.That(p.Accepted,Is.True);Assert.That(p.NextState.Practice.Risk.Defcon.Level,Is.EqualTo(5));Assert.That(p.NextState.Practice.Risk.Military.UssrEarned,Is.GreaterThan(0));}
        [Test] public void Every_coup_uses_the_latest_state_and_keeps_history_across_space()
        {
            var s=Coup(Ready(),"iran").NextState;var first=s.Practice.Risk.CoupReceipts.Single().Value;
            var card=ActionPracticeTests.Playable(s,CardActionKind.Space);s=ActionPracticeRules.Plan(s,PlayerSide.US,CardActionKind.Space,"space",card).NextState;
            var latest=s.RandomState;var p=Coup(s,"south_africa","second");Assert.That(p.Accepted,Is.True);var n=p.NextState;
            Pcg32RandomTests.Same(p.CoupReceipt.RandomBefore,latest);Assert.That(n.Countries[new CountryId("country.iran")],Is.EqualTo(first.Calculation.After));
            Assert.That(n.Practice.Risk.CoupReceipts.Count,Is.EqualTo(2));Assert.That(n.Practice.Space.UsAttempts,Is.EqualTo(1));Assert.That(n.Practice.Risk.Military.AppliedCredits.Count,Is.EqualTo(2));
        }
        [Test] public void Nuclear_war_commits_winner_and_stops_root_completion_and_card_cleanup()
        {
            var s=Ready();foreach(var target in new[]{"iran","iraq","south_africa"}) {var p=Coup(s,target,target);Assert.That(p.Accepted,Is.True);s=p.NextState;}
            Assert.That(s.Practice.Risk.Defcon.Level,Is.EqualTo(2));Assert.That(s.Practice.Actor,Is.EqualTo(PlayerSide.US));var card=Card(s);
            var nuclear=Coup(s,"nigeria","nuclear");var n=nuclear.NextState;Assert.That(nuclear.Accepted,Is.True);Assert.That(n.Ended,Is.True);Assert.That(n.Practice.Risk.Defcon.Level,Is.EqualTo(1));Assert.That(n.Practice.Risk.Result.Winner,Is.EqualTo(PlayerSide.USSR));
            Assert.That(n.Practice.CompletedActions,Is.EqualTo(s.Practice.CompletedActions));Assert.That(n.Cards.ResolvingCards,Does.Contain(card));Assert.That(n.Cards.DiscardPile,Has.No.Member(card));
            Assert.That(ActionPracticeRules.Plan(n,PlayerSide.US,CardActionKind.Space,"after",n.Cards.GetHand(PlayerSide.US)[0]).Error,Is.EqualTo(RuleError.GameAlreadyEnded));
        }
        [TestCase("poland",RuleError.IllegalTarget)] [TestCase("unknown",RuleError.UnknownCountry)]
        public void Illegal_target_does_not_use_a_card_or_die(string country,RuleError error)
        {var s=Ready();var p=Coup(s,country);Assert.That(p.Error,Is.EqualTo(error));Assert.That(p.NextState,Is.Null);Assert.That(s.Practice.Risk.CoupReceipts,Is.Empty);}
        [Test] public void Geographic_restrictions_apply_to_next_actor()
        {
            var s=Coup(Ready(),"iran").NextState;
            Assert.That(ActionPracticeRules.CheckCoupTarget(s,PlayerSide.US,Card(s),new CountryId("country.poland")),Is.EqualTo(RuleError.IllegalTarget));
        }
        [Test] public void Missing_target_and_extra_target_on_other_action_are_rejected()
        {
            var s=Ready();Assert.That(ActionPracticeRules.Plan(s,PlayerSide.USSR,CardActionKind.Coup,"missing",Card(s)).Error,Is.EqualTo(RuleError.InvalidCommand));
            Assert.That(ActionPracticeRules.Plan(s,PlayerSide.USSR,CardActionKind.Influence,"extra",Card(s),new CountryId("country.iran")).Error,Is.EqualTo(RuleError.InvalidCommand));
        }
        [Test] public void Legacy_practice_without_risk_explicitly_refuses_coup()
        {var s=ActionPracticeTests.Ready();Assert.That(ActionPracticeRules.Plan(s,PlayerSide.USSR,CardActionKind.Coup,"legacy",Card(s),new CountryId("country.iran")).Error,Is.EqualTo(RuleError.UnsupportedCardAction));}
        [Test] public void Opponent_event_cannot_bypass_the_unsupported_event_gate_via_coup()
        {
            var s=Ready();var id=s.Cards.GetHand(PlayerSide.USSR).First(c=>s.Practice.Definition.Cards.GetCard(c).Affiliation==CardAffiliation.US);
            Assert.That(ActionPracticeRules.Plan(s,PlayerSide.USSR,CardActionKind.Coup,"opponent",id,new CountryId("country.iran")).Error,Is.EqualTo(RuleError.UnsupportedCardAction));
        }
    }
}
