using ColdWarStruggle.Core;
namespace ColdWarStruggle.Application
{
    public sealed class PublicTurnEnd
    {
        public int Turn { get; }
        public int RequiredMilitary { get; }
        public int UsEarned { get; }
        public int UssrEarned { get; }
        public int UsShortfall { get; }
        public int UssrShortfall { get; }
        public int VpDelta { get; }
        public int BeforeVp { get; }
        public int AfterVp { get; }
        public bool UsHoldsScoring { get; }
        public bool UssrHoldsScoring { get; }
        public PlayerSide? MilitaryCandidate { get; }
        public PlayerSide? Winner { get; }
        public string RuleRowId { get; }
        public string Authority { get; }
        public bool ChinaFlipped { get; }
        public int ExpiredOpsEffects { get; }
        internal PublicTurnEnd(PracticeTurnEndReceipt r)
        {
            var a=r.Assessment;Turn=a.TurnNumber;RequiredMilitary=a.Defcon.Level;UsEarned=a.UsEarned;UssrEarned=a.UssrEarned;UsShortfall=a.UsShortfall;UssrShortfall=a.UssrShortfall;VpDelta=a.NetVpForUS;
            BeforeVp=r.BeforeVp;AfterVp=r.Decision.SignedVp;UsHoldsScoring=r.UsHolds;UssrHoldsScoring=r.UssrHolds;MilitaryCandidate=r.MilitaryCandidate;Winner=r.Decision.Winner;RuleRowId=r.Decision.HeldScoringRowId;Authority=r.Decision.Authority;ChinaFlipped=r.ChinaFlipped;ExpiredOpsEffects=r.ExpiredOpsEffects;
        }
    }
}
