using System;
using System.Collections.Generic;
using System.Linq;
using ColdWarStruggle.Core;
using ColdWarStruggle.Application;
namespace ColdWarStruggle.Presentation
{
    // Only filtered public state, or an explicitly supplied matching test observation.
    public sealed class HeadlinePanelFrame
    {
        public bool CanChoose { get; }
        public bool OwnLocked { get; }
        public bool OpponentLocked { get; }
        public bool IsObserver { get; }
        public string Prompt { get; }
        public IReadOnlyList<CardId> Candidates { get; }
        public IReadOnlyList<string> ResolutionLines { get; }
        public IReadOnlyList<string> ObserverLines { get; }
        static string Side(PlayerSide side)=>side==PlayerSide.US?"美国":"苏联";
        HeadlinePanelFrame(PlayerViewSnapshot view,IReadOnlyDictionary<string,string> names,LocalTestSnapshot observer)
        {
            if(view==null || names==null)throw new ArgumentNullException();
            if(observer!=null && (observer.PublicView.GameId!=view.GameId || observer.PublicView.Version!=view.Version))
                throw new ArgumentException("Observer snapshot must match the game and version.");
            string Name(CardId id)
            {
                if(!names.TryGetValue(id.Value,out var value) || string.IsNullOrWhiteSpace(value))throw new ArgumentException("Missing headline title.");
                return value;
            }
            var h=view.Headline;var selection=h?.Selection;
            CanChoose=selection?.CanChoose==true;OwnLocked=selection?.OwnLocked==true;OpponentLocked=selection?.OpponentLocked==true;IsObserver=observer!=null;
            Candidates=Array.AsReadOnly((selection?.Candidates??Array.Empty<CardId>()).ToArray());
            var rows=new List<string>();
            if(h==null)Prompt=view.Practice?.HeadlineEnabled==true?"完成部署后进入头条选择。":"此练习跳过头条。";
            else if(selection!=null)
            {
                Prompt=OwnLocked?"已锁定头条："+Name(selection.OwnCard.Value)+"。等待对方选择。":
                    selection.WaitingForOpponentFirst?"等待对方先选头条。":Candidates.Count==0?"当前手牌没有可用头条；相关效果尚未接入。":"从本方手牌选择头条，确认后锁定。";
                if(selection.OpponentCard.HasValue)Prompt+=" 对方头条："+Name(selection.OpponentCard.Value)+"。";
                else if(OpponentLocked)Prompt+=" 对方已锁定，牌名未公开。";
            }
            else
            {
                Prompt=h.Status==PublicHeadlineStatus.Terminal?"头条阶段已终局，后续结算停止。":
                    h.WaitingOwner.HasValue?"等待"+Side(h.WaitingOwner.Value)+"完成事件选择。":
                    h.Status==PublicHeadlineStatus.Completed?"头条结算完成，按当前行动方继续。":"正在结算已公开的头条。";
                foreach(var entry in h.Order)
                {
                    var done=h.CompletedRoots.SingleOrDefault(x=>x.Card.Equals(entry.Card) && x.PlayedBy==entry.PlayedBy);
                    var status=done!=null?(done.Outcome==HeadlineRootOutcome.Resolved?"已发生":"已取消（"+Name(done.CancellationSource.Value)+"）"):
                        h.Status==PublicHeadlineStatus.Terminal?"终局中断":h.CurrentPlayedBy==entry.PlayedBy && h.WaitingOwner.HasValue?"等待"+Side(h.WaitingOwner.Value)+"选择":"待结算";
                    rows.Add(Side(entry.PlayedBy)+"："+Name(entry.Card)+" · "+status);
                }
            }
            ResolutionLines=Array.AsReadOnly(rows.ToArray());
            ObserverLines=Array.AsReadOnly(observer==null?Array.Empty<string>():observer.ReservedHeadlines.OrderBy(x=>x.Key)
                .Select(x=>"测试观察 · "+Side(x.Key)+"已锁定："+Name(x.Value)).ToArray());
        }
        public static HeadlinePanelFrame Create(PlayerViewSnapshot view,IReadOnlyDictionary<string,string> names,LocalTestSnapshot observer=null)=>new HeadlinePanelFrame(view,names,observer);
    }
}
