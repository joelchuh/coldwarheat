using System;
using ColdWarStruggle.Core;
namespace ColdWarStruggle.Application
{
    public sealed class PublicOlympicPair
    {
        public long Round {get;}
        public int UsRoll {get;}
        public int UssrRoll {get;}
        public int UsTotal {get;}
        public int UssrTotal {get;}
        public PlayerSide? Winner {get;}
        public int AwardVp {get;}
        internal PublicOlympicPair(OlympicPairReceipt pair)
        {var c=pair.Plan.Calculation;Round=pair.RoundIndex;UsRoll=c.UsRoll;UssrRoll=c.UssrRoll;UsTotal=c.UsTotal;UssrTotal=c.UssrTotal;Winner=c.Winner;AwardVp=c.AwardVp;}
    }
    public sealed class PublicOlympicView
    {
        public CardId Card {get;}
        public PlayerSide Sponsor {get;}
        public PlayerSide? PendingOwner {get;}
        public OlympicResolutionStage Stage {get;}
        public OlympicBranch? Branch {get;}
        public OperationsMode? Mode {get;}
        public bool CanRespond {get;}
        public bool WaitingForInternal {get;}
        public PublicOlympicPair LastPair {get;}
        internal PublicOlympicView(GameState state,PlayerSide viewer)
        {
            var e=state.Practice.Olympic;var f=e.Flow;Card=f.Current.Definition.Card.Id;Sponsor=f.Sponsor;PendingOwner=f.PendingOwner;Stage=f.Stage;Branch=f.Branch;Mode=e.Mode;
            CanRespond=ActionPracticeRules.CanRespondToOlympic(state,viewer)==RuleError.None;WaitingForInternal=f.Stage==OlympicResolutionStage.Rolling;
            LastPair=f.LastPair==null?null:new PublicOlympicPair(f.LastPair);
        }
    }
    public sealed class PublicOlympicResult
    {
        public CardId Card {get;}
        public PlayerSide Sponsor {get;}
        public OlympicBranch? Branch {get;}
        public OlympicPracticeOutcome Outcome {get;}
        public PublicOlympicPair LastPair {get;}
        public int? DefconBefore {get;}
        public int DefconAfter {get;}
        public int VpForUS {get;}
        public bool Terminal {get;}
        public PlayerSide? Winner {get;}
        internal PublicOlympicResult(OlympicPracticeResult result,PlayerViewSnapshot view)
        {
            var f=result.Flow;Card=f.Current.Definition.Card.Id;Sponsor=f.Sponsor;Branch=f.Branch;Outcome=result.Outcome;LastPair=f.LastPair==null?null:new PublicOlympicPair(f.LastPair);
            DefconBefore=f.Boycott?.Before.Defcon.Level;DefconAfter=view.Practice.Defcon.Value;VpForUS=view.Practice.VpForUS;Terminal=view.Practice.EndReason.HasValue;Winner=view.Practice.Winner;
        }
    }
}
