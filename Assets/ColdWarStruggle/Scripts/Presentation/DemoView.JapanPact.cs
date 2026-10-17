using ColdWarStruggle.Application;
namespace ColdWarStruggle.Presentation
{
    public sealed partial class DemoView
    {
        string JapanPactSummary(PublicJapanPactEffect effect)
            =>"长期保护：美日安保 · "+Name(effect.Country)+" · 苏联不可政变/调整";
        public string ReadPermanentEffectsSummary()
        {var effect=controller.ReadView().JapanPact;return effect==null?"":JapanPactSummary(effect);}
        string JapanPactEventMessage(PublicJapanPactEffect effect,PublicCardEventResult grant)
            =>JapanPactSummary(effect)+"\n回合末不会失效。"+(grant==null?"":"\n"+InfluenceEventMessage(grant));
    }
}
