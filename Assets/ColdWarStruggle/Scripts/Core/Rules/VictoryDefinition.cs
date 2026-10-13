using System;
using System.Collections.Generic;
using System.Linq;
namespace ColdWarStruggle.Core
{
    public enum VictoryOutcome { Continue, Deferred, Finished, UnresolvedAdjudication }
    public enum VictoryCheckpoint { VpBatchClosed, MilitaryAssessed, HeldScoringAudited, NuclearWarReached, ScoringResolved }
    internal static class VictoryGuard
    {
        internal static string Text(string value)
        {
            if(string.IsNullOrWhiteSpace(value) || value.Length>256)throw new ArgumentException("Victory identifier must be nonempty and at most 256 characters.");
            return value;
        }
        internal static void Side(PlayerSide? value)
        {
            if(value.HasValue && value!=PlayerSide.US && value!=PlayerSide.USSR)throw new ArgumentException("Unknown victory side.");
        }
    }
    public sealed class HeldScoringRow
    {
        public string Id { get; }
        public bool UsHolds { get; }
        public bool UssrHolds { get; }
        public PlayerSide? MilitaryCandidate { get; }
        public VictoryOutcome Outcome { get; }
        public PlayerSide? Winner { get; }
        public string Authority { get; }
        public string AdoptionStatus { get; }
        public HeldScoringRow(string id,bool us,bool ussr,PlayerSide? candidate,VictoryOutcome outcome,PlayerSide? winner,string authority,string adoption)
        {
            Id=VictoryGuard.Text(id);Authority=VictoryGuard.Text(authority);VictoryGuard.Side(candidate);VictoryGuard.Side(winner);
            bool adopted=adoption=="adopted" || adoption=="adopted-project-interpretation";
            bool pending=adoption=="pending";
            if((!adopted && !pending) || (pending && (outcome!=VictoryOutcome.UnresolvedAdjudication || winner.HasValue))
                || (adopted && outcome!=VictoryOutcome.Continue && outcome!=VictoryOutcome.Finished)
                || (outcome==VictoryOutcome.Finished && !winner.HasValue)
                || (outcome==VictoryOutcome.Continue && (winner.HasValue || candidate.HasValue || us || ussr)))
                throw new ArgumentException("Invalid held-scoring outcome or adoption.");
            UsHolds=us;UssrHolds=ussr;MilitaryCandidate=candidate;Outcome=outcome;Winner=winner;AdoptionStatus=adoption;
        }
    }
    public sealed class VictoryDefinition
    {
        public int InitialSignedVp { get; }
        public int AutomaticVpThreshold { get; }
        public IReadOnlyList<HeldScoringRow> HeldScoringRows { get; }
        public VictoryDefinition(int initial,int threshold,IEnumerable<HeldScoringRow> rows)
        {
            if(threshold<1 || initial<=-(long)threshold || initial>=threshold || rows==null)
                throw new ArgumentException("Invalid initial victory definition.");
            // Four hold combinations times three candidate possibilities. Bound enumeration before copying.
            var copy=rows.Take(13).ToArray();
            if(copy.Length!=12 || copy.Any(x=>x==null) || copy.Select(x=>x.Id).Distinct(StringComparer.Ordinal).Count()!=12
                || copy.Select(x=>(x.UsHolds,x.UssrHolds,x.MilitaryCandidate)).Distinct().Count()!=12)
                throw new ArgumentException("Held-scoring matrix must cover exactly twelve unique conditions.");
            InitialSignedVp=initial;AutomaticVpThreshold=threshold;HeldScoringRows=Array.AsReadOnly(copy);
        }
    }
}
