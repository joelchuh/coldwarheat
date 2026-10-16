using System;
namespace ColdWarStruggle.Core
{
    public sealed class OlympicGamesDefinition
    {
        public CardCatalog Cards { get; }
        public CardDefinition Card { get; }
        public string EffectId { get; }
        public StandardDieDefinition Die { get; }
        public int SponsorModifier { get; }
        public int WinnerVp { get; }
        public int TieVp { get; }
        public int DefconDelta { get; }
        public int BoycottBaseOps { get; }
        public OlympicGamesDefinition(CardCatalog cards,StandardDieDefinition die,CardId card,string effectId,int sponsorModifier,int winnerVp,int tieVp,int defconDelta,int boycottBaseOps)
        {
            if(cards==null || die==null)throw new ArgumentNullException();
            if(card.Value!="card.olympic_games" || effectId!="effect.olympic_games" || !cards.Contains(card)
                || sponsorModifier!=2 || winnerVp!=2 || tieVp!=0 || defconDelta!=-1 || boycottBaseOps!=4)throw new ArgumentException("Unsupported Olympic profile.");
            var c=cards.GetCard(card);
            if(c.Kind!=CardKind.Event || c.PrintedNumber!=20 || c.PrintedOps!=2 || c.Affiliation!=CardAffiliation.Neutral || c.Era!=GameEra.Early
                || c.RemoveAfterResolvedEvent || c.DisplayPersistentReminder || c.Effect.EffectId!=effectId || c.Effect.ContractVersion!="0.1.0"
                || c.Effect.Kind!=CardParameterBindingKind.Bound || c.Effect.Parameters.Count!=1 || c.Effect.Parameters[0].DatasetId!="effects.deluxe2015.olympic_games"
                || c.Effect.Parameters[0].JsonPointer!="/parameters/olympic_games")throw new ArgumentException("Printed card does not bind this Olympic event.");
            Cards=cards;Card=c;Die=die;EffectId=effectId;SponsorModifier=sponsorModifier;WinnerVp=winnerVp;TieVp=tieVp;DefconDelta=defconDelta;BoycottBaseOps=boycottBaseOps;
        }
    }
}
