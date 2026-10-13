using System;
using System.Linq;
using ColdWarStruggle.Application;
using ColdWarStruggle.Core;
using UnityEngine;
namespace ColdWarStruggle.Presentation
{
    public sealed partial class DemoView
    {
        void DrawDeploymentControls(PlayerViewSnapshot view)
        {
            var phase=view.Deployment;var q=controller.DeploymentTargets();var draft=controller.DeploymentDraft;
            var amount=draft.FirstOrDefault(a=>a.Country.Equals(controller.SelectedCountry)).Amount;
            var region=phase.Region=="region.eastern_europe"?"东欧":phase.Region=="region.western_europe"?"西欧":phase.Region;
            var message=phase.Complete?"部署练习完成。\n正式发牌与头条尚未接入。"
                :q.Error==RuleError.WrongActor?"请切换至 "+Side(phase.Owner.Value)+"。\n目标范围："+region
                :q.Error!=RuleError.None?ErrorText(q.Error):$"{region}部署草稿 {draft.Sum(a=>a.Amount)} / {q.RequiredAmount} 点\n当前国家：{amount} 点";
            Text(972,366,262,42,message,small);
            GUI.enabled=controller.CanChangeDeploymentAllocation(controller.SelectedCountry,-1);
            if(Button(972,410,126,29,"− 1 点"))controller.ChangeDeploymentAllocation(controller.SelectedCountry,-1);
            GUI.enabled=controller.CanChangeDeploymentAllocation(controller.SelectedCountry,1);
            if(Button(1108,410,126,29,"+ 1 点"))controller.ChangeDeploymentAllocation(controller.SelectedCountry,1);
            GUI.enabled=controller.CanConfirmDeployment;
            if(Button(972,445,262,39,phase.Complete?"已完成部署":"确认本方完整部署"))ConfirmDeployment();
            GUI.enabled=draft.Count>0;
            if(Button(972,490,262,33,"清空部署草稿"))controller.ClearDeploymentDraft();
            GUI.enabled=controller.CanRetryDeployment;
            if(Button(972,531,262,33,"重发上次部署命令"))RetryDeployment();
            GUI.enabled=true;
            var result=controller.LastDeploymentResult;
            Text(972,574,262,62,result==null?"确认前可修改分配；切换地图保留草稿。\n切换操作方会清除未确认草稿。"
                :!result.Accepted?"未执行："+ErrorText(result.Error)
                :result.CompletionSequence.HasValue?"完成收据 · 部署已提交。"
                :"部署已提交。请按左侧提示切换操作方。",small);
        }
        async void ConfirmDeployment() { try { await controller.ConfirmDeploymentAsync(); } catch(Exception e) { fatal=e.Message;Debug.LogException(e); } }
        async void RetryDeployment() { try { await controller.RetryDeploymentAsync(); } catch(Exception e) { fatal=e.Message;Debug.LogException(e); } }
    }
}
