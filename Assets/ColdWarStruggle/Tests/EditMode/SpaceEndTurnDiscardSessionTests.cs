using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ColdWarStruggle.Core;
using ColdWarStruggle.Application;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class SpaceEndTurnDiscardSessionTests
    {
        [TestCase(PlayerSide.US,true)] [TestCase(PlayerSide.US,false)] [TestCase(PlayerSide.USSR,true)] [TestCase(PlayerSide.USSR,false)]
        public async Task Fixed_identity_private_candidates_and_idempotent_decision(PlayerSide owner,bool discard)
        {
            var h=new LocalGameHost(SpaceEndTurnDiscardTests.Assessed(owner),true);var own=owner==PlayerSide.US?h.UsSession:h.UssrSession;var other=owner==PlayerSide.US?h.UssrSession:h.UsSession;
            var v=own.ReadView();Assert.That(v.SpaceDiscard,Is.Not.Null);Assert.That(v.SpaceDiscard.CanChoose,Is.True);Assert.That(v.SpaceDiscard.Candidates,Has.Count.EqualTo(2));
            Assert.That(other.ReadView().SpaceDiscard.CanChoose,Is.False);Assert.That(other.ReadView().SpaceDiscard.Candidates,Is.Empty);
            Assert.Throws<NotSupportedException>(()=>((IList<CardId>)v.SpaceDiscard.Candidates).Clear());
            var json=JsonConvert.SerializeObject(other.ReadView());Assert.That(json,Does.Not.Contain("card.blockade").And.Not.Contain("card.duck_and_cover"));
            var cmd=new CardActionCommand("choose",v.GameId,v.Version,discard?CardActionKind.DiscardHeldCard:CardActionKind.KeepHeldCards,discard?SpaceEndTurnDiscardTests.Held:(CardId?)null);
            var wrong=await other.SubmitCardActionAsync(cmd);Assert.That(wrong.Error,Is.EqualTo(RuleError.WrongActor));Assert.That(wrong.View.SpaceDiscard.Candidates,Is.Empty);
            Assert.That((await own.SubmitCardActionAsync(new CardActionCommand("stale",v.GameId,v.Version-1,CardActionKind.KeepHeldCards))).Error,Is.EqualTo(RuleError.StaleVersion));
            var r=await own.SubmitCardActionAsync(cmd);Assert.That(r.Accepted,Is.True,r.Error.ToString());Assert.That(await own.SubmitCardActionAsync(cmd),Is.SameAs(r));
            Assert.That(r.View.SpaceDiscard.Candidates,Is.Empty);Assert.That(r.View.SpaceDiscard.CanChoose,Is.False);Assert.That(r.View.Version,Is.EqualTo(v.Version+1));
            var projected=JsonConvert.SerializeObject(other.ReadView().SpaceDiscard);Assert.That(projected,Does.Not.Contain("card.duck_and_cover").And.Not.Contain("Track").And.Not.Contain("Audit").And.Not.Contain("Random").And.Not.Contain("DrawPile"));
            Assert.That(other.ReadView().SpaceDiscard.DiscardedCard,Is.EqualTo(discard?SpaceEndTurnDiscardTests.Held:(CardId?)null));
            Assert.That((await own.SubmitCardActionAsync(new CardActionCommand("choose",v.GameId,v.Version,discard?CardActionKind.KeepHeldCards:CardActionKind.DiscardHeldCard,discard?(CardId?)null:SpaceEndTurnDiscardTests.Held))).Error,Is.EqualTo(RuleError.CommandIdConflict));
            Assert.That((await own.SubmitCardActionAsync(new CardActionCommand("again",v.GameId,r.View.Version,CardActionKind.KeepHeldCards))).Error,Is.EqualTo(RuleError.ActionSliceComplete));
            Assert.That(await other.SubmitCardActionAsync(cmd),Is.SameAs(wrong));Assert.That(own.ReadView().Version,Is.EqualTo(r.View.Version));
        }
    }
}
