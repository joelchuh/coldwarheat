using System;
using System.Collections.Generic;
using System.Linq;
using ColdWarStruggle.Core;
using ColdWarStruggle.Application;
using UnityEngine;
namespace ColdWarStruggle.Presentation
{
    public sealed class DemoScenarioOption
    {
        public string Id { get; }
        public string Title { get; }
        public PlayerSide Actor { get; }
        public CountryId SelectedCountry { get; }
        public DemoScenarioOption(string id,string title,PlayerSide actor,CountryId selected)
        { Id=id;Title=title;Actor=actor;SelectedCountry=selected; }
    }
    // A replaceable diagnostic view. It renders projections and submits intent only.
    public sealed class DemoView : MonoBehaviour
    {
        DemoController controller;
        IReadOnlyDictionary<string,string> names;
        IReadOnlyList<DemoScenarioOption> scenarios;
        int scenarioIndex,regionIndex;
        bool revealBoth=true;
        Vector2 scroll;
        Font font;
        GUIStyle title,label,small,button,large;
        string fatal;
        readonly string[] regions={"region.europe","region.asia","region.middle_east","region.africa","region.central_america","region.south_america"};
        readonly string[] regionNames={"欧洲","亚洲","中东","非洲","中美洲","南美洲"};
        static readonly Color Background=new Color(.035f,.057f,.095f),Panel=new Color(.075f,.105f,.15f),Muted=new Color(.58f,.66f,.76f),Ink=new Color(.91f,.94f,.98f),Blue=new Color(.32f,.64f,1f),Red=new Color(1f,.43f,.4f),Green=new Color(.36f,.83f,.65f);
        public void Initialize(DemoController controller,IReadOnlyDictionary<string,string> names,IReadOnlyList<DemoScenarioOption> scenarios)
        { this.controller=controller;this.names=names;this.scenarios=scenarios;Reset(0); }
        public void ShowError(string message) { fatal=message; }
        void Reset(int index)
        { var s=scenarios[index];controller.LoadScenario(s.Id,s.Actor,s.SelectedCountry);scenarioIndex=index;regionIndex=0;scroll=Vector2.zero; }
        void Styles()
        {
            if(label!=null)return;
            font=Font.CreateDynamicFontFromOSFont(new[]{"Microsoft YaHei","SimHei","Arial"},18);
            label=new GUIStyle(GUI.skin.label){font=font,fontSize=17,wordWrap=true};label.normal.textColor=Ink;
            small=new GUIStyle(label){fontSize=14};small.normal.textColor=Muted;
            title=new GUIStyle(label){fontSize=30,fontStyle=FontStyle.Bold};
            large=new GUIStyle(title){fontSize=40};
            button=new GUIStyle(GUI.skin.button){font=font,fontSize=16,wordWrap=true,padding=new RectOffset(10,10,5,5)};
        }
        static void Block(Rect r,Color color) { var old=GUI.color;GUI.color=color;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=old; }
        void Text(float x,float y,float w,float h,string value,GUIStyle style=null) => GUI.Label(new Rect(x,y,w,h),value,style??label);
        bool Button(float x,float y,float w,float h,string text) => GUI.Button(new Rect(x,y,w,h),text,button);
        static string Side(PlayerSide side)=>side==PlayerSide.US?"美国 US":"苏联 USSR";
        string Name(CountryId id)=>names.TryGetValue(id.Value,out var name)?name:id.Value;
        void OnGUI()
        {
            Styles();float scale=Math.Min(Screen.width/1280f,Screen.height/820f);
            GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-1280*scale)/2,(Screen.height-820*scale)/2,0),Quaternion.identity,new Vector3(scale,scale,1));
            Block(new Rect(0,0,1280,820),Background);
            Text(26,20,900,46,"COLD WAR / 冷战热斗 · 规则操作 Demo",title);
            Text(28,70,1100,28,"单人双边测试  /  人工练习局面  /  占位界面，可替换正式地图",small);
            if(fatal!=null) { Text(30,140,1180,400,"加载失败："+fatal);return; }
            if(controller==null) { Text(30,140,800,60,"正在读取演示数据…");return; }
            if(Button(26,115,225,40,"练习 1：美国 → 法国")) { Reset(0);GUIUtility.ExitGUI(); }
            if(Button(264,115,225,40,"练习 2：苏联 → 东德")) { Reset(1);GUIUtility.ExitGUI(); }
            if(Button(502,115,155,40,"重置当前场景")) { Reset(scenarioIndex);GUIUtility.ExitGUI(); }
            if(Button(840,115,190,40,"操作方：美国")) { controller.SelectSide(PlayerSide.US);GUIUtility.ExitGUI(); }
            if(Button(1040,115,214,40,"操作方：苏联")) { controller.SelectSide(PlayerSide.USSR);GUIUtility.ExitGUI(); }
            var view=controller.ReadView();var legal=controller.LegalTargets();
            Block(new Rect(26,177,250,600),Panel);Block(new Rect(294,177,640,600),Panel);Block(new Rect(952,177,302,600),Panel);
            Text(44,195,220,26,"当前测试局面",small);Text(44,226,214,60,scenarios[scenarioIndex].Title);
            Text(44,307,220,26,"规则要求的行动方",small);Text(44,338,220,34,Side(view.Actor));
            Text(44,389,220,26,"你正在操作",small);Text(44,420,220,34,Side(controller.SelectedSide));
            Text(44,485,220,26,"剩余 OPS",small);Text(44,513,210,60,view.RemainingOps.ToString(),large);
            Text(44,594,208,150,"试一试：\n1. 在默认目标投放 3 点\n2. 重发命令，观察不重复扣点\n3. 切换另一方，观察资格检查\n4. 重置后选择其他国家",small);
            Text(312,194,590,28,"国家面板  ·  绿色表示当前可投放目标",small);
            for(int i=0;i<regions.Length;i++)
                if(Button(312+(i%3)*201,232+(i/3)*39,193,32,(regionIndex==i?"● ":"")+regionNames[i])) { regionIndex=i;scroll=Vector2.zero; }
            var countries=view.Countries.Where(c=>c.Definition.Region==regions[regionIndex]).ToArray();
            scroll=GUI.BeginScrollView(new Rect(307,321,614,440),scroll,new Rect(0,0,591,((countries.Length+1)/2)*75));
            for(int i=0;i<countries.Length;i++)
            {
                var c=countries[i];var id=c.Definition.Id;var box=new Rect((i%2)*294,(i/2)*75,286,67);
                bool selected=id.Equals(controller.SelectedCountry);bool allowed=legal.Targets.Contains(id);
                Block(box,selected?new Color(.18f,.27f,.39f):new Color(.10f,.14f,.20f));
                Block(new Rect(box.x,box.y,4,box.height),allowed?Green:Muted*.45f);
                if(GUI.Button(box,"",GUIStyle.none))controller.SelectCountry(id);
                Text(box.x+12,box.y+6,265,27,(selected?"› ":"")+Name(id));
                Text(box.x+12,box.y+34,264,28,$"美 {c.US}   苏 {c.USSR}    稳定 {c.Definition.Stability}"+(c.Definition.Battleground?"  战场":""),small);
            }
            GUI.EndScrollView();
            var selectedCountry=view.Countries.First(c=>c.Definition.Id.Equals(controller.SelectedCountry));
            Text(972,195,262,26,"已选国家",small);Text(972,228,262,65,Name(controller.SelectedCountry),new GUIStyle(title){fontSize=24});
            Text(972,292,262,40,$"美国 {selectedCountry.US}   /   苏联 {selectedCountry.USSR}");
            Text(972,329,262,28,"控制方："+(selectedCountry.Controller.HasValue?Side(selectedCountry.Controller.Value):"无"),small);
            if(Button(972,374,262,43,"投放 1 点影响力"))Place(1);
            if(Button(972,427,262,43,"一次提交 3 点"))Place(3);
            GUI.enabled=controller.CanRetry;if(Button(972,480,262,37,"重发上次命令（不会重复结算）"))Retry();GUI.enabled=true;
            var result=controller.LastResult;
            Text(972,538,262,96,result==null?"选择国家后提交操作。\n非法操作会显示原因，局面不变。":result.Accepted?"操作已接受\n逐点费用："+string.Join(" + ",result.Costs):"操作被拒绝\n"+ErrorText(result.Error),small);
            revealBoth=GUI.Toggle(new Rect(972,642,264,28),revealBoth," 查看双方示例手牌",button);
            if(revealBoth)
            {
                var both=controller.ReadBoth();Text(972,681,266,60,"美："+string.Join("、",both.USHand)+"\n苏："+string.Join("、",both.USSRHand),small);
            }
            else Text(972,681,266,62,"本方："+string.Join("、",view.OwnHand)+"\n对手："+view.OpponentHandCount+" 张（隐藏）",small);
            Text(28,786,1220,27,$"本 Demo 仅实现影响力操作；示例牌不能出牌，未启用完整回合、计分或 AI。   局面版本 {view.Version} · 已结算点数 {view.EventSequence}",small);
        }
        async void Place(int points) { try { await controller.SubmitAsync(points); } catch(Exception e) { fatal=e.Message;Debug.LogException(e); } }
        async void Retry() { try { await controller.RetryAsync(); } catch(Exception e) { fatal=e.Message;Debug.LogException(e); } }
        static string ErrorText(RuleError error)
        {
            switch(error)
            {
                case RuleError.WrongActor:return "当前操作方没有本次行动资格。";
                case RuleError.IllegalTarget:return "该国不在行动开始时的可达范围。";
                case RuleError.InsufficientBudget:return "剩余 OPS 不足，整条命令未执行。";
                case RuleError.StaleVersion:return "局面已变化，请按最新状态重试。";
                case RuleError.CommandIdConflict:return "同一命令编号不能改变请求内容。";
                default:return error.ToString();
            }
        }
        void OnDestroy() { if(font!=null)Destroy(font); }
    }
}
