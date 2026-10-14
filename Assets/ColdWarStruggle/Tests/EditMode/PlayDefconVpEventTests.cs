using System;
using System.Linq;
using System.Threading.Tasks;
using ColdWarStruggle.Core;
using ColdWarStruggle.Infrastructure;
using ColdWarStruggle.Application;
using ColdWarStruggle.Presentation;
using Newtonsoft.Json;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class PlayDefconVpEventTests
    {
        internal static PracticeDefconVpCatalog Effects(CardCatalog cards)=>new PracticeDefconVpCatalog(cards,DefconVpEventLoader.LoadImmediateSubset(DefconVpEventTests.Data(),DefconVpEventTests.Schema()));
        // Explicit fixture initial track/score and all-era zones; not a real fresh deal.
        internal static GameState Ready(string card="card.duck_and_cover",int level=5,int vp=0)=>PlayScoringTests.Ready(card,vp,configure:d=> {
            var r=d.Risk;var f=r.Defcon;var risk=new PracticeRiskDefinition(r.Coup,new DefconDefinition(f.Map,level,f.MaximumLevel,f.NuclearLevel,f.TurnStartImprovement,f.BattlegroundCoupDegradation,f.RestrictedActions,f.BlockedRegions),r.Military,r.Victory);
            return new ActionPracticeDefinition(d.Cards,d.Ops,d.Space,d.Calendar,risk,d.Scoring,PlayInfluenceEventTests.Effects(f.Map,d.Cards),Effects(d.Cards));});
        static CardActionTransition Play(GameState s,string card)=>ActionPracticeRules.Plan(s,s.Practice.Actor,CardActionKind.Event,"event",new CardId(card));
        [TestCase("duck_and_cover",5,4,1)][TestCase("nuclear_test_ban",2,4,0)][TestCase("nuclear_test_ban",5,5,-3)]
        public void Immediate_track_event_completes_card_and_root_without_other_side_effects(string name,int level,int after,int vp)
        {
            var card="card."+name;var s=Ready(card,level);var p=Play(s,card);Assert.That(p.Accepted,Is.True);var n=p.NextState;
            Assert.That(n.Practice.Risk.Defcon.Level,Is.EqualTo(after));Assert.That(n.Practice.VpForUS,Is.EqualTo(vp));Assert.That(n.Practice.CompletedActions,Is.EqualTo(1));Assert.That(n.Practice.Actor,Is.EqualTo(PlayerSide.US));Assert.That(n.Practice.Stage,Is.EqualTo(ActionPracticeStage.ChooseCard));
            Assert.That(n.Cards.DiscardPile,Does.Contain(new CardId(card)));Assert.That(n.Cards.ResolvingCards,Is.Empty);Assert.That(n.Cards.GetHand(PlayerSide.USSR),Has.No.Member(new CardId(card)));Assert.That(n.InfluenceEvent,Is.Null);Assert.That(n.Operations.Budget,Is.Zero);Assert.That(n.Countries,Is.EquivalentTo(s.Countries));Pcg32RandomTests.Same(s.RandomState,n.RandomState);Assert.That(n.Practice.Space,Is.SameAs(s.Practice.Space));Assert.That(n.Practice.Risk.Military,Is.SameAs(s.Practice.Risk.Military));Assert.That(p.EventTransition,Is.Null);Assert.That(p.DefconVpEvent,Is.Not.Null);
        }
        [TestCase("duck_and_cover",2,19,1,19,PlayerSide.US,VictoryCheckpoint.NuclearWarReached)]
        [TestCase("nuclear_test_ban",3,-19,3,-20,PlayerSide.USSR,VictoryCheckpoint.VpBatchClosed)]
        public void Terminal_event_stops_cleanup_root_completion_and_further_actions(string name,int level,int vp,int after,int final,PlayerSide winner,VictoryCheckpoint checkpoint)
        {
            var card="card."+name;var s=Ready(card,level,vp);var result=Play(s,card);Assert.That(result.Accepted,Is.True);var n=result.NextState;Assert.That(n.Ended,Is.True);Assert.That(n.Practice.Risk.Defcon.Level,Is.EqualTo(after));Assert.That(n.Practice.VpForUS,Is.EqualTo(final));Assert.That(n.Practice.Risk.Result.Winner,Is.EqualTo(winner));Assert.That(n.Practice.Risk.Result.Checkpoint,Is.EqualTo(checkpoint));Assert.That(n.Cards.ResolvingCards,Does.Contain(new CardId(card)));Assert.That(n.Cards.DiscardPile,Is.Empty);Assert.That(n.Practice.CompletedActions,Is.Zero);Assert.That(n.Practice.ActiveCard,Is.EqualTo(new CardId(card)));Assert.That(Play(n,card).Error,Is.EqualTo(RuleError.GameAlreadyEnded));
        }
        [TestCase(true)][TestCase(false)] public void Counter_overflow_rejects_track_score_and_card_together(bool version)
        {var s=Ready();s=PlayScoringTests.Replace(s,version:version?long.MaxValue:(long?)null,sequence:version?null:(long?)long.MaxValue);var p=Play(s,"card.duck_and_cover");Assert.That(p.Error,Is.EqualTo(RuleError.NumericOverflow));Assert.That(p.NextState,Is.Null);Assert.That(s.Practice.Risk.Defcon.Level,Is.EqualTo(5));Assert.That(s.Practice.VpForUS,Is.Zero);Assert.That(s.Cards.DiscardPile,Is.Empty);}
        [Test] public void Event_after_real_coup_and_space_keeps_all_other_histories()
        {
            var s=Ready();var c=PlayCoupTests.Coup(s,"iran");Assert.That(c.Accepted,Is.True);s=c.NextState;
            s=ActionPracticeRules.Plan(s,PlayerSide.US,CardActionKind.Space,"space",ActionPracticeTests.Playable(s,CardActionKind.Space)).NextState;var p=Play(s,"card.duck_and_cover");Assert.That(p.Accepted,Is.True);var n=p.NextState;
            Assert.That(n.Practice.Risk.CoupReceipts.Keys,Is.EquivalentTo(s.Practice.Risk.CoupReceipts.Keys));Assert.That(n.Practice.Risk.CoupReceipts.Single().Value,Is.SameAs(c.CoupReceipt));Assert.That(n.Practice.Risk.Military,Is.SameAs(s.Practice.Risk.Military));Assert.That(n.Practice.Space,Is.SameAs(s.Practice.Space));Assert.That(n.Countries,Is.EquivalentTo(s.Countries));Pcg32RandomTests.Same(n.RandomState,s.RandomState);Assert.That(n.Practice.Risk.Defcon.Level,Is.EqualTo(3));Assert.That(n.Practice.CompletedActions,Is.EqualTo(3));
        }
        [Test] public void Catalog_definition_requires_risk_shared_cards_and_disjoint_event_ids()
        {
            var s=Ready();var d=s.Practice.Definition;
            Assert.Throws<ArgumentException>(()=>new ActionPracticeDefinition(d.Cards,d.Ops,d.Space,d.Calendar,defconVpEvents:d.DefconVpEvents));
            Assert.Throws<ArgumentException>(()=>new ActionPracticeDefinition(d.Cards,d.Ops,d.Space,d.Calendar,d.Risk,defconVpEvents:Effects(CardCatalogLoaderTests.Load())));
            var overlap=new PracticeInfluenceCatalog(s.Map,d.Cards,new[]{new ClearThenControlEffect("card.duck_and_cover","effect.duck_and_cover",PlayerSide.US,new CountryId("country.cuba"))});
            Assert.Throws<ArgumentException>(()=>new ActionPracticeDefinition(d.Cards,d.Ops,d.Space,d.Calendar,d.Risk,d.Scoring,overlap,d.DefconVpEvents));
        }
        [Test] public void Wrong_actor_not_owned_unsupported_or_bad_payload_never_execute_event()
        {
            var s=Ready();var id=new CardId("card.duck_and_cover");Assert.That(ActionPracticeRules.Plan(s,PlayerSide.US,CardActionKind.Event,"wrong",id).Error,Is.EqualTo(RuleError.WrongActor));
            Assert.That(ActionPracticeRules.Plan(s,PlayerSide.USSR,CardActionKind.Event,"extra",id,new CountryId("country.iran")).Error,Is.EqualTo(RuleError.InvalidCommand));
            Assert.That(Play(s,s.Cards.GetHand(PlayerSide.US).First().Value).Error,Is.EqualTo(RuleError.CardNotOwned));
            Assert.That(Play(PlayScoringTests.Ready("card.duck_and_cover"),"card.duck_and_cover").Error,Is.EqualTo(RuleError.UnsupportedCardAction));Assert.That(Play(Ready("card.how_i_learned_to_stop_worrying"),"card.how_i_learned_to_stop_worrying").Error,Is.EqualTo(RuleError.UnsupportedCardAction));
        }
        [TestCase(false)][TestCase(true)] public async Task Host_publishes_public_feedback_and_replays_same_receipt_even_after_terminal(bool nuclear)
        {
            var h=new LocalGameHost(Ready(level:nuclear?2:5),true);var s=h.UssrSession;var v=s.ReadView();var command=new CardActionCommand("event",v.GameId,v.Version,CardActionKind.Event,new CardId("card.duck_and_cover"));
            var r=await s.SubmitCardActionAsync(command);Assert.That(r.Accepted,Is.True);Assert.That(r.DefconVpEvent,Is.Not.Null);Assert.That(r.Event,Is.Null);Assert.That(r.DefconVpEvent.Terminal,Is.EqualTo(nuclear));Assert.That(r.DefconVpEvent.Award,Is.EqualTo(nuclear?(int?)null:1));Assert.That(r.DefconVpEvent.VpForUS,Is.EqualTo(nuclear?0:1));var current=s.ReadView().Version;
            Assert.That(await s.SubmitCardActionAsync(command),Is.SameAs(r));Assert.That(s.ReadView().Version,Is.EqualTo(current));Assert.That((await s.SubmitCardActionAsync(new CardActionCommand("event",v.GameId,v.Version,CardActionKind.Space,command.Card))).Error,Is.EqualTo(RuleError.CommandIdConflict));
            var json=JsonConvert.SerializeObject(r.DefconVpEvent);Assert.That(json,Does.Not.Contain("Definition").And.Not.Contain("Random").And.Not.Contain("Hand"));
        }
        [Test] public async Task Host_rejects_old_game_version_and_wrong_side_without_track_mutation()
        {
            var h=new LocalGameHost(Ready(),true);var v=h.UssrSession.ReadView();var id=new CardId("card.duck_and_cover");
            Assert.That((await h.UssrSession.SubmitCardActionAsync(new CardActionCommand("game","wrong",v.Version,CardActionKind.Event,id))).Error,Is.EqualTo(RuleError.WrongGame));Assert.That((await h.UssrSession.SubmitCardActionAsync(new CardActionCommand("old",v.GameId,v.Version-1,CardActionKind.Event,id))).Error,Is.EqualTo(RuleError.StaleVersion));Assert.That((await h.UsSession.SubmitCardActionAsync(new CardActionCommand("side",v.GameId,v.Version,CardActionKind.Event,id))).Error,Is.EqualTo(RuleError.WrongActor));Assert.That(h.UssrSession.ReadView().Version,Is.EqualTo(v.Version));
        }
        [Test] public async Task Existing_controller_event_use_and_retry_expose_track_result()
        {var h=new LocalGameHost(Ready(),true);var c=new DemoController(_=>h);c.LoadScenario("fixture",PlayerSide.USSR,new CountryId("country.iran"));c.SelectCard(new CardId("card.duck_and_cover"));var r=await c.PlayCardAsync(CardActionKind.Event);Assert.That(r.Accepted,Is.True);Assert.That(c.LastCardResult.DefconVpEvent.DefconTo,Is.EqualTo(4));Assert.That(c.SelectedCard,Is.Null);Assert.That(await c.RetryCardAsync(),Is.SameAs(r));Assert.That(c.ReadView().Practice.CompletedActions,Is.EqualTo(1));}
    }
}
