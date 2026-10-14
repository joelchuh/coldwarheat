using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using ColdWarStruggle.Core;
using ColdWarStruggle.Application;
using ColdWarStruggle.Presentation;
using Newtonsoft.Json;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class PlayScoringSessionTests
    {
        static CardActionCommand Command(IGameSession s,string card="card.europe_scoring",string id="score")
        {var v=s.ReadView();return new CardActionCommand(id,v.GameId,v.Version,CardActionKind.Scoring,new CardId(card));}
        [Test] public async Task Scoring_result_and_retry_are_filtered_and_do_not_double_apply()
        {
            var h=new LocalGameHost(PlayScoringTests.Ready(),false);var s=h.UssrSession;var cmd=Command(s);var v=s.ReadView();
            Assert.That(s.QueryCardActions().Cards.Single(x=>x.Card.Equals(cmd.Card.Value)).Scoring,Is.EqualTo(RuleError.None));
            var r=await s.SubmitCardActionAsync(cmd);Assert.That(r.Accepted,Is.True);Assert.That(r.Scoring,Is.Not.Null);Assert.That(r.View.Practice.HasScoring,Is.True);Assert.That(r.Scoring.RegionId,Is.EqualTo("region.europe"));
            Assert.That(await s.SubmitCardActionAsync(cmd),Is.SameAs(r));Assert.That(s.ReadView().Version,Is.EqualTo(v.Version+1));
            Assert.That((await s.SubmitCardActionAsync(new CardActionCommand(cmd.CommandId,cmd.GameId,cmd.ExpectedVersion,CardActionKind.Space,cmd.Card))).Error,Is.EqualTo(RuleError.CommandIdConflict));
            var json=JsonConvert.SerializeObject(r.Scoring);foreach(var forbidden in new[]{"Definition","SnapshotId","Random","Countries","Receipt"})Assert.That(json,Does.Not.Contain(forbidden));
        }
        [Test] public void Preview_is_public_readonly_and_side_independent_for_all_regions()
        {
            var state=PlayScoringTests.Ready();var h=new LocalGameHost(state,false);var before=h.UsSession.ReadView();
            foreach(var region in state.Practice.Definition.Scoring.Regions.Select(r=>r.Id).Concat(new[]{"region.southeast_asia"}))
            {
                var a=h.UsSession.QueryScoringPreview(region,before.Version);var b=h.UssrSession.QueryScoringPreview(region,before.Version);
                Assert.That(a.Error,Is.EqualTo(RuleError.None));Assert.That(a.Score.RegionId,Is.EqualTo(region));Assert.That(JsonConvert.SerializeObject(a),Is.EqualTo(JsonConvert.SerializeObject(b)));
            }
            var after=h.UsSession.ReadView();Assert.That(after.Version,Is.EqualTo(before.Version));Assert.That(after.Practice.VpForUS,Is.EqualTo(before.Practice.VpForUS));Assert.That(after.OwnHand,Is.EqualTo(before.OwnHand));Assert.That(after.Cards.DiscardCount,Is.EqualTo(before.Cards.DiscardCount));
        }
        [Test] public void Preview_does_not_depend_on_which_side_holds_the_scoring_card()
        {
            var s=PlayScoringTests.Ready();var cards=new CardZones(s.Practice.Definition.Cards,s.Cards.DrawPile,new Dictionary<PlayerSide,IReadOnlyList<CardId>> {{PlayerSide.US,s.Cards.GetHand(PlayerSide.USSR)},{PlayerSide.USSR,s.Cards.GetHand(PlayerSide.US)}},s.Cards.EraReserve,s.Cards.DiscardPile,s.Cards.RemovedCards,s.Cards.ResolvingCards,s.Cards.China,s.Cards.EnteredEras);
            var alternate=new GameState(s.Map,s.Countries,s.Operations,s.GameId,s.Version,s.SpentOps,s.EventSequence,turn:s.Turn,cards:cards,randomState:s.RandomState,practice:s.Practice);
            var a=new LocalGameHost(s,false).UsSession.QueryScoringPreview("region.europe",s.Version);var b=new LocalGameHost(alternate,false).UsSession.QueryScoringPreview("region.europe",s.Version);
            Assert.That(JsonConvert.SerializeObject(a),Is.EqualTo(JsonConvert.SerializeObject(b)));
        }
        [Test] public void Preview_rejects_stale_unknown_and_unconfigured_without_default_scores()
        {
            var h=new LocalGameHost(PlayScoringTests.Ready(),true);var s=h.UsSession;var v=s.ReadView();Assert.That(s.QueryScoringPreview("region.europe",v.Version-1).Error,Is.EqualTo(RuleError.StaleVersion));
            foreach(var region in new[]{"region.missing",null,new string('x',257)}){var r=s.QueryScoringPreview(region,v.Version);Assert.That(r.Error,Is.EqualTo(RuleError.InvalidCommand));Assert.That(r.Score,Is.Null);}
            var legacy=new LocalGameHost(PlayCoupTests.Ready(),true).UsSession;Assert.That(legacy.QueryScoringPreview("region.europe",legacy.ReadView().Version).Error,Is.EqualTo(RuleError.UnsupportedCardAction));
        }
        [Test] public async Task Controller_uses_scoring_and_refreshes_preview_after_state_change()
        {
            var c=new DemoController(id=>new LocalGameHost(PlayScoringTests.Ready(),true));c.LoadScenario("scoring",PlayerSide.USSR,new CountryId("country.poland"));
            var before=c.ScoringPreview("region.europe");Assert.That(c.ScoringPreview("region.europe"),Is.SameAs(before));
            c.SelectCard(new CardId("card.europe_scoring"));Assert.That((await c.PlayCardAsync(CardActionKind.Scoring)).Accepted,Is.True);Assert.That(c.LastCardResult.Scoring,Is.Not.Null);Assert.That(c.SelectedCard,Is.Null);
            Assert.That(c.ScoringPreview("region.europe").Version,Is.GreaterThan(before.Version));Assert.That(c.ScoringPreview("region.asia").Score.RegionId,Is.EqualTo("region.asia"));
        }
        [Test] public async Task Terminal_scoring_projects_reason_keeps_receipt_and_allows_only_read_preview()
        {
            var state=PlayScoringTests.Ready(empty:true);var countries=state.Countries.ToDictionary(x=>x.Key,x=>x.Value);RegionScoringTests.Control(state.Map,countries,PlayerSide.US,state.Map.GetCountriesInRegion("region.europe").Where(c=>c.Battleground).Select(c=>c.Id.Value).ToArray());
            var h=new LocalGameHost(PlayScoringTests.Replace(state,countries),false);var s=h.UssrSession;var cmd=Command(s);var result=await s.SubmitCardActionAsync(cmd);
            Assert.That(result.View.Practice.Winner,Is.EqualTo(PlayerSide.US));Assert.That(result.View.Practice.EndReason,Is.EqualTo(VictoryCheckpoint.ScoringResolved));Assert.That(await s.SubmitCardActionAsync(cmd),Is.SameAs(result));
            Assert.That(s.QueryScoringPreview("region.europe",s.ReadView().Version).Score.EuropeControlWinner,Is.EqualTo(PlayerSide.US));
            Assert.That((await s.SubmitCardActionAsync(Command(s,id:"after"))).Error,Is.EqualTo(RuleError.GameAlreadyEnded));
        }
        [Test] public void Ordinary_event_and_wrong_actor_scoring_options_are_not_enabled()
        {
            var s=PlayScoringTests.Ready();var h=new LocalGameHost(s,true);var eventCard=PlayCoupTests.Card(s);
            Assert.That(h.UssrSession.QueryCardActions().Cards.Single(x=>x.Card.Equals(eventCard)).Scoring,Is.EqualTo(RuleError.UnsupportedCardAction));
            Assert.That(h.UsSession.QueryCardActions().Cards.All(x=>x.Scoring==RuleError.WrongActor),Is.True);
        }
    }
}
