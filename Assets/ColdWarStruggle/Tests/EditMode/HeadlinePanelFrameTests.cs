using System;
using System.Collections.Generic;
using System.Linq;
using ColdWarStruggle.Core;
using ColdWarStruggle.Application;
using ColdWarStruggle.Presentation;
using Newtonsoft.Json;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class HeadlinePanelFrameTests
    {
        internal static LocalGameHost Host(string us="defectors",string su="fidel",int level=5)
            =>new LocalGameHost(HeadlineFlowTests.Start(DefectorsHeadlineResolutionTests.Prepared(us,su,collecting:true,level:level)),true);
        internal static Dictionary<string,string> Names()=>CardCatalogLoaderTests.Load().Cards.ToDictionary(c=>c.Id.Value,c=>c.Id.Value,StringComparer.Ordinal);
        static HeadlineCommandResult Submit(IGameSession s,string card)=>HeadlineSessionTests.Submit(s,card);
        [Test] public void Normal_frame_never_discloses_first_opponent_card_or_priority()
        {
            var h=Host();Submit(h.UsSession,"defectors");var frame=HeadlinePanelFrame.Create(h.UssrSession.ReadView(),Names());
            Assert.That(frame.OpponentLocked,Is.True);Assert.That(frame.OwnLocked,Is.False);Assert.That(frame.CanChoose,Is.True);Assert.That(frame.IsObserver,Is.False);
            Assert.That(frame.ResolutionLines,Is.Empty);Assert.That(frame.ObserverLines,Is.Empty);Assert.That(JsonConvert.SerializeObject(frame),Does.Not.Contain("card.defectors"));
            var own=HeadlinePanelFrame.Create(h.UsSession.ReadView(),Names());Assert.That(own.CanChoose,Is.False);Assert.That(own.OwnLocked,Is.True);Assert.That(own.Prompt,Does.Contain("card.defectors"));
        }
        [Test] public void Explicit_observer_can_see_reserved_card_without_changing_selection_permission()
        {
            var h=Host();Submit(h.UsSession,"defectors");var v=h.UssrSession.ReadView();var before=v.Version;
            var f=HeadlinePanelFrame.Create(v,Names(),h.ReadLocalTestSnapshot());Assert.That(f.IsObserver,Is.True);Assert.That(f.ObserverLines,Has.Count.EqualTo(1));Assert.That(f.ObserverLines.Single(),Does.Contain("card.defectors"));Assert.That(f.CanChoose,Is.True);Assert.That(h.UsSession.ReadView().Version,Is.EqualTo(before));
        }
        [Test] public void Observer_must_match_game_and_version()
        {
            var h=HeadlineDemoCatalogTests.Catalog().CreateHost(HeadlineDemoCatalogTests.Id);var old=h.ReadLocalTestSnapshot();var v=h.UsSession.ReadView();
            Assert.That(h.UsSession.SubmitHeadlineAsync(new HeadlineCommand("lock",v.GameId,v.Version,v.Headline.Selection.Candidates.First())).Result.Accepted,Is.True);
            Assert.Throws<ArgumentException>(()=>HeadlinePanelFrame.Create(h.UsSession.ReadView(),Names(),old));
            var other=HeadlineDemoCatalogTests.Catalog().CreateHost(HeadlineDemoCatalogTests.Id);Assert.Throws<ArgumentException>(()=>HeadlinePanelFrame.Create(other.UsSession.ReadView(),Names(),h.ReadLocalTestSnapshot()));
        }
        [Test] public void Resolved_lines_follow_actual_queue_and_distinguish_cancellation()
        {
            var h=Host(su:"red_scare_purge");Submit(h.UsSession,"defectors");Submit(h.UssrSession,"red_scare_purge");var f=HeadlinePanelFrame.Create(h.UsSession.ReadView(),Names());
            Assert.That(f.CanChoose,Is.False);Assert.That(f.ResolutionLines,Has.Count.EqualTo(2));Assert.That(f.ResolutionLines[0],Does.Contain("card.defectors"));Assert.That(f.ResolutionLines[0],Does.Contain("已发生"));Assert.That(f.ResolutionLines[1],Does.Contain("已取消"));Assert.That(f.ResolutionLines[1],Does.Contain("card.red_scare_purge"));
        }
        [Test] public void Waiting_and_terminal_lines_never_mark_unfinished_roots_successful()
        {
            var h=Host("decolonization","defectors");Submit(h.UsSession,"decolonization");Submit(h.UssrSession,"defectors");var f=HeadlinePanelFrame.Create(h.UsSession.ReadView(),Names());
            Assert.That(f.Prompt,Does.Contain("苏联"));Assert.That(f.ResolutionLines[0],Does.Contain("等待"));Assert.That(f.ResolutionLines[1],Does.Contain("待结算"));
            h=Host("duck_and_cover","defectors",2);Submit(h.UsSession,"duck_and_cover");Submit(h.UssrSession,"defectors");f=HeadlinePanelFrame.Create(h.UsSession.ReadView(),Names());
            Assert.That(f.ResolutionLines.Any(x=>x.Contains("已发生")||x.Contains("已取消")),Is.False);Assert.That(f.Prompt,Does.Contain("终局"));
        }
        [Test] public void No_supported_candidates_is_an_explicit_boundary()
        {var f=HeadlinePanelFrame.Create(Host("blockade","fidel").UsSession.ReadView(),Names());Assert.That(f.Candidates,Is.Empty);Assert.That(f.Prompt,Does.Contain("尚未接入"));}
        [Test] public void Space_selection_wait_displays_wait_instead_of_unsupported_boundary()
        {
            var space=new SpaceRaceDefinition(0,1,1,1,new[]{new SpaceStepDefinition("space.one",1,1,1,1,0,0,"ability.headline"),new SpaceStepDefinition("space.two",2,1,1,1,0,0)},new[]{new SpaceAbilityDefinition("ability.headline",SpaceAbilityKind.HeadlineSelectionPolicy,headlinePolicy:"OpponentRevealsFirst")});
            var s=HeadlineFlowTests.Start(HeadlineFlowTests.Prepared(earlyOnly:true,space:space));s=HeadlineFlowTests.Choose(s,PlayerSide.US,"fidel");s=HeadlineFlowTests.Choose(s,PlayerSide.USSR,"romanian_abdication");
            s=PlayTurnOpsEventTests.Give(s,PlayerSide.USSR,"card.olympic_games");s=NextTurnPlayTests.Apply(s,CardActionKind.Influence,"card.olympic_games");s=NextTurnPlayTests.Apply(s,CardActionKind.FinishInfluence);
            s=PlayTurnOpsEventTests.Give(s,PlayerSide.US,"card.olympic_games");s=NextTurnPlayTests.Apply(s,CardActionKind.Space,"card.olympic_games");s=NextTurnPlayTests.Assessed(s);
            var h=new LocalGameHost(s,true);var v=h.UsSession.ReadView();Assert.That(h.UsSession.SubmitCardActionAsync(new CardActionCommand("next",v.GameId,v.Version,CardActionKind.BeginNextTurn)).Result.Accepted,Is.True);
            var f=HeadlinePanelFrame.Create(h.UsSession.ReadView(),Names());Assert.That(f.CanChoose,Is.False);Assert.That(f.Prompt,Does.Contain("等待对方先选"));Assert.That(f.Prompt,Does.Not.Contain("尚未接入"));
            v=h.UssrSession.ReadView();var card=v.Headline.Selection.Candidates.First();Assert.That(h.UssrSession.SubmitHeadlineAsync(new HeadlineCommand("first",v.GameId,v.Version,card)).Result.Accepted,Is.True);
            f=HeadlinePanelFrame.Create(h.UsSession.ReadView(),Names());Assert.That(f.CanChoose,Is.True);Assert.That(f.Prompt,Does.Not.Contain("等待对方先选"));Assert.That(f.Prompt,Does.Contain(card.Value));
        }
        [Test] public void Legacy_view_has_no_headline_rows()
        {
            var h=new LocalGameHost(HeadlineFlowTests.Start(DefectorsHeadlineResolutionTests.Prepared()),true);var f=HeadlinePanelFrame.Create(h.UsSession.ReadView(),Names());Assert.That(f.CanChoose,Is.False);Assert.That(f.Candidates,Is.Empty);Assert.That(f.ResolutionLines,Is.Empty);Assert.That(f.Prompt,Does.Contain("跳过头条"));
        }
        [Test] public void Exposed_collections_are_readonly()
        {
            var f=HeadlinePanelFrame.Create(Host().UsSession.ReadView(),Names());Assert.Throws<NotSupportedException>(()=>((IList<CardId>)f.Candidates).Clear());Assert.Throws<NotSupportedException>(()=>((IList<string>)f.ResolutionLines).Clear());Assert.Throws<NotSupportedException>(()=>((IList<string>)f.ObserverLines).Clear());
        }
        [TestCase(false)][TestCase(true)] public void Missing_or_blank_used_title_is_rejected(bool blank)
        {var h=Host();Submit(h.UsSession,"defectors");var names=Names();if(blank)names["card.defectors"]=" ";else names.Remove("card.defectors");Assert.Throws<ArgumentException>(()=>HeadlinePanelFrame.Create(h.UsSession.ReadView(),names));}
    }
}
