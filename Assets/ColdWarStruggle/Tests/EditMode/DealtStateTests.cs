using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ColdWarStruggle.Core;
using ColdWarStruggle.Application;
using Newtonsoft.Json;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class DealtStateTests
    {
        InitialDealResult deal;
        MapDefinition map;
        [OneTimeSetUp] public void Prepare()
        {
            map=DefinitionTests.World();deal=InitialDealService.Create(CardCatalogLoaderTests.Load(),InitialDealLoaderTests.Load(),Pcg32Random.FromSeed(42,54));
        }
        public static GameState WithDeal(GameState source,InitialDealResult deal)=>new GameState(source.Map,source.Countries,source.Operations,
            source.GameId,source.Version,source.SpentOps,source.EventSequence,source.Ended,influenceEvent:source.InfluenceEvent,turn:source.Turn,
            deployment:source.Deployment,cards:deal.Cards,randomState:deal.Random);
        GameState State()=>WithDeal(SetupDeploymentTests.Empty(map),deal);
        void UnchangedCards(GameState state)
        {
            Assert.That(state.Cards,Is.SameAs(deal.Cards));Assert.That(state.RandomState,Is.SameAs(deal.Random));
            foreach(var side in new[]{PlayerSide.US,PlayerSide.USSR})Assert.That(state.GetHand(side),Is.EqualTo(deal.Cards.GetHand(side).Select(c=>c.Value)));
        }
        [Test] public void Authoritative_card_state_is_the_only_hand_source_and_is_immutable()
        {
            var state=State();UnchangedCards(state);Assert.That(state.GetHand(PlayerSide.US),Has.Count.EqualTo(8));
            Assert.Throws<NotSupportedException>(()=>((IList<string>)state.GetHand(PlayerSide.US))[0]="override");
            Assert.That(()=>state.GetHand((PlayerSide)99),Throws.InstanceOf<ArgumentException>());
        }
        [Test] public void Cards_and_random_are_required_together_and_cannot_compete_with_fixture_hands()
        {
            var source=SetupDeploymentTests.Empty(map);
            Assert.That(()=>new GameState(map,source.Countries,source.Operations,cards:deal.Cards),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>new GameState(map,source.Countries,source.Operations,randomState:deal.Random),Throws.InstanceOf<ArgumentException>());
            var hands=new Dictionary<PlayerSide,IReadOnlyList<string>>{{PlayerSide.US,Array.Empty<string>()},{PlayerSide.USSR,Array.Empty<string>()}};
            Assert.That(()=>new GameState(map,source.Countries,source.Operations,hands:hands,cards:deal.Cards,randomState:deal.Random),Throws.InstanceOf<ArgumentException>());
            Assert.That(source.Cards,Is.Null);Assert.That(source.RandomState,Is.Null);
        }
        [Test] public void Both_deployment_transitions_keep_cards_and_random_without_redealing()
        {
            var a=InitialDeploymentFlow.Begin(State(),SetupDeploymentTests.Definition(map),"deal-deployment");Assert.That(a.Accepted,Is.True);UnchangedCards(a.NextState);
            var b=InitialDeploymentFlow.Resolve(a.NextState,PlayerSide.USSR,a.NextState.Deployment.DecisionId,new[]{new InfluenceAllocation(new CountryId("country.poland"),6)});
            Assert.That(b.Accepted,Is.True);UnchangedCards(b.NextState);
            var c=InitialDeploymentFlow.Resolve(b.NextState,PlayerSide.US,b.NextState.Deployment.DecisionId,new[]{new InfluenceAllocation(new CountryId("country.canada"),7)});
            Assert.That(c.Accepted,Is.True);Assert.That(c.NextState.Deployment.Complete,Is.True);UnchangedCards(c.NextState);
        }
        [Test] public void Event_and_ordinary_influence_copies_keep_authoritative_cards()
        {
            var before=WithDeal(InfluenceEventFlowTests.State(),deal);
            var begin=InfluenceEventFlow.Begin(before,PlayerSide.US);Assert.That(begin.Accepted,Is.True);UnchangedCards(begin.NextState);
            var done=InfluenceEventFlow.Resolve(begin.NextState,PlayerSide.USSR,begin.NextState.InfluenceEvent.DecisionId,InfluenceEventFlowTests.Four());
            Assert.That(done.Accepted,Is.True);UnchangedCards(done.NextState);
            var placement=InfluenceRules.PlanPlacement(done.NextState,done.NextState.Operations,new[]{new CountryId("country.angola")});
            Assert.That(placement.Accepted,Is.True);UnchangedCards(placement.NextState);
        }
        [TestCase(PlayerSide.US)] [TestCase(PlayerSide.USSR)]
        public void Normal_projection_contains_only_own_cards_and_public_counts(PlayerSide side)
        {
            var host=new LocalGameHost(State(),false);var session=side==PlayerSide.US?host.UsSession:host.UssrSession;var view=session.ReadView();
            var other=side==PlayerSide.US?PlayerSide.USSR:PlayerSide.US;
            Assert.That(view.OwnHand,Is.EqualTo(deal.Cards.GetHand(side).Select(c=>c.Value)));Assert.That(view.OpponentHandCount,Is.EqualTo(8));
            Assert.That(view.Cards.DrawPileCount,Is.EqualTo(19));Assert.That(view.Cards.EraReserveCount,Is.EqualTo(67));
            Assert.That(view.Cards.China.Owner,Is.EqualTo(PlayerSide.USSR));Assert.That(view.Cards.China.Face,Is.EqualTo(CardFace.FaceUp));
            Assert.That(view.Cards.DiscardCount+view.Cards.RemovedCount+view.Cards.ResolvingCount,Is.Zero);
            var json=JsonConvert.SerializeObject(view);
            foreach(var id in deal.Cards.GetHand(other).Concat(deal.Cards.DrawPile))Assert.That(json,Does.Not.Contain(id.Value));
            Assert.That(json,Does.Not.Contain("DrawCount"));Assert.That(json,Does.Not.Contain("Increment"));Assert.That(json,Does.Not.Contain("AlgorithmId"));
            Assert.That(typeof(PublicCardSummary).GetProperties().Any(p=>p.PropertyType==typeof(CardZones) || p.PropertyType==typeof(GameState) || p.PropertyType==typeof(RandomSnapshot)),Is.False);
            Assert.Throws<InvalidOperationException>(()=>host.ReadLocalTestSnapshot());
        }
        [Test] public void Enabled_observer_only_adds_both_hands_and_legacy_fixtures_have_no_card_summary()
        {
            var host=new LocalGameHost(State(),true);var observed=host.ReadLocalTestSnapshot();
            Assert.That(observed.USHand,Is.EqualTo(deal.Cards.GetHand(PlayerSide.US).Select(c=>c.Value)));Assert.That(observed.USSRHand,Is.EqualTo(deal.Cards.GetHand(PlayerSide.USSR).Select(c=>c.Value)));
            Assert.That(JsonConvert.SerializeObject(observed),Does.Not.Contain("AlgorithmId"));
            var fixture=new LocalGameHost(SetupDeploymentTests.Empty(map),true);Assert.That(fixture.UsSession.ReadView().Cards,Is.Null);
        }
        [Test] public async Task Deployment_rejection_and_historical_retry_do_not_change_dealt_hands()
        {
            var start=InitialDeploymentFlow.Begin(State(),SetupDeploymentTests.Definition(map),"deal-retry").NextState;
            var host=new LocalGameHost(start,true);var session=host.UssrSession;var v=session.ReadView();
            DeploymentCommandEnvelope Cmd(string id,int n)=>new DeploymentCommandEnvelope(id,v.GameId,v.Version,v.Deployment.ExecutionId,v.Deployment.DecisionId,new[]{new InfluenceAllocation(new CountryId("country.poland"),n)});
            var invalid=await session.SubmitDeploymentAsync(Cmd("bad",5));Assert.That(invalid.Accepted,Is.False);
            var command=Cmd("once",6);var result=await session.SubmitDeploymentAsync(command);Assert.That(result.Accepted,Is.True);
            Assert.That(await session.SubmitDeploymentAsync(command),Is.SameAs(result));
            Assert.That(host.UsSession.ReadView().OwnHand,Is.EqualTo(deal.Cards.GetHand(PlayerSide.US).Select(c=>c.Value)));
            Assert.That(session.ReadView().OwnHand,Is.EqualTo(deal.Cards.GetHand(PlayerSide.USSR).Select(c=>c.Value)));
            Assert.That(session.ReadView().Cards.DrawPileCount,Is.EqualTo(19));
        }
    }
}
