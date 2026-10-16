using System;
using ColdWarStruggle.Core;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public sealed class OlympicContestTests
    {
        [TestCase(PlayerSide.US)][TestCase(PlayerSide.USSR)]
        public void Every_raw_pair_resolves_for_either_sponsor_with_unclamped_totals(PlayerSide sponsor)
        {
            var d=OlympicDefinitionTests.Load();int ties=0,sponsorWins=0,opponentWins=0;
            for(int us=1;us<=6;us++)for(int su=1;su<=6;su++)
            {
                var r=OlympicContestRules.Calculate(d,sponsor,us,su);var a=us+(sponsor==PlayerSide.US?2:0);var b=su+(sponsor==PlayerSide.USSR?2:0);
                Assert.That(r.UsRoll,Is.EqualTo(us));Assert.That(r.UssrRoll,Is.EqualTo(su));Assert.That(r.UsTotal,Is.EqualTo(a));Assert.That(r.UssrTotal,Is.EqualTo(b));Assert.That(r.Sponsor,Is.EqualTo(sponsor));
                Assert.That(r.Tie,Is.EqualTo(a==b));Assert.That(r.Winner,Is.EqualTo(a==b?(PlayerSide?)null:a>b?PlayerSide.US:PlayerSide.USSR));Assert.That(r.AwardVp,Is.EqualTo(a==b?0:2));
                if(r.Tie)ties++;else if(r.Winner==sponsor)sponsorWins++;else opponentWins++;
            }
            Assert.That(ties,Is.EqualTo(4));Assert.That(sponsorWins,Is.EqualTo(26));Assert.That(opponentWins,Is.EqualTo(6));
        }
        [TestCase(0,1)][TestCase(7,1)][TestCase(1,0)][TestCase(1,7)]
        public void Raw_dice_outside_shared_faces_are_rejected(int us,int su)
            =>Assert.That(()=>OlympicContestRules.Calculate(OlympicDefinitionTests.Load(),PlayerSide.US,us,su),Throws.InstanceOf<ArgumentException>());
        [Test] public void Missing_definition_or_unknown_sponsor_is_not_a_default_side()
        {
            Assert.Throws<ArgumentNullException>(()=>OlympicContestRules.Calculate(null,PlayerSide.US,1,1));
            Assert.That(()=>OlympicContestRules.Calculate(OlympicDefinitionTests.Load(),(PlayerSide)99,1,1),Throws.InstanceOf<ArgumentException>());
        }
    }
}
