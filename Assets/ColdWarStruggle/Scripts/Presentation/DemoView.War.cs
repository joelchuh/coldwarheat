using System;
using System.Collections.Generic;
using System.Linq;
using ColdWarStruggle.Core;
using ColdWarStruggle.Application;
using UnityEngine;
namespace ColdWarStruggle.Presentation
{
    public sealed partial class DemoView
    {
        public string ReadHeadlineWarFeedback()
        {
            var step=controller.CurrentWarHeadline;
            return step==null?null:"头条 · "+WarResultMessage(step.War);
        }
        string WarResultMessage(PublicWarResult war)
        {
            var sources=new List<string>();if(war.ControlledNeighbors.Count>0)sources.Add("邻国 "+string.Join("、",war.ControlledNeighbors.Select(Name)));
            if(war.TargetControlPenalty)sources.Add("目标自身");if(war.OpponentSuperpowerAdjacent)sources.Add("超级大国接壤");
            var source=sources.Count==0?"无对方控制修正":string.Join("；",sources);
            return "战争 · "+CardTitle(war.Card)+" → "+Name(war.Target)+"\n"+
                $"骰 {war.Roll} − 控制修正 {war.Penalty} = {war.AdjustedRoll}；需 ≥{war.WinningResult}："+(war.Success?"胜利":"失败")+"\n"+source+"\n"+
                $"影响力 美 {war.Before.US}→{war.After.US} / 苏 {war.Before.USSR}→{war.After.USSR}\n"+
                Side(war.Beneficiary)+$"军事 +{war.MilitaryCredit}（{war.MilitaryBefore}→{war.MilitaryAfter}） · VP {war.VpBefore}→{war.VpAfter}"+(war.Terminal?"\n已达胜利条件 · 后续步骤停止":"");
        }
        void DrawWarControls(PlayerViewSnapshot view)
        {
            var choice=view.War;
            Text(972,365,262,82,CardTitle(choice.Card)+"\n"+Side(choice.Owner)+"选择战争目标："+string.Join("、",choice.Candidates.Select(Name))+"。",small);
            if(!choice.CanChoose)
            {
                if(Button(972,455,262,42,"切换至 "+Side(choice.Owner))){controller.SelectSide(choice.Owner);GUIUtility.ExitGUI();}
            }
            else
            {
                GUI.enabled=controller.CanConfirmWar;
                if(Button(972,455,262,42,"确认战争："+Name(controller.SelectedCountry)))ConfirmWar();
                GUI.enabled=true;
            }
            Text(972,508,262,64,"地图绿色为候选目标。\n确认后掷骰并结算；选择期间不产生收益，也不推进原行动。",small);
        }
        async void ConfirmWar()
        {try{await controller.ConfirmWarAsync();}catch(Exception e){fatal=e.Message;Debug.LogException(e);}}
    }
}
