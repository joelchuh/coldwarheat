using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
namespace ColdWarStruggle.Core
{
    public enum DefconVpEventKind { DegradeThenUsAward, ControllerAwardThenImprove }
    public enum DefconVpEventPolicy { NoActiveInterference }
    public enum DefconVpStep { DefconChanged, VpBatchClosed, NuclearWarReached }
    public sealed class DefconVpEffectDefinition
    {
        public CardId Card { get; }
        public string EffectId { get; }
        public DefconVpEventKind Kind { get; }
        public int DefconDelta { get; }
        public int VpConstant { get; }
        public DefconVpEffectDefinition(CardId card,string effect,DefconVpEventKind kind,int delta,int constant)
        {
            if(string.IsNullOrWhiteSpace(card.Value) || string.IsNullOrWhiteSpace(effect) || effect.Length>128 || !Enum.IsDefined(typeof(DefconVpEventKind),kind)
                || constant<0 || (kind==DefconVpEventKind.DegradeThenUsAward?delta>=0:delta<=0))throw new ArgumentException("Invalid immediate DEFCON/VP effect.");
            Card=card;EffectId=effect;Kind=kind;DefconDelta=delta;VpConstant=constant;
        }
    }
    public sealed class PracticeDefconVpCatalog
    {
        public CardCatalog Cards { get; }
        public IReadOnlyDictionary<CardId,DefconVpEffectDefinition> Effects { get; }
        public PracticeDefconVpCatalog(CardCatalog cards,IEnumerable<DefconVpEffectDefinition> effects)
        {
            if(cards==null || effects==null)throw new ArgumentNullException();
            var copy=effects.Take(cards.Cards.Count+1).ToArray();
            if(copy.Length==0 || copy.Length>cards.Cards.Count || copy.Any(e=>e==null) || copy.Select(e=>e.Card).Distinct().Count()!=copy.Length)throw new ArgumentException("Invalid event catalog.");
            foreach(var e in copy)
            {
                var p=cards.GetCard(e.Card);var affiliation=e.Kind==DefconVpEventKind.DegradeThenUsAward?CardAffiliation.US:CardAffiliation.Neutral;
                if(p.Kind!=CardKind.Event || p.Effect.EffectId!=e.EffectId || p.Affiliation!=affiliation || p.DisplayPersistentReminder)throw new ArgumentException("Printed event binding mismatch.");
            }
            Cards=cards;Effects=new ReadOnlyDictionary<CardId,DefconVpEffectDefinition>(copy.ToDictionary(e=>e.Card));
        }
    }
}
