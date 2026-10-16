using System;
using System.Threading.Tasks;
using ColdWarStruggle.Application;
using ColdWarStruggle.Core;
namespace ColdWarStruggle.Presentation
{
    public sealed partial class DemoController
    {
        public Task<CardActionResult> RespondOlympicAsync(bool participate)=>SendCard(participate?CardActionKind.ParticipateOlympic:CardActionKind.BoycottOlympic);
        public Task<CardActionResult> ChooseOlympicOperationsAsync(OperationsMode mode)
        {
            if(!Enum.IsDefined(typeof(OperationsMode),mode))throw new ArgumentOutOfRangeException(nameof(mode));
            var kind=mode==OperationsMode.Coup?CardActionKind.UseOlympicCoup:mode==OperationsMode.Realignment?CardActionKind.UseOlympicRealignment:CardActionKind.UseOlympicInfluence;
            return SendCard(kind,target:mode==OperationsMode.Coup?SelectedCountry:(CountryId?)null);
        }
        public Task<CardActionResult> DeclineOlympicOperationsAsync()=>SendCard(CardActionKind.DeclineOlympicOperations);
        public CardActionResult AdvanceInternalOlympic()
        {
            if(ReadView().Olympic?.WaitingForInternal!=true)return null;
            var result=host.AdvanceInternalOlympic(SelectedSide);LastCardResult=result;LastResult=null;legal=null;
            if(result.Accepted){selectedCard=null;SelectingCoup=false;ResetEventInteraction(false);}
            MarkInteractionChanged();return result; // Keep lastCardCommand: retry still belongs to the original player response.
        }
    }
}
