using System;
namespace ColdWarStruggle.Core
{
    public sealed class OlympicContestResult
    {
        public PlayerSide Sponsor { get; }
        public int UsRoll { get; }
        public int UssrRoll { get; }
        public int UsTotal { get; }
        public int UssrTotal { get; }
        public bool Tie => !Winner.HasValue;
        public PlayerSide? Winner { get; }
        public int AwardVp { get; }
        internal OlympicContestResult(PlayerSide sponsor,int usRoll,int ussrRoll,int usTotal,int ussrTotal,PlayerSide? winner,int award)
        {Sponsor=sponsor;UsRoll=usRoll;UssrRoll=ussrRoll;UsTotal=usTotal;UssrTotal=ussrTotal;Winner=winner;AwardVp=award;}
    }
    public static class OlympicContestRules
    {
        public static OlympicContestResult Calculate(OlympicGamesDefinition definition,PlayerSide sponsor,int usRoll,int ussrRoll)
        {
            if(definition==null)throw new ArgumentNullException(nameof(definition));
            RequireSide(sponsor);var die=definition.Die;
            if(usRoll<die.Minimum || usRoll>die.Maximum || ussrRoll<die.Minimum || ussrRoll>die.Maximum)throw new ArgumentOutOfRangeException("Raw Olympic die outside shared faces.");
            var us=checked(usRoll+(sponsor==PlayerSide.US?definition.SponsorModifier:0));
            var su=checked(ussrRoll+(sponsor==PlayerSide.USSR?definition.SponsorModifier:0));
            PlayerSide? winner=us==su?(PlayerSide?)null:us>su?PlayerSide.US:PlayerSide.USSR;
            return new OlympicContestResult(sponsor,usRoll,ussrRoll,us,su,winner,winner.HasValue?definition.WinnerVp:definition.TieVp);
        }
        internal static void RequireSide(PlayerSide side)
        {if(side!=PlayerSide.US && side!=PlayerSide.USSR)throw new ArgumentOutOfRangeException(nameof(side));}
    }
}
