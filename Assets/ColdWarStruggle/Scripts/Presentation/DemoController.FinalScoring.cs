using System.Threading.Tasks;
using ColdWarStruggle.Core;
using ColdWarStruggle.Application;
namespace ColdWarStruggle.Presentation
{
    public sealed partial class DemoController
    {
        public Task<CardActionResult> CompleteFinalScoringAsync()=>SendCard(CardActionKind.FinalScoring);
        public string FinalScoringPrompt
        {
            get
            {
                var v=ReadView();var p=v.Practice;if(p?.FinalScoringEnabled!=true)return null;
                if(p.FinalScoring!=null)return "最终计分已完成；可查看六地区明细或返回终局地图。";
                if(v.SpaceDiscard?.Status==SpaceEndTurnDiscardStatus.Pending)return "请先完成太空回合末弃牌或保留选择，再执行最终计分。";
                if(p.FinalScoringAvailable)return "第10回合审核已完成。下一步：六地区最终计分与中国牌奖励。";
                if(p.FinalScoringError==RuleError.GameAlreadyEnded)return "对局已结束，无需最终计分。";
                if(v.Turn.Number!=10)return "最终计分将在第10回合全部行动和回合末审核后进行。";
                if(p.FinalScoringError==RuleError.UnsupportedCardAction)return "尚有未接入的太空期末能力，不能跳过后结算。";
                return "请先完成本回合行动与军事／扣留计分牌审核。";
            }
        }
    }
}
