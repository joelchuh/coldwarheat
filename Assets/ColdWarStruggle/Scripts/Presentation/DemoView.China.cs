using System.Linq;
using ColdWarStruggle.Core;
using ColdWarStruggle.Application;
using UnityEngine;
namespace ColdWarStruggle.Presentation
{
    public sealed partial class DemoView
    {
        string RegionalBudgetHint(PlayerViewSnapshot view)
        {
            var b=view.RegionalOps;if(b==null)return "";
            return b.EligibleRegions.Count==0?"已出奖励区；本次按普通 OPS 预算。":
                "奖励条件：全程用于 "+string.Join("、",b.EligibleRegions.Select(ScoringRegionName))+"。出区会重算预算。";
        }
        string RemainingOpsText(PlayerViewSnapshot view)=>view.RegionalOps?.EligibleRegions.Count>0?$"最多剩余 {view.RemainingOps} OPS":$"剩余 {view.RemainingOps} OPS";
        void DrawChinaSelector(PlayerViewSnapshot view,float x,float y,float width,float height)
        {
            var china=view.Cards.China;var option=controller.CardOptions().China;
            var selectable=(view.Headline==null || view.Headline.Status==PublicHeadlineStatus.Completed) && option!=null && china.Owner==view.Viewer && china.Face==CardFace.FaceUp;
            GUI.enabled=selectable;
            if(Button(x,y,width,height,"中国牌 · "+Side(china.Owner)+(china.Face==CardFace.FaceUp?" 正面":" 背面")+(selectable?" · 选择":"")))
            {controller.SelectCard(china.Id);handPanelOpen=false;GUIUtility.ExitGUI();}
            GUI.enabled=true;
        }
    }
}
