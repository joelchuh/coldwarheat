using System;
using System.Linq;
using System.Threading.Tasks;
using ColdWarStruggle.Core;
using ColdWarStruggle.Application;
using ColdWarStruggle.Infrastructure;
using ColdWarStruggle.Presentation;
using Newtonsoft.Json;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class ChinaCardSessionTests
    {
        static CardId China=>ChinaCardPlayTests.China;
        static CountryId C(string id)=>ChinaCardPlayTests.C(id);
        internal static DemoCatalog Before()=>PlayRealignmentCatalogTests.Add(PlayTurnOpsCatalogTests.Add(PlayDefconVpCatalogTests.Add(PlayInfluenceEventCatalogTests.Add(PlayScoringCatalogTests.Catalog()))));
        internal static DemoCatalog Add(DemoCatalog c)=>c.WithPracticeChina(ChinaCardPlayTests.Data(),ChinaCardPlayTests.Schema());
        internal static DemoCatalog Catalog()=>Add(Before()).WithQuickStartScenario(QuickStartDemoTests.Data());
        [Test] public async Task Coup_preview_follows_target_and_matches_committed_strength_without_drawing_random()
        {
            var state=ChinaCardPlayTests.Ready();var board=state.Countries.ToDictionary(x=>x.Key,x=>x.Value);board[C("japan")]=new CountryState(1,1);board[C("thailand")]=new CountryState(1,1);var h=new LocalGameHost(PlayScoringTests.Replace(state,board),true);var c=new DemoController(_=>h);c.LoadScenario("preview",PlayerSide.USSR,C("japan"));
            c.SelectCard(China);Assert.That(c.SelectedCoupQuote,Is.Null);c.SelectCoupUse();var version=c.ReadView().Version;
            Assert.That(c.SelectedCoupQuote,Is.Not.Null);Assert.That(c.SelectedCoupQuote.Strength,Is.EqualTo(5));Assert.That(c.SelectedCoupQuote.MilitaryCredit,Is.EqualTo(5));
            c.SelectCountry(C("angola"));Assert.That(c.SelectedCoupQuote.Strength,Is.EqualTo(4));c.SelectCountry(C("thailand"));Assert.That(c.SelectedCoupQuote.Strength,Is.EqualTo(5));
            Assert.That(c.ReadView().Version,Is.EqualTo(version));var quote=c.SelectedCoupQuote;var result=await c.PlayCardAsync(CardActionKind.Coup);Assert.That(result.Accepted,Is.True);Assert.That(result.Coup.Strength,Is.EqualTo(quote.Strength));Assert.That(result.Coup.MilitaryCredit,Is.EqualTo(quote.MilitaryCredit));Assert.That(c.SelectedCoupQuote,Is.Null);
        }
        [Test] public void Coup_preview_rejects_stale_wrong_identity_and_unknown_target_without_quote()
        {
            var h=new LocalGameHost(ChinaCardPlayTests.Ready(),true);var v=h.UssrSession.ReadView();
            var stale=h.UssrSession.QueryCoupQuote(China,C("japan"),v.Version-1);Assert.That(stale.Error,Is.EqualTo(RuleError.StaleVersion));Assert.That(stale.Strength,Is.Null);
            var wrong=h.UsSession.QueryCoupQuote(China,C("japan"),v.Version);Assert.That(wrong.Error,Is.EqualTo(RuleError.WrongActor));Assert.That(wrong.Strength,Is.Null);
            var unknown=h.UssrSession.QueryCoupQuote(China,C("missing"),v.Version);Assert.That(unknown.Error,Is.EqualTo(RuleError.UnknownCountry));Assert.That(unknown.Strength,Is.Null);
        }
        [Test] public void Public_China_option_is_separate_from_real_hand_and_lookup_preserves_ownership_checks()
        {var h=new LocalGameHost(ChinaCardPlayTests.Ready(),true);var s=h.UssrSession;var v=s.ReadView();var o=s.QueryCardActions();Assert.That(o.China,Is.Not.Null);Assert.That(o.Find(China),Is.SameAs(o.China));Assert.That(o.Cards.Select(x=>x.Card.Value),Is.EquivalentTo(v.OwnHand));Assert.That(v.OwnHand,Has.No.Member(China.Value));Assert.That(o.China.CurrentOps,Is.EqualTo(4));Assert.That(o.China.RegionalMaximum,Is.EqualTo(5));Assert.That(o.China.Influence,Is.EqualTo(RuleError.None));Assert.That(o.China.Space,Is.EqualTo(RuleError.None));Assert.That(o.China.Event,Is.EqualTo(RuleError.UnsupportedCardAction));Assert.That(h.UsSession.QueryCardActions().China.Influence,Is.EqualTo(RuleError.WrongActor));}
        [Test] public async Task Host_China_transfer_is_idempotent_and_regional_target_query_rejects_outside_fifth()
        {var h=new LocalGameHost(ChinaCardPlayTests.Ready(),true);var session=h.UssrSession;var v=session.ReadView();var cmd=new CardActionCommand("china",v.GameId,v.Version,CardActionKind.Influence,China);var r=await session.SubmitCardActionAsync(cmd);Assert.That(r.Accepted,Is.True);Assert.That(r.View.RegionalOps,Is.Not.Null);Assert.That(r.View.RegionalOps.Total,Is.EqualTo(5));Assert.That(r.View.RegionalOps.HasTargets,Is.False);Assert.That(await session.SubmitCardActionAsync(cmd),Is.SameAs(r));Assert.That(r.View.Cards.ResolvingCount,Is.Zero);Assert.That(r.View.Cards.China.Owner,Is.EqualTo(PlayerSide.US));Assert.That((await session.SubmitCardActionAsync(new CardActionCommand("china",v.GameId,v.Version,CardActionKind.Space,China))).Error,Is.EqualTo(RuleError.CommandIdConflict));var place=await session.SubmitAsync(new CommandEnvelope("four",r.View.GameId,r.View.Version,r.View.ContextId,Enumerable.Repeat(C("japan"),4)));Assert.That(place.Accepted,Is.True);var n=place.View;Assert.That(n.RegionalOps.Spent,Is.EqualTo(4));Assert.That(n.RegionalOps.HasTargets,Is.True);var q=session.QueryInfluenceTargets(n.ContextId,n.Version);Assert.That(q.Targets,Has.Member(C("japan")));Assert.That(q.Targets,Has.No.Member(C("poland")));Assert.That(JsonConvert.SerializeObject(n.RegionalOps),Does.Not.Contain("Random").And.Not.Contain("OwnHand").And.Not.Contain("Quote"));Assert.That((await h.UsSession.SubmitCardActionAsync(new CardActionCommand("steal",n.GameId,n.Version,CardActionKind.Space,China))).Error,Is.EqualTo(RuleError.WrongActor));}
        [Test] public async Task Controller_selects_owned_China_and_routes_coup_then_clears_identity_intent()
        {var h=new LocalGameHost(ChinaCardPlayTests.Ready(),true);var c=new DemoController(_=>h);c.LoadScenario("test",PlayerSide.USSR,C("angola"));c.SelectCard(China);Assert.That(c.SelectedCard,Is.EqualTo(China));c.SelectCoupUse();Assert.That(c.LegalTargets().Targets,Has.Member(C("angola")));c.SelectSide(PlayerSide.US);Assert.That(c.SelectedCard,Is.Null);Assert.That(c.SelectingCoup,Is.False);Assert.Throws<ArgumentException>(()=>c.SelectCard(China));c.SelectSide(PlayerSide.USSR);c.SelectCard(China);c.SelectCoupUse();var r=await c.PlayCardAsync(CardActionKind.Coup);Assert.That(r.Accepted,Is.True);Assert.That(r.Coup.Strength,Is.EqualTo(4));Assert.That(await c.RetryCardAsync(),Is.SameAs(r));c.SelectSide(PlayerSide.US);Assert.Throws<ArgumentException>(()=>c.SelectCard(China));}
        [Test] public async Task Controller_China_influence_finish_keeps_hand_count_and_face_down_after_switch()
        {var h=new LocalGameHost(ChinaCardPlayTests.Ready(),true);var c=new DemoController(_=>h);c.LoadScenario("test",PlayerSide.USSR,C("vietnam"));var count=c.ReadView().OwnHand.Count;c.SelectCard(China);var r=await c.PlayCardAsync(CardActionKind.Influence);Assert.That(r.Accepted,Is.True);Assert.That((await c.SubmitAsync(5)).Accepted,Is.True);Assert.That(c.ReadView().RemainingOps,Is.Zero);Assert.That((await c.FinishInfluenceAsync()).Accepted,Is.True);Assert.That(c.ReadView().OwnHand.Count,Is.EqualTo(count));Assert.That(c.ReadView().RegionalOps,Is.Null);c.SelectSide(PlayerSide.US);Assert.That(c.CardOptions().China.Influence,Is.EqualTo(RuleError.ChinaUnavailable));Assert.That(c.ReadView().Cards.DiscardCount,Is.Zero);}
        [Test] public void Catalog_assembles_shared_China_data_without_replacing_event_or_initial_deal_instances()
        {var before=Before();var old=before.Scenarios.Single(s=>s.PracticeDefinition!=null);var after=Add(before);var scenario=after.Scenarios.Single(s=>s.PracticeDefinition!=null);Assert.That(scenario.PracticeDefinition.China.Map,Is.SameAs(after.Map));Assert.That(scenario.PracticeDefinition.China.Cards,Is.SameAs(after.PrintedCards));Assert.That(scenario.InitialCards,Is.SameAs(old.InitialCards));Assert.That(scenario.PracticeDefinition.Realignment,Is.SameAs(old.PracticeDefinition.Realignment));Assert.That(scenario.PracticeDefinition.TurnOpsEvents,Is.SameAs(old.PracticeDefinition.TurnOpsEvents));Assert.That(scenario.PracticeDefinition.InfluenceEvents,Is.SameAs(old.PracticeDefinition.InfluenceEvents));var quick=after.WithQuickStartScenario(QuickStartDemoTests.Data());Assert.That(quick.CreateHost(quick.Scenarios.Last().Id).UssrSession.QueryCardActions().China.Influence,Is.EqualTo(RuleError.None));}
        [Test] public void Catalog_rejects_early_duplicate_and_after_quick_start_assembly()
        {Assert.That(()=>Add(PlayScoringCatalogTests.Catalog()),Throws.InstanceOf<ArgumentException>());Assert.That(()=>Add(Add(Before())),Throws.InstanceOf<ArgumentException>());Assert.That(()=>Add(Before().WithQuickStartScenario(QuickStartDemoTests.Data())),Throws.InstanceOf<ArgumentException>());}
        [Test] public async Task Default_deal_China_can_go_to_space_with_exactly_one_action_and_no_hand_discard()
        {var catalog=Catalog();var h=catalog.CreateHost(catalog.Scenarios.Last().Id);var session=h.UssrSession;var v=session.ReadView();var r=await session.SubmitCardActionAsync(new CardActionCommand("china-space",v.GameId,v.Version,CardActionKind.Space,China));Assert.That(r.Accepted,Is.True);Assert.That(r.View.Practice.CompletedActions,Is.EqualTo(1));Assert.That(r.View.OwnHand,Is.EqualTo(v.OwnHand));Assert.That(r.View.Cards.DiscardCount,Is.EqualTo(v.Cards.DiscardCount));Assert.That(r.View.Cards.China.Owner,Is.EqualTo(PlayerSide.US));Assert.That(r.View.Cards.China.Face,Is.EqualTo(CardFace.FaceDown));}
    }
}
