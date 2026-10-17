using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using ColdWarStruggle.Core;
using ColdWarStruggle.Infrastructure;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class PlayScoringTests
    {
        static string Read(string p)=>File.ReadAllText(Path.Combine(DefinitionTests.Root,p));
        internal static GameState Ready(string scoring="card.europe_scoring",int vp=0,bool empty=false,Func<ActionPracticeDefinition,ActionPracticeDefinition> configure=null)
        {
            var s=PlayCoupTests.Prepared();var d=s.Practice.Definition;var rd=d.Risk;
            var victory=new VictoryDefinition(vp,rd.Victory.AutomaticVpThreshold,rd.Victory.HeldScoringRows);
            var risk=new PracticeRiskDefinition(rd.Coup,rd.Defcon,rd.Military,victory);
            var scoringDefinition=RegionScoringDefinitionLoader.LoadSubset(Read("Data/Rulesets/deluxe-2015/region-scoring.json"),Read("Docs/Contracts/region-scoring-rules.schema.json"),s.Map);
            var definition=new ActionPracticeDefinition(d.Cards,d.Ops,d.Space,d.Calendar,risk,scoringDefinition);
            var p=ActionPracticeState.Create(configure==null?definition:configure(definition));
            var id=new CardId(scoring);var hands=new Dictionary<PlayerSide,IReadOnlyList<CardId>> {
                {PlayerSide.US,s.Cards.GetHand(PlayerSide.US).Where(c=>!c.Equals(id)).ToArray()},
                {PlayerSide.USSR,s.Cards.GetHand(PlayerSide.USSR).Skip(1).Where(c=>!c.Equals(id)).Concat(new[]{id}).ToArray()}};
            var used=hands.Values.SelectMany(x=>x).ToHashSet();
            // Explicit all-era card-zone fixture for testing seven shared card definitions, not a real turn-one deal.
            var cards=new CardZones(d.Cards,d.Cards.Cards.Where(c=>c.Kind!=CardKind.China && !used.Contains(c.Id)).Select(c=>c.Id),hands,
                Array.Empty<CardId>(),Array.Empty<CardId>(),Array.Empty<CardId>(),Array.Empty<CardId>(),s.Cards.China,new[]{GameEra.Early,GameEra.Mid,GameEra.Late});
            var state=new GameState(s.Map,empty?RegionScoringTests.Empty(s.Map):s.Countries,s.Operations,s.GameId,s.Version,s.SpentOps,s.EventSequence,
                turn:s.Turn,deployment:s.Deployment,cards:cards,randomState:s.RandomState,practice:p,japanPact:p.Definition.JapanPact==null?null:JapanPactState.Create(p.Definition.JapanPact,s.GameId));
            return ActionPracticeRules.Plan(state,PlayerSide.USSR,CardActionKind.StartPractice,"start").NextState;
        }
        internal static GameState Replace(GameState s,IReadOnlyDictionary<CountryId,CountryState> countries=null,RandomSnapshot random=null,long? version=null,long? sequence=null)
            =>new GameState(s.Map,countries??s.Countries,s.Operations,s.GameId,version??s.Version,s.SpentOps,sequence??s.EventSequence,turn:s.Turn,cards:s.Cards,randomState:random??s.RandomState,practice:s.Practice,japanPact:s.JapanPact);
        internal static CardActionTransition Score(GameState s,string id)=>ActionPracticeRules.Plan(s,s.Practice.Actor,CardActionKind.Scoring,"score",new CardId(id));
        [TestCase("europe")] [TestCase("asia")] [TestCase("middle_east")] [TestCase("africa")] [TestCase("central_america")] [TestCase("south_america")] [TestCase("southeast_asia")]
        public void Every_scoring_card_uses_its_region_and_zero_net_still_completes(string region)
        {
            var id="card."+region+"_scoring";var s=Ready(id,empty:true);var result=Score(s,id);Assert.That(result.Accepted,Is.True);var n=result.NextState;
            Assert.That(result.ScoringResult.RegionId,Is.EqualTo("region."+region));Assert.That(result.ScoringResult.NetVpForUS,Is.Zero);Assert.That(n.Practice.CompletedActions,Is.EqualTo(1));Assert.That(n.Ended,Is.False);
            Assert.That(n.Cards.GetHand(PlayerSide.USSR),Has.No.Member(new CardId(id)));Assert.That(n.Cards.ResolvingCards,Is.Empty);
            Assert.That(region=="southeast_asia"?n.Cards.RemovedCards:n.Cards.DiscardPile,Does.Contain(new CardId(id)));
            Pcg32RandomTests.Same(n.RandomState,s.RandomState);Assert.That(n.Countries,Is.EquivalentTo(s.Countries));Assert.That(n.Practice.Risk.Military,Is.SameAs(s.Practice.Risk.Military));
        }
        [TestCase(PlayerSide.US)] [TestCase(PlayerSide.USSR)] public void Europe_control_wins_without_numeric_score_or_root_completion(PlayerSide side)
        {
            var s=Ready(vp:17,empty:true);var countries=s.Countries.ToDictionary(x=>x.Key,x=>x.Value);
            RegionScoringTests.Control(s.Map,countries,side,s.Map.GetCountriesInRegion("region.europe").Where(c=>c.Battleground).Select(c=>c.Id.Value).ToArray());
            s=Replace(s,countries);var r=Score(s,"card.europe_scoring");Assert.That(r.Accepted,Is.True);Assert.That(r.ScoringResult.NetVpForUS,Is.Null);Assert.That(r.NextState.Practice.VpForUS,Is.EqualTo(17));
            Assert.That(r.NextState.Ended,Is.True);Assert.That(r.NextState.Practice.Risk.Result.Winner,Is.EqualTo(side));Assert.That(r.NextState.Practice.CompletedActions,Is.Zero);Assert.That(r.NextState.Cards.ResolvingCards,Does.Contain(new CardId("card.europe_scoring")));
            Assert.That(Score(r.NextState,"card.europe_scoring").Error,Is.EqualTo(RuleError.GameAlreadyEnded));
        }
        [Test] public void Standard_scoring_applies_one_net_batch_and_checks_victory()
        {
            var s=Ready("card.central_america_scoring",-17,true);var countries=s.Countries.ToDictionary(x=>x.Key,x=>x.Value);
            RegionScoringTests.Control(s.Map,countries,PlayerSide.USSR,"country.cuba","country.haiti","country.dominican_republic");RegionScoringTests.Control(s.Map,countries,PlayerSide.US,"country.guatemala");
            var r=Score(Replace(s,countries),"card.central_america_scoring");Assert.That(r.ScoringResult.NetVpForUS,Is.EqualTo(-4));Assert.That(r.NextState.Practice.VpForUS,Is.EqualTo(-21));Assert.That(r.NextState.Practice.Risk.Result.Winner,Is.EqualTo(PlayerSide.USSR));Assert.That(r.NextState.Ended,Is.True);
        }
        [Test] public void Equal_awards_do_not_trigger_a_transient_threshold_victory()
        {
            var s=Ready("card.africa_scoring",19,true);var countries=s.Countries.ToDictionary(x=>x.Key,x=>x.Value);RegionScoringTests.Control(s.Map,countries,PlayerSide.US,"country.zaire");RegionScoringTests.Control(s.Map,countries,PlayerSide.USSR,"country.angola");
            var r=Score(Replace(s,countries),"card.africa_scoring");Assert.That(r.ScoringResult.US.TotalVp,Is.EqualTo(2));Assert.That(r.ScoringResult.USSR.TotalVp,Is.EqualTo(2));Assert.That(r.NextState.Practice.VpForUS,Is.EqualTo(19));Assert.That(r.NextState.Ended,Is.False);
        }
        [Test] public void Space_can_now_finish_a_game_after_previous_scores()
        {
            var initial=Ready(vp:-19);var card=ActionPracticeTests.Playable(initial,CardActionKind.Space);bool succeeded=false;
            for(ulong seed=0;seed<24 && !succeeded;seed++)
            {
                var s=Replace(initial,random:Pcg32Random.FromSeed(seed,54).Capture());var r=ActionPracticeRules.Plan(s,PlayerSide.USSR,CardActionKind.Space,"space",card);Assert.That(r.Accepted,Is.True);
                if(!r.SpaceReceipt.Success)continue;succeeded=true;
                Assert.That(r.NextState.Ended,Is.True);Assert.That(r.NextState.Practice.Risk.Result.Winner,Is.EqualTo(PlayerSide.USSR));Assert.That(r.NextState.Practice.VpForUS,Is.EqualTo(-21));Assert.That(r.NextState.Practice.CompletedActions,Is.Zero);Assert.That(r.NextState.Cards.ResolvingCards,Does.Contain(card));
                Assert.That(ActionPracticeRules.Plan(r.NextState,PlayerSide.USSR,CardActionKind.Influence,"after",card).Error,Is.EqualTo(RuleError.GameAlreadyEnded));
            }
            Assert.That(succeeded,Is.True);
        }
        [Test] public void Event_and_extra_target_cannot_be_used_for_scoring()
        {
            var s=Ready();Assert.That(ActionPracticeRules.Plan(s,PlayerSide.USSR,CardActionKind.Scoring,"bad",PlayCoupTests.Card(s)).Error,Is.EqualTo(RuleError.UnsupportedCardAction));
            Assert.That(ActionPracticeRules.Plan(s,PlayerSide.USSR,CardActionKind.Scoring,"bad-target",new CardId("card.europe_scoring"),new CountryId("country.poland")).Error,Is.EqualTo(RuleError.InvalidCommand));
        }
        [Test] public void Legacy_no_scoring_configuration_remains_explicitly_unsupported()
        {var s=PlayCoupTests.Ready();Assert.That(ActionPracticeRules.Plan(s,PlayerSide.USSR,CardActionKind.Scoring,"old",PlayCoupTests.Card(s)).Error,Is.EqualTo(RuleError.UnsupportedCardAction));}
        [TestCase(true)] [TestCase(false)] public void Counter_overflow_keeps_vp_cards_and_rng_unchanged(bool version)
        {
            var s=Ready(empty:true);s=Replace(s,version:version?long.MaxValue:s.Version,sequence:version?s.EventSequence:long.MaxValue);var r=Score(s,"card.europe_scoring");Assert.That(r.Error,Is.EqualTo(RuleError.NumericOverflow));Assert.That(r.NextState,Is.Null);Assert.That(s.Practice.VpForUS,Is.Zero);Assert.That(s.Cards.ResolvingCards,Is.Empty);
        }
    }
}
