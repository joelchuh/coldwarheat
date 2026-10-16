using System;
using System.Linq;
using System.Threading.Tasks;
using ColdWarStruggle.Core;
using ColdWarStruggle.Application;
namespace ColdWarStruggle.Presentation
{
    public sealed partial class DemoController
    {
        public bool CanDiscardForBlockade
        {
            get{var choice=ReadView().Blockade;var card=SelectedCard;return choice?.CanChoose==true && card.HasValue && choice.Candidates.Any(c=>c.Card.Equals(card.Value));}
        }
        public bool CanDeclineBlockade=>ReadView().Blockade?.CanChoose==true;
        public Task<CardActionResult> DiscardForBlockadeAsync(CardId card)
        {
            var choice=ReadView().Blockade;
            if(choice?.CanChoose!=true || !choice.Candidates.Any(c=>c.Card.Equals(card)))throw new InvalidOperationException("No eligible card in this identity's current blockade decision.");
            return SendCard(CardActionKind.DiscardForBlockade,card);
        }
        public Task<CardActionResult> DeclineBlockadeAsync()
        {
            if(!CanDeclineBlockade)throw new InvalidOperationException("This identity cannot answer the current blockade.");
            return SendCard(CardActionKind.DeclineBlockade);
        }
    }
}
