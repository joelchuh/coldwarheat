using System;
namespace ColdWarStruggle.Core
{
    // Counts completed root actions only. Card eligibility and in-flight root ownership belong to the coordinator.
    public sealed class ActionRoundProgress
    {
        readonly int usCompleted,ussrCompleted;
        readonly bool usDeclined,ussrDeclined;
        public GameTurnContext Turn { get; }
        public SpaceRaceDefinition SpaceDefinition { get; }
        public PlayerSide? CurrentActor { get; }
        public int Round => CurrentActor.HasValue?checked(CompletedFor(CurrentActor.Value)+1):Math.Max(usCompleted,ussrCompleted);
        public bool IsOptional => CurrentActor.HasValue && CompletedFor(CurrentActor.Value)>=Turn.Parameters.NormalActionsPerSide;
        public bool IsComplete => !CurrentActor.HasValue;
        public int TotalCompleted => checked(usCompleted+ussrCompleted);
        ActionRoundProgress(GameTurnContext turn,SpaceRaceDefinition definition,int us,int su,bool declinedUs,bool declinedSu,PlayerSide? actor)
        { Turn=turn;SpaceDefinition=definition;usCompleted=us;ussrCompleted=su;usDeclined=declinedUs;ussrDeclined=declinedSu;CurrentActor=actor; }
        public static ActionRoundProgress Create(GameTurnContext turn,SpaceRaceDefinition definition)
            =>new ActionRoundProgress(turn??throw new ArgumentNullException(nameof(turn)),definition??throw new ArgumentNullException(nameof(definition)),0,0,false,false,PlayerSide.USSR);
        static void ValidateSide(PlayerSide side)
        { if(side!=PlayerSide.US && side!=PlayerSide.USSR)throw new ArgumentOutOfRangeException(nameof(side)); }
        void ValidateTrack(SpaceTrackState track)
        {
            if(track==null)throw new ArgumentNullException(nameof(track));
            if(!ReferenceEquals(track.Definition,SpaceDefinition))throw new ArgumentException("Action progress must use the shared space definition.",nameof(track));
        }
        public int CompletedFor(PlayerSide side) { ValidateSide(side);return side==PlayerSide.US?usCompleted:ussrCompleted; }
        public bool DeclinedFor(PlayerSide side) { ValidateSide(side);return side==PlayerSide.US?usDeclined:ussrDeclined; }
        int Allowance(SpaceTrackState track,PlayerSide side)=>Math.Max(Turn.Parameters.NormalActionsPerSide,
            DeclinedFor(side)?0:SpaceAbilityRules.Query(track,side).ActionRoundAllowance??0);
        public bool CanBegin(SpaceTrackState track,PlayerSide side)
        { ValidateTrack(track);ValidateSide(side);return CurrentActor==side && CompletedFor(side)<Allowance(track,side); }
        public int RemainingAllowance(SpaceTrackState track,PlayerSide side)
        {
            ValidateTrack(track);ValidateSide(side);
            return IsComplete?0:Math.Max(0,Allowance(track,side)-CompletedFor(side));
        }
        // A coordinator calls this only after a legally started root finishes; a loss of ability inside that root never erases it.
        public ActionRoundProgress CompleteRoot(SpaceTrackState track,PlayerSide side)
        {
            ValidateTrack(track);ValidateSide(side);
            if(CurrentActor!=side)throw new InvalidOperationException("Only the current root can complete.");
            return Schedule(track,side==PlayerSide.US?checked(usCompleted+1):usCompleted,
                side==PlayerSide.USSR?checked(ussrCompleted+1):ussrCompleted,usDeclined,ussrDeclined);
        }
        public ActionRoundProgress DeclineRemaining(SpaceTrackState track,PlayerSide side)
        {
            ValidateTrack(track);ValidateSide(side);
            if(CurrentActor!=side || !IsOptional)throw new InvalidOperationException("Only an unstarted optional slot can be declined.");
            return Schedule(track,usCompleted,ussrCompleted,usDeclined||side==PlayerSide.US,ussrDeclined||side==PlayerSide.USSR);
        }
        ActionRoundProgress Schedule(SpaceTrackState track,int us,int su,bool declinedUs,bool declinedSu)
        {
            var next=new ActionRoundProgress(Turn,SpaceDefinition,us,su,declinedUs,declinedSu,null);
            PlayerSide? actor=null;var normal=Turn.Parameters.NormalActionsPerSide;
            if(us<normal || su<normal)actor=su<=us?PlayerSide.USSR:PlayerSide.US;
            else
            {
                var usAvailable=us<next.Allowance(track,PlayerSide.US);var suAvailable=su<next.Allowance(track,PlayerSide.USSR);
                if(suAvailable && (!usAvailable || su<=us))actor=PlayerSide.USSR;
                else if(usAvailable)actor=PlayerSide.US;
            }
            return new ActionRoundProgress(Turn,SpaceDefinition,us,su,declinedUs,declinedSu,actor);
        }
    }
}
