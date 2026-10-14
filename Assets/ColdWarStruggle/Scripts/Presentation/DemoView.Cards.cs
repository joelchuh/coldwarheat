using System;
using System.Linq;
using ColdWarStruggle.Core;
using ColdWarStruggle.Application;
using UnityEngine;
namespace ColdWarStruggle.Presentation
{
    public sealed partial class DemoView
    {
        CardCatalog printedCards;
        bool handPanelOpen;
        Vector2 handScroll;
        void DrawHandSummary(PlayerViewSnapshot view)
        {
            bool action=view.Practice!=null && view.Deployment==null;
            revealBoth=GUI.Toggle(new Rect(972,action?676:642,264,action?26:28),revealBoth," 测试观察：查看双方",button);
            if(Button(972,action?706:680,262,action?27:41,$"手牌 本方{view.OwnHand.Count} / 对方{view.OpponentHandCount}"))
            { handPanelOpen=true;handScroll=Vector2.zero;GUIUtility.ExitGUI(); }
            Text(972,action?737:730,262,40,$"牌库 {view.Cards.DrawPileCount} · 弃牌 {view.Cards.DiscardCount} · 移出 {view.Cards.RemovedCount}\n中国牌：{Side(view.Cards.China.Owner)} / {(view.Cards.China.Face==CardFace.FaceUp?"正面":"背面")}",small);
        }
        void DrawHandPanel(PlayerViewSnapshot view)
        {
            Block(new Rect(26,177,1228,600),Panel);
            Text(48,192,750,38,view.Practice!=null?"手牌 · 选择本方牌使用":"开局手牌 · 只读预览",new GUIStyle(title){fontSize=27});
            revealBoth=GUI.Toggle(new Rect(826,197,275,30),revealBoth," 观察双方（测试）",button);
            if(Button(1114,192,120,36,"返回局面")){handPanelOpen=false;GUIUtility.ExitGUI();}
            var frame=HandPanelFrame.Create(view,printedCards,names,revealBoth?controller.ReadBoth():null);
            Text(48,234,1170,24,frame.IsObserver?"测试观察已开启：显示双方手牌；只能选择当前操作方自己的牌，切换身份后可操作另一方。":"仅显示当前操作方的手牌。上方切换操作方后会重新读取该方视图。",small);
            float height=frame.Rows.Sum(r=>32+Math.Max(1,(r.Cards.Count+7)/8)*184+18);
            handScroll=GUI.BeginScrollView(new Rect(44,259,1194,476),handScroll,new Rect(0,0,1168,Math.Max(476,height)));
            float rowY=0;
            var cardTitle=new GUIStyle(label){fontSize=17,fontStyle=FontStyle.Bold};
            var opsStyle=new GUIStyle(title){fontSize=19};
            var footStyle=new GUIStyle(small){fontSize=12};
            var options=view.Practice==null?null:controller.CardOptions().Cards.ToDictionary(c=>c.Card);
            foreach(var row in frame.Rows)
            {
                Text(0,rowY,1140,27,$"{Side(row.Side)} · {row.Cards.Count} 张普通手牌"+(row.Side==view.Viewer?" · 当前操作方":" · 测试观察"),label);
                if(row.Cards.Count==0)Text(12,rowY+42,1100,32,"此方没有普通手牌。",small);
                for(int i=0;i<row.Cards.Count;i++)
                {
                    var card=row.Cards[i];float x=(i%8)*146,y=rowY+32+(i/8)*184;
                    var accent=card.Affiliation switch {CardAffiliation.US=>Blue,CardAffiliation.USSR=>Red,_=>Green};
                    Block(new Rect(x,y,138,172),Background);Block(new Rect(x,y,138,4),accent);
                    cardTitle.fontSize=17;
                    var titleContent=new GUIContent(card.Title);
                    while(cardTitle.fontSize>11 && cardTitle.CalcHeight(titleContent,118)>62)
                        cardTitle.fontSize--;
                    Text(x+10,y+14,118,62,card.Title,cardTitle);
                    Text(x+10,y+79,118,27,card.Kind==CardKind.Scoring?"计分":$"印刷 OPS {card.PrintedOps}",opsStyle);
                    var current=row.Side==view.Viewer && options!=null && options.TryGetValue(card.Id,out var quote)?quote.CurrentOps:null;
                    if(current.HasValue)Text(x+10,y+104,118,18,$"当前 OPS {current.Value}",footStyle);
                    var affiliation=card.Affiliation switch {CardAffiliation.US=>"美国事件",CardAffiliation.USSR=>"苏联事件",CardAffiliation.Neutral=>"中立事件",_=>"地区计分"};
                    Text(x+10,y+(current.HasValue?122:115),118,current.HasValue?18:25,affiliation,current.HasValue?footStyle:small);
                    if(view.Practice!=null && row.Side==view.Viewer)
                    {
                        if(Button(x+8,y+140,122,27,"选择这张牌"))
                        { controller.SelectCard(card.Id);handPanelOpen=false;GUI.EndScrollView();GUIUtility.ExitGUI(); }
                    }
                    else Text(x+10,y+145,118,22,card.RemoveAfterEvent?"★ 事件后移出":card.Kind==CardKind.Scoring?"计分牌":"事件牌",footStyle);
                }
                rowY+=32+Math.Max(1,(row.Cards.Count+7)/8)*184+18;
            }
            GUI.EndScrollView();
            Text(48,744,1172,26,$"选牌只改变本地意图，点击用途后才提交；中国牌单独持有，本批暂不可使用。  版本 {frame.Version}",small);
            Text(28,786,1220,27,"行动试玩支持影响力、政变、太空、计分与十二张已接事件；对手事件加OPS、其他事件和完整回合待接入。",small);
        }
    }
}
