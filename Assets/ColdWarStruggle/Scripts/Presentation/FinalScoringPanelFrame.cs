using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ColdWarStruggle.Core;
using ColdWarStruggle.Application;
namespace ColdWarStruggle.Presentation
{
    public sealed class FinalScoringRow
    {
        public string RegionId { get; }
        public string Label { get; }
        public string US { get; }
        public string USSR { get; }
        public string Net { get; }
        internal FinalScoringRow(PublicRegionScore score)
        {
            RegionId=score.RegionId;
            Label=RegionId switch {"region.europe"=>"欧洲","region.asia"=>"亚洲","region.middle_east"=>"中东","region.africa"=>"非洲","region.central_america"=>"中美洲","region.south_america"=>"南美洲",_=>RegionId};
            US=SideScore(score.US);USSR=SideScore(score.USSR);
            Net=score.EuropeControlWinner.HasValue?(score.EuropeControlWinner==PlayerSide.US?"美国":"苏联")+"直接获胜":FinalScoringPanelFrame.Signed(score.NetVpForUS.Value);
        }
        static string SideScore(RegionSideScore score)
        {
            var tier=score.Tier switch {RegionTier.Presence=>"存在",RegionTier.Domination=>"支配",RegionTier.Control=>"控制",_=>"无"};
            if(!score.TotalVp.HasValue)return "控制欧洲 · 直接获胜";
            return tier+" · "+score.TotalVp.Value+" VP\n基础 "+score.BaseVp+" + 战场 "+score.BattlegroundVp+" + 邻接 "+score.EnemyAdjacencyVp;
        }
    }
    public sealed class FinalScoringPanelFrame
    {
        public string Outcome { get; }
        public string Equation { get; }
        public string ChinaLine { get; }
        public IReadOnlyList<FinalScoringRow> Rows { get; }
        internal static string Signed(int value)=>value.ToString("+0;-0;0",CultureInfo.InvariantCulture);
        FinalScoringPanelFrame(PublicFinalScoring r)
        {
            Outcome=(r.IsDraw?"平局":r.Winner==PlayerSide.US?"美国获胜":"苏联获胜")+(r.EuropeControl?" · 欧洲控制":" · 最终计分");
            Equation=r.EuropeControl?"欧洲控制直接决定胜负；胜利点保留为 "+r.BeforeVp+"。"
                :"审核后 VP "+r.BeforeVp+" + 最终净分 ("+Signed(r.NetVpForUS.Value)+") = "+r.AfterVp+"（美国正 / 苏联负）";
            ChinaLine="中国牌："+(r.ChinaOwner==PlayerSide.US?"美国":"苏联")+"持有 · 奖励 "+Signed(r.ChinaVpForUS)+" VP（美国正 / 苏联负）";
            Rows=Array.AsReadOnly(r.Regions.Select(x=>new FinalScoringRow(x)).ToArray());
        }
        public static FinalScoringPanelFrame Create(PublicFinalScoring report)
            =>new FinalScoringPanelFrame(report??throw new ArgumentNullException(nameof(report)));
    }
}
