using System;
using System.Linq;
using ColdWarStruggle.Core;
using ColdWarStruggle.Application;
using UnityEngine;
namespace ColdWarStruggle.Presentation
{
    public sealed partial class DemoView
    {
        public HeadlinePanelFrame ReadHeadlineFrame()=>HeadlinePanelFrame.Create(controller.ReadView(),names,TestObserverEnabled?controller.ReadBoth():null);
        void DrawHeadlineMapSummary(PlayerViewSnapshot view,float left,float width)
        {
            var f=HeadlinePanelFrame.Create(view,names);
            if(f.ResolutionLines.Count==0)Text(left+18,188,width-36,40,f.Prompt,small);
            else for(int i=0;i<f.ResolutionLines.Count;i++)Text(left+18,184+i*21,width-36,21,f.ResolutionLines[i],small);
        }
        void DrawHeadlineControls(PlayerViewSnapshot view)
        {
            var f=ReadHeadlineFrame();
            Text(972,366,262,112,f.Prompt+(f.ObserverLines.Count>0?"\n"+string.Join("\n",f.ObserverLines):""),small);
            var selected=controller.SelectedCard;var allowed=f.CanChoose && selected.HasValue && f.Candidates.Contains(selected.Value);
            Text(972,479,262,30,allowed?"已选："+CardTitle(selected.Value):f.CanChoose?"可选头条："+f.Candidates.Count+" 张":"本方暂不能继续选牌。",small);
            GUI.enabled=allowed;
            if(Button(972,512,262,35,"确认选为头条 · 锁定"))SubmitHeadline();
            GUI.enabled=f.CanChoose;
            if(Button(972,553,262,31,"打开手牌 · 选择头条"))OpenCards();
            GUI.enabled=true;
            var other=view.Viewer==PlayerSide.US?PlayerSide.USSR:PlayerSide.US;
            if(Button(972,590,262,31,"切换至 "+Side(other))){controller.SelectSide(other);GUIUtility.ExitGUI();}
            var result=controller.LastHeadlineResult;
            if(result!=null)Text(972,622,262,22,result.Accepted?"头条命令已接受 · 以当前局面为准":ErrorText(result.Error),small);
            GUI.enabled=controller.CanRetryHeadline;
            if(Button(972,646,262,27,"重发头条命令（幂等）"))RetryHeadline();
            GUI.enabled=true;
        }
        async void SubmitHeadline()
        {try{await controller.SubmitSelectedHeadlineAsync();}catch(Exception e){fatal=e.Message;Debug.LogException(e);}}
        async void RetryHeadline()
        {try{await controller.RetryHeadlineAsync();}catch(Exception e){fatal=e.Message;Debug.LogException(e);}}
    }
}
