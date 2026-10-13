using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ColdWarStruggle.Core;
using ColdWarStruggle.Application;
using ColdWarStruggle.Presentation;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class HandPanelTests
    {
        [Test] public void Normal_frame_changes_identity_without_retaining_the_other_hand()
        {
            var data=DealtDemoCatalogTests.Catalog();var host=data.CreateHost(DealtDemoCatalogTests.ScenarioId);
            var us=HandPanelFrame.Create(host.UsSession.ReadView(),data.PrintedCards,data.Names);
            var soviet=HandPanelFrame.Create(host.UssrSession.ReadView(),data.PrintedCards,data.Names);
            Assert.That(us.IsObserver,Is.False);Assert.That(us.Rows,Has.Count.EqualTo(1));Assert.That(us.Rows[0].Side,Is.EqualTo(PlayerSide.US));
            Assert.That(soviet.Rows,Has.Count.EqualTo(1));Assert.That(soviet.Rows[0].Side,Is.EqualTo(PlayerSide.USSR));
            Assert.That(us.Rows[0].Cards.Select(c=>c.Id.Value),Is.EqualTo(host.UsSession.ReadView().OwnHand));
            Assert.That(soviet.Rows[0].Cards.Select(c=>c.Id.Value),Is.EqualTo(host.UssrSession.ReadView().OwnHand));
            Assert.That(us.Rows[0].Cards.Select(c=>c.Id).Intersect(soviet.Rows[0].Cards.Select(c=>c.Id)),Is.Empty);
            Assert.Throws<NotSupportedException>(()=>((IList<HandPanelRow>)us.Rows).Clear());Assert.Throws<NotSupportedException>(()=>((IList<HandCardDisplay>)us.Rows[0].Cards).Clear());
        }
        [Test] public void Observer_frame_shows_both_sides_with_current_viewer_first_and_no_authority_object()
        {
            var data=DealtDemoCatalogTests.Catalog();var host=data.CreateHost(DealtDemoCatalogTests.ScenarioId);var v=host.UssrSession.ReadView();
            var frame=HandPanelFrame.Create(v,data.PrintedCards,data.Names,host.ReadLocalTestSnapshot());
            Assert.That(frame.IsObserver,Is.True);Assert.That(frame.Rows.Select(r=>r.Side),Is.EqualTo(new[]{PlayerSide.USSR,PlayerSide.US}));
            Assert.That(frame.Rows.Sum(r=>r.Cards.Count),Is.EqualTo(16));Assert.That(frame.GameId,Is.EqualTo(v.GameId));Assert.That(frame.Version,Is.EqualTo(v.Version));
            Assert.That(typeof(HandPanelFrame).GetProperties().Any(p=>p.PropertyType==typeof(GameState) || p.PropertyType==typeof(CardZones) || p.PropertyType==typeof(RandomSnapshot)),Is.False);
        }
        [Test] public async Task Stale_or_foreign_observer_snapshot_is_rejected_before_rendering_hidden_cards()
        {
            var data=DealtDemoCatalogTests.Catalog();var host=data.CreateHost(DealtDemoCatalogTests.ScenarioId);var old=host.ReadLocalTestSnapshot();var v=host.UssrSession.ReadView();
            var foreign=data.CreateHost(DealtDemoCatalogTests.ScenarioId).ReadLocalTestSnapshot();
            Assert.That(()=>HandPanelFrame.Create(v,data.PrintedCards,data.Names,foreign),Throws.InstanceOf<ArgumentException>());
            var result=await host.UssrSession.SubmitDeploymentAsync(new DeploymentCommandEnvelope("progress",v.GameId,v.Version,v.Deployment.ExecutionId,v.Deployment.DecisionId,new[]{new InfluenceAllocation(new CountryId("country.poland"),6)}));
            Assert.That(result.Accepted,Is.True);Assert.That(()=>HandPanelFrame.Create(host.UssrSession.ReadView(),data.PrintedCards,data.Names,old),Throws.InstanceOf<ArgumentException>());
        }
        [Test] public void Scoring_display_preserves_absent_ops_and_card_title_comes_from_shared_names()
        {
            var data=DealtDemoCatalogTests.Catalog();var cat=data.PrintedCards;var scoring=new CardId("card.asia_scoring");var eventCard=new CardId("card.fidel");
            var hands=new Dictionary<PlayerSide,IReadOnlyList<CardId>>{{PlayerSide.US,new[]{scoring}},{PlayerSide.USSR,new[]{eventCard}}};
            var zones=new CardZones(cat,cat.GetEraCards(GameEra.Early).Select(c=>c.Id).Except(new[]{scoring,eventCard}),hands,
                cat.Cards.Where(c=>c.Era==GameEra.Mid || c.Era==GameEra.Late).Select(c=>c.Id),Array.Empty<CardId>(),Array.Empty<CardId>(),Array.Empty<CardId>(),CardZonesTests.China(),new[]{GameEra.Early});
            var source=SetupDeploymentTests.Empty(data.Map);var state=new GameState(data.Map,source.Countries,source.Operations,cards:zones,randomState:Pcg32Random.FromSeed(0,0).Capture());
            var host=new LocalGameHost(state,true);var frame=HandPanelFrame.Create(host.UsSession.ReadView(),cat,data.Names,host.ReadLocalTestSnapshot());
            var score=frame.Rows[0].Cards.Single();Assert.That(score.PrintedOps,Is.Null);Assert.That(score.Kind,Is.EqualTo(CardKind.Scoring));Assert.That(score.Title,Is.EqualTo("Asia Scoring"));
            var fidel=frame.Rows[1].Cards.Single();Assert.That(fidel.PrintedOps,Is.EqualTo(2));Assert.That(fidel.Affiliation,Is.EqualTo(CardAffiliation.USSR));Assert.That(fidel.RemoveAfterEvent,Is.True);
        }
        [Test] public void Missing_static_inputs_and_legacy_fixture_do_not_guess_real_card_data()
        {
            var data=DealtDemoCatalogTests.Catalog();var view=data.CreateHost(DealtDemoCatalogTests.ScenarioId).UsSession.ReadView();
            Assert.That(()=>HandPanelFrame.Create(null,data.PrintedCards,data.Names),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>HandPanelFrame.Create(view,null,data.Names),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>HandPanelFrame.Create(view,data.PrintedCards,null),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>HandPanelFrame.Create(view,data.PrintedCards,new Dictionary<string,string>()),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>HandPanelFrame.Create(data.CreateHost(DeploymentDemoTests.ScenarioId).UsSession.ReadView(),data.PrintedCards,data.Names),Throws.InstanceOf<ArgumentException>());
        }
        [Test] public void Hand_launch_option_is_explicit_and_rejects_ambiguous_or_unknown_values()
        {
            Assert.That(DemoLaunchOptions.ShowHands(Array.Empty<string>()),Is.False);Assert.That(DemoLaunchOptions.ShowHands(new[]{"--demo-hands=open"}),Is.True);
            Assert.That(DemoLaunchOptions.ShowHands(new[]{"--demo-hands=closed"}),Is.False);
            Assert.That(()=>DemoLaunchOptions.ShowHands(null),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>DemoLaunchOptions.ShowHands(new[]{"--demo-hands=unknown"}),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>DemoLaunchOptions.ShowHands(new[]{"--demo-hands=open","--demo-hands=closed"}),Throws.InstanceOf<ArgumentException>());
        }
    }
}
