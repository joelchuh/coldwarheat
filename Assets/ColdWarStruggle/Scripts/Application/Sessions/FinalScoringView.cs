using System;
using System.Linq;
using System.Collections.Generic;
using ColdWarStruggle.Core;
namespace ColdWarStruggle.Application
{
    public sealed class PublicFinalScoring
    {
        public int BeforeVp { get; }
        public int AfterVp { get; }
        public IReadOnlyList<PublicRegionScore> Regions { get; }
        public PlayerSide ChinaOwner { get; }
        public int ChinaVpForUS { get; }
        public int? NetVpForUS { get; }
        public PlayerSide? Winner { get; }
        public bool EuropeControl { get; }
        public bool IsDraw=>!Winner.HasValue;
        internal PublicFinalScoring(PracticeFinalScoringReceipt receipt)
        {
            BeforeVp=receipt.BeforeVp;AfterVp=receipt.Decision.SignedVp;Winner=receipt.Decision.Winner;
            var r=receipt.Result;Regions=Array.AsReadOnly(r.Regions.Select(x=>new PublicRegionScore(x)).ToArray());
            ChinaOwner=r.ChinaOwner;ChinaVpForUS=r.ChinaVpForUS;NetVpForUS=r.NetVpForUS;EuropeControl=r.EuropeControlWinner.HasValue;
        }
    }
}
