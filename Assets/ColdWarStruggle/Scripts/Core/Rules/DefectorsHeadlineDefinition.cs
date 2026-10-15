using System;
namespace ColdWarStruggle.Core
{
    public sealed class DefectorsHeadlineDefinition
    {
        public CardCatalog Catalog { get; }
        public CardId Card { get; }
        public DefectorsHeadlineDefinition(CardCatalog catalog,CardId card)
        {
            Catalog=catalog??throw new ArgumentNullException(nameof(catalog));
            if(card.Value!="card.defectors" || !catalog.Contains(card))throw new ArgumentException("Unknown selected Defectors binding.");
            var printed=catalog.GetCard(card);
            if(printed.Kind!=CardKind.Event || printed.Affiliation!=CardAffiliation.US || printed.RemoveAfterResolvedEvent)throw new ArgumentException("Incompatible Defectors card definition.");
            Card=card;
        }
    }
    public enum HeadlineOrderStrategy { OrdinaryPrintedValue, USSelectedDefectorsFirst }
    public enum HeadlineRootOutcome { Resolved, Cancelled }
    public sealed class HeadlineRootCompletion
    {
        public CardId Card { get; }
        public PlayerSide PlayedBy { get; }
        public HeadlineRootOutcome Outcome { get; }
        public CardId? CancellationSource { get; }
        internal HeadlineRootCompletion(CardId card,PlayerSide playedBy,HeadlineRootOutcome outcome,CardId? source=null)
        {Card=card;PlayedBy=playedBy;Outcome=outcome;CancellationSource=source;}
    }
}
