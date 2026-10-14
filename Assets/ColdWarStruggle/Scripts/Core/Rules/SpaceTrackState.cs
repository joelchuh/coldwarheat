using System;
namespace ColdWarStruggle.Core
{
    // A read-only snapshot; constructing it is not an authorized move or VP award.
    public sealed class SpaceTrackState
    {
        public SpaceRaceDefinition Definition { get; }
        public int US { get; }
        public int USSR { get; }
        public SpaceTrackState(SpaceRaceDefinition definition, int us, int ussr)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            if (us < definition.InitialPosition || us > definition.FinalPosition || ussr < definition.InitialPosition || ussr > definition.FinalPosition)
                throw new ArgumentOutOfRangeException(nameof(us), "Both markers must lie within the track.");
            US = us; USSR = ussr;
        }
        public int PositionFor(PlayerSide side) => side switch
        {
            PlayerSide.US => US,
            PlayerSide.USSR => USSR,
            _ => throw new ArgumentOutOfRangeException(nameof(side))
        };
    }
}
