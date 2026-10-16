using System;
using UnityEngine;
namespace ColdWarStruggle.Presentation
{
    public sealed partial class DemoView
    {
        Vector2 scenarioScroll;
        public bool IsScenarioPickerOpen { get; private set; }
        public void OpenScenarioPicker()
        {
            if(controller==null || scenarios==null)throw new InvalidOperationException("The demo is not initialized.");
            scenarioScroll=new Vector2(0,scenarioIndex*58);IsScenarioPickerOpen=true;
        }
        public void CancelScenarioPicker() { IsScenarioPickerOpen=false; }
        public void SelectScenario(int index)
        {
            if(!IsScenarioPickerOpen)throw new InvalidOperationException("Open the practice list before selecting.");
            if(index<0 || index>=scenarios.Count)throw new ArgumentOutOfRangeException(nameof(index));
            Reset(index);IsScenarioPickerOpen=false;
        }
        void DrawScenarioPicker()
        {
            Block(new Rect(26,115,1228,662),Panel);
            Text(48,134,930,44,"选择练习 · "+scenarios.Count+" 个场景",title);
            Text(48,188,940,44,"选择后会重新开始该练习；取消可返回当前对局，保留已选手牌和草稿。",small);
            if(Button(1050,137,182,40,"取消 · 返回对局")){CancelScenarioPicker();GUIUtility.ExitGUI();}
            scenarioScroll=GUI.BeginScrollView(new Rect(48,238,1184,514),scenarioScroll,new Rect(0,0,1156,scenarios.Count*58));
            for(int i=0;i<scenarios.Count;i++)
                if(Button(0,i*58,1144,50,$"{i+1:00}  ·  {scenarios[i].Title}"+(i==scenarioIndex?"  [当前场景]":"")))
                {
                    SelectScenario(i);GUI.EndScrollView();GUIUtility.ExitGUI();
                }
            GUI.EndScrollView();
        }
    }
}
