using System;
using System.Collections.Generic;
using System.Linq;
namespace ColdWarStruggle.Core
{
    public sealed class CardCatalog
    {
        public IReadOnlyList<CardDefinition> Cards { get; }
        readonly Dictionary<CardId,CardDefinition> byId;
        readonly Dictionary<int,CardDefinition> byNumber;
        public CardCatalog(IEnumerable<CardDefinition> cards)
        {
            var copy=cards?.Take(257).ToArray();
            if(copy==null || copy.Length==0 || copy.Length>256 || copy.Any(c=>c==null) || copy.Select(c=>c.Id).Distinct().Count()!=copy.Length
                || copy.Select(c=>c.PrintedNumber).Distinct().Count()!=copy.Length)throw new ArgumentException("Invalid card catalog.");
            Cards=Array.AsReadOnly(copy.OrderBy(c=>c.Id.Value,StringComparer.Ordinal).ToArray());
            byId=copy.ToDictionary(c=>c.Id);byNumber=copy.ToDictionary(c=>c.PrintedNumber);
        }
        public bool Contains(CardId id)=>byId.ContainsKey(id);
        public CardDefinition GetCard(CardId id)=>byId.TryGetValue(id,out var card)?card:throw new ArgumentException("Unknown card ID.");
        public CardDefinition GetPrintedCard(int number)=>byNumber.TryGetValue(number,out var card)?card:throw new ArgumentException("Unknown printed card number.");
        public IReadOnlyList<CardDefinition> GetEraCards(GameEra era)
        {
            if(!Enum.IsDefined(typeof(GameEra),era))throw new ArgumentOutOfRangeException(nameof(era));
            return Array.AsReadOnly(Cards.Where(c=>c.DeckEntry==CardDeckEntry.EraDeck && c.Era==era).ToArray());
        }
    }
}
