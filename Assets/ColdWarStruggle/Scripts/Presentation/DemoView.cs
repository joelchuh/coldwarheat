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
    public sealed partial class DemoView : MonoBehaviour
    {
        DemoController controller;
        IReadOnlyDictionary<string,string> names;
        IReadOnlyList<DemoScenarioOption> scenarios;
        int scenarioIndex,regionIndex;
        bool revealBoth=true;
        MapPresenter mapPresenter;
        DrawableMapView mapView;
        WorldMapAssets worldAssets;
        Font font;
        GUIStyle title,label,small,button,large;
        string fatal;
        public bool IsReady=>controller!=null && fatal==null;
        string captureStatus="F12：保存游戏画面";
        readonly string[] regions={"region.europe","region.asia","region.middle_east","region.africa","region.central_america","region.south_america"};
        readonly string[] regionNames={"欧洲","亚洲","中东","非洲","中美洲","南美洲"};
        static readonly Color Background=new Color(.035f,.057f,.095f),Panel=new Color(.075f,.105f,.15f),Muted=new Color(.58f,.66f,.76f),Ink=new Color(.91f,.94f,.98f),Blue=new Color(.32f,.64f,1f),Red=new Color(1f,.43f,.4f),Green=new Color(.36f,.83f,.65f);
        public void Initialize(DemoController controller,IReadOnlyDictionary<string,string> names,IReadOnlyList<DemoScenarioOption> scenarios,int initialScenario=0,DiagnosticMapKind mapKind=DiagnosticMapKind.Tiles,WorldMapAssets worldAssets=null,bool focusSelected=false,CardCatalog printedCards=null,bool showHands=false)
        {
            if(focusSelected && mapKind!=DiagnosticMapKind.World)throw new ArgumentException("Selected focus requires the world view.");
            if(initialScenario<0 || initialScenario>=scenarios.Count)throw new ArgumentOutOfRangeException(nameof(initialScenario));
            this.controller=controller;this.names=names;this.scenarios=scenarios;this.worldAssets=worldAssets;this.printedCards=printedCards;Reset(initialScenario);
            if(showHands && (printedCards==null || controller.ReadView().Cards==null))throw new ArgumentException("Hand panel requires a real dealt scenario.");
            handPanelOpen=showHands;
            mapPresenter?.Dispose();mapPresenter=new MapPresenter(controller,names);SwitchMap(mapKind);
            if(focusSelected && mapView is WorldMapView world)world.RequestCountryFocus(controller.SelectedCountry);
        }
        public void ShowError(string message) { fatal=message; }
        public void SetCaptureStatus(string message) { captureStatus=message; }
        void Reset(int index)
        {
            var s=scenarios[index];controller.LoadScenario(s.Id,s.Actor,s.SelectedCountry);scenarioIndex=index;
            handPanelOpen=false;handScroll=Vector2.zero;
            var region=controller.ReadView().Countries.First(c=>c.Definition.Id.Equals(s.SelectedCountry)).Definition.Region;
            regionIndex=Math.Max(0,System.Array.IndexOf(regions,region));
        }
        void SwitchMap(DiagnosticMapKind kind)
        { var next=DrawableMapView.CreateView(kind,worldAssets);mapPresenter.Attach(next);mapView=next; }
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
            Text(26,20,900,46,"COLD WAR / 冷战热斗 · 计分与行动 Demo",title);
            Text(28,70,780,28,"单人双边测试  /  人工练习局面  /  世界地图占位，可更换画风",small);
            Text(820,70,434,28,captureStatus,small);
            if(fatal!=null) { Text(30,140,1180,400,"加载失败："+fatal);return; }
            if(controller==null) { Text(30,140,800,60,"正在读取演示数据…");return; }
            if(Button(26,115,90,40,"上一个")) { Reset((scenarioIndex+scenarios.Count-1)%scenarios.Count);GUIUtility.ExitGUI(); }
            Text(126,119,348,36,$"练习 {scenarioIndex+1} / {scenarios.Count}  ·  "+scenarios[scenarioIndex].Title,small);
            if(Button(485,115,90,40,"下一个")) { Reset((scenarioIndex+1)%scenarios.Count);GUIUtility.ExitGUI(); }
            if(Button(589,115,145,40,"重置当前场景")) { Reset(scenarioIndex);GUIUtility.ExitGUI(); }
            var nextMap=DrawableMapView.NextKind(mapView.Kind,worldAssets!=null);
            if(Button(745,115,85,40,nextMap==DiagnosticMapKind.Tiles?"切卡片":nextMap==DiagnosticMapKind.Table?"切列表":"切地图"))
            { SwitchMap(nextMap);GUIUtility.ExitGUI(); }
            if(Button(840,115,190,40,"操作方：美国")) { controller.SelectSide(PlayerSide.US);GUIUtility.ExitGUI(); }
            if(Button(1040,115,214,40,"操作方：苏联")) { controller.SelectSide(PlayerSide.USSR);GUIUtility.ExitGUI(); }
            var view=controller.ReadView();mapPresenter.Refresh();
            if(view.Practice!=null && view.Deployment==null)DrawPracticeStatus(view);
            if(handPanelOpen && view.Cards!=null){DrawHandPanel(view);return;}
            bool wideMap=view.Practice!=null && view.Deployment==null;
            float mapLeft=wideMap?26:294,mapWidth=wideMap?908:640;
            Block(new Rect(mapLeft,177,mapWidth,600),Panel);Block(new Rect(952,177,302,600),Panel);
            if(!wideMap)
            {
            Block(new Rect(26,177,250,600),Panel);
            Text(44,195,220,26,"当前测试局面",small);Text(44,226,214,60,scenarios[scenarioIndex].Title);
            Text(44,307,220,26,view.Deployment!=null?"当前部署方":view.Event==null?"规则要求的行动方":view.Event.Stage==InfluenceEventStage.AwaitingChoice?"规则要求的选择方":"事件触发方",small);
            Text(44,338,220,34,view.Deployment!=null?(view.Deployment.Owner.HasValue?Side(view.Deployment.Owner.Value):"部署已完成"):Side(view.Event==null?view.Actor:view.Event.Stage==InfluenceEventStage.AwaitingChoice?view.Event.Owner:view.Event.PhasingPlayer));
            Text(44,389,220,26,"你正在操作",small);Text(44,420,220,34,Side(controller.SelectedSide));
            Text(44,455,220,27,view.Deployment!=null?"初始影响力部署练习":view.Practice!=null?$"行动试玩 {view.Practice.Round} / {view.Practice.RoundLimit}":TurnText(view.Turn),small);
            Text(44,485,220,26,view.Deployment!=null?"待分配部署额度":"剩余 OPS",small);
            Text(44,513,210,60,(view.Deployment!=null?view.Deployment.RequiredAmount-controller.DeploymentDraft.Sum(a=>a.Amount):view.RemainingOps).ToString(),large);
            Text(44,594,208,150,view.Deployment!=null?"先苏联、后美国：\n1. 选国后用右侧 ±1 分配\n2. 用完额度后确认\n3. 按提示切换操作方\n草稿未确认不改变局面。\n"+(view.Cards!=null?"可先查看真实初始手牌。":"手牌只是示例，尚未发牌。"):view.Practice!=null?"行动试玩：\n1. 打开手牌选择本方牌\n2. 选择影响力或太空\n3. 投放后明确结束行动\n4. 按提示切换操作方\n当前无头条、回合末或完整事件。":view.Event==null?"试一试：\n1. 在默认目标投放 3 点\n2. 重发命令，观察不重复扣点\n3. 切换另一方，观察资格检查\n4. 重置后选择其他国家":"事件练习：\n1. 点击开始事件\n2. 按提示切换选择方\n3. 选国，按右侧提示编辑\n4. 确认后一次结算\n事件不消耗 OPS。",small);
            }
            Text(mapLeft+18,194,mapWidth-36,28,controller.SelectingCoup?"政变目标 · 绿色为合法目标，选国后在右侧确认":view.Event==null && view.Deployment==null?"世界地图 · 绿色为可投放目标":"国家面板 · 绿色为合法选择，确认后才结算",small);
            for(int i=0;i<regions.Length;i++)
                if(Button(mapLeft+18+(wideMap?i:i%3)*(wideMap?145:201),232+(wideMap?0:i/3)*39,wideMap?137:193,32,(regionIndex==i?"● ":"")+regionNames[i]))
                { regionIndex=i;if(mapView is WorldMapView world)world.RequestRegionFocus(regions[i]); }
            bool scoringPreview=wideMap && view.Practice.HasScoring;
            if(scoringPreview)DrawScoringPreview(view,mapLeft,mapWidth);
            mapView.Draw(new Rect(mapLeft+13,wideMap?(scoringPreview?311:280):321,mapWidth-26,wideMap?(scoringPreview?450:481):440),regions[regionIndex],label,small);
            var selectedCountry=view.Countries.First(c=>c.Definition.Id.Equals(controller.SelectedCountry));
            Text(972,195,262,26,"已选国家",small);Text(972,228,262,65,Name(controller.SelectedCountry),new GUIStyle(title){fontSize=24});
            Text(972,292,262,40,$"美国 {selectedCountry.US}   /   苏联 {selectedCountry.USSR}");
            Text(972,329,262,28,"控制方："+(selectedCountry.Controller.HasValue?Side(selectedCountry.Controller.Value):"无"),small);
            if(view.Deployment!=null)DrawDeploymentControls(view);
            else if(view.Event!=null)DrawEventControls(view);
            else if(view.Practice!=null)DrawPracticeControls(view);
            else
            {
            if(Button(972,374,262,43,"投放 1 点影响力"))Place(1);
            if(Button(972,427,262,43,"一次提交 3 点"))Place(3);
            GUI.enabled=controller.CanRetry;if(Button(972,480,262,37,"重发上次命令（不会重复结算）"))Retry();GUI.enabled=true;
            var result=controller.LastResult;
            Text(972,538,262,96,result==null?"选择国家后提交操作。\n非法操作会显示原因，局面不变。":result.Accepted?"操作已接受\n逐点费用："+string.Join(" + ",result.Costs):"操作被拒绝\n"+ErrorText(result.Error),small);
            }
            if(view.Cards!=null)DrawHandSummary(view);
            else
            {
            revealBoth=GUI.Toggle(new Rect(972,642,264,28),revealBoth," 查看双方示例手牌",button);
            if(revealBoth)
            {
                var both=controller.ReadBoth();Text(972,681,266,60,"美："+string.Join("、",both.USHand)+"\n苏："+string.Join("、",both.USSRHand),small);
            }
            else Text(972,681,266,62,"本方："+string.Join("、",view.OwnHand)+"\n对手："+view.OpponentHandCount+" 张（隐藏）",small);
            }
            Text(28,786,1220,27,$"真实手牌 / 部署 / 影响力、政变、太空与计分试玩；完整头条、事件、回合末与 AI 尚未接入。   版本 {view.Version} · 序号 {view.EventSequence}",small);
        }
        static string TurnText(GameTurnContext turn)
        {
            if(turn==null)return "独立操作练习";
            var era=turn.Era switch { GameEra.Early=>"早期",GameEra.Mid=>"中期",GameEra.Late=>"晚期",_=>throw new InvalidOperationException("Unknown era.") };
            return $"第 {turn.Number} 回合 · {era}";
        }
        async void Place(int points) { try { await controller.SubmitAsync(points); } catch(Exception e) { fatal=e.Message;Debug.LogException(e); } }
        async void Retry() { try { await controller.RetryAsync(); } catch(Exception e) { fatal=e.Message;Debug.LogException(e); } }
        static string ErrorText(RuleError error)
        {
            switch(error)
            {
                case RuleError.UnsupportedCardAction:return "此牌尚不支持这个用途。";
                case RuleError.CardNotOwned:return "此牌不在当前操作方手中。";
                case RuleError.ActionNotReady:return "请先完成当前阶段。";
                case RuleError.SpaceAttemptUnavailable:return "OPS 不足或本回合太空次数已用完。";
                case RuleError.ActionSliceComplete:return "本回合行动试玩已结束。";
                case RuleError.WrongActor:return "当前操作方没有本次行动资格。";
                case RuleError.IllegalTarget:return "目标不符合当前用途的限制。";
                case RuleError.InsufficientBudget:return "剩余 OPS 不足，整条命令未执行。";
                case RuleError.StaleVersion:return "局面已变化，请按最新状态重试。";
                case RuleError.CommandIdConflict:return "同一命令编号不能改变请求内容。";
                case RuleError.EventInProgress:return "先完成当前事件与选择。";
                case RuleError.EventAlreadyResolved:return "这个事件实例已经结算。";
                case RuleError.InvalidSelection:return "须提交完整、不同且合法的目标集合。";
                case RuleError.StaleDecision:return "选择请求已过期，请刷新局面。";
                case RuleError.EventRequiresRuling:return "此组合缺少已确认规则，停止结算。";
                case RuleError.EventForbidden:return "当前规则禁止该事件。";
                case RuleError.MissingTurnContext:return "缺少当前回合信息，无法结算时代事件。";
                case RuleError.SetupInProgress:return "当前处于初始部署，不能执行普通行动。";
                case RuleError.SetupNotActive:return "当前练习没有初始部署。";
                case RuleError.SetupComplete:return "部署已完成。";
                case RuleError.OutsideSetupRegion:return "该国不在当前允许部署的子区。";
                case RuleError.IncorrectAllocationTotal:return "须分配完本阶段的全部部署额度。";
                default:return error.ToString();
            }
        }
        void OnDestroy() { mapPresenter?.Dispose();if(font!=null)Destroy(font); }
    }
}
