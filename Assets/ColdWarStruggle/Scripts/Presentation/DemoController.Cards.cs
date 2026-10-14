using System;
using System.Linq;
using System.Threading.Tasks;
using ColdWarStruggle.Core;
using ColdWarStruggle.Application;
namespace ColdWarStruggle.Presentation
{
    public sealed partial class DemoController
    {
        CardId? selectedCard;
        string selectedCardGame;
        long selectedCardVersion;
        PlayerSide selectedCardSide,lastCardSide;
        CardActionCommand lastCardCommand;
        public CardId? SelectedCard
        {
            get
            {
                var v=ReadView();
                if(v.GameId!=selectedCardGame || v.Version!=selectedCardVersion || selectedCardSide!=SelectedSide)selectedCard=null;
                return selectedCard;
            }
        }
        public CardActionResult LastCardResult { get; private set; }
        public bool CanRetryCard=>lastCardCommand!=null && lastCardSide==SelectedSide;
        public CardActionOptions CardOptions()=>Session.QueryCardActions();
        public void SelectCard(CardId card)
        {
            var v=ReadView();if(!v.OwnHand.Contains(card.Value))throw new ArgumentException("Only this identity's real hand can be selected.");
            selectedCard=card;selectedCardGame=v.GameId;selectedCardVersion=v.Version;selectedCardSide=SelectedSide;MarkInteractionChanged();
        }
        public Task<CardActionResult> PlayCardAsync(CardActionKind kind)
        {
            if(kind!=CardActionKind.Influence && kind!=CardActionKind.Space)throw new ArgumentException("A card use is required.");
            var card=SelectedCard??throw new InvalidOperationException("Select a card first.");return SendCard(kind,card);
        }
        public Task<CardActionResult> StartPracticeAsync()=>SendCard(CardActionKind.StartPractice);
        public Task<CardActionResult> FinishInfluenceAsync()=>SendCard(CardActionKind.FinishInfluence);
        async Task<CardActionResult> SendCard(CardActionKind kind,CardId? card=null)
        {
            var v=ReadView();var epoch=generation;var session=Session;
            var command=new CardActionCommand(Guid.NewGuid().ToString("N"),v.GameId,v.Version,kind,card);
            lastCardCommand=command;lastCardSide=SelectedSide;
            var result=await session.SubmitCardActionAsync(command);
            if(epoch==generation) { LastCardResult=result;LastResult=null;legal=null;if(result.Accepted)selectedCard=null;MarkInteractionChanged(); }
            return result;
        }
        public async Task<CardActionResult> RetryCardAsync()
        {
            if(!CanRetryCard)throw new InvalidOperationException("No card command for this identity.");
            var epoch=generation;var result=await Session.SubmitCardActionAsync(lastCardCommand);
            if(epoch==generation)LastCardResult=result;return result;
        }
        void ResetCardInteraction(bool clearReceipt)
        { selectedCard=null;selectedCardGame=null;LastCardResult=null;if(clearReceipt)lastCardCommand=null;MarkInteractionChanged(); }
    }
}
