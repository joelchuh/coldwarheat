using ColdWarStruggle.Core;
namespace ColdWarStruggle.Application
{
    public sealed class PublicJapanPactEffect
    {
        public CardId Card { get; }
        public string EffectId { get; }
        public CountryId Country { get; }
        public PlayerSide BlockedActor { get; }
        public string Duration { get; }
        internal PublicJapanPactEffect(JapanPactInstance instance)
        {
            Card=instance.Definition.Card;EffectId=instance.Definition.EffectId;Country=instance.Definition.ImmediateInfluence.Country;
            BlockedActor=PlayerSide.USSR;Duration=instance.Definition.Duration;
        }
    }
}
