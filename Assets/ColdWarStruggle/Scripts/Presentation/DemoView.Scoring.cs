using System;
using ColdWarStruggle.Core;
using ColdWarStruggle.Application;
namespace ColdWarStruggle.Presentation
{
    public sealed partial class DemoView
    {
        string ScoringRegionName(string region)
        {var index=Array.IndexOf(regions,region);return index>=0?regionNames[index]:region=="region.southeast_asia"?"东南亚":region;}
        static string ScoreSummary(PublicRegionScore score)=>score.EuropeControlWinner.HasValue
            ?"欧洲控制："+Side(score.EuropeControlWinner.Value)+" 将直接获胜"
            :$"美 {score.US.TotalVp} / 苏 {score.USSR.TotalVp} · 净 VP {score.NetVpForUS:+0;-0;0}";
        void DrawScoringPreview(PlayerViewSnapshot view,float left,float width)
        {
            var preview=controller.ScoringPreview(regions[regionIndex]);
            Text(left+18,270,width-36,33,preview.Error==RuleError.None
                ?ScoringRegionName(preview.Score.RegionId)+"公开预览："+ScoreSummary(preview.Score)+(view.Practice?.EndReason!=null?"（终局复盘）":"（出牌时复核）")
                :"计分预览："+ErrorText(preview.Error),small);
        }
        void DrawScoringChoice(CardId card)
        {
            var preview=controller.ScoringPreview(printedCards.GetCard(card).ScoringRegion);
            Text(972,412,262,57,preview.Error==RuleError.None?"当前局面：\n"+ScoreSummary(preview.Score):ErrorText(preview.Error),small);
            if(Button(972,479,262,38,"打出计分牌 · 立即结算"))UseCard(CardActionKind.Scoring);
            if(Button(972,525,262,29,"重新选择手牌"))OpenCards();
        }
        static string VictoryMessage(PublicPracticeView practice)
        {
            var reason=practice.EndReason==VictoryCheckpoint.NuclearWarReached?"核战结束":practice.EndReason==VictoryCheckpoint.ScoringResolved?"地区计分结束":practice.EndReason==VictoryCheckpoint.HeldScoringAudited?"回合末审核结束":"胜利点达到获胜条件";
            return reason+" · "+Side(practice.Winner.Value)+" 获胜。\n"+(practice.EndReason==VictoryCheckpoint.NuclearWarReached?"导致 DEFCON 降至 1 的行动方判负。\n":"")+"可重置后尝试其他路线。";
        }
    }
}
