using System;
using ColdWarStruggle.Application;
using UnityEngine;
namespace ColdWarStruggle.Presentation
{
    public sealed partial class DemoView
    {
        void DrawEmptyHandChoice(PlayerViewSnapshot view)
        {
            if(!view.Practice.CanSkipEmptyHand)return;
            if(GUI.Button(new Rect(972,153,262,22),"手牌用尽 · 跳过本次行动",
                new GUIStyle(button){fontSize=13,padding=new RectOffset(6,6,1,1)}))SkipEmptyHand();
        }
        async void SkipEmptyHand()
        { try { await controller.SkipEmptyHandAsync(); } catch(Exception e) { fatal=e.Message;Debug.LogException(e); } }
    }
}
