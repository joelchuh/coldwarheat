using System;
using System.Linq;
using System.Threading.Tasks;
using ColdWarStruggle.Core;
using ColdWarStruggle.Application;
namespace ColdWarStruggle.Presentation
{
    public sealed partial class DemoController
    {
        public bool CanDiscardSelectedHeldCard
        {
            get { var v=ReadView();var card=SelectedCard;return card.HasValue && v.SpaceDiscard?.CanChoose==true && v.SpaceDiscard.Candidates.Contains(card.Value); }
        }
        public bool CanSelectChinaCard
        {
            get
            {
                var v=ReadView();var china=v.Cards?.China;
                return china!=null && v.Practice?.EndReason==null && v.SpaceDiscard==null
                    && (v.Headline==null || v.Headline.Status==PublicHeadlineStatus.Completed)
                    && china.Owner==v.Viewer && china.Face==CardFace.FaceUp && CardOptions().China!=null;
            }
        }
        public Task<CardActionResult> DiscardSelectedHeldCardAsync()
        {
            if(!CanDiscardSelectedHeldCard)throw new InvalidOperationException("Select a current discard candidate for this identity.");
            return SendCard(CardActionKind.DiscardHeldCard,SelectedCard);
        }
        public Task<CardActionResult> KeepHeldCardsAsync()=>SendCard(CardActionKind.KeepHeldCards);
    }
}
