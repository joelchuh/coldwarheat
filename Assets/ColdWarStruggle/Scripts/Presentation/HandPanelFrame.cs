using System;
using System.Collections.Generic;
using System.Linq;
using ColdWarStruggle.Core;
using ColdWarStruggle.Application;
namespace ColdWarStruggle.Presentation
{
    public sealed class HandCardDisplay
    {
        public CardId Id { get; }
        public string Title { get; }
        public CardKind Kind { get; }
        public int? PrintedOps { get; }
        public CardAffiliation Affiliation { get; }
        public bool RemoveAfterEvent { get; }
        internal HandCardDisplay(CardDefinition card,string title)
        { Id=card.Id;Title=title;Kind=card.Kind;PrintedOps=card.PrintedOps;Affiliation=card.Affiliation;RemoveAfterEvent=card.RemoveAfterResolvedEvent; }
    }
    public sealed class HandPanelRow
    {
        public PlayerSide Side { get; }
        public IReadOnlyList<HandCardDisplay> Cards { get; }
        internal HandPanelRow(PlayerSide side,IEnumerable<string> ids,CardCatalog catalog,IReadOnlyDictionary<string,string> names)
        {
            Side=side;
            Cards=Array.AsReadOnly(ids.Select(id=>
            {
                var card=catalog.GetCard(new CardId(id));
                if(!names.TryGetValue(id,out var title) || string.IsNullOrWhiteSpace(title))throw new ArgumentException("Missing card title.");
                return new HandCardDisplay(card,title);
            }).ToArray());
        }
    }
    public sealed class HandPanelFrame
    {
        public string GameId { get; }
        public long Version { get; }
        public bool IsObserver { get; }
        public IReadOnlyList<HandPanelRow> Rows { get; }
        HandPanelFrame(PlayerViewSnapshot view,IEnumerable<HandPanelRow> rows,bool observer)
        { GameId=view.GameId;Version=view.Version;IsObserver=observer;Rows=Array.AsReadOnly(rows.ToArray()); }
        public static HandPanelFrame Create(PlayerViewSnapshot view,CardCatalog catalog,IReadOnlyDictionary<string,string> names,LocalTestSnapshot observer=null)
        {
            if(view==null || view.Cards==null || catalog==null || names==null)throw new ArgumentException("Real card view and static definitions are required.");
            if(observer!=null && (observer.PublicView.GameId!=view.GameId || observer.PublicView.Version!=view.Version))
                throw new ArgumentException("Observer snapshot is from another game or version.");
            var rows=new List<HandPanelRow>{new HandPanelRow(view.Viewer,view.OwnHand,catalog,names)};
            if(observer!=null)
            {
                var other=view.Viewer==PlayerSide.US?PlayerSide.USSR:PlayerSide.US;
                rows.Add(new HandPanelRow(other,other==PlayerSide.US?observer.USHand:observer.USSRHand,catalog,names));
            }
            return new HandPanelFrame(view,rows,observer!=null);
        }
    }
}
