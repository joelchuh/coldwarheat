using System;
using ColdWarStruggle.Application;
using UnityEngine;
namespace ColdWarStruggle.Presentation
{
    public sealed partial class DemoView
    {
        bool finalReportOpen;
        void DrawFinalScoringControls(PlayerViewSnapshot view)
        {
            var frame=FinalScoringPanelFrame.Create(view.Practice.FinalScoring);
            Text(972,366,262,64,frame.Outcome,new GUIStyle(label){fontSize=22,fontStyle=FontStyle.Bold});
            Text(972,435,262,105,frame.Equation+"\n"+frame.ChinaLine,small);
            if(Button(972,557,262,34,finalReportOpen?"返回终局地图":"查看六地区结算明细")) {finalReportOpen=!finalReportOpen;GUIUtility.ExitGUI();}
            Text(972,600,262,40,"对局已结束。可切换地图与身份复盘，或重置场景。",small);
            GUI.enabled=controller.CanRetryCard;
            if(Button(972,646,262,27,"重发最终命令（幂等）"))RetryCard();
            GUI.enabled=true;
        }
        void DrawFinalScoringScreen(PlayerViewSnapshot view)
        {
            var frame=FinalScoringPanelFrame.Create(view.Practice.FinalScoring);
            Block(new Rect(26,177,908,600),Panel);Block(new Rect(952,177,302,600),Panel);
            Text(44,194,694,42,frame.Outcome,new GUIStyle(title){fontSize=28});
            if(Button(770,194,145,36,"返回终局地图")){finalReportOpen=false;GUIUtility.ExitGUI();}
            Text(44,247,865,40,frame.Equation,label);
            Text(44,299,130,26,"计分地区",small);Text(190,299,275,26,"美国",new GUIStyle(label){normal={textColor=Blue}});
            Text(480,299,275,26,"苏联",new GUIStyle(label){normal={textColor=Red}});Text(780,299,130,26,"净 VP（美+）",small);
            for(int i=0;i<frame.Rows.Count;i++)
            {
                var row=frame.Rows[i];float y=334+i*54;
                if(i%2==0)Block(new Rect(38,y-3,882,53),Background);
                Text(48,y+7,126,34,row.Label,label);Text(190,y,275,50,row.US,small);Text(480,y,275,50,row.USSR,small);Text(780,y+7,128,38,row.Net,label);
            }
            Text(44,671,865,34,frame.ChinaLine,label);
            Text(44,718,865,40,"东南亚已计入亚洲，不重复加分。全部地区汇总后判胜；欧洲控制优先。",small);
            Text(972,195,262,34,"本局结算记录",new GUIStyle(title){fontSize=23});
            Text(972,244,262,102,controller.TurnEndMessage,small);
            DrawFinalScoringControls(view);DrawHandSummary(view);
            Text(28,786,1220,27,$"第10回合最终计分 · 只读结果；切换显示不会重复结算。   版本 {view.Version} · 序号 {view.EventSequence}",small);
        }
        async void CompleteFinalScoring()
        {
            try
            {
                var result=await controller.CompleteFinalScoringAsync();
                if(result.Accepted && controller.ReadView().GameId==result.View.GameId)finalReportOpen=true;
            }
            catch(Exception e){fatal=e.Message;Debug.LogException(e);}
        }
    }
}
