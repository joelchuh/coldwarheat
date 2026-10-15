using System;
using System.Collections.Generic;
using System.Linq;
using ColdWarStruggle.Core;
namespace ColdWarStruggle.Application
{
    // The future Host must supply its authenticated identity; this projector does not authenticate callers.
    public sealed class HeadlineSelectionView
    {
        public PlayerSide Viewer { get; }
        public CardId? OwnCard { get; }
        public CardId? OpponentCard { get; }
        public bool OwnLocked { get; }
        public bool OpponentLocked { get; }
        public bool Ready { get; }
        public bool CanChoose { get; }
        public IReadOnlyList<CardId> Candidates { get; }
        HeadlineSelectionView(HeadlineSelectionState state,PlayerSide viewer)
        {
            Viewer=viewer;var other=viewer==PlayerSide.US?PlayerSide.USSR:PlayerSide.US;
            OwnCard=state.Cards.GetHeadline(viewer);OwnLocked=OwnCard.HasValue;
            var opposing=state.Cards.GetHeadline(other);OpponentLocked=opposing.HasValue;
            Ready=state.Ready;CanChoose=HeadlineSelectionRules.CanChoose(state,viewer);
            OpponentCard=Ready || state.ChoosesSecond==viewer?opposing:null;
            Candidates=Array.AsReadOnly(CanChoose?state.Cards.GetHand(viewer).Where(id=>state.SupportedCards.Contains(id)).ToArray():Array.Empty<CardId>());
        }
        public static HeadlineSelectionView Create(HeadlineSelectionState state,PlayerSide viewer)
        {
            if(state==null)throw new ArgumentNullException(nameof(state));
            if(viewer!=PlayerSide.US && viewer!=PlayerSide.USSR)throw new ArgumentOutOfRangeException(nameof(viewer));
            return new HeadlineSelectionView(state,viewer);
        }
    }
}
