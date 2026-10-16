using System;
using ColdWarStruggle.Application;
using ColdWarStruggle.Core;
using UnityEngine;
namespace ColdWarStruggle.Presentation
{
    public sealed partial class DemoView
    {
        bool DrawExtraActionChoice(PlayerViewSnapshot view)
        {
            var p=view.Practice;
            if(!p.IsExtraAction || p.Stage!=ActionPracticeStage.ChooseCard)return false;
            Text(972,578,262,23,$"已行动 美 {p.UsCompletedActions} / 苏 {p.UssrCompletedActions} · 额外可选",small);
            GUI.enabled=controller.CanDeclineExtraActions;
            if(Button(972,608,262,31,"放弃本回合剩余额外行动"))DeclineExtraActions();
            GUI.enabled=true;return true;
        }
        async void DeclineExtraActions()
        { try { await controller.DeclineExtraActionsAsync(); } catch(Exception e) { fatal=e.Message;Debug.LogException(e); } }
    }
}
