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
                if(v.GameId!=selectedCardGame || v.Version!=selectedCardVersion || selectedCardSide!=SelectedSide){selectedCard=null;SelectingCoup=false;legal=null;}
                return selectedCard;
            }
        }
        public CardActionResult LastCardResult { get; private set; }
        public bool CanRetryCard=>lastCardCommand!=null && lastCardSide==SelectedSide;
        public CardActionOptions CardOptions()=>Session.QueryCardActions();
        public void SelectCard(CardId card)
        {
            var v=ReadView();if(!v.OwnHand.Contains(card.Value))throw new ArgumentException("Only this identity's real hand can be selected.");
            SelectingCoup=false;legal=null;selectedCard=card;selectedCardGame=v.GameId;selectedCardVersion=v.Version;selectedCardSide=SelectedSide;MarkInteractionChanged();
        }
        public bool SelectingCoup { get; private set; }
        public void SelectCoupUse()
        {
            var card=SelectedCard??throw new InvalidOperationException("Select a card first.");
            var option=CardOptions().Cards.Single(c=>c.Card.Equals(card));
            if(option.Coup!=RuleError.None)throw new InvalidOperationException("This card cannot be used for a coup.");
            SelectingCoup=true;legal=null;MarkInteractionChanged();
        }
        public Task<CardActionResult> PlayCardAsync(CardActionKind kind)
        {
            if(kind!=CardActionKind.Influence && kind!=CardActionKind.Space && kind!=CardActionKind.Coup)throw new ArgumentException("A card use is required.");
            var card=SelectedCard??throw new InvalidOperationException("Select a card first.");return SendCard(kind,card,kind==CardActionKind.Coup?SelectedCountry:(CountryId?)null);
        }
        public Task<CardActionResult> StartPracticeAsync()=>SendCard(CardActionKind.StartPractice);
        public Task<CardActionResult> FinishInfluenceAsync()=>SendCard(CardActionKind.FinishInfluence);
        async Task<CardActionResult> SendCard(CardActionKind kind,CardId? card=null,CountryId? target=null)
        {
            var v=ReadView();var epoch=generation;var session=Session;
            var command=new CardActionCommand(Guid.NewGuid().ToString("N"),v.GameId,v.Version,kind,card,target);
            lastCardCommand=command;lastCardSide=SelectedSide;
            var result=await session.SubmitCardActionAsync(command);
            if(epoch==generation) { LastCardResult=result;LastResult=null;legal=null;if(result.Accepted){selectedCard=null;SelectingCoup=false;}MarkInteractionChanged(); }
            return result;
        }
        public async Task<CardActionResult> RetryCardAsync()
        {
            if(!CanRetryCard)throw new InvalidOperationException("No card command for this identity.");
            var epoch=generation;var result=await Session.SubmitCardActionAsync(lastCardCommand);
            if(epoch==generation)LastCardResult=result;return result;
        }
        void ResetCardInteraction(bool clearReceipt)
        { SelectingCoup=false;legal=null;selectedCard=null;selectedCardGame=null;LastCardResult=null;if(clearReceipt)lastCardCommand=null;MarkInteractionChanged(); }
    }
}
