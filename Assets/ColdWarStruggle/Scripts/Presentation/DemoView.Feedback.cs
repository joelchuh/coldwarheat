using UnityEngine;
namespace ColdWarStruggle.Presentation
{
    public sealed partial class DemoView
    {
        Vector2 feedbackScroll,feedbackReaderScroll;
        string feedbackText,feedbackReaderGame;
        bool feedbackReaderOpen;
        int feedbackReaderGeneration;
        long feedbackReaderVersion;
        public bool IsFeedbackReaderOpen
        {
            get
            {
                if(!feedbackReaderOpen || controller==null || controller.ViewGeneration!=feedbackReaderGeneration)return false;
                var view=controller.ReadView();return view.GameId==feedbackReaderGame && view.Version==feedbackReaderVersion;
            }
        }
        public bool TryOpenFeedback()
        {
            if(controller==null || string.IsNullOrWhiteSpace(ReadVisibleFeedback()))return false;
            var view=controller.ReadView();feedbackReaderGame=view.GameId;feedbackReaderVersion=view.Version;
            feedbackReaderGeneration=controller.ViewGeneration;feedbackReaderScroll=Vector2.zero;feedbackReaderOpen=true;return true;
        }
        public void CloseFeedback(){feedbackReaderOpen=false;feedbackReaderGame=null;feedbackReaderScroll=Vector2.zero;}
        public string ReadExpandedFeedback()=>IsFeedbackReaderOpen?ReadVisibleFeedback():null;
        string ReadVisibleFeedback()
        {
            if(controller.ReadView().Event==null)return ReadPracticeFeedback();
            var message=ReadEventFeedback();var coup=controller.EventPanelCoupMessage;
            return coup==null?message:controller.LastEventResult?.Accepted==false?coup+"\n\n"+message:coup;
        }
        void DrawFeedback(float y,float height,string message)
        {
            message=message??"";
            if(feedbackText!=message){feedbackText=message;feedbackScroll=Vector2.zero;}
            if(message.Length==0)return;
            if(Button(972,y,262,27,"展开结果")){TryOpenFeedback();GUIUtility.ExitGUI();}
            var content=new GUIContent(message);var visibleHeight=Mathf.Max(1,height-31);
            var contentHeight=Mathf.Max(visibleHeight,small.CalcHeight(content,240));
            feedbackScroll=GUI.BeginScrollView(new Rect(972,y+31,262,visibleHeight),feedbackScroll,new Rect(0,0,240,contentHeight));
            GUI.Label(new Rect(0,0,240,contentHeight),content,small);GUI.EndScrollView();
        }
        void DrawFeedbackReader()
        {
            if(Event.current.type==EventType.KeyDown && Event.current.keyCode==KeyCode.Escape)
            {CloseFeedback();Event.current.Use();GUIUtility.ExitGUI();}
            Block(new Rect(26,115,1228,662),Panel);
            Text(48,134,930,44,"行动与事件 · 结果明细",title);
            Text(48,188,940,36,"查看当前反馈，关闭后继续对局。",label);
            if(Button(1050,137,182,40,"关闭 · 返回对局")){CloseFeedback();GUIUtility.ExitGUI();}
            var style=new GUIStyle(label){fontSize=22};var content=new GUIContent(ReadExpandedFeedback()??"");
            var contentHeight=Mathf.Max(500,style.CalcHeight(content,1124));
            feedbackReaderScroll=GUI.BeginScrollView(new Rect(48,238,1184,514),feedbackReaderScroll,new Rect(0,0,1156,contentHeight));
            GUI.Label(new Rect(12,0,1124,contentHeight),content,style);GUI.EndScrollView();
        }
    }
}
