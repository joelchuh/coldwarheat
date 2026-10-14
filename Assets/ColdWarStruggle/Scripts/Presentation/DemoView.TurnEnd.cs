using System;
using ColdWarStruggle.Application;
using UnityEngine;
namespace ColdWarStruggle.Presentation
{
    public sealed partial class DemoView
    {
        void DrawTurnEndReport(PlayerViewSnapshot view)
        {
            Text(972,366,262,245,controller.TurnEndMessage,small);
            if(controller.LastCardResult?.Accepted==false)Text(972,610,262,30,ErrorText(controller.LastCardResult.Error),small);
            GUI.enabled=controller.CanRetryCard;
            if(Button(972,646,262,27,"重发结算命令（幂等）"))RetryCard();
            GUI.enabled=true;
        }
        async void AssessTurnEnd()
        { try { await controller.AssessTurnEndAsync(); } catch(Exception e) { fatal=e.Message;Debug.LogException(e); } }
    }
}
