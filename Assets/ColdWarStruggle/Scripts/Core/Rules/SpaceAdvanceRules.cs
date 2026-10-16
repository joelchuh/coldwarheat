using System;
namespace ColdWarStruggle.Core
{
    public sealed class SpaceAdvanceCalculation
    {
        public SpaceTrackState Before { get; }
        public SpaceTrackState After { get; }
        public PlayerSide Beneficiary { get; }
        public int FromPosition => Before.PositionFor(Beneficiary);
        public int ToPosition => After.PositionFor(Beneficiary);
        public bool FirstArrival { get; }
        public int AwardVp { get; }
        public int NetVpForUS => Beneficiary==PlayerSide.US ? AwardVp : -AwardVp;
        internal SpaceAdvanceCalculation(SpaceTrackState before,SpaceTrackState after,PlayerSide beneficiary,bool first,int award)
        { Before=before;After=after;Beneficiary=beneficiary;FirstArrival=first;AwardVp=award; }
    }
    public static class SpaceAdvanceRules
    {
        // Shared arithmetic only: the caller owns authorization, receipts and atomic host publication.
        public static SpaceAdvanceCalculation CalculateSingleStep(SpaceTrackState track,PlayerSide beneficiary)
        {
            if(track==null)throw new ArgumentNullException(nameof(track));
            var from=track.PositionFor(beneficiary);
            if(from==track.Definition.FinalPosition)throw new InvalidOperationException("No space remains on the track.");
            var target=track.Definition.GetSpace(from+1);
            var opponent=beneficiary==PlayerSide.US?PlayerSide.USSR:PlayerSide.US;
            var first=track.PositionFor(opponent)<target.Position;
            var after=new SpaceTrackState(track.Definition,beneficiary==PlayerSide.US?target.Position:track.US,beneficiary==PlayerSide.USSR?target.Position:track.USSR);
            return new SpaceAdvanceCalculation(track,after,beneficiary,first,first?target.FirstVp:target.SecondVp);
        }
    }
}
