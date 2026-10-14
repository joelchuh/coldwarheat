using System;
using System.Linq;
using ColdWarStruggle.Core;
using ColdWarStruggle.Infrastructure;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class ActionPracticeTests
    {
        internal static ActionPracticeDefinition Definition()
            =>new ActionPracticeDefinition(CardCatalogLoaderTests.Load(),new OpsGeneralDefinition(1,4,(OpsValuePurpose[])Enum.GetValues(typeof(OpsValuePurpose))),SpaceAttemptTests.State().Track.Definition,TurnCalendarTests.Calendar());
        internal static GameState Prepared(bool deployed=true)
        {
            var d=Definition();var map=DefinitionTests.World();var deal=InitialDealService.Create(d.Cards,InitialDealLoaderTests.Load(),Pcg32Random.FromSeed(42,54));
            var source=SetupDeploymentTests.Empty(map);
            var s=new GameState(map,source.Countries,source.Operations,cards:deal.Cards,randomState:deal.Random,turn:d.Calendar.ForTurn(1),practice:ActionPracticeState.Create(d));
            s=InitialDeploymentFlow.Begin(s,SetupDeploymentTests.Definition(map),"deployment").NextState;
            if(!deployed)return s;
            s=InitialDeploymentFlow.Resolve(s,PlayerSide.USSR,s.Deployment.DecisionId,new[]{new InfluenceAllocation(new CountryId("country.poland"),6)}).NextState;
            return InitialDeploymentFlow.Resolve(s,PlayerSide.US,s.Deployment.DecisionId,new[]{new InfluenceAllocation(new CountryId("country.france"),7)}).NextState;
        }
        internal static GameState Ready()=>ActionPracticeRules.Plan(Prepared(),PlayerSide.USSR,CardActionKind.StartPractice,"start").NextState;
        internal static CardId Playable(GameState s,CardActionKind kind)=>s.Cards.GetHand(s.Practice.Actor).First(c=>ActionPracticeRules.CanPlay(s,s.Practice.Actor,c,kind)==RuleError.None);
        [Test] public void Start_is_explicit_and_keeps_deal_map_and_rng()
        {
            var s=Prepared();Assert.That(s.Practice.Stage,Is.EqualTo(ActionPracticeStage.AwaitingStart));
            var p=ActionPracticeRules.Plan(s,PlayerSide.USSR,CardActionKind.StartPractice,"start");Assert.That(p.Accepted,Is.True);
            Assert.That(p.NextState.Deployment,Is.Null);Assert.That(p.NextState.Practice.Stage,Is.EqualTo(ActionPracticeStage.ChooseCard));
            Assert.That(p.NextState.Cards,Is.SameAs(s.Cards));Assert.That(p.NextState.RandomState,Is.SameAs(s.RandomState));
            Assert.That(p.NextState.Countries,Is.EquivalentTo(s.Countries));Assert.That(p.NextState.Version,Is.EqualTo(s.Version+1));
            Assert.That(s.Deployment.Complete,Is.True);
        }
        [Test] public void Incomplete_setup_cannot_start()
        { Assert.That(ActionPracticeRules.Plan(Prepared(false),PlayerSide.USSR,CardActionKind.StartPractice,"start").Error,Is.EqualTo(RuleError.SetupInProgress)); }
        [Test] public void Wrong_side_cannot_start_or_play()
        {
            Assert.That(ActionPracticeRules.Plan(Prepared(),PlayerSide.US,CardActionKind.StartPractice,"start").Error,Is.EqualTo(RuleError.WrongActor));
            var s=Ready();Assert.That(ActionPracticeRules.Plan(s,PlayerSide.US,CardActionKind.Space,"x",s.Cards.GetHand(PlayerSide.US)[0]).Error,Is.EqualTo(RuleError.WrongActor));
        }
        [Test] public void Cannot_restart_a_started_slice()
        { Assert.That(ActionPracticeRules.Plan(Ready(),PlayerSide.USSR,CardActionKind.StartPractice,"again").Error,Is.EqualTo(RuleError.ActionNotReady)); }
        [Test] public void Opponent_hand_is_not_owned_and_unconfigured_china_is_unsupported()
        {
            var s=Ready();Assert.That(ActionPracticeRules.CanPlay(s,PlayerSide.USSR,s.Cards.GetHand(PlayerSide.US)[0],CardActionKind.Space),Is.EqualTo(RuleError.CardNotOwned));
            Assert.That(ActionPracticeRules.CanPlay(s,PlayerSide.USSR,s.Cards.China.Id,CardActionKind.Space),Is.EqualTo(RuleError.UnsupportedCardAction));
        }
        [Test] public void Opponent_events_cannot_be_silently_skipped_for_ops()
        {
            var s=Ready();foreach(var card in s.Cards.GetHand(PlayerSide.USSR).Where(id=>s.Practice.Definition.Cards.GetCard(id).Affiliation==CardAffiliation.US))
                Assert.That(ActionPracticeRules.CanPlay(s,PlayerSide.USSR,card,CardActionKind.Influence),Is.EqualTo(RuleError.UnsupportedCardAction));
        }
        [Test] public void Space_consumes_real_card_updates_rng_and_alternates_once()
        {
            var s=Ready();var id=Playable(s,CardActionKind.Space);var p=ActionPracticeRules.Plan(s,PlayerSide.USSR,CardActionKind.Space,"space1",id);var n=p.NextState;
            Assert.That(p.Accepted,Is.True);Assert.That(n.Cards.GetHand(PlayerSide.USSR),Has.No.Member(id));Assert.That(n.Cards.DiscardPile,Does.Contain(id));Assert.That(n.Cards.RemovedCards,Is.Empty);
            Assert.That(n.Cards.ResolvingCards,Is.Empty);Assert.That(n.RandomState.DrawCount,Is.GreaterThan(s.RandomState.DrawCount));Assert.That(n.Practice.Space.UssrAttempts,Is.EqualTo(1));
            Assert.That(n.Practice.Actor,Is.EqualTo(PlayerSide.US));Assert.That(n.Practice.CompletedActions,Is.EqualTo(1));Assert.That(n.Practice.VpForUS,Is.EqualTo(p.SpaceReceipt.NetVpForUS));
            Assert.That(n.Version,Is.EqualTo(s.Version+1));Assert.That(s.Cards.GetHand(PlayerSide.USSR),Does.Contain(id));Assert.That(s.Practice.Space.UssrAttempts,Is.Zero);
        }
        [Test] public void Rejected_space_attempt_does_not_change_inputs()
        {
            var s=Ready();var low=s.Cards.GetHand(PlayerSide.USSR).First(id=>s.Practice.Definition.Cards.GetCard(id).PrintedOps==1);
            var p=ActionPracticeRules.Plan(s,PlayerSide.USSR,CardActionKind.Space,"low",low);Assert.That(p.Error,Is.EqualTo(RuleError.SpaceAttemptUnavailable));Assert.That(p.NextState,Is.Null);Assert.That(s.Practice.Space.UssrAttempts,Is.Zero);
        }
        [Test] public void Influence_uses_printed_quote_moves_to_resolving_and_blocks_other_card()
        {
            var s=Ready();var id=Playable(s,CardActionKind.Influence);var n=ActionPracticeRules.Plan(s,PlayerSide.USSR,CardActionKind.Influence,"ops",id).NextState;
            Assert.That(n.Cards.ResolvingCards,Is.EqualTo(new[]{id}));Assert.That(n.Cards.GetHand(PlayerSide.USSR),Has.No.Member(id));Assert.That(n.Cards.DiscardPile,Is.Empty);
            Assert.That(n.Operations.Budget,Is.EqualTo(s.Practice.Definition.Cards.GetCard(id).PrintedOps));Assert.That(n.Practice.CompletedActions,Is.Zero);
            Assert.That(ActionPracticeRules.Plan(n,PlayerSide.USSR,CardActionKind.Space,"other",n.Cards.GetHand(PlayerSide.USSR)[0]).Error,Is.EqualTo(RuleError.ActionNotReady));
        }
        [Test] public void Ordinary_placement_preserves_card_action_and_finish_discards_star_without_event()
        {
            var s=Ready();var id=Playable(s,CardActionKind.Influence);s=ActionPracticeRules.Plan(s,PlayerSide.USSR,CardActionKind.Influence,"ops",id).NextState;
            var p=InfluenceRules.PlanPlacement(s,s.Operations,new[]{new CountryId("country.poland")});Assert.That(p.Accepted,Is.True);Assert.That(p.NextState.Practice,Is.SameAs(s.Practice));
            var n=ActionPracticeRules.Plan(p.NextState,PlayerSide.USSR,CardActionKind.FinishInfluence,"finish").NextState;
            Assert.That(n.Cards.DiscardPile,Does.Contain(id));Assert.That(n.Cards.ResolvingCards,Is.Empty);Assert.That(n.Practice.Actor,Is.EqualTo(PlayerSide.US));Assert.That(n.SpentOps,Is.Zero);Assert.That(n.Operations.Budget,Is.Zero);
            Assert.That(n.GetInfluence(new CountryId("country.poland"),PlayerSide.USSR),Is.EqualTo(7));Assert.That(n.Practice.Space,Is.SameAs(s.Practice.Space));Assert.That(n.RandomState,Is.SameAs(s.RandomState));
        }
        [Test] public void Cannot_finish_before_selecting_card()
        { Assert.That(ActionPracticeRules.Plan(Ready(),PlayerSide.USSR,CardActionKind.FinishInfluence,"end").Error,Is.EqualTo(RuleError.ActionNotReady)); }
        [TestCase(CardActionKind.StartPractice)] [TestCase(CardActionKind.FinishInfluence)]
        public void Control_commands_reject_card_payload(CardActionKind kind)
        { var s=Ready();Assert.That(ActionPracticeRules.Plan(s,PlayerSide.USSR,kind,"x",s.Cards.GetHand(PlayerSide.USSR)[0]).Error,Is.EqualTo(RuleError.InvalidCommand)); }
    }
}
