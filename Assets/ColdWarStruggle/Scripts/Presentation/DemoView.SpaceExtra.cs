using System;
using ColdWarStruggle.Application;
using ColdWarStruggle.Core;
using UnityEngine;
namespace ColdWarStruggle.Presentation
{
    public sealed partial class DemoView
    {
        void DrawExtraActionChoice(PlayerViewSnapshot view)
        {
            var p=view.Practice;
            if(!p.IsExtraAction || p.Stage!=ActionPracticeStage.ChooseCard)return;
            GUI.enabled=controller.CanDeclineExtraActions;
            if(GUI.Button(new Rect(972,153,262,22),"放弃本回合剩余额外行动",
                new GUIStyle(button){fontSize=13,padding=new RectOffset(6,6,1,1)}))DeclineExtraActions();
            GUI.enabled=true;
        }
        async void DeclineExtraActions()
        { try { await controller.DeclineExtraActionsAsync(); } catch(Exception e) { fatal=e.Message;Debug.LogException(e); } }
    }
}
